// This file keeps the data files of the examples and the tests out of the
// github.com/edragoev1/pdfjet/v9 Go module, as resources/images/go.mod keeps
// the images out: a folder with a go.mod of its own is a module of its own,
// which the module zip that go get downloads leaves out. The directory has no
// Go code and nothing imports this module.
module github.com/edragoev1/pdfjet/v9/resources/data

go 1.26.0
