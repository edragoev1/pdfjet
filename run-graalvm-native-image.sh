echo "Main-Class: examples.Example_$1" > manifest.txt
/opt/graalvm-ce-java17-22.1.0/bin/javac -encoding utf-8 -Xlint examples/Example_$1.java com/pdfjet/*.java
/opt/graalvm-ce-java17-22.1.0/bin/jar -cvfm Example_$1.jar manifest.txt examples/Example_$1.class com/pdfjet/*.class
/opt/graalvm-ce-java17-22.1.0/bin/native-image -jar Example_$1.jar Example_$1.exe
rm manifest.txt
rm *.exe.build_artifacts.txt
