# PDF library for Java, C#, Swift and Go developers

This high performance library have no dependencies on external packages and should be usable on the widest variety of plaforms supported by the Java, C#, Swift and Go languages.


```
To build the Java version and compile and run all examples:

./build-java.sh


To build the C# version using .NET and compile and run all examples:

./build-dotnet.sh


To build the Go version and compile and run all examples:

./build-go.sh

## To use the Go library:
```bash
go get github.com/edragoev1/pdfjet@latest


To build the Swift version and compile and run all examples:

./build-swift.sh


To compile and run specific Java example use the following command:

./run-java.sh 01


To compile and run specific C# example use one of the following:

./run-dotnet.sh 01


To compile and run specific Go example:

./run-go.sh 01


To compile and run specific Swift example:

./run-swift.sh 01

Make sure you install these first:
sudo apt install libc6-dev
sudo apt install gcc
```

## Documentation

The API references and the example pages are published at
<https://edragoev1.github.io/pdfjet/>. The `Documentation` GitHub Actions
workflow rebuilds and publishes the site on every push to `master`, so the
generated HTML is not kept in git. This needs the repository's Pages source
(Settings > Pages) set to GitHub Actions.

To build them locally, `./generate-documentation.sh` runs Javadoc for the Java
port into `docs/java`, and [DocFX](https://dotnet.github.io/docfx/) for the C#
port into `docs/_net`. DocFX reads the XML doc comments (`/// <summary>`) in
`net/pdfjet`; its configuration is in `docfx/`. Install DocFX once with
`dotnet tool install -g docfx`.

## Java compatibility

The Java build scripts compile the library with `javac --release 8`, so
`PDFjet.jar` runs on Java 8 and later. Building needs `javac` from JDK 9 or
newer, because Java 8's `javac` has no `--release` option.

## Port differences

The Java, C# and Go ports all support encrypted PDF files. The Swift port does
not: it has no `Encryption`, `Passwords`, `Permissions`, `UserAccess`, `AES128`
or `AES256`, because Swift has no AES-CBC implementation in its standard library
on Linux (swift-crypto only ships AES-GCM), and this library deliberately has no
dependencies on external packages. `Example_30` demonstrates encryption, so it
exists for Java, C# and Go but not for Swift; `build-swift.sh` skips it.

Public setters return the object they were called on, so calls can be chained.
In Java, C# and Swift the `Drawable` interface declares `setLocation` as well as
`drawOn`. In Go it declares only `DrawOn`: a Go type only satisfies an interface
with an exact signature match, and each Go `SetLocation` returns its own type so
it can be chained.
