#!/bin/bash

# Builds the C# port and its unit tests in tests/dotnet, and runs the tests with
# xUnit. The test project compiles the library sources itself, so the tests
# reach its internal classes, and NuGet restores xUnit for the tests only; the
# library has no dependencies. Warnings fail the build, as in build-dotnet.sh.

cd "$(dirname "$0")" || exit 1

dotnet test tests/dotnet/PDFjet.Tests.csproj -c release -p:TreatWarningsAsErrors=true
