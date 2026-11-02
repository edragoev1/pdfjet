// fonts_embedded.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

//go:build !pdfjet_nofonts

package fonts

import "embed"

// FS has the font files built into every program, by Family/File, such as
// IBMPlexSans/IBMPlexSans-Regular.ttf, and the licenses of the families.
//
//go:embed CascadiaMono/*.ttf CascadiaMono/LICENSE
//go:embed IBMPlexSans/*.ttf IBMPlexSans/license.txt
//go:embed IBMPlexSerif/*.ttf IBMPlexSerif/license.txt
var FS embed.FS
