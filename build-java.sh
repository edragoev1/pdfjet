#!/bin/bash
# Stop at the first failure: a library that does not build, or go vet, was
# passed over, and the script gave the exit code of the last example (review
# of 17 October 2026).
set -e
# The folder of this script, which has the files the examples read, from wherever it is run.
cd "$(dirname "$0")" || exit 1
# All the classes, of the subpackages too, so that a class that is removed is
# not left in PDFjet.jar.
rm -rf out/production

mkdir -p out/production

# --release 8 builds Java 8 class files, so PDFjet.jar and the examples run on
# Java 8 and later whichever JDK builds them, and rejects any API newer than
# Java 8. Java 8's javac has no --release option and builds Java 8 class files
# anyway, so the option is left out there.
RELEASE="--release 8"
if ! javac --release 8 -version > /dev/null 2>&1; then
    RELEASE=""
fi

# -Werror fails the build on any warning and then writes no class files.
javac -O -encoding utf-8 $RELEASE -Xlint -Xlint:-options -Werror \
    com/pdfjet/*.java \
    com/pdfjet/barcodes/*.java \
    com/pdfjet/pdf417/*.java \
    com/pdfjet/qrcode/*.java \
    com/pdfjet/datamatrix/*.java \
    com/pdfjet/internal/*.java \
    com/pdfjet/fonts/*.java \
    com/pdfjet/encryption/*.java \
    -d out/production
jar cf PDFjet.jar -C out/production .

pids=()
for i in $(seq 1 50);
do
    if [ $i -lt 10 ]; then
        javac -O -encoding utf-8 $RELEASE -Xlint -Xlint:-options -Werror -cp PDFjet.jar examples/Example_0$i.java -d out/production &
    else
        javac -O -encoding utf-8 $RELEASE -Xlint -Xlint:-options -Werror -cp PDFjet.jar examples/Example_$i.java -d out/production &
    fi
    pids+=($!)
done
# Each compile in the background is waited for on its own, so that one that
# fails stops the script here. By the pids taken as they started: jobs -p does
# not list a job that has ended, so a compile that failed fast was passed over.
for pid in "${pids[@]}"; do
    wait "$pid"
done

for i in $(seq 1 50);
do
    if [ $i -lt 10 ]; then
        java -cp .:PDFjet.jar:out/production examples.Example_0$i
    else
        java -cp .:PDFjet.jar:out/production examples.Example_$i
    fi
done
