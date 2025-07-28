package pdfjet

/**
 * dimension.go
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

// Dimension encapsulates the width and height of a component.
type Dimension struct {
	w float32
	h float32
}

// NewDimension Constructor for creating dimension objects.
//
// @param width the width.
// @param height the height.
func NewDimension(width, height float32) *Dimension {
	dimension := new(Dimension)
	dimension.w = width
	dimension.h = height
	return dimension
}

// GetWidth gets the width of the component.
func (dimension *Dimension) GetWidth() float32 {
	return dimension.w
}

// GetHeight gets the height of the component.
func (dimension *Dimension) GetHeight() float32 {
	return dimension.h
}
