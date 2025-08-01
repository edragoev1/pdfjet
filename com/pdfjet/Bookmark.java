/**
 *  Bookmark.java
 *
©2025 PDFjet Software

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
package com.pdfjet;

import java.util.*;

/**
 * Please see Example_51 and Example_52
 *
 */
public class Bookmark {
    protected Page page = null;
    protected float y = 0f;
    protected int objNumber = 0;
    protected String prefix = null;

    private int destNumber = 0;
    private String key = null;
    private String title = null;
    private Bookmark parent = null;
    private Bookmark prev = null;
    private Bookmark next = null;
    private List<Bookmark> children = null;
    private Destination dest = null;

    /**
     * Creates a bookmark.
     *
     * @param pdf the PDF.
     */
    public Bookmark(PDF pdf) {
        pdf.toc = this;
    }

    /**
     * Creates a bookmark and sets the location, key and title.
     *
     * @param page the page.
     * @param y the vertical location on the page.
     * @param key the key.
     * @param title the title.
     */
    private Bookmark(Page page, float y, String key, String title) {
        this.page = page;
        this.y = y;
        this.key = key;
        this.title = title;
    }

    /**
     * Add bookmark with the spacified title to the page.
     *
     * @param page the page.
     * @param title the title.
     * @return the bookmark.
     */
    public Bookmark addBookmark(Page page, Title title) {
        Bookmark bm = this;
        while (bm.parent != null) {
            bm = bm.getParent();
        }
        String key = bm.next();
        Bookmark bookmark2 = new Bookmark(
                page,
                title.textLine.getDestinationY(),
                key,
                title.textLine.text.replaceAll("\\s+", " "));
        bookmark2.parent = this;
        bookmark2.dest = page.addDestination(key, title.textLine.getDestinationY());
        if (children == null) {
            children = new ArrayList<Bookmark>();
        } else {
            bookmark2.prev = children.get(children.size() - 1);
            children.get(children.size() - 1).next = bookmark2;
        }
        children.add(bookmark2);
        return bookmark2;
    }

    /**
     * Returns the destination key.
     *
     * @return the destination key.
     */
    public String getDestKey() {
        return this.key;
    }

    /**
     * Returns the bookmark title.
     *
     * @return the bookmark title.
     */
    public String getTitle() {
        return this.title;
    }

    /**
     * Returns the bookmark parent.
     *
     * @return the bookmark parent.
     */
    public Bookmark getParent() {
        return this.parent;
    }

    /**
     * Auto number the bookmark.
     *
     * @param textLine the text line.
     * @return the bookmark.
     */
    public Bookmark autoNumber(TextLine textLine) {
        Bookmark bm = getPrevBookmark();
        if (bm == null) {
            bm = getParent();
            if (bm.prefix == null) {
                prefix = "1";
            } else {
                prefix = bm.prefix + ".1";
            }
        } else {
            if (bm.prefix == null) {
                if (bm.getParent().prefix == null) {
                    prefix = "1";
                } else {
                    prefix = bm.getParent().prefix + ".1";
                }
            } else {
                int index = bm.prefix.lastIndexOf('.');
                if (index == -1) {
                    prefix = String.valueOf(Integer.parseInt(bm.prefix) + 1);
                } else {
                    prefix = bm.prefix.substring(0, index) + ".";
                    prefix += String.valueOf(Integer.parseInt(bm.prefix.substring(index + 1)) + 1);
                }
            }
        }
        textLine.setText(prefix);
        title = prefix + " " + title;
        return this;
    }

    protected List<Bookmark> toArrayList() {
        List<Bookmark> list = new ArrayList<Bookmark>();
        Queue<Bookmark> queue = new java.util.LinkedList<Bookmark>();
        int objNumber = 0;
        queue.add(this);
        while (!queue.isEmpty()) {
            Bookmark bm = queue.poll();
            bm.objNumber = objNumber++;
            list.add(bm);
            if (bm.getChildren() != null) {
                queue.addAll(bm.getChildren());
            }
        }
        return list;
    }

    protected List<Bookmark> getChildren() {
        return this.children;
    }

    protected Bookmark getPrevBookmark() {
        return this.prev;
    }

    protected Bookmark getNextBookmark() {
        return this.next;
    }

    protected Bookmark getFirstChild() {
        return this.children.get(0);
    }

    protected Bookmark getLastChild() {
        return children.get(children.size() - 1);
    }

    protected Destination getDestination() {
        return this.dest;
    }

    private String next() {
        destNumber++;
        return "dest#" + destNumber;
    }
}   // End of Bookmark.java
