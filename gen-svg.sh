javac -encoding utf-8 -Xlint com/pdfjet/*.java
jar cf PDFjet.jar com/pdfjet/*.class

java -cp .:PDFjet.jar com.pdfjet.SVG $1
