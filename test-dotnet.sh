#!/bin/bash

# Builds the C# port and its unit tests in tests/dotnet, and runs the tests with
# xUnit. The test project compiles the library sources itself, so the tests
# reach its internal classes, and NuGet restores xUnit for the tests only; the
# library has no dependencies. Warnings fail the build, as in build-dotnet.sh.
cd "$(dirname "$0")" || exit 1

# The tests run with the small library, which reads the fonts PDFjet carries
# from resources/fonts, as built from the repository and for the releases on
# GitHub; then those of BundledFontsTest again with the fonts inside the
# library, as the DLL of NuGet is built (-p:PDFjetFonts=true). The constant
# PDFJET_FONTS it then has rebuilds the test assembly at each switch.
#
# With the .NET 10 SDK, the tests run on .NET 8 and on .NET 10 (see
# PDFjet.Tests.csproj), one after the other, not at once, so that the tests
# that time the reader are not slowed by the other run.
set -e
dotnet test tests/dotnet/PDFjet.Tests.csproj -c release -p:TreatWarningsAsErrors=true -p:TestTfmsInParallel=false
dotnet test tests/dotnet/PDFjet.Tests.csproj -c release -p:TreatWarningsAsErrors=true -p:TestTfmsInParallel=false -p:PDFjetFonts=true \
    --filter "FullyQualifiedName~BundledFontsTest"
