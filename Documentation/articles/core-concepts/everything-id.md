---
uid: everything-id
---

# Everything has an ID

Every aspect/object of the game which adds structure must have an ID.

The ID is used to identify the object. It is used for example in the network messages to identify objects to
synchronize.

Also this is one way to implement mods with optional dependencies. If a mod has a optional dependency it can access a
specific object by defining an ID, instead of the need to have the other mod assembly referenced.