using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Scriban;
using SharedCode;
using static MintyCore.Generator.Registry.RegistryHelper;
using static MintyCore.Generator.DiagnosticsHelper;

namespace MintyCore.Generator.Registry;

public class RegistryGeneratoor : IIncrementalGenerator
{
    private const string TemplateDirectory = "MintyCore.Generator.Registry.Templates";

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        //Few things to note:
        //All called methods should be static. As this is a huge pipeline setup which will be conditionally run when needed there should no state be saved in the generator itself.
        //The lambda methods created in the pipeline should be as minimalistic as possible and call into static methods.
        //Those will contain the needed logic for the pipeline. By this those methods can be tested easily.

        //Gather all mod symbols in the compilation. This should result in an array with the length of one
        var modSymbolProvider = context.SyntaxProvider.CreateSyntaxProvider(
            predicate: static (node, _) => node is ClassDeclarationSyntax,
            transform: static (syntaxContext, _) =>
            {
                var classNode = (ClassDeclarationSyntax)syntaxContext.Node;
                return ClassSyntaxAsModSymbol(syntaxContext.SemanticModel, classNode);
            }).Where(x => x is not null).Select<INamedTypeSymbol?, INamedTypeSymbol>((x, _) => x!).Collect();

        //Extract all informations from newly added registry classes
        var registryInformationProvider = context.SyntaxProvider.CreateSyntaxProvider(
                predicate: static (node, _) => node is ClassDeclarationSyntax,
                transform: static (INamedTypeSymbol? typeSymbol, AttributeData? data) (
                    syntaxContext, _) =>
                {
                    var classNode = (ClassDeclarationSyntax)syntaxContext.Node;
                    var symbol = syntaxContext.SemanticModel.GetDeclaredSymbol(classNode);
                    if (symbol is not INamedTypeSymbol namedTypeSymbol) return (null, null);

                    // ReSharper disable once ConvertIfStatementToReturnStatement for readability
                    if (!IsIRegistryClass(namedTypeSymbol)) return (null, null);

                    return (namedTypeSymbol, GetRegistryClassAttributeDataOrNull(namedTypeSymbol));
                }
            ).Where(x => x.Item1 is not null && x.Item2 is not null)
            .Select((x, _) => (x.Item1!, x.Item2!))
            .Select((x, _) =>
            {
                List<Diagnostic> diagnostics = new();
                var registerMethodInfos = ExtractRegisterMethodsFromRegistryClass(x.Item1, x.Item2, diagnostics);
                return (registerMethodInfos, diagnostics);
            });

        var registryMethodDiagnosticsProvider = registryInformationProvider.SelectMany((x, _) => x.diagnostics);

        //Register a fake source output for the registry method diagnostics
        context.RegisterSourceOutput(registryMethodDiagnosticsProvider,
            static (spc, diagnostic) => { spc.ReportDiagnostic(diagnostic); });

        var flatRegistryInformationProvider = registryInformationProvider.SelectMany((x, _) => x.registerMethodInfos);

        context.RegisterSourceOutput(flatRegistryInformationProvider, static (productionContext, registerMethodInfo) =>
        {
            if (registerMethodInfo.MethodType != RegisterMethodType.File)
            {
                productionContext.AddSource(
                    $"{registerMethodInfo.Namespace}.{registerMethodInfo.ClassName}.{registerMethodInfo.MethodName}.Attribute.g.cs",
                    GenerateRegisterMethodAttributeSource(registerMethodInfo));
            }

            productionContext.AddSource(
                $"{registerMethodInfo.Namespace}.{registerMethodInfo.ClassName}.{registerMethodInfo.MethodName}.Info.g.cs",
                GenerateRegisterMethodInfoSource(registerMethodInfo));
        });


        //Find all existing registry method infos (added by other mods)
        IncrementalValueProvider<ImmutableArray<RegisterMethod>> existingRegistryMethodInfoProvider = context
            .CompilationProvider.Select((compilation, token) =>
            {
                var referencedMetadatas = compilation.References;

                foreach (var metadata in referencedMetadatas)
                {
                    var symbol = compilation.GetAssemblyOrModuleSymbol(metadata);
                    if (symbol is IModuleSymbol moduleSymbol)
                    {
                        
                    }

                    if (symbol is IAssemblySymbol assemblySymbol)
                    {
                        assemblySymbol.Glob
                    }
                }

                return new ImmutableArray<RegisterMethod>();
            });
        
