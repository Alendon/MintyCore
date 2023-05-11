# Registry Concept

## What is a Registry?

A Registry is a class which handles the addition (registration) of new objects/content/structure to the game through a
associated id.

Additionally it may allow the modification of objects added by other mods.

One registry should only register one type of object.
If a group of objects is closely related, it may be registered by one registry.

## Registry Dependencies

It is possible that a registry depends on the output of another registry. In this case a dependency can be defined. The
RegistryManager will execute the dependent registry first.

## Registry Phases

There are 3 phases where a part of a registry can be executed.

1. **Pre**-Registry Phase: Currently unused.
2. **Main**-Registry Phase: New objects should be registered here.
3. **Post**-Registry Phase: Alterations of objects should be done here.

## Oddities

Although it is called Registry the actual registration of ids is done by the mods themselves.

This is done to allow a easier and simpler way to implement a new Registry and register new objects, as the required
code will be provided by a SourceGenerator.