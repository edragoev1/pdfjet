/**
 *  Bookmark.cs
 *
Copyright 2020 Innovatics Inc.

Redistribution and use in source and binary forms, with or without modification,
are permitted provided that the following conditions are met:

    * Redistributions of source code must retain the above copyright notice,
      this list of conditions and the following disclaimer.

    * Redistributions in binary form must reproduce the above copyright notice,
      this list of conditions and the following disclaimer in the documentation
      and / or other materials provided with the distribution.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
"AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL,
EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR
PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
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
