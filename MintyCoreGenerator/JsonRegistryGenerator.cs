using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Newtonsoft.Json;

namespace MintyCoreGenerator;

//[Generator]
public class JsonRegistryGenerator : ISourceGenerator
{
    private const string JsonFileName = "GenerateRegistryData.json";

    public void Initialize(GeneratorInitializationContext context)
    {
    }

    public void Execute(GeneratorExecutionContext context)
    {
        var jsonFile = context.AdditionalFiles.FirstOrDefault(file => file.Path.EndsWith(JsonFileName));
        if (jsonFile is null)
        {
            return;
        }

        var fileText = jsonFile.GetText();
        if (fileText is null) return;

        var reader = new JsonTextReader(new StringReader(fileText.ToString()));

        var serializer = JsonSerializer.Create();

        var data = serializer.Deserialize<RegistryDataSheet>(reader);

        if (data is null) return;

        foreach (var registry in data.Registries)
        {
            var classText = GenerateIdClass(registry, data.ModId);
            context.AddSource($"{registry.FullIdClassName}.g.cs", classText);
        }
    }

    private static string GenerateIdClass(RegistryDescription registryDescription, string modId)
    {
        var sb = new StringBuilder();
        var namespaceSplitIndex = registryDescription.FullIdClassName.LastIndexOf('.');
        var @namespace = registryDescription.FullIdClassName.Substring(0, namespaceSplitIndex);
        var @class = registryDescription.FullIdClassName.Substring(namespaceSplitIndex + 1);

        WriteUsingsAndNameSpace(sb, @namespace);
        WriteClassHead(sb, @class);

        foreach (var value in registryDescription.Values)
        {
            WriteValueField(sb, value);
        }

        WriteRegisterAllHead(sb, modId, registryDescription.CategoryId);
        foreach (var value in registryDescription.Values)
        {
            WriteRegisterValue(sb, registryDescription.FullRegisterMethodName, value, registryDescription.Parameters,
                registryDescription.ModIdParameterName, registryDescription.CategoryIdParameterName, registryDescription.ObjectIdParameterName);
        }

        WriteRegisterAllFood(sb);
        WriteClassFood(sb);

        return sb.ToString();
    }

    private static void WriteClassFood(StringBuilder sb)
    {
        sb.Append("}");
    }

    private static void WriteRegisterAllFood(StringBuilder sb)
    {
        sb.AppendLine("    }");
    }

    private static void WriteUsingsAndNameSpace(StringBuilder sb, string @namespace)
    {
        sb.AppendLine(
            $@"using RegistryManager = MintyCore.Modding.RegistryManager;
using Identification = MintyCore.Utils.Identification;
using Logger = MintyCore.Utils.Logger;

namespace {@namespace};
");
    }

    private static void WriteClassHead(StringBuilder sb, string @class)
    {
        sb.AppendLine($@"
public partial class {@class}
{{
");
    }

    private static void WriteValueField(StringBuilder sb, Value value)
    {
        var comments = value.Comment.Split('\n');
        sb.AppendLine(@"    /// <summary>");

        foreach (var comment in comments)
        {
            if (!string.IsNullOrEmpty(comment))
                sb.AppendLine($@"    /// {comment}");
        }

        sb.AppendLine($@"    ///     <remarks>Source generated property</remarks>
    /// </summary>
    public static Identification {value.FieldName} {{ get; internal set; }}");
    }

    private static void WriteRegisterAllHead(StringBuilder sb, string modId, string registryCategoryId)
    {
        sb.AppendLine($@"
    internal static void RegisterAll()
    {{
        Logger.AssertAndThrow(RegistryManager.TryGetModId(""{modId}"", out var modId), ""Failed to get numeric mod id for {modId}"", ""Generated/{modId}/Registry"");
        Logger.AssertAndThrow(RegistryManager.TryGetCategoryId(""{registryCategoryId}"", out var categoryId), ""Failed to get numeric category id for {registryCategoryId}"", ""Generated/{modId}/Registry"");
        ");
    }

    private static void WriteRegisterValue(StringBuilder sb, string fullRegisterMethodName, Value value,
        Parameter[] registryParameters, string modIdParameterName, string categoryIdParameterName,
        string objectIdParameterName)
    {
        sb.Append($"        {value.FieldName} = {fullRegisterMethodName}(");
        for (int i = 0; i < registryParameters.Length; i++)
        {
            var parameter = registryParameters[i];
            string parameterValue;

            if (parameter.Name.Equals(modIdParameterName))
            {
                parameterValue = "modId";
            }
            else if (parameter.Name.Equals(categoryIdParameterName))
            {
                parameterValue = "categoryId";
            }
            else if (parameter.Name.Equals(objectIdParameterName))
            {
                parameterValue = $"\"{value.Id}\"";
            }
            else if (value.ParameterValues.TryGetValue(parameter.Name, out parameterValue))
            {
                if (parameter.IsStringParameter)
                {
                    parameterValue = $"\"{parameterValue}\"";
                }
            }
            else
            {
                if (string.IsNullOrEmpty(parameter.DefaultValue))
                    continue;
                parameterValue = parameter.DefaultValue;
            }

            sb.Append(i != 0 ? $", {parameterValue}" : $"{parameterValue}");
        }

        sb.AppendLine(");");
    }
}

public class RegistryDataSheet
{
    public string ModId { get; set; } = String.Empty;

    public RegistryDescription[] Registries { get; set; } = Array.Empty<RegistryDescription>();
}

public class RegistryDescription
{
    //Name of the Register method
    public string FullRegisterMethodName { get; set; } = String.Empty;

    //Name of the class containing the generated ids. Name including namespace
    public string FullIdClassName { get; set; } = String.Empty;

    //The category string id of the registry. Only needed if the CategoryId parameter of type ushort is provided
    public string CategoryId { get; set; } = String.Empty;

    public string ModIdParameterName { get; set; } = "modId";
    public string CategoryIdParameterName { get; set; } = "categoryId";
    public string ObjectIdParameterName { get; set; } = "objectId";


    //Array of parameters used; Only string representable values are allows (numbers, bool, string...)
    public Parameter[] Parameters { get; set; } = Array.Empty<Parameter>();

    //Array of the values to register
    public Value[] Values { get; set; } = Array.Empty<Value>();
}

public class Parameter
{
    //Name of the parameter, will get mapped to Value.ParameterValues.Key
    public string Name { get; set; } = String.Empty;

    //Default Value of the parameter if non provided
    public string DefaultValue { get; set; } = String.Empty;

    public bool IsStringParameter { get; set; }
}

public class Value
{
    //Name of the field to create in id class container
    public string FieldName { get; set; } = String.Empty;

    //String identifier for the value
    public string Id { get; set; } = String.Empty;

    //Map of parameter values
    public Dictionary<string, string> ParameterValues { get; set; } = new Dictionary<string, string>();

    public string Comment { get; set; } = String.Empty;
}