using SharedCode;

namespace MintyCore.Generator.Tests.CommonTemplates;

public static class Templates
{
    private const string TemplateDir = "MintyCore.Generator.Tests.CommonTemplates";
    
    public static string MessageInterface
    {
        get
        {
            var template = EmbeddedFileHelper.ReadEmbeddedTextFile($"{TemplateDir}.MessageInterface.sbncs");
            Assert.NotNull(template);
            return template;
        }
    } 
    
    public static string ModInterface
    {
        get
        {
            var template = EmbeddedFileHelper.ReadEmbeddedTextFile($"{TemplateDir}.ModInterface.sbncs");
            Assert.NotNull(template);
            return template;
        }
    }
    
    public static string RegistryBaseCode
    {
        get
        {
            var template = EmbeddedFileHelper.ReadEmbeddedTextFile($"{TemplateDir}.RegistryBaseCode.sbncs");
            Assert.NotNull(template);
            return template;
        }
    }
    
    
    public static string TestMod
    {
        get
        {
            var template = EmbeddedFileHelper.ReadEmbeddedTextFile($"{TemplateDir}.TestMod.sbncs");
            Assert.NotNull(template);
            return template;
        }
    }
    
    public static string Identification
    {
        get
        {
            var template = EmbeddedFileHelper.ReadEmbeddedTextFile($"{TemplateDir}.Identification.sbncs");
            Assert.NotNull(template);
            return template;
        }
    }
}