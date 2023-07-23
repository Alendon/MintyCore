using System.Collections.Immutable;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace MintyCore.Generator.Tests;

public static class SourceGenHelper
{
    public static void Compile(IIncrementalGenerator generator, out Compilation? outputCompilation,
        out ImmutableArray<Diagnostic> diagnostics, out SyntaxTree[] generatedTrees, params string[] source)
    {
        var syntaxTrees = source.Select(s =>  CSharpSyntaxTree.ParseText(s)).ToArray();
        var compilation = CSharpCompilation.Create("test_compilation", syntaxTrees,
            new []{ MetadataReference.CreateFromFile(typeof(object).Assembly.Location)},
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver.RunGeneratorsAndUpdateCompilation(compilation, out outputCompilation, out diagnostics);

        generatedTrees = outputCompilation.SyntaxTrees.Where(t => !syntaxTrees.Contains(t)).ToArray();
    }

    public static void Compile(ISourceGenerator generator, out Compilation? outputCompilation,
        out ImmutableArray<Diagnostic> diagnostics, out SyntaxTree[] generatedTrees, params string[] source)
    {
        var syntaxTrees = source.Select(s =>  CSharpSyntaxTree.ParseText(s)).ToArray();

        var compilation = CSharpCompilation.Create("test_compilation", syntaxTrees,
            new []{ MetadataReference.CreateFromFile(typeof(object).Assembly.Location)},
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver.RunGeneratorsAndUpdateCompilation(compilation, out outputCompilation, out diagnostics);

        generatedTrees = outputCompilation.SyntaxTrees.Where(t => !syntaxTrees.Contains(t)).ToArray();
    }

    public static void Analyze(DiagnosticAnalyzer analyzer, out ImmutableArray<Diagnostic> diagnostics,
        params string[] source)
    {
        var syntaxTrees = source.Select(s =>  CSharpSyntaxTree.ParseText(s)).ToArray();

        var compilation = CSharpCompilation.Create("test_compilation", syntaxTrees,
            new []{ MetadataReference.CreateFromFile(typeof(object).Assembly.Location)},
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));


        diagnostics = compilation.WithAnalyzers(ImmutableArray.Create(analyzer))
            .GetAnalyzerDiagnosticsAsync().Result;
    }
}