# PDF library for Java, C#, Swift and Go developers

This high performance library have no dependencies on external packages and should be usable on the widest variety of plaforms supported by the Java, C#, Swift and Go languages.


```
To build the Java version of PDFjet and compile and run all examples:

./build-java.sh


To build the C# version of PDFjet using Mono and compile and run all examples:

./build-mono.sh


To build the Go version of PDFjet and compile and run all examples:

./build-go.sh


To build the Swift version of PDFjet and compile and run all examples:

./build-swift.sh


To compile and run specific Java example (from 01 to 50) use one of the following:

./run-java.sh 07

./run-graalvm.sh 07             (Make sure GraalVM is installed in the correct directory)

./run-graalvm-native-image.sh 07


To compile and run specific C# example use one of the following:

./run-mono.sh 23

./run-dotnet.sh 23


To compile and run specific Go example:

./run-go.sh 05


To compile and run specific Swift example:

./run-swift.sh 15

If you have compile errors please run:
sudo apt install libc6-dev
