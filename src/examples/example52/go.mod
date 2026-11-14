// Example_52 is a module of its own: it shapes with go-text/typesetting, a
// dependency of the example alone, never of PDFjet.
module example52

go 1.26.0

require (
	github.com/edragoev1/pdfjet/v9 v9.0.0
	golang.org/x/image v0.23.0
)

require github.com/go-text/typesetting v0.3.5

replace github.com/edragoev1/pdfjet/v9 => ../../..
