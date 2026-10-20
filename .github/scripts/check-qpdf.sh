#!/bin/bash
# Reads each PDF with qpdf --check: the cross-reference table, the lengths of
# the streams and their compression, the encryption, which a PDF can get wrong
# and still pass veraPDF and the viewers (the owner, 20 October 2026). qpdf
# says "No syntax or stream encoding errors found" and exits 0 for a PDF it
# finds nothing wrong with: 3 is a warning, as of a stream it repaired, 2 an
# error. An encrypted one is opened with its owner password, "world", as
# Example_47 has it, or the Cyrillic one of encrypted-pdfs.
#
#   .github/scripts/check-qpdf.sh build/check-examples/pdfs/*/*.pdf ...
#
# Run by check-examples.sh and by the compare job of the Build workflow.

if ! command -v qpdf > /dev/null; then
    echo "qpdf was not found. Install it, with \"sudo apt-get install qpdf\" on Ubuntu,"
    echo "or \"brew install qpdf\" on macOS."
    exit 1
fi

failed=0
count=0
for pdf in "$@"; do
    count=$((count + 1))
    for password in "" world "владелец"; do
        out=$(qpdf --password="$password" --check "$pdf" 2>&1)
        status=$?
        if [ $status -ne 2 ] || ! grep -q "invalid password" <<< "$out"; then
            break
        fi
    done
    if [ $status -ne 0 ]; then
        echo "qpdf: $pdf"
        grep -v "^checking \|^PDF Version\|^File is not" <<< "$out" | head -n 10
        failed=1
    fi
done
if [ $count = 0 ]; then
    echo "qpdf was given no PDFs."
    exit 1
fi
if [ $failed = 1 ]; then
    echo "qpdf found problems in the PDFs above."
    exit 1
fi
echo "qpdf found no problems in the $count PDFs."
