# Compile and package the library.
javac -O -encoding utf-8 -Xlint com/pdfjet/*.java -d out/production
jar cf PDFjet.jar -C out/production .

# Compile and run the Example_?? program.
javac -encoding utf-8 -Xlint -cp PDFjet.jar examples/Example_$1.java
java -cp .:PDFjet.jar examples.Example_$1

mupdf Example_$1.pdf
