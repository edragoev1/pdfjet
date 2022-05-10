# Compile and package the PDFjet library.
/opt/jdk-17.0.3+7_openj9/bin/javac -encoding utf-8 -Xlint com/pdfjet/*.java
/opt/jdk-17.0.3+7_openj9/bin/jar cf PDFjet.jar com/pdfjet/*.class

# Compile and run the Example_?? program.
/opt/jdk-17.0.3+7_openj9/bin/javac -encoding utf-8 -Xlint -cp .:PDFjet.jar examples/Example_$1.java
/opt/jdk-17.0.3+7_openj9/bin/java -cp .:PDFjet.jar examples.Example_$1

mupdf Example_$1.pdf
