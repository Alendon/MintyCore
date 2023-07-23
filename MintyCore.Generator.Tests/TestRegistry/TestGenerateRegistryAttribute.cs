using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using MintyCore.Generator.Registry;

namespace MintyCore.Generator.Tests.TestRegistry;

//TODO fix generic registry to set full interface/baseclass name in attribute.
//TODO Also add a diagnostic that only a single base/interface is allowed

public class TestGenerateRegistryAttribute
{
    [Fact]
    public void GenerateAttribute_GenericRegistryNoConstraints_ShouldGenerateCorrectCode()
    {
        string testCode = """
using MintyCore.Modding;
using MintyCore.Modding.Attributes;
using MintyCore.Utils;

namespace TestMod.Registries;

[Registry("test")]
public class TestRegistry : IRegistry
{
    [RegisterMethod(ObjectRegistryPhase.Main)]
    public static void RegistryType<GenericType>(Identification id)
    {
    }
}
""";

        ISourceGenerator generator = new RegistryGenerator();
        Compile(generator, out var compilation, out var diagnostics, out var generatedTrees, testCode,
            RegistryBaseCode, ModInterface, TestMod, Identification);

        Assert.Empty(diagnostics);
        Assert.Equal(3, generatedTrees.Length);
        var attributeTree =
            generatedTrees.FirstOrDefault(x => x.FilePath.EndsWith("TestMod_Registries_TestRegistry_Att.g.cs"));
        var regIdsTree = generatedTrees.FirstOrDefault(x => x.FilePath.EndsWith("TestMod.Test.reg.g.cs"));
        var modExtensionTree = generatedTrees.FirstOrDefault(x => x.FilePath.EndsWith("TestMod.Test.g.cs"));

        Assert.NotNull(attributeTree);
        Assert.NotNull(regIdsTree);
        Assert.NotNull(modExtensionTree);

        var expectedAttributeCode = """
using System;
using JetBrains.Annotations;

#nullable enable
namespace TestMod.Registries;
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false)]
[MeansImplicitUse]
public class RegistryTypeAttribute : MintyCore.Modding.Attributes.RegisterBaseAttribute
{
    public RegistryTypeAttribute(string id)
    {
    }

    public const string ClassName = "TestMod.Registries.TestRegistry";
    public const string MethodName = "RegistryType";
    public const int RegisterType = 2;
    public const string? ResourceSubFolder = "null";
    public const bool HasFile = false;
    public const bool UseExistingId = false;
    public const int GenericConstraints = 0;
    public const string GenericTypeConstraints = "";
    public const int RegistryPhase = 2;
    public const string PropertyType = "";
    public const string CategoryId = "test";
}
""";
        var expectedRegIdsCode = """
using System;

#nullable enable
namespace TestMod.Identifications;
public static partial class RegistryIDs
{
    public static ushort Test { get; private set; }

    internal static void Register()
    {
        var modId = TestMod.Test.Instance!.ModId;
        Test = MintyCore.Modding.RegistryManager.AddRegistry<TestMod.Registries.TestRegistry>(modId, "test", null);
    }
}
""";
        var expectedModExtensionCode = """
using System;

namespace TestMod;
public partial class Test
{
    internal static void InternalRegister()
    {
        TestMod.Identifications.RegistryIDs.Register();
    }

    internal static void InternalUnregister()
    {
    }
}
""";

        var expectedAttributeTree = CSharpSyntaxTree.ParseText(expectedAttributeCode);
        var expectedRegIdsTree = CSharpSyntaxTree.ParseText(expectedRegIdsCode);
        var expectedModExtensionTree = CSharpSyntaxTree.ParseText(expectedModExtensionCode);

        Assert.True(expectedAttributeTree.IsEquivalentTo(attributeTree));
        Assert.True(expectedRegIdsTree.IsEquivalentTo(regIdsTree));
        Assert.True(expectedModExtensionTree.IsEquivalentTo(modExtensionTree));
    }

