/**
 *  OptionalContentGroup.swift
 *
Copyright 2023 Innovatics Inc.

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
import Foundation

///
/// Container for drawable objects that can be drawn on a page as part of Optional Content Group.
/// Please see the PDF specification and Example_30 for more details.
///
///  @author Mark Paxton
///
public class OptionalContentGroup {
    var name: String?
    var ocgNumber = 0
    var objNumber = 0
    var visible: Bool?
    var printable: Bool?
    var exportable: Bool?

    private var components = [Drawable]()

    public init(_ name: String) {
        self.name = name
    }

    public func add(_ drawable: Drawable) {
        components.append(drawable)
    }

    public func setVisible(_ visible: Bool) {
        self.visible = visible
    }

    public func setPrintable(_ printable: Bool) {
        self.printable = printable
    }

    public func setExportable(_ exportable: Bool) {
        self.exportable = exportable
    }

    public func drawOn(_ page: Page) {
        if !components.isEmpty {
            page.pdf.groups.append(self)
            ocgNumber = page.pdf.groups.count

            page.pdf.newobj()
            page.pdf.append("<<\n")
            page.pdf.append("/Type /OCG\n")
            page.pdf.append("/Name (" + name! + ")\n")
            page.pdf.append("/Usage <<\n")
            if visible != nil {
                page.pdf.append("/View << /ViewState /ON >>\n")
            } else {
                page.pdf.append("/View << /ViewState /OFF >>\n")
            }
            if printable != nil {
                page.pdf.append("/Print << /PrintState /ON >>\n")
            } else {
                page.pdf.append("/Print << /PrintState /OFF >>\n")
            }
            if exportable != nil {
                page.pdf.append("/Export << /ExportState /ON >>\n")
            } else {
                page.pdf.append("/Export << /ExportState /OFF >>\n")
            }
            page.pdf.append(">>\n")
            page.pdf.append(">>\n")
            page.pdf.endobj()

            objNumber = page.pdf.getObjNumber()

            page.append("/OC /OC")
            page.append(ocgNumber)
            page.append(" BDC\n")
            for component in components {
                component.drawOn(page)
            }
            page.append("\nEMC\n")
        }
    }
}   // End of OptionalContentGroup.swift
