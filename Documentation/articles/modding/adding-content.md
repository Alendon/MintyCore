# Adding Content

Every piece of content whether its an item, shader or a system needs to be registered into the game. 
By registering it gets assigned a unique id which is used to identify it in the game.
This id is used to reference the content in the game and allows other mods to interact with it without needing to reference your mod assembly.

The engine utilizes a source generator to automatically generate the boilerplate code for you to register content into the game.
This allows you to focus on the actual content and not worry about the registration.

As there are many different types of content, there are three different ways to register them:
1. Registering a "Type". This is used for registering content like components or systems where each one is a different type which gets added into the game.
To register a new type you need to annotate the type itself with the associated `RegisterTypeAttribute`.
2. Registering a "Property". This is used for registering content which is defined through a general describing object.
The object returned by the property gets then passed to the actual registry to register the new content.
Also annotate the property with the associated `RegisterPropertyAttribute`.
3. Registering through the resource file. The last method is to use the resource file. This is used for content which is only defined by a single file. For example a mesh, shader or texture.
To use this you need to provide a file named `GenerateRegistryData.json` in the root of your project.
This file contains a list of content types which can be registered and the actual file names to register.

After you have registered your content, a new class will be added automatically to your project in the `YourMod.Identifications` namespace. 
This class contains all ids of a specific type of content you have registered.
By using this class you can directly access the id of the content you've registered.

You can also manually register content although this is not recommended. To get a understanding of how the registration works you can take a look at the source generated files.

