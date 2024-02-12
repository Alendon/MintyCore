using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp;
using MintyCore.Generator.Registry;

namespace MintyCore.Generator.Tests.TestRegistry;

public class TestSourceGenerator
{
    [Fact]
    public void RenderRegistryIds_NoRegistry_GenerateCorrectCode()
    {
        var modInfo = new ModInfo
        {
            Namespace = "Test",
            ClassName = "TestMod"
        };

        var registries = new RegisterMethodInfo[]
        {
        };

        var result = SourceBuilder.RenderRegistryIDs(modInfo, registries);
        
        Assert.Empty(result);
    }

    [Fact]
    public void RenderRegistryIds_OneRegistry_GenerateCorrectCode()
    {
        var modInfo = new ModInfo
        {
            Namespace = "Test",
            ClassName = "TestMod"
        };

        var registries = new RegisterMethodInfo[]
        {
            new()
            {
                Namespace = "Test.Registry",
                ClassName = "BlockRegistry",
                CategoryId = "block",
            }
        };

        string expected =
            """
            #nullable enable 
            #pragma warning disable CS1591
            
            namespace Test.Identifications;

            [global::MintyCore.Modding.Attributes.RegistryProvider]
            public class RegistryIDs : global::MintyCore.Modding.Providers.IRegistryProvider
            {
                public static ushort Block { get; private set; }
                
                void global::MintyCore.Modding.Providers.IRegistryProvider.Register(global::Autofac.ILifetimeScope lifetimeScope, ushort modId)
                {
                    var registryManager = global::Autofac.ResolutionExtensions.Resolve<global::MintyCore.Modding.IModManager>(lifetimeScope).RegistryManager;
                    Block = registryManager.AddRegistry<global::Test.Registry.BlockRegistry>(modId, "block", null, global::MintyCore.Utils.GameType.);
                }
            }
            """;

        var result = SourceBuilder.RenderRegistryIDs(modInfo, registries);

        var expectedTree = CSharpSyntaxTree.ParseText(expected);
        var resultTree = CSharpSyntaxTree.ParseText(result);

        Assert.True(expectedTree.IsEquivalentTo(resultTree));
    }

    [Fact]
    public void RenderRegistryIds_TwoRegistry_GenerateCorrectCode()
    {
        var modInfo = new ModInfo
        {
            Namespace = "Test",
            ClassName = "TestMod"
        };

        var registries = new RegisterMethodInfo[]
        {
            new()
            {
                Namespace = "Test.Registry",
                ClassName = "BlockRegistry",
                CategoryId = "block",
            },
            new()
            {
                Namespace = "Test.Registry",
                ClassName = "TextureRegistry",
                CategoryId = "texture",
                ResourceSubFolder = "textures"
            }
        };

        string expected =
            """
            #nullable enable
            #pragma warning disable CS1591
            
            namespace Test.Identifications;
            [global::MintyCore.Modding.Attributes.RegistryProvider]
            public class RegistryIDs : global::MintyCore.Modding.Providers.IRegistryProvider
            {
                public static ushort Block { get; private set; }
                public static ushort Texture { get; private set; }
                
                void global::MintyCore.Modding.Providers.IRegistryProvider.Register(global::Autofac.ILifetimeScope lifetimeScope, ushort modId)
                {
                    var registryManager = global::Autofac.ResolutionExtensions.Resolve<global::MintyCore.Modding.IModManager>(lifetimeScope).RegistryManager;
                    Block = registryManager.AddRegistry<global::Test.Registry.BlockRegistry>(modId, "block", null, global::MintyCore.Utils.GameType.);
                    Texture = registryManager.AddRegistry<global::Test.Registry.TextureRegistry>(modId, "texture", "textures", global::MintyCore.Utils.GameType.);
                }
            }
            """;

        var result = SourceBuilder.RenderRegistryIDs(modInfo, registries);

        var expectedTree = CSharpSyntaxTree.ParseText(expected);
        var resultTree = CSharpSyntaxTree.ParseText(result);

        Assert.True(expectedTree.IsEquivalentTo(resultTree));
    }

