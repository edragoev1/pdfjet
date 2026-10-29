// cjk.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

// Package cjk builds the fonts PDFjet carries for Chinese, Japanese and
// Korean into a program: IBM Plex Sans JP, KR, SC and TC, Regular, SemiBold
// and Bold, about 48 MB. A program that draws in them imports it for its
// effect alone, with one line:
//
//	import _ "github.com/edragoev1/pdfjet/v9/resources/fonts/cjk"
//
// Without it, a path to one of these fonts, such as IBMPlexSansJP.Regular,
// is read from the file of that path, else from the folder resources/fonts
// of the working directory, else it is an error that names this line.
package cjk

import (
	"github.com/edragoev1/pdfjet/v9/resources/fonts"
	"github.com/edragoev1/pdfjet/v9/resources/fonts/IBMPlexSansJP"
	"github.com/edragoev1/pdfjet/v9/resources/fonts/IBMPlexSansKR"
	"github.com/edragoev1/pdfjet/v9/resources/fonts/IBMPlexSansSC"
	"github.com/edragoev1/pdfjet/v9/resources/fonts/IBMPlexSansTC"
)

func init() {
	fonts.Register("IBMPlexSansJP", ibmplexsansjp.FS)
	fonts.Register("IBMPlexSansKR", ibmplexsanskr.FS)
	fonts.Register("IBMPlexSansSC", ibmplexsanssc.FS)
	fonts.Register("IBMPlexSansTC", ibmplexsanstc.FS)
}
