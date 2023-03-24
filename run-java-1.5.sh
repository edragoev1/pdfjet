rm -f out/production/com/pdfjet/*.class
rm -f out/production/examples/*.class

# Compile and package the library.
/opt/jdk1.5.0_22/bin/javac -O -encoding utf-8 -Xlint com/pdfjet/*.java -d out/production
/opt/jdk1.5.0_22/bin/jar cf PDFjet.jar -C out/production .

# Compile and run the Example_?? program.
/opt/jdk1.5.0_22/bin/javac -encoding utf-8 -Xlint -cp PDFjet.jar examples/Example_$1.java -d out/production
/opt/jdk1.5.0_22/bin/java -cp PDFjet.jar:out/production examples.Example_$1

# mupdf Example_$1.pdf
evince Example_$1.pdf
