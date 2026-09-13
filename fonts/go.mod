// This file keeps the fonts out of the github.com/edragoev1/pdfjet/v9 Go
// module. Go leaves a directory that has its own go.mod out of a module, and
// with the fonts the module is larger than the 500 MiB Go allows, so go get
// fails. The directory has no Go code and nothing imports this module.
module github.com/edragoev1/pdfjet/fonts