    [Fact]
    public void RenderRegisterMethodInfo_GenericRegistry_GenerateCorrectCode()
    {
        RegisterMethodInfo methodInfo = new()
        {
            Namespace = "Test.Registry",
            ClassName = "BlockRegistry",
            CategoryId = "block",
            Constraints = GenericConstraints.ValueType | GenericConstraints.Constructor,
            MethodName = "RegisterCustomBlock",
            InvocationReturnType = null,
            HasFile = false,
            RegistryPhase = 2,
            ResourceSubFolder = null,
            RegisterType = RegisterMethodType.Generic,
            GenericConstraintTypes = new[] { "global::TestMod.IBlock", "global::TestMod.IBlock2" },
            GameType = "Local"
        };

        var expectedResult =
            """
            #nullable enable 
            namespace Test.Registry;

            public sealed class BlockRegistry_RegisterCustomBlock : global::MintyCore.Modding.Attributes.RegisterMethodInfo
            {
                private BlockRegistry_RegisterCustomBlock() { }
                
                public const string Namespace = "Test.Registry";
                public const string ClassName = "BlockRegistry";
                public const string MethodName = "RegisterCustomBlock";
                public const int RegisterType = 2;
                public const string? ResourceSubFolder = null;
                public const bool HasFile = false;
                public const int Constraints = 17;
                public const string GenericConstraintTypes = "global::TestMod.IBlock,global::TestMod.IBlock2";
                public const int RegistryPhase = 2;
                public const string? InvocationReturnType = null;
                public const string CategoryId = "block";
                public const string GameType = "Local";
            }
            """;

        var actualResult = SourceBuilder.RenderRegisterMethodInfo(methodInfo);

        var expectedTree = CSharpSyntaxTree.ParseText(expectedResult);
        var resultTree = CSharpSyntaxTree.ParseText(actualResult);

        Assert.True(expectedTree.IsEquivalentTo(resultTree));
    }

    [Fact]
    public void RenderRegisterMethodInfo_PropertyRegistryWithFile_GenerateCorrectCode()
    {
        RegisterMethodInfo methodInfo = new()
        {
            Namespace = "Test.Registry",
            ClassName = "BlockRegistry",
            CategoryId = "block",
            Constraints = 0,
            MethodName = "RegisterCustomBlock",
            InvocationReturnType = "global::TestMod.Block",
            HasFile = true,
            RegistryPhase = 2,
            ResourceSubFolder = "blocks",
            RegisterType = RegisterMethodType.Invocation,
            GenericConstraintTypes = Array.Empty<string>(),
            GameType = "Local"
        };

        var expectedResult =
            """
            #nullable enable 
            #pragma warning disable CS1591
            
            namespace Test.Registry;

            public sealed class BlockRegistry_RegisterCustomBlock : global::MintyCore.Modding.Attributes.RegisterMethodInfo
            {
                private BlockRegistry_RegisterCustomBlock() { }
                
                public const string Namespace = "Test.Registry";
                public const string ClassName = "BlockRegistry";
                public const string MethodName = "RegisterCustomBlock";
                public const int RegisterType = 1;
                public const string? ResourceSubFolder = "blocks";
                public const bool HasFile = true;
                public const int Constraints = 0;
                public const string GenericConstraintTypes = "";
                public const int RegistryPhase = 2;
                public const string? InvocationReturnType = "global::TestMod.Block";
                public const string CategoryId = "block";
                public const string GameType = "Local";
            }
            """;

        var actualResult = SourceBuilder.RenderRegisterMethodInfo(methodInfo);

        var expectedTree = CSharpSyntaxTree.ParseText(expectedResult);
        var resultTree = CSharpSyntaxTree.ParseText(actualResult);

        Assert.True(expectedTree.IsEquivalentTo(resultTree));
    }

    [Fact]
    public void RenderRegistryObjectIDs_RegisterFile_GenerateCorrectCode()
    {
        var modInfo = new ModInfo
        {
            Namespace = "Test",
            ClassName = "TestMod"
        };

        RegisterMethodInfo methodInfo = new()
        {
            Namespace = "Test.Registry",
            ClassName = "TextureRegistry",
            CategoryId = "texture",
            Constraints = 0,
            MethodName = "RegisterTexture",
            InvocationReturnType = null,
            HasFile = true,
            RegistryPhase = 2,
            ResourceSubFolder = "textures",
            RegisterType = RegisterMethodType.File,
            GenericConstraintTypes = Array.Empty<string>()
        };

        RegisterObject registerObject = new()
        {
            Id = "test",
            File = "test.png",
            RegisterMethodInfo = methodInfo
        };

        var result = SourceBuilder.RenderRegistryObjectIDs(modInfo, ImmutableArray.Create(registerObject));

        var expected = """
                       #nullable enable 
                       #pragma warning disable CS1591
                       
                       namespace Test.Identifications;
                                              
                       [global::MintyCore.Modding.Attributes.RegistryObjectProviderAttribute("texture")]
                       public class TextureIDs : global::MintyCore.Modding.Providers.IMainRegisterProvider
                       {
                       
                           
                           public static global::MintyCore.Utils.Identification Test { get; private set; }
                           
                           void MintyCore.Modding.Providers.IMainRegisterProvider.MainRegister(global::Autofac.ILifetimeScope lifetimeScope, ushort modId)
                           {
                               var registryManager = global::Autofac.ResolutionExtensions.Resolve<global::MintyCore.Modding.IModManager>(lifetimeScope).RegistryManager;
                               if (!registryManager.TryGetCategoryId("texture", out var categoryId))
                               {
                                   throw new global::System.Exception("Failed to get category (\"texture\") id for MainRegister");
                               }
                               
                               {
                                   
                                   
                                   var id = registryManager.RegisterObjectId(modId, categoryId, "test", "test.png");
                                   Test = id;
                                   
                                   var registryClass = global::Autofac.ResolutionExtensions.ResolveNamed<global::Test.Registry.TextureRegistry>(lifetimeScope, global::MintyCore.Utils.AutofacHelper.UnsafeSelfName);
                                   registryClass.RegisterTexture(id);
                               }
                           }
                       }
                       """;

        var expectedTree = CSharpSyntaxTree.ParseText(expected);
        var resultTree = CSharpSyntaxTree.ParseText(result);

        Assert.True(expectedTree.IsEquivalentTo(resultTree));
    }

