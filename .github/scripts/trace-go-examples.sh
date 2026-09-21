#!/bin/bash
# Builds the Go examples with the texttrace tag and runs them in a folder, which
# then has their Example_NN.pdf files and text-trace.jsonl, the strings every
# example drew on every page (see src/texttrace.go). check-example-text.py
# compares the text of the PDFs with that trace:
#
#     .github/scripts/trace-go-examples.sh /tmp/text/go
#     python3 .github/scripts/check-example-text.py /tmp/text/go/text-trace.jsonl '/tmp/text/{port}'
#
# The examples run in the folder, with links to the fonts, images and data they
# read, so the Example_NN.pdf files in the repository are left alone. The
# tagged build draws the same PDFs as the default one.

set -e

if [ $# -ne 1 ]; then
    echo "Usage: $0 OUTPUT_FOLDER"
    exit 1
fi

root=$(cd "$(dirname "$0")/../.." && pwd)
mkdir -p "$1"
out=$(cd "$1" && pwd)

for dir in data fonts images PngSuite examples src; do
    ln -sfn "$root/$dir" "$out/$dir"
done
rm -f "$out"/Example_*.pdf "$out/text-trace.jsonl"

bin="$out/bin"
mkdir -p "$bin"
(cd "$root/src" && go build -tags texttrace -o "$bin/" ./examples/...)

cd "$out"
for i in $(seq -w 1 51); do
    PDFJET_TEXT_TRACE="$out/text-trace.jsonl" "$bin/example$i"
done
rm -rf "$bin"
