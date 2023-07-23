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
}