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

// Adobe Acrobat opens no AES-256 file whose encryption dictionary has no
// /Length, though PDF 2.0 leaves it optional for V 5: it says it cannot
// decrypt it (found on 1 October 2026, with Example_47).
func TestEncryptionTheDictionarySaysTheKeyIs256BitsForAcrobat(t *testing.T) {
	pdf := testEncryptedWith(t, "", "world")
	if !regexp.MustCompile(`/R 6\n/Length 256\n`).Match(pdf) {
		t.Error("no /Length 256 in the encryption dictionary")
	}
}

func TestEncryptionPermissionsAreANegativeNumberWithTheReservedBitsSet(t *testing.T) {
	if got := testAccessValue(t, testEncryptedWith(t, "hello", "world")); got != -3900 {
		t.Errorf("/P %d", got)
	}
}

func TestEncryptionPdfUaGrantsExtractionForAccessibility(t *testing.T) {
	testAllowCoreFonts(t)
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

func TestEncryptionACryptFilterThatIsAnObjectOfItsOwnIsFollowed(t *testing.T) {
	// The /CF dictionary and the filter in it as objects of their own, as
	// pdf.js tests them in issue7665: the method was not found, and the
	// streams and the strings of the PDF were left encrypted.
	object := func(number int, raw string) *PDFobj {
		obj := getObjectAt([]byte(raw), 0)
		obj.number = number
		return obj
	}
	objects := []*PDFobj{
		object(7, "7 0 obj << /StdCF 8 0 R >> endobj"),
		object(8, "8 0 obj << /AuthEvent /DocOpen /CFM /AESV3 /Length 32 >> endobj"),
		object(9, "9 0 obj << /StdCF << /CFM /AESV2 >> >> endobj"),
	}
	for _, c := range []struct {
		cf   string
		want int
	}{
		{"7 0 R", cryptAES256},
		{"9 0 R", cryptAES128},
		{"<< /StdCF 8 0 R >>", cryptAES256},
		{"<< /StdCF << /CFM /V2 >> >>", cryptRC4},
		{"99 0 R", cryptNone},
	} {
		encrypt := object(6, "6 0 obj << /Filter /Standard /V 5 /CF "+c.cf+" /StmF /StdCF >> endobj")
		if got := getCryptMethod(encrypt, "/StdCF", objects); got != c.want {
			t.Errorf("/CF %s: method %d, want %d", c.cf, got, c.want)
		}
	}
}

// A stamp's stream is encrypted as every other stream of an encrypted
// document, and reads back as it was drawn (review of 11 October 2026, #1).
func TestEncryptionEncryptsTheStreamOfAStamp(t *testing.T) {
	doc := testNewDoc()
	enc, err := NewEncryption(doc.pdf,
		encryption.NewPasswords().SetUserPassword("hello").SetOwnerPassword("world"),
		encryption.NewPermissions().Grant(encryption.Print))
	if err != nil {
		t.Fatal(err)
	}
	doc.pdf.SetEncryption(enc)
	page := NewPage(doc.pdf, letter.Portrait())
	stamp := NewStamp(doc.pdf).SetSize(20, 10)
	stamp.FillRect(0, 0, 20, 10)
	stamp.Complete()
	stamp.DrawOn(page)
	pdf := doc.complete()
	objects, err := testNewPDF().ReadWithPassword(pdf, "hello")
	if err != nil {
		t.Fatal(err)
	}
	var form *PDFobj
	for _, obj := range objects {
		if obj.GetValue("/Subtype") == "/Form" {
			form = obj
		}
	}
	if form == nil {
		t.Fatal("no form XObject")
	}
	if data := string(form.GetData()); !strings.Contains(data, "20 0 l") {
		t.Errorf("the stamp's stream reads back as %q", data)
	}
}

// A password is prepared with SASLprep, as PDF 2.0 asks of AES-256: the PDF
// opens with an accented letter typed as one character or as a letter and an
// accent, with a full-width letter or its ASCII one, and a password of a
// character PDF 2.0 does not allow is refused (review of 11 October 2026, #27).
func TestEncryptionThePasswordIsPreparedWithSASLprep(t *testing.T) {
	for _, c := range []struct{ set, open string }{
		{"cafe\u0301", "caf\u00e9"},
		{"caf\u00e9", "cafe\u0301"},
		{"\uff30\uff21\uff33\uff33", "PASS"},
		{"I\u00adX", "\u2168"},
	} {
		pdf := testEncryptedWith(t, c.set, "owner")
		if _, err := testNewPDF().ReadWithPassword(pdf, c.open); err != nil {
			t.Errorf("set %q, opened with %q: %v", c.set, c.open, err)
		}
		if _, err := testNewPDF().ReadWithPassword(pdf, c.set); err != nil {
			t.Errorf("set %q, opened with it: %v", c.set, err)
		}
	}
	doc := testNewDoc()
	_, err := NewEncryption(doc.pdf, encryption.NewPasswords().SetUserPassword("bell\u0007"),
		encryption.NewPermissions())
	if err == nil || !strings.Contains(err.Error(), "U+0007") {
		t.Errorf("a password of a control character: %v", err)
	}
}

// A PDF whose password was taken as it was, before 9.0.5 and by other
// writers, still opens with it: the password's own bytes are tried after the
// prepared ones.
func TestEncryptionAPasswordTakenAsItWasStillOpens(t *testing.T) {
	got := aes256Passwords("cafe\u0301")
	if len(got) != 2 || string(got[0]) != "caf\u00e9" || string(got[1]) != "cafe\u0301" {
		t.Errorf("%q", got)
	}
	if got := aes256Passwords("ascii"); len(got) != 1 {
		t.Errorf("%q", got)
	}
	// A password that is not UTF-8, Latin-1 here, is tried as its own bytes
	// (review of 12 October 2026, R2).
	if got := aes256Passwords("caf\xe9"); len(got) != 1 || string(got[0]) != "caf\xe9" {
		t.Errorf("%q", got)
	}
}

// The padding of a stream encrypted with AES is a new slice: appended to the
// stream, it was written into the spare capacity of a stream that was read,
// which several goroutines merging one Read wrote at once (review of
// 17 October 2026).
func TestTheAESPaddingLeavesTheStreamAlone(t *testing.T) {
	buffer := make([]byte, 72, 80)
	for i := range buffer[:80] {
		buffer[:80][i] = 0xAA
	}
	padded := aesPkcs7Pad(buffer[:72], 16)
	if len(padded) != 80 || padded[79] != 8 {
		t.Fatalf("the padding is wrong: %d bytes, the last %d", len(padded), padded[len(padded)-1])
	}
	if buffer[:80][72] != 0xAA {
		t.Errorf("the padding was written into the stream's spare capacity")
	}
}
