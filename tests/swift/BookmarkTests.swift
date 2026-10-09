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

    private func item(_ objects: [PDFobj], _ title: String) throws -> PDFobj {
        for obj in objects {
            let value = obj.getValue("/Title")
            if !value.isEmpty && obj.getValue("/Producer").isEmpty && TestSupport.utf16Hex(value) == title {
                return obj
            }
        }
        throw PDFjetError(message: "no outline item " + title)
    }

    @Test func nestedBookmarksMakeATreeThatReadersNeedNotRepair() throws {
        let memory = MemoryPDF()
        let pdf = memory.pdf
        let font = TestSupport.helvetica(pdf)
        let page = Page(pdf, Letter.PORTRAIT)
        let root = Bookmark(pdf)
        _ = root.addBookmark(page, Title(font, "A", 10, 10))
        let b = root.addBookmark(page, Title(font, "B", 10, 30))
        _ = b.addBookmark(page, Title(font, "B1", 10, 50))
        let b2 = b.addBookmark(page, Title(font, "B2", 10, 70))
        _ = b2.addBookmark(page, Title(font, "B2a", 10, 90))
        _ = root.addBookmark(page, Title(font, "C", 10, 110))
        try pdf.complete()

        let objects = try TestSupport.read(memory.bytes)
        let outlines = try #require(objects.first { $0.getValue("/Type") == "/Outlines" })
        func number(_ title: String) throws -> String {
            return String(try item(objects, title).getNumber())
        }
        func value(_ title: String, _ key: String) throws -> String {
            return try item(objects, title).getValue(key)
        }
        let root0 = String(outlines.getNumber())
        // The outline dictionary has the items of the first level.
        #expect(try outlines.getValue("/First") == number("A"))
        #expect(try outlines.getValue("/Last") == number("C"))
        #expect(outlines.getValue("/Count") == "3")
        #expect(try value("A", "/Parent") == root0)
        #expect(try value("C", "/Parent") == root0)

        // A nested item has the item above it as its parent, and a closed item
        // counts the items that opening it shows.
        #expect(try value("B", "/Count") == "-2")
        #expect(try value("B", "/First") == number("B1"))
        #expect(try value("B", "/Last") == number("B2"))
        #expect(try value("B1", "/Parent") == number("B"))
        #expect(try value("B2", "/Parent") == number("B"))
        #expect(try value("B1", "/Next") == number("B2"))
        #expect(try value("B2", "/Prev") == number("B1"))
        #expect(try value("B2", "/Count") == "-1")
        #expect(try value("B2a", "/Parent") == number("B2"))
        #expect(try value("B2a", "/Count") == "")
    }

    private static let streamFont = "fonts/IBMPlexSans/IBMPlexSans-Regular.ttf"

    // A document with the headings, as text lines of their structure types,
    // H1 on the first page and the others on the second.
    private func headingsPDF(_ tagged: Bool, _ headings: [(String, String)]) throws -> MemoryPDF {
        let memory = tagged ? MemoryPDF(Compliance.PDF_UA_1) : MemoryPDF()
        if tagged {
            _ = memory.pdf.setTitle("Title")
        }
        let font = try Font(memory.pdf, TestSupport.open(BookmarkTests.streamFont))
        var page: Page?
        for (i, heading) in headings.enumerated() {
            if page == nil || heading.0 == "H1" && i > 0 {
                page = Page(memory.pdf, Letter.PORTRAIT)
            }
            TextLine(font, heading.1).setStructureType(StructElem(rawValue: heading.0)!)
                    .setLocation(70, Float(100 + 40 * i)).drawOn(page)
        }
        return memory
    }

    // A tagged document with headings and no bookmarks of its own has the
    // bookmarks of its headings, each under the heading of a higher level
    // before it, and each at the top of its heading on its page, as PAC asks.
    @Test(.enabled(if: TestSupport.exists(streamFont), "the fonts directory is not here"))
    func aTaggedDocumentHasTheBookmarksOfItsHeadings() throws {
        let memory = try headingsPDF(true, [("H1", "Intro"), ("H2", "What  it is"), ("H3", "In short"),
                                            ("H2", "Why"), ("H1", "Use")])
        try memory.pdf.complete()
        let objects = try TestSupport.read(memory.bytes)
        let outlines = try #require(objects.first { $0.getValue("/Type") == "/Outlines" })
        func number(_ title: String) throws -> String {
            return String(try item(objects, title).getNumber())
        }
        func value(_ title: String, _ key: String) throws -> String {
            return try item(objects, title).getValue(key)
        }
        #expect(try outlines.getValue("/First") == number("Intro"))
        #expect(try outlines.getValue("/Last") == number("Use"))
        // The whitespace of a title is collapsed, as addBookmark does
        #expect(try value("What it is", "/Parent") == number("Intro"))
        #expect(try value("In short", "/Parent") == number("What it is"))
        #expect(try value("Why", "/Parent") == number("Intro"))
        #expect(try value("What it is", "/Next") == number("Why"))
        // The destination: the page of the heading, and the top of its text
        let intro = try value("Intro", "/Dest")
        let use = try value("Use", "/Dest")
        #expect(intro.contains("/XYZ") && use.contains("/XYZ"))
        let introPage = intro.split(separator: " ")[1]
        let usePage = use.split(separator: " ")[1]
        #expect(introPage != usePage, "Intro and Use, on two pages, go to one: \(intro), \(use)")
        // 792 - (100 - 12): the top of a line of 12 points at 100
        #expect(intro.contains("/XYZ 0 704 0"), "Intro goes to \(intro)")
    }

    // A document that is not tagged has no headings, and one with bookmarks
    // of its own keeps them.
    @Test(.enabled(if: TestSupport.exists(streamFont), "the fonts directory is not here"))
    func ofHeadingsOnlyInATaggedDocumentWithoutBookmarks() throws {
        let untagged = try headingsPDF(false, [("H1", "Intro")])
        try untagged.pdf.complete()
        #expect(!TestSupport.latin1(untagged.bytes).contains("/Outlines"))

        let own = try headingsPDF(true, [("H1", "Intro")])
        let page = Page(own.pdf, Letter.PORTRAIT)
        let font = try Font(own.pdf, TestSupport.open(BookmarkTests.streamFont))
        _ = Bookmark(own.pdf).addBookmark(page, Title(font, "Mine", 10, 10))
        try own.pdf.complete()
        let objects = try TestSupport.read(own.bytes)
        _ = try item(objects, "Mine")
        #expect(throws: PDFjetError.self) { try item(objects, "Intro") }
    }

    @Test func aTitleIsATextStringThatEveryReaderDecodes() throws {
        let memory = MemoryPDF()
        let page = Page(memory.pdf, Letter.PORTRAIT)
        let root = Bookmark(memory.pdf)
        _ = root.addBookmark(page, Title(TestSupport.helvetica(memory.pdf), "\u{dc}bersicht \u{2013} r\u{e9}sum\u{e9}", 10, 10))
        try memory.pdf.complete()
        let obj = try item(try TestSupport.read(memory.bytes), "\u{dc}bersicht \u{2013} r\u{e9}sum\u{e9}")
        #expect(obj.getValue("/Title").lowercased().hasPrefix("<feff00dc"))
    }
}
