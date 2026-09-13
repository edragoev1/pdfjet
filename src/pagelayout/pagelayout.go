// pagelayout.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

// Package pagelayout defines the page layouts used when the document is opened.
package pagelayout

// PageLayout specifies the page layout used when the document is opened.
type PageLayout string

// Used to specify the PDF page layout.
const (
	SinglePage     PageLayout = "SinglePage"     // Display one page at a time
	OneColumn      PageLayout = "OneColumn"      // Display the pages in one column
	TwoColumnLeft  PageLayout = "TwoColumnLeft"  // Odd-numbered pages on the left
	TwoColumnRight PageLayout = "TwoColumnRight" // Odd-numbered pages on the right
	TwoPageLeft    PageLayout = "TwoPageLeft"    // Odd-numbered pages on the left
	TwoPageRight   PageLayout = "TwoPageRight"   // Odd-numbered pages on the right
)