        context.RegisterSourceOutput(existingRegistryMethodInfoProvider, (productionContext, array) =>
        {
            productionContext.AddSource("a", "");
        });
        
    }

    public static string GenerateRegisterMethodInfoSource(RegisterMethod registerMethodInfo)
    {
        var template =
            Template.Parse(EmbeddedFileHelper.ReadEmbeddedTextFileOrThrow($"{TemplateDirectory}.RegisterMethodInfo.sbncs"));
        
        Dictionary<string, object?> templateArguments = new ()
        {
            { "RegistryNamespace", registerMethodInfo.Namespace },
            { "RegisterMethodName", registerMethodInfo.MethodName },
            { "UseExistingId", registerMethodInfo.UseExistingId },
            { "RegistryClassName", registerMethodInfo.ClassName },
            { "RegisterType", (int)registerMethodInfo.MethodType },
            { "SubFolder", registerMethodInfo.ResourceSubFolder },
            { "HasFile", registerMethodInfo.HasFile },
            { "GenericConstraints", (int)registerMethodInfo.Constraints },
            { "GenericTypeConstraints", registerMethodInfo.GenericConstraintTypes },
            { "RegistryPhase", registerMethodInfo.RegistryPhase },
            { "PropertyType", registerMethodInfo.PropertyType },
            { "CategoryId", registerMethodInfo.CategoryId }
        };
        
        return template.Render(templateArguments);
    }

    public static string GenerateRegisterMethodAttributeSource(RegisterMethod registerMethodInfo)
    {
        var template =
            Template.Parse(EmbeddedFileHelper.ReadEmbeddedTextFileOrThrow($"{TemplateDirectory}.Attribute.sbncs"));

        Dictionary<string, object?> templateArguments = new()
        {
            {
                "StructTarget",
                registerMethodInfo.MethodType == RegisterMethodType.Generic &&
                (registerMethodInfo.Constraints & GenericConstraints.ValueType) != 0
            },
            {
                "ClassTarget",
                registerMethodInfo.MethodType == RegisterMethodType.Generic &&
                (registerMethodInfo.Constraints & GenericConstraints.ReferenceType) != 0
            },
            { "PropertyTarget", registerMethodInfo.MethodType == RegisterMethodType.Property },
            { "RegistryNamespace", registerMethodInfo.Namespace },
            { "RegistryClassName", registerMethodInfo.ClassName},
            {"RegisterMethodName", registerMethodInfo.MethodName},
            {"UseExistingId", registerMethodInfo.UseExistingId}
        };

        return template.Render(templateArguments);
    }

    public static INamedTypeSymbol? ClassSyntaxAsModSymbol(SemanticModel semanticModel,
        ClassDeclarationSyntax classNode)
    {
        var symbol = semanticModel.GetDeclaredSymbol(classNode);

        if (symbol is not INamedTypeSymbol { TypeKind: TypeKind.Class, IsAbstract: false } classSymbol)
        {
            return null;
        }

        return classSymbol.AllInterfaces.Any(i => i.ToDisplayString() == ModName) ? classSymbol : null;
    }

    public static bool IsIRegistryClass(INamedTypeSymbol classSymbol)
    {
        var interfaces = classSymbol.AllInterfaces;
        return Enumerable.Any(interfaces, i => i.ToString().Equals(RegistryInterfaceName));
    }

    public static AttributeData? GetRegistryClassAttributeDataOrNull(INamedTypeSymbol classSymbol)
    {
        return classSymbol.GetAttributes().FirstOrDefault(attributeData =>
            attributeData.AttributeClass is { } attributeClass &&
            attributeClass.ToString().Equals(RegistryClassAttributeName));
    }

    public static List<RegisterMethod> ExtractRegisterMethodsFromRegistryClass(INamedTypeSymbol registryClass,
        AttributeData registryAttribute, List<Diagnostic> diagnostics)
    {
        List<(IMethodSymbol methodSymbol, RegisterMethodType registerType, int registryPhase, RegisterMethodOptions
                registerMethodOptions)>
            registerMethods = new();

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
                diagnostics.Add(InvalidRegisterMethod(methodSymbol.Locations.FirstOrDefault(),
                    methodSymbol.ToString(), "No parameters found"));
                continue;
            }

            var firstParameter = methodSymbol.Parameters[0];
            if (!firstParameter.Type.ToString().Equals(IdentificationName))
            {
                diagnostics.Add(InvalidRegisterMethod(methodSymbol.Locations.FirstOrDefault(),
                    methodSymbol.ToString(), "First parameter must be of type Identification"));
                continue;
            }

            var registryPhaseValue = methodAttribute.ConstructorArguments[0].Value;
            var registryPhase = (int?)registryPhaseValue ?? 0;

            var registerMethodOptionsValue = methodAttribute.ConstructorArguments[1].Value;
            var registerMethodOptions = (RegisterMethodOptions)((int?)registerMethodOptionsValue ?? 0);

            //if registerMethodOptions has the HasFile and UseExistingId Flag report an error
            if ((registerMethodOptions & (RegisterMethodOptions.HasFile | RegisterMethodOptions.UseExistingId)) ==
                (RegisterMethodOptions.HasFile | RegisterMethodOptions.UseExistingId))
            {
                diagnostics.Add(InvalidRegisterMethod(methodSymbol.Locations.FirstOrDefault(),
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
                diagnostics.Add(InvalidRegisterMethod(methodSymbol.Locations.FirstOrDefault(),
                    methodSymbol.ToString(), "Register Method not supported"));
                continue;
            }

            registerMethods.Add((methodSymbol, registerMethodType, registryPhase, registerMethodOptions));
        }

        if (registerMethods.Count == 0)
        {
            diagnostics.Add(NoRegisterMethods(registryClass.Locations.FirstOrDefault(),
                registryClass.ToString()));
        }

        List<RegisterMethod> registerMethodList = new List<RegisterMethod>();

        //Populate register method info class
        foreach (var (methodSymbol, registerType, registryPhase, options) in registerMethods)
        {
            //TODO add a proper check that the second constructor argument is not null
            //It should never happen as its a default parameter but just in case

            RegisterMethod method = new()
            {
                HasFile = (options & RegisterMethodOptions.HasFile) != 0,
                UseExistingId = (options & RegisterMethodOptions.UseExistingId) != 0,
                MethodName = methodSymbol.Name,
                ClassName = registryClass.Name,
                Namespace = registryClass.ContainingNamespace?.ToString() ?? "",
                RegistryPhase = registryPhase,
                MethodType = registerType,
                CategoryId = registryId!,
                ResourceSubFolder = registryAttribute.ConstructorArguments[1].Value?.ToString() ?? null
            };

            switch (registerType)
            {
                case RegisterMethodType.Generic:
                {
                    var (constraints, typeConstraints) =
                        GenericHelper.GetGenericConstraint(methodSymbol.TypeParameters[0]);
                    method.Constraints = constraints;
                    method.GenericConstraintTypes = typeConstraints;
                    break;
                }

                case RegisterMethodType.Property:
                {
                    method.PropertyType = methodSymbol.Parameters[1].Type
                        .ToDisplayString(new SymbolDisplayFormat(SymbolDisplayGlobalNamespaceStyle.Included,
                            SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces));
                    break;
                }
            }

            registerMethodList.Add(method);
        }

        return registerMethodList;
    }
}

public struct RegisterMethod
{
    public RegisterMethod()
    {
    }

    public RegisterMethodType MethodType { get; set; } = 0;
    public string MethodName { get; set; } = "";
    public string ClassName { get; set; } = "";
    public string Namespace { get; set; } = "";

    public int RegistryPhase { get; set; } = -1;

    public bool HasFile { get; set; } = false;

    public string[] GenericConstraintTypes { get; set; } = Array.Empty<string>();
    public GenericConstraints Constraints { get; set; } = GenericConstraints.None;

    public string? PropertyType { get; set; } = null;

    public string CategoryId { get; set; } = "";
    public bool UseExistingId { get; set; } = false;
    public string? ResourceSubFolder { get; set; } = null;
}