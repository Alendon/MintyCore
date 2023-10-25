﻿using System;
using MintyCore.Modding;
using MintyCore.Registries;
using MintyCore.Render;
using MintyCore.Render.Managers.Interfaces;
using Silk.NET.Vulkan;

namespace MintyCore;

/// <summary>
///     The Engine/CoreGame <see cref="IMod" /> which adds all essential stuff to the game
/// </summary>
public sealed class MintyCoreMod : IMod
{
    /// <inheritdoc />
    public void Dispose()
    {
    }

    internal static ModManifest ConstructManifest()
    {
        return new ModManifest
        {
            Authors = new[]
            {
                "Alendon", "Erikiller"
            },
            Version = new Version(0, 5),
            IsRootMod = true,
            Identifier = "minty_core",
            Description = "The base mod of the MintyCore engine",
            Name = "MintyCore",
            ModDependencies = Array.Empty<string>(),
            //external dependencies can be omitted
            ExternalDependencies = Array.Empty<ExternalDependency>()
        };
    }

    /// <inheritdoc />
    public void PreLoad()
    {
    }

    /// <inheritdoc />
    public void Load()
    {
    }

    /// <inheritdoc />
    public void PostLoad()
    {
    }

    /// <inheritdoc />
    public void Unload()
    {
    }

    [RegisterDescriptorSet("sampled_texture")]
    internal static DescriptorSetInfo TextureBindInfo => new()
    {
        Bindings = new[]
        {
            new DescriptorSetLayoutBinding
            {
                Binding = 0,
                DescriptorCount = 1,
                DescriptorType = DescriptorType.CombinedImageSampler,
                StageFlags = ShaderStageFlags.FragmentBit
            }
        },
        DescriptorSetsPerPool = 100
    };
}