using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Emit;
using MintyCore.Generator.Registry;
using Moq;
using Scriban;
using SharedCode;

namespace MintyCore.Generator.Tests.TestRegistry;

public class UnitTestRegistryGenerator
{
    private const string TestTemplateDir = "MintyCore.Generator.Tests.TestRegistry.Templates";

    private static Template RegistryTemplate => Template.Parse(
        EmbeddedFileHelper.ReadEmbeddedTextFileOrThrow($"{TestTemplateDir}.Input_Registry.sbncs"));

    private static Template AttributeTemplate =>
        Template.Parse(EmbeddedFileHelper.ReadEmbeddedTextFileOrThrow($"{TestTemplateDir}.RegistryAttribute.sbncs"));

    private static Template InfoTemplate =>
        Template.Parse(EmbeddedFileHelper.ReadEmbeddedTextFileOrThrow($"{TestTemplateDir}.RegistryInfo.sbncs"));

    [Fact]
    public void ClassSyntaxAsModSymbol_ValidModClassNode_ShouldReturnModSymbol()
    {
        var modInterfaceTree = CSharpSyntaxTree.ParseText(ModInterface);
        var modClassTree = CSharpSyntaxTree.ParseText(TestMod);

        var compilation = CreateCompilation(modInterfaceTree, modClassTree);

        var modClassNode = modClassTree.GetRoot().DescendantNodes().Where(n => n is ClassDeclarationSyntax)
            .Cast<ClassDeclarationSyntax>().First();

        var semanticModel = compilation.GetSemanticModel(modClassTree);
        var expectedModSymbol = semanticModel.GetDeclaredSymbol(modClassNode);


        var actualModSymbol = RegistryGeneratoor.ClassSyntaxAsModSymbol(semanticModel, modClassNode);

        Assert.Equal(expectedModSymbol, actualModSymbol);
    }

    [Fact]
    public void ClassSyntaxAsModSymbol_WithoutInterfaceModClassNode_ShouldReturnNull()
    {
        var modInterfaceTree = CSharpSyntaxTree.ParseText(ModInterface);
        var modClassTree = CSharpSyntaxTree.ParseText("""
                                                      public class TestMod
                                                      {
                                                          public void TestMethod()
                                                          {
                                                          }
                                                      }
                                                      """);
        var compilation = CreateCompilation(modInterfaceTree, modClassTree);

        var modClassNode = modClassTree.GetRoot().DescendantNodes().Where(n => n is ClassDeclarationSyntax)
            .Cast<ClassDeclarationSyntax>().First();

        var semanticModel = compilation.GetSemanticModel(modClassTree);

        var actualModSymbol = RegistryGeneratoor.ClassSyntaxAsModSymbol(semanticModel, modClassNode);

        Assert.Null(actualModSymbol);
    }

    [Fact]
    public void ClassSyntaxAsModSymbol_AbstractModClassNode_ShouldReturnNull()
    {
        var modInterfaceTree = CSharpSyntaxTree.ParseText(ModInterface);
        var modClassTree = CSharpSyntaxTree.ParseText("""
                                                      using MintyCore.Modding;

                                                      public abstract class Test : IMod
                                                      {
                                                          
                                                      }
                                                      """);

        var compilation = CreateCompilation(modInterfaceTree, modClassTree);

        var modClassNode = modClassTree.GetRoot().DescendantNodes().Where(n => n is ClassDeclarationSyntax)
            .Cast<ClassDeclarationSyntax>().First();

        var semanticModel = compilation.GetSemanticModel(modClassTree);

        var actualModSymbol = RegistryGeneratoor.ClassSyntaxAsModSymbol(semanticModel, modClassNode);

        Assert.Null(actualModSymbol);
    }

    [Fact]
    public void IsIRegistryClass_ValidRegistryClass_ShouldReturnTrue()
    {
        var registryClassTree = CSharpSyntaxTree.ParseText(RegistryTemplate.Render());
        var registryBaseCodeTree = CSharpSyntaxTree.ParseText(RegistryBaseCode);

        var compilation = CreateCompilation(registryClassTree, registryBaseCodeTree);

        var registryClassNode = registryClassTree.GetRoot().DescendantNodes().Where(n => n is ClassDeclarationSyntax)
            .Cast<ClassDeclarationSyntax>().First();
        var semanticModel = compilation.GetSemanticModel(registryClassTree);
        var registryClassSymbol = semanticModel.GetDeclaredSymbol(registryClassNode);


        Assert.True(RegistryGeneratoor.IsIRegistryClass(registryClassSymbol!));
    }

    [Fact]
    public void IsIRegistryClass_InvalidValidRegistryClass_ShouldReturnFalse()
    {
        var registryClassTree = CSharpSyntaxTree.ParseText("""
                                                           public class TestRegistry
                                                           {
                                                               public void TestMethod()
                                                               {
                                                               }
                                                           }
                                                           """);
        var registryBaseCodeTree = CSharpSyntaxTree.ParseText(RegistryBaseCode);

        var compilation = CreateCompilation(registryClassTree, registryBaseCodeTree);

        var registryClassNode = registryClassTree.GetRoot().DescendantNodes().Where(n => n is ClassDeclarationSyntax)
            .Cast<ClassDeclarationSyntax>().First();
        var semanticModel = compilation.GetSemanticModel(registryClassTree);
        var registryClassSymbol = semanticModel.GetDeclaredSymbol(registryClassNode);


        Assert.False(RegistryGeneratoor.IsIRegistryClass(registryClassSymbol!));
    }

