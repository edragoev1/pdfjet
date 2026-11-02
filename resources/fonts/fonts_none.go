// fonts_none.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

//go:build pdfjet_nofonts

package fonts

import "embed"

// FS is empty with the build tag pdfjet_nofonts: the program draws with fonts
// of its own, and the 11 MB of the fonts PDFjet carries are left out of it.
var FS embed.FS
