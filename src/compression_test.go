// compression_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"strings"
	"testing"

	"github.com/edragoev1/pdfjet/v9/src/encryption"
)

// testCompressionPDF returns a PDF of one page that draws the text, compressed
// or not, encrypted or not.
func testCompressionPDF(t *testing.T, compression, encrypted bool) []byte {
	t.Helper()
	doc := testNewDoc()
	if encrypted {
		enc, err := NewEncryption(doc.pdf, encryption.NewPasswords(), encryption.NewPermissions())
		if err != nil {
			t.Fatal(err)
		}
		doc.pdf.SetEncryption(enc)
	}
	doc.pdf.SetCompression(compression)
	// Forty lines, as a stream too short to gain is stored as it is
	page := NewPage(doc.pdf, testLetterPortrait())
	font := testHelvetica(doc.pdf)
	for i := 0; i < 40; i++ {
		NewTextLine(font, "Readable").SetLocation(50, float32(50+15*i)).DrawOn(page)
	}
	return doc.complete()
}

// testCompressionContent returns the content of the page, read back from the
// PDF.
func testCompressionContent(t *testing.T, bytes []byte) string {
	t.Helper()
	objects, err := testNewPDF().ReadWithPassword(bytes, "")
	if err != nil {
		t.Fatal(err)
	}
	return testPageContents(objects)[0]
}

func TestCompressionTheContentOfThePagesIsCompressedUnlessSetOtherwise(t *testing.T) {
	compressed := string(testCompressionPDF(t, true, false))
	if !strings.Contains(compressed, "/Filter /FlateDecode") || strings.Contains(compressed, testHex("Readable")) {
		t.Error("the content of the page is not compressed by default")
	}
	plain := string(testCompressionPDF(t, false, false))
	// The fonts are core fonts, so nothing else is compressed
	if strings.Contains(plain, "/Filter /FlateDecode") {
		t.Error("the content of the page is compressed after SetCompression(false)")
	}
	if !strings.Contains(plain, testHex("Readable")) || !strings.Contains(plain, "\nBT\n") {
		t.Error("the text is not in the file as it is drawn")
	}
}

// An encrypted document numbers its font after the encryption, so each is
// compared with the one compressed the other way.
func TestCompressionUncompressedContentIsReadBackTheSameEncryptedOrNot(t *testing.T) {
	plain := testCompressionContent(t, testCompressionPDF(t, true, false))
	if !strings.Contains(plain, testHex("Readable")) {
		t.Fatalf("the text missing from %q", plain)
	}
	if got := testCompressionContent(t, testCompressionPDF(t, false, false)); got != plain {
		t.Errorf("uncompressed %q, compressed %q", got, plain)
	}
	encrypted := testCompressionContent(t, testCompressionPDF(t, true, true))
	if !strings.Contains(encrypted, testHex("Readable")) {
		t.Fatalf("the text missing from %q", encrypted)
	}
	if got := testCompressionContent(t, testCompressionPDF(t, false, true)); got != encrypted {
		t.Errorf("encrypted, uncompressed %q, compressed %q", got, encrypted)
	}
}
