package pdfjet

/**
 * field.go
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

// Field describes field object that is used from the Form class.
// Please see Example_45
type Field struct {
	x              float32
	values         []string
	altDescription []string
	actualText     []string
	format         bool
}

// NewField constructs field object.
func NewField(x float32, values []string, format bool) *Field {
	field := new(Field)
	field.x = x
	field.values = values
	field.format = format
	if values != nil {
		field.altDescription = values
		field.actualText = values
		for i := 0; i < len(values); i++ {
			field.altDescription[i] = values[i]
			field.actualText[i] = values[i]
		}
	}
	return field
}

// SetAltDescription sets the alt description.
func (field *Field) SetAltDescription(altDescription string) *Field {
	field.altDescription[0] = altDescription
	return field
}

// SetActualText sets the alt description.
func (field *Field) SetActualText(actualText string) *Field {
	field.actualText[0] = actualText
	return field
}
