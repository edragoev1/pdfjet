#!/bin/bash
# Writes the PDFjet booklets with PDFjet: one for each port, with the code of
# that port, and one with the code of the four ports.
#
#   booklet/build.sh                 all five booklets
#   booklet/build.sh java all        the Java one and the one with all four
#
# The editions are java, csharp, go, swift and all. The booklets are written to
# booklet/PDFjet-Booklet-*.pdf.

bash "$(dirname "$0")/../get-fonts-and-data.sh" || exit 1
cd "$(dirname "$0")/.." || exit 1

# --release 8 builds Java 8 class files, as the library's own scripts do. Java
# 8's javac has no --release option and builds them anyway.
RELEASE="--release 8"
if ! javac --release 8 -version > /dev/null 2>&1; then
    RELEASE=""
fi

rm -rf build/booklet
mkdir -p build/booklet

# What each snippet draws, shown under its code: page 1 of the PDF the Java
# snippets write, as booklet/check-snippets.sh leaves them, made by mutool into
# an SVG, which the booklet draws as vectors with PDFjet's SVGImage, the area
# drawn on, and a picture of the page, for the drawings SVGImage cannot draw.
# The four ports draw the same, as check-snippets.sh checks.
SNIPPETS=build/check-snippets/java/out
DRAWINGS=build/booklet/drawings
if ! command -v mutool > /dev/null; then
    echo "The booklet needs mutool, of MuPDF, for the drawings of the snippets."
    exit 1
fi
if [ ! -d "$SNIPPETS" ]; then
    echo "Run booklet/check-snippets.sh first: the booklet shows what the snippets draw."
    exit 1
fi
mkdir -p "$DRAWINGS"
for name in $(sed -n 's/^@snippet \([a-z0-9-]*\) .*/\1/p' booklet/content.txt); do
    # The snippets that write a file of another name, and those whose result
    # is a document rather than a drawing: merged, split, stamped or
    # encrypted.
    case $name in
        accessible-document) file=accessible ;;
        archival-document) file=archival ;;
        merge | split | existing-pages | encryption) continue ;;
        *) file=$name ;;
    esac
    pdf="$SNIPPETS/$file.pdf"
    if [ ! -f "$pdf" ]; then
        echo "$pdf is missing: run booklet/check-snippets.sh."
        exit 1
    fi
    mutool draw -q -F bbox -o "$DRAWINGS/$name.bbox" "$pdf" 1 || exit 1
    mutool draw -q -F svg -o "$DRAWINGS/$name.svg" "$pdf" 1 || exit 1
    mutool draw -q -r 200 -o "$DRAWINGS/$name.png" "$pdf" 1 || exit 1
done
javac -encoding utf-8 $RELEASE -Xlint -Xlint:-options -d build/booklet \
    com/pdfjet/*.java \
    com/pdfjet/barcodes/*.java \
    com/pdfjet/pdf417/*.java \
    com/pdfjet/qrcode/*.java \
    com/pdfjet/datamatrix/*.java \
    com/pdfjet/internal/*.java \
    com/pdfjet/fonts/*.java \
    com/pdfjet/encryption/*.java \
    booklet/Booklet.java || exit 1
java -cp build/booklet Booklet "$@"
