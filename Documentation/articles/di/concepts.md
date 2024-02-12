# Dependency Injection in the Game Engine

The Dependency Injection (DI) concept in this game engine primarily aims to abstract singletons, offering multiple
container levels and varied service types.

## Container Levels

Services and dependencies are registered and managed within the engine on several distinct levels:

1. **Engine**: Loaded directly at the start.
2. **Root Mods**: Initialized after the engine is up and running.
3. **Game Mods**: When a game session begins.

## Service Types and Registration

Every type registered is named as "unsafe-self". This facilitates a "Service Locator" pattern if required, although
typically, its use is not recommended.

The game engine also permits a single instance to be created for every mod. While the registry classes have not been
fully fleshed out, they are expected to exist as a single instance but remain accessible to the registration logic.

## Registration Logic

Interestingly, this logic does not strictly adhere to the traditional DI pattern but more closely resembles a Service
Locator:

* A source generator scans all registry calls, generating a class filled with boilerplate code to register each object, grouped by registration type per mod.
* The registration method in this class accepts the lifetime scope for the mod (either Root or Game Mods scope) and resolves the necessary Registry Class.
* Every object then gets registered. Depending on the nature of the registration, a different approach is taken, such as with file, generic, or static property registrations.

If classes aren’t registered, a new scope with the required class is created to resolve them. This way, the class can
access content from the DI container, like when accessing the Vulkan Swapchain Format, for instance.

## Message Behavior

Messages in the engine also have a unique behavior as they essentially need to access content from the DI container. For
this, there's a separate DI container for the MessageHandler, containing all messages.

Upon receiving a message, the MessageHandler resolves the message and invokes the handle method. If there's a need to
send a message, a new message instance can be initiated through the MessageHandler.