#!/bin/bash
# Builds the 51 example projects against PDFjet.dll and runs them.
# Each example writes its PDF to this directory.

for i in $(seq -w 1 51); do
    dotnet build examples/Example_$i/Example_$i.csproj -c release &
done
wait

for i in $(seq -w 1 51); do
    dotnet examples/Example_$i/bin/release/net8.0/Example_$i.dll
done
