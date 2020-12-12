A fork of https://github.com/fholm/UnsafeCollections

**This fork is still a WIP**

This fork contains many bug fixes/improvements and an easier to use API.

Project is targeted as a .Net 2.0 Standard library. Usable in Unity via dll.

## Currently Implemented

- Array
- List
- Stack
- Queue
- Bit Set
- Ring Buffer
- Min Heap
- Max Heap
- Hash Map
- Hash Set
- Ordered Map (Not Complete, Public API Missing)
- Ordered Set
- Concurrent Single Producer Single Consumer Queue (SPCS, lockless)

## Planned Additions

- Concurrent Multi Producer Multi Consumer Queue (MPMC, mostly lockless)
- Concurrent Multi Producer Multi Consumer Dictionary (MPMC, mostly lockless)

## Build
Use Preprocessor directive UNITY to build the project using the Unity memory allocators instead of the .Net ones.

The library is usable in both .Net and Unity.


### ToDo
- Add type safety for Collections other than UnsafeArray.
- Add wrappers for often-used collections to make the API easier to use.

### Future
- Generate managed and unmanaged wrappers via T4 templates
