// Example_52 is a module of its own: it imports PDFjet's HarfBuzz module,
// github.com/edragoev1/pdfjet/harfbuzz/v9, which carries go-text/typesetting,
// so that PDFjet itself has no dependency.
module example52

go 1.26.0

require (
	github.com/edragoev1/pdfjet/harfbuzz/v9 v9.1.9
	github.com/edragoev1/pdfjet/v9 v9.2.1
)

require (
	github.com/go-text/typesetting v0.3.5 // indirect
	golang.org/x/image v0.23.0 // indirect
)

replace github.com/edragoev1/pdfjet/v9 => ../../..

replace github.com/edragoev1/pdfjet/harfbuzz/v9 => ../../../harfbuzz
