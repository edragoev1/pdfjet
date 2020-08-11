package pdfjet

/**
 * optionalcontentgroup.go
 *
Copyright 2020 Innovatics Inc.

Redistribution and use in source and binary forms, with or without modification,
are permitted provided that the following conditions are met:

* Redistributions of source code must retain the above copyright notice,
  this list of conditions and the following disclaimer.

* Redistributions in binary form must reproduce the above copyright notice,
  this list of conditions and the following disclaimer in the documentation
  and / or other materials provided with the distribution.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
"AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL,
EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR
PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/

// OptionalContentGroup is container for drawable objects that can be drawn on a page as part of Optional Content Group.
// Please see the PDF specification and Example_30 for more details.
//
// @author Mark Paxton
//
type OptionalContentGroup struct {
	name       string
	objNumber  int
	ocgNumber  int
	visible    bool
	printable  bool
	exportable bool
	components []Drawable
}

// NewOptionalContentGroup constructs optional content group.
func NewOptionalContentGroup(name string) *OptionalContentGroup {
	ocg := new(OptionalContentGroup)
	ocg.name = name
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

// DrawOn draws the components in the optional contect group on the page.
func (ocg *OptionalContentGroup) DrawOn(page *Page) {
	if len(ocg.components) > 0 {
		page.pdf.groups = append(page.pdf.groups, ocg)
		ocg.ocgNumber = len(page.pdf.groups)

		page.pdf.newobj()
		page.pdf.appendString("<<\n")
		page.pdf.appendString("/Type /OCG\n")
		page.pdf.appendString("/Name (" + ocg.name + ")\n")
		page.pdf.appendString("/Usage <<\n")
		if ocg.visible {
			page.pdf.appendString("/View << /ViewState /ON >>\n")
		} else {
			page.pdf.appendString("/View << /ViewState /OFF >>\n")
		}
		if ocg.printable {
			page.pdf.appendString("/Print << /PrintState /ON >>\n")
		} else {
			page.pdf.appendString("/Print << /PrintState /OFF >>\n")
		}
		if ocg.exportable {
			page.pdf.appendString("/Export << /ExportState /ON >>\n")
		} else {
			page.pdf.appendString("/Export << /ExportState /OFF >>\n")
		}
		page.pdf.appendString(">>\n")
		page.pdf.appendString(">>\n")
		page.pdf.endobj()

		ocg.objNumber = page.pdf.getObjNumber()

		appendString(&page.buf, "/OC /OC")
		appendInteger(&page.buf, ocg.ocgNumber)
		appendString(&page.buf, " BDC\n")
		for _, component := range ocg.components {
			component.DrawOn(page)
		}
		appendString(&page.buf, "\nEMC\n")
	}
}