    [Fact]
    public void RenderRegistryObjectIDs_RegisterProperty_GenerateCorrectCode()
    {
        var modInfo = new ModInfo
        {
            Namespace = "Test",
            ClassName = "TestMod"
        };

        RegisterMethodInfo methodInfo = new()
        {
            Namespace = "Test.Registry",
            ClassName = "TextureRegistry",
            CategoryId = "texture",
            Constraints = 0,
            MethodName = "RegisterTexture",
            InvocationReturnType = "global::TestMod.TextureInfo",
            HasFile = true,
            RegistryPhase = 2,
            ResourceSubFolder = "textures",
            RegisterType = RegisterMethodType.Invocation,
            GenericConstraintTypes = Array.Empty<string>()
        };

        RegisterObject registerObject = new()
        {
            Id = "test",
            File = "test.png",
            RegisterMethodInfo = methodInfo,
            RegisterProperty = "TestMod.Textures.Test"
        };

        var result = SourceBuilder.RenderRegistryObjectIDs(modInfo, ImmutableArray.Create(registerObject));

        var expected = """
                       #nullable enable
                       #pragma warning disable CS1591
                       
                       namespace Test.Identifications;
                       
                       [global::MintyCore.Modding.Attributes.RegistryObjectProviderAttribute("texture")]
                       public class TextureIDs : global::MintyCore.Modding.Providers.IMainRegisterProvider
                       {
                           public static global::MintyCore.Utils.Identification Test { get; private set; }
                           
                           void MintyCore.Modding.Providers.IMainRegisterProvider.MainRegister(global::Autofac.ILifetimeScope lifetimeScope, ushort modId)
                           {
                               var registryManager = global::Autofac.ResolutionExtensions.Resolve<global::MintyCore.Modding.IModManager>(lifetimeScope).RegistryManager;
                               if (!registryManager.TryGetCategoryId("texture", out var categoryId))
                               {
                                   throw new global::System.Exception("Failed to get category (\"texture\") id for MainRegister");
                               }
                               
                               {
                                   var registerParameter = global::TestMod.Textures.Test;
                                   
                                   
                                   var id = registryManager.RegisterObjectId(modId, categoryId, "test", "test.png");
                                   Test = id;
                                   
                                   var registryClass = global::Autofac.ResolutionExtensions.ResolveNamed<global::Test.Registry.TextureRegistry>(lifetimeScope, global::MintyCore.Utils.AutofacHelper.UnsafeSelfName);
                                   registryClass.RegisterTexture(id, registerParameter);
                               }
                           }
                       }
                       """;

        var expectedTree = CSharpSyntaxTree.ParseText(expected);
        var resultTree = CSharpSyntaxTree.ParseText(result);

        Assert.True(expectedTree.IsEquivalentTo(resultTree));
    }

