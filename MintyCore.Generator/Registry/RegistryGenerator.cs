using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Newtonsoft.Json;
using static MintyCoreGenerator.Registry.SourceBuilder;
using static MintyCoreGenerator.DiagnosticsHelper;

namespace MintyCoreGenerator.Registry;

[Generator]
public class RegistryGenerator : ISourceGenerator
{
    private const string RegistryInterfaceName = "MintyCore.Modding.IRegistry";
    private const string RegistryClassAttributeName = "MintyCore.Modding.Attributes.RegistryAttribute";
    private const string RegistryMethodAttributeName = "MintyCore.Modding.Attributes.RegisterMethodAttribute";
    private const string RegisterBaseAttributeName = "MintyCore.Modding.Attributes.RegisterBaseAttribute";
    private const string IdentificationName = "MintyCore.Utils.Identification";
    private const string ModName = "MintyCore.Modding.IMod";

    private RegistryData _registryData = new();

    private INamedTypeSymbol? ModSymbol { get; set; }

    private Dictionary<(string registryClass, int registryPhase), List<RegisterMethod>> _registerMethods = new();

    public void Initialize(GeneratorInitializationContext context)
    {
    }

    public void Execute(GeneratorExecutionContext context)
    {
        _registryData = new();
        ModSymbol = null;
        _registerMethods = new();

        var nodes = from tree in context.Compilation.SyntaxTrees
            from syntaxNode in tree.GetRoot().DescendantNodes()
            select syntaxNode;
        var syntaxNodes = nodes as SyntaxNode[] ?? nodes.ToArray();

        var classNodesEnumerable = from node in syntaxNodes
            where node.IsKind(SyntaxKind.ClassDeclaration) && node is ClassDeclarationSyntax
            select node as ClassDeclarationSyntax;
        var classNodes = classNodesEnumerable as ClassDeclarationSyntax[] ?? classNodesEnumerable.ToArray();


        //Find mod class
        foreach (var classNode in classNodes)
        {
            var semanticModel = context.Compilation.GetSemanticModel(classNode.SyntaxTree);
            if (semanticModel.GetDeclaredSymbol(classNode) is not { } classSymbol)
                continue;

            if (classSymbol.IsAbstract || classSymbol.TypeKind != TypeKind.Class) continue;

            if (!classSymbol.Interfaces.Any(@interface => @interface.ToString().Equals(ModName))) continue;

            ModSymbol = classSymbol;
            break;
        }

        //Create register attributes
        foreach (var classNode in classNodes)
        {
            if (classNode is not {AttributeLists.Count: > 0, BaseList.Types.Count: > 0}) continue;

            var semanticModel = context.Compilation.GetSemanticModel(classNode.SyntaxTree);
            var result = IsValidRegistryClass(semanticModel, classNode);
            if (result is null) continue;

            GenerateRegistryAttributes(context, result);
        }

        //Find property registries
        var propertyNodes = from node in syntaxNodes
            where node is PropertyDeclarationSyntax {AttributeLists.Count: > 0}
            select node as PropertyDeclarationSyntax;
        foreach (var propertyNode in propertyNodes)
        {
            var result =
                CheckPotentialRegistryAttribute(context.Compilation.GetSemanticModel(propertyNode.SyntaxTree),
                    propertyNode);
            if (result is null) continue;

            FetchRegisterMethodInfo(context, result);
        }

        //Find generic registries
        var typeDeclNodes = from node in syntaxNodes
            where node is TypeDeclarationSyntax {AttributeLists.Count: > 0}
            select node as TypeDeclarationSyntax;
        foreach (var typeDeclNode in typeDeclNodes)
        {
            if (typeDeclNode is null) continue;
            var result =
                CheckPotentialRegistryAttribute(context.Compilation.GetSemanticModel(typeDeclNode.SyntaxTree),
                    typeDeclNode);
            if (result is null) continue;

            FetchRegisterMethodInfo(context, result);
        }


        var registryFile =
            context.AdditionalFiles.FirstOrDefault(file => file.Path.EndsWith("GenerateRegistryData.json"));
        if (registryFile is not null)
            ProcessJsonFile(registryFile);
        GenerateRegistrySource(context);
    }

