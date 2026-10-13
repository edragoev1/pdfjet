#!/bin/bash
# Compiles the examples of PDFjet Pro against PDFjet.jar, which has PDFjet Pro
# in it, and runs them.

rm -rf out-pro
mkdir out-pro
javac -encoding utf-8 -cp PDFjet.jar examples/pro/*.java -d out-pro || exit 1

for example in SignDocument WriteInvoice DrawSwissQRBill; do
    java -cp PDFjet.jar:out-pro $example || exit 1
done
