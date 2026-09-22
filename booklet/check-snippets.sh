#!/bin/bash
# Builds and runs the snippets of the booklet, booklet/snippets, in the four
# ports, and checks that each port writes the same PDFs as the Java snippets.
# Run it from anywhere; it works in build/check-snippets. The library of each
# port is built from the repository.
#
# Needs the four toolchains and python3 with PyMuPDF; veraPDF, run as
# "verapdf" or as the command in VERAPDF, checks the PDF/UA and PDF/A
# snippets when it is there.

cd "$(dirname "$0")/.." || exit 1
ROOT=$(pwd)
WORK=$ROOT/build/check-snippets
rm -rf "$WORK"

# Each port runs its snippets in a folder of its own, with links to the fonts,
# images and data they read.
for port in java csharp go swift; do
    mkdir -p "$WORK/$port/out"
    for dir in fonts images data; do
        ln -s "$ROOT/$dir" "$WORK/$port/out/$dir"
    done
done

echo "Building and running the java snippets"
RELEASE="--release 8"
if ! javac --release 8 -version > /dev/null 2>&1; then
    RELEASE=""
fi
javac -encoding utf-8 $RELEASE -Xlint -Xlint:-options -Werror -d "$WORK/java/classes" \
    com/pdfjet/*.java \
    com/pdfjet/barcodes/*.java \
    com/pdfjet/pdf417/*.java \
    com/pdfjet/qrcode/*.java \
    com/pdfjet/datamatrix/*.java \
    com/pdfjet/fonts/*.java \
    com/pdfjet/encryption/*.java \
    booklet/snippets/java/Snippets.java || exit 1
(cd "$WORK/java/out" && java -cp "$WORK/java/classes" Snippets) || exit 1

echo "Building and running the csharp snippets"
dotnet build PDFjet.csproj -c release -p:TreatWarningsAsErrors=true > "$WORK/csharp/build.log" 2>&1 ||
    { tail -n 20 "$WORK/csharp/build.log"; exit 1; }
dotnet build booklet/snippets/csharp/Snippets.csproj -c release -p:TreatWarningsAsErrors=true \
    -o "$WORK/csharp/bin" >> "$WORK/csharp/build.log" 2>&1 ||
    { tail -n 20 "$WORK/csharp/build.log"; exit 1; }
(cd "$WORK/csharp/out" && dotnet "$WORK/csharp/bin/Snippets.dll") || exit 1

echo "Building and running the go snippets"
go vet ./booklet/snippets/go || exit 1
go build -o "$WORK/go/snippets" ./booklet/snippets/go || exit 1
(cd "$WORK/go/out" && "$WORK/go/snippets") || exit 1

echo "Building and running the swift snippets"
swift build --configuration release --product BookletSnippets -Xswiftc -warnings-as-errors \
    > "$WORK/swift/build.log" 2>&1 || { tail -n 20 "$WORK/swift/build.log"; exit 1; }
bin=$(swift build --configuration release --show-bin-path)
(cd "$WORK/swift/out" && "$bin/BookletSnippets") || exit 1

PYTHON=${PYTHON:-python3}
if [ -x "$ROOT/build/check-examples/venv/bin/python" ]; then
    PYTHON=$ROOT/build/check-examples/venv/bin/python
fi
"$PYTHON" booklet/check-snippets.py "$WORK"
