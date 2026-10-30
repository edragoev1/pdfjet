#!/bin/bash
# Builds .commercial-packages/PDFjet-For.NET-Pro-vX.Y.Z.zip, a self-contained
# package for .NET clients: PDFjet.dll, the DocFX reference, the example
# projects with the PDFs they create, the files they read (resources/data,
# images, PngSuite), the fonts PDFjet carries (resources/fonts, each family
# with its license, which PDFjet.dll reads from there: the DLL of the package
# is small, that of NuGet carries them, see package-nuget.sh), and scripts
# that build and run the examples against PDFjet.dll.
# The library sources and the font tools in util are not in the package.
#
# The README, the commercial LICENSE and the build and run scripts of the
# package are in .packaging/dotnet.
#
# The package for buyers, Solo and Team, is PDFjet Pro: the library and PDFjet
# Pro (the owner, 12 October 2026: "The whole reason to buy the packaged version
# is to get the Pro"), with the commercial license agreement of pdfjet.com as
# its LICENSE: .commercial-packages/PDFjet-For.NET-Pro-vX.Y.Z.zip.
#
# It also builds .commercial-packages/PDFjet-For.NET-Core-vX.Y.Z.zip, PDFjet
# Core: the library and its examples, with no PDFjet Pro, under the MIT
# License of this repository, which the library is (the owner, 12 October
# 2026).
#
# PDFjet Pro is built from the last commit of the pdfjet-pro repository, beside
# this one or in PDFJET_PRO, with the files of .packaging/dotnet-pro.

# PDFjet.dll is strong-named, in both packages: the key is not in the
# repository, but in the owner's private files, and is given by PDFJET_SNK:
#
#   PDFJET_SNK=~/Projects/private/pdfjet-pro-private/signing/PDFjet.snk .packaging/package-dotnet.sh
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
NAME="PDFjet-For.NET-Pro-$VERSION"
STAGE="build/package-dotnet/$NAME"
ZIP="$PWD/.commercial-packages/$NAME.zip"
CORE_NAME="PDFjet-For.NET-Core-$VERSION"
REPO=$PWD
PRO=${PDFJET_PRO:-$(cd .. && pwd)/pdfjet-pro}
if ! git -C "$PRO" rev-parse --verify -q HEAD > /dev/null; then
    echo "PDFjet Pro is not in $PRO: set PDFJET_PRO to its repository."
    exit 1
fi
# The version is the same in the four ports, their tests and Example_37, and
# CHANGELOG.md gives it its date; PDFJET_DRAFT=1 asks for no date, for a
# package made to look at before the release (review of 17 October 2026).
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
git archive -o "$TAR" HEAD \
    .packaging/dotnet .packaging/dotnet-pro LICENSE \
    net net48 PDFjet.csproj resources/fonts resources/data examples images PngSuite docfx \
    CHANGELOG.md THIRD-PARTIES.TXT examples-dotnet.html
tar -x -C "$STAGE" -f "$TAR"
rm "$TAR"

# resources/data is as it is in the repository, the data files the examples
# read with the README that says where they come from. resources/fonts is the
# fonts PDFjet carries, each family with its license, which the small
# PDFjet.dll of the package reads from there (the owner, 29 October 2026): the
# font files and the licenses alone, not the Go and Swift files beside them.
# The Java examples and the Go files of resources and images are not needed,
# but Example_42 attaches Example_02.java.
find "$STAGE/examples" -maxdepth 1 -name '*.java' ! -name Example_02.java -delete
rm -f "$STAGE/images/go.mod" "$STAGE/resources/data/go.mod"
find "$STAGE/resources/fonts" -maxdepth 1 -type f -delete
find "$STAGE/resources/fonts" -mindepth 2 \( -type d -o \( -type f ! -name '*.ttf' ! -name '*.otf' \
    ! -name '*.txt' ! -name LICENSE \) \) -prune -exec rm -rf {} +
