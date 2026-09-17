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
    var visible = false
    private var printable = false
    private var exportable = false
    private var components = [Drawable]()

    /// Creates an optional content group, also called a layer.
    public init(_ pdf: PDF, _ name: String) {
        self.pdf = pdf
        self.name = name
    }

    /// Returns the name of this group.
    public func getName() -> String {
        return self.name!
    }

    /// Adds a drawable to this group.
    @discardableResult
    public func add(_ drawable: Drawable) -> OptionalContentGroup {
        components.append(drawable)
        return self
    }

    /// Removes all drawables from this group.
    @discardableResult
    public func clear() -> OptionalContentGroup {
        components.removeAll()
        return self
    }

    /// Returns the drawables in this group.
    public func getComponents() -> [Drawable] {
        return components
    }

    /// Sets whether this group is visible.
    @discardableResult
    public func setVisible(_ visible: Bool) -> OptionalContentGroup {
        self.visible = visible
        return self
    }

    /// Sets whether this group is printed.
    @discardableResult
    public func setPrintable(_ printable: Bool) -> OptionalContentGroup {
        self.printable = printable
        return self
    }

    /// Sets whether this group is exported.
    @discardableResult
    public func setExportable(_ exportable: Bool) -> OptionalContentGroup {
        self.exportable = exportable
        return self
    }

    /// Draws this group and its drawables on the specified page.
    ///
    /// - Returns: the largest x and y coordinates of the bottom right corners of the drawables in this group.
    @discardableResult
    public func drawOn(_ page: Page) -> [Float] {
        if page.pdf !== pdf {
            page.pdf.fail("The optional content group belongs to another PDF.")
            return [0.0, 0.0]
        }
        var xy: [Float] = [0.0, 0.0]
        if ocgNumber == -1 {
            pdf.newObj()
            pdf.append(Token.beginDictionary)
            pdf.append("/Type /OCG\n")
            pdf.append("/Name <")
            pdf.append(pdf.textString(name!))
            pdf.append(">\n")
            pdf.append("/Usage <<\n")
            if visible {
                pdf.append("/View << /ViewState /ON >>\n")
            } else {
                pdf.append("/View << /ViewState /OFF >>\n")
            }
            if printable {
                pdf.append("/Print << /PrintState /ON >>\n")
            } else {
                pdf.append("/Print << /PrintState /OFF >>\n")
            }
            if exportable {
                pdf.append("/Export << /ExportState /ON >>\n")
            } else {
                pdf.append("/Export << /ExportState /OFF >>\n")
            }
            pdf.append(">>\n")
            pdf.append(Token.endDictionary)
            pdf.endObj()

            objNumber = pdf.getObjNumber()

            pdf.groups.append(self)
            ocgNumber = pdf.groups.count
        }

        if components.count > 0 {
            page.append("/OC /OC")
            page.append(ocgNumber)
            page.append(" BDC\n")
            for component in components {
                let corner = component.drawOn(page)
                xy[0] = max(xy[0], corner[0])
                xy[1] = max(xy[1], corner[1])
            }
            page.append("\nEMC\n")
        }
        return xy
    }
}   // End of OptionalContentGroup.swift
