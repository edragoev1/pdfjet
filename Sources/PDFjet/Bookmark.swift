/**
 *  Bookmark.swift
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

extension String {
    func indexOf(_ input: String) -> String.Index? {
        return self.range(of: input)?.lowerBound
    }

    func lastIndexOf(_ input: String) -> String.Index? {
        return self.range(of: input, options: .backwards)?.lowerBound
    }
}

///
/// Please see Example_51 and Example_52
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


    public init(_ pdf: PDF) {
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


    @discardableResult
    public func addBookmark(
            _ page: Page,
            _ title: Title) -> Bookmark {
        var bm = self
        while bm.parent != nil {
            bm = bm.getParent()!
        }
        let key = bm.goToNext()

        let bookmark = Bookmark(page, title.textLine!.getDestinationY(), key,
                title.textLine!.text!.components(separatedBy: " ").filter{ !$0.isEmpty }.joined(separator: " "))
        bookmark.parent = self
        bookmark.dest = page.addDestination(key, title.textLine!.getDestinationY())
        if children == nil {
            children = [Bookmark]()
        } else {
            bookmark.prev = children![children!.count - 1]
            children![children!.count - 1].next = bookmark
        }
        children!.append(bookmark)
        return bookmark
    }


    public func getDestKey() -> String {
        return self.key!
    }


    public func getTitle() -> String {
        return self.title!
    }


    public func getParent() -> Bookmark? {
        return self.parent
    }


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
                    // TODO: Compare to the Java code!!!
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


    private func goToNext() -> String {
        destNumber += 1
        return "dest#" + String(destNumber)
    }

}   // End of Bookmark.swift
