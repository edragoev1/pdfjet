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

# The Swift API reference is built by DocC, which comes with the Swift toolchain,
# from the doc comments in Sources/PDFjet. The pages expect to be served from
# /pdfjet/swift/, as on GitHub Pages.
rm -rf docs/swift
swift package dump-symbol-graph --minimum-access-level public --skip-synthesized-members
docc convert \
    --additional-symbol-graph-dir "$(dirname "$(swift build --show-bin-path)")/symbolgraph" \
    --fallback-display-name PDFjet \
    --fallback-bundle-identifier com.pdfjet.PDFjet \
    --transform-for-static-hosting \
    --hosting-base-path pdfjet/swift \
    --output-path docs/swift
# DocC has no page at its root, so the root redirects to the PDFjet module page.
cat > docs/swift/index.html <<'EOF'
<!doctype html>
<meta charset="utf-8">
<meta http-equiv="refresh" content="0; url=documentation/pdfjet/">
<title>PDFjet for Swift</title>
<a href="documentation/pdfjet/">PDFjet for Swift</a>
EOF
