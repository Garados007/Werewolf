# Test setup for Logic Tests

The directory `Basic` contains all generall tests that checks if the code
generation works at all and if the build in functions works. All other
directories contain tests for their respective modes.

To run these tests, you have to build it first using `dotnet build` to let all
the required code generate. After that a simple `dotnet test` is sufficient.

If one directory contains a `*.w5logic` file, then this directory will be
handled as a single mode and all `*.w5logic` files (inclusive the
subdirectories) will be compiled into a single file `Logic.cs`. An empty
testfile with the name of the directory and `Test.cs` appended to it, will be
generated if none was found.
