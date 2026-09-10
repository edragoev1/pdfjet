if [ $# -eq 0 ]; then
    echo "Please provide an example number:"
    echo "./run-java.sh 33"
    exit 1
fi

# Very important!!
./clean.sh

mkdir -p out/production

# Compile the PDFjet library to Java 8 class files, so it runs on Java 8 and later.
javac -O -encoding utf-8 --release 8 -Xlint -Xlint:-options \
    com/pdfjet/*.java \
    com/pdfjet/barcodes/*.java \
    com/pdfjet/pdf417/*.java \
    com/pdfjet/qrcode/*.java \
    com/pdfjet/corefonts/*.java \
    com/pdfjet/fonts/*.java \
    com/pdfjet/encryption/*.java \
    -d out/production

# Compile and run the Example_?? program.
javac -encoding utf-8 -Xlint -cp out/production examples/Example_$1.java -d out/production
java -cp out/production examples.Example_$1

mupdf Example_$1.pdf
# evince Example_$1.pdf
