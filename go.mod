module github.com/edragoev1/pdfjet/v9

go 1.26.0

toolchain go1.27.2

// The Go module proxy cannot serve v9.0.0 to v9.0.2: the fonts in their tree
// make it time out. From v9.0.3 the fonts were in the repository pdfjet-fonts;
// from v9.1.7 the module has the fonts PDFjet carries, in resources/fonts.
retract [v9.0.0, v9.0.2]
