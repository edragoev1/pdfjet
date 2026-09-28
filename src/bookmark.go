// bookmark.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"regexp"
	"strconv"
	"strings"

	"github.com/edragoev1/pdfjet/v9/src/structelem"
)

// Bookmark please see Example_48
type Bookmark struct {
	destNumber int
	page       *Page
	y          float32
	key        string
	title      string
	parent     *Bookmark
	prev       *Bookmark
	next       *Bookmark
	children   []*Bookmark
	dest       *Destination
	objNumber  int
	prefix     string
	pdf        *PDF // The document of the root bookmark
}

// NewBookmark creates new bookmark.
func NewBookmark(pdf *PDF) *Bookmark {
	bookmark := new(Bookmark)
	bookmark.pdf = pdf
	pdf.toc = bookmark
	return bookmark
}

// NewBookmarkAt creates new bookmark at the specified y coordinate.
func NewBookmarkAt(page *Page, y float32, key, title string) *Bookmark {
	bookmark := new(Bookmark)
	bookmark.page = page
	bookmark.y = y
	bookmark.key = key
	bookmark.title = title
	return bookmark
}

// AddBookmark adds bookmark to the page.
func (bookmark *Bookmark) AddBookmark(page *Page, title *Title) *Bookmark {
	bm := bookmark
	for bm.parent != nil {
		bm = bm.GetParent()
	}
	if bm.pdf != nil && page.pdf != bm.pdf {
		bm.pdf.fail("The page belongs to another PDF.")
	}
	key := bm.nextKey()
	whitespace := regexp.MustCompile(`\s+`)
	bookmark2 := NewBookmarkAt(
		page,
		title.textLine.destinationY(),
		key,
		whitespace.ReplaceAllString(title.textLine.text, " "))
	bookmark2.parent = bookmark
	bookmark2.dest = page.AddDestination(key, title.textLine.destinationY())
	if bookmark.children == nil {
		bookmark.children = make([]*Bookmark, 0)
	} else {
		bookmark2.prev = bookmark.children[len(bookmark.children)-1]
		bookmark.children[len(bookmark.children)-1].next = bookmark2
	}
	bookmark.children = append(bookmark.children, bookmark2)
	return bookmark2
}

// GetDestinationName returns the name of the destination of this bookmark.
func (bookmark *Bookmark) GetDestinationName() string {
	return bookmark.key
}

// GetTitle returns the title of the bookmark.
func (bookmark *Bookmark) GetTitle() string {
	return bookmark.title
}

// GetParent returns the parent bookmark.
func (bookmark *Bookmark) GetParent() *Bookmark {
	return bookmark.parent
}

// AutoNumber auto numbers the bookmark.
func (bookmark *Bookmark) AutoNumber(textLine *TextLine) *Bookmark {
	bm := bookmark.getPrevBookmark()
	if bm == nil {
		bm = bookmark.GetParent()
		if bm.prefix == "" {
			value := "1"
			bookmark.prefix = value
		} else {
			value := bm.prefix + ".1"
			bookmark.prefix = value
		}
	} else {
		if bm.prefix == "" {
			if bm.GetParent().prefix == "" {
				value := "1"
				bookmark.prefix = value
			} else {
				value := bm.GetParent().prefix + ".1"
				bookmark.prefix = value
			}
		} else {
			index := strings.LastIndex(bm.prefix, ".")
			if index == -1 {
				temp, err := strconv.Atoi(bm.prefix)
				if err != nil {
					panic(err)
				}
				value := strconv.Itoa(temp + 1)
				bookmark.prefix = value
			} else {
				value := (bm.prefix)[:index] + "."
				temp, err := strconv.Atoi((bm.prefix)[index+1:])
				if err != nil {
					panic(err)
				}
				value += strconv.Itoa(temp + 1)
				bookmark.prefix = value
			}
		}
	}
	textLine.SetText(bookmark.prefix)
	bookmark.title = bookmark.prefix + " " + bookmark.title
	return bookmark
}

func (bookmark *Bookmark) toArrayList() []*Bookmark {
	list := make([]*Bookmark, 0)
	queue := make([]*Bookmark, 0)
	objNumber := 0
	queue = append(queue, bookmark)
	for len(queue) != 0 {
		bookmark := queue[0] // Get the first element.
		queue = queue[1:]    // Remove the first element.
		bookmark.objNumber = objNumber
		objNumber++
		list = append(list, bookmark)
		if bookmark.getChildren() != nil {
			queue = append(queue, bookmark.getChildren()...)
		}
	}
	return list
}

func (bookmark *Bookmark) getChildren() []*Bookmark {
	return bookmark.children
}

func (bookmark *Bookmark) getPrevBookmark() *Bookmark {
	return bookmark.prev
}

func (bookmark *Bookmark) getNextBookmark() *Bookmark {
	return bookmark.next
}

func (bookmark *Bookmark) getFirstChild() *Bookmark {
	return bookmark.children[0]
}

func (bookmark *Bookmark) getLastChild() *Bookmark {
	return bookmark.children[len(bookmark.children)-1]
}

func (bookmark *Bookmark) getDestination() *Destination {
	return bookmark.dest
}

func (bookmark *Bookmark) nextKey() string {
	bookmark.destNumber++
	return "dest#" + strconv.Itoa(bookmark.destNumber)
}

// heading is a heading of a tagged document, H1 to H6, as it was drawn.
type heading struct {
	level int
	title string
	page  *Page
	top   float32 // The top of its text on the page
}

// headingLevel returns the level of a heading, 1 for H1 to 6 for H6, or 0 for
// a structure type that is not a heading.
func headingLevel(structure structelem.StructElem) int {
	switch structure {
	case structelem.H1:
		return 1
	case structelem.H2:
		return 2
	case structelem.H3:
		return 3
	case structelem.H4:
		return 4
	case structelem.H5:
		return 5
	case structelem.H6:
		return 6
	}
	return 0
}

// bookmarksOfHeadings returns the bookmarks of the headings of the document,
// each under the heading before it of a higher level: an H2 under the H1
// before it, and an H1 at the top. It is called once the pages are written,
// when the number of the page of each heading is known.
func (pdf *PDF) bookmarksOfHeadings() *Bookmark {
	root := NewBookmark(pdf)
	type open struct {
		level    int
		bookmark *Bookmark
	}
	var stack []open
	for _, h := range pdf.headings {
		for len(stack) > 0 && stack[len(stack)-1].level >= h.level {
			stack = stack[:len(stack)-1]
		}
		parent := root
		if len(stack) > 0 {
			parent = stack[len(stack)-1].bookmark
		}
		dest := newDestination("", 0, h.page.height-h.top)
		dest.pageObjNumber = h.page.objNumber
		bookmark := &Bookmark{page: h.page, y: h.top, title: h.title, parent: parent, dest: dest}
		if n := len(parent.children); n > 0 {
			bookmark.prev = parent.children[n-1]
			parent.children[n-1].next = bookmark
		}
		parent.children = append(parent.children, bookmark)
		stack = append(stack, open{h.level, bookmark})
	}
	return root
}
