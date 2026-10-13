#!/bin/bash
# Builds the example projects against PDFjet.dll and runs them.

for i in $(seq -w 1 50); do
    dotnet run --project examples/Example_$i -c release
done
