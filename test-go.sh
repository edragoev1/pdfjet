#!/bin/bash

# Runs the unit tests of the Go port, the *_test.go files next to the sources in
# src, with the standard testing package. go vet checks the tests as it checks
# the library. The tests read the PngSuite images and the data from the
# repository root, the fonts PDFjet carries from resources/fonts, which the
# library embeds, and the test fonts from tests/fonts.
cd "$(dirname "$0")/src" || exit 1

go vet ./... || exit 1
go test ./...
