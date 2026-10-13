#!/bin/bash
# Builds the example projects of PDFjet Pro against PDFjet.dll and
# PDFjet.Sign.dll and runs them.

for example in SignDocument WriteInvoice DrawSwissQRBill; do
    dotnet run --project examples/pro/$example -c release || exit 1
done
