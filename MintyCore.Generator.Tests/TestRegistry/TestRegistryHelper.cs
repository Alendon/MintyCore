using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using MintyCore.Generator.Registry;
using Moq;
using Scriban;
using SharedCode;

namespace MintyCore.Generator.Tests.TestRegistry;

public class TestRegistryHelper
{
    private const string TestTemplateDir = "MintyCore.Generator.Tests.TestRegistry.Templates";

    private static Template InfoTemplate =>
        Template.Parse(EmbeddedFileHelper.ReadEmbeddedTextFileOrThrow($"{TestTemplateDir}.RegistryInfo.sbncs"));

    private static Template AttributeTemplate =>
        Template.Parse(
            EmbeddedFileHelper.ReadEmbeddedTextFileOrThrow($"{TestTemplateDir}.Input_RegistryAttribute.sbncs"));

    [Fact]
    public void GetModInfo_ValidModSymbol_ShouldReturnValidModInfo()
    {
        var compilation = CreateCompilation(ModInterface, TestMod);
        var symbol = compilation.GetTypeByMetadataName("TestMod.Test");

        var expected = new ModInfo { Namespace = "TestMod", ClassName = "Test" };

        var actual = RegistryHelper.GetModInfo(symbol);

        Assert.NotNull(actual);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void GetModInfo_InvalidModSymbol_ShouldReturnNull()
    {
        string invalidMod = """
                            namespace TestMod
                            {
                                public class Test
                                {
                                }
                            }
                            """;
        var compilation = CreateCompilation(ModInterface, invalidMod);
        var symbol = compilation.GetSymbolsWithName("Test", SymbolFilter.Type).First();

        var result = RegistryHelper.GetModInfo(symbol);

        Assert.Null(result);
    }

    [Fact]
    public void ExtractRegisterMethodInfoFromSymbol_GenericMethodInfo_ShouldReturnValidMethodInfo()
    {
        var expectedRegisterMethod = EmptyRegisterMethodInfo with
        {
            RegisterType = RegisterMethodType.Generic,
            Constraints = GenericConstraints.ValueType | GenericConstraints.UnmanagedType,
            GenericConstraintTypes = new[] { "global::System.IDisposable", "global::System.IEquatable<string>" },
            RegistryPhase = 2,
        };

        var registerInfoSource = InfoTemplate.Render(expectedRegisterMethod, x => x.Name);
        var compilation = CreateCompilation(registerInfoSource, RegistryBaseCode, ModInterface, Identification);

        var registerInfoSymbol = compilation.GetTypeByMetadataName("TestMod.Registries.TestRegistry_RegisterTest");

        var actualRegisterMethod =
            RegistryHelper.ExtractRegisterMethodInfoFromSymbol(registerInfoSymbol!, CancellationToken.None);

        AssertRegisterMethodEquality(expectedRegisterMethod, actualRegisterMethod);
    }

    [Fact]
    public void ExtractRegisterMethodInfoFromSymbol_PropertyMethodInfo_ShouldReturnValidMethodInfo()
    {
        var expectedRegisterMethod = EmptyRegisterMethodInfo with
        {
            RegisterType = RegisterMethodType.Invocation,
            RegistryPhase = 2,
            HasFile = true,
            InvocationReturnType = "global::System.IDisposable"
        };

        var registerInfoSource = InfoTemplate.Render(expectedRegisterMethod, x => x.Name);
        var compilation = CreateCompilation(registerInfoSource, RegistryBaseCode, ModInterface, Identification);

        var registerInfoSymbol = compilation.GetTypeByMetadataName("TestMod.Registries.TestRegistry_RegisterTest");

        var actualRegisterMethod =
            RegistryHelper.ExtractRegisterMethodInfoFromSymbol(registerInfoSymbol!, CancellationToken.None);

        AssertRegisterMethodEquality(expectedRegisterMethod, actualRegisterMethod);
    }

    [Fact]
    public void ExtractRegisterMethodInfoFromSymbol_FileMethodInfo_ShouldReturnValidMethodInfo()
    {
        var expectedRegisterMethod = EmptyRegisterMethodInfo with
        {
            RegisterType = RegisterMethodType.Invocation,
            RegistryPhase = 2,
            HasFile = true,
            ResourceSubFolder = "test"
        };

        var registerInfoSource = InfoTemplate.Render(expectedRegisterMethod, x => x.Name);
        var compilation = CreateCompilation(registerInfoSource, RegistryBaseCode, ModInterface, Identification);

        var registerInfoSymbol = compilation.GetTypeByMetadataName("TestMod.Registries.TestRegistry_RegisterTest");

        var actualRegisterMethod =
            RegistryHelper.ExtractRegisterMethodInfoFromSymbol(registerInfoSymbol!, CancellationToken.None);

        AssertRegisterMethodEquality(expectedRegisterMethod, actualRegisterMethod);
    }

    [Fact]
    public void ExtractFileRegisterObjects_EmptyJson_ShouldReturnEmptyEnumerable()
    {
        var compilation = CreateCompilation(RegistryBaseCode, ModInterface, Identification, TestMod);

        var result = RegistryHelper.ExtractFileRegisterObjects(
            ((ImmutableArray<AdditionalText>.Empty, compilation), ImmutableArray<RegisterMethodInfo>.Empty),
            CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public void ExtractFileRegisterObjects_SingleElementNewRegistry_ShouldReturnSingleRegisterObject()
    {
        var newRegisterMethod = EmptyRegisterMethodInfo with
        {
            RegisterType = RegisterMethodType.File,
            HasFile = true
        };

        var registryJson = """
                            [
                            {
                                RegisterMethodInfo: "TestMod.Registries.TestRegistry_RegisterTest",
                                Entries: [
                                    {
                                        File: "test.cfg",
                                        Id: "test"
                                    }
                                ]
                            }
                            ]

                           """;
        var extraFiles = ImmutableArray.Create(CreateAdditionalText(registryJson, "test.json"));
        var newRegisterMethodInfos = ImmutableArray.Create(newRegisterMethod);

        var compilation = CreateCompilation(RegistryBaseCode, ModInterface, Identification, TestMod);

        var result = RegistryHelper.ExtractFileRegisterObjects(
            ((extraFiles, compilation), newRegisterMethodInfos),
            CancellationToken.None);

        var registryObject = Assert.Single(result);
        AssertRegisterMethodEquality(newRegisterMethod, registryObject.RegisterMethodInfo);
        Assert.Equal("test.cfg", registryObject.File);
        Assert.Equal("test", registryObject.Id);
    }

    [Fact]
    public void ExtractFileRegisterObjects_MultipleElementsNewRegistry_ShouldReturnMultipleRegisterObjects()
    {
        var newRegisterMethod = EmptyRegisterMethodInfo with
        {
            RegisterType = RegisterMethodType.File,
            HasFile = true
        };

        var registryJson = """
                            [
                            {
                                RegisterMethodInfo: "TestMod.Registries.TestRegistry_RegisterTest",
                                Entries: [
                                    {
                                        File: "test.cfg",
                                        Id: "test"
                                    },
                                    {
                                        File: "test2.cfg",
                                        Id: "test2"
                                    },
                                    {
                                        File: "test3.cfg",
                                        Id: "test3"
                                    }
                                ]
                            }
                            ]

                           """;
        var extraFiles = ImmutableArray.Create(CreateAdditionalText(registryJson, "test.json"));
        var newRegisterMethodInfos = ImmutableArray.Create(newRegisterMethod);

        var compilation = CreateCompilation(RegistryBaseCode, ModInterface, Identification, TestMod);

        var result = RegistryHelper.ExtractFileRegisterObjects(
            ((extraFiles, compilation), newRegisterMethodInfos),
            CancellationToken.None).ToArray();

        Assert.Equal(3, result.Length);

        AssertRegisterMethodEquality(newRegisterMethod, result[0].RegisterMethodInfo);
        Assert.Equal("test.cfg", result[0].File);
        Assert.Equal("test", result[0].Id);

        AssertRegisterMethodEquality(newRegisterMethod, result[1].RegisterMethodInfo);
        Assert.Equal("test2.cfg", result[1].File);
        Assert.Equal("test2", result[1].Id);

        AssertRegisterMethodEquality(newRegisterMethod, result[2].RegisterMethodInfo);
        Assert.Equal("test3.cfg", result[2].File);
        Assert.Equal("test3", result[2].Id);
    }

    [Fact]
    public void ExtractFileRegisterObjects_SingleElementExistingRegistry_ShouldReturnSingleRegisterObject()
    {
        var registerMethod = EmptyRegisterMethodInfo with
        {
            RegisterType = RegisterMethodType.File,
            HasFile = true,
        };

        var registryInfo = InfoTemplate.Render(registerMethod, x => x.Name);

        var registryJson = """
                            [
                            {
                                RegisterMethodInfo: "TestMod.Registries.TestRegistry_RegisterTest",
                                Entries: [
                                    {
                                        File: "test.cfg",
                                        Id: "test"
                                    }
                                ]
                            }
                            ]

                           """;
        var extraFiles = ImmutableArray.Create(CreateAdditionalText(registryJson, "test.json"));

        var compilation = CreateCompilation(RegistryBaseCode, ModInterface, Identification, TestMod, registryInfo);

        var result = RegistryHelper.ExtractFileRegisterObjects(
            ((extraFiles, compilation), ImmutableArray<RegisterMethodInfo>.Empty),
            CancellationToken.None);

        var registryObject = Assert.Single(result);
        AssertRegisterMethodEquality(registerMethod, registryObject.RegisterMethodInfo);
        Assert.Equal("test.cfg", registryObject.File);
        Assert.Equal("test", registryObject.Id);
    }

    [Fact]
    public void ExtractFileRegisterObjects_MultipleElementsExistingRegistry_ShouldReturnMultipleRegisterObjects()
    {
        var newRegisterMethod = EmptyRegisterMethodInfo with
        {
            RegisterType = RegisterMethodType.File,
            HasFile = true
        };

        var registryJson = """
                            [
                            {
                                RegisterMethodInfo: "TestMod.Registries.TestRegistry_RegisterTest",
                                Entries: [
                                    {
                                        File: "test.cfg",
                                        Id: "test"
                                    },
                                    {
                                        File: "test2.cfg",
                                        Id: "test2"
                                    },
                                    {
                                        File: "test3.cfg",
                                        Id: "test3"
                                    }
                                ]
                            }
                            ]

                           """;
        var extraFiles = ImmutableArray.Create(CreateAdditionalText(registryJson, "test.json"));
        var registryInfo = InfoTemplate.Render(newRegisterMethod, x => x.Name);

        var compilation = CreateCompilation(RegistryBaseCode, ModInterface, Identification, TestMod, registryInfo);

        var result = RegistryHelper.ExtractFileRegisterObjects(
            ((extraFiles, compilation), ImmutableArray<RegisterMethodInfo>.Empty),
            CancellationToken.None).ToArray();

        Assert.Equal(3, result.Length);

        AssertRegisterMethodEquality(newRegisterMethod, result[0].RegisterMethodInfo);
        Assert.Equal("test.cfg", result[0].File);
        Assert.Equal("test", result[0].Id);

        AssertRegisterMethodEquality(newRegisterMethod, result[1].RegisterMethodInfo);
        Assert.Equal("test2.cfg", result[1].File);
        Assert.Equal("test2", result[1].Id);

        AssertRegisterMethodEquality(newRegisterMethod, result[2].RegisterMethodInfo);
        Assert.Equal("test3.cfg", result[2].File);
        Assert.Equal("test3", result[2].Id);
    }

    [Fact]
    public void CheckValidConstraints_BaseType_ShouldReturnTrue()
    {
        string symbolSource = @"""
                                public class TestClass {}

                                namespace TestMod
                                {
                                    public class Test : TestClass
                                    {
                                    }
                                }
                                """;

        var compilation = CreateCompilation(symbolSource);

        var symbol = compilation.GetTypeByMetadataName("TestMod.Test")!;

        Assert.True(RegistryHelper.CheckValidConstraint(GenericConstraints.None, new[] { "TestClass" }, symbol));
    }
    
    [Fact]
    public void CheckValidConstraints_BaseType_ShouldReturnFalse()
    {
        string symbolSource = @"""
                                public class TestClass {}

                                namespace TestMod
                                {
                                    public class Test
                                    {
                                    }
                                }
                                """;

        var compilation = CreateCompilation(symbolSource);

        var symbol = compilation.GetTypeByMetadataName("TestMod.Test")!;

        Assert.False(RegistryHelper.CheckValidConstraint(GenericConstraints.None, new[] { "TestClass" }, symbol));
    }
    
    [Fact]
    public void CheckValidConstraints_NestedBaseType_ShouldReturnTrue()
    {
        string symbolSource = @"""
                                public class TestClass {}
                                
                                public class Nested : TestClass {}

                                namespace TestMod
                                {
                                    public class Test : Nested
                                    {
                                    }
                                }
                                """;

        var compilation = CreateCompilation(symbolSource);

        var symbol = compilation.GetTypeByMetadataName("TestMod.Test")!;

        Assert.True(RegistryHelper.CheckValidConstraint(GenericConstraints.None, new[] { "TestClass" }, symbol));
    }
    
    [Fact]
    public void CheckValidConstraints_Interface_ShouldReturnTrue()
    {
        string symbolSource = @"""
                                public interface ITest {}

                                namespace TestMod
                                {
                                    public class Test : ITest
                                    {
                                    }
                                }
                                """;

        var compilation = CreateCompilation(symbolSource);

        var symbol = compilation.GetTypeByMetadataName("TestMod.Test")!;

        Assert.True(RegistryHelper.CheckValidConstraint(GenericConstraints.None, new[] { "ITest" }, symbol));
    }
    
    [Fact]
    public void CheckValidConstraints_Interface_ShouldReturnFalse()
    {
        string symbolSource = @"""
                                public interface ITest {}

                                namespace TestMod
                                {
                                    public class Test
                                    {
                                    }
                                }
                                """;

        var compilation = CreateCompilation(symbolSource);

        var symbol = compilation.GetTypeByMetadataName("TestMod.Test")!;

        Assert.False(RegistryHelper.CheckValidConstraint(GenericConstraints.None, new[] { "ITest" }, symbol));
    }
    
    [Fact]
    public void CheckValidConstraints_NestedInterface_ShouldReturnTrue()
    {
        string symbolSource = @"""
                                public interface ITest {}
                                
                                public class Nested : ITest {}

                                namespace TestMod
                                {
                                    public class Test : Nested
                                    {
                                    }
                                }
                                """;

        var compilation = CreateCompilation(symbolSource);

        var symbol = compilation.GetTypeByMetadataName("TestMod.Test")!;

        Assert.True(RegistryHelper.CheckValidConstraint(GenericConstraints.None, new[] { "ITest" }, symbol));
    }

    [Fact]
    public void CheckValidConstraints_RefType_ShouldReturnTrue()
    {
        string symbolSource = @"""                              
                                namespace TestMod
                                {
                                    public class Test
                                    {
                                    }
                                }
                                """;
        var compilation = CreateCompilation(symbolSource);
        var symbol = compilation.GetTypeByMetadataName("TestMod.Test")!;

        Assert.True(RegistryHelper.CheckValidConstraint(GenericConstraints.ReferenceType, Array.Empty<string>(), symbol));
    }
    
    [Fact]
    public void CheckValidConstraints_RefType_ShouldReturnFalse()
    {
        string symbolSource = @"""                              
                                namespace TestMod
                                {
                                    public struct Test
                                    {
                                    }
                                }
                                """;
        var compilation = CreateCompilation(symbolSource);
        var symbol = compilation.GetTypeByMetadataName("TestMod.Test")!;

        Assert.False(RegistryHelper.CheckValidConstraint(GenericConstraints.ReferenceType, Array.Empty<string>(), symbol));
    }
    
    [Fact]
    public void CheckValidConstraints_UnmanagedType_ShouldReturnTrue()
    {
        string symbolSource = @"""                              
                                namespace TestMod
                                {
                                    public struct Test
                                    {
                                        public byte Data;
                                    }
                                }
                                """;
        var compilation = CreateCompilation(symbolSource);
        var symbol = compilation.GetTypeByMetadataName("TestMod.Test")!;

        Assert.True(RegistryHelper.CheckValidConstraint(GenericConstraints.ValueType | GenericConstraints.UnmanagedType, Array.Empty<string>(), symbol));
    }
    
    [Fact]
    public void CheckValidConstraints_UnmanagedType_ShouldReturnFalse()
    {
        string symbolSource = @"""                              
                                namespace TestMod
                                {
                                    public class Test
                                    {
                                    }
                                }
                                """;
        var compilation = CreateCompilation(symbolSource);
        var symbol = compilation.GetTypeByMetadataName("TestMod.Test")!;

        Assert.False(RegistryHelper.CheckValidConstraint(GenericConstraints.ValueType | GenericConstraints.UnmanagedType, Array.Empty<string>(), symbol));
    }
    
    

    private static AdditionalText CreateAdditionalText(string text, string path)
    {
        var additionalText = new Mock<AdditionalText>();
        additionalText.Setup(x => x.GetText(It.IsAny<CancellationToken>())).Returns(SourceText.From(text));
        additionalText.Setup(x => x.Path).Returns(path);
        return additionalText.Object;
    }

    private static RegisterMethodInfo EmptyRegisterMethodInfo => new()
    {
        Namespace = "TestMod.Registries",
        ClassName = "TestRegistry",
        MethodName = "RegisterTest",
        RegistryPhase = 2,
        CategoryId = "test",
    };

    private static void AssertRegisterObjectEquality(RegisterObject expected, RegisterObject result)
    {
        AssertRegisterMethodEquality(expected.RegisterMethodInfo, result.RegisterMethodInfo);
        Assert.Equal(expected.RegisterType, result.RegisterType);
        Assert.Equal(expected.Id, result.Id);
        Assert.Equal(expected.File, result.File);
        Assert.Equal(expected.RegisterProperty, result.RegisterProperty);
    }

    private static void AssertRegisterMethodEquality(RegisterMethodInfo expected, RegisterMethodInfo actual)
    {
        Assert.Equal(expected.RegisterType, actual.RegisterType);
        Assert.Equal(expected.MethodName, actual.MethodName);
        Assert.Equal(expected.ClassName, actual.ClassName);
        Assert.Equal(expected.RegistryPhase, actual.RegistryPhase);
        Assert.Equal(expected.HasFile, actual.HasFile);
        Assert.Equal(expected.Constraints, actual.Constraints);
        Assert.Equal(expected.InvocationReturnType, actual.InvocationReturnType);
        Assert.Equal(expected.CategoryId, actual.CategoryId);
        Assert.Equal(expected.ResourceSubFolder, actual.ResourceSubFolder);
        Assert.Equal(expected.Namespace, actual.Namespace);

        Assert.True(expected.GenericConstraintTypes.SequenceEqual(actual.GenericConstraintTypes,
            StringComparer.InvariantCulture));
    }
}