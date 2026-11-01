#!/bin/bash
# The folder of this script: run from anywhere else, it deleted the bin, obj,
# *.pdf and *.dll of that folder.
cd "$(dirname "$0")" || exit 1
rm -rf out/production
rm -f net/pdfjet/*.exe.mdb
rm -f examples/*.exe.mdb
rm -f tests/*.exe.mdb
rm -f util/*.class
rm -f *.jar
rm -f javac.*.args
rm -f *.exe
rm -f *.mdb
rm -f *.dll
rm -rf bin
rm -rf obj
rm -rf .build
rm -f *.pdf
rm -rf examples/Example_*/bin
rm -rf examples/Example_*/obj
