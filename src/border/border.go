// border.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package border

// Used to control the visibility of the borders of a TextBox.
// See textbox.go for more information.
const (
	None   uint32 = 0x00000000
	Top    uint32 = 0x00010000
	Bottom uint32 = 0x00020000
	Left   uint32 = 0x00040000
	Right  uint32 = 0x00080000
	All    uint32 = 0x000F0000
)
