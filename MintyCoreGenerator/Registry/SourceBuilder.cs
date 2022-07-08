using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace MintyCoreGenerator.Registry;

public static class SourceBuilder
{
    public static string ComposeRegistryAttribute(INamedTypeSymbol registryClass,
        List<RegisterMethod> registerMethodList)
    {
        StringBuilder sb = new();


        sb.Append($@"
using System;
using JetBrains.Annotations;

#pragma warning disable CS1591
#nullable enable

namespace {registryClass.ContainingNamespace};");

        foreach (var registerMethod in registerMethodList)
        {
            if (registerMethod.RegisterMethodType != RegisterMethodType.Generic &&
                registerMethod.RegisterMethodType != RegisterMethodType.Property) continue;

            var attributeTarget = registerMethod.RegisterMethodType switch
            {
                RegisterMethodType.Generic => "AttributeTargets.Class | AttributeTargets.Struct",
                RegisterMethodType.Property => "AttributeTargets.Property",
                _ => "Invalid"
            };

            sb.Append($@"
[AttributeUsage({attributeTarget}, AllowMultiple = false)]
[MeansImplicitUse]
public class {registerMethod.MethodName}Attribute : MintyCore.Modding.Attributes.RegisterBaseAttribute
{{
    public {registerMethod.MethodName}Attribute(string id{(registerMethod.HasFile ? ", string file" : "")}{(registerMethod.UseExistingId ? ", string modId" : "")}) 
    {{
    }}

    public const string ClassName = ""{registerMethod.ClassName}"";
    public const string MethodName = ""{registerMethod.MethodName}"";
    public const int RegisterType = {((int) registerMethod.RegisterMethodType).ToString()};
    public const string? ResourceSubFolder = ""{registerMethod.ResourceSubFolder ?? "null"}"";
    public const bool HasFile = {registerMethod.HasFile.ToString().ToLower()};
    public const bool UseExistingId = {registerMethod.UseExistingId.ToString().ToLower()};
    public const int GenericConstraints = {(registerMethod.GenericConstraints.HasValue ? ((int) registerMethod.GenericConstraints.Value).ToString() : "0")};
    public const string GenericTypeConstraints = ""{(registerMethod.GenericConstraintTypes is not null ? string.Join(",", registerMethod.GenericConstraintTypes) : "")}"";
    public const int RegistryPhase = {registerMethod.RegistryPhase};
    public const string PropertyType = ""{(registerMethod.PropertyType ?? "")}"";
    public const string CategoryId = ""{registerMethod.CategoryId}"";
}}
");
        }

        return Normalize(sb.ToString());
    }

    public static string ComposeRegistryMethodAndClassExtension(string registryClass, int registryPhase,
        List<RegisterMethod> registerMethodInfos, string @namespace, string modFullName,
        out string eventSubscribeExpression,
        out string eventUnsubscribeExpression)
    {
        StringBuilder sb = new();

        if (registerMethodInfos.Count == 0)
        {
            eventSubscribeExpression = "";
            eventUnsubscribeExpression = "";
            return "";
        }

        var className = registerMethodInfos[0].CategoryId;
        //convert from underscore delimited to camel case
        var classNameCamelCase =
            string.Join("", className.Split('_').Select(x => x.Substring(0, 1).ToUpper() + x.Substring(1)));

        var stringPhase = registryPhase switch
        {
            1 => "Pre",
            2 => "Main",
            3 => "Post",
            _ => "Invalid"
        };

        sb.Append($@"

namespace {@namespace};

#pragma warning disable CS1591

public static partial class {classNameCamelCase}IDs
{{
    internal static void {stringPhase}Register()
    {{
        if(!MintyCore.Modding.RegistryManager.TryGetCategoryId(""{registerMethodInfos[0].CategoryId}"", out var categoryId))
        {{
            throw new System.Exception();
        }}
");
        List<string> ids = new();

        foreach (var methodInfo in registerMethodInfos)
        {
            var idCamelCase = string.Join("",
                methodInfo.Id.Split('_').Select(x => x.Substring(0, 1).ToUpper() + x.Substring(1)));
            if (!methodInfo.UseExistingId)
                ids.Add(idCamelCase);

            sb.AppendLine("{");

            sb.AppendLine(methodInfo.ModIdOverwrite is null
                ? $"var modId = {modFullName}.Instance.ModId;"
                : $"MintyCore.Utils.Logger.AssertAndThrow(MintyCore.Modding.RegistryManager.TryGetModId(\"{methodInfo.ModIdOverwrite}\", out var modId), \"ModId {methodInfo.ModIdOverwrite} not found\", \"Registry\");");

            sb.AppendLine(methodInfo.UseExistingId
                ? $"MintyCore.Utils.Logger.AssertAndThrow(MintyCore.Modding.RegistryManager.TryGetObjectId(modId, categoryId, \"{methodInfo.Id}\", out var id), \"ObjectId {methodInfo.Id} not found\", \"Registry\");"
                : $"var id = MintyCore.Modding.RegistryManager.RegisterObjectId(modId, categoryId, \"{methodInfo.Id}\"{(methodInfo.HasFile ? $", \"{methodInfo.File}\"" : "")});");

            if (!methodInfo.UseExistingId)
            {
                sb.AppendLine($"{idCamelCase} = id;");
            }

            sb.Append($@"
        {registryClass}.{methodInfo.MethodName}");
            if (methodInfo.RegisterMethodType == RegisterMethodType.Generic)
            {
                sb.Append($"<{methodInfo.TypeToRegister}>");
            }

            sb.Append($"(id");

            if (methodInfo.RegisterMethodType == RegisterMethodType.Property)
            {
                sb.Append($", {methodInfo.PropertyToRegister}");
            }

            sb.AppendLine(");\n}");
        }

        sb.AppendLine("    }");

        foreach (var id in ids)
        {
            sb.AppendLine($"public static MintyCore.Utils.Identification {id} {{get; private set;}}");
        }


        sb.Append("}");


        string registerEvent = registerMethodInfos[0].RegistryPhase switch
        {
            1 => "OnPreRegister",
            2 => "OnRegister",
            3 => "OnPostRegister",
            _ => "Invalid"
        };

        eventSubscribeExpression =
            $"{registryClass}.{registerEvent} += {@namespace}.{classNameCamelCase}IDs.{stringPhase}Register;";
        eventUnsubscribeExpression =
            $"{registryClass}.{registerEvent} -= {@namespace}.{classNameCamelCase}IDs.{stringPhase}Register;";

        return Normalize(sb.ToString());
    }

    public static string ComposeRegistryRegisterMethod(RegistryData data, string @namespace, string modFullName,
        out string registerMethod)
    {
        StringBuilder sb = new();

        var registerMethods = data.RegisterMethods.Values.ToArray();

        var ids = registerMethods.Select(x => x.CategoryId).ToArray();

        //convert ids from underscore delimited to camel case
        var idsCamelCase = ids.Select(x =>
            string.Join("", x.Split('_').Select(y => y.Substring(0, 1).ToUpper() + y.Substring(1)))).ToArray();

        var idFields = idsCamelCase.Select(id => $"public static ushort {id} {{get; private set;}}").ToArray();

        var registryClassNames = registerMethods.Select(x => x.ClassName).ToArray();

        var resourceFolders = registerMethods.Select(x => x.ResourceSubFolder).ToArray();

        //combine ids, idFields and registryClassNames into a tuple (id, idField, registryClassName)
        var combined = Combine(ids, idsCamelCase, registryClassNames, resourceFolders);
        var registerCalls = combined.Select(x =>
            $"{x.idField} = MintyCore.Modding.RegistryManager.AddRegistry<{x.className}>(modId, \"{x.id}\", {(x.resourceFolder is not null ? $"\"{x.resourceFolder}\"" : "null")});");

        sb.AppendLine($@"
using System;
#pragma warning disable CS1591
#nullable enable

namespace {@namespace};

public static partial class RegistryIDs
{{
    {string.Join("\n", idFields.Distinct())}

    internal static void Register()
    {{
        var modId = {modFullName}.Instance!.ModId;
        {string.Join("\n", registerCalls)}
    }}

}}

");
        registerMethod = $"{@namespace}.RegistryIDs.Register();";
        return Normalize(sb.ToString());

        static IEnumerable<(string id, string idField, string className, string? resourceFolder)> Combine(string[] ids,
            string[] idFields, string[] classNames, string?[] resourceFolders)
        {
            HashSet<string> processedIDs = new();
            for (var i = 0; i < ids.Length; i++)
            {
                if (processedIDs.Contains(ids[i]))
                    continue;
                processedIDs.Add(ids[i]);
                yield return (ids[i], idFields[i], classNames[i], resourceFolders[i]);
            }
        }
    }

    public static string ComposeRegisterMethod(INamedTypeSymbol modSymbol,
        List<string> registryEventSubscribeExpressions, List<string> registryEventUnsubscribeExpressions,
        string registerMethodToCall)
    {
        StringBuilder sb = new();

        sb.AppendLine($@"
using System;
#pragma warning disable CS1591

namespace {modSymbol.ContainingNamespace};

{modSymbol.DeclaredAccessibility.ToCSharpString()} partial class {modSymbol.Name}
{{

    internal static void InternalRegister()
    {{
        {registerMethodToCall}
        {string.Join("\n", registryEventSubscribeExpressions)}
    }}

    internal static void InternalUnregister()
    {{
        {string.Join("\n", registryEventUnsubscribeExpressions)}
    }}

}}
");


        return Normalize(sb.ToString());
    }

    private static string Normalize(string code)
    {
        return SyntaxFactory.ParseCompilationUnit(code).NormalizeWhitespace().ToFullString();
    }
}