    [Fact]
    public void RenderRegistryObjectIDs_RegisterGeneric_GenerateCorrectCode()
    {
        var modInfo = new ModInfo
        {
            Namespace = "Test",
            ClassName = "TestMod"
        };

        RegisterMethodInfo methodInfo = new()
        {
            Namespace = "Test.Registry",
            ClassName = "TextureRegistry",
            CategoryId = "texture",
            Constraints = 0,
            MethodName = "RegisterTexture",
            InvocationReturnType = null,
            HasFile = true,
            RegistryPhase = 2,
            ResourceSubFolder = "textures",
            RegisterType = RegisterMethodType.Generic,
            GenericConstraintTypes = Array.Empty<string>()
        };

        RegisterObject registerObject = new()
        {
            Id = "test",
            File = "test.png",
            RegisterMethodInfo = methodInfo,
            RegisterType = "TestMod.TextureInfo"
        };

        var result = SourceBuilder.RenderRegistryObjectIDs(modInfo, ImmutableArray.Create(registerObject));

        var expected = """
                       #nullable enable
                       #pragma warning disable CS1591
                       
                       namespace Test.Identifications;
                       
                       [global::MintyCore.Modding.Attributes.RegistryObjectProviderAttribute("texture")]
                       public class TextureIDs : global::MintyCore.Modding.Providers.IMainRegisterProvider
                       {
                           public static global::MintyCore.Utils.Identification Test { get; private set; }
                           
                           void MintyCore.Modding.Providers.IMainRegisterProvider.MainRegister(global::Autofac.ILifetimeScope lifetimeScope, ushort modId)
                           {
                               var registryManager = global::Autofac.ResolutionExtensions.Resolve<global::MintyCore.Modding.IModManager>(lifetimeScope).RegistryManager;
                               if (!registryManager.TryGetCategoryId("texture", out var categoryId))
                               {
                                   throw new global::System.Exception("Failed to get category (\"texture\") id for MainRegister");
                               }
                               
                               {
                                   var id = registryManager.RegisterObjectId(modId, categoryId, "test", "test.png");
                                   Test = id;
                                   
                                   var registryClass = global::Autofac.ResolutionExtensions.ResolveNamed<global::Test.Registry.TextureRegistry>(lifetimeScope, global::MintyCore.Utils.AutofacHelper.UnsafeSelfName);
                                   registryClass.RegisterTexture<global::TestMod.TextureInfo>(id);
                               }
                           }
                       }
                       """;

        var expectedTree = CSharpSyntaxTree.ParseText(expected);
        var resultTree = CSharpSyntaxTree.ParseText(result);

        Assert.True(expectedTree.IsEquivalentTo(resultTree));
    }

    [Fact]
    public void RenderAttribute_RegisterPropertyAttribute_GenerateCorrectCode()
    {
        RegisterMethodInfo methodInfo = new()
        {
            Namespace = "Test.Registry",
            ClassName = "TextureRegistry",
            CategoryId = "texture",
            Constraints = 0,
            MethodName = "RegisterTexture",
            InvocationReturnType = "global::TestMod.TextureInfo",
            HasFile = true,
            RegistryPhase = 2,
            ResourceSubFolder = "textures",
            RegisterType = RegisterMethodType.Invocation,
            GenericConstraintTypes = Array.Empty<string>()
        };

        var result = SourceBuilder.RenderAttribute(methodInfo);

        var expected = """
                       #nullable enable 
                       #pragma warning disable CS1591
                       
                       namespace Test.Registry;
                       
                       [global::System.AttributeUsage(global::System.AttributeTargets.Property | global::System.AttributeTargets.Method, AllowMultiple = false)]
                       [global::MintyCore.Modding.Attributes.ReferencedRegisterMethod<global::Test.Registry.TextureRegistry_RegisterTexture>()]
                       [global::JetBrains.Annotations.MeansImplicitUseAttribute]
                       public sealed class RegisterTextureAttribute : global::MintyCore.Modding.Attributes.RegisterBaseAttribute
                       {
                           public RegisterTextureAttribute(string id, string file)
                           {
                           }
                       }
                       """;
        
        var expectedTree = CSharpSyntaxTree.ParseText(expected);
        var resultTree = CSharpSyntaxTree.ParseText(result);
        
        Assert.True(expectedTree.IsEquivalentTo(resultTree));
    }
    
    [Fact]
    public void RenderAttribute_RegisterGenericAttribute_GenerateCorrectCode()
    {
        RegisterMethodInfo methodInfo = new()
        {
            Namespace = "Test.Registry",
            ClassName = "TextureRegistry",
            CategoryId = "texture",
            Constraints = 0,
            MethodName = "RegisterTexture",
            InvocationReturnType = null,
            HasFile = false,
            RegistryPhase = 2,
            ResourceSubFolder = null,
            RegisterType = RegisterMethodType.Generic,
            GenericConstraintTypes = Array.Empty<string>()
        };

        var result = SourceBuilder.RenderAttribute(methodInfo);

        var expected = """
                       #nullable enable 
                       namespace Test.Registry;

                       [global::System.AttributeUsage(global::System.AttributeTargets.Class | global::System.AttributeTargets.Struct, AllowMultiple = false)]
                       [global::MintyCore.Modding.Attributes.ReferencedRegisterMethod<global::Test.Registry.TextureRegistry_RegisterTexture>()]
                       [global::JetBrains.Annotations.MeansImplicitUseAttribute]
                       public sealed class RegisterTextureAttribute : global::MintyCore.Modding.Attributes.RegisterBaseAttribute
                       {
                           public RegisterTextureAttribute(string id)
                           {
                           }
                       }
                       """;
        
        var expectedTree = CSharpSyntaxTree.ParseText(expected);
        var resultTree = CSharpSyntaxTree.ParseText(result);
        
        Assert.True(expectedTree.IsEquivalentTo(resultTree));
    }
}