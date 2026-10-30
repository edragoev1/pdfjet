// cjk.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

// Package cjk builds the fonts PDFjet carries for Chinese, Japanese and
// Korean into a program: IBM Plex Sans JP, KR, SC and TC, Regular, SemiBold
// and Bold, about 48 MB, each family in the folder of its name here, with
// its license. A program that draws in them imports it for its effect
// alone, with one line:
//
//	import _ "github.com/edragoev1/pdfjet/v9/resources/fonts/cjk"
//
// Without it, a path to one of these fonts, such as IBMPlexSansJP.Regular,
// is read from the file of that path, else from the folder
// resources/fonts/cjk of the working directory, else it is an error that
// names this line.
//
// This folder is the one copy of the CJK fonts in the repository: the Java
// package com.pdfjet:pdfjet-fonts-cjk, the NuGet package PDFjet.Fonts.CJK
// and the Swift product PDFjetFontsCJK, whose source is beside this file,
// are made of it too.
package cjk

import (
	"embed"
	"io/fs"

	"github.com/edragoev1/pdfjet/v9/resources/fonts"
)

// files has the font files and the licenses, by Family/File.
//
//go:embed IBMPlexSansJP/*.otf IBMPlexSansJP/license.txt
//go:embed IBMPlexSansKR/*.otf IBMPlexSansKR/license.txt
//go:embed IBMPlexSansSC/*.otf IBMPlexSansSC/license.txt
//go:embed IBMPlexSansTC/*.otf IBMPlexSansTC/license.txt
var files embed.FS

func init() {
	for _, family := range []string{"IBMPlexSansJP", "IBMPlexSansKR", "IBMPlexSansSC", "IBMPlexSansTC"} {
		sub, err := fs.Sub(files, family)
		if err != nil {
			panic(err)
		}
		fonts.Register(family, sub)
	}
}
