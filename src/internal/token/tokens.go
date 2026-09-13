// token.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

// Package token defines PDF syntax tokens as byte slices. It is internal,
// as Token is in the other ports, because every PDF being written shares the
// slices.
package token

// PDF structure tokens as byte arrays.
// WARNING: These are shared, mutable byte slices. Treat as READ-ONLY - DO NOT MODIFY!
// Any modification will corrupt PDF generation.
var (
	Space           = byte(' ')
	Newline         = byte('\n')
	BeginDictionary = []byte("<<\n")
	EndDictionary   = []byte(">>\n")
	Stream          = []byte("stream\n")
	EndStream       = []byte("\nendstream\n")
	NewObj          = []byte(" 0 obj\n")
	EndObj          = []byte("endobj\n")
	ObjRef          = []byte(" 0 R\n")
	BeginText       = []byte("BT\n")
	EndText         = []byte("ET\n")
	Length          = []byte("/Length ")
	Type            = []byte("/Type ")
	Resources       = []byte("/Resources ")
)
