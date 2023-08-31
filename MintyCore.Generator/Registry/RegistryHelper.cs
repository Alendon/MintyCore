using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using JetBrains.Annotations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Newtonsoft.Json;

namespace MintyCore.Generator.Registry;

public static class RegistryHelper
{
    internal const string RegistryInterfaceName = "MintyCore.Modding.IRegistry";
    internal const string RegistryClassAttributeName = "MintyCore.Modding.Attributes.RegistryAttribute";
    internal const string RegistryMethodAttributeName = "MintyCore.Modding.Attributes.RegisterMethodAttribute";
    internal const string RegisterBaseAttributeName = "MintyCore.Modding.Attributes.RegisterBaseAttribute";
    internal const string RegisterMethodInfoBaseName = "MintyCore.Modding.Attributes.RegisterMethodInfo";

    internal const string ReferenceRegisterMethodName =
        "MintyCore.Modding.Attributes.ReferencedRegisterMethodAttribute";

    internal const string IdentificationName = "MintyCore.Utils.Identification";
    internal const string ModName = "MintyCore.Modding.IMod";

    public static ModInfo? GetModInfo(ISymbol? symbol)
    {
        if (symbol is not INamedTypeSymbol
            {
                AllInterfaces.Length: > 0, TypeKind: TypeKind.Class, IsAbstract: false
            } classSymbol)
            return null;

        if (!classSymbol.AllInterfaces.Any(x => ModName.Equals(x.ToDisplayString())))
            return null;

        return new ModInfo
            { Namespace = classSymbol.ContainingNamespace.ToDisplayString(), ClassName = classSymbol.Name };
    }

    public static IEnumerable<RegisterObject> ExtractFileRegisterObjects(
        ((ImmutableArray<AdditionalText> registryJsonFiles, Compilation compilation), ImmutableArray<RegisterMethodInfo>
            ) arg1,
        CancellationToken cancellationToken)
    {
        var registryJsonFiles = arg1.Item1.registryJsonFiles;
        var compilation = arg1.Item1.compilation;
        ImmutableArray<RegisterMethodInfo> newRegisterMethodInfos = arg1.Item2;

        List<JsonRegistry> registries = new();
        foreach (var file in registryJsonFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var text = file.GetText(cancellationToken);
            if (text is null)
                continue;

            var entries = JsonConvert.DeserializeObject<JsonRegistry[]>(text.ToString());
            if (entries is null)
                continue;

            registries.AddRange(entries);
        }

        Dictionary<string, RegisterMethodInfo> methodInfoLookup = new();
        List<RegisterObject> registerObjects = new();

        foreach (var registry in registries)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var methodInfo = GetOrUpdateRegisterMethodInfoCache(registry.RegisterMethodInfo, methodInfoLookup,
                newRegisterMethodInfos, compilation);

            registerObjects.AddRange(registry.Entries.Select(entry => new RegisterObject
                { File = entry.File, Id = entry.Id, RegisterMethodInfo = methodInfo }));
        }

