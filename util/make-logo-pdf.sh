#!/bin/bash
# Writes data/testPDFs/PDFjetLogo.pdf, the logo of pdfjet.com as the vector
# graphics of one page, from images/readme/pdfjet-logo.svg. Run it from
# anywhere; it builds PDFjet from this checkout.
#
#   util/make-logo-pdf.sh

cd "$(dirname "$0")/.." || exit 1
mkdir -p util/out
javac -nowarn -encoding utf-8 -d util/out com/pdfjet/*.java com/pdfjet/fonts/*.java \
    com/pdfjet/encryption/*.java util/MakeLogoPDF.java || exit 1
java -cp util/out MakeLogoPDF
