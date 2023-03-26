mcs -debug -sdk:4 Test_$1.cs -reference:../PDFjet.dll
mono --debug Test_$1.exe
evince Test_$1.pdf
