using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static MintyCore.Generator.Registry.RegistryHelper;

namespace MintyCore.Generator.Registry;

[Generator]
public class RegistryGenerator : IIncrementalGenerator
{
    [SuppressMessage("ReSharper", "SuggestVarOrType_Elsewhere")]
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        //find mod class and extract required mod infos
        IncrementalValueProvider<ModInfo> modInfo = FindModClass(context);


        //find new registry classes and extract register method infos
        IncrementalValuesProvider<RegisterMethodInfo> newRegisterMethodInfos =
            FindNewRegisterMethodInfos(context);

        IncrementalValueProvider<ImmutableArray<RegisterMethodInfo>> newRegisterMethodInfosList =
            newRegisterMethodInfos.Collect();


        //find generic registry calls
        IncrementalValuesProvider<RegisterObject> genericRegisterObjects =
            FindGenericRegisterCalls(context, newRegisterMethodInfosList);

        //find property registry calls
        IncrementalValuesProvider<RegisterObject> propertyRegisterObjects =
            FindPropertyRegisterCalls(context, newRegisterMethodInfosList);
        
        //find method registry calls
        IncrementalValuesProvider<RegisterObject> methodRegisterObjects =
            FindMethodRegisterCalls(context, newRegisterMethodInfosList);

        //extract registry calls from registry json file
        IncrementalValuesProvider<RegisterObject> fileRegisterObjects =
            ExtractRegisterCallsFromJson(context, newRegisterMethodInfosList);


        //write new register method info source
        context.RegisterSourceOutput(newRegisterMethodInfos, static (context, registerMethod) =>
        {
            context.AddSource(
                $"{registerMethod.Namespace}.{registerMethod.ClassName}.{registerMethod.MethodName}.info.g.cs",
                SourceBuilder.RenderRegisterMethodInfo(registerMethod));
        });

        //write register attributes from new register method infos
        context.RegisterSourceOutput(newRegisterMethodInfos.Where(x => x.RegisterType is not RegisterMethodType.File),
            static (context, registerMethod) =>
            {
                context.AddSource(
                    $"{registerMethod.Namespace}.{registerMethod.ClassName}.{registerMethod.MethodName}.att.g.cs",
                    SourceBuilder.RenderAttribute(registerMethod));
            });

        //collect a list of used register classes
        IncrementalValueProvider<ImmutableArray<RegisterMethodInfo>> usedRegisterClasses =
            genericRegisterObjects.Collect()
                .Combine(propertyRegisterObjects.Collect())
                .Combine(methodRegisterObjects.Collect())
                .Combine(fileRegisterObjects.Collect())
                .Select((tuple, _) =>
                {
                    var arr1 = tuple.Item1.Left.Left;
                    var arr2 = tuple.Item1.Left.Right;
                    var arr3 = tuple.Item1.Right;
                    var arr4 = tuple.Item2;

                    return arr1.Concat(arr2).Concat(arr3).Concat(arr4)
                        .GroupBy(item => $"{item.RegisterMethodInfo.Namespace}.{item.RegisterMethodInfo.ClassName}")
                        .Select(group => group.First())
                        .Select(x => x.RegisterMethodInfo)
                        .ToImmutableArray();
                });


        //write mod extension with mod info and used register method infos
        context.RegisterSourceOutput(modInfo.Combine(usedRegisterClasses), static (context, tuple) =>
        {
            var (mod, registerMethodInfos) = tuple;
            context.AddSource($"{mod.Namespace}.{mod.ClassName}.g.cs",
                SourceBuilder.RenderModExtension(mod, registerMethodInfos));
        });

        //write registry ids with mod info and newly added register method infos
        var newRegistryClasses = newRegisterMethodInfosList.Select(
            (x, _) =>
                x.GroupBy(y => y.CategoryName)
                    .Select(methodInfos => methodInfos.First()));

        context.RegisterSourceOutput(modInfo.Combine(newRegistryClasses), static (context, tuple) =>
        {
            var (mod, registerMethodInfos) = tuple;
            context.AddSource($"{mod.Namespace}.Identifications.RegistryIDs.g.cs",
                SourceBuilder.RenderRegistryIDs(mod, registerMethodInfos));
        });


