#!/bin/bash
# Compresses the .otf and .ttf fonts of a folder into .stream files, with Zopfli.
#
#   util/generate-stream-fonts-files.sh fonts/IBMPlexSans

cd "$(dirname "$0")/.." || exit 1
mkdir -p util/out
javac -d util/out util/GenerateStreamFontsFiles.java || exit 1
java -cp util/out GenerateStreamFontsFiles "$1"
