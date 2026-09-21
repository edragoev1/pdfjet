#!/bin/sh
# Fetches the PDFs that tests/corpus/check-corpus.py reads: the test PDFs of
# pdf.js with its manifest, which has the passwords, and the veraPDF corpus,
# each at the commit below so that a run reads the same files as the last.
#
#     tests/corpus/fetch-corpora.sh [DIR]
#
# DIR is .corpora by default. The files are fetched, not committed: their
# copyright belongs to many people. The pdf.js files that are links to a PDF
# on the web are not fetched.
set -e

PDFJS_COMMIT=d54c193bd4dd6c34759cb88f1a3b78db66f0962c
VERAPDF_COMMIT=bb75f4f0073d9350dfd058c0162a367e6fadf25e
DIR=${1:-.corpora}

# fetch URL DIR COMMIT [SPARSE PATHS...]
fetch() {
    url=$1 dir=$2 commit=$3
    shift 3
    if [ "$(git -C "$dir" rev-parse HEAD 2> /dev/null)" = "$commit" ]; then
        return
    fi
    rm -rf "$dir"
    git init -q "$dir"
    git -C "$dir" remote add origin "$url"
    if [ $# -gt 0 ]; then
        git -C "$dir" sparse-checkout set "$@"
    fi
    git -C "$dir" fetch -q --depth 1 --filter=blob:none origin "$commit"
    git -C "$dir" checkout -q FETCH_HEAD
}

mkdir -p "$DIR"
fetch https://github.com/mozilla/pdf.js.git "$DIR/pdfjs" $PDFJS_COMMIT test/pdfs test/test_manifest.json
fetch https://github.com/veraPDF/veraPDF-corpus.git "$DIR/verapdf" $VERAPDF_COMMIT
