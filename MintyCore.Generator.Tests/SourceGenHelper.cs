using System.Collections.Immutable;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace MintyCore.Generator.Tests;

public static partial class SourceGenHelper
{
    public static void Compile(IIncrementalGenerator generator, out Compilation? outputCompilation,
        out ImmutableArray<Diagnostic> diagnostics, out SyntaxTree[] generatedTrees, params string[] source)
    {
        var syntaxTrees = source.Select(s =>  CSharpSyntaxTree.ParseText(s)).ToArray();
        var compilation = CSharpCompilation.Create("test_compilation", syntaxTrees,
            Array.Empty<MetadataReference>(),
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
            Array.Empty<MetadataReference>(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver.RunGeneratorsAndUpdateCompilation(compilation, out outputCompilation, out diagnostics);

        generatedTrees = outputCompilation.SyntaxTrees.Where(t => !syntaxTrees.Contains(t)).ToArray();
    }


    [System.Text.RegularExpressions.GeneratedRegex("\\s+")]
    private static partial System.Text.RegularExpressions.Regex WhiteSpaceRegex();

    public static bool CodeMatch(string expected, string actual)
    {
        //replace all whitespace characters with a single space
        //these includes, tabs, whitespaces, newlines, etc.
        expected = WhiteSpaceRegex().Replace(expected, " ");
        actual = WhiteSpaceRegex().Replace(actual, " ");

        return expected == actual;
    }

    public static void Analyze(DiagnosticAnalyzer analyzer, out ImmutableArray<Diagnostic> diagnostics,
        params string[] source)
    {
        var syntaxTrees = source.Select(s =>  CSharpSyntaxTree.ParseText(s)).ToArray();

        var compilation = CSharpCompilation.Create("test_compilation", syntaxTrees,
            Array.Empty<MetadataReference>(),
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));


        diagnostics = compilation.WithAnalyzers(ImmutableArray.Create(analyzer))
            .GetAnalyzerDiagnosticsAsync().Result;
    }
}