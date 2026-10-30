#!/bin/bash

# Runs the unit tests of the Go port, the *_test.go files next to the sources in
# src, with the standard testing package. go vet checks the tests as it checks
# the library. The tests read the images of tests/data/PngSuite and the data
# from the repository, the fonts PDFjet carries from resources/fonts, which the
# library embeds, and the test fonts from tests/fonts. The packages of
# resources/fonts have no tests, but go vet reads them and go test builds them.
cd "$(dirname "$0")/src" || exit 1

go vet ./... ../resources/fonts/... || exit 1
go test ./... ../resources/fonts/...