    [Fact]
    public void GenerateAttribute_GenericRegistryWithNewConstraint_ShouldGenerateCorrectCode()
    {
        string testCode = """
using MintyCore.Modding;
using MintyCore.Modding.Attributes;
using MintyCore.Utils;

namespace TestMod.Registries;

[Registry("test")]
public class TestRegistry : IRegistry
{
    [RegisterMethod(ObjectRegistryPhase.Main)]
    public static void RegistryType<GenericType>(Identification id) where GenericType : new()
    {
    }
}
""";

        ISourceGenerator generator = new RegistryGenerator();
        Compile(generator, out var compilation, out var diagnostics, out var generatedTrees, testCode,
            RegistryBaseCode, ModInterface, TestMod, Identification);

        Assert.Empty(diagnostics);
        Assert.Equal(3, generatedTrees.Length);
        var attributeTree =
            generatedTrees.FirstOrDefault(x => x.FilePath.EndsWith("TestMod_Registries_TestRegistry_Att.g.cs"));
        var regIdsTree = generatedTrees.FirstOrDefault(x => x.FilePath.EndsWith("TestMod.Test.reg.g.cs"));
        var modExtensionTree = generatedTrees.FirstOrDefault(x => x.FilePath.EndsWith("TestMod.Test.g.cs"));

        Assert.NotNull(attributeTree);
        Assert.NotNull(regIdsTree);
        Assert.NotNull(modExtensionTree);

        var expectedAttributeCode = """
using System;
using JetBrains.Annotations;

#nullable enable
namespace TestMod.Registries;
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false)]
[MeansImplicitUse]
public class RegistryTypeAttribute : MintyCore.Modding.Attributes.RegisterBaseAttribute
{
    public RegistryTypeAttribute(string id)
    {
    }

    public const string ClassName = "TestMod.Registries.TestRegistry";
    public const string MethodName = "RegistryType";
    public const int RegisterType = 2;
    public const string? ResourceSubFolder = "null";
    public const bool HasFile = false;
    public const bool UseExistingId = false;
    public const int GenericConstraints = 1;
    public const string GenericTypeConstraints = "";
    public const int RegistryPhase = 2;
    public const string PropertyType = "";
    public const string CategoryId = "test";
}
""";
        var expectedRegIdsCode = """
using System;

#nullable enable
namespace TestMod.Identifications;
public static partial class RegistryIDs
{
    public static ushort Test { get; private set; }

    internal static void Register()
    {
        var modId = TestMod.Test.Instance!.ModId;
        Test = MintyCore.Modding.RegistryManager.AddRegistry<TestMod.Registries.TestRegistry>(modId, "test", null);
    }
}
""";
        var expectedModExtensionCode = """
using System;

namespace TestMod;
public partial class Test
{
    internal static void InternalRegister()
    {
        TestMod.Identifications.RegistryIDs.Register();
    }

    internal static void InternalUnregister()
    {
    }
}
""";

        var expectedAttributeTree = CSharpSyntaxTree.ParseText(expectedAttributeCode);
        var expectedRegIdsTree = CSharpSyntaxTree.ParseText(expectedRegIdsCode);
        var expectedModExtensionTree = CSharpSyntaxTree.ParseText(expectedModExtensionCode);

        Assert.True(expectedAttributeTree.IsEquivalentTo(attributeTree));
        Assert.True(expectedRegIdsTree.IsEquivalentTo(regIdsTree));
        Assert.True(expectedModExtensionTree.IsEquivalentTo(modExtensionTree));
    }

