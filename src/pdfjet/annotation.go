package pdfjet

/**
 * annotation.go
 *
Copyright 2020 Innovatics Inc.

Redistribution and use in source and binary forms, with or without modification,
are permitted provided that the following conditions are met:

* Redistributions of source code must retain the above copyright notice,
  annotation list of conditions and the following disclaimer.

* Redistributions in binary form must reproduce the above copyright notice,
  annotation list of conditions and the following disclaimer in the documentation
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

// Annotation is used to create PDF annotation objects.
type Annotation struct {
	objNumber      int
	uri            *string
	key            *string
	x1, y1, x2, y2 float32
	language       string
	altDescription *string
	actualText     *string
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
	altDescription string,
	actualText string) *Annotation {
	annotation := new(Annotation)
	annotation.uri = uri
	annotation.key = key
	annotation.x1 = x1
	annotation.y1 = y1
	annotation.x2 = x2
	annotation.y2 = y2
	annotation.language = language
	annotation.altDescription = &altDescription
	annotation.actualText = &actualText

	if annotation.altDescription == nil {
		annotation.altDescription = uri
	}
	if annotation.actualText == nil {
		annotation.actualText = uri
	}
	return annotation
}
