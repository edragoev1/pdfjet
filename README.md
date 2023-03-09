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

./run-graalvm-native-image.sh 07 (Make sure GraalVM is installed in the correct directory)


To compile and run specific C# example use one of the following:

./run-mono.sh 23

./run-dotnet.sh 23


To compile and run specific Go example:

./run-go.sh 05


To compile and run specific Swift example:

./run-swift.sh 15

NOTE: On freshly installed Ubuntu 22.04 I got two errors:

fatal error: sys/types.h: No such file or directory

This was fixed by running:

sudo apt install libc6-dev

Then I got this error:

error: invalid linker name in argument '-fuse-ld=gold'

I fixed this by installing the GCC:

sudo apt install gcc