    [Fact]
    public void GenerateAttribute_GenericRegistryWithUnmanagedConstraint_ShouldGenerateCorrectCode()
    {
        string testCode = """
using MintyCore.Modding;
using MintyCore.Modding.Attributes;
using MintyCore.Utils;

namespace TestMod.Registries;

[Registry("test")]
public class TestRegistry : IRegistry
{
    [RegisterMethod(ObjectRegistryPhase.Main)]
    public static void RegistryType<GenericType>(Identification id) where GenericType : unmanaged
    {
    }
}
""";

        ISourceGenerator generator = new RegistryGenerator();
        Compile(generator, out var compilation, out var diagnostics, out var generatedTrees, testCode,
            RegistryBaseCode, ModInterface, TestMod, Identification);

        Assert.Empty(diagnostics);
        Assert.Equal(3, generatedTrees.Length);
        var attributeTree =
            generatedTrees.FirstOrDefault(x => x.FilePath.EndsWith("TestMod_Registries_TestRegistry_Att.g.cs"));
        var regIdsTree = generatedTrees.FirstOrDefault(x => x.FilePath.EndsWith("TestMod.Test.reg.g.cs"));
        var modExtensionTree = generatedTrees.FirstOrDefault(x => x.FilePath.EndsWith("TestMod.Test.g.cs"));

        Assert.NotNull(attributeTree);
        Assert.NotNull(regIdsTree);
        Assert.NotNull(modExtensionTree);

        var expectedAttributeCode = """
using System;
using JetBrains.Annotations;

#nullable enable
namespace TestMod.Registries;
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false)]
[MeansImplicitUse]
public class RegistryTypeAttribute : MintyCore.Modding.Attributes.RegisterBaseAttribute
{
    public RegistryTypeAttribute(string id)
    {
    }

    public const string ClassName = "TestMod.Registries.TestRegistry";
    public const string MethodName = "RegistryType";
    public const int RegisterType = 2;
    public const string? ResourceSubFolder = "null";
    public const bool HasFile = false;
    public const bool UseExistingId = false;
    public const int GenericConstraints = 24;
    public const string GenericTypeConstraints = "";
    public const int RegistryPhase = 2;
    public const string PropertyType = "";
    public const string CategoryId = "test";
}
""";
        var expectedRegIdsCode = """
using System;

#nullable enable
namespace TestMod.Identifications;
public static partial class RegistryIDs
{
    public static ushort Test { get; private set; }

    internal static void Register()
    {
        var modId = TestMod.Test.Instance!.ModId;
        Test = MintyCore.Modding.RegistryManager.AddRegistry<TestMod.Registries.TestRegistry>(modId, "test", null);
    }
}
""";
        var expectedModExtensionCode = """
using System;

namespace TestMod;
public partial class Test
{
    internal static void InternalRegister()
    {
        TestMod.Identifications.RegistryIDs.Register();
    }

    internal static void InternalUnregister()
    {
    }
}
""";
        var expectedAttributeTree = CSharpSyntaxTree.ParseText(expectedAttributeCode);
        var expectedRegIdsTree = CSharpSyntaxTree.ParseText(expectedRegIdsCode);
        var expectedModExtensionTree = CSharpSyntaxTree.ParseText(expectedModExtensionCode);

        Assert.True(expectedAttributeTree.IsEquivalentTo(attributeTree));
        Assert.True(expectedRegIdsTree.IsEquivalentTo(regIdsTree));
        Assert.True(expectedModExtensionTree.IsEquivalentTo(modExtensionTree));
    }

