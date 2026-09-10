rm -rf docs/java

javadoc -public -doctitle "PDFjet for Java" -windowtitle "PDFjet for Java" \
    com/pdfjet/*.java \
    com/pdfjet/barcodes/*.java \
    com/pdfjet/corefonts/*.java \
    com/pdfjet/encryption/*.java \
    com/pdfjet/fonts/*.java \
    com/pdfjet/pdf417/*.java \
    com/pdfjet/qrcode/*.java \
    -d docs/java

# The C# API reference is built by DocFX from the XML doc comments in net/pdfjet.
# Install it once with: dotnet tool install -g docfx
rm -rf docs/_net docfx/api
docfx docfx/docfx.json

# The Go API reference is built by doc2go from the doc comments in src.
# The example programs in src/examples are left out.
# Install it once with: go install go.abhg.dev/doc2go@v0.12.2
rm -rf docs/go
doc2go -out docs/go -home github.com/edragoev1/pdfjet/src -rel-link-style index \
    $(go list ./src/... | grep -v /examples/)
