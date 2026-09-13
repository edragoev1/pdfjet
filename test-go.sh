#!/bin/bash

# Runs the unit tests of the Go port, the *_test.go files next to the sources in
# src, with the standard testing package. go vet checks the tests as it checks
# the library. The tests read the PngSuite images and the fonts from the
# repository root; the ones that need the fonts skip when they are not there.

cd "$(dirname "$0")/src" || exit 1

go vet ./... || exit 1
go test ./...
