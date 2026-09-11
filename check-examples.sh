#!/bin/bash
# Runs the checks of the Build workflow locally, so they can pass before a push.
#
#   ./check-examples.sh
#
# Builds the Java, C#, Go and Swift ports and runs their examples with the
# build-*.sh scripts, as the workflow's port jobs do, then checks the example
# PDFs with .github/scripts/check-example-pdfs.py, as its compare job does.
#
# The ports are built one after another in this folder. clean.sh runs before
# each one, so no output of an earlier build, like the DLL of an example that
# no longer compiles, is used. The PDFs and build logs are kept in
# build/check-examples, with the Python environment the check runs in.
#
# Needs the four toolchains, python3 and veraPDF, run as "verapdf" or as the
# command in the VERAPDF environment variable.

cd "$(dirname "$0")" || exit 1

WORK=build/check-examples

VERAPDF=${VERAPDF:-verapdf}
if ! command -v "$VERAPDF" > /dev/null; then
    echo "veraPDF was not found. Install it, see https://docs.verapdf.org/install/,"
    echo "or set VERAPDF to its command."
    exit 1
fi
export VERAPDF

# The check uses the PyMuPDF version that the Build workflow installs.
pymupdf=$(grep -o 'pymupdf==[0-9.]*' .github/workflows/build.yml)
if [ ! -x "$WORK/venv/bin/python" ]; then
    python3 -m venv "$WORK/venv" || exit 1
fi
"$WORK/venv/bin/pip" install -q "$pymupdf" || exit 1

rm -rf "$WORK/pdfs" "$WORK/logs"
mkdir -p "$WORK/logs"

for port in java dotnet go swift; do
    echo "Building and running the $port examples, logging to $WORK/logs/$port.log"
    bash clean.sh > /dev/null
    # bash -e stops at the first command that fails, as in the workflow.
    if ! bash -e build-$port.sh > "$WORK/logs/$port.log" 2>&1; then
        tail -n 20 "$WORK/logs/$port.log"
        echo "The $port build failed."
        exit 1
    fi
    mkdir -p "$WORK/pdfs/$port"
    missing=0
    for i in $(seq -w 1 50); do
        # The Swift port has no Example_30, see build-swift.sh.
        if [ $port = swift ] && [ $i = 30 ]; then
            continue
        fi
        if [ -s Example_$i.pdf ]; then
            mv Example_$i.pdf "$WORK/pdfs/$port/"
        else
            echo "$port: Example_$i.pdf was not created"
            missing=1
        fi
    done
    if [ $missing = 1 ]; then
        exit 1
    fi
done

"$WORK/venv/bin/python" .github/scripts/check-example-pdfs.py "$WORK/pdfs/{port}"
