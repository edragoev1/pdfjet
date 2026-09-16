#!/bin/bash
# Builds one example project against PDFjet.dll and runs it.
#
#   ./run-dotnet.sh 33

if [ -z "$1" ]; then
    echo "Usage: ./run-dotnet.sh 33"
    exit 1
fi

dotnet run --project examples/Example_$1 -c release
