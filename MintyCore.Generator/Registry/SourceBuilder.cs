using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using JetBrains.Annotations;
using Scriban;
using SharedCode;

namespace MintyCore.Generator.Registry;

public static class SourceBuilder
{
    private const string TemplateDirectory = "MintyCore.Generator.Registry.Templates";

    private static Template ModExtensionTemplate =>
        Template.Parse(EmbeddedFileHelper.ReadEmbeddedTextFileOrThrow($"{TemplateDirectory}.ModExtension.sbncs"));

    private static Template RegistryIdsTemplate =>
        Template.Parse(EmbeddedFileHelper.ReadEmbeddedTextFileOrThrow($"{TemplateDirectory}.RegistryIds.sbncs"));

    private static Template RegisterMethodInfoTemplate =>
        Template.Parse(EmbeddedFileHelper.ReadEmbeddedTextFileOrThrow($"{TemplateDirectory}.RegisterMethodInfo.sbncs"));

    private static Template RegistryObjectIDsTemplate =>
        Template.Parse(EmbeddedFileHelper.ReadEmbeddedTextFileOrThrow($"{TemplateDirectory}.RegistryObjectIds.sbncs"));

    private static Template RegisterAttributeTemplate =>
        Template.Parse(EmbeddedFileHelper.ReadEmbeddedTextFileOrThrow($"{TemplateDirectory}.Attribute.sbncs"));

    public static string RenderModExtension(ModInfo mod, IEnumerable<RegisterMethodInfo> registerMethodInfos)
    {
        return ModExtensionTemplate.Render(new { Mod = mod, Registries = registerMethodInfos }, member => member.Name);
    }

    public static string RenderRegistryIDs(ModInfo mod, IEnumerable<RegisterMethodInfo> registerMethodInfos)
    {
        return RegistryIdsTemplate.Render(new { Mod = mod, Registries = registerMethodInfos }, member => member.Name);
    }

    public static string RenderRegisterMethodInfo(RegisterMethodInfo registerMethodInfo)
    {
        return RegisterMethodInfoTemplate.Render(registerMethodInfo, member => member.Name);
    }

    public static string RenderRegistryObjectIDs(ModInfo mod, ImmutableArray<RegisterObject> registerObjects)
    {
        if (registerObjects.Length == 0)
            return string.Empty;

        var phases = new RegistryPhase[]
        {
            new()
            {
                Name = "PreRegister",
                RegisterObjects = registerObjects.Where(x => x.RegisterMethodInfo.RegistryPhase == 1)
            },
            new()
            {
                Name = "MainRegister",
                RegisterObjects = registerObjects.Where(x => x.RegisterMethodInfo.RegistryPhase == 2)
            },
            new()
            {
                Name = "PostRegister",
                RegisterObjects = registerObjects.Where(x => x.RegisterMethodInfo.RegistryPhase == 3)
            }
        };

        return RegistryObjectIDsTemplate.Render(
            new
            {
                Mod = mod, 
                RegistryPhases = phases, 
                RegistryName = registerObjects[0].RegisterMethodInfo.CategoryName,
                registerObjects[0].RegisterMethodInfo.CategoryId
            }, member => member.Name);
    }

    struct RegistryPhase
    {
        [UsedImplicitly]
        public IEnumerable<RegisterObject> RegisterObjects;
        [UsedImplicitly]
        public string Name;
    }

    public static string RenderAttribute(RegisterMethodInfo registerMethodInfo)
    {
        var requireStruct = (registerMethodInfo.Constraints & GenericConstraints.ValueType) != 0;
        var requireClass = (registerMethodInfo.Constraints & GenericConstraints.ReferenceType) != 0;

        var structTarget = (requireStruct || !requireClass) && registerMethodInfo.RegisterType == RegisterMethodType.Generic;
        var classTarget = (requireClass || !requireStruct) && registerMethodInfo.RegisterType == RegisterMethodType.Generic;
        var propertyTarget = registerMethodInfo.RegisterType == RegisterMethodType.Property;

        return RegisterAttributeTemplate.Render(new
        {
            Registry = registerMethodInfo,
            StructTarget = structTarget,
            ClassTarget = classTarget,
            PropertyTarget = propertyTarget
        }, member => member.Name);
    }
}