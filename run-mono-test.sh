if [ $# -eq 0 ]; then
    echo "Please provide an example number:"
    echo "./run-mono.sh 33"
    exit 1
fi

mcs -debug -sdk:4 -warn:2 net/pdfjet/*.cs -reference:System.Drawing.dll -target:library -out:PDFjet.dll
mcs -debug -sdk:4 tests/Test_$1.cs -reference:PDFjet.dll
mv tests/Test_$1.exe .
chmod 777 Test_$1.exe
mono --debug Test_$1.exe

# mupdf Test_$1.pdf
evince Test_$1.pdf
