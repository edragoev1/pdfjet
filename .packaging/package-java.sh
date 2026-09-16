#!/bin/bash
# Builds .commercial-packages/PDFjet-ForJava-vX.Y.Z.zip, a self-contained
# package for Java clients: PDFjet.jar, the Javadoc reference, the examples
# with the PDFs they create, the files they read (data, fonts, images,
# PngSuite), the font tools in util, and scripts that build and run the
# examples against PDFjet.jar.
# The library sources are not in the package.
#
# The build and run scripts of the package are in .packaging/java.
#
# The package is made from the last commit, not the working tree. Its PDFs are
# created by its own build-java.sh, so the scripts a client runs are tested.
# PDFjet.jar is built for Java 8, as the build scripts of the repository are.

# Stop at the first failure, so a broken build is never zipped.
set -e

cd "$(dirname "$0")/.."

VERSION=$(sed -n 's/.*producer = "PDFjet \(v[0-9.]*\)".*/\1/p' com/pdfjet/PDF.java)
if [ -z "$VERSION" ]; then
    echo "Could not read the version from com/pdfjet/PDF.java"
    exit 1
fi
NAME="PDFjet-ForJava-$VERSION"
STAGE="build/package-java/$NAME"
ZIP="$PWD/.commercial-packages/$NAME.zip"

if [ -n "$(git status --porcelain)" ]; then
    echo "Warning: uncommitted changes are left out; the package is made from $(git rev-parse --short HEAD)."
fi

rm -rf "$STAGE" "$ZIP"
mkdir -p "$STAGE" .commercial-packages

git archive HEAD \
    .packaging/java com examples data fonts images PngSuite util \
    README.md LICENSE CHANGELOG.md THIRD-PARTIES.TXT examples-java.html \
    | tar -x -C "$STAGE"

# The C# example projects and the go.mod files of the Go port are not needed.
find "$STAGE/examples" -mindepth 1 -maxdepth 1 -type d -exec rm -rf {} +
rm -f "$STAGE/data/go.mod" "$STAGE/fonts/go.mod" "$STAGE/images/go.mod"

cd "$STAGE"

# The same javac options as build-java.sh.
RELEASE="--release 8"
if ! javac --release 8 -version > /dev/null 2>&1; then
    RELEASE=""
fi
javac -O -encoding utf-8 $RELEASE -Xlint -Xlint:-options -Werror \
    com/pdfjet/*.java \
    com/pdfjet/barcodes/*.java \
    com/pdfjet/pdf417/*.java \
    com/pdfjet/qrcode/*.java \
    com/pdfjet/datamatrix/*.java \
    com/pdfjet/fonts/*.java \
    com/pdfjet/encryption/*.java \
    -d out/library
jar cf PDFjet.jar -C out/library .

# The same javadoc command as generate-documentation.sh.
javadoc -quiet -public -doctitle "PDFjet for Java" -windowtitle "PDFjet for Java" \
    com/pdfjet/*.java \
    com/pdfjet/barcodes/*.java \
    com/pdfjet/encryption/*.java \
    com/pdfjet/fonts/*.java \
    com/pdfjet/pdf417/*.java \
    com/pdfjet/qrcode/*.java \
    com/pdfjet/datamatrix/*.java \
    -d docs/java

rm -rf com out

# The build and run scripts of the package build the examples against
# PDFjet.jar; the scripts of the repository build the library from its sources.
mv .packaging/java/* .
rm -rf .packaging

# Creates the PDFs of the package with its own script.
./build-java.sh
rm -rf out

# An example that fails can leave a PDF it had begun, so each must be complete.
for i in $(seq -w 1 51); do
    if ! tail -c 32 "Example_$i.pdf" 2>/dev/null | grep -q '%%EOF'; then
        echo "Example_$i.pdf was not created or is incomplete"
        exit 1
    fi
done

cd ..
zip -q -r -9 "$ZIP" "$NAME"
cd ../..
rm -rf build/package-java

echo "Created $ZIP"
