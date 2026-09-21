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
    // The object numbers of the kids. A parent keeps the numbers and not the
    // kids, so that a page can write its elements and let go of them.
    var kids = [Int]()
    // True for an element a drawable goes on adding to after the page it was
    // made on is written, like the Table of a table that runs over pages. It
    // is written when the document is completed; every other element is
    // written with its page and let go of.
    var open = false
    var language: String?
    var altDescription: String?
    var actualText: String?
    var annotation: Annotation?
}
