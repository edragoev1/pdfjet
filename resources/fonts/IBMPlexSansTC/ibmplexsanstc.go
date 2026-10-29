// ibmplexsanstc.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

// Package ibmplexsanstc embeds IBM Plex Sans TC, Regular, SemiBold and Bold,
// and its license. A program builds it in by importing the package cjk,
// which registers it with PDFjet, not this package.
package ibmplexsanstc

import "embed"

// FS has the font files and the license, by their names alone.
//
//go:embed *.otf license.txt
var FS embed.FS
