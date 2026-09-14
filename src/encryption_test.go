// encryption_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"regexp"
	"strconv"
	"strings"
	"testing"

	"github.com/edragoev1/pdfjet/v9/src/compliance"
	"github.com/edragoev1/pdfjet/v9/src/encryption"
	"github.com/edragoev1/pdfjet/v9/src/letter"
)

// AES-256 encryption with the standard security handler, read back with the
// passwords. The Java tests are in the encryption package; Go's Encryption is
// in the root package.

func testEncrypted(t *testing.T, level compliance.Compliance, passwords *encryption.Passwords, permissions *encryption.Permissions) []byte {
	t.Helper()
	doc := testNewDoc()
	doc.pdf.SetCompliance(level)
	doc.pdf.SetTitle("Encrypted title")
	enc, err := NewEncryption(doc.pdf, passwords, permissions)
	if err != nil {
		t.Fatal(err)
	}
	doc.pdf.SetEncryption(enc)
	page := NewPage(doc.pdf, letter.Portrait())
	line := NewTextLine(testHelvetica(doc.pdf), "Secret text")
	line.SetLocation(50, 50)
	line.DrawOn(page)
	return doc.complete()
}

func testEncryptedWith(t *testing.T, user, owner string) []byte {
	t.Helper()
	return testEncrypted(t, compliance.PDF_1_7,
		encryption.NewPasswords().SetUserPassword(user).SetOwnerPassword(owner),
		encryption.NewPermissions().Grant(encryption.Print))
}

func testAccessValue(t *testing.T, pdf []byte) int {
	t.Helper()
	match := regexp.MustCompile(`/P (-?\d+)`).FindSubmatch(pdf)
	if match == nil {
		t.Fatal("no /P")
	}
	value, _ := strconv.Atoi(string(match[1]))
	return value
}

func testReadTitle(t *testing.T, pdf []byte, password string) string {
	t.Helper()
	objects, err := testNewPDF().ReadWithPassword(pdf, password)
	if err != nil {
		t.Fatal(err)
	}
	info := testFindObject(objects, "/Producer")
	if info == nil {
		t.Fatal("no info dictionary")
	}
	return testUTF16Hex(t, info.GetValue("/Title"))
}

func TestEncryptionReadsBackWithTheUserOrTheOwnerPassword(t *testing.T) {
	pdf := testEncryptedWith(t, "hello", "world")
	for _, password := range []string{"hello", "world"} {
		objects, err := testNewPDF().ReadWithPassword(pdf, password)
		if err != nil {
			t.Fatal(err)
		}
		if got := len(testNewPDF().GetPageObjects(objects)); got != 1 {
			t.Errorf("%s: pages %d", password, got)
		}
		if got := testReadTitle(t, pdf, password); got != "Encrypted title" {
			t.Errorf("%s: title %q", password, got)
		}
	}
}

func TestEncryptionTheTitleIsNotInTheFileInPlainText(t *testing.T) {
	raw := string(testEncryptedWith(t, "hello", "world"))
	if strings.Contains(raw, "feff0045006e0063") || !strings.Contains(raw, "/Encrypt ") {
		t.Error("the title is in plain text or there is no /Encrypt")
	}
}

func TestEncryptionAWrongOrMissingPasswordFailsWithAMessage(t *testing.T) {
	pdf := testEncryptedWith(t, "hello", "world")
	if _, err := testNewPDF().ReadWithPassword(pdf, "wrong"); err == nil || err.Error() != "The password of the PDF is not correct." {
		t.Errorf("wrong password: %v", err)
	}
	if _, err := testNewPDF().Read(pdf); err == nil || err.Error() != "The PDF needs a password." {
		t.Errorf("missing password: %v", err)
	}
}

func TestEncryptionAnEmptyUserPasswordOpensWithoutAPassword(t *testing.T) {
	info := testFindObject(testRead(t, testEncryptedWith(t, "", "owner")), "/Producer")
	if info == nil || testUTF16Hex(t, info.GetValue("/Title")) != "Encrypted title" {
		t.Error("wrong title")
	}
}

func TestEncryptionANonAsciiPasswordIsUtf8(t *testing.T) {
	pdf := testEncryptedWith(t, "пароль", "owner")
	if got := testReadTitle(t, pdf, "пароль"); got != "Encrypted title" {
		t.Errorf("title %q", got)
	}
}

func TestEncryptionPasswordsAreCutAt127Bytes(t *testing.T) {
	password := strings.Repeat("a", 200)
	pdf := testEncryptedWith(t, password, "owner")
	if got := testReadTitle(t, pdf, password[:127]); got != "Encrypted title" {
		t.Errorf("title %q", got)
	}
	if _, err := testNewPDF().ReadWithPassword(pdf, password[:126]); err == nil {
		t.Error("126 bytes opened the PDF")
	}
}

func TestEncryptionPermissionsAreANegativeNumberWithTheReservedBitsSet(t *testing.T) {
	if got := testAccessValue(t, testEncryptedWith(t, "hello", "world")); got != -3900 {
		t.Errorf("/P %d", got)
	}
}

func TestEncryptionPdfUaGrantsExtractionForAccessibility(t *testing.T) {
	pdf := testEncrypted(t, compliance.PDF_UA_1,
		encryption.NewPasswords().SetUserPassword("hello").SetOwnerPassword("world"),
		encryption.NewPermissions().Grant(encryption.Print))
	access := testAccessValue(t, pdf)
	if access != -3388 {
		t.Errorf("/P %d", access)
	}
	if !encryption.ExtractContentsForAccessibility.IsSetIn(encryption.UserAccess(uint32(int32(access)))) {
		t.Error("no extraction for accessibility")
	}
}
