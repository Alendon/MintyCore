---
uid: static-runtime
---
# Dynamic at Initialization - Static at Runtime

## Every part of the game should be modifiable.

The core concept of a mod-able game. This includes adding new and altering other stuff.

Currently it is not planned to allow the removal of content added by other mods. 

## The runtime should be static.

The prohibition of adding new content to the game while its running allows to make optimizations, to achieve (for
example) a better performance.

This is directly tied to the Registry System. It is not allowed to change anything through this while the game is running.



