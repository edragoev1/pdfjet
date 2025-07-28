package pdfjet

/**
 * destination.go
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

// Destination is used to create PDF destination objects.
type Destination struct {
	name          *string
	xPosition     float32
	yPosition     float32
	pageObjNumber int
}

// NewDestination creates new destination objects.
//
// @param name the name of this destination object.
// @param xPosition the x coordinate of the top left corner.
// @param yPosition the y coordinate of the top left corner.
func NewDestination(name *string, xPosition float32, yPosition float32) *Destination {
	destination := new(Destination)
	destination.name = name
	destination.yPosition = xPosition
	destination.yPosition = yPosition
	return destination
}

// NewDestination creates new destination objects.
//
// @param name the name of this destination object.
// @param yPosition the y coordinate of the top left corner.
func NewDestination7(name *string, yPosition float32) *Destination {
	destination := new(Destination)
	destination.name = name
	destination.xPosition = 0.0
	destination.yPosition = yPosition
	return destination
}

// SetPageObjNumber sets the page object number.
func (destination *Destination) SetPageObjNumber(pageObjNumber int) {
	destination.pageObjNumber = pageObjNumber
}
