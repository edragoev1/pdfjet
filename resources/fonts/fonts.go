// fonts.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

// Package fonts holds the fonts PDFjet carries in itself, each family in a
// folder of its name with its license, as in the folder fonts of
// pdfjet-fonts: IBM Plex Sans and Serif, from Thin to Bold, Source Serif 4
// and Cascadia Mono, from Light to Bold, upright and italic; IBM Plex Sans
// Arabic, Hebrew and Thai, Thin to Bold; IBM Plex Math; and IBM Plex Sans JP,
// KR, SC and TC, Regular, SemiBold and Bold, as .otf. All are under the SIL
// Open Font License 1.1.
//
// The pdfjet package reads them from here: a path to one of them, such as
// the constants of the packages IBMPlexSans and the others, is read from FS
// when there is no such file, and the text of an SVG image is drawn in them
// unless a font is set. Go embeds the files of the folder of a package
// alone, so this package is in resources/fonts itself.
package fonts

import "embed"

// FS has the font files, by Family/File, such as
// IBMPlexSans/IBMPlexSans-Regular.ttf, and the licenses of the families.
//
//go:embed */*.ttf */*.otf */license.txt CascadiaMono/LICENSE SourceSerif4/OFL.txt
var FS embed.FS
