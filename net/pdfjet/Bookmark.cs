/*
 * Bookmark.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace PDFjet.NET {
/// <summary>
/// Please see Example_48
/// </summary>
public class Bookmark {
    private int destNumber = 0;
    private String key = null;
    private String title = null;
    private Bookmark parent = null;
    private Bookmark prev = null;
    private Bookmark next = null;
    private List<Bookmark> children = null;
    private Destination dest = null;
    private PDF pdf = null;     // The document of the root bookmark
    internal int objNumber = 0;
    internal String prefix = null;

    /// <summary>Creates the root bookmark of the document outline.</summary>
    public Bookmark(PDF pdf) {
        this.pdf = pdf;
        pdf.toc = this;
    }

    private Bookmark(String key, String title) {
        this.key = key;
        this.title = title;
    }

    /// <summary>
    /// Adds a bookmark with the specified title that points to the page.
    /// </summary>
    /// <param name="page">the page.</param>
    /// <param name="title">the title.</param>
    /// <returns>the new bookmark.</returns>
    public Bookmark AddBookmark(Page page, Title title) {
        Bookmark bm = this;
        while (bm.parent != null) {
            bm = bm.GetParent();
        }
        if (bm.pdf != null && page.pdf != bm.pdf) {
            bm.pdf.Fail(new ArgumentException("The page belongs to another PDF."));
        }
        String key = bm.NextKey();

        Bookmark bookmark = new Bookmark(
                key, Regex.Replace(title.textLine.text, @"\s+"," "));
        bookmark.parent = this;
        bookmark.dest = page.AddDestination(key, title.textLine.DestinationY());
        if (children == null) {
            children = new List<Bookmark>();
        } else {
            bookmark.prev = children[children.Count - 1];
            children[children.Count - 1].next = bookmark;
        }
        children.Add(bookmark);
        return bookmark;
    }

    // Returns the bookmarks of the headings of the document, each under the
    // heading before it of a higher level: an H2 under the H1 before it, and
    // an H1 at the top. It is called once the pages are written, when the
    // number of the page of each heading is known.
    internal static Bookmark OfHeadings(PDF pdf) {
        Bookmark root = new Bookmark(pdf);
        List<Bookmark> stack = new List<Bookmark>();
        List<int> levels = new List<int>();
        foreach (Heading h in pdf.headings) {
            while (levels.Count > 0 && levels[levels.Count - 1] >= h.level) {
                levels.RemoveAt(levels.Count - 1);
                stack.RemoveAt(stack.Count - 1);
            }
            Bookmark parent = (stack.Count > 0) ? stack[stack.Count - 1] : root;
            Destination dest = new Destination("", 0f, h.page.height - h.top);
            dest.pageObjNumber = h.page.objNumber;
            Bookmark bookmark = new Bookmark(null, h.title);
            bookmark.parent = parent;
            bookmark.dest = dest;
            if (parent.children == null) {
                parent.children = new List<Bookmark>();
            } else if (parent.children.Count > 0) {
                bookmark.prev = parent.children[parent.children.Count - 1];
                parent.children[parent.children.Count - 1].next = bookmark;
            }
            parent.children.Add(bookmark);
            stack.Add(bookmark);
            levels.Add(h.level);
        }
        return root;
    }

    /// <summary>Returns the name of the destination of this bookmark.</summary>
    public String GetDestinationName() {
        return this.key;
    }

    /// <summary>Returns the title of this bookmark.</summary>
    public String GetTitle() {
        return this.title;
    }

    /// <summary>Returns the parent bookmark.</summary>
    public Bookmark GetParent() {
        return this.parent;
    }

    /// <summary>Numbers this bookmark by its position, for example 1.2, and adds the number to the title.</summary>
    public Bookmark AutoNumber(TextLine text) {
        Bookmark bm = GetPrevBookmark();
        if (bm == null) {
            bm = GetParent();
            if (bm.prefix == null) {
                prefix = "1";
            } else {
                prefix = bm.prefix + ".1";
            }
        } else {
            if (bm.prefix == null) {
                if (bm.GetParent().prefix == null) {
                    prefix = "1";
                } else {
                    prefix = bm.GetParent().prefix + ".1";
                }
            } else {
                int index = bm.prefix.LastIndexOf('.');
                if (index == -1) {
                    prefix = (Int32.Parse(bm.prefix, CultureInfo.InvariantCulture) + 1).ToString(CultureInfo.InvariantCulture);
                } else {
                    prefix = bm.prefix.Substring(0, index) + ".";
                    prefix += (Int32.Parse(bm.prefix.Substring(index + 1), CultureInfo.InvariantCulture) + 1).ToString(CultureInfo.InvariantCulture);
                }
            }
        }
        text.SetText(prefix);
        title = prefix + " " + title;
        return this;
    }

    internal List<Bookmark> ToArrayList() {
        List<Bookmark> list = new List<Bookmark>();
        List<Bookmark> queue = new List<Bookmark>();
        queue.Add(this);
        int objNumber = 0;
        while (queue.Count > 0) {
            Bookmark bm = queue[0];
            queue.RemoveAt(0);
            bm.objNumber = objNumber++;
            list.Add(bm);
            List<Bookmark> children = bm.GetChildren();
            if (children != null) {
                foreach (Bookmark bm2 in children) {
                    queue.Add(bm2);
                }
            }
        }
        return list;
    }

    internal List<Bookmark> GetChildren() {
        return this.children;
    }

    internal Bookmark GetPrevBookmark() {
        return this.prev;
    }

    internal Bookmark GetNextBookmark() {
        return this.next;
    }

    internal Bookmark GetFirstChild() {
        return this.children[0];
    }

    internal Bookmark GetLastChild() {
        return children[children.Count - 1];
    }

    internal Destination GetDestination() {
        return this.dest;
    }

    private String NextKey() {
        ++destNumber;
        return "dest#" + destNumber.ToString(CultureInfo.InvariantCulture);
    }
}   // End of Bookmark.cs

// A heading of a tagged document, H1 to H6, as it was drawn.
internal class Heading {
    internal int level;
    internal String title;
    internal Page page;
    internal float top;     // The top of its text on the page

    internal Heading(int level, String title, Page page, float top) {
        this.level = level;
        this.title = title;
        this.page = page;
        this.top = top;
    }

    // Returns the level of a heading, 1 for H1 to 6 for H6, or 0 for a
    // structure type that is not a heading.
    internal static int Level(StructElem structure) {
        switch (structure) {
        case StructElem.H1: return 1;
        case StructElem.H2: return 2;
        case StructElem.H3: return 3;
        case StructElem.H4: return 4;
        case StructElem.H5: return 5;
        case StructElem.H6: return 6;
        }
        return 0;
    }
}
}   // End of namespace PDFjet.NET
