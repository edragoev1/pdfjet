#!/bin/bash
# Builds .commercial-packages/PDFjet-ForJava-Pro-vX.Y.Z.zip, a self-contained
# package for Java clients: PDFjet.jar, the Javadoc reference, the examples
# with the PDFs they create, the files they read (resources/data, images,
# PngSuite), the fonts PDFjet carries (resources/fonts, which PDFjet.jar reads
# from there: the jar of the package is small, that of Maven Central carries them),
# and scripts that build and run the examples against PDFjet.jar.
# The library sources and the font tools in util are not in the package.
#
# The README, the commercial LICENSE and the build and run scripts of the
# package are in .packaging/java.
#
# The package for buyers, Solo and Team, is PDFjet Pro: the library and PDFjet
# Pro (the owner, 12 October 2026: "The whole reason to buy the packaged version
# is to get the Pro"), with the commercial license agreement of pdfjet.com as
# its LICENSE: .commercial-packages/PDFjet-ForJava-Pro-vX.Y.Z.zip.
#
# It also builds .commercial-packages/PDFjet-ForJava-Core-vX.Y.Z.zip, PDFjet
# Core: the library and its examples, with no PDFjet Pro, under the MIT
# License of this repository, which the library is (the owner, 12 October
# 2026).
#
# PDFjet Pro is built from the last commit of the pdfjet-pro repository, beside
# this one or in PDFJET_PRO, with the files of .packaging/java-pro.

# Stop at the first failure, so a broken build is never zipped.
set -e

cd "$(dirname "$0")/.."

VERSION=$(sed -n 's/.*producer = "PDFjet \(v[0-9.]*\)".*/\1/p' com/pdfjet/PDF.java)
if [ -z "$VERSION" ]; then
    echo "Could not read the version from com/pdfjet/PDF.java"
    exit 1
fi
# A package is made from HEAD, so HEAD must be the tag of the version: a run
# on a later commit deleted the zip of the release and made another under its
# name (review of 30 October 2026). PDFJET_DRAFT=1 makes one to look at before
# the release instead, -draft in its name, with its folder inside, and leaves
# the zips of the release as they are.
DRAFT=""
if [ -n "${PDFJET_DRAFT:-}" ]; then
    DRAFT="-draft"
elif [ "$(git rev-parse HEAD)" != "$(git rev-parse -q --verify "refs/tags/$VERSION^{commit}")" ]; then
    echo "HEAD is $(git describe --tags --always HEAD), not the tag $VERSION: check out the tag, or set PDFJET_DRAFT=1."
    exit 1
fi
NAME="PDFjet-ForJava-Pro-$VERSION$DRAFT"
STAGE="build/package-java/$NAME"
ZIP="$PWD/.commercial-packages/$NAME.zip"
CORE_NAME="PDFjet-ForJava-Core-$VERSION$DRAFT"
REPO=$PWD
PRO=${PDFJET_PRO:-$(cd .. && pwd)/pdfjet-pro}
if ! git -C "$PRO" rev-parse --verify -q HEAD > /dev/null; then
    echo "PDFjet Pro is not in $PRO: set PDFJET_PRO to its repository."
    exit 1
fi
# The version is the same in the four ports, their tests and Example_37, and
# CHANGELOG.md gives it its date; PDFJET_DRAFT=1 asks for no date (review of
# 17 October 2026).
if ! util/check-version.sh ${PDFJET_DRAFT:+--draft} > /dev/null; then
    exit 1
fi
# The Pro zip is of the release when the last commit of pdfjet-pro is tagged
# with its version; before Pro's tag it is a draft, made again from the tag
# (the checklist, step 9), which PDFJET_PRO_RELEASE=1 asks for: then a commit
# that is not the tag is refused (review of 17 October 2026).
PRO_TAG=$(git -C "$PRO" describe --exact-match --tags HEAD 2> /dev/null || true)
if [ "$PRO_TAG" != "$VERSION" ]; then
    if [ -n "${PDFJET_PRO_RELEASE:-}" ]; then
        echo "pdfjet-pro is at $(git -C "$PRO" describe --tags HEAD), not at its tag $VERSION."
        exit 1
    fi
    echo "Note: pdfjet-pro is at $(git -C "$PRO" describe --tags HEAD), not tagged $VERSION:"
    echo "the Pro zip is a draft, to make again from Pro's tag with PDFJET_PRO_RELEASE=1."
fi
CORE_ZIP="$PWD/.commercial-packages/$CORE_NAME.zip"

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

