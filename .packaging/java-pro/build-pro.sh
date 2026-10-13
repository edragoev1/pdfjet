#!/bin/bash
# Compiles the examples of PDFjet Pro against PDFjet.jar and PDFjet-Sign.jar and
# runs them.

rm -rf out-pro
mkdir out-pro
javac -encoding utf-8 -cp PDFjet.jar:PDFjet-Sign.jar examples/pro/*.java -d out-pro || exit 1

for example in SignDocument WriteInvoice DrawSwissQRBill; do
    java -cp PDFjet.jar:PDFjet-Sign.jar:out-pro $example || exit 1
done
