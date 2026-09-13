// pagemode.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

// Package pagemode defines the page modes used when the document is opened.
package pagemode

// PageMode specifies the page mode used when the document is opened.
type PageMode string

// Constants used to specify the PDF page mode.
const (
	UseNone        PageMode = "UseNone"        // Neither document outline nor thumbnail images visible
	UseOutlines    PageMode = "UseOutlines"    // Document outline visible
	UseThumbs      PageMode = "UseThumbs"      // Thumbnail images visible
	FullScreen     PageMode = "FullScreen"     // Full-screen mode
	UseOC          PageMode = "UseOC"          // (PDF 1.5) Optional content group panel visible
	UseAttachments PageMode = "UseAttachments" // (PDF 1.6) Attachments panel visible
)
