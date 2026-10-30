// fonts.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

// Package fonts holds the fonts PDFjet carries in itself, each family in a
// folder of its name with its license: IBM Plex Sans and Serif, from Thin to
// Bold, and Cascadia Mono, from Light to Bold, upright and italic, for every
// European language, in Latin, Greek and Cyrillic. All are under the SIL
// Open Font License 1.1.
//
// Go builds the files a package embeds into every program that imports it,
// uncompressed, so this package embeds the European fonts alone, about 11 MB.
// The fonts of every other script, IBM Plex Sans JP, KR, SC and TC, IBM Plex
// Sans Arabic, Hebrew and Thai, about 52 MB, are in the
// folder world, the package that embeds them, and a program builds them in
// with one line:
//
//	import _ "github.com/edragoev1/pdfjet/v9/resources/fonts/world"
//
// The pdfjet package reads the fonts from here: a path to one of them, such
// as the constants of the packages IBMPlexSans and the others, is read from
// the folder resources/fonts of the working directory, or
// resources/fonts/world for a font of the package world, when there is no
// such file, else from here, and the text of an SVG image is drawn in them
// unless a font is set. Go embeds the files of the folder of a package
// alone, so this package is in resources/fonts itself.
package fonts

import (
	"embed"
	"io/fs"
	"strings"
	"sync"
)

// FS has the font files built into every program, by Family/File, such as
// IBMPlexSans/IBMPlexSans-Regular.ttf, and the licenses of the families.
//
//go:embed CascadiaMono/*.ttf CascadiaMono/LICENSE
//go:embed IBMPlexSans/*.ttf IBMPlexSans/license.txt
//go:embed IBMPlexSerif/*.ttf IBMPlexSerif/license.txt
var FS embed.FS

var (
	mutex    sync.RWMutex
	families = map[string]fs.FS{} // The families registered, by name
)

// Register adds a family of fonts that FS leaves out, its files by their
// names alone, such as IBMPlexSansJP-Regular.otf. It is for the package
// world, which registers its families when a program imports it;
// a program has no need to call it.
func Register(family string, files fs.FS) {
	mutex.Lock()
	defer mutex.Unlock()
	families[family] = files
}

// ReadFile reads a font file or a license by Family/File, from FS or from a
// family registered.
func ReadFile(name string) ([]byte, error) {
	if files, file := registered(name); files != nil {
		return fs.ReadFile(files, file)
	}
	return FS.ReadFile(name)
}

// Stat describes a font file or a license by Family/File, from FS or from a
// family registered.
func Stat(name string) (fs.FileInfo, error) {
	if files, file := registered(name); files != nil {
		return fs.Stat(files, file)
	}
	return fs.Stat(FS, name)
}

// registered returns the files of the family of the name and the name of
// the file among them, or nil when the family is not registered.
func registered(name string) (fs.FS, string) {
	family, file, ok := strings.Cut(name, "/")
	if !ok {
		return nil, ""
	}
	mutex.RLock()
	defer mutex.RUnlock()
	return families[family], file
}
