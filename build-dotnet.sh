#!/bin/bash

# Stop at the first failure: a library that does not build, or go vet, was
# passed over, and the script gave the exit code of the last example (review
# of 17 October 2026).
set -e
# The folder of this script, which has the files the examples read, from wherever it is run.
cd "$(dirname "$0")" || exit 1
rm -rf bin
rm -rf obj

# TreatWarningsAsErrors fails the build on any warning and then writes no DLL.
# The -warnaserror switch would still write it, so an example built in the
# background with a warning would run as if nothing had happened.

# Build the PDFjet library project
dotnet build PDFjet.csproj -c release -p:TreatWarningsAsErrors=true

# Build Example_01 to Example_06
pids=()
for i in {1..50}
do
    if [ $i -lt 10 ]; then
        dotnet build examples/Example_0$i/Example_0$i.csproj -c release -p:TreatWarningsAsErrors=true &
    else
        dotnet build examples/Example_$i/Example_$i.csproj -c release -p:TreatWarningsAsErrors=true &
    fi
    pids+=($!)
done
# Each compile in the background is waited for on its own, so that one that
# fails stops the script here. By the pids taken as they started: jobs -p does
# not list a job that has ended, so a compile that failed fast was passed over.
for pid in "${pids[@]}"; do
    wait "$pid"
done

# Run Example_01 to Example_06
for i in {1..50}
do
    if [ $i -lt 10 ]; then
        dotnet examples/Example_0$i/bin/release/net8.0/Example_0$i.dll
    else
        dotnet examples/Example_$i/bin/release/net8.0/Example_$i.dll
    fi
done