    private void ProcessJsonFile(AdditionalText registryFile)
    {
        var fileText = registryFile.GetText();
        if (fileText is null) return;

        var reader = new JsonTextReader(new StringReader(fileText.ToString()));

        var serializer = JsonSerializer.Create();

        var jsonDataArray = serializer.Deserialize<JsonData[]>(reader);

        if (jsonDataArray is null) return;

        foreach (var jsonData in jsonDataArray)
        {
            RegisterMethod method = new()
            {
                CategoryId = jsonData.RegistryId,
                ClassName = jsonData.FullRegistryClassName,
                MethodName = jsonData.RegisterMethodName,
                HasFile = true,
                RegistryPhase = jsonData.RegistryPhase,
                RegisterMethodType = RegisterMethodType.File
            };
            foreach (var entry in jsonData.ToRegister)
            {
                method.Id = entry.Id;
                method.File = entry.File;

                var key = (method.ClassName, method.RegistryPhase);

                if (!_registerMethods.ContainsKey(key))
                    _registerMethods.Add(key, new List<RegisterMethod>());

                _registerMethods[key].Add(method);
            }
        }
    }

    private void GenerateRegistrySource(GeneratorExecutionContext context)
    {
        if (ModSymbol is null)
        {
            //context.ReportDiagnostic(DiagnosticsHelper.NoModFound());
            return;
        }

        List<string> registryEventSubscribeExpressions = new();
        List<string> registryEventUnsubscribeExpressions = new();

        var registryNamespace = $"{ModSymbol.ContainingNamespace}.Identifications";

        foreach (var registerMethod in _registerMethods)
        {
            var (registryClass, registryPhase) = registerMethod.Key;
            var registerMethodInfos = registerMethod.Value;
            if (registryClass is null || registerMethodInfos is null) continue;

            context.AddSource($"{registryClass}.{registryPhase}.g.cs",
                ComposeRegistryMethodAndClassExtension(registryClass, registryPhase, registerMethodInfos,
                    registryNamespace, ModSymbol.ToString(), out string eventSubscribeExpressions,
                    out string eventUnsubscribeExpressions));

            registryEventSubscribeExpressions.Add(eventSubscribeExpressions);
            registryEventUnsubscribeExpressions.Add(eventUnsubscribeExpressions);
        }

        context.AddSource($"{ModSymbol}.reg.g.cs",
            ComposeRegistryRegisterMethod(_registryData, registryNamespace, ModSymbol.ToString(),
                out var registerMethodToCall));

        context.AddSource($"{ModSymbol}.g.cs",
            ComposeRegisterMethod(ModSymbol, registryEventSubscribeExpressions, registryEventUnsubscribeExpressions,
                registerMethodToCall));
    }

    private void FetchRegisterMethodInfo(GeneratorExecutionContext context, (ISymbol, SyntaxNode)? symbolAndNode)
    {
        if (symbolAndNode is null) return;

        var (symbol, node) = symbolAndNode.Value;

        RegisterMethod registerMethod = default;
        bool found = false;

        var datas = symbol.GetAttributes();
        for (var index = 0; index < datas.Length; index++)
        {
            var attribute = datas[index];
            if (attribute.AttributeClass is not { } attributeClass) continue;

            if (attributeClass.Kind != SymbolKind.ErrorType &&
                (attributeClass.BaseType is null ||
                 !attributeClass.BaseType.ToString().Equals(RegisterBaseAttributeName))) continue;

            if (_registryData.GetRegisterMethod(attribute, node, out registerMethod, out var diagnostic))
            {
                found = true;
                break;
            }

            if (diagnostic is null) continue;


            context.ReportDiagnostic(diagnostic);
            if (diagnostic.IsWarningAsError) return;
        }

        if (!found || registerMethod.ClassName is null) return;

        switch (registerMethod.RegisterMethodType)
        {
            case RegisterMethodType.Generic:
            {
                if (symbol is not INamedTypeSymbol namedTypeSymbol) return;
                if (!GenericHelper.CheckValidConstraint(registerMethod.GenericConstraints,
                        registerMethod.GenericConstraintTypes, namedTypeSymbol))
                {
                    context.ReportDiagnostic(DiagnosticsHelper.InvalidGenericTypeForRegistry(namedTypeSymbol));
                    return;
                }

                registerMethod.TypeToRegister = namedTypeSymbol.ToString();
                break;
            }

            case RegisterMethodType.Property:
            {
                if (symbol is not IPropertySymbol {Type: INamedTypeSymbol namedTypeSymbol} propertySymbol) return;
                if (!namedTypeSymbol.ToString().Equals(registerMethod.PropertyType))
                {
                    context.ReportDiagnostic(DiagnosticsHelper.InvalidPropertyTypeForRegistry(propertySymbol));
                    return;
                }

                registerMethod.PropertyToRegister = propertySymbol.ToString();
                break;
            }
        }

        (string, int) key = (registerMethod.ClassName, registerMethod.RegistryPhase);
        if (!_registerMethods.ContainsKey(key)) _registerMethods.Add(key, new List<RegisterMethod>());

        _registerMethods[key].Add(registerMethod);
    }

