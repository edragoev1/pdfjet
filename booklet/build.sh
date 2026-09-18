#!/bin/bash
# Writes the PDFjet booklets with PDFjet: one for each port, with the code of
# that port, and one with the code of the four ports.
#
#   booklet/build.sh                 all five booklets
#   booklet/build.sh java all        the Java one and the one with all four
#
# The editions are java, csharp, go, swift and all. The booklets are written to
# booklet/PDFjet-Booklet-*.pdf.

cd "$(dirname "$0")/.." || exit 1

# --release 8 builds Java 8 class files, as the library's own scripts do. Java
# 8's javac has no --release option and builds them anyway.
RELEASE="--release 8"
if ! javac --release 8 -version > /dev/null 2>&1; then
    RELEASE=""
fi

rm -rf build/booklet
mkdir -p build/booklet
javac -encoding utf-8 $RELEASE -Xlint -Xlint:-options -d build/booklet \
    com/pdfjet/*.java \
    com/pdfjet/barcodes/*.java \
    com/pdfjet/pdf417/*.java \
    com/pdfjet/qrcode/*.java \
    com/pdfjet/datamatrix/*.java \
    com/pdfjet/fonts/*.java \
    com/pdfjet/encryption/*.java \
    booklet/Booklet.java || exit 1
java -cp build/booklet Booklet "$@"
