if [ $# -eq 0 ]; then
    echo "Please provide an example number:"
    echo "./run-java.sh 33"
    exit 1
fi

# Very important!!
./clean.sh

mkdir -p out/production

# Compile and package the library.
javac -O -encoding utf-8 -Xlint com/pdfjet/*.java com/pdfjet/font/*.java -d out/production
# jar cf PDFjet.jar -C out/production .

# Compile and run the Example_?? program.
javac -encoding utf-8 -Xlint -cp out/production examples/Example_$1.java -d out/production
java -cp out/production examples.Example_$1

# firefox Example_$1.pdf
# mupdf Example_$1.pdf
evince Example_$1.pdf
