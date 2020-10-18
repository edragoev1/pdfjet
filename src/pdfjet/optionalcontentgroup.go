package pdfjet

/**
 * optionalcontentgroup.go
 *
Copyright 2020 Innovatics Inc.

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
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
