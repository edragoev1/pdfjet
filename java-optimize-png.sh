rm -f out/production/com/pdfjet/*.class
rm -f out/production/examples/*.class

javac -O -encoding utf-8 -Xlint com/pdfjet/*.java -d out/production
jar cf PDFjet.jar -C out/production .
java -cp .:PDFjet.jar com.pdfjet.OptimizePNG $1
