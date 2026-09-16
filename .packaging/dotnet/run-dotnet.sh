#!/bin/bash
# Builds one example project against PDFjet.dll, runs it and opens its PDF.
#
#   ./run-dotnet.sh 33

if [ -z "$1" ]; then
    echo "Please provide an example number:"
    echo "./run-dotnet.sh 33"
    exit 1
fi

dotnet build examples/Example_$1/Example_$1.csproj -c release || exit 1
dotnet examples/Example_$1/bin/release/net8.0/Example_$1.dll || exit 1

if command -v xdg-open > /dev/null; then
    xdg-open Example_$1.pdf
elif command -v open > /dev/null; then
    open Example_$1.pdf
fi
