rm -f out/production/com/pdfjet/*.class
rm -f out/production/examples/*.class

# Optimize TTF or OTF font by converting it to .ttf.stream or .otf.stream
# These stream fonts can be enbedded in PDFs much faster
javac -O -encoding utf-8 -Xlint com/pdfjet/*.java -d out/production
jar cf PDFjet.jar -C out/production .
java --class-path PDFjet.jar:out/production com.pdfjet.OptimizeOTF $1
