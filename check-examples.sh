#!/bin/bash
# Runs the checks of the Build workflow locally, so they can pass before a push.
#
#   ./check-examples.sh
#
# Builds the Java, C#, Go and Swift ports and runs their examples with the
# build-*.sh scripts and their unit tests with the test-*.sh scripts, as the
# workflow's port jobs do, builds and tests the Java port again with JDK 8, as
# its java (JDK 8) job does, then checks the example PDFs with
# .github/scripts/check-example-pdfs.py, as its compare job does.
#
# The ports are built one after another in this folder. clean.sh runs before
# each one, so no output of an earlier build, like the DLL of an example that
# no longer compiles, is used. The PDFs and build logs are kept in
# build/check-examples, with the Python environment the check runs in.
#
# Needs the four toolchains, python3 and veraPDF, run as "verapdf" or as the
# command in the VERAPDF environment variable, and a JDK 8, found in the
# JAVA8_HOME environment variable or in /opt/jdk8* or /usr/lib/jvm.

cd "$(dirname "$0")" || exit 1

WORK=build/check-examples

VERAPDF=${VERAPDF:-verapdf}
if ! command -v "$VERAPDF" > /dev/null; then
    echo "veraPDF was not found. Install it, see https://docs.verapdf.org/install/,"
    echo "or set VERAPDF to its command."
    exit 1
fi
export VERAPDF

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
    for i in $(seq -w 1 51); do
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
for i in $(seq -w 1 51); do
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

"$WORK/venv/bin/python" .github/scripts/check-example-pdfs.py "$WORK/pdfs/{port}"
