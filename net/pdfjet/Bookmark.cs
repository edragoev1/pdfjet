/**
 *  Bookmark.cs
 *
Copyright 2020 Innovatics Inc.

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

using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;


namespace PDFjet.NET {
/**
 * Please see Example_51 and Example_52
 */
public class Bookmark {

    private int destNumber = 0;
    private Page page = null;
    private float y = 0f;
    private String key = null;
    private String title = null;
    private Bookmark parent = null;
    private Bookmark prev = null;
    private Bookmark next = null;
    private List<Bookmark> children = null;
    private Destination dest = null;

    internal int objNumber = 0;
    internal String prefix = null;


    public Bookmark(PDF pdf) {
        pdf.toc = this;
    }


    private Bookmark(Page page, float y, String key, String title) {
        this.page = page;
        this.y = y;
        this.key = key;
        this.title = title;
    }


    public Bookmark AddBookmark(Page page, Title title) {
        Bookmark bm = this;
        while (bm.parent != null) {
            bm = bm.GetParent();
        }
        String key = bm.Next();

        Bookmark bookmark = new Bookmark(
                page, title.textLine.GetDestinationY(), key, Regex.Replace(title.textLine.text, @"\s+"," "));
        bookmark.parent = this;
        bookmark.dest = page.AddDestination(key, title.textLine.GetDestinationY());
        if (children == null) {
            children = new List<Bookmark>();
        }
        else {
            bookmark.prev = children[children.Count - 1];
            children[children.Count - 1].next = bookmark;
        }
        children.Add(bookmark);
        return bookmark;
    }


    public String GetDestKey() {
        return this.key;
    }


    public String GetTitle() {
        return this.title;
    }


    public Bookmark GetParent() {
        return this.parent;
    }


    public Bookmark AutoNumber(TextLine text) {
        Bookmark bm = GetPrevBookmark();
        if (bm == null) {
            bm = GetParent();
            if (bm.prefix == null) {
                prefix = "1";
            }
            else {
                prefix = bm.prefix + ".1";
            }
        }
        else {
            if (bm.prefix == null) {
                if (bm.GetParent().prefix == null) {
                    prefix = "1";
                }
                else {
                    prefix = bm.GetParent().prefix + ".1";
                }
            }
            else {
                int index = bm.prefix.LastIndexOf('.');
                if (index == -1) {
                    prefix = (Int32.Parse(bm.prefix) + 1).ToString();
                }
                else {
                    prefix = bm.prefix.Substring(0, index) + ".";
                    prefix += (Int32.Parse(bm.prefix.Substring(index + 1)) + 1).ToString();
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


    private String Next() {
        ++destNumber;
        return "dest#" + destNumber.ToString();
    }

}   // End of Bookmark.cs
}   // End of namespace PDFjet.NET
