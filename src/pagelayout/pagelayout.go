package pagelayout

/**
 * pagelayout.go
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */

// Used to specify the PDF page layout.
const (
	SinglePage     = "SinglePage"     // Display one page at a time
	OneColumn      = "OneColumn"      // Display the pages in one column
	TwoColumnLeft  = "TwoColumnLeft"  // Odd-numbered pages on the left
	TwoColumnRight = "TwoColumnRight" // Odd-numbered pages on the right
	TwoPageLeft    = "TwoPageLeft"    // Odd-numbered pages on the left
	TwoPageRight   = "TwoPageRight"   // Odd-numbered pages on the right
)