# The folder of the Go package cjk, left empty
find "$STAGE/resources/fonts" -mindepth 1 -type d -empty -delete
for family in "$STAGE"/resources/fonts/*/; do
    if [ ! -f "$family/license.txt" ] && [ ! -f "$family/OFL.txt" ] && [ ! -f "$family/LICENSE" ]; then
        echo "$family has no license"
        exit 1
    fi
done

cd "$STAGE"

# The same build as build-dotnet.sh, strong-named, with the release as the file
# version: 9.0.3 of v9.0.3. Without -p:PDFjetFonts=true, the DLL is small and
# reads the fonts from resources/fonts.
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
mv LICENSE ../LICENSE-MIT
mv .packaging/dotnet/* .
mv .packaging/dotnet-pro ..
rm -rf .packaging

# Creates the PDFs of the package with its own script.
./build-dotnet.sh
rm -rf examples/Example_*/bin examples/Example_*/obj

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
# repository, compiled into PDFjet.dll with the library (the owner, 12 October
# 2026: one PDFjet.dll): the same assembly, PDFjet 9.0.0.0 with the same strong
# name, so that it replaces the PDFjet.dll of PDFjet Core, and of the releases
# before, with no change to a project that references it; "PDFjet Pro" as its
# product, so that it is told from the one of PDFjet Core. It is built from a
# copy of the two repositories, as a build beside them can find another
# PDFjet.dll than the one meant. net48/PDFjet.dll stays PDFjet Core, as PDFjet
# Pro is built for .NET 8. With it, the example projects of PDFjet Pro and the
# certificate of the signing one.
cd "$NAME"
PRO_TAR=$PWD/../pro.tar
mkdir pro-src
git -C "$REPO" archive -o "$PRO_TAR" HEAD net PDFjet.csproj
tar -x -C pro-src -f "$PRO_TAR"
git -C "$PRO" archive -o "$PRO_TAR" HEAD net/pdfjet/Sign net/pdfjet/Invoice \
    net/pdfjet/Hyphenation src/hyphenation/patterns \
    examples/package/dotnet tests/data/rsa-signer.p12 \
    tests/ltv-server.py tests/data/ltv-signer.p12 tests/data/ltv-root.pem tests/data/ltv-root.key \
    tests/data/ltv-root-index.txt tests/data/ltv-ca.pem tests/data/ltv-ca.key tests/data/ltv-ca-index.txt \
    tests/data/ltv-tsa.pem tests/data/ltv-tsa.key tests/data/ltv-ocsp.pem tests/data/ltv-ocsp.key
tar -x -C pro-src -f "$PRO_TAR"
rm "$PRO_TAR"
perl -0pi -e 's|(    <Compile Include="net/pdfjet/Encryption/\*\.cs" />\n)|$1    <Compile Include="net/pdfjet/Sign/*.cs" />\n    <Compile Include="net/pdfjet/Invoice/*.cs" />\n    <Compile Include="net/pdfjet/Hyphenation/*.cs" />\n    <EmbeddedResource Include="src/hyphenation/patterns/*.txt" LogicalName="PDFjet.Hyphenation.%(Filename)%(Extension)" />\n|' \
    pro-src/PDFjet.csproj
if ! grep -q 'net/pdfjet/Hyphenation/\*\.cs' pro-src/PDFjet.csproj; then
    echo "The sources of PDFjet Pro could not be added to PDFjet.csproj"
    exit 1
fi
dotnet build pro-src/PDFjet.csproj -c release -p:TreatWarningsAsErrors=true \
    -p:SignAssembly=true -p:AssemblyOriginatorKeyFile="$PDFJET_SNK" \
    -p:FileVersion="${VERSION#v}" -p:InformationalVersion="${VERSION#v}" \
    -p:Product="PDFjet Pro" -o pro-out
cp pro-out/PDFjet.dll PDFjet.dll
mkdir -p examples/pro
cp -r pro-src/examples/package/dotnet/* examples/pro/
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
# The licenses of the hyphenation patterns built into PDFjet.dll, beside it,
# as their MIT licenses ask that the notice go with every copy
cp pro-src/src/hyphenation/patterns/LICENSES.txt HYPHENATION-PATTERNS-LICENSES.txt
rm -rf pro-src pro-out
mv ../dotnet-pro/build-pro.sh ../dotnet-pro/build-pro.cmd .
cat ../dotnet-pro/README-PRO.md >> README.md
rm -rf ../dotnet-pro

# Creates the PDFs of PDFjet Pro with the package's own script.
./build-pro.sh
rm -rf examples/pro/*/bin examples/pro/*/obj
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
rm -rf build/package-dotnet

# PDFjet Core again, without the version in its name, for the GitHub release:
# the website links to releases/latest/download/PDFjet-For.NET-Core.zip, which
# GitHub sends to the latest release's file of that name, so that the link
# never changes; the version is in the name of the folder inside.
LATEST_ZIP="$(dirname "$CORE_ZIP")/PDFjet-For.NET-Core.zip"
cp "$CORE_ZIP" "$LATEST_ZIP"

echo "Created $ZIP"
echo "Created $CORE_ZIP"
echo "Created $LATEST_ZIP, the same, for the GitHub release"
