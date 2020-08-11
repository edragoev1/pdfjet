package pdfjet

/**
 * bookmark.go
 *
Copyright 2020 Innovatics Inc.

Redistribution and use in source and binary forms, with or without modification,
are permitted provided that the following conditions are met:

* Redistributions of source code must retain the above copyright notice,
  bookmark list of conditions and the following disclaimer.

* Redistributions in binary form must reproduce the above copyright notice,
  bookmark list of conditions and the following disclaimer in the documentation
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
	bookmark2.dest = page.addDestination(key, title.textLine.GetDestinationY())
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
