// fonts_embedded.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

//go:build !pdfjet_nofonts

package fonts

import "embed"

// FS has the font files built into every program, by Family/File, such as
// IBMPlexSans/IBMPlexSans-Regular.otf, and the licenses of the families;
// a program built with the tag pdfjet_nofonts has none (fonts_none.go).
//
//go:embed CascadiaMono/*.otf CascadiaMono/LICENSE
//go:embed IBMPlexSans/*.otf IBMPlexSans/license.txt
//go:embed IBMPlexSerif/*.otf IBMPlexSerif/license.txt
var FS embed.FS

// Embedded tells whether FS has the fonts: false with the tag pdfjet_nofonts.
const Embedded = true