rm -rf "$(dirname "$STAGE")" "$ZIP" "$CORE_ZIP"
mkdir -p "$STAGE" .commercial-packages

# Through a file, not a pipe, so that a failing git archive stops the script.
TAR=$PWD/$STAGE.tar
# resources is as it is in the repository: the fonts PDFjet carries, each
# family with its license, and the data files the examples read, with the
# README that says where they come from.
git archive -o "$TAR" HEAD \
    .packaging/java .packaging/java-pro LICENSE \
    com examples resources/images tests/data/PngSuite resources/fonts resources/data \
    CHANGELOG.md THIRD-PARTIES.TXT examples-java.html
tar -x -C "$STAGE" -f "$TAR"
rm "$TAR"

# The C# example projects and the Go and Swift files of resources and images
# are not needed.
find "$STAGE/examples" -mindepth 1 -maxdepth 1 -type d -exec rm -rf {} +
rm -f "$STAGE/resources/images/go.mod" "$STAGE/resources/data/go.mod"
# Every .go and .swift file of resources/fonts: fonts.go and the packages
# world and cjk, which embed the fonts for Go, and the sources of the Swift
# targets PDFjetFonts and PDFjetFontsWorld. The fonts of the other scripts
# than the European ones stay in resources/fonts/world, where PDFjet.jar
# reads them.
find "$STAGE/resources/fonts" \( -name '*.go' -o -name '*.swift' \) -delete
find "$STAGE/resources/fonts" -type d -empty -delete

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

