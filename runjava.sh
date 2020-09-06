#!/bin/sh

# /opt/jdk1.5.0_22/bin/javac -encoding utf-8 -Xlint com/pdfjet/*.java
# /opt/jdk1.5.0_22/bin/jar cf PDFjet.jar com/pdfjet/*.class
# /opt/jdk1.5.0_22/bin/javac -encoding utf-8 -Xlint -cp .:PDFjet.jar $1.java
# /opt/jdk1.5.0_22/bin/javac -cp .:PDFjet.jar $1.java
# /opt/jdk1.5.0_22/bin/java -cp .:PDFjet.jar $1 $2

# Compile and package the library.
javac -encoding utf-8 -Xlint com/pdfjet/*.java
jar cf PDFjet.jar com/pdfjet/*.class

# Compile and run the Example_?? program.
javac -encoding utf-8 -Xlint -cp .:PDFjet.jar examples/Example_$1.java
java -cp .:PDFjet.jar examples.Example_$1 $2

# mupdf-gl Example_$1.pdf
evince Example_$1.pdf
