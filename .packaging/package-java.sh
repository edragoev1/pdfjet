#!/bin/bash
# Builds .commercial-packages/PDFjet-ForJava-vX.Y.Z.zip, a self-contained
# package for Java clients: PDFjet.jar, the Javadoc reference, the examples
# with the PDFs they create, the files they read (data, the fonts,
# images, PngSuite), and scripts that build and run the examples against PDFjet.jar.
# The library sources and the font tools in util are not in the package.
#
# The README, the commercial LICENSE and the build and run scripts of the
# package are in .packaging/java.
#
# It also builds .commercial-packages/PDFjet-ForJava-Eval-vX.Y.Z.zip, the
# evaluation package: the library and its examples, with the evaluation license
# agreement of .packaging/LICENSE-EVALUATION as its LICENSE.
#
# The package for buyers, Solo and Team, has PDFjet Pro too, which the
# evaluation package has not (the owner, 12 October 2026: "The whole reason to
# buy the packaged version is to get the Pro"): PDFjet-Sign.jar, its reference
# and three examples, built from the last commit of the pdfjet-pro repository,
# beside this one or in PDFJET_PRO, with the files of .packaging/java-pro.
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
EVAL_NAME="PDFjet-ForJava-Eval-$VERSION"
PRO=${PDFJET_PRO:-$(cd .. && pwd)/pdfjet-pro}
if ! git -C "$PRO" rev-parse --verify -q HEAD > /dev/null; then
    echo "PDFjet Pro is not in $PRO: set PDFJET_PRO to its repository."
    exit 1
fi
EVAL_ZIP="$PWD/.commercial-packages/$EVAL_NAME.zip"

# From 9.0.5 every file and directory of a package has one time: the day of
# the commit the package is made from, in Ontario, where it is made, at
# 17:00:00, a round time as on the CDs of old, so that a file's time says
# which release it is of. The zip stores it as it is, with no time zone, so it
# reads 17:00:00 wherever it is unpacked; and two builds of a release differ
# only in what they make, not in when (reproducible builds,
# SOURCE_DATE_EPOCH). The zip lists its entries in sorted order and without
# the extra fields of each system (-X). RELEASE_DAY=20261015, given, is the
# day instead, for a release whose last commit is of another day.
RELEASE_DAY=${RELEASE_DAY:-$(TZ=America/Toronto git log -1 --format=%cd --date=format-local:%Y%m%d HEAD)}
if ! [[ "$RELEASE_DAY" =~ ^[0-9]{8}$ ]]; then
    echo "RELEASE_DAY is a day such as 20261015, not $RELEASE_DAY."
    exit 1
fi
export TZ=UTC
RELEASE_TIME=${RELEASE_DAY}1700.00
stamp() {
    find "$1" -exec touch -h -t "$RELEASE_TIME" {} +
}
zip_sorted() {
    find "$2" -print | LC_ALL=C sort | zip -q -9 -X "$1" -@
}

if [ -n "$(git status --porcelain)" ]; then
    echo "Warning: uncommitted changes are left out; the package is made from $(git rev-parse --short HEAD)."
fi

rm -rf "$(dirname "$STAGE")" "$ZIP" "$EVAL_ZIP"
mkdir -p "$STAGE" .commercial-packages

# Through a file, not a pipe, so that a failing git archive stops the script.
TAR=$PWD/$STAGE.tar
git archive -o "$TAR" HEAD \
    .packaging/java .packaging/java-pro .packaging/LICENSE-EVALUATION \
    com examples images PngSuite \
    CHANGELOG.md THIRD-PARTIES.TXT examples-java.html
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

# The C# example projects and the go.mod file of the Go port are not needed.
find "$STAGE/examples" -mindepth 1 -maxdepth 1 -type d -exec rm -rf {} +
rm -f "$STAGE/images/go.mod"

