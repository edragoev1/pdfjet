module github.com/edragoev1/pdfjet/v9

go 1.27.0

// The Go module proxy cannot serve v9.0.0 to v9.0.2: the fonts in their tree
// make it time out. From v9.0.3 the fonts are in the repository pdfjet-fonts.
retract [v9.0.0, v9.0.2]
