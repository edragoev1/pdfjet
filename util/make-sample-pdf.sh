#!/bin/bash
# Writes resources/data/testPDFs/sample-document.pdf, a report of two pages of
# a made-up company, which Example_49 merges and Example_50 stamps. Run it from
# anywhere; it builds PDFjet from this checkout. Its output does not change
# from run to run, as long as PDFjet and the JDK's compression do not.
#
#   util/make-sample-pdf.sh

cd "$(dirname "$0")/.." || exit 1
mkdir -p util/out
javac -nowarn -encoding utf-8 -d util/out com/pdfjet/*.java com/pdfjet/fonts/*.java \
    com/pdfjet/encryption/*.java com/pdfjet/internal/*.java util/MakeSamplePDF.java || exit 1
java -cp util/out:resources/fonts MakeSamplePDF
