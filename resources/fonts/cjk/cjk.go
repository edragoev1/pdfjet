// cjk.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

// Package cjk builds the fonts of the package world into a program, which
// has the Chinese, Japanese and Korean fonts PDFjet carries and those of
// every other script but the European ones, so that a program that imports
// it still builds.
//
// Deprecated: import the package world instead:
//
//	import _ "github.com/edragoev1/pdfjet/v9/resources/fonts/world"
package cjk

import (
	// The fonts, registered with PDFjet by the package world
	_ "github.com/edragoev1/pdfjet/v9/resources/fonts/world"
)
