mcs -warn:2 -debug -sdk:4 net/pdfjet/*.cs -reference:System.Drawing.dll -target:library -out:PDFjet.dll
mcs -debug -sdk:4 net/pdfjet/OptimizePNG.cs -reference:PDFjet.dll
mv net/pdfjet/OptimizePNG.exe .
chmod 777 OptimizePNG.exe
mono --debug OptimizePNG.exe $1
