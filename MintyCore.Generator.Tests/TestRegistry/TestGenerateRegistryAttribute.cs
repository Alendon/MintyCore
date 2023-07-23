using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using MintyCore.Generator.Registry;
using Scriban;
using Scriban.Parsing;
using SharedCode;

namespace MintyCore.Generator.Tests.TestRegistry;

//TODO fix generic registry to set full interface/baseclass name in attribute.
//TODO Also add a diagnostic that only a single base/interface is allowed

public class TestGenerateRegistryAttribute
{
    private const string TestTemplateDir = "MintyCore.Generator.Tests.TestRegistry.TestTemplates_GenerateAttribute";

    [Fact]
    public void GenerateAttribute_GenericRegistryNoConstraints_ShouldGenerateCorrectCode()
    {
        var testCode =
            EmbeddedFileHelper.ReadEmbeddedTextFileOrThrow($"{TestTemplateDir}.Input_GenerateAttribute_Generic_NoConstraints.sbncs");

        var attributeTemplateString =
            EmbeddedFileHelper.ReadEmbeddedTextFileOrThrow($"{TestTemplateDir}.Output_RegistryAttribute.sbncs");

        var attributeTemplate = Template.Parse(attributeTemplateString);
        var expectedAttributeCode = attributeTemplate.Render(new Dictionary<string, object>
        {
            { "RegisterMethodName", "RegisterType" },
            { "RegisterType", (int)RegisterMethodType.Generic },
            { "RegistryPhase", 2 },
            { "AttributeTarget", "AttributeTargets.Class | AttributeTargets.Struct"}
        });

        var expectedRegIdsTemplateString =
            EmbeddedFileHelper.ReadEmbeddedTextFileOrThrow($"{TestTemplateDir}.Output_RegistryIds.sbncs");

        var expectedRegIdsTemplate = Template.Parse(expectedRegIdsTemplateString);
        var expectedRegIdsCode = expectedRegIdsTemplate.Render();

        var expectedModExtensionCode =
            EmbeddedFileHelper.ReadEmbeddedTextFileOrThrow($"{TestTemplateDir}.Output_ModExtension.sbncs");

        var expectedAttributeTree = CSharpSyntaxTree.ParseText(expectedAttributeCode);
        var expectedRegIdsTree = CSharpSyntaxTree.ParseText(expectedRegIdsCode);
        var expectedModExtensionTree = CSharpSyntaxTree.ParseText(expectedModExtensionCode);


        ISourceGenerator generator = new RegistryGenerator();
        Compile(generator, out _, out var diagnostics, out var generatedTrees, testCode,
            RegistryBaseCode, ModInterface, TestMod, Identification);


        Assert.Empty(diagnostics);
        Assert.Equal(3, generatedTrees.Length);
        var attributeTree = Array.Find(generatedTrees,
            x => x.FilePath.EndsWith("TestMod_Registries_TestRegistry_Att.g.cs"));
        var regIdsTree = Array.Find(generatedTrees, x => x.FilePath.EndsWith("TestMod.Test.reg.g.cs"));
        var modExtensionTree = Array.Find(generatedTrees, x => x.FilePath.EndsWith("TestMod.Test.g.cs"));

        Assert.NotNull(attributeTree);
        Assert.NotNull(regIdsTree);
        Assert.NotNull(modExtensionTree);

        Assert.True(expectedAttributeTree.IsEquivalentTo(attributeTree));
        Assert.True(expectedRegIdsTree.IsEquivalentTo(regIdsTree));
        Assert.True(expectedModExtensionTree.IsEquivalentTo(modExtensionTree));
    }

