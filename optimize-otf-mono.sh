mcs -warn:2 -debug -sdk:4 net/pdfjet/*.cs -reference:System.Drawing.dll -target:library -out:PDFjet.dll
mcs -debug -sdk:4 net/pdfjet/OptimizeOTF.cs -reference:PDFjet.dll
mv net/pdfjet/OptimizeOTF.exe .
chmod 777 OptimizeOTF.exe
mono --debug OptimizeOTF.exe $1
