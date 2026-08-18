/**
 * pagemode.go
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */

package pagemode

// Constants used to specify the PDF page mode.
const (
	UseNone     = "UseNone"     // Neither document outline nor thumbnail images visible
	UseOutlines = "UseOutlines" // Document outline visible
	UseThumbs   = "UseThumbs"   // Thumbnail images visible
	FullScreen  = "FullScreen"  // Full-screen mode
	UseOC       = "UseOC"       // (PDF 1.5) Optional content group panel visible
)
