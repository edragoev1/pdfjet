// Writes two encrypted PDFs beside Example_30 for check-viewers.py: one with
// a Cyrillic user password, which a viewer must pass on as UTF-8, and one with
// a 200 byte user password, which a viewer must cut at 127 bytes, as PDFjet
// does. The passwords are the ones in PASSWORDS in check-viewers.py.
//
//	go run ./.github/scripts/encrypted-pdfs OUTPUT_FOLDER
//
// Run it from the root of the repository, which has the fonts.

package main

import (
	"fmt"
	"log"
	"os"
	"path/filepath"
	"strings"

	pdfjet "github.com/edragoev1/pdfjet/v9/src"
	"github.com/edragoev1/pdfjet/v9/src/IBMPlexSans"
	"github.com/edragoev1/pdfjet/v9/src/encryption"
	"github.com/edragoev1/pdfjet/v9/src/letter"
)

func write(path, user, owner, text string) {
	pdf, err := pdfjet.NewPDFFile(path)
	if err != nil {
		log.Fatal(err)
	}
	passwords := encryption.NewPasswords().SetUserPassword(user).SetOwnerPassword(owner)
	permissions := encryption.NewPermissions().Grant(encryption.Print | encryption.PrintHighQuality)
	enc, err := pdfjet.NewEncryption(pdf, passwords, permissions)
	if err != nil {
		log.Fatal(err)
	}
	pdf.SetEncryption(enc)

	font := pdfjet.NewFontFromFile(pdf, IBMPlexSans.Regular)
	font.SetSize(24.0)
	page := pdfjet.NewPage(pdf, letter.Portrait())
	line := pdfjet.NewTextLine(font, text)
	line.SetLocation(72.0, 100.0)
	line.DrawOn(page)
	if err := pdf.Complete(); err != nil {
		log.Fatal(err)
	}
}

func main() {
	if len(os.Args) != 2 {
		fmt.Println("Usage: go run ./.github/scripts/encrypted-pdfs OUTPUT_FOLDER")
		os.Exit(1)
	}
	out := os.Args[1]
	if err := os.MkdirAll(out, 0o755); err != nil {
		log.Fatal(err)
	}
	write(filepath.Join(out, "Encrypted_Cyrillic.pdf"), "пароль", "владелец",
		"Opened with a Cyrillic password")
	write(filepath.Join(out, "Encrypted_200_Bytes.pdf"), strings.Repeat("0123456789", 20), "world",
		"Opened with a 200 byte password")
}
