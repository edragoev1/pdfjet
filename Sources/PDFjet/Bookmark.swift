/**
 * Bookmark.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation

extension String {
    func indexOf(_ input: String) -> String.Index? {
        return self.range(of: input)?.lowerBound
    }
    func lastIndexOf(_ input: String) -> String.Index? {
        return self.range(of: input, options: .backwards)?.lowerBound
    }
}

///
/// Please see Example_48
///
public class Bookmark {
    private var destNumber = 0
    private var page: Page?
    private var y: Float = 0.0
    private var key: String?
    private var title: String?
    private var parent: Bookmark?
    private var prev: Bookmark?
    private var next: Bookmark?
    private var children: [Bookmark]?
    private var dest: Destination?
    var objNumber = 0
    var prefix: String?
    private weak var pdf: PDF?  // The document of the root bookmark

    /// Creates the root bookmark of the document outline.
    public init(_ pdf: PDF) {
        self.pdf = pdf
        pdf.toc = self
    }

    private init(
            _ page: Page,
            _ y: Float,
            _ key: String,
            _ title: String) {
        self.page = page
        self.y = y
        self.key = key
        self.title = title
    }

    // The bookmark of a heading, which goes to its destination, not to a
    // destination named on its page.
    private init(
            _ page: Page,
            _ top: Float,
            _ title: String,
            _ dest: Destination) {
        self.page = page
        self.y = top
        self.title = title
        self.dest = dest
    }

    // Returns the bookmarks of the headings of the document, each under the
    // heading before it of a higher level: an H2 under the H1 before it, and
    // an H1 at the top. It is called once the pages are written, when the
    // number of the page of each heading is known.
    static func ofHeadings(_ pdf: PDF, _ headings: [Heading]) -> Bookmark {
        let root = Bookmark(pdf)
        var stack = [(level: Int, bookmark: Bookmark)]()
        for heading in headings {
            while let last = stack.last, last.level >= heading.level {
                stack.removeLast()
            }
            let parent = stack.last?.bookmark ?? root
            let dest = Destination("", 0.0, heading.page.height - heading.top)
            dest.pageObjNumber = heading.page.objNumber
            let bookmark = Bookmark(heading.page, heading.top, heading.title, dest)
            bookmark.parent = parent
            if parent.children == nil {
                parent.children = [Bookmark]()
            } else if let before = parent.children!.last {
                bookmark.prev = before
                before.next = bookmark
            }
            parent.children!.append(bookmark)
            stack.append((heading.level, bookmark))
        }
        return root
    }

    /// Adds a bookmark with the specified title that points to the page, and returns the new bookmark.
    @discardableResult
    public func addBookmark(
            _ page: Page,
            _ title: Title) -> Bookmark {
        var bm = self
        while bm.parent != nil {
            bm = bm.getParent()!
        }
        if let pdf = bm.pdf, page.pdf !== pdf {
            pdf.fail("The page belongs to another PDF.")
        }
        let key = bm.nextKey()

        let bookmark = Bookmark(page, title.textLine.destinationY(), key,
                title.textLine.text!.replacingOccurrences(
                        of: "[ \\t\\n\\x0B\\f\\r]+", with: " ", options: .regularExpression))
        bookmark.parent = self
        bookmark.dest = page.addDestination(key, title.textLine.destinationY())
        if children == nil {
            children = [Bookmark]()
        } else {
            bookmark.prev = children![children!.count - 1]
            children![children!.count - 1].next = bookmark
        }
        children!.append(bookmark)
        return bookmark
    }

    /// Returns the name of the destination of this bookmark, or nil for the root bookmark.
    public func getDestinationName() -> String? {
        return self.key
    }

    /// Returns the title of this bookmark, or nil for the root bookmark.
    public func getTitle() -> String? {
        return self.title
    }

    /// Returns the parent bookmark.
    public func getParent() -> Bookmark? {
        return self.parent
    }

    /// Numbers this bookmark by its position, for example 1.2, and adds the number to the title.
    @discardableResult
    public func autoNumber(_ text: TextLine) -> Bookmark {
        var bm = getPrevBookmark()
        if bm == nil {
            bm = getParent()
            if bm!.prefix == nil {
                prefix = "1"
            } else {
                prefix = bm!.prefix! + ".1"
            }
        } else {
            if bm!.prefix == nil {
                if bm!.getParent()!.prefix == nil {
                    prefix = "1"
                } else {
                    prefix = bm!.getParent()!.prefix! + ".1"
                }
            } else {
                if let index = bm!.prefix!.lastIndexOf(".") {
                    // The same as the Java code: keep the prefix up to the
                    // last dot and increment the number after it.
                    let index2 = bm!.prefix!.index(after: index)
                    prefix = String(bm!.prefix![...index]) +
                            String(Int(bm!.prefix![index2...])! + 1)
                } else {
                    prefix = String(Int(bm!.prefix!)! + 1)
                }
            }
        }
        text.setText(prefix!)
        title = prefix! + " " + title!
        return self
    }

    func toArrayList() -> [Bookmark] {
        var objNumber = 0
        var list = [Bookmark]()
        var queue = [Bookmark]()
        queue.append(self)
        while !queue.isEmpty {
            let bookmark = queue.remove(at: 0)
            bookmark.objNumber = objNumber
            objNumber += 1
            list.append(bookmark)
            if bookmark.getChildren() != nil {
                queue.append(contentsOf: bookmark.getChildren()!)
            }
        }
        return list
    }

    func getChildren() -> [Bookmark]? {
        return self.children
    }

    func getPrevBookmark() -> Bookmark? {
        return self.prev
    }

    func getNextBookmark() -> Bookmark? {
        return self.next
    }

    func getFirstChild() -> Bookmark? {
        return self.children![0]
    }

    func getLastChild() -> Bookmark? {
        return children![children!.count - 1]
    }

    func getDestination() -> Destination? {
        return self.dest
    }

    private func nextKey() -> String {
        destNumber += 1
        return "dest#" + String(destNumber)
    }
}   // End of Bookmark.swift

// A heading of a tagged document, H1 to H6, as it was drawn.
struct Heading {
    let level: Int
    let title: String
    let page: Page
    let top: Float  // The top of its text on the page
}

// Returns the level of a heading, 1 for H1 to 6 for H6, or 0 for a structure
// type that is not a heading.
func headingLevel(_ structure: StructElem) -> Int {
    switch structure {
    case .H1: return 1
    case .H2: return 2
    case .H3: return 3
    case .H4: return 4
    case .H5: return 5
    case .H6: return 6
    default: return 0
    }
}
