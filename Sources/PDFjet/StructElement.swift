/**
 * StructElement.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */

/// A structure element of the structure tree that PDF writes for a tagged
/// document: the marked content it refers to and its kids.
class StructElement {
    var objNumber: Int?
    var structure: String?
    var pageObjNumber: Int?
    // The marked content this element refers to, or -1 for an element that
    // groups its kids, like a table row.
    var mcid = 0
    // The attributes dictionary, like <</O /Table /Scope /Column>>, or nil.
    var attributes: String?
    // The parent element, or nil for a child of the Document element. The
    // pages hold the elements, so the parent does not hold on to them.
    weak var parent: StructElement?
    // The object number of the parent, which an element that is written when
    // the document is completed, like the Link of a text and its annotation,
    // refers to after its page has let its parent go.
    var parentObjNumber: Int?
    // The structure type of the parent, "" for the Document, which tells
    // whether an element PDF makes inline stands as a block after the page
    // has let its parent go.
    var parentStructure = ""
    // The object numbers of the kids. A parent keeps the numbers and not the
    // kids, so that a page can write its elements and let go of them.
    var kids = [Int]()
    // The marked contents of an element that holds several of them, like a
    // paragraph whose words are drawn one at a time; and, in their order among
    // them, the elements of the words that are links, as the negative of their
    // object numbers.
    var mcids = [Int]()
    // True for an element a drawable goes on adding to after the page it was
    // made on is written, like the Table of a table that runs over pages. It
    // is written when the document is completed; every other element is
    // written with its page and let go of.
    var open = false
    var language: String?
    var altDescription: String?
    var actualText: String?
    var annotation: Annotation?

    // The structure types that PDF makes inline, which stand in a paragraph;
    // one that is a kid of an element that groups others, like the Document,
    // stands as a block, and says so with the attribute Placement Block, or
    // PAC warns of it as a possibly inappropriate use: a figure, a link or an
    // annotation drawn on its own, and not inside a paragraph.
    private static let inlineLevelStructures: Set<String> = [
        "Figure", "Formula", "Form", "Note", "Link", "Annot",
    ]

    // The structure types that group others and hold blocks, not text.
    private static let groupingStructures: Set<String> = [
        "Document", "Part", "Art", "Sect", "Div", "BlockQuote",
        "Caption", "TOC", "TOCI", "Index", "NonStruct", "Private",
    ]

    // Whether the element is of an inline type and stands as a block, a kid
    // of the Document or of another element that groups others.
    func placedAsBlock() -> Bool {
        guard let structure = structure, StructElement.inlineLevelStructures.contains(structure) else {
            return false
        }
        return parentStructure == "" || StructElement.groupingStructures.contains(parentStructure)
    }

    // The attributes with Placement Block among those of the owner Layout: in
    // the Layout attributes the element has, like the BBox of a figure, or in
    // their own dictionary beside attributes of another owner.
    static func withPlacementBlock(_ attributes: String?) -> String {
        let layout = "<</O /Layout "
        guard let attributes = attributes, !attributes.isEmpty else {
            return "<</O /Layout /Placement /Block>>"
        }
        if attributes.hasPrefix(layout) {
            return layout + "/Placement /Block " + attributes.dropFirst(layout.count)
        }
        return "[" + attributes + " <</O /Layout /Placement /Block>>]"
    }
}
