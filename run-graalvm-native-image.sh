echo "Main-Class: examples.Example_$1" > manifest.txt
javac -encoding utf-8 -Xlint examples/Example_$1.java com/pdfjet/*.java
jar -cvfm Example_$1.jar manifest.txt examples/Example_$1.class com/pdfjet/*.class
native-image -jar Example_$1.jar Example_$1.exe
rm manifest.txt
