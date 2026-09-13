#!/bin/bash

# Builds the Java port and its unit tests in tests/java, and runs the tests with
# the JUnit Platform console launcher. JUnit is the only dependency of the tests,
# not of the library: the script downloads its standalone jar once, into
# build/junit, and checks its SHA-256. JUnit 5 runs on Java 8, so the tests build
# and run there too.

cd "$(dirname "$0")" || exit 1

JUNIT_VERSION=1.14.4
JUNIT_SHA256=7c6968cbcaf4301c729f202b23b7d736c5d88be625fc7d27ad5d746146a8bc28
JUNIT=build/junit/junit-platform-console-standalone-$JUNIT_VERSION.jar
JUNIT_URL=https://repo1.maven.org/maven2/org/junit/platform/junit-platform-console-standalone/$JUNIT_VERSION/junit-platform-console-standalone-$JUNIT_VERSION.jar

if [ ! -f "$JUNIT" ]; then
    mkdir -p build/junit
    curl -fsSL --retry 3 -o "$JUNIT.part" "$JUNIT_URL" || exit 1
    mv "$JUNIT.part" "$JUNIT"
fi
if command -v sha256sum > /dev/null; then
    actual=$(sha256sum "$JUNIT" | cut -d ' ' -f 1)
else
    actual=$(shasum -a 256 "$JUNIT" | cut -d ' ' -f 1)
fi
if [ "$actual" != "$JUNIT_SHA256" ]; then
    echo "The SHA-256 of $JUNIT is not $JUNIT_SHA256."
    rm -f "$JUNIT"
    exit 1
fi

# --release 8 as in build-java.sh; Java 8's javac has no such option.
RELEASE="--release 8"
if ! javac --release 8 -version > /dev/null 2>&1; then
    RELEASE=""
fi

OUT=build/test-java
rm -rf "$OUT"
mkdir -p "$OUT/classes" "$OUT/test-classes"

javac -O -encoding utf-8 $RELEASE -Xlint -Xlint:-options -Werror \
    com/pdfjet/*.java \
    com/pdfjet/barcodes/*.java \
    com/pdfjet/pdf417/*.java \
    com/pdfjet/qrcode/*.java \
    com/pdfjet/datamatrix/*.java \
    com/pdfjet/fonts/*.java \
    com/pdfjet/encryption/*.java \
    -d "$OUT/classes" || exit 1

javac -encoding utf-8 $RELEASE -Xlint -Xlint:-options -Werror -proc:none \
    -cp "$OUT/classes:$JUNIT" \
    $(find tests/java -name '*.java' | sort) \
    -d "$OUT/test-classes" || exit 1

java -jar "$JUNIT" execute \
    --class-path "$OUT/classes:$OUT/test-classes" \
    --scan-class-path "$OUT/test-classes" \
    --disable-banner \
    --details=summary \
    --fail-if-no-tests