        return registerObjects;
    }

    public static RegisterMethodInfo GetOrUpdateRegisterMethodInfoCache(
        string registerMethodInfoName,
        Dictionary<string, RegisterMethodInfo> methodInfoLookup,
        ImmutableArray<RegisterMethodInfo> newRegisterMethodInfos,
        Compilation compilation)
    {
        methodInfoLookup.TryGetValue(registerMethodInfoName, out var methodInfo);
        var found = methodInfo is not null;

        methodInfo ??= newRegisterMethodInfos.FirstOrDefault(x =>
            registerMethodInfoName.Equals($"{x.Namespace}.{x.ClassName}_{x.MethodName}"));

        //extract class name from register method info
        if (methodInfo is null)
        {
            var symbol = compilation.GetTypeByMetadataName(registerMethodInfoName) ??
                         throw new Exception($"Could not find register method info {registerMethodInfoName}");
            methodInfo = ExtractRegisterMethodInfoFromSymbol(symbol, CancellationToken.None);
        }

        if (!found)
            methodInfoLookup[registerMethodInfoName] = methodInfo;

        return methodInfo;
    }

    public static RegisterMethodInfo ExtractRegisterMethodInfoFromSymbol(INamedTypeSymbol methodInfoSymbol,
        CancellationToken token)
    {
        token.ThrowIfCancellationRequested();

        var methodInfo = new RegisterMethodInfo
        {
            Namespace = GetConstFieldValue<string>(methodInfoSymbol, nameof(RegisterMethodInfo.Namespace)),
            ClassName = GetConstFieldValue<string>(methodInfoSymbol, nameof(RegisterMethodInfo.ClassName)),
            MethodName = GetConstFieldValue<string>(methodInfoSymbol, nameof(RegisterMethodInfo.MethodName)),
            RegisterType =
                GetConstFieldValue<RegisterMethodType>(methodInfoSymbol, nameof(RegisterMethodInfo.RegisterType)),
            ResourceSubFolder =
                GetConstFieldValueNullable<string>(methodInfoSymbol, nameof(RegisterMethodInfo.ResourceSubFolder)),
            HasFile = GetConstFieldValue<bool>(methodInfoSymbol, nameof(RegisterMethodInfo.HasFile)),
            Constraints =
                GetConstFieldValue<GenericConstraints>(methodInfoSymbol, nameof(RegisterMethodInfo.Constraints)),
            GenericConstraintTypes =
                GetConstFieldValue<string>(methodInfoSymbol, nameof(RegisterMethodInfo.GenericConstraintTypes))
                    .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries),
            RegistryPhase = GetConstFieldValue<int>(methodInfoSymbol, nameof(RegisterMethodInfo.RegistryPhase)),
            PropertyType =
                GetConstFieldValueNullable<string>(methodInfoSymbol, nameof(RegisterMethodInfo.PropertyType)),
            CategoryId = GetConstFieldValue<string>(methodInfoSymbol, nameof(RegisterMethodInfo.CategoryId))
        };

        return methodInfo;
    }

    private static T GetConstFieldValue<T>(INamedTypeSymbol symbol, string fieldName)
    {
        var field = symbol.GetMembers().OfType<IFieldSymbol>()
            .FirstOrDefault(field => field.IsConst && field.Name == fieldName);
        if (field is null) throw new InvalidOperationException();
        return (T?)field.ConstantValue ?? throw new InvalidOperationException();
    }

    private static T? GetConstFieldValueNullable<T>(INamedTypeSymbol symbol, string fieldName)
    {
        var field = symbol.GetMembers().OfType<IFieldSymbol>()
            .FirstOrDefault(field => field.IsConst && field.Name == fieldName);
        if (field is null) throw new InvalidOperationException();
        return (T?)field.ConstantValue;
    }

    private struct JsonRegistry
    {
        [UsedImplicitly] public string RegisterMethodInfo { get; set; }
        [UsedImplicitly] public Entry[] Entries { get; set; }

        public struct Entry
        {
            [UsedImplicitly] public string Id { get; set; }
            [UsedImplicitly] public string File { get; set; }
        }
    }

    public static RegisterObject? ExtractPropertyRegistryCall(
        (IPropertySymbol Left, ImmutableArray<RegisterMethodInfo> Right) arg1, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var propertySymbol = arg1.Left;
        var newRegisterMethodInfos = arg1.Right;

        var registerAttribute = propertySymbol.GetAttributes().FirstOrDefault(x =>
            RegisterBaseAttributeName.Equals(x.AttributeClass?.BaseType?.ToDisplayString()));

        var errorAttribute = propertySymbol.GetAttributes().FirstOrDefault(x =>
            x.AttributeClass?.Kind == SymbolKind.ErrorType);


        //keep it simple for now. If a precompiled registry attribute is found check for this,
        //otherwise check for the optional error attribute.
        //Currently no checks if multiple error attributes are present

        RegisterMethodInfo? registerMethodInfo = null;
        string? id = null;
        string? file = null;

        if (registerAttribute is not null)
        {
            ExtractRegisterInfoFromAttribute(registerAttribute, cancellationToken,
                out registerMethodInfo, out id,
                out file);
        }

        if (registerMethodInfo is null && errorAttribute is not null)
        {
            ExtractRegisterInfoFromErrorAttribute(
                errorAttribute, newRegisterMethodInfos,
                out registerMethodInfo, out id, out file);
        }

        if (registerMethodInfo is null || id is null)
            return null;

        var propertyType = propertySymbol.Type.ToDisplayString();
        if (registerMethodInfo.PropertyType is not null && !registerMethodInfo.PropertyType.Equals(propertyType))
            return null;

        return new RegisterObject
        {
            RegisterMethodInfo = registerMethodInfo,
            Id = id,
            RegisterProperty = propertySymbol.ToDisplayString(),
            File = file
        };
    }

    public static RegisterObject? ExtractGenericRegistryCall(
        (INamedTypeSymbol Left, ImmutableArray<RegisterMethodInfo> Right) arg1, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var typeSymbol = arg1.Left;
        var newRegisterMethodInfos = arg1.Right;

        var registerAttribute = typeSymbol.GetAttributes().FirstOrDefault(x =>
            RegisterBaseAttributeName.Equals(x.AttributeClass?.BaseType?.ToDisplayString()));

        var errorAttribute = typeSymbol.GetAttributes().FirstOrDefault(x =>
            x.AttributeClass?.Kind == SymbolKind.ErrorType);


        //keep it simple for now. If a precompiled registry attribute is found check for this,
        //otherwise check for the optional error attribute.
        //Currently no checks if multiple error attributes are present

        RegisterMethodInfo? registerMethodInfo = null;
        string? id = null;
        string? file = null;
        if (registerAttribute is not null)
        {
            ExtractRegisterInfoFromAttribute(registerAttribute, cancellationToken,
                out registerMethodInfo, out id,
                out file);
        }

        if (registerMethodInfo is null && errorAttribute is not null)
        {
            ExtractRegisterInfoFromErrorAttribute(
                errorAttribute, newRegisterMethodInfos,
                out registerMethodInfo, out id, out file);
        }

        if (registerMethodInfo is null || id is null)
            return null;

        if (!CheckValidConstraint(registerMethodInfo.Constraints,
                registerMethodInfo.GenericConstraintTypes, typeSymbol))
            return null;


        return new RegisterObject
        {
            RegisterMethodInfo = registerMethodInfo,
            Id = id,
            RegisterType = typeSymbol.ToDisplayString(),
            File = file
        };
    }

    private static void ExtractRegisterInfoFromErrorAttribute(AttributeData errorAttribute,
        ImmutableArray<RegisterMethodInfo> newRegisterMethodInfos, out RegisterMethodInfo? registerMethodInfo,
        out string? id, out string? file)
    {
        registerMethodInfo = null;
        id = null;
        file = null;

        var syntax = errorAttribute.ApplicationSyntaxReference?.GetSyntax() as AttributeSyntax;
        if (syntax?.ArgumentList is not { } argumentList) return;

        if (!ExtractErrorAttributeConstructor(argumentList.Arguments, out id, out file)) return;

        if (errorAttribute.AttributeClass is not { Kind: SymbolKind.ErrorType } attributeClass) return;

        var attributeClassName = attributeClass.Name;
        //remove optional Attribute suffix
        if (attributeClassName.EndsWith("Attribute"))
            attributeClassName = attributeClassName.Substring(0, attributeClassName.Length - "Attribute".Length);

        registerMethodInfo = newRegisterMethodInfos.FirstOrDefault(x => x.MethodName.Equals(attributeClassName));
    }

    private static void ExtractRegisterInfoFromAttribute(AttributeData registerAttribute,
        CancellationToken cancellationToken,
        out RegisterMethodInfo? registerMethodInfo, out string? id, out string? file)
    {
        cancellationToken.ThrowIfCancellationRequested();

        registerMethodInfo = null;
        id = null;
        file = null;

        if (!ExtractAttributeConstructor(registerAttribute.ConstructorArguments, out id, out file)) return;

        if (registerAttribute.AttributeClass is not { } attributeClass) return;

        var registerInfoAttribute = attributeClass.GetAttributes().FirstOrDefault(x =>
            ReferenceRegisterMethodName.Equals(x.AttributeClass?.ToDisplayString(
                new SymbolDisplayFormat(SymbolDisplayGlobalNamespaceStyle.Omitted,
                    SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces))));

        if (registerInfoAttribute?.AttributeClass is not { TypeArguments.Length: 1 } registerInfoClass) return;

        if (registerInfoClass.TypeArguments[0] is not INamedTypeSymbol registerInfoType) return;

        registerMethodInfo = ExtractRegisterMethodInfoFromSymbol(registerInfoType, cancellationToken);
    }

    private static bool ExtractErrorAttributeConstructor(
        SeparatedSyntaxList<AttributeArgumentSyntax> registerAttributeConstructorArguments,
        out string? id, out string? file)
    {
        id = null;
        file = null;

        if (registerAttributeConstructorArguments.Count is 0 or > 2) return false;

        if (registerAttributeConstructorArguments[0].Expression is not LiteralExpressionSyntax idValue) return false;
        id = idValue.Token.ValueText;

        if (registerAttributeConstructorArguments.Count != 2) return true;

        if (registerAttributeConstructorArguments[1].Expression is not LiteralExpressionSyntax fileValue) return false;
        file = fileValue.Token.ValueText;

        return true;
    }

    private static bool ExtractAttributeConstructor(ImmutableArray<TypedConstant> registerAttributeConstructorArguments,
        out string? id, out string? file)
    {
        id = null;
        file = null;

        if (registerAttributeConstructorArguments.Length is 0 or > 2) return false;

        if (registerAttributeConstructorArguments[0].Value is not string idValue) return false;
        id = idValue;

        if (registerAttributeConstructorArguments.Length != 2) return true;

        if (registerAttributeConstructorArguments[1].Value is not string fileValue) return false;
        file = fileValue;

        return true;
    }


    public static IEnumerable<RegisterMethodInfo> ExtractRegisterMethodsFromRegistryClass(
        (INamedTypeSymbol registryClass, AttributeData registryAttribute) arg1, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var registryClass = arg1.registryClass;
        var registryAttribute = arg1.registryAttribute;

        var baseRegisterMethodInfo = new RegisterMethodInfo()
        {
            Namespace = registryClass.ContainingNamespace.ToDisplayString(),
            ClassName = registryClass.Name,
            CategoryId = (string)registryAttribute.ConstructorArguments[0].Value!,
            ResourceSubFolder = registryAttribute.ConstructorArguments[1].Value as string,
        };

        var registerMethods = new List<RegisterMethodInfo>();

        var methodMembers = registryClass.GetMembers().OfType<IMethodSymbol>().ToArray();

        foreach (var methodSymbol in methodMembers)
        {
            var registryMethodAttribute = methodSymbol.GetAttributes().FirstOrDefault(
                x => RegistryMethodAttributeName.Equals(x.AttributeClass?.ToDisplayString()));
            if (registryMethodAttribute is null) continue;

            var parameters = methodSymbol.Parameters;
            var typeParameters = methodSymbol.TypeParameters;
            if (parameters.Length is 0 or > 2 || typeParameters.Length > 1) continue;

            if (!IdentificationName.Equals(parameters[0].Type.ToDisplayString())) continue;

            if (registryMethodAttribute.ConstructorArguments[0].Value is not int phaseValue) continue;

            if (registryMethodAttribute.ConstructorArguments[1].Value is not int registryOptions) continue;
            var hasFile = (registryOptions & (int)RegisterMethodOptions.HasFile) != 0;

            var isProperty = parameters.Length == 2;
            var isGeneric = typeParameters.Length == 1;

            var registerType = (hasFile, isProperty, isGeneric) switch
            {
                (true, false, false) => RegisterMethodType.File,
                (_, true, false) => RegisterMethodType.Property,
                (_, false, true) => RegisterMethodType.Generic,
                _ => RegisterMethodType.Invalid
            };
            if (registerType == RegisterMethodType.Invalid) continue;

            registerMethods.Add(baseRegisterMethodInfo with
            {
                MethodName = methodSymbol.Name,
                RegisterType = registerType,
                RegistryPhase = phaseValue,
                HasFile = hasFile,
                Constraints = registerType == RegisterMethodType.Generic
                    ? GetGenericConstraints(methodSymbol.TypeParameters[0])
                    : GenericConstraints.None,
                GenericConstraintTypes = registerType == RegisterMethodType.Generic
                    ? GetGenericConstraintTypes(methodSymbol.TypeParameters[0])
                    : Array.Empty<string>(),
                PropertyType =
                registerType == RegisterMethodType.Property ? parameters[1].Type.ToDisplayString() : null,
            });
        }

        return registerMethods;
    }

    public static GenericConstraints GetGenericConstraints(ITypeParameterSymbol symbol)
    {
        var constraints = GenericConstraints.None;
        constraints |= symbol.HasReferenceTypeConstraint
            ? GenericConstraints.ReferenceType
            : GenericConstraints.None;
        constraints |= symbol.HasValueTypeConstraint ? GenericConstraints.ValueType : GenericConstraints.None;
        constraints |= symbol.HasConstructorConstraint ? GenericConstraints.Constructor : GenericConstraints.None;
        constraints |= symbol.HasUnmanagedTypeConstraint
            ? GenericConstraints.UnmanagedType
            : GenericConstraints.None;
        constraints |= symbol.HasNotNullConstraint ? GenericConstraints.NotNull : GenericConstraints.None;

        return constraints;
    }

    public static string[] GetGenericConstraintTypes(ITypeParameterSymbol symbol)
    {
        return symbol.ConstraintTypes.Select(type => type.ToDisplayString(new SymbolDisplayFormat(
            SymbolDisplayGlobalNamespaceStyle.Omitted,
            SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
            SymbolDisplayGenericsOptions.IncludeTypeParameters
        ))).ToArray();
    }

    public static bool CheckValidConstraint(GenericConstraints? genericConstraints, string[]? genericConstraintTypes,
        INamedTypeSymbol namedTypeSymbol)
    {
        if (genericConstraints is null || genericConstraintTypes is null)
        {
            return true;
        }

        var constraints = genericConstraints.Value;
        var constraintTypes = genericConstraintTypes;

        if (constraints.HasFlag(GenericConstraints.ReferenceType) && !namedTypeSymbol.IsReferenceType)
        {
            return false;
        }

        if (constraints.HasFlag(GenericConstraints.ValueType) && !namedTypeSymbol.IsValueType)
        {
            return false;
        }

        if (constraints.HasFlag(GenericConstraints.UnmanagedType) && !namedTypeSymbol.IsUnmanagedType)
        {
            return false;
        }

        //check if al generic constraint types are present
        // ReSharper disable once InvertIf
        if (constraintTypes.Length > 0)
        {
            var baseTypesEnum = GetBaseTypes(namedTypeSymbol);
            var namedTypeSymbols = baseTypesEnum as INamedTypeSymbol[] ?? baseTypesEnum.ToArray();
            var interfaces = namedTypeSymbol.AllInterfaces;

            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var constraintType in constraintTypes)
            {
                if (string.IsNullOrEmpty(constraintType)) continue;

                var interfaceFound = interfaces.Any(@interface => @interface.ToString().Equals(constraintType));
                var baseFound = Array.Exists(namedTypeSymbols, type => type.ToString().Equals(constraintType));

                var found = interfaceFound || baseFound;
                if (!found)
                    return false;
            }
        }

        return true;
    }

    private static IEnumerable<INamedTypeSymbol> GetBaseTypes(ITypeSymbol symbol)
    {
        var current = symbol.BaseType;
        while (current != null)
        {
            yield return current;
            current = current.BaseType;
        }
    }
}