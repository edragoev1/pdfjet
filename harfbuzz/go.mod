// The HarfBuzz module of PDFjet: ComplexText's shaper for Go, HarfBuzz in
// pure Go (go-text/typesetting), on every system and CPU Go runs on, with
// nothing to install. A module of its own, so that PDFjet itself keeps no
// dependency. Released with PDFjet, of the same version: the tag
// harfbuzz/v9.x.y beside v9.x.y. The replace is for working in this
// repository; a program that imports the module gets the PDFjet of require.
module github.com/edragoev1/pdfjet/harfbuzz/v9

go 1.26.0

require (
	github.com/edragoev1/pdfjet/v9 v9.1.9
	github.com/go-text/typesetting v0.3.5
	golang.org/x/image v0.23.0
)

replace github.com/edragoev1/pdfjet/v9 => ../
