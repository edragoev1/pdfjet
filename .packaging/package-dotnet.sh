#!/bin/bash
# Builds .commercial-packages/PDFjet-For.NET-vX.Y.Z.zip, a self-contained
# package for .NET clients: PDFjet.dll, the DocFX reference, the example
# projects with the PDFs they create, the files they read (data, the .stream
# fonts, images, PngSuite), and scripts that build and run the examples against PDFjet.dll.
# The library sources and the font tools in util are not in the package.
#
# The README, the commercial LICENSE and the build and run scripts of the
# package are in .packaging/dotnet.
#
# It also builds .commercial-packages/PDFjet-For.NET-Eval-vX.Y.Z.zip, the
# evaluation package: the same files, with the evaluation license agreement of
# .packaging/LICENSE-EVALUATION as its LICENSE.
#
# The package is made from the last commit, not the working tree. Its PDFs are
# created by its own build-dotnet.sh, so the scripts a client runs are tested.
# Building the reference needs DocFX: dotnet tool install -g docfx

# PDFjet.dll is strong-named, in both packages: the key is not in the
# repository, but in the owner's private files, and is given by PDFJET_SNK:
#
#   PDFJET_SNK=~/Projects/pdfjet-pro-private/signing/PDFjet.snk .packaging/package-dotnet.sh
#
# Without it the script stops, so that no package is made unsigned. Its public
# key token is e66c1909913f295d.

# Stop at the first failure, so a broken build is never zipped.
set -e

if [ -z "$PDFJET_SNK" ] || [ ! -f "$PDFJET_SNK" ]; then
    echo "PDFJET_SNK must name the strong-name key, PDFjet.snk, to sign PDFjet.dll."
    exit 1
fi
PDFJET_SNK=$(cd "$(dirname "$PDFJET_SNK")" && pwd)/$(basename "$PDFJET_SNK")

cd "$(dirname "$0")/.."

VERSION=$(sed -n 's/.*producer = "PDFjet \(v[0-9.]*\)".*/\1/p' net/pdfjet/PDF.cs)
if [ -z "$VERSION" ]; then
    echo "Could not read the version from net/pdfjet/PDF.cs"
    exit 1
fi
NAME="PDFjet-For.NET-$VERSION"
STAGE="build/package-dotnet/$NAME"
ZIP="$PWD/.commercial-packages/$NAME.zip"
EVAL_NAME="PDFjet-For.NET-Eval-$VERSION"
EVAL_ZIP="$PWD/.commercial-packages/$EVAL_NAME.zip"

if [ -n "$(git status --porcelain)" ]; then
    echo "Warning: uncommitted changes are left out; the package is made from $(git rev-parse --short HEAD)."
fi

rm -rf "$(dirname "$STAGE")" "$ZIP" "$EVAL_ZIP"
mkdir -p "$STAGE" .commercial-packages

# Through a file, not a pipe, so that a failing git archive stops the script.
TAR=$PWD/$STAGE.tar
git archive -o "$TAR" HEAD \
    .packaging/dotnet .packaging/LICENSE-EVALUATION \
    net net48 PDFjet.csproj examples images PngSuite docfx \
    CHANGELOG.md THIRD-PARTIES.TXT examples-dotnet.html
tar -x -C "$STAGE" -f "$TAR"

# fonts and data are the repositories pdfjet-fonts and pdfjet-data, which
# get-fonts-and-data.sh fetches and git archive does not have: each is
# archived at the commit that fonts-and-data.txt of the last commit pins,
# whatever commit the folder is at, and its README, about the repository, is
# left out.
bash get-fonts-and-data.sh
for dir in fonts data; do
    commit=$(git show HEAD:fonts-and-data.txt | sed -n "s/^$dir [^ ]* \([0-9a-f]*\).*/\1/p")
    if [ -z "$commit" ] || ! git -C "$dir" cat-file -e "$commit^{commit}" 2> /dev/null; then
        echo "$dir does not have the commit ${commit:-that} fonts-and-data.txt of the last commit pins: run get-fonts-and-data.sh"
        exit 1
    fi
    git -C "$dir" archive -o "$TAR" --prefix="$dir/" "$commit"
    tar -x -C "$STAGE" -f "$TAR"
    rm -f "$STAGE/$dir/README.md"