# The fonts of the package are the fonts directory as it is: the .ttf files,
# which PDFjet subsets, and IBM Plex Sans as .otf too, which Examples 07, 48
# and 50 read.

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
    com/pdfjet/internal/*.java \
    com/pdfjet/fonts/*.java \
    com/pdfjet/encryption/*.java \
    -d out/library
# The entries of the jar have the time of the release too, the manifest that
# jar writes as well, with --date, which JDK 17 and later have.
stamp out/library
if jar --help 2>&1 | grep -q -- '--date'; then
    jar --date="${RELEASE_DAY:0:4}-${RELEASE_DAY:4:2}-${RELEASE_DAY:6:2}T17:00:00Z" \
        --create --file PDFjet.jar -C out/library .
else
    jar cf PDFjet.jar -C out/library .
fi

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

# The LICENSE of the package is the commercial license agreement of
# pdfjet.com, its README is about the prebuilt library, and its build and run
# scripts build the examples against PDFjet.jar, where those of the repository
# build the library from its sources.
mv .packaging/java/* .
mv .packaging/LICENSE-EVALUATION ..
mv .packaging/java-pro ..
rm -rf .packaging

# Creates the PDFs of the package with its own script.
./build-java.sh
rm -rf out

# An example that fails can leave a PDF it had begun, so each must be complete.
for i in $(seq -w 1 50); do
    if ! tail -c 32 "Example_$i.pdf" 2>/dev/null | grep -q '%%EOF'; then
        echo "Example_$i.pdf was not created or is incomplete"
        exit 1
    fi
done

cd ..

# The evaluation package: the library and its examples, as they are now, under
# the evaluation license.
cp -a "$NAME" "$EVAL_NAME"
mv LICENSE-EVALUATION "$EVAL_NAME/LICENSE"
stamp "$EVAL_NAME"
zip_sorted "$EVAL_ZIP" "$EVAL_NAME"

# The package for buyers adds PDFjet Pro, from the last commit of its
# repository: PDFjet-Sign.jar, built for Java 8 as PDFjet.jar is, against it,
# its reference, its examples and the certificate of the signing one.
cd "$NAME"
PRO_TAR=$PWD/../pro.tar
git -C "$PRO" archive -o "$PRO_TAR" HEAD com/pdfjet/sign com/pdfjet/invoice \
    examples/package/java tests/data/rsa-signer.p12
mkdir pro-src
tar -x -C pro-src -f "$PRO_TAR"
rm "$PRO_TAR"
javac -O -encoding utf-8 $RELEASE -Xlint -Xlint:-options -Werror -cp PDFjet.jar \
    pro-src/com/pdfjet/sign/*.java pro-src/com/pdfjet/invoice/*.java -d out/pro
stamp out/pro
if jar --help 2>&1 | grep -q -- '--date'; then
    jar --date="${RELEASE_DAY:0:4}-${RELEASE_DAY:4:2}-${RELEASE_DAY:6:2}T17:00:00Z" \
        --create --file PDFjet-Sign.jar -C out/pro .
else
    jar cf PDFjet-Sign.jar -C out/pro .
fi
javadoc -quiet -public -doctitle "PDFjet Pro for Java" -windowtitle "PDFjet Pro for Java" \
    -cp PDFjet.jar pro-src/com/pdfjet/sign/*.java pro-src/com/pdfjet/invoice/*.java \
    -d docs/java-pro
mkdir -p examples/pro
cp pro-src/examples/package/java/*.java examples/pro/
cp pro-src/tests/data/rsa-signer.p12 examples/pro/demo-signer.p12
rm -rf pro-src out
mv ../java-pro/build-pro.sh ../java-pro/build-pro.cmd .
cat ../java-pro/README-PRO.md >> README.md
rm -rf ../java-pro

# Creates the PDFs of PDFjet Pro with the package's own script.
./build-pro.sh
rm -rf out-pro
for pdf in Pro_SignDocument Pro_WriteInvoice Pro_DrawSwissQRBill; do
    if ! tail -c 32 "$pdf.pdf" 2>/dev/null | grep -q '%%EOF'; then
        echo "$pdf.pdf was not created or is incomplete"
        exit 1
    fi
done

cd ..
stamp "$NAME"
zip_sorted "$ZIP" "$NAME"
cd ../..
rm -rf build/package-java

# The evaluation package again, without the version in its name, for the
# GitHub release: the website links to releases/latest/download/PDFjet-ForJava-Eval.zip,
# which GitHub sends to the latest release's file of that name, so that the
# link never changes. The version is in the name of the folder inside.
LATEST_ZIP="$(dirname "$EVAL_ZIP")/PDFjet-ForJava-Eval.zip"
cp "$EVAL_ZIP" "$LATEST_ZIP"

echo "Created $ZIP"
echo "Created $EVAL_ZIP"
echo "Created $LATEST_ZIP, the same, for the GitHub release"
