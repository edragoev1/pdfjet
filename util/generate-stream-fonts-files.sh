#!/bin/bash
# Compresses the .otf and .ttf fonts of a folder into .stream files, with Zopfli.
#
#   util/generate-stream-fonts-files.sh [--pdfjet-forms-format] fonts/IBMPlexSans
#
# --pdfjet-forms-format writes .otf.stream files as the server of PDFjet Forms
# keeps them, which every version of PDFjet reads: the CFF data of the font
# without its other tables, and no GPOS marks.
#
# fonts is the repository pdfjet-fonts: commit the .stream files there, push
# them, and write the new commit in fonts-and-data.txt of pdfjet.
#
# The generator is in the com.pdfjet package to use the library's OTF parser,
# so it is compiled with the library sources, into util/out and not the jar.

cd "$(dirname "$0")/.." || exit 1
mkdir -p util/out
javac -encoding utf-8 -nowarn -sourcepath . -d util/out \
    util/GenerateStreamFontsFiles.java || exit 1
java -cp util/out com.pdfjet.GenerateStreamFontsFiles "$@"
