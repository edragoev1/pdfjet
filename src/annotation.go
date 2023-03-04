package pdfjet

/**
 * annotation.go
 *
Copyright 2023 Innovatics Inc.

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

// Annotation is used to create PDF annotation objects.
type Annotation struct {
	objNumber      int
	uri            *string
	key            *string
	x1, y1, x2, y2 float32
	language       string
	actualText     *string
	altDescription *string
	fileAttachment *FileAttachment
}

// NewAnnotation is the constructor used to create annotation objects.
//
// @param uri the URI string.
// @param key the destination name.
// @param x1 the x coordinate of the top left corner.
// @param y1 the y coordinate of the top left corner.
// @param x2 the x coordinate of the bottom right corner.
// @param y2 the y coordinate of the bottom right corner.
func NewAnnotation(
	uri *string,
	key *string,
	x1, y1, x2, y2 float32,
	language string,
	actualText string,
	altDescription string) *Annotation {
	annotation := new(Annotation)
	annotation.uri = uri
	annotation.key = key
	annotation.x1 = x1
	annotation.y1 = y1
	annotation.x2 = x2
	annotation.y2 = y2
	annotation.language = language
	annotation.actualText = &actualText
	annotation.altDescription = &altDescription

	if annotation.actualText == nil {
		annotation.actualText = uri
	}
	if annotation.altDescription == nil {
		annotation.altDescription = uri
	}
	return annotation
}
