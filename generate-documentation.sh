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