    [Fact]
    public void GetRegistryClassAttributeDataOrNull_ValidRegistryClass_ShouldReturnAttributeData()
    {
        var registryClassTree = CSharpSyntaxTree.ParseText(RegistryTemplate.Render());
        var registryBaseCodeTree = CSharpSyntaxTree.ParseText(RegistryBaseCode);

        var compilation = CreateCompilation(registryClassTree, registryBaseCodeTree);

        var registryClassNode = registryClassTree.GetRoot().DescendantNodes().Where(n => n is ClassDeclarationSyntax)
            .Cast<ClassDeclarationSyntax>().First();
        var semanticModel = compilation.GetSemanticModel(registryClassTree);
        var registryClassSymbol = semanticModel.GetDeclaredSymbol(registryClassNode);

        var expectedAttributeData = registryClassSymbol!.GetAttributes().FirstOrDefault();


        var actualAttributeData = RegistryGeneratoor.GetRegistryClassAttributeDataOrNull(registryClassSymbol);

        Assert.Equal(expectedAttributeData, actualAttributeData);
    }

    [Fact]
    public void GetRegistryClassAttributeDataOrNull_WithoutAttribute_ShouldReturnNull()
    {
        var registryClassTree = CSharpSyntaxTree.ParseText("""
                                                           public class TestRegistry : IRegistry
                                                           {
                                                               public void TestMethod()
                                                               {
                                                               }
                                                           }
                                                           """);

        var registryBaseCodeTree = CSharpSyntaxTree.ParseText(RegistryBaseCode);

        var compilation = CreateCompilation(registryClassTree, registryBaseCodeTree);

        var registryClassNode = registryClassTree.GetRoot().DescendantNodes().Where(n => n is ClassDeclarationSyntax)
            .Cast<ClassDeclarationSyntax>().First();
        var semanticModel = compilation.GetSemanticModel(registryClassTree);
        var registryClassSymbol = semanticModel.GetDeclaredSymbol(registryClassNode);


        var actualAttributeData = RegistryGeneratoor.GetRegistryClassAttributeDataOrNull(registryClassSymbol!);

        Assert.Null(actualAttributeData);
    }

    [Fact]
    public void GetRegistryClassAttributeDataOrNull_WithInvalidAttribute_ShouldReturnNull()
    {
        var invalidAttributeTree = CSharpSyntaxTree.ParseText("""
                                                              public class TestAttribute : Attribute
                                                              {
                                                              }
                                                              """);

        var registryClassTree = CSharpSyntaxTree.ParseText("""
                                                           [TestAttribute]
                                                           public class TestRegistry : IRegistry
                                                           {
                                                               public void TestMethod()
                                                               {
                                                               }
                                                           }
                                                           """);

        var registryBaseCodeTree = CSharpSyntaxTree.ParseText(RegistryBaseCode);

        var compilation = CreateCompilation(registryClassTree, registryBaseCodeTree, invalidAttributeTree);

        var registryClassNode = registryClassTree.GetRoot().DescendantNodes().Where(n => n is ClassDeclarationSyntax)
            .Cast<ClassDeclarationSyntax>().First();
        var semanticModel = compilation.GetSemanticModel(registryClassTree);
        var registryClassSymbol = semanticModel.GetDeclaredSymbol(registryClassNode);


        var actualAttributeData = RegistryGeneratoor.GetRegistryClassAttributeDataOrNull(registryClassSymbol!);

        Assert.Null(actualAttributeData);
    }

    [InlineData(null, GenericConstraints.None)]
    [InlineData("System.IDisposable", GenericConstraints.None,
        "global::System.IDisposable")]
    [InlineData("System.Collections.IDictionary<Identification, GenericType>", GenericConstraints.None,
        "global::System.Collections.IDictionary<global::MintyCore.Utils.Identification, GenericType>")]
    [InlineData("new", GenericConstraints.Constructor)]
    [InlineData("class", GenericConstraints.ReferenceType)]
    [InlineData("struct", GenericConstraints.ValueType)]
    [InlineData("unmanaged", GenericConstraints.ValueType | GenericConstraints.UnmanagedType)]
    [InlineData("notnull", GenericConstraints.NotNull)]
    [Theory]
    public void ExtractRegisterMethodInfosFromAttribute_Generic_ShouldReturnRegisterMethodInfo(string? constraint,
        GenericConstraints genericConstraints, params string[] typeConstraints)
    {
        Dictionary<string, object?> templateArgs = new()
        {
            {"IsGeneric", true},
            {"TypeConstraint", constraint}
        };
        var registryClassTree = CSharpSyntaxTree.ParseText(RegistryTemplate.Render(templateArgs));

        var registryBaseCodeTree = CSharpSyntaxTree.ParseText(RegistryBaseCode);
        var identificationTree = CSharpSyntaxTree.ParseText(Identification);

        var compilation = CreateCompilation(registryClassTree, registryBaseCodeTree, identificationTree);

        var registryClassNode = registryClassTree.GetRoot().DescendantNodes().Where(n => n is ClassDeclarationSyntax)
            .Cast<ClassDeclarationSyntax>().First();
        var semanticModel = compilation.GetSemanticModel(registryClassTree);
        var registryClassSymbol = semanticModel.GetDeclaredSymbol(registryClassNode);


        var registryAttributeData = registryClassSymbol!.GetAttributes().FirstOrDefault();


        var expectedRegisterMethodInfo = EmptyRegisterMethodInfo with
        {
            MethodType = RegisterMethodType.Generic,
            Constraints = genericConstraints,
            GenericConstraintTypes = typeConstraints
        };

        var diagnostics = new List<Diagnostic>();
        var actualRegisterMethodInfos =
            RegistryGeneratoor.ExtractRegisterMethodsFromRegistryClass(registryClassSymbol, registryAttributeData!,
                diagnostics);

        Assert.Empty(diagnostics);
        var actualRegisterMethodInfo = Assert.Single(actualRegisterMethodInfos);

        AssertRegisterMethodEquality(expectedRegisterMethodInfo, actualRegisterMethodInfo);
    }