    [Fact]
    public void GenerateAttribute_GenericRegistryWithInterfaceConstraint_ShouldGenerateCorrectCode()
    {
        string testCode = """
using MintyCore.Modding;
using MintyCore.Modding.Attributes;
using MintyCore.Utils;

namespace TestMod.Registries;

[Registry("test")]
public class TestRegistry : IRegistry
{
    [RegisterMethod(ObjectRegistryPhase.Main)]
    public static void RegistryType<GenericType>(Identification id) where GenericType : ITestInterface
    {
    }
}

public Interface ITestInterface{}
""";

        ISourceGenerator generator = new RegistryGenerator();
        Compile(generator, out var compilation, out var diagnostics, out var generatedTrees, testCode,
            RegistryBaseCode, ModInterface, TestMod, Identification);

        Assert.Empty(diagnostics);
        Assert.Equal(3, generatedTrees.Length);
        var attributeTree =
            generatedTrees.FirstOrDefault(x => x.FilePath.EndsWith("TestMod_Registries_TestRegistry_Att.g.cs"));
        var regIdsTree = generatedTrees.FirstOrDefault(x => x.FilePath.EndsWith("TestMod.Test.reg.g.cs"));
        var modExtensionTree = generatedTrees.FirstOrDefault(x => x.FilePath.EndsWith("TestMod.Test.g.cs"));

        Assert.NotNull(attributeTree);
        Assert.NotNull(regIdsTree);
        Assert.NotNull(modExtensionTree);

        var expectedAttributeCode = """
using System;
using JetBrains.Annotations;

#nullable enable
namespace TestMod.Registries;
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false)]
[MeansImplicitUse]
public class RegistryTypeAttribute : MintyCore.Modding.Attributes.RegisterBaseAttribute
{
    public RegistryTypeAttribute(string id)
    {
    }

    public const string ClassName = "TestMod.Registries.TestRegistry";
    public const string MethodName = "RegistryType";
    public const int RegisterType = 2;
    public const string? ResourceSubFolder = "null";
    public const bool HasFile = false;
    public const bool UseExistingId = false;
    public const int GenericConstraints = 0;
    public const string GenericTypeConstraints = "ITestInterface";
    public const int RegistryPhase = 2;
    public const string PropertyType = "";
    public const string CategoryId = "test";
}
""";
        var expectedRegIdsCode = """
using System;

#nullable enable
namespace TestMod.Identifications;
public static partial class RegistryIDs
{
    public static ushort Test { get; private set; }

    internal static void Register()
    {
        var modId = TestMod.Test.Instance!.ModId;
        Test = MintyCore.Modding.RegistryManager.AddRegistry<TestMod.Registries.TestRegistry>(modId, "test", null);
    }
}
""";
        var expectedModExtensionCode = """
using System;

namespace TestMod;
public partial class Test
{
    internal static void InternalRegister()
    {
        TestMod.Identifications.RegistryIDs.Register();
    }

    internal static void InternalUnregister()
    {
    }
}
""";
        var expectedAttributeTree = CSharpSyntaxTree.ParseText(expectedAttributeCode);
        var expectedRegIdsTree = CSharpSyntaxTree.ParseText(expectedRegIdsCode);
        var expectedModExtensionTree = CSharpSyntaxTree.ParseText(expectedModExtensionCode);

        Assert.True(expectedAttributeTree.IsEquivalentTo(attributeTree));
        Assert.True(expectedRegIdsTree.IsEquivalentTo(regIdsTree));
        Assert.True(expectedModExtensionTree.IsEquivalentTo(modExtensionTree));
    }

