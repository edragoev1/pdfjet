#!/bin/bash

# Builds the Swift port and its unit tests in tests/swift, and runs the tests
# with Swift Testing, which comes with the Swift toolchain. The tests are the
# PDFjetTests target of Package.swift. -warnings-as-errors fails the build on
# any warning, as in build-swift.sh.

cd "$(dirname "$0")" || exit 1

swift test -Xswiftc -warnings-as-errors