        //combine and group register calls by class name
        IncrementalValuesProvider<ImmutableArray<RegisterObject>> groupedRegisterObjects =
            genericRegisterObjects.Collect()
                .Combine(propertyRegisterObjects.Collect())
                .Combine(methodRegisterObjects.Collect())
                .Combine(fileRegisterObjects.Collect())
                .SelectMany(IEnumerable<RegisterObject> (tuple, _) =>
                {
                    var arr1 = tuple.Left.Left.Left;
                    var arr2 = tuple.Left.Left.Right;
                    var arr3 = tuple.Left.Right;
                    var arr4 = tuple.Right;

                    return arr1.Concat(arr2).Concat(arr3).Concat(arr4);
                })
                .Collect()
                .SelectMany((registerObjects, cancellationToken) =>
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    //group register calls by registry namespace and class name
                    return registerObjects
                        .GroupBy(x => $"{x.RegisterMethodInfo.Namespace}.{x.RegisterMethodInfo.ClassName}")
                        .Select(x => x.ToImmutableArray());
                });

        //write registry calls with mod info and grouped register objects
        context.RegisterSourceOutput(groupedRegisterObjects.Combine(modInfo), static (context, tuple) =>
        {
            var (registerObjects, mod) = tuple;

            if (registerObjects.Length == 0) return;
            var methodInfo = registerObjects[0].RegisterMethodInfo;

            context.AddSource($"{mod.Namespace}.Identifications.{methodInfo.CategoryName}IDs.g.cs",
                SourceBuilder.RenderRegistryObjectIDs(mod, registerObjects));
        });
    }

    private static IncrementalValuesProvider<RegisterObject> ExtractRegisterCallsFromJson(
        IncrementalGeneratorInitializationContext context,
        IncrementalValueProvider<ImmutableArray<RegisterMethodInfo>> newRegisterMethodInfosList)
    {
        return context.AdditionalTextsProvider
            .Where(text => text.Path.EndsWith(".registry.json")).Collect()
            .Combine(context.CompilationProvider)
            .Combine(newRegisterMethodInfosList)
            .SelectMany(ExtractFileRegisterObjects);
    }

    private static IncrementalValuesProvider<RegisterObject> FindPropertyRegisterCalls(
        IncrementalGeneratorInitializationContext context,
        IncrementalValueProvider<ImmutableArray<RegisterMethodInfo>> newRegisterMethodInfosList)
    {
        return context.SyntaxProvider.CreateSyntaxProvider(
                static (node, _) => node is PropertyDeclarationSyntax { AttributeLists.Count: > 0 },
                static IPropertySymbol? (syntaxContext, _) =>
                {
                    if (syntaxContext.SemanticModel.GetDeclaredSymbol(syntaxContext.Node) is not IPropertySymbol
                        propertySymbol)
                        return null;

                    var hasErrorAttribute = propertySymbol.GetAttributes()
                        .Any(x => x.AttributeClass?.Kind == SymbolKind.ErrorType);

                    var hasRegisterAttribute = propertySymbol.GetAttributes()
                        .Any(x => x.AttributeClass?.BaseType?.ToDisplayString() == RegisterBaseAttributeName);

                    return hasErrorAttribute || hasRegisterAttribute ? propertySymbol : null;
                }
            ).Where(x => x is not null).Select<IPropertySymbol?, IPropertySymbol>((x, _) => x!)
            .Combine(newRegisterMethodInfosList)
            .Select(ExtractPropertyRegistryCall)
            .Where(
                x =>
                    x is not null)
            .Select(
                (x, _)
                    =>
                    x!);
    }
    
    private static IncrementalValuesProvider<RegisterObject> FindMethodRegisterCalls(
        IncrementalGeneratorInitializationContext context,
        IncrementalValueProvider<ImmutableArray<RegisterMethodInfo>> newRegisterMethodInfosList)
    {
        return context.SyntaxProvider.CreateSyntaxProvider(
                static (node, _) => node is MethodDeclarationSyntax { AttributeLists.Count: > 0 },
                static IMethodSymbol? (syntaxContext, _) =>
                {
                    if (syntaxContext.SemanticModel.GetDeclaredSymbol(syntaxContext.Node) is not IMethodSymbol
                        methodSymbol)
                        return null;

                    var hasErrorAttribute = methodSymbol.GetAttributes()
                        .Any(x => x.AttributeClass?.Kind == SymbolKind.ErrorType);

                    var hasRegisterAttribute = methodSymbol.GetAttributes()
                        .Any(x => x.AttributeClass?.BaseType?.ToDisplayString() == RegisterBaseAttributeName);

                    return hasErrorAttribute || hasRegisterAttribute ? methodSymbol : null;
                }
            ).Where(x => x is not null).Select<IMethodSymbol?, IMethodSymbol>((x, _) => x!)
            .Combine(newRegisterMethodInfosList)
            .Select(ExtractMethodRegistryCall)
            .Where(
                x =>
                    x is not null)
            .Select(
                (x, _)
                    =>
                    x!);
    }

    private static IncrementalValuesProvider<RegisterObject> FindGenericRegisterCalls(
        IncrementalGeneratorInitializationContext context,
        IncrementalValueProvider<ImmutableArray<RegisterMethodInfo>> newRegisterMethodInfosList)
    {
        return context.SyntaxProvider.CreateSyntaxProvider(
                static (node, _) =>
                    node is TypeDeclarationSyntax,
                static INamedTypeSymbol? (syntaxContext, _) =>
                {
                    if (syntaxContext.SemanticModel.GetDeclaredSymbol(syntaxContext.Node) is not INamedTypeSymbol
                        namedTypeSymbol)
                        return null;

                    if (namedTypeSymbol.GetAttributes().Length == 0)
                        return null;

                    var hasErrorAttribute = namedTypeSymbol.GetAttributes()
                        .Any(x => x.AttributeClass?.Kind == SymbolKind.ErrorType);

                    var hasRegisterAttribute = namedTypeSymbol.GetAttributes()
                        .Any(x => x.AttributeClass?.BaseType?.ToDisplayString() == RegisterBaseAttributeName);

                    return hasErrorAttribute || hasRegisterAttribute ? namedTypeSymbol : null;
                }).Where(x => x is not null).Select<INamedTypeSymbol?, INamedTypeSymbol>((x, _) => x!)
            .Combine(newRegisterMethodInfosList)
            .Select(ExtractGenericRegistryCall)
            .Where(x => x is not null).Select((x, _) => x!);
    }

    private static IncrementalValuesProvider<RegisterMethodInfo> FindNewRegisterMethodInfos(
        IncrementalGeneratorInitializationContext context)
    {
        return context.SyntaxProvider.CreateSyntaxProvider(
                static (node, _) => node is ClassDeclarationSyntax { AttributeLists.Count: > 0, },
                static (INamedTypeSymbol registryClass, AttributeData registryAttribute)? (syntaxContext, _) =>
                {
                    if (syntaxContext.SemanticModel.GetDeclaredSymbol(syntaxContext.Node) is not INamedTypeSymbol
                            {
                                DeclaredAccessibility: Accessibility.Public, IsAbstract: false
                            }
                            classSymbol) return null;
                    if (!classSymbol.AllInterfaces.Any(i => RegistryInterfaceName.Equals(i.ToDisplayString())))
                        return null;
                    var registryAttribute = classSymbol.GetAttributes()
                        .FirstOrDefault(x =>
                            RegistryClassAttributeName.Equals(x.AttributeClass?.ToDisplayString()));

                    return registryAttribute is not null ? (classSymbol, registryAttribute) : null;
                }
            ).Where(x => x is not null).Select((x, _) => x!.Value)
            .SelectMany(ExtractRegisterMethodsFromRegistryClass);
    }

    private static IncrementalValueProvider<ModInfo> FindModClass(IncrementalGeneratorInitializationContext context)
    {
        return context.SyntaxProvider.CreateSyntaxProvider(
                static (node, _) => node is ClassDeclarationSyntax,
                static ModInfo? (syntaxContext, _) =>
                    GetModInfo(syntaxContext.SemanticModel.GetDeclaredSymbol(syntaxContext.Node)))
            .Collect().Select((modInfos, _) => modInfos
                .Where(x => x is not null)
                .Select(x => x!.Value)
                .First());
    }
}