#!/bin/bash
# Checks that every place that holds the version of PDFjet holds the same one,
# and that CHANGELOG.md gives it a date: the producer strings of the four
# ports, Example_37's Data Matrix text in its four versions, and the tests of
# the producer string. Prints the version, as v9.1.1, or the places that differ
# and fails. The package scripts run it, as the version was read from one file
# of each and nothing saw a port left behind (review of 17 October 2026).
#
#   util/check-version.sh                the version, checked
#   util/check-version.sh --draft        the same, with no date asked of CHANGELOG
#   util/check-version.sh --at v9.1.7    the files of that commit, not of this tree

cd "$(dirname "$0")/.." || exit 1

DRAFT=""
AT=""
while [ $# -gt 0 ]; do
    case $1 in
        --draft) DRAFT=1; shift ;;
        --at) AT=$2; shift 2 ;;
        *) echo "Unknown option $1" >&2; exit 1 ;;
    esac
done

# The text of a file, of the commit asked for or of this tree.
content() {
    if [ -n "$AT" ]; then
        git show "$AT:$1"
    else
        cat "$1"
    fi
}

VERSION=$(content com/pdfjet/PDF.java | sed -n 's/.*producer = "PDFjet \(v[0-9.]*\)".*/\1/p')
if [ -z "$VERSION" ]; then
    echo "Could not read the version from com/pdfjet/PDF.java" >&2
    exit 1
fi
NUMBER=${VERSION#v}
failed=0

# Each file must hold the text.
expect() {
    if ! content "$1" | grep -qF -- "$2"; then
        echo "$1 does not hold $2" >&2
        failed=1
    fi
}

for file in com/pdfjet/PDF.java net/pdfjet/PDF.cs src/pdf.go Sources/PDFjet/PDF.swift; do
    expect "$file" "\"PDFjet $VERSION\""
done
for file in examples/Example_37.java examples/Example_37/Example_37.cs \
        src/examples/example37/main.go Sources/Example_37/main.swift; do
    expect "$file" "PDFjet $NUMBER"
done
for file in tests/java/com/pdfjet/ReviewWriterTest.java tests/dotnet/ReviewWriterTest.cs \
        src/review_writer_test.go tests/swift/ReviewWriterTests.swift; do
    expect "$file" "PDFjet $VERSION"
done
if [ -z "$DRAFT" ]; then
    if ! content CHANGELOG.md | grep -qE "^## $VERSION — [0-9]{4}-[0-9]{2}-[0-9]{2}\$"; then
        echo "CHANGELOG.md has no \"## $VERSION — YYYY-MM-DD\": give the release its date" >&2
        failed=1
    fi
fi

if [ $failed -ne 0 ]; then
    exit 1
fi
echo "$VERSION"
