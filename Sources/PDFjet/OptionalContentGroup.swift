/**
 *  OptionalContentGroup.swift
 *
 *  Copyright (c) 2026 PDFjet Software
 *  Licensed under the MIT License. See LICENSE file in the project root.
 *
 *  Original author: Mark Paxton
 *  Modified and adapted for use in PDFjet by Evgeni Dragoev
 */
import Foundation

///
/// Container for drawable objects that can be drawn on a page as part of Optional Content Group.
/// Please see the PDF specification and Example_30 for more details.
///
///  @author Mark Paxton
///
public class OptionalContentGroup {
    var objNumber = 0
    var name: String?

    private var pdf: PDF
    private var ocgNumber: Int = -1
    private var visible: Bool?
    private var printable: Bool?
    private var exportable: Bool?
    private var components = [Drawable]()

    public init(_ pdf: PDF, _ name: String) {
        self.pdf = pdf
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
        if ocgNumber == -1 {
            pdf.newobj()
            pdf.append(Token.beginDictionary)
            pdf.append("/Type /OCG\n")
            pdf.append("/Name (" + name! + ")\n")
            pdf.append("/Usage <<\n")
            if visible != nil {
                pdf.append("/View << /ViewState /ON >>\n")
            } else {
                pdf.append("/View << /ViewState /OFF >>\n")
            }
            if printable != nil {
                pdf.append("/Print << /PrintState /ON >>\n")
            } else {
                pdf.append("/Print << /PrintState /OFF >>\n")
            }
            if exportable != nil {
                pdf.append("/Export << /ExportState /ON >>\n")
            } else {
                pdf.append("/Export << /ExportState /OFF >>\n")
            }
            pdf.append(">>\n")
            pdf.append(Token.endDictionary)
            pdf.endobj()

            objNumber = pdf.getObjNumber()

            pdf.groups.append(self)
            ocgNumber = pdf.groups.count
        }

        if components.count > 0 {
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
