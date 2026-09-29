#!/bin/bash
# Runs the checks of the Build workflow locally, so they can pass before a push.
#
#   ./check-examples.sh
#
# Builds the Java, C#, Go and Swift ports and runs their examples with the
# build-*.sh scripts and their unit tests with the test-*.sh scripts, as the
# workflow's port jobs do, builds and tests the Java port again with JDK 8, as
# its java (JDK 8) job does, then checks the example PDFs with
# .github/scripts/check-example-pdfs.py and their tags with
# .github/scripts/check-pdfua-tags.py, as its compare job does, and opens the
# Java PDFs in PDFium, pdf.js and Poppler with .github/scripts/check-viewers.py,
# as its viewers job does; its PDFKit check needs macOS, so it is left to the
# workflow. Last, it builds and runs the snippets of the booklet in the four
# ports and compares their PDFs, with booklet/check-snippets.sh.
#
# The ports are built one after another in this folder. clean.sh runs before
# each one, so no output of an earlier build, like the DLL of an example that
# no longer compiles, is used. The PDFs and build logs are kept in
# build/check-examples, with the Python environment the check runs in.
#
# Needs the four toolchains, python3 and veraPDF, run as "verapdf" or as the
# command in the VERAPDF environment variable, a JDK 8, found in the
# JAVA8_HOME environment variable or in /opt/jdk8* or /usr/lib/jvm, Node 22.13
# or later with npm, for pdf.js, and Poppler's pdftoppm and pdftotext, all on
# the PATH. The script stops before it builds anything when one is missing.

bash "$(dirname "$0")/get-fonts-and-data.sh" || exit 1
cd "$(dirname "$0")" || exit 1

WORK=build/check-examples

VERAPDF=${VERAPDF:-verapdf}
if ! command -v "$VERAPDF" > /dev/null; then
    echo "veraPDF was not found. Install it, see https://docs.verapdf.org/install/,"
    echo "or set VERAPDF to its command."
    exit 1
fi
export VERAPDF

# pdf.js needs Node 22.13 or later; the workflow runs it with Node 24. A missing
# or older Node fails the script, as a missing veraPDF does, rather than skip
# the check that the workflow then fails.
if ! node -e 'const [major, minor] = process.versions.node.split(".").map(Number);
        process.exit(major > 22 || (major === 22 && minor >= 13) ? 0 : 1)' 2> /dev/null ||
        ! command -v npm > /dev/null; then
    echo "The pdf.js check needs Node 22.13 or later, with npm, as \"node\" on the PATH;"
    echo "found $(node --version 2> /dev/null || echo "no node"). Get Node 24 from https://nodejs.org/"
    echo "and put its bin folder first on the PATH."
    exit 1
fi

if ! command -v pdftoppm > /dev/null || ! command -v pdftotext > /dev/null; then
    echo "Poppler's pdftoppm and pdftotext were not found. Install them, with"
    echo "\"sudo apt-get install poppler-utils\" on Ubuntu, or \"brew install poppler\" on macOS."
    exit 1
fi

# JDK 8's javac has no --release option, and it rejects some code that javac 9
# and later accept with --release 8, so only a real JDK 8 checks the Java port
# for it.
if [ -z "$JAVA8_HOME" ]; then
    for jdk in /opt/jdk8* /opt/jdk-8* /usr/lib/jvm/*-8-* /usr/lib/jvm/java-1.8*; do
        if [ -x "$jdk/bin/javac" ]; then
            JAVA8_HOME=$jdk
            break
        fi
    done
fi
if [ -z "$JAVA8_HOME" ] || ! "$JAVA8_HOME/bin/javac" -version 2>&1 | grep -q ' 1\.8\.'; then
    echo "A JDK 8 was not found. Install one, for example from https://adoptium.net/,"
    echo "or set JAVA8_HOME to its folder."
    exit 1
fi

# The checks use the PyMuPDF and pypdfium2 versions that the Build workflow
# installs.
pymupdf=$(grep -o 'pymupdf==[0-9.]*' .github/workflows/build.yml | head -n 1)
pypdfium2=$(grep -o 'pypdfium2==[0-9.]*' .github/workflows/build.yml | head -n 1)
if [ ! -x "$WORK/venv/bin/python" ]; then
    python3 -m venv "$WORK/venv" || exit 1
fi
"$WORK/venv/bin/pip" install -q "$pymupdf" "$pypdfium2" || exit 1

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
    for i in $(seq -w 1 57); do
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
    echo "Running the $port unit tests, logging to $WORK/logs/$port-tests.log"
    if ! bash -e test-$port.sh > "$WORK/logs/$port-tests.log" 2>&1; then
        tail -n 40 "$WORK/logs/$port-tests.log"
        echo "The $port unit tests failed."
        exit 1
    fi
done

# The Java port again with JDK 8, as the java (JDK 8) job of the workflow does.
# Its PDFs are not compared, as in the workflow.
echo "Building and running the java examples with JDK 8, logging to $WORK/logs/java8.log"
bash clean.sh > /dev/null
if ! JAVA_HOME="$JAVA8_HOME" PATH="$JAVA8_HOME/bin:$PATH" \
        bash -e build-java.sh > "$WORK/logs/java8.log" 2>&1; then
    tail -n 20 "$WORK/logs/java8.log"
    echo "The java build with JDK 8 failed."
    exit 1
fi
missing=0
for i in $(seq -w 1 57); do
    if [ ! -s Example_$i.pdf ]; then
        echo "java (JDK 8): Example_$i.pdf was not created"
        missing=1
    fi
done
bash clean.sh > /dev/null
if [ $missing = 1 ]; then
    exit 1
fi
echo "Running the java unit tests with JDK 8, logging to $WORK/logs/java8-tests.log"
if ! JAVA_HOME="$JAVA8_HOME" PATH="$JAVA8_HOME/bin:$PATH" \
        bash -e test-java.sh > "$WORK/logs/java8-tests.log" 2>&1; then
    tail -n 40 "$WORK/logs/java8-tests.log"
    echo "The java unit tests with JDK 8 failed."
    exit 1
fi

"$WORK/venv/bin/python" .github/scripts/check-example-pdfs.py "$WORK/pdfs/{port}" || exit 1

# What the tags of the tagged documents stand for, which veraPDF cannot check.
# The Java PDFs stand for all four ports: check-example-pdfs.py has just
# checked that the content streams of the other three are the same.
"$WORK/venv/bin/python" .github/scripts/check-pdfua-tags.py "$WORK/pdfs/java" || exit 1

# The Java PDFs, and two more encrypted like Example_30, in PDFium, pdf.js and
# Poppler, whose renders and contact sheets are kept in $WORK/viewers/ENGINE.
# pdf.js is installed from package-lock.json, as in the workflow.
rm -rf "$WORK/encrypted" "$WORK/viewers"
go run ./.github/scripts/encrypted-pdfs "$WORK/encrypted" || exit 1
npm ci --no-audit --no-fund --silent --prefix .github/scripts/render-pdfjs || exit 1
for engine in pdfium pdfjs poppler; do
    echo "Checking the example PDFs in $engine"
    "$WORK/venv/bin/python" .github/scripts/check-viewers.py $engine \
        "$WORK/pdfs/java" "$WORK/encrypted" "$WORK/viewers/$engine" || exit 1
done

# The snippets of the booklet, which it works on in build/check-snippets with
# the Python environment above.
echo "Building and running the booklet snippets"
PYTHON="$WORK/venv/bin/python" bash booklet/check-snippets.sh
