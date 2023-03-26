mcs -debug -sdk:4 Example_$1.cs -reference:../PDFjet.dll
mono --debug Example_$1.exe
evince Example_$1.pdf
