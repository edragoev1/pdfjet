if [ $# -eq 0 ]; then
    echo "Please provide an example number:"
    echo "./run-mono.sh 33"
    exit 1
fi

# Very important!!
./clean.sh

mcs -debug -sdk:4 -warn:2 net/pdfjet/*.cs -reference:System.Drawing.dll -target:library -out:PDFjet.dll
mcs -debug -sdk:4 examples/Example_$1.cs -reference:PDFjet.dll
mv examples/Example_$1.exe .
chmod 777 Example_$1.exe
mono --debug Example_$1.exe

# mupdf Example_$1.pdf
evince Example_$1.pdf