    [Fact]
    public void ExtractRegisterMethodInfosFromAttribute_Property_ShouldReturnRegisterMethodInfo()
    {
        Dictionary<string, object?> templateArgs = new()
        {
            {"PropertyType", "System.IDisposable"},
        };
        var registryClassTree = CSharpSyntaxTree.ParseText(RegistryTemplate.Render(templateArgs));

        var registryBaseCodeTree = CSharpSyntaxTree.ParseText(RegistryBaseCode);
        var identificationTree = CSharpSyntaxTree.ParseText(Identification);

        var compilation = CreateCompilation(registryClassTree, registryBaseCodeTree, identificationTree);

        var registryClassNode = registryClassTree.GetRoot().DescendantNodes().Where(n => n is ClassDeclarationSyntax)
            .Cast<ClassDeclarationSyntax>().First();
        var semanticModel = compilation.GetSemanticModel(registryClassTree);
        var registryClassSymbol = semanticModel.GetDeclaredSymbol(registryClassNode);


        var registryAttributeData = registryClassSymbol!.GetAttributes().FirstOrDefault();


        var expectedRegisterMethodInfo = EmptyRegisterMethodInfo with
        {
            MethodType = RegisterMethodType.Property,
            PropertyType = "global::System.IDisposable"
        };

        var diagnostics = new List<Diagnostic>();
        var actualRegisterMethodInfos =
            RegistryGeneratoor.ExtractRegisterMethodsFromRegistryClass(registryClassSymbol, registryAttributeData!,
                diagnostics);

        Assert.Empty(diagnostics);
        var actualRegisterMethodInfo = Assert.Single(actualRegisterMethodInfos);

        AssertRegisterMethodEquality(expectedRegisterMethodInfo, actualRegisterMethodInfo);
    }

    [Fact]
    public void ExtractRegisterMethodInfosFromAttribute_EmptyRegistry_ShouldReportWarning()
    {
        var registryClassTree = CSharpSyntaxTree.ParseText(
            """
            using MintyCore.Modding;
            using MintyCore.Modding.Attributes;
            using MintyCore.Utils;

            namespace TestMod.Registries;

            [Registry("test")]
            public class TestRegistry : IRegistry { }
            """);

        var registryBaseCodeTree = CSharpSyntaxTree.ParseText(RegistryBaseCode);
        var identificationTree = CSharpSyntaxTree.ParseText(Identification);

        var compilation = CreateCompilation(registryClassTree, registryBaseCodeTree, identificationTree);

        var registryClassNode = registryClassTree.GetRoot().DescendantNodes().Where(n => n is ClassDeclarationSyntax)
            .Cast<ClassDeclarationSyntax>().First();
        var semanticModel = compilation.GetSemanticModel(registryClassTree);
        var registryClassSymbol = semanticModel.GetDeclaredSymbol(registryClassNode);


        var registryAttributeData = registryClassSymbol!.GetAttributes().FirstOrDefault();


        var diagnostics = new List<Diagnostic>();
        var actualRegisterMethodInfos =
            RegistryGeneratoor.ExtractRegisterMethodsFromRegistryClass(registryClassSymbol, registryAttributeData!,
                diagnostics);

        var diagnostic = Assert.Single(diagnostics);
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.DefaultSeverity);
        Assert.Equal(DiagnosticIDs.NoRegisterMethods.ToIdString(), diagnostic.Id);

