rm out/production/com/pdfjet/*.class
rm out/production/examples/*.class

# Compile and package the library.
javac -O -encoding utf-8 -Xlint com/pdfjet/*.java -d out/production
jar cf PDFjet.jar -C out/production .

# Compile and run the Example_?? program.
javac -encoding utf-8 -Xlint -cp PDFjet.jar examples/Example_$1.java -d out/production
java -cp PDFjet.jar:out/production examples.Example_$1

# firefox Example_$1.pdf
# mupdf Example_$1.pdf
evince Example_$1.pdf
