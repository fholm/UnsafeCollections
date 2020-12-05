A fork of https://github.com/fholm/UnsafeCollections

Project is targeted as a .Net 2.0 Standard library. Usable in Unity via dll.

## Build
Use Preprocessor directive UNITY to build the project using the Unity memory allocators instead of the .Net ones.

The library is usable in both .Net as well as Unity.


### ToDo
- Add type safety for Collections other than UnsafeArray.
- Add wrappers for often-used collections to make the API easier to use.

### Future
- Generate managed and unmanaged wrappers via T4 templates
