package border

/**
 * border.go
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */

// Constants used to control the visibility of cell borders.
// See the Cell class for more information.
const (
	None   = 0x00000000
	Top    = 0x00010000
	Bottom = 0x00020000
	Left   = 0x00040000
	Right  = 0x00080000
	All    = 0x000F0000
)
