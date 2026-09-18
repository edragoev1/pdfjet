#!/bin/bash
# Compresses the .otf and .ttf fonts of a folder into .stream files, with Zopfli.
#
#   util/generate-stream-fonts-files.sh fonts/IBMPlexSans
#
# The generator is in the com.pdfjet package to use the library's OTF parser,
# so it is compiled with the library sources, into util/out and not the jar.

cd "$(dirname "$0")/.." || exit 1
mkdir -p util/out
javac -encoding utf-8 -nowarn -sourcepath . -d util/out \
    util/GenerateStreamFontsFiles.java || exit 1
java -cp util/out com.pdfjet.GenerateStreamFontsFiles "$1"
