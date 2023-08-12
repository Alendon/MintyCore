using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MintyCore.Generator.Registry;

public class RegistryData
{
    public readonly Dictionary<string, OldRegisterMethod> RegisterMethods = new();


    public bool GetRegisterMethod(AttributeData attribute, SyntaxNode node, out OldRegisterMethod registerMethod,
        out Diagnostic? diagnostic)
    {
        registerMethod = default;
        diagnostic = null;
        if (attribute.AttributeClass is not { } attributeClass) return false;

        if (attributeClass.Kind == SymbolKind.ErrorType)
        {
            if (!RegisterMethods.TryGetValue(attributeClass.Name.Replace("Attribute", ""), out registerMethod))
            {
                return false;
            }

            if (node is not MemberDeclarationSyntax member) return false;

            //find the matching attribute in member
            var attributeSyntax = member.AttributeLists.SelectMany(x => x.Attributes)
                .FirstOrDefault(x => x.Name is { } name && name.ToString() == attributeClass.Name);

            if (attributeSyntax is not
                {ArgumentList.Arguments : {Count: > 0} arguments}) return false;

            if (arguments[0].Expression is not LiteralExpressionSyntax idExpression) return false;

            registerMethod.Id = idExpression.Token.ValueText;

            if (registerMethod.HasFile)
            {
                if (arguments.Count < 2) return false;
                if (arguments[1].Expression is not LiteralExpressionSyntax fileExpression) return false;
                registerMethod.File = fileExpression.Token.ValueText;
            }

            if (registerMethod.UseExistingId)
            {
                if (arguments.Count < 2) return false;
                if (arguments[1].Expression is not LiteralExpressionSyntax fileExpression) return false;
                registerMethod.ModIdOverwrite = fileExpression.Token.ValueText;
            }

            return true;
        }

        registerMethod = new();

        var attributeMembers = attributeClass.GetMembers();

        foreach (var member in attributeMembers)
        {
            if (member is not IFieldSymbol field) continue;

            switch (member.Name)
            {
                case "ClassName":
                {
                    if (field.ConstantValue is not string className)
                    {
                        diagnostic = DiagnosticsHelper.InvalidRegisterAttribute(attributeClass, "ClassName");
                        return false;
                    }

                    registerMethod.ClassName = className;
                    break;
                }
                case "ResourceSubFolder":
                {
                    if (field.ConstantValue is string folder)
                    {
                        registerMethod.ResourceSubFolder = folder;
                    }

                    break;
                }
                case "MethodName":
                {
                    if (field.ConstantValue is not string methodName)
                    {
                        diagnostic = DiagnosticsHelper.InvalidRegisterAttribute(attributeClass, "MethodName");
                        return false;
                    }

                    registerMethod.MethodName = methodName;
                    break;
                }
                case "RegisterType":
                {
                    if (field.ConstantValue is not int methodType)
                    {
                        diagnostic = DiagnosticsHelper.InvalidRegisterAttribute(attributeClass, "RegisterType");
                        return false;
                    }

                    registerMethod.RegisterMethodType = (RegisterMethodType) methodType;
                    break;
                }
                case "HasFile":
                {
                    if (field.ConstantValue is not bool hasFile)
                    {
                        diagnostic = DiagnosticsHelper.InvalidRegisterAttribute(attributeClass, "HasFile");
                        return false;
                    }

                    registerMethod.HasFile = hasFile;
                    break;
                }
                case "UseExistingId":
                {
                    if (field.ConstantValue is not bool useExistingId)
                    {
                        diagnostic = DiagnosticsHelper.InvalidRegisterAttribute(attributeClass, "UseExistingId");
                        return false;
                    }

                    registerMethod.UseExistingId = useExistingId;
                    break;
                }
                case "GenericConstraints":
                {
                    if (field.ConstantValue is not int genericConstraints)
                    {
                        diagnostic = DiagnosticsHelper.InvalidRegisterAttribute(attributeClass, "GenericConstraints");
                        return false;
                    }

                    registerMethod.GenericConstraints = (GenericConstraints) genericConstraints;
                    break;
                }
                case "GenericTypeConstraints":
                {
                    if (field.ConstantValue is not string constraints)
                    {
                        diagnostic =
                            DiagnosticsHelper.InvalidRegisterAttribute(attributeClass, "GenericTypeConstraints");
                        return false;
                    }

                    registerMethod.GenericConstraintTypes = constraints.Split(',');
                    break;
                }
                case "RegistryPhase":
                {
                    if (field.ConstantValue is not int phase)
                    {
                        diagnostic = DiagnosticsHelper.InvalidRegisterAttribute(attributeClass, "RegistryPhase");
                        return false;
                    }

                    registerMethod.RegistryPhase = phase;
                    break;
                }
                case "PropertyType":
                {
                    if (field.ConstantValue is not string propertyType)
                    {
                        diagnostic = DiagnosticsHelper.InvalidRegisterAttribute(attributeClass, "PropertyType");
                        return false;
                    }

                    registerMethod.PropertyType = propertyType;
                    break;
                }
                case "CategoryId":
                {
                    if (field.ConstantValue is not string categoryId)
                    {
                        diagnostic = DiagnosticsHelper.InvalidRegisterAttribute(attributeClass, "CategoryId");
                        return false;
                    }

                    registerMethod.CategoryId = categoryId;
                    break;
                }
            }
        }


        var constructor = attribute.ConstructorArguments;

        if (constructor.Length < 1 ||
            constructor[0].Value is not string idValue) return false;
        registerMethod.Id = idValue;

        if (registerMethod.HasFile)
        {
            if (constructor.Length < 2 ||
                constructor[1].Value is not string fileValue) return false;
            registerMethod.File = fileValue;
        }

        // ReSharper disable once InvertIf
        if (registerMethod.UseExistingId)
        {
            if (constructor.Length < 3 ||
                constructor[2].Value is not string modId) return false;
            registerMethod.ModIdOverwrite = modId;
        }

        return true;
    }
}

public struct OldRegisterMethod
{
    public OldRegisterMethod()
    {
    }

    public RegisterMethodType RegisterMethodType = 0;
    public string MethodName = "";
    public string ClassName = "";
    public int RegistryPhase = -1;

    public bool HasFile = false;

    public string[]? GenericConstraintTypes = null;
    public GenericConstraints? GenericConstraints = null;

    public string? PropertyType = null;


    public string Id = "";
    public string File = "";

    public string PropertyToRegister = "";
    public string TypeToRegister = "";
    public string CategoryId = "";
    public bool UseExistingId = false;
    public string? ModIdOverwrite { get; set; } = null;
    public string? ResourceSubFolder { get; set; } = null;
}

public enum RegisterMethodType
{
    Invalid = 0,
    Property = 1,
    Generic = 2,
    File = 3
}