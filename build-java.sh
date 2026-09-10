rm -f out/production/com/pdfjet/*.class
rm -f out/production/com/pdfjet/fonts/*.class
rm -f out/production/examples/*.class

mkdir -p out/production

# --release 8 builds Java 8 class files, so PDFjet.jar runs on Java 8 and later.
javac -O -encoding utf-8 --release 8 -Xlint -Xlint:-options \
    com/pdfjet/*.java \
    com/pdfjet/barcodes/*.java \
    com/pdfjet/pdf417/*.java \
    com/pdfjet/qrcode/*.java \
    com/pdfjet/fonts/*.java \
    com/pdfjet/encryption/*.java \
    -d out/production
jar cf PDFjet.jar -C out/production .

for i in $(seq 1 50);
do
    if [ $i -lt 10 ]; then
        javac -O -encoding utf-8 -Xlint -cp PDFjet.jar examples/Example_0$i.java -d out/production &
    else
        javac -O -encoding utf-8 -Xlint -cp PDFjet.jar examples/Example_$i.java -d out/production &
    fi
done
wait

for i in $(seq 1 50);
do
    if [ $i -lt 10 ]; then
        java -cp .:PDFjet.jar:out/production examples.Example_0$i
    else
        java -cp .:PDFjet.jar:out/production examples.Example_$i
    fi
done
