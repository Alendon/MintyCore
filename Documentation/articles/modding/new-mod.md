# Create a new mod
## Introduction
This guide will show you how to create a new mod for the game. It will cover the basics of creating a mod, and how to add new content to the game.

## Setup nuke

To create a new mod, you will need to create a new Repository for the mod. Although it is theoretically possible to have multiple mods inside a single repository, it is not recommended as you likely need a different build configuration for each mod.

You should have the nuke global tool installed, if not go to the [installation guide](../getting-started/installation.md#installation-prerequisites) and follow the instructions there.

Now setup nuke by running the following command in the root of your repository:

```bash
nuke :setup
```
Then follow the instructions on screen.

After nuke is setup, replace the Build.cs and parameters.json file with the one from Techardry also add the DependencyResolver.cs from Techardry alongside the Build.cs file. After that configure the parameters.json file to your liking.

You can now run nuke by the command line or directly from your ide if it supports it. The major ones have support through plugins.

## Create a new mod

You need to create a new class library to the repository, this will contain the mod itself.

The name of the class library should be the (simplified) name of the mod.

## Configure the mod

Open the .csproj file of the class library and add the following lines directly inside the `<Project>` tag:

```xml
<ProjectExtensions>
        <MintyCoreMod>
            <Name>My Awesome Mod</Name>
            <Id>my_awesome_mod</Id>
            <Version>0.0.1</Version>
            <Description>Just a simple mod to test engine features</Description>
            <Authors>Alendon;Erikiller</Authors>
            <IsRootMod>false</IsRootMod>

            <Dependency Id="minty_core" PackageName="MintyCore" Version="0.4.1"/>
        </MintyCoreMod>
    </ProjectExtensions>
```
This is your main configuration for the mod. 

- The name can be any string but the id must be in snake_case and globally unique.
- The version should be in the semantic versioning format. 
- The description can be any string.
- The authors should be a list of the authors of the mod, separated by semicolons.
- The IsRootMod should be set to false, unless this mod should be loaded directly after the game starts. More on this can be read under RootMods.
- You can add multiple Dependencies to the mod. The id must be the id of the mod you want to add. The package name the name of the package on nuget. The version should be in the semantic versioning format. If no Version is specified, the latest version will be used.

Now run the nuke restore target to restore the dependencies.

## Create the main class

Now add a new class to the class library, this will be the main class of the mod. 
The name of the class can not be the same as the root namespace.
The build will currently fail otherwise. Keep this in mind as some IDEs encourage you to name the root namespace after the folder which is often the same as the class library name.

This new class needs to inherit from the `MintyCore.Modding.IMod` interface.

The mod class needs to be public, partial and sealed. This is required to ensure that no side effects will be created and to enable the source generation of helper methods.

From the `Load` and `Unload` Method the `InternalRegister` and `InternalUnregister` methods should be called. Those methods get created by the source generator and enables the source generated registration of the mod content.

Additionally the mod class needs to define a singleton property of the type of the mod class itself. This is required to enable the source generated registration of the mod content.

The mod class should look like this:

```csharp
public sealed partial class MyAwesomeMod : IMod
{
    public static MyAwesomeMod? Instance { get; private set; }

    public ushort ModId { get; set; }

    public void PreLoad()
    {
        Instance = this;
    }
    
    public void Load()
    {
        InternalRegister();
    }

    public void PostLoad()
    {
    }

    public void Unload()
    {
        InternalUnregister();
    }
    
    public void Dispose()
    {
    }
}
```

## Build the mod

To build the mod, just run the nuke `BuildModFile` target. This will automatically build the project and pack it to a mod file which then can be loaded by the game.

## Load the mod

To load the mod into the game, copy the mod file to the `Mods` folder in the game directory.

Alternatively you can start the game with the `addModDir=<path>` command line argument. This will add the specified path to the mod search path. This can be used to load mods from a different directory than the `Mods` folder.

## Publish to nuget

To publish the mod to nuget, run the nuke `PublishMod` target. This will automatically build the project and create a nuget package. The package then needs to be manually uploaded to nuget.