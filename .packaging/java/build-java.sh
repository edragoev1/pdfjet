#!/bin/bash
# Compiles the 51 examples against PDFjet.jar and runs them.
# Each example writes its PDF to this directory.

rm -rf out
mkdir -p out

# --release 8 builds Java 8 class files, as PDFjet.jar is. Java 8's javac has
# no --release option and builds Java 8 class files anyway.
RELEASE="--release 8"
if ! javac --release 8 -version > /dev/null 2>&1; then
    RELEASE=""
fi

javac -encoding utf-8 $RELEASE -Xlint:-options -cp PDFjet.jar examples/Example_*.java -d out || exit 1

for i in $(seq -w 1 51); do
    java -cp PDFjet.jar:out examples.Example_$i
done
