#!/bin/bash

# Very important!!
rm -rf bin
rm -rf obj

# TreatWarningsAsErrors fails the build on any warning and then writes no DLL.
# The -warnaserror switch would still write it, so an example built in the
# background with a warning would run as if nothing had happened.

# Build the PDFjet library project
dotnet build PDFjet.csproj -c release -p:TreatWarningsAsErrors=true

# Build Example_01 to Example_50
for i in {1..50}
do
    if [ $i -lt 10 ]; then
        dotnet build examples/Example_0$i/Example_0$i.csproj -c release -p:TreatWarningsAsErrors=true &
    else
        dotnet build examples/Example_$i/Example_$i.csproj -c release -p:TreatWarningsAsErrors=true &
    fi
done
wait

# Run Example_01 to Example_50
for i in {1..50}
do
    if [ $i -lt 10 ]; then
        dotnet examples/Example_0$i/bin/release/net8.0/Example_0$i.dll
    else
        dotnet examples/Example_$i/bin/release/net8.0/Example_$i.dll
    fi
done
