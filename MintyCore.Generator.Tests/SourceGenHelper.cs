using System.Collections.Immutable;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using MintyCore.Modding;
using MintyCore.Network;

namespace MintyCore.Generator.Tests;

public static partial class SourceGenHelper
{
    public static void Compile(IIncrementalGenerator generator, string source, out Compilation? outputCompilation,
        out ImmutableArray<Diagnostic> diagnostics, out SyntaxTree[] generatedTrees)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create("test_compilation", new[] { syntaxTree },
            new[] { MetadataReference.CreateFromFile(typeof(IMessage).GetTypeInfo().Assembly.Location) },
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver.RunGeneratorsAndUpdateCompilation(compilation, out outputCompilation, out diagnostics);

        generatedTrees = outputCompilation.SyntaxTrees.Where(t => t != syntaxTree).ToArray();
    }

    public static void Compile(ISourceGenerator generator, string source, out Compilation? outputCompilation,
        out ImmutableArray<Diagnostic> diagnostics, out SyntaxTree[] generatedTrees)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create("test_compilation", new[] { syntaxTree },
            new[] { MetadataReference.CreateFromFile(typeof(IMessage).GetTypeInfo().Assembly.Location) },
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver.RunGeneratorsAndUpdateCompilation(compilation, out outputCompilation, out diagnostics);

        generatedTrees = outputCompilation.SyntaxTrees.Where(t => t != syntaxTree).ToArray();
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
    
    public static void Analyze(string testCode, DiagnosticAnalyzer analyzer, out ImmutableArray<Diagnostic> diagnostics)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(testCode);

        var compilation = CSharpCompilation.Create("test_compilation", new[] { syntaxTree },
            new[] { MetadataReference.CreateFromFile(typeof(IMod).GetTypeInfo().Assembly.Location) },
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));


        diagnostics = compilation.WithAnalyzers(ImmutableArray.Create(analyzer))
            .GetAnalyzerDiagnosticsAsync().Result;
    }
}