    private static (ISymbol, SyntaxNode)? CheckPotentialRegistryAttribute(SemanticModel semanticModel, SyntaxNode node)
    {
        var typeSymbol = semanticModel.GetDeclaredSymbol(node);
        if (typeSymbol is null) return null;

        var attributes = typeSymbol.GetAttributes();

        foreach (var attribute in attributes)
        {
            if (attribute.AttributeClass is not { } attributeClass) continue;

            //A error type is a potential register attribute
            if (attributeClass.Kind == SymbolKind.ErrorType) return (typeSymbol, node);

            //If the base type of the attribute is RegisterBaseAttributeName (compare with const string at the beginning)
            //This type has a registry attribute
            if (attributeClass.BaseType is not null
                && attributeClass.BaseType.ToString().Equals(RegisterBaseAttributeName))
                return (typeSymbol, node);
        }

        return null;
    }

    private void GenerateRegistryAttributes(GeneratorExecutionContext context,
        INamedTypeSymbol classSymbol)
    {
        var registryClass = classSymbol;

        List<(IMethodSymbol methodSymbol, RegisterMethodType registerType, int registryPhase, RegisterMethodOptions
                registerMethodOptions)>
            registerMethods = new();

        var registryAttribute = registryClass.GetAttributes().First(attribute =>
            attribute.AttributeClass!.ToString().Equals(RegistryClassAttributeName));
        var registryId = registryAttribute.ConstructorArguments.First().Value as string;

        //Search for all register methods
        foreach (var memberSymbol in registryClass.GetMembers())
        {
            if (memberSymbol is not IMethodSymbol methodSymbol) continue;

            var attributes = memberSymbol.GetAttributes();
            if (attributes.Length == 0) continue;

            //Search for the register method attribute
            AttributeData? methodAttribute = null;
            foreach (var attributeData in attributes)
            {
                var attributeClass = attributeData.AttributeClass;
                if (attributeClass is null) continue;

                if (!attributeClass.ToString().Equals(RegistryMethodAttributeName)) continue;

                methodAttribute = attributeData;
                break;
            }

            if (methodAttribute is null || methodAttribute.ConstructorArguments.Length < 2) continue;

            if (methodSymbol.Parameters.Length == 0)
            {
                context.ReportDiagnostic(InvalidRegisterMethod(methodSymbol.Locations.FirstOrDefault(),
                    methodSymbol.ToString(), "No parameters found"));
                continue;
            }

            var firstParameter = methodSymbol.Parameters[0];
            if (!firstParameter.Type.ToString().Equals(IdentificationName))
            {
                context.ReportDiagnostic(InvalidRegisterMethod(methodSymbol.Locations.FirstOrDefault(),
                    methodSymbol.ToString(), "First parameter must be of type Identification"));
                continue;
            }

            var registryPhaseValue = methodAttribute.ConstructorArguments[0].Value;
            var registryPhase = (int?) registryPhaseValue ?? 0;

            var registerMethodOptionsValue = methodAttribute.ConstructorArguments[1].Value;
            var registerMethodOptions = (RegisterMethodOptions) ((int?) registerMethodOptionsValue ?? 0);

            //if registerMethodOptions has the HasFile and UseExistingId Flag report an error
            if ((registerMethodOptions & (RegisterMethodOptions.HasFile | RegisterMethodOptions.UseExistingId)) ==
                (RegisterMethodOptions.HasFile | RegisterMethodOptions.UseExistingId))
            {
                context.ReportDiagnostic(InvalidRegisterMethod(methodSymbol.Locations.FirstOrDefault(),
                    methodSymbol.ToString(), "Invalid Flag combination"));
                continue;
            }

            //Get the register method type
            var parameterCount = methodSymbol.Parameters.Length;
            var genericTypeCount = methodSymbol.TypeParameters.Length;

            var hasFile = (registerMethodOptions & RegisterMethodOptions.HasFile) != 0;
            var registerMethodType = (parameterCount, genericTypeCount, hasFile) switch
            {
                (1, 0, true) => RegisterMethodType.File,
                (2, 0, _) => RegisterMethodType.Property,
                (1, 1, _) => RegisterMethodType.Generic,
                _ => RegisterMethodType.Invalid
            };

            if (registerMethodType == RegisterMethodType.Invalid)
            {
                context.ReportDiagnostic(InvalidRegisterMethod(methodSymbol.Locations.FirstOrDefault(),
                    methodSymbol.ToString(), "Register Method not supported"));
                continue;
            }

            registerMethods.Add((methodSymbol, registerMethodType, registryPhase, registerMethodOptions));
        }

        if (registerMethods.Count == 0)
        {
            context.ReportDiagnostic(NoRegisterMethods(registryClass.Locations.FirstOrDefault(),
                registryClass.ToString()));
        }

        List<RegisterMethod> registerMethodList = new List<RegisterMethod>();

        //Populate register method info class
        foreach (var (methodSymbol, registerType, registryPhase, options) in registerMethods)
        {
            if (registerType == RegisterMethodType.Invalid) continue;

            RegisterMethod method = new()
            {
                HasFile = (options & RegisterMethodOptions.HasFile) != 0,
                UseExistingId = (options & RegisterMethodOptions.UseExistingId) != 0,
                MethodName = methodSymbol.Name,
                ClassName = registryClass.ToString(),
                RegistryPhase = registryPhase,
                RegisterMethodType = registerType,
                CategoryId = registryId!,
                ResourceSubFolder = (string?) registryAttribute.ConstructorArguments[1].Value
            };

            switch (registerType)
            {
                case RegisterMethodType.Generic:
                {
                    var (constraints, typeConstraints) =
                        GenericHelper.GetGenericConstraint(methodSymbol.TypeParameters[0]);
                    method.GenericConstraints = constraints;
                    method.GenericConstraintTypes = typeConstraints;
                    break;
                }

                case RegisterMethodType.Property:
                {
                    method.PropertyType = methodSymbol.Parameters[1].Type.ToString();
                    break;
                }
            }

            registerMethodList.Add(method);
            _registryData.RegisterMethods.Add(method.MethodName, method);
        }

        context.AddSource($"{registryClass.ToString().Replace('.', '_')}_Att.g.cs",
            ComposeRegistryAttribute(registryClass, registerMethodList));
    }