# The same javadoc command as generate-documentation.sh, with no time in the
# pages, so that two builds of a release give the same reference (review of
# 17 October 2026). The PDFs of the examples differ, by their dates and IDs.
javadoc -quiet -notimestamp -public -doctitle "PDFjet for Java" -windowtitle "PDFjet for Java" \
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
mv LICENSE ../LICENSE-MIT
mv .packaging/java/* .
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

# PDFjet Core: the library and its examples, as they are now, under the MIT
# License.
cp -a "$NAME" "$CORE_NAME"
mv LICENSE-MIT "$CORE_NAME/LICENSE"
stamp "$CORE_NAME"
zip_sorted "$CORE_ZIP" "$CORE_NAME"

# The package for buyers adds PDFjet Pro, from the last commit of its
# repository, compiled into PDFjet.jar with the library (the owner, 12 October
# 2026: one PDFjet.jar), for Java 8, with "PDFjet Pro" in its manifest so that
# it is told from the jar of PDFjet Core; the API reference of both; its
# examples, the certificate of the signing one and the test PKI of the
# long-term signature one; and the POM of the jar,
# com.pdfjet:pdfjet-pro, to install it in a Maven repository.
cd "$NAME"
PRO_TAR=$PWD/../pro.tar
mkdir pro-src
git -C "$REPO" archive -o "$PRO_TAR" HEAD com
tar -x -C pro-src -f "$PRO_TAR"
git -C "$PRO" archive -o "$PRO_TAR" HEAD com/pdfjet/sign com/pdfjet/invoice \
    com/pdfjet/hyphenation src/hyphenation/patterns \
    examples/package/java tests/data/rsa-signer.p12 \
    tests/ltv-server.py tests/data/ltv-signer.p12 tests/data/ltv-root.pem tests/data/ltv-root.key \
    tests/data/ltv-root-index.txt tests/data/ltv-ca.pem tests/data/ltv-ca.key tests/data/ltv-ca-index.txt \
    tests/data/ltv-tsa.pem tests/data/ltv-tsa.key tests/data/ltv-ocsp.pem tests/data/ltv-ocsp.key
tar -x -C pro-src -f "$PRO_TAR"
rm "$PRO_TAR"
javac -O -encoding utf-8 $RELEASE -Xlint -Xlint:-options -Werror \
    pro-src/com/pdfjet/*.java \
    pro-src/com/pdfjet/barcodes/*.java \
    pro-src/com/pdfjet/pdf417/*.java \
    pro-src/com/pdfjet/qrcode/*.java \
    pro-src/com/pdfjet/datamatrix/*.java \
    pro-src/com/pdfjet/internal/*.java \
    pro-src/com/pdfjet/fonts/*.java \
    pro-src/com/pdfjet/encryption/*.java \
    pro-src/com/pdfjet/sign/*.java \
    pro-src/com/pdfjet/invoice/*.java \
    pro-src/com/pdfjet/hyphenation/*.java \
    -d out/library
# The hyphenation patterns, resources of com.pdfjet.hyphenation, with their
# licenses; and those beside the jar too, as their MIT licenses ask that the
# notice go with every copy
mkdir -p out/library/com/pdfjet/hyphenation/patterns
cp pro-src/src/hyphenation/patterns/*.txt out/library/com/pdfjet/hyphenation/patterns/
cp pro-src/src/hyphenation/patterns/LICENSES.txt HYPHENATION-PATTERNS-LICENSES.txt
stamp out/library
printf 'Implementation-Title: PDFjet Pro\nImplementation-Version: %s\nImplementation-Vendor: PDFjet Software\n' \
    "${VERSION#v}" > pro-manifest.txt
rm PDFjet.jar
if jar --help 2>&1 | grep -q -- '--date'; then
    jar --date="${RELEASE_DAY:0:4}-${RELEASE_DAY:4:2}-${RELEASE_DAY:6:2}T17:00:00Z" \
        --create --file PDFjet.jar --manifest pro-manifest.txt -C out/library .
else
    jar cfm PDFjet.jar pro-manifest.txt -C out/library .
fi
rm pro-manifest.txt
rm -rf docs/java
javadoc -quiet -notimestamp -public -doctitle "PDFjet Pro for Java" -windowtitle "PDFjet Pro for Java" \
    -sourcepath pro-src \
    pro-src/com/pdfjet/*.java \
    pro-src/com/pdfjet/barcodes/*.java \
    pro-src/com/pdfjet/encryption/*.java \
    pro-src/com/pdfjet/fonts/*.java \
    pro-src/com/pdfjet/pdf417/*.java \
    pro-src/com/pdfjet/qrcode/*.java \
    pro-src/com/pdfjet/datamatrix/*.java \
    pro-src/com/pdfjet/sign/*.java \
    pro-src/com/pdfjet/invoice/*.java \
    pro-src/com/pdfjet/hyphenation/*.java \
    -d docs/java
mkdir -p examples/pro
cp pro-src/examples/package/java/*.java examples/pro/
cp pro-src/tests/data/rsa-signer.p12 examples/pro/demo-signer.p12
# The test PKI of the long-term signature example, SignLongTerm, which no one
# trusts, and ltv-server.py, its OCSP responder, CRLs, CA certificates and time
# stamp authority on 127.0.0.1:19180, which build-pro.sh starts
mkdir -p examples/pro/ltv
cp pro-src/tests/ltv-server.py pro-src/tests/data/ltv-* examples/pro/ltv/
# The packages are under PDFjet Pro's commercial license, so the server's
# copy carries the header of the packaged examples, not the repository's.
sed -i -e '/^# Licensed under the GNU Affero General Public License/,/^# SPDX-License-Identifier: AGPL-3.0-only/c\
# A test server of PDFjet Pro, for the packages of PDFjet.' examples/pro/ltv/ltv-server.py
if grep -q AGPL examples/pro/ltv/ltv-server.py; then
    echo "examples/pro/ltv/ltv-server.py still names the AGPL." >&2
    exit 1
fi
rm -rf pro-src out
mv ../java-pro/build-pro.sh ../java-pro/build-pro.cmd .
# The POM and the README of PDFjet Pro, with its version: the jar installed
# as com.pdfjet:pdfjet-pro in a Maven repository, as the README says
sed "s/@VERSION@/${VERSION#v}/g" ../java-pro/pom.xml > pom.xml
sed "s/@VERSION@/${VERSION#v}/g" ../java-pro/README-PRO.md >> README.md
rm -rf ../java-pro

# Creates the PDFs of PDFjet Pro with the package's own script.
./build-pro.sh
rm -rf out-pro
for pdf in Pro_SignDocument Pro_WriteInvoice Pro_DrawSwissQRBill Pro_DrawEPCQRCode Pro_HyphenateText \
        Pro_SignLongTerm_B-LT Pro_SignLongTerm_B-LTA Pro_SignLongTerm_B-LTA-renewed; do
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

# PDFjet Core again, without the version in its name, for the GitHub release:
# the website links to releases/latest/download/PDFjet-ForJava-Core.zip, which
# GitHub sends to the latest release's file of that name, so that the link
# never changes; the version is in the name of the folder inside.
LATEST_ZIP="$(dirname "$CORE_ZIP")/PDFjet-ForJava-Core.zip"
if [ -z "$DRAFT" ]; then
    cp "$CORE_ZIP" "$LATEST_ZIP"
fi

echo "Created $ZIP"
echo "Created $CORE_ZIP"
if [ -z "$DRAFT" ]; then
    echo "Created $LATEST_ZIP, the same, for the GitHub release"
fi
