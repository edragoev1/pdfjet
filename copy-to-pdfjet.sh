cp -R com/pdfjet/*.java ../pdfjet/com/pdfjet
rm ../pdfjet/com/pdfjet/BigTable.java

cp -R net/pdfjet/*.cs ../pdfjet/net/pdfjet
rm ../pdfjet/net/pdfjet/BigTable.cs

cp -R Sources/* ../pdfjet/Sources
rm ../pdfjet/Sources/PDFjet/BigTable.swift

cp -R src/* ../pdfjet/src
rm ../pdfjet/src/bigtable.go

cp -R examples/*.java ../pdfjet/examples
cp -R examples/*.cs ../pdfjet/examples

rm -f ../pdfjet/examples/Example_52.java
rm -f ../pdfjet/examples/Example_53.java
rm -f ../pdfjet/examples/Example_52.cs
rm -f ../pdfjet/examples/Example_53.cs