        Assert.Empty(actualRegisterMethodInfos);
    }

    [Fact]
    public void ExtractRegisterMethodInfosFromAttribute_GenericAndProperty_ShouldReportError()
    {
        Dictionary<string, object?> templateArgs = new()
        {
            {"PropertyType", "System.IDisposable"},
            {"IsGeneric", true}
        };
        var registryClassTree = CSharpSyntaxTree.ParseText(RegistryTemplate.Render(templateArgs));

        var registryBaseCodeTree = CSharpSyntaxTree.ParseText(RegistryBaseCode);
        var identificationTree = CSharpSyntaxTree.ParseText(Identification);

        var compilation = CreateCompilation(registryClassTree, registryBaseCodeTree, identificationTree);

        var registryClassNode = registryClassTree.GetRoot().DescendantNodes().Where(n => n is ClassDeclarationSyntax)
            .Cast<ClassDeclarationSyntax>().First();
        var semanticModel = compilation.GetSemanticModel(registryClassTree);
        var registryClassSymbol = semanticModel.GetDeclaredSymbol(registryClassNode);


        var registryAttributeData = registryClassSymbol!.GetAttributes().FirstOrDefault();


        var diagnostics = new List<Diagnostic>();
        var actualRegisterMethodInfos =
            RegistryGeneratoor.ExtractRegisterMethodsFromRegistryClass(registryClassSymbol, registryAttributeData!,
                diagnostics);

        Assert.Empty(actualRegisterMethodInfos);

        Assert.Equal(2, diagnostics.Count);

        var diagnostic = diagnostics[0];
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.DefaultSeverity);
        Assert.Equal(DiagnosticIDs.InvalidRegisterMethod.ToIdString(), diagnostic.Id);
        Assert.Contains("not supported", diagnostic.GetMessage());

        diagnostic = diagnostics[1];
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.DefaultSeverity);
        Assert.Equal(DiagnosticIDs.NoRegisterMethods.ToIdString(), diagnostic.Id);
    }

    [Fact]
    public void ExtractRegisterMethodInfosFromAttribute_HasFileAndUseExistingId_ShouldReportError()
    {
        Dictionary<string, object?> templateArgs = new()
        {
            {"PropertyType", "System.IDisposable"},
            {"HasFile", true},
            {"UseExistingId", true}
        };
        var registryClassTree = CSharpSyntaxTree.ParseText(RegistryTemplate.Render(templateArgs));

        var registryBaseCodeTree = CSharpSyntaxTree.ParseText(RegistryBaseCode);
        var identificationTree = CSharpSyntaxTree.ParseText(Identification);

        var compilation = CreateCompilation(registryClassTree, registryBaseCodeTree, identificationTree);

        var registryClassNode = registryClassTree.GetRoot().DescendantNodes().Where(n => n is ClassDeclarationSyntax)
            .Cast<ClassDeclarationSyntax>().First();
        var semanticModel = compilation.GetSemanticModel(registryClassTree);
        var registryClassSymbol = semanticModel.GetDeclaredSymbol(registryClassNode);


        var registryAttributeData = registryClassSymbol!.GetAttributes().FirstOrDefault();


        var diagnostics = new List<Diagnostic>();
        var actualRegisterMethodInfos =
            RegistryGeneratoor.ExtractRegisterMethodsFromRegistryClass(registryClassSymbol, registryAttributeData!,
                diagnostics);

        Assert.Empty(actualRegisterMethodInfos);

        Assert.Equal(2, diagnostics.Count);

        var diagnostic = diagnostics[0];
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.DefaultSeverity);
        Assert.Equal(DiagnosticIDs.InvalidRegisterMethod.ToIdString(), diagnostic.Id);
        Assert.Contains("Invalid Flag combination", diagnostic.GetMessage());

        diagnostic = diagnostics[1];
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.DefaultSeverity);
        Assert.Equal(DiagnosticIDs.NoRegisterMethods.ToIdString(), diagnostic.Id);
    }

    [Fact]
    public void ExtractRegisterMethodInfosFromAttribute_HasFile_ShouldReturnRegisterMethodInfo()
    {
        Dictionary<string, object?> templateArgs = new()
        {
            {"PropertyType", "System.IDisposable"},
            {"HasFile", true}
        };
        var registryClassTree = CSharpSyntaxTree.ParseText(RegistryTemplate.Render(templateArgs));

        var registryBaseCodeTree = CSharpSyntaxTree.ParseText(RegistryBaseCode);
        var identificationTree = CSharpSyntaxTree.ParseText(Identification);

        var compilation = CreateCompilation(registryClassTree, registryBaseCodeTree, identificationTree);

        var registryClassNode = registryClassTree.GetRoot().DescendantNodes().Where(n => n is ClassDeclarationSyntax)
            .Cast<ClassDeclarationSyntax>().First();
        var semanticModel = compilation.GetSemanticModel(registryClassTree);
        var registryClassSymbol = semanticModel.GetDeclaredSymbol(registryClassNode);


        var registryAttributeData = registryClassSymbol!.GetAttributes().FirstOrDefault();


        var expectedRegisterMethodInfo = EmptyRegisterMethodInfo with
        {
            MethodType = RegisterMethodType.Property,
            PropertyType = "global::System.IDisposable",
            HasFile = true
        };

        var diagnostics = new List<Diagnostic>();
        var actualRegisterMethodInfos =
            RegistryGeneratoor.ExtractRegisterMethodsFromRegistryClass(registryClassSymbol, registryAttributeData!,
                diagnostics);

        Assert.Empty(diagnostics);
        var actualRegisterMethodInfo = Assert.Single(actualRegisterMethodInfos);

        AssertRegisterMethodEquality(expectedRegisterMethodInfo, actualRegisterMethodInfo);
    }

    [Fact]
    public void ExtractRegisterMethodInfosFromAttribute_FileOnly_ShouldReturnRegisterMethodInfo()
    {
        Dictionary<string, object?> templateArgs = new()
        {
            {"HasFile", true}
        };
        var registryClassTree = CSharpSyntaxTree.ParseText(RegistryTemplate.Render(templateArgs));

        var registryBaseCodeTree = CSharpSyntaxTree.ParseText(RegistryBaseCode);
        var identificationTree = CSharpSyntaxTree.ParseText(Identification);

        var compilation = CreateCompilation(registryClassTree, registryBaseCodeTree, identificationTree);

        var registryClassNode = registryClassTree.GetRoot().DescendantNodes().Where(n => n is ClassDeclarationSyntax)
            .Cast<ClassDeclarationSyntax>().First();
        var semanticModel = compilation.GetSemanticModel(registryClassTree);
        var registryClassSymbol = semanticModel.GetDeclaredSymbol(registryClassNode);


        var registryAttributeData = registryClassSymbol!.GetAttributes().FirstOrDefault();


        var expectedRegisterMethodInfo = EmptyRegisterMethodInfo with
        {
            MethodType = RegisterMethodType.File,
            HasFile = true
        };

        var diagnostics = new List<Diagnostic>();
        var actualRegisterMethodInfos =
            RegistryGeneratoor.ExtractRegisterMethodsFromRegistryClass(registryClassSymbol, registryAttributeData!,
                diagnostics);

        Assert.Empty(diagnostics);
        var actualRegisterMethodInfo = Assert.Single(actualRegisterMethodInfos);

        AssertRegisterMethodEquality(expectedRegisterMethodInfo, actualRegisterMethodInfo);
    }

    [Fact]
    public void ExtractRegisterMethodInfosFromAttribute_FileOnlyWithFolder_ShouldReturnRegisterMethodInfo()
    {
        Dictionary<string, object?> templateArgs = new()
        {
            {"HasFile", true},
            {"ResourceSubFolder", "test"}
        };
        var registryClassTree = CSharpSyntaxTree.ParseText(RegistryTemplate.Render(templateArgs));

        var registryBaseCodeTree = CSharpSyntaxTree.ParseText(RegistryBaseCode);
        var identificationTree = CSharpSyntaxTree.ParseText(Identification);

        var compilation = CreateCompilation(registryClassTree, registryBaseCodeTree, identificationTree);

        var registryClassNode = registryClassTree.GetRoot().DescendantNodes().Where(n => n is ClassDeclarationSyntax)
            .Cast<ClassDeclarationSyntax>().First();
        var semanticModel = compilation.GetSemanticModel(registryClassTree);
        var registryClassSymbol = semanticModel.GetDeclaredSymbol(registryClassNode);


        var registryAttributeData = registryClassSymbol!.GetAttributes().FirstOrDefault();


        var expectedRegisterMethodInfo = EmptyRegisterMethodInfo with
        {
            MethodType = RegisterMethodType.File,
            HasFile = true,
            ResourceSubFolder = "test"
        };

        var diagnostics = new List<Diagnostic>();
        var actualRegisterMethodInfos =
            RegistryGeneratoor.ExtractRegisterMethodsFromRegistryClass(registryClassSymbol, registryAttributeData!,
                diagnostics);

        Assert.Empty(diagnostics);
        var actualRegisterMethodInfo = Assert.Single(actualRegisterMethodInfos);

        AssertRegisterMethodEquality(expectedRegisterMethodInfo, actualRegisterMethodInfo);
    }

    [Fact]
    public void ExtractRegisterMethodInfosFromAttribute_UseExistingId_ShouldReturnRegisterMethodInfo()
    {
        Dictionary<string, object?> templateArgs = new()
        {
            {"PropertyType", "System.IDisposable"},
            {"UseExistingId", true}
        };
        var registryClassTree = CSharpSyntaxTree.ParseText(RegistryTemplate.Render(templateArgs));

        var registryBaseCodeTree = CSharpSyntaxTree.ParseText(RegistryBaseCode);
        var identificationTree = CSharpSyntaxTree.ParseText(Identification);

        var compilation = CreateCompilation(registryClassTree, registryBaseCodeTree, identificationTree);

        var registryClassNode = registryClassTree.GetRoot().DescendantNodes().Where(n => n is ClassDeclarationSyntax)
            .Cast<ClassDeclarationSyntax>().First();
        var semanticModel = compilation.GetSemanticModel(registryClassTree);
        var registryClassSymbol = semanticModel.GetDeclaredSymbol(registryClassNode);


        var registryAttributeData = registryClassSymbol!.GetAttributes().FirstOrDefault();


        var expectedRegisterMethodInfo = EmptyRegisterMethodInfo with
        {
            MethodType = RegisterMethodType.Property,
            PropertyType = "global::System.IDisposable",
            UseExistingId = true
        };

        var diagnostics = new List<Diagnostic>();
        var actualRegisterMethodInfos =
            RegistryGeneratoor.ExtractRegisterMethodsFromRegistryClass(registryClassSymbol, registryAttributeData!,
                diagnostics);

        Assert.Empty(diagnostics);
        var actualRegisterMethodInfo = Assert.Single(actualRegisterMethodInfos);

        AssertRegisterMethodEquality(expectedRegisterMethodInfo, actualRegisterMethodInfo);
    }

    [Fact]
    public void ExtractRegisterMethodInfosFromAttribute_EmptyParameters_ShouldReportWarning()
    {
        var registryClassTree = CSharpSyntaxTree.ParseText(
            """
            using MintyCore.Modding;
            using MintyCore.Modding.Attributes;
            using MintyCore.Utils;

            namespace TestMod.Registries;

            [Registry("test")]
            public class TestRegistry : IRegistry {
            
                [RegisterMethod(ObjectRegistryPhase.Main)
                public static void Register(){}
            }
            """);

        var registryBaseCodeTree = CSharpSyntaxTree.ParseText(RegistryBaseCode);
        var identificationTree = CSharpSyntaxTree.ParseText(Identification);

        var compilation = CreateCompilation(registryClassTree, registryBaseCodeTree, identificationTree);

        var registryClassNode = registryClassTree.GetRoot().DescendantNodes().Where(n => n is ClassDeclarationSyntax)
            .Cast<ClassDeclarationSyntax>().First();
        var semanticModel = compilation.GetSemanticModel(registryClassTree);
        var registryClassSymbol = semanticModel.GetDeclaredSymbol(registryClassNode);


        var registryAttributeData = registryClassSymbol!.GetAttributes().FirstOrDefault();


        var diagnostics = new List<Diagnostic>();
        var actualRegisterMethodInfos =
            RegistryGeneratoor.ExtractRegisterMethodsFromRegistryClass(registryClassSymbol, registryAttributeData!,
                diagnostics);

        Assert.Empty(actualRegisterMethodInfos);

        Assert.Equal(2, diagnostics.Count);

        var diagnostic = diagnostics[0];
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.DefaultSeverity);
        Assert.Equal(DiagnosticIDs.InvalidRegisterMethod.ToIdString(), diagnostic.Id);
        Assert.Contains("No parameters found", diagnostic.GetMessage());

        diagnostic = diagnostics[1];
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.DefaultSeverity);
        Assert.Equal(DiagnosticIDs.NoRegisterMethods.ToIdString(), diagnostic.Id);
    }

    [Fact]
    public void ExtractRegisterMethodInfosFromAttribute_WrongFirstParameter_ShouldReportWarning()
    {
        var registryClassTree = CSharpSyntaxTree.ParseText(
            """
            using MintyCore.Modding;
            using MintyCore.Modding.Attributes;
            using MintyCore.Utils;

            namespace TestMod.Registries;

            [Registry("test")]
            public class TestRegistry : IRegistry {
            
                [RegisterMethod(ObjectRegistryPhase.Main)
                public static void Register(object value){}
            }
            """);

        var registryBaseCodeTree = CSharpSyntaxTree.ParseText(RegistryBaseCode);
        var identificationTree = CSharpSyntaxTree.ParseText(Identification);

        var compilation = CreateCompilation(registryClassTree, registryBaseCodeTree, identificationTree);

        var registryClassNode = registryClassTree.GetRoot().DescendantNodes().Where(n => n is ClassDeclarationSyntax)
            .Cast<ClassDeclarationSyntax>().First();
        var semanticModel = compilation.GetSemanticModel(registryClassTree);
        var registryClassSymbol = semanticModel.GetDeclaredSymbol(registryClassNode);


        var registryAttributeData = registryClassSymbol!.GetAttributes().FirstOrDefault();


        var diagnostics = new List<Diagnostic>();
        var actualRegisterMethodInfos =
            RegistryGeneratoor.ExtractRegisterMethodsFromRegistryClass(registryClassSymbol, registryAttributeData!,
                diagnostics);

        Assert.Empty(actualRegisterMethodInfos);

        Assert.Equal(2, diagnostics.Count);

        var diagnostic = diagnostics[0];
        Assert.Equal(DiagnosticSeverity.Error, diagnostic.DefaultSeverity);
        Assert.Equal(DiagnosticIDs.InvalidRegisterMethod.ToIdString(), diagnostic.Id);
        Assert.Contains("First parameter must be of type Identification", diagnostic.GetMessage());

        diagnostic = diagnostics[1];
        Assert.Equal(DiagnosticSeverity.Warning, diagnostic.DefaultSeverity);
        Assert.Equal(DiagnosticIDs.NoRegisterMethods.ToIdString(), diagnostic.Id);
    }

    [Fact]
    public void GenerateRegisterMethodInfoSource_Generic_ShouldOutputCorrectCode()
    {
        string[] constraintTypes = {"global::System.IDisposable", "global::System.IEquatable<string>"};

        Dictionary<string, object> templateInput = new()
        {
            {"RegisterType", (int) RegisterMethodType.Generic},
            {"GenericConstraints", (int) (GenericConstraints.ValueType | GenericConstraints.UnmanagedType)},
            {"GenericTypeConstraints", string.Join(",", constraintTypes.Select(x => $"{x}"))},
            {"RegistryPhase", 2},
        };

        var expectedAttributeSource = InfoTemplate.Render(templateInput);

        RegisterMethod registerMethodInfo = new()
        {
            ClassName = "TestRegistry",
            Namespace = "TestMod.Registries",
            MethodName = "RegisterTest",
            MethodType = RegisterMethodType.Generic,
            Constraints = GenericConstraints.ValueType | GenericConstraints.UnmanagedType,
            GenericConstraintTypes = constraintTypes,
            RegistryPhase = 2,
            CategoryId = "test"
        };

        var actualAttributeSource = RegistryGeneratoor.GenerateRegisterMethodInfoSource(registerMethodInfo);

        var expectedAttributeSyntaxTree = CSharpSyntaxTree.ParseText(expectedAttributeSource);
        var actualAttributeSyntaxTree = CSharpSyntaxTree.ParseText(actualAttributeSource);


        Assert.True(expectedAttributeSyntaxTree.IsEquivalentTo(actualAttributeSyntaxTree));
    }

    [Fact]
    public void GenerateRegisterMethodInfoSource_Property_ShouldOutputCorrectCode()
    {
        Dictionary<string, object> templateInput = new()
        {
            {"RegisterType", (int) RegisterMethodType.Property},
            {"RegistryPhase", 3},
            {"PropertyType", "global::System.IDisposable"},
        };

        var expectedAttributeSource = InfoTemplate.Render(templateInput);

        RegisterMethod registerMethodInfo = new()
        {
            ClassName = "TestRegistry",
            Namespace = "TestMod.Registries",
            MethodName = "RegisterTest",
            MethodType = RegisterMethodType.Property,
            PropertyType = "global::System.IDisposable",
            RegistryPhase = 3,
            CategoryId = "test",
        };

        var actualAttributeSource = RegistryGeneratoor.GenerateRegisterMethodInfoSource(registerMethodInfo);

        var expectedAttributeSyntaxTree = CSharpSyntaxTree.ParseText(expectedAttributeSource);
        var actualAttributeSyntaxTree = CSharpSyntaxTree.ParseText(actualAttributeSource);


        Assert.True(expectedAttributeSyntaxTree.IsEquivalentTo(actualAttributeSyntaxTree));
    }

    [Fact]
    public void GenerateRegisterMethodInfoSource_PropertyHasFile_ShouldOutputCorrectCode()
    {
        Dictionary<string, object> templateInput = new()
        {
            {"RegisterType", (int) RegisterMethodType.Property},
            {"RegistryPhase", 3},
            {"PropertyType", "global::System.IDisposable"},
            {"HasFile", true},
        };

        var expectedAttributeSource = InfoTemplate.Render(templateInput);

        RegisterMethod registerMethodInfo = new()
        {
            ClassName = "TestRegistry",
            Namespace = "TestMod.Registries",
            MethodName = "RegisterTest",
            MethodType = RegisterMethodType.Property,
            PropertyType = "global::System.IDisposable",
            RegistryPhase = 3,
            CategoryId = "test",
            HasFile = true
        };

        var actualAttributeSource = RegistryGeneratoor.GenerateRegisterMethodInfoSource(registerMethodInfo);

        var expectedAttributeSyntaxTree = CSharpSyntaxTree.ParseText(expectedAttributeSource);
        var actualAttributeSyntaxTree = CSharpSyntaxTree.ParseText(actualAttributeSource);


        Assert.True(expectedAttributeSyntaxTree.IsEquivalentTo(actualAttributeSyntaxTree));
    }

    [Fact]
    public void GenerateRegisterMethodInfoSource_PropertyExistingId_ShouldOutputCorrectCode()
    {
        Dictionary<string, object> templateInput = new()
        {
            {"RegisterType", (int) RegisterMethodType.Property},
            {"RegistryPhase", 3},
            {"PropertyType", "global::System.IDisposable"},
            {"UseExistingId", "true"}
        };

        var expectedAttributeSource = InfoTemplate.Render(templateInput);

        RegisterMethod registerMethodInfo = new()
        {
            ClassName = "TestRegistry",
            Namespace = "TestMod.Registries",
            MethodName = "RegisterTest",
            MethodType = RegisterMethodType.Property,
            PropertyType = "global::System.IDisposable",
            RegistryPhase = 3,
            CategoryId = "test",
            UseExistingId = true
        };

        var actualAttributeSource = RegistryGeneratoor.GenerateRegisterMethodInfoSource(registerMethodInfo);

        var expectedAttributeSyntaxTree = CSharpSyntaxTree.ParseText(expectedAttributeSource);
        var actualAttributeSyntaxTree = CSharpSyntaxTree.ParseText(actualAttributeSource);


        Assert.True(expectedAttributeSyntaxTree.IsEquivalentTo(actualAttributeSyntaxTree));
    }

    [Fact]
    public void GenerateRegisterMethodInfoSource_FileRegisterMethod_ShouldOutputCorrectCode()
    {
        Dictionary<string, object> templateInput = new()
        {
            {"RegisterType", (int) RegisterMethodType.File},
            {"RegistryPhase", 2},
            {"HasFile", true}
        };

        var expectedAttributeSource = InfoTemplate.Render(templateInput);

        RegisterMethod registerMethodInfo = new()
        {
            ClassName = "TestRegistry",
            Namespace = "TestMod.Registries",
            MethodName = "RegisterTest",
            MethodType = RegisterMethodType.File,
            RegistryPhase = 2,
            CategoryId = "test",
            HasFile = true
        };

        var actualAttributeSource = RegistryGeneratoor.GenerateRegisterMethodInfoSource(registerMethodInfo);

        var expectedAttributeSyntaxTree = CSharpSyntaxTree.ParseText(expectedAttributeSource);
        var actualAttributeSyntaxTree = CSharpSyntaxTree.ParseText(actualAttributeSource);


        Assert.True(expectedAttributeSyntaxTree.IsEquivalentTo(actualAttributeSyntaxTree));
    }

    [Fact]
    public void GenerateRegisterMethodAttributeSource_GenericNoConstraints_ShouldOutputCorrectCode()
    {
        Dictionary<string, object> templateArgs = new()
        {
            {"AttributeTargets", "global::System.AttributeTargets.Class | global::System.AttributeTargets.Struct"},
        };
        var expectedOutput = AttributeTemplate.Render(templateArgs);


        var registerMethodInfo = EmptyRegisterMethodInfo with
        {
            MethodType = RegisterMethodType.Generic,
        };
        var actualOutput = RegistryGeneratoor.GenerateRegisterMethodAttributeSource(registerMethodInfo);
        
        var expectedAttributeSyntaxTree = CSharpSyntaxTree.ParseText(expectedOutput);
        var actualAttributeSyntaxTree = CSharpSyntaxTree.ParseText(actualOutput);
        
        Assert.True(expectedAttributeSyntaxTree.IsEquivalentTo(actualAttributeSyntaxTree));
    }
    
    [Fact]
    public void GenerateRegisterMethodAttributeSource_GenericClassConstraint_ShouldOutputCorrectCode()
    {
        Dictionary<string, object> templateArgs = new()
        {
            {"AttributeTargets", "global::System.AttributeTargets.Class"},
        };
        var expectedOutput = AttributeTemplate.Render(templateArgs);


        var registerMethodInfo = EmptyRegisterMethodInfo with
        {
            MethodType = RegisterMethodType.Generic,
            Constraints = GenericConstraints.ReferenceType
        };
        var actualOutput = RegistryGeneratoor.GenerateRegisterMethodAttributeSource(registerMethodInfo);
        
        var expectedAttributeSyntaxTree = CSharpSyntaxTree.ParseText(expectedOutput);
        var actualAttributeSyntaxTree = CSharpSyntaxTree.ParseText(actualOutput);
        
        Assert.True(expectedAttributeSyntaxTree.IsEquivalentTo(actualAttributeSyntaxTree));
    }
    
    [Fact]
    public void GenerateRegisterMethodAttributeSource_GenericUnmanagedConstraint_ShouldOutputCorrectCode()
    {
        Dictionary<string, object> templateArgs = new()
        {
            {"AttributeTargets", "global::System.AttributeTargets.Struct"},
        };
        var expectedOutput = AttributeTemplate.Render(templateArgs);


        var registerMethodInfo = EmptyRegisterMethodInfo with
        {
            MethodType = RegisterMethodType.Generic,
            Constraints = GenericConstraints.ValueType | GenericConstraints.UnmanagedType
        };
        var actualOutput = RegistryGeneratoor.GenerateRegisterMethodAttributeSource(registerMethodInfo);
        
        var expectedAttributeSyntaxTree = CSharpSyntaxTree.ParseText(expectedOutput);
        var actualAttributeSyntaxTree = CSharpSyntaxTree.ParseText(actualOutput);
        
        Assert.True(expectedAttributeSyntaxTree.IsEquivalentTo(actualAttributeSyntaxTree));
    }

    [Fact]
    public void FindRegisterMethodInfoSymbols_SingleMethodInfoClass_ShouldReturnMethodInfoSymbol()
    {
        var registerInfoParams = new Dictionary<string, object>()
        {
            {"RegisterType", (int) RegisterMethodType.Generic},
            {"RegistryPhase", 2}
        };
        var registerInfo = InfoTemplate.Render(registerInfoParams);

        using MemoryStream memoryStream =
            Compile("test_base", RegistryBaseCode, ModInterface, Identification, registerInfo);

        var testCompilation = CreateCompilation(MetadataReference.CreateFromStream(memoryStream),
            """
            namespace HelloWorld;
            [TestMod.Registries.RegisterTestAttribute]
            public class TestClass
            {
                public MintyCore.Utils.Identification ID { get; set; }
            }
            """);

        var tokenSource = new CancellationTokenSource();
        IEnumerable<INamedTypeSymbol> symbols = Enumerable.Empty<INamedTypeSymbol>();

        Stopwatch sw = Stopwatch.StartNew();
        for (int i = 0; i < 1000; i++)
        {
            symbols = RegistryGeneratoor.FindRegisterMethodInfoSymbols(testCompilation, tokenSource.Token);
        }

        sw.Stop();

        var typeSymbol = Assert.Single(symbols);

        Assert.Equal("TestMod.Registries.TestRegistry_RegisterTest", typeSymbol.ToString());
    }

    [Fact]
    public void ExtractRegisterMethodInfoFromSymbol_GenericMethodInfo_ShouldReturnValidMethodInfo()
    {
        Dictionary<string, object> infoTemplateArgs = new()
        {
            {"RegisterType", (int) RegisterMethodType.Generic},
            {"GenericConstraints", (int) (GenericConstraints.ValueType | GenericConstraints.UnmanagedType)},
            {"GenericTypeConstraints", "global::System.IDisposable,global::System.IEquatable<string>"},
            {"RegistryPhase", 2},
            {"UseExistingId", true},
        };

        var expectedRegisterMethod = EmptyRegisterMethodInfo with
        {
            MethodType = RegisterMethodType.Generic,
            Constraints = GenericConstraints.ValueType | GenericConstraints.UnmanagedType,
            GenericConstraintTypes = new[] {"global::System.IDisposable", "global::System.IEquatable<string>"},
            RegistryPhase = 2,
            UseExistingId = true
        };

        var registerInfoSource = InfoTemplate.Render(infoTemplateArgs);
        var compilation = CreateCompilation(registerInfoSource, RegistryBaseCode, ModInterface, Identification);

        var registerInfoSymbol =
            (INamedTypeSymbol) compilation.GetSymbolsWithName("TestRegistry_RegisterTest", SymbolFilter.Type).First();

        var actualRegisterMethod =
            RegistryGeneratoor.ExtractRegisterMethodInfoFromSymbol(registerInfoSymbol, CancellationToken.None);
        
        AssertRegisterMethodEquality(expectedRegisterMethod, actualRegisterMethod);
    }
    
    [Fact]
    public void ExtractRegisterMethodInfoFromSymbol_PropertyMethodInfo_ShouldReturnValidMethodInfo()
    {
        Dictionary<string, object> infoTemplateArgs = new()
        {
            {"RegisterType", (int) RegisterMethodType.Property},
            {"RegistryPhase", 2},
            {"HasFile", true},
            {"PropertyType", "global::System.IDisposable"},
        };

        var expectedRegisterMethod = EmptyRegisterMethodInfo with
        {
            MethodType = RegisterMethodType.Property,
            RegistryPhase = 2,
            HasFile = true,
            PropertyType = "global::System.IDisposable"
        };

        var registerInfoSource = InfoTemplate.Render(infoTemplateArgs);
        var compilation = CreateCompilation(registerInfoSource, RegistryBaseCode, ModInterface, Identification);

        var registerInfoSymbol =
            (INamedTypeSymbol) compilation.GetSymbolsWithName("TestRegistry_RegisterTest", SymbolFilter.Type).First();

        var actualRegisterMethod =
            RegistryGeneratoor.ExtractRegisterMethodInfoFromSymbol(registerInfoSymbol, CancellationToken.None);
        
        AssertRegisterMethodEquality(expectedRegisterMethod, actualRegisterMethod);
    }
    
    [Fact]
    public void ExtractRegisterMethodInfoFromSymbol_FileMethodInfo_ShouldReturnValidMethodInfo()
    {
        Dictionary<string, object> infoTemplateArgs = new()
        {
            {"RegisterType", (int) RegisterMethodType.Property},
            {"RegistryPhase", 2},
            {"HasFile", true},
            {"ResourceSubFolder", "test"},
        };

        var expectedRegisterMethod = EmptyRegisterMethodInfo with
        {
            MethodType = RegisterMethodType.Property,
            RegistryPhase = 2,
            HasFile = true,
            ResourceSubFolder = "test"
        };

        var registerInfoSource = InfoTemplate.Render(infoTemplateArgs);
        var compilation = CreateCompilation(registerInfoSource, RegistryBaseCode, ModInterface, Identification);

        var registerInfoSymbol =
            (INamedTypeSymbol) compilation.GetSymbolsWithName("TestRegistry_RegisterTest", SymbolFilter.Type).First();

        var actualRegisterMethod =
            RegistryGeneratoor.ExtractRegisterMethodInfoFromSymbol(registerInfoSymbol, CancellationToken.None);
        
        AssertRegisterMethodEquality(expectedRegisterMethod, actualRegisterMethod);
    }


    private static void AssertRegisterMethodEquality(RegisterMethod expected, RegisterMethod actual)
    {
        Assert.Equal(expected.MethodType, actual.MethodType);
        Assert.Equal(expected.MethodName, actual.MethodName);
        Assert.Equal(expected.ClassName, actual.ClassName);
        Assert.Equal(expected.RegistryPhase, actual.RegistryPhase);
        Assert.Equal(expected.HasFile, actual.HasFile);
        Assert.Equal(expected.Constraints, actual.Constraints);
        Assert.Equal(expected.PropertyType, actual.PropertyType);
        Assert.Equal(expected.CategoryId, actual.CategoryId);
        Assert.Equal(expected.UseExistingId, actual.UseExistingId);
        Assert.Equal(expected.ResourceSubFolder, actual.ResourceSubFolder);
        Assert.Equal(expected.Namespace, actual.Namespace);

        Assert.Equal(expected.GenericConstraintTypes.Length, actual.GenericConstraintTypes.Length);

        foreach (var expectedGenericConstraintType in expected.GenericConstraintTypes)
        {
            Assert.Contains(expectedGenericConstraintType, actual.GenericConstraintTypes,
                StringComparer.Ordinal);
        }
    }

    private static RegisterMethod EmptyRegisterMethodInfo => new()
    {
        Namespace = "TestMod.Registries",
        ClassName = "TestRegistry",
        MethodName = "RegisterTest",
        RegistryPhase = 2,
        CategoryId = "test",
    };
}