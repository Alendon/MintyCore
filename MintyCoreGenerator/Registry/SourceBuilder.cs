using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace MintyCoreGenerator.Registry;

public static class SourceBuilder
{
    public static string ComposeRegistryAttribute(INamedTypeSymbol registryClass,
        List<RegisterMethod> registerMethodList)
    {
        StringBuilder sb = new();


        sb.Append($@"
using System;

#pragma warning disable CS1591

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
public class {registerMethod.MethodName}Attribute : MintyCore.Modding.Attributes.RegisterBaseAttribute
{{
    public {registerMethod.MethodName}Attribute(string id{(registerMethod.HasFile ? ", string file" : "")})
    {{
    }}

    public const string ClassName = ""{registerMethod.ClassName}"";
    public const string MethodName = ""{registerMethod.MethodName}"";
    public const int RegisterType = {((int) registerMethod.RegisterMethodType).ToString()};
    public const bool HasFile = {registerMethod.HasFile.ToString().ToLower()};
    public const int GenericConstraints = {(registerMethod.GenericConstraints.HasValue ? ((int) registerMethod.GenericConstraints.Value).ToString() : "0")};
    public const string GenericTypeConstraints = ""{(registerMethod.GenericConstraintTypes is not null ? string.Join(",", registerMethod.GenericConstraintTypes) : "")}"";
    public const int RegistryPhase = {registerMethod.RegistryPhase};
    public const string PropertyType = ""{(registerMethod.PropertyType ?? "")}"";
    public const string CategoryId = ""{registerMethod.CategoryId}"";
}}
");
        }

        return sb.ToString();
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

public static partial class {classNameCamelCase}Ids
{{
    internal static void {stringPhase}Register()
    {{
        var modId = {modFullName}.Instance.ModId;
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
            ids.Add(idCamelCase);

            sb.Append($@"
        {idCamelCase} = MintyCore.Modding.RegistryManager.RegisterObjectId(modId, categoryId, ""{methodInfo.Id}""{(methodInfo.HasFile ? $", \"{methodInfo.File}\"" : "")});
        {registryClass}.{methodInfo.MethodName}");
            if (methodInfo.RegisterMethodType == RegisterMethodType.Generic)
            {
                sb.Append($"<{methodInfo.TypeToRegister}>");
            }

            sb.Append($"({idCamelCase}");

            if (methodInfo.RegisterMethodType == RegisterMethodType.Property)
            {
                sb.Append($", {methodInfo.PropertyToRegister}");
            }

            sb.AppendLine(");");
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
            $"{registryClass}.{registerEvent} += {@namespace}.{classNameCamelCase}Ids.{stringPhase}Register;";
        eventUnsubscribeExpression =
            $"{registryClass}.{registerEvent} -= {@namespace}.{classNameCamelCase}Ids.{stringPhase}Register;";

        return sb.ToString();
    }

    public static string ComposeRegistryRegisterMethod(RegistryData data, string @namespace, string modFullName,
        out string registerMethod)
    {
        StringBuilder sb = new();

        var registerMethods = data.RegisterMethods.Values.ToArray();

        var ids = registerMethods.Select(x => x.CategoryId).ToArray();

        //convert ids from underscore delimited to camel case
        var idsCamelCase = ids.Select(x => string.Join("", x.Split('_').Select(y => y.Substring(0, 1).ToUpper() + y.Substring(1)))).ToArray();

        var idFields = idsCamelCase.Select(id => $"public static ushort {id} {{get; private set;}}").ToArray();

        var registryClassNames = registerMethods.Select(x => x.ClassName).ToArray();

        //combine ids, idFields and registryClassNames into a tuple (id, idField, registryClassName)
        var combined = Combine(ids, idsCamelCase, registryClassNames);
        var registerCalls = combined.Select(x =>
            $"{x.idField} = MintyCore.Modding.RegistryManager.AddRegistry<{x.className}>(modId, \"{x.id}\");");

        sb.AppendLine($@"
using System;
#pragma warning disable CS1591

namespace {@namespace};

public static partial class RegistryIds
{{
    {string.Join("\n", idFields)}

    internal static void Register()
    {{
        var modId = {modFullName}.Instance.ModId;
        {string.Join("\n", registerCalls)}
    }}

}}

");
        registerMethod = $"{@namespace}.RegistryIds.Register();";
        return sb.ToString();

        static IEnumerable<(string id, string idField, string className)> Combine(string[] ids,
            string[] idFields, string[] classNames)
        {
            for (var i = 0; i < ids.Length; i++)
            {
                yield return (ids[i], idFields[i], classNames[i]);
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


        return sb.ToString();
    }
}