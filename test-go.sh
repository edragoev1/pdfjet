#!/bin/bash

# Runs the unit tests of the Go port, the *_test.go files next to the sources in
# src, with the standard testing package. go vet checks the tests as it checks
# the library. The tests read the images of tests/data/PngSuite and the data
# from the repository, the fonts PDFjet carries from resources/fonts, which the
# library embeds, and the test fonts from tests/fonts. The packages of
# resources/fonts have no tests, but go vet reads them and go test builds them.
# Built with the tag pdfjet_nofonts, which leaves those fonts out of a program,
# the library is checked by go vet again and by the tests of that tag.
cd "$(dirname "$0")/src" || exit 1

go vet ./... ../resources/fonts/... || exit 1
go vet -tags pdfjet_nofonts . ../resources/fonts || exit 1
go test -tags pdfjet_nofonts -run '^TestNoFonts' . || exit 1
go test ./... ../resources/fonts/...
