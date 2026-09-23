#!/bin/bash
# Builds the example projects against PDFjet.dll and runs them.

for i in $(seq -w 1 55); do
    dotnet run --project examples/Example_$i -c release
done
