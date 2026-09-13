// imagetype.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

// Package imagetype defines the supported image types.
package imagetype

// ImageType specifies the type of an image.
type ImageType int

// Used to specify the image type.
// Supported types: imagetype.JPG, imagetype.PNG and imagetype.BMP
// See the Image class for more information.
const (
	JPG ImageType = iota
	PNG
	BMP
)
