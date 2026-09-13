/**
 * BookmarkTests.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Testing
@testable import PDFjet

@Suite struct BookmarkTests {
    @Test func theRootHasNoTitleAndNoDestination() {
        let root = Bookmark(TestSupport.newPDF())
        #expect(root.getTitle() == nil)
        #expect(root.getDestinationName() == nil)
    }

    @Test func aTitleHasItsWhitespaceCollapsed() {
        let pdf = TestSupport.newPDF()
        let root = Bookmark(pdf)
        let page = Page(pdf, Letter.PORTRAIT)
        let child = root.addBookmark(page, Title(TestSupport.helvetica(pdf), "Chapter\t one\n  intro", 10, 10))
        #expect(child.getTitle() == "Chapter one intro")
        #expect(child.getDestinationName() != nil)
        #expect(child.getParent() === root)
    }
}
