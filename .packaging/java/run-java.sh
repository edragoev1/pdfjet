#!/bin/bash
# Compiles one example against PDFjet.jar, runs it and opens its PDF.
#
#   ./run-java.sh 33

if [ $# -eq 0 ]; then
    echo "Please provide an example number:"
    echo "./run-java.sh 33"
    exit 1
fi

mkdir -p out

RELEASE="--release 8"
if ! javac --release 8 -version > /dev/null 2>&1; then
    RELEASE=""
fi

javac -encoding utf-8 $RELEASE -Xlint:-options -cp PDFjet.jar examples/Example_$1.java -d out || exit 1
java -cp PDFjet.jar:out examples.Example_$1 || exit 1

if command -v xdg-open > /dev/null; then
    xdg-open Example_$1.pdf
elif command -v open > /dev/null; then
    open Example_$1.pdf
fi