    private static INamedTypeSymbol? IsValidRegistryClass(SemanticModel semanticModel, SyntaxNode node)
    {
        if (semanticModel.GetDeclaredSymbol(node) is not INamedTypeSymbol classSymbol)
            return null;

        var interfaces = classSymbol.AllInterfaces;
        var hasInterface = false;
        for (int i = 0; i < interfaces.Length && !hasInterface; i++)
        {
            var @interface = interfaces[i];
            hasInterface |= @interface.ToString().Equals(RegistryInterfaceName);
        }

        if (!hasInterface) return null;

        var registryAttributeData = classSymbol.GetAttributes().FirstOrDefault(attributeData =>
            attributeData.AttributeClass is { } attributeClass &&
            attributeClass.ToString().Equals(RegistryClassAttributeName));

        if (registryAttributeData is null) return null;

        return classSymbol;
    }
}

internal struct JsonData
{
    public string FullRegistryClassName { get; set; }
    public string RegistryId { get; set; }
    public int RegistryPhase { get; set; }
    public string RegisterMethodName { get; set; }

    public Entry[] ToRegister { get; set; }
}

struct Entry
{
    public string Id { get; set; }
    public string File { get; set; }
}

[Flags]
public enum RegisterMethodOptions
{
    None = 0,
    HasFile = 1 << 0,
    UseExistingId = 1 << 1
}