#!/bin/bash
# Builds .commercial-packages/PDFjet-For.NET-vX.Y.Z.zip, a self-contained
# package for .NET clients: PDFjet.dll, the DocFX reference, the example
# projects with the PDFs they create, the files they read (data, fonts, images,
# PngSuite), and scripts that build and run the examples against PDFjet.dll.
# The library sources and the font tools in util are not in the package.
#
# The README, the commercial LICENSE and the build and run scripts of the
# package are in .packaging/dotnet.
#
# The package is made from the last commit, not the working tree. Its PDFs are
# created by its own build-dotnet.sh, so the scripts a client runs are tested.
# Building the reference needs DocFX: dotnet tool install -g docfx

# Stop at the first failure, so a broken build is never zipped.
set -e

cd "$(dirname "$0")/.."

VERSION=$(sed -n 's/.*producer = "PDFjet \(v[0-9.]*\)".*/\1/p' net/pdfjet/PDF.cs)
if [ -z "$VERSION" ]; then
    echo "Could not read the version from net/pdfjet/PDF.cs"
    exit 1
fi
NAME="PDFjet-For.NET-$VERSION"
STAGE="build/package-dotnet/$NAME"
ZIP="$PWD/.commercial-packages/$NAME.zip"

if [ -n "$(git status --porcelain)" ]; then
    echo "Warning: uncommitted changes are left out; the package is made from $(git rev-parse --short HEAD)."
fi

rm -rf "$STAGE" "$ZIP"
mkdir -p "$STAGE" .commercial-packages

git archive HEAD \
    .packaging/dotnet net PDFjet.csproj examples data fonts images PngSuite docfx \
    CHANGELOG.md THIRD-PARTIES.TXT examples-dotnet.html \
    | tar -x -C "$STAGE"

# The Java examples and the go.mod files of the Go port are not needed, but
# Example_32 draws the source of Example_02.java.
find "$STAGE/examples" -maxdepth 1 -name '*.java' ! -name Example_02.java -delete
rm -f "$STAGE/data/go.mod" "$STAGE/fonts/go.mod" "$STAGE/images/go.mod"

cd "$STAGE"

# The same build as build-dotnet.sh.
dotnet build PDFjet.csproj -c release -p:TreatWarningsAsErrors=true
cp bin/release/net8.0/PDFjet.dll .

# The same docfx command as generate-documentation.sh; it writes docs/dotnet.
docfx docfx/docfx.json

rm -rf net PDFjet.csproj bin obj docfx

# The example projects reference the PDFjet.dll of the package.
sed -i 's|<HintPath>../../bin/release/net8.0/PDFjet.dll</HintPath>|<HintPath>../../PDFjet.dll</HintPath>|' \
    examples/Example_*/Example_*.csproj
if grep -L '<HintPath>../../PDFjet.dll</HintPath>' examples/Example_*/Example_*.csproj | grep -q .; then
    echo "An example project does not reference ../../PDFjet.dll"
    exit 1
fi

# The LICENSE of the package is the commercial license agreement of
# pdfjet.com, its README is about the prebuilt library, and its build and run
# scripts build the examples against PDFjet.dll, where those of the repository
# build the library from its sources.
mv .packaging/dotnet/* .
rm -rf .packaging

# Creates the PDFs of the package with its own script.
./build-dotnet.sh
rm -rf examples/Example_*/bin examples/Example_*/obj

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
rm -rf build/package-dotnet

echo "Created $ZIP"
