#!/bin/bash
# Compiles the examples against PDFjet.jar and runs them.

rm -rf out
mkdir out
javac -encoding utf-8 -cp PDFjet.jar examples/*.java -d out || exit 1

for i in $(seq -w 1 51); do
    java -cp PDFjet.jar:out examples.Example_$i
done