    [Theory]
    [InlineData("new()", (int)GenericConstraints.Constructor)]
    [InlineData("class", (int)GenericConstraints.ReferenceType)]
    [InlineData("struct", (int)GenericConstraints.ValueType)]
    [InlineData("unmanaged", (int)GenericConstraints.ValueType | (int)GenericConstraints.UnmanagedType)]
    [InlineData("notnull", (int)GenericConstraints.NotNull)]
    public void GenerateAttribute_GenericRegistryWithNewConstraint_ShouldGenerateCorrectCode(string constraint,
        int numericConstraint)
    {
        var testCodeTemplateString =
            EmbeddedFileHelper.ReadEmbeddedTextFileOrThrow($"{TestTemplateDir}.Input_GenerateAttribute_Generic_WithConstraint.sbncs");

        var testCodeTemplate = Template.Parse(testCodeTemplateString);
        var testCode = testCodeTemplate.Render(new Dictionary<string, object>
        {
            { "TypeConstraint", constraint }
        });


        var attributeTemplateString =
            EmbeddedFileHelper.ReadEmbeddedTextFileOrThrow($"{TestTemplateDir}.Output_RegistryAttribute.sbncs");

        var attributeTemplate = Template.Parse(attributeTemplateString);
        var expectedAttributeCode = attributeTemplate.Render(new Dictionary<string, object>
        {
            { "RegisterMethodName", "RegisterType" },
            { "RegisterType", (int)RegisterMethodType.Generic },
            { "RegistryPhase", 2 },
            { "GenericConstraints", numericConstraint },
            { "AttributeTarget", "AttributeTargets.Class | AttributeTargets.Struct"}
        });


        var expectedRegIdsTemplateString =
            EmbeddedFileHelper.ReadEmbeddedTextFileOrThrow($"{TestTemplateDir}.Output_RegistryIds.sbncs");

        var expectedRegIdsTemplate = Template.Parse(expectedRegIdsTemplateString);
        var expectedRegIdsCode = expectedRegIdsTemplate.Render();

        var expectedModExtensionCode =
            EmbeddedFileHelper.ReadEmbeddedTextFileOrThrow($"{TestTemplateDir}.Output_ModExtension.sbncs");

        var expectedAttributeTree = CSharpSyntaxTree.ParseText(expectedAttributeCode);
        var expectedRegIdsTree = CSharpSyntaxTree.ParseText(expectedRegIdsCode);
        var expectedModExtensionTree = CSharpSyntaxTree.ParseText(expectedModExtensionCode);

        ISourceGenerator generator = new RegistryGenerator();
        Compile(generator, out _, out var diagnostics, out var generatedTrees, testCode,
            RegistryBaseCode, ModInterface, TestMod, Identification);

        Assert.Empty(diagnostics);
        Assert.Equal(3, generatedTrees.Length);
        var attributeTree = Array.Find(generatedTrees,
            x => x.FilePath.EndsWith("TestMod_Registries_TestRegistry_Att.g.cs"));
        var regIdsTree = Array.Find(generatedTrees, x => x.FilePath.EndsWith("TestMod.Test.reg.g.cs"));
        var modExtensionTree = Array.Find(generatedTrees, x => x.FilePath.EndsWith("TestMod.Test.g.cs"));

        Assert.NotNull(attributeTree);
        Assert.NotNull(regIdsTree);
        Assert.NotNull(modExtensionTree);


        Assert.True(expectedAttributeTree.IsEquivalentTo(attributeTree));
        Assert.True(expectedRegIdsTree.IsEquivalentTo(regIdsTree));
        Assert.True(expectedModExtensionTree.IsEquivalentTo(modExtensionTree));
    }


    [Fact]
    public void GenerateAttribute_GenericRegistryWithInterfaceConstraint_ShouldGenerateCorrectCode()
    {
        var testCode =
            EmbeddedFileHelper.ReadEmbeddedTextFileOrThrow($"{TestTemplateDir}.Input_GenerateAttribute_Generic_WithTypeConstraint.sbncs");

        var attributeTemplateString =
            EmbeddedFileHelper.ReadEmbeddedTextFileOrThrow($"{TestTemplateDir}.Output_RegistryAttribute.sbncs");

        var attributeTemplate = Template.Parse(attributeTemplateString);
        var expectedAttributeCode = attributeTemplate.Render(new Dictionary<string, object>
        {
            { "RegisterMethodName", "RegisterType" },
            { "RegisterType", (int)RegisterMethodType.Generic },
            { "RegistryPhase", 2 },
            { "GenericTypeConstraints", "ITestInterface"},
            { "AttributeTarget", "AttributeTargets.Class | AttributeTargets.Struct"}
        });


        var expectedRegIdsTemplateString =
            EmbeddedFileHelper.ReadEmbeddedTextFileOrThrow($"{TestTemplateDir}.Output_RegistryIds.sbncs");

        var expectedRegIdsTemplate = Template.Parse(expectedRegIdsTemplateString);
        var expectedRegIdsCode = expectedRegIdsTemplate.Render();

        var expectedModExtensionCode =
            EmbeddedFileHelper.ReadEmbeddedTextFileOrThrow($"{TestTemplateDir}.Output_ModExtension.sbncs");
        
        
        var expectedAttributeTree = CSharpSyntaxTree.ParseText(expectedAttributeCode);
        var expectedRegIdsTree = CSharpSyntaxTree.ParseText(expectedRegIdsCode);
        var expectedModExtensionTree = CSharpSyntaxTree.ParseText(expectedModExtensionCode);


        ISourceGenerator generator = new RegistryGenerator();
        Compile(generator, out _, out var diagnostics, out var generatedTrees, testCode,
            RegistryBaseCode, ModInterface, TestMod, Identification);

        Assert.Empty(diagnostics);
        Assert.Equal(3, generatedTrees.Length);
        var attributeTree = Array.Find(generatedTrees, x => x.FilePath.EndsWith("TestMod_Registries_TestRegistry_Att.g.cs"));
        var regIdsTree = Array.Find(generatedTrees, x => x.FilePath.EndsWith("TestMod.Test.reg.g.cs"));
        var modExtensionTree = Array.Find(generatedTrees, x => x.FilePath.EndsWith("TestMod.Test.g.cs"));

        Assert.NotNull(attributeTree);
        Assert.NotNull(regIdsTree);
        Assert.NotNull(modExtensionTree);


        Assert.True(expectedAttributeTree.IsEquivalentTo(attributeTree));
        Assert.True(expectedRegIdsTree.IsEquivalentTo(regIdsTree));
        Assert.True(expectedModExtensionTree.IsEquivalentTo(modExtensionTree));
    }

