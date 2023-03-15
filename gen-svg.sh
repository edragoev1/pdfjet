javac -O -encoding utf-8 -Xlint com/pdfjet/*.java -d out/production
jar cf PDFjet.jar -C out/production .

java -cp .:PDFjet.jar com.pdfjet.SVG $1
