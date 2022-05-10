package pdfjet

/**
 * bookmark.go
 *
Copyright 2022 Innovatics Inc.

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

import (
	"log"
	"regexp"
	"strconv"
	"strings"
)

// Bookmark please see Example_51 and Example_52
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
	prefix     *string
}

// NewBookmark creates new bookmark.
func NewBookmark(pdf *PDF) *Bookmark {
	bookmark := new(Bookmark)
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
	key := bm.getNext()
	whitespace := regexp.MustCompile(`\s+`)
	bookmark2 := NewBookmarkAt(
		page,
		title.textLine.GetDestinationY(),
		key,
		whitespace.ReplaceAllString(title.textLine.text, " "))
	bookmark2.parent = bookmark
	bookmark2.dest = page.AddDestination(&key, title.textLine.GetDestinationY())
	if bookmark.children == nil {
		bookmark.children = make([]*Bookmark, 0)
	} else {
		bookmark2.prev = bookmark.children[len(bookmark.children)-1]
		bookmark.children[len(bookmark.children)-1].next = bookmark2
	}
	bookmark.children = append(bookmark.children, bookmark2)
	return bookmark2
}

// GetDestKey returns the destination key.
func (bookmark *Bookmark) GetDestKey() string {
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
		if bm.prefix == nil {
			value := "1"
			bookmark.prefix = &value
		} else {
			value := *bm.prefix + ".1"
			bookmark.prefix = &value
		}
	} else {
		if bm.prefix == nil {
			if bm.GetParent().prefix == nil {
				value := "1"
				bookmark.prefix = &value
			} else {
				value := *bm.GetParent().prefix + ".1"
				bookmark.prefix = &value
			}
		} else {
			index := strings.LastIndex(*bm.prefix, ".")
			if index == -1 {
				temp, err := strconv.Atoi(*bm.prefix)
				if err != nil {
					log.Fatal(err)
				}
				value := strconv.Itoa(temp + 1)
				bookmark.prefix = &value
			} else {
				value := (*bm.prefix)[:index] + "."
				temp, err := strconv.Atoi((*bm.prefix)[index+1:])
				if err != nil {
					log.Fatal(err)
				}
				value += strconv.Itoa(temp + 1)
				bookmark.prefix = &value
			}
		}
	}
	textLine.SetText(*bookmark.prefix)
	bookmark.title = *bookmark.prefix + " " + bookmark.title
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

func (bookmark *Bookmark) getNext() string {
	bookmark.destNumber++
	return "dest#" + strconv.Itoa(bookmark.destNumber)
}