    [Fact]
    public void GenerateAttribute_PropertyRegistry_ShouldGenerateCorrectCode()
    {
        string testCode = """
using MintyCore.Modding;
using MintyCore.Modding.Attributes;
using MintyCore.Utils;

namespace TestMod.Registries;

[Registry("test")]
public class TestRegistry : IRegistry
{
    [RegisterMethod(ObjectRegistryPhase.Main)]
    public static void RegistryData(Identification id, PropertyData data)
    {
    }
}

public struct PropertyData{}

""";

        ISourceGenerator generator = new RegistryGenerator();
        Compile(generator, out var compilation, out var diagnostics, out var generatedTrees, testCode,
            RegistryBaseCode, ModInterface, TestMod, Identification);

        Assert.Empty(diagnostics);
        Assert.Equal(3, generatedTrees.Length);
        var attributeTree =
            generatedTrees.FirstOrDefault(x => x.FilePath.EndsWith("TestMod_Registries_TestRegistry_Att.g.cs"));
        var regIdsTree = generatedTrees.FirstOrDefault(x => x.FilePath.EndsWith("TestMod.Test.reg.g.cs"));
        var modExtensionTree = generatedTrees.FirstOrDefault(x => x.FilePath.EndsWith("TestMod.Test.g.cs"));

        Assert.NotNull(attributeTree);
        Assert.NotNull(regIdsTree);
        Assert.NotNull(modExtensionTree);

        var expectedAttributeCode = """
using System;
using JetBrains.Annotations;

#nullable enable
namespace TestMod.Registries;
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
[MeansImplicitUse]
public class RegistryDataAttribute : MintyCore.Modding.Attributes.RegisterBaseAttribute
{
    public RegistryDataAttribute(string id)
    {
    }

    public const string ClassName = "TestMod.Registries.TestRegistry";
    public const string MethodName = "RegistryData";
    public const int RegisterType = 1;
    public const string? ResourceSubFolder = "null";
    public const bool HasFile = false;
    public const bool UseExistingId = false;
    public const int GenericConstraints = 0;
    public const string GenericTypeConstraints = "";
    public const int RegistryPhase = 2;
    public const string PropertyType = "TestMod.Registries.PropertyData";
    public const string CategoryId = "test";
}
""";
        var expectedRegIdsCode = """
using System;

#nullable enable
namespace TestMod.Identifications;
public static partial class RegistryIDs
{
    public static ushort Test { get; private set; }

    internal static void Register()
    {
        var modId = TestMod.Test.Instance!.ModId;
        Test = MintyCore.Modding.RegistryManager.AddRegistry<TestMod.Registries.TestRegistry>(modId, "test", null);
    }
}
""";
        var expectedModExtensionCode = """
using System;

namespace TestMod;
public partial class Test
{
    internal static void InternalRegister()
    {
        TestMod.Identifications.RegistryIDs.Register();
    }

    internal static void InternalUnregister()
    {
    }
}
""";

        var expectedAttributeTree = CSharpSyntaxTree.ParseText(expectedAttributeCode);
        var expectedRegIdsTree = CSharpSyntaxTree.ParseText(expectedRegIdsCode);
        var expectedModExtensionTree = CSharpSyntaxTree.ParseText(expectedModExtensionCode);
        
        Assert.True(expectedAttributeTree.IsEquivalentTo(attributeTree));
        Assert.True(expectedRegIdsTree.IsEquivalentTo(regIdsTree));
        Assert.True(expectedModExtensionTree.IsEquivalentTo(modExtensionTree));
    }

    [Fact]
    public void GenerateAttribute_InvalidRegistry_ShouldGenerateError()
    {
        string testCode = """
using MintyCore.Modding;
using MintyCore.Modding.Attributes;
using MintyCore.Utils;

namespace TestMod.Registries;

[Registry("test")]
public class TestRegistry : IRegistry
{
    [RegisterMethod(ObjectRegistryPhase.Main)]
    public static void RegistryData(Identification id)
    {
    }
}
""";

        ISourceGenerator generator = new RegistryGenerator();
        Compile(generator, out var compilation, out var diagnostics, out var generatedTrees, testCode,
            RegistryBaseCode, ModInterface, TestMod, Identification);

        Assert.Equal(2, diagnostics.Length);

        Assert.Contains(diagnostics, x => x.Id == "MC1101");
        Assert.Contains(diagnostics, x => x.Id == "MC1202");
    }

    [Fact]
    public void GenerateAttribute_NoRegistryMethod_ShouldGenerateWarning()
    {
        string testCode = """
using MintyCore.Modding;
using MintyCore.Modding.Attributes;
using MintyCore.Utils;

namespace TestMod.Registries;

[Registry("test")]
public class TestRegistry : IRegistry
{

}
""";

        ISourceGenerator generator = new RegistryGenerator();
        Compile(generator, out var compilation, out var diagnostics, out var generatedTrees, testCode,
            RegistryBaseCode, ModInterface, TestMod, Identification);

        Assert.Single(diagnostics);

        Assert.Contains(diagnostics, x => x.Id == "MC1101");
    }
}