    [Fact]
    public void GenerateAttribute_PropertyRegistry_ShouldGenerateCorrectCode()
    {
        string testCode = EmbeddedFileHelper.ReadEmbeddedTextFileOrThrow($"{TestTemplateDir}.Input_GenerateAttribute_Property.sbncs");
        
        var attributeTemplateString =
            EmbeddedFileHelper.ReadEmbeddedTextFileOrThrow($"{TestTemplateDir}.Output_RegistryAttribute.sbncs");
        var attributeTemplate = Template.Parse(attributeTemplateString);
        var expectedAttributeCode = attributeTemplate.Render(new Dictionary<string, object>
        {
            { "RegisterMethodName", "RegisterData" },
            { "RegisterType", (int)RegisterMethodType.Property },
            { "RegistryPhase", 2 },
            { "PropertyType", "TestMod.Registries.PropertyData"},
            { "AttributeTarget", "AttributeTargets.Property"}
        });


        var expectedRegIdsTemplateString =
            EmbeddedFileHelper.ReadEmbeddedTextFileOrThrow($"{TestTemplateDir}.Output_RegistryIds.sbncs");

        var expectedRegIdsTemplate = Template.Parse(expectedRegIdsTemplateString);
        var expectedRegIdsCode = expectedRegIdsTemplate.Render();

        var expectedModExtensionCode =
            EmbeddedFileHelper.ReadEmbeddedTextFileOrThrow($"{TestTemplateDir}.Output_ModExtension.sbncs");
        
        var expectedAttributeTree = CSharpSyntaxTree.ParseText(expectedAttributeCode);
        var expectedRegIdsTree = CSharpSyntaxTree.ParseText(expectedRegIdsCode);
        var expectedModExtensionTree = CSharpSyntaxTree.ParseText(expectedModExtensionCode);

        ISourceGenerator generator = new RegistryGenerator();
        Compile(generator, out _, out var diagnostics, out var generatedTrees, testCode,
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
        
        Assert.True(expectedAttributeTree.IsEquivalentTo(attributeTree));
        Assert.True(expectedRegIdsTree.IsEquivalentTo(regIdsTree));
        Assert.True(expectedModExtensionTree.IsEquivalentTo(modExtensionTree));
    }

    [Fact]
    public void GenerateAttribute_InvalidRegistry_ShouldGenerateError()
    {
        string testCode = EmbeddedFileHelper.ReadEmbeddedTextFileOrThrow($"{TestTemplateDir}.Input_GenerateAttribute_InvalidRegistry.sbncs");

        ISourceGenerator generator = new RegistryGenerator();
        Compile(generator, out _, out var diagnostics, out _, testCode,
            RegistryBaseCode, ModInterface, TestMod, Identification);

        Assert.Equal(2, diagnostics.Length);

        Assert.Contains(diagnostics, x => x.Id == "MC1101");
        Assert.Contains(diagnostics, x => x.Id == "MC1202");
    }

    [Fact]
    public void GenerateAttribute_NoRegistryMethod_ShouldGenerateWarning()
    {
        var testCode =
            EmbeddedFileHelper.ReadEmbeddedTextFileOrThrow($"{TestTemplateDir}.Input_GenerateAttribute_NoRegistryMethod.sbncs");

        ISourceGenerator generator = new RegistryGenerator();
        Compile(generator, out _, out var diagnostics, out _, testCode,
            RegistryBaseCode, ModInterface, TestMod, Identification);

        Assert.Single(diagnostics);

        Assert.Contains(diagnostics, x => x.Id == "MC1101");
    }
}