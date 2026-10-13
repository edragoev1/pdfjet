@echo off
REM Builds the example projects of PDFjet Pro against PDFjet.dll and
REM PDFjet.Sign.dll and runs them.

for %%e in (SignDocument WriteInvoice DrawSwissQRBill) do dotnet run --project examples\pro\%%e -c release || exit /b 1
