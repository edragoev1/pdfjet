/*
 * Bookmark.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import java.util.*;

/**
 * Please see Example_51 and Example_52
 */
public class Bookmark {
    /** The page this bookmark points to. */
    protected Page page = null;
    /** The y coordinate of the bookmark destination on the page. */
    protected float y = 0f;
    /** The object number of this bookmark. */
    protected int objNumber = 0;
    /** The number prefix added to the title by autoNumber. */
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
     * Add bookmark with the specified title to the page.
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

    /**
     * Returns this bookmark and all of its descendants in breadth-first order.
     *
     * @return the list of bookmarks.
     */
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

    /**
     * Returns the child bookmarks.
     *
     * @return the children, or null if there are none.
     */
    protected List<Bookmark> getChildren() {
        return this.children;
    }

    /**
     * Returns the previous sibling bookmark.
     *
     * @return the previous bookmark.
     */
    protected Bookmark getPrevBookmark() {
        return this.prev;
    }

    /**
     * Returns the next sibling bookmark.
     *
     * @return the next bookmark.
     */
    protected Bookmark getNextBookmark() {
        return this.next;
    }

    /**
     * Returns the first child bookmark.
     *
     * @return the first child.
     */
    protected Bookmark getFirstChild() {
        return this.children.get(0);
    }

    /**
     * Returns the last child bookmark.
     *
     * @return the last child.
     */
    protected Bookmark getLastChild() {
        return children.get(children.size() - 1);
    }

    /**
     * Returns the destination of this bookmark.
     *
     * @return the destination.
     */
    protected Destination getDestination() {
        return this.dest;
    }

    private String next() {
        destNumber++;
        return "dest#" + destNumber;
    }
}   // End of Bookmark.java
