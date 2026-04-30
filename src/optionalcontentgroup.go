package pdfjet

/**
 * optionalcontentgroup.go
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 *
 * Original author: Mark Paxton
 * Modified and adapted for use in PDFjet by Evgeni Dragoev
 */

import (
	"encoding/hex"
	"fmt"

	"github.com/edragoev1/pdfjet/src/encryption"
	"github.com/edragoev1/pdfjet/src/token"
)

// OptionalContentGroup is container for drawable objects that can be drawn on a page as part of Optional Content Group.
// Please see the PDF specification and Example_30 for more details.
//
// @author Mark Paxton
type OptionalContentGroup struct {
	objNumber  int
	pdf        *PDF
	name       string
	ocgNumber  int
	visible    bool
	printable  bool
	exportable bool
	components []Drawable
}

// NewOptionalContentGroup constructs optional content group.
func NewOptionalContentGroup(pdf *PDF, name string) *OptionalContentGroup {
	ocg := new(OptionalContentGroup)
	ocg.pdf = pdf
	ocg.name = name
	ocg.ocgNumber = -1
	ocg.components = make([]Drawable, 0)
	return ocg
}

// Add appends drawable component to this optional content group.
func (ocg *OptionalContentGroup) Add(drawable Drawable) {
	ocg.components = append(ocg.components, drawable)
}

// SetVisible sets the visibility of the group.
func (ocg *OptionalContentGroup) SetVisible(visible bool) {
	ocg.visible = visible
}

// SetPrintable sets the printable components.
func (ocg *OptionalContentGroup) SetPrintable(printable bool) {
	ocg.printable = printable
}

// SetExportable sets the exportable components.
func (ocg *OptionalContentGroup) SetExportable(exportable bool) {
	ocg.exportable = exportable
}

// DrawOn draws the components in the optional content group on the page.
func (ocg *OptionalContentGroup) DrawOn(page *Page) {
	if ocg.ocgNumber == -1 {
		ocg.pdf.newobj()
		ocg.pdf.appendByteArray(token.BeginDictionary)
		ocg.pdf.appendString("/Type /OCG\n")

		nameBytes := []byte(ocg.name)
		if ocg.pdf.encryption != nil {
			var err error
			nameBytes, err = encryption.Encrypt(nameBytes, ocg.pdf.encryption.GetKey())
			if err != nil {
				fmt.Println("encryption failed:", err)
				return
			}
		}
		ocg.pdf.appendString("/Name <")
		ocg.pdf.appendString(hex.EncodeToString(nameBytes))
		ocg.pdf.appendString(">\n")

		ocg.pdf.appendString("/Usage <<\n")
		if ocg.visible {
			ocg.pdf.appendString("/View << /ViewState /ON >>\n")
		} else {
			ocg.pdf.appendString("/View << /ViewState /OFF >>\n")
		}
		if ocg.printable {
			ocg.pdf.appendString("/Print << /PrintState /ON >>\n")
		} else {
			ocg.pdf.appendString("/Print << /PrintState /OFF >>\n")
		}
		if ocg.exportable {
			ocg.pdf.appendString("/Export << /ExportState /ON >>\n")
		} else {
			ocg.pdf.appendString("/Export << /ExportState /OFF >>\n")
		}
		ocg.pdf.appendString(">>\n")
		ocg.pdf.appendByteArray(token.EndDictionary)
		ocg.pdf.endobj()

		ocg.objNumber = ocg.pdf.getObjNumber()

		ocg.pdf.groups = append(ocg.pdf.groups, ocg)
		ocg.ocgNumber = len(ocg.pdf.groups)
	}

	if len(ocg.components) > 0 {
		page.appendString("/OC /OC")
		page.appendInteger(ocg.ocgNumber)
		page.appendString(" BDC\n")
		for _, component := range ocg.components {
			component.DrawOn(page)
		}
		page.appendString("\nEMC\n")
	}
}
