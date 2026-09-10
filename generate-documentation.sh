rm -f docs/java/com/pdfjet/*.html
rm -f docs/java/*.html

javadoc -public -doctitle "PDFjet for Java" -windowtitle "PDFjet for Java" com/pdfjet/*.java -d docs/java

# The C# API reference is built by DocFX from the XML doc comments in net/pdfjet.
# Install it once with: dotnet tool install -g docfx
rm -rf docs/_net docfx/api
docfx docfx/docfx.json
