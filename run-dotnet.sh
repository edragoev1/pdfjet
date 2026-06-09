#!/bin/bash

# Check if an argument is provided (example number)
if [ -z "$1" ]; then
    echo "Usage: $0 <example_number>"
    exit 1
fi

# Very important!!
rm -rf bin
rm -rf obj

# Build the PDFjet library
dotnet build PDFjet.csproj -c release

dotnet build examples/Example_$1/Example_$1.csproj -c release
dotnet examples/Example_$1/bin/release/net8.0/Example_$1.dll

# Open the generated PDF for the specified Example
mupdf Example_$1.pdf
# evince Example_$1.pdf
