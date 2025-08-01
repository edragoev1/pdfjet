rm -f out/production/com/pdfjet/*.class
rm -f out/production/com/pdfjet/font/*.class
rm -f out/production/examples/*.class

mkdir -p out/production

javac -O -encoding utf-8 -Xlint com/pdfjet/*.java com/pdfjet/font/*.java -d out/production
jar cf PDFjet.jar -C out/production .

for i in $(seq 1 50);
do
    if [ $i -lt 10 ]; then
        javac -O -encoding utf-8 -Xlint -cp PDFjet.jar examples/Example_0$i.java -d out/production
    else
        javac -O -encoding utf-8 -Xlint -cp PDFjet.jar examples/Example_$i.java -d out/production
    fi
done

for i in $(seq 1 50);
do
    if [ $i -lt 10 ]; then
        java -cp .:PDFjet.jar:out/production examples.Example_0$i
    else
        java -cp .:PDFjet.jar:out/production examples.Example_$i
    fi
done