done
rm "$TAR"

# The Java examples and the go.mod file of the Go port are not needed, but
# Example_32 draws the source of Example_02.java.
find "$STAGE/examples" -maxdepth 1 -name '*.java' ! -name Example_02.java -delete
rm -f "$STAGE/images/go.mod"

# The fonts of the package are the fonts directory as it is: the .ttf files,
# which PDFjet subsets, and IBM Plex Sans as .otf too, which Examples 28, 37
# and 50 read.

cd "$STAGE"

# The same build as build-dotnet.sh, strong-named, with the release as the file
# version: 9.0.3 of v9.0.3.
dotnet build PDFjet.csproj -c release -p:TreatWarningsAsErrors=true \
    -p:SignAssembly=true -p:AssemblyOriginatorKeyFile="$PDFJET_SNK" \
    -p:FileVersion="${VERSION#v}" -p:InformationalVersion="${VERSION#v}"
cp bin/release/net8.0/PDFjet.dll .

# The same library for .NET Framework 4.8, from the same sources, strong-named
# with the same key and version: net48/PDFjet.dll and the System.Memory DLLs
# beside it (see net48/README.md of the repository). It needs the .NET 10 SDK,
# which net48/build.sh finds.
net48/build.sh -p:SignAssembly=true -p:AssemblyOriginatorKeyFile="$PDFJET_SNK" \
    -p:FileVersion="${VERSION#v}" -p:InformationalVersion="${VERSION#v}"
mkdir -p net48-dll
cp net48/bin/release/net48/*.dll net48-dll/
rm -rf net48
mv net48-dll net48

# The same docfx command as generate-documentation.sh; it writes docs/dotnet.
docfx docfx/docfx.json

rm -rf net PDFjet.csproj bin obj docfx

# The example projects reference the PDFjet.dll of the package.
# perl, as sed -i is not the same in GNU sed and in the sed of macOS.
perl -pi -e 's|<HintPath>../../bin/release/net8.0/PDFjet.dll</HintPath>|<HintPath>../../PDFjet.dll</HintPath>|' \
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
mv .packaging/LICENSE-EVALUATION ..
rm -rf .packaging

# Creates the PDFs of the package with its own script.
./build-dotnet.sh
rm -rf examples/Example_*/bin examples/Example_*/obj

# An example that fails can leave a PDF it had begun, so each must be complete.
for i in $(seq -w 1 57); do
    if ! tail -c 32 "Example_$i.pdf" 2>/dev/null | grep -q '%%EOF'; then
        echo "Example_$i.pdf was not created or is incomplete"
        exit 1
    fi
done

cd ..
zip -q -r -9 "$ZIP" "$NAME"

# The evaluation package is the same package under the evaluation license.
mv "$NAME" "$EVAL_NAME"
mv LICENSE-EVALUATION "$EVAL_NAME/LICENSE"
zip -q -r -9 "$EVAL_ZIP" "$EVAL_NAME"
cd ../..
rm -rf build/package-dotnet

# The evaluation package again, without the version in its name, for the
# GitHub release: the website links to releases/latest/download/PDFjet-For.NET-Eval.zip,
# which GitHub sends to the latest release's file of that name, so that the
# link never changes. The version is in the name of the folder inside.
LATEST_ZIP="$(dirname "$EVAL_ZIP")/PDFjet-For.NET-Eval.zip"
cp "$EVAL_ZIP" "$LATEST_ZIP"

echo "Created $ZIP"
echo "Created $EVAL_ZIP"
echo "Created $LATEST_ZIP, the same, for the GitHub release"
