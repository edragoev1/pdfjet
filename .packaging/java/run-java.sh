#!/bin/bash
# Compiles one example against PDFjet.jar and runs it.
#
#   ./run-java.sh 33

if [ -z "$1" ]; then
    echo "Usage: ./run-java.sh 33"
    exit 1
fi

mkdir -p out
javac -encoding utf-8 -cp PDFjet.jar examples/Example_$1.java -d out || exit 1
java -cp PDFjet.jar:out examples.Example_$1
