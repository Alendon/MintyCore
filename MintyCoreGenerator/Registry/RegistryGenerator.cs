using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static MintyCoreGenerator.Registry.SourceBuilder;
using static MintyCoreGenerator.Registry.DiagnosticsHelper;

namespace MintyCoreGenerator.Registry;

[Generator]
public class RegistryGenerator : IIncrementalGenerator
{
    private const string RegistryInterfaceName = "MintyCore.Modding.IRegistry";
    private const string RegistryClassAttributeName = "MintyCore.Modding.Attributes.RegistryAttribute";
    private const string RegistryMethodAttributeName = "MintyCore.Modding.Attributes.RegisterMethodAttribute";
    private const string IdentificationName = "MintyCore.Utils.Identification";

    private RegistryData registryData = new();

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        //Register Output for Attribute creation
        context.RegisterSourceOutput(context.SyntaxProvider.CreateSyntaxProvider(
                //Check if the class declaration has at least one attribute and one base type (interface or class)
                (node, _) => node is ClassDeclarationSyntax {AttributeLists.Count: > 0, BaseList.Types.Count: > 0},
                (syntaxContext, _) => IsValidRegistryClass(syntaxContext)).Where(info => info is not null),
            GenerateRegistryAttributes
        );

        //Register Output for Property registry creation

        //Register Output for Class registry creation

        //Register fake Output for File registry creation
        //Just return true for the first node and false for the rest
    }

    private void GenerateRegistryAttributes(SourceProductionContext productionContext,
        (INamedTypeSymbol, AttributeData)? info)
    {
        if (info is null) return;
        var (registryClass, registryClassAttribute) = info.Value;

        List<(IMethodSymbol methodSymbol, RegisterMethodType registerType, int registryPhase, bool hasFile)> registerMethods = new();

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
                productionContext.ReportDiagnostic(InvalidRegisterMethod(methodSymbol.Locations.FirstOrDefault(),
                    methodSymbol.ToString()));
                continue;
            }
            
            var firstParameter = methodSymbol.Parameters[0];
            if (!firstParameter.ToString().Equals(IdentificationName))
            {
                productionContext.ReportDiagnostic(InvalidRegisterMethod(methodSymbol.Locations.FirstOrDefault(),
                    methodSymbol.ToString()));
                continue;
            }

            var registryPhaseValue = methodAttribute.ConstructorArguments[0].Value;
            var registryPhase = (int?) registryPhaseValue ?? 0;

            var hasFileValue = methodAttribute.ConstructorArguments[1].Value;
            var hasFile = (bool?) hasFileValue ?? false;

            //Get the register method type
            var parameterCount = methodSymbol.Parameters.Length;
            var genericTypeCount = methodSymbol.TypeParameters.Length;

            var registerMethodType = (parameterCount, genericTypeCount, hasFile) switch
            {
                (1, 0, true) => RegisterMethodType.File,
                (2, 0, _) => RegisterMethodType.Generic,
                (1, 1, _) => RegisterMethodType.Property,
                _ => RegisterMethodType.Invalid
            };

            if (registerMethodType == RegisterMethodType.Invalid)
            {
                productionContext.ReportDiagnostic(InvalidRegisterMethod(methodSymbol.Locations.FirstOrDefault(),
                    methodSymbol.ToString()));
                continue;
            }

            registerMethods.Add((methodSymbol, registerMethodType, registryPhase, hasFile));
        }

        if (registerMethods.Count == 0)
        {
            productionContext.ReportDiagnostic(NoRegisterMethods(registryClass.Locations.FirstOrDefault(),
                registryClass.ToString()));
        }
    }

    private static (INamedTypeSymbol, AttributeData)? IsValidRegistryClass(GeneratorSyntaxContext syntaxContext)
    {
        if (syntaxContext.SemanticModel.GetDeclaredSymbol(syntaxContext.Node) is not INamedTypeSymbol classSymbol)
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

        return (classSymbol, registryAttributeData);
    }


    
}