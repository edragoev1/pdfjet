package pdfjet

/**
 * compositetextline.go
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

import (
	"effect"
	"math"
)

// CompositeTextLine constructs composite text line objects.
// This class was designed and implemented by Jon T. Swanson, Ph.D.
// Refactored and integrated into the project by Eugene Dragoev - 1st of June 2020.
type CompositeTextLine struct {
	X         int
	Y         int
	textLines []*TextLine
	position  [2]float32
	current   [2]float32
	// Subscript and Superscript size factors
	subscriptSizeFactor   float32
	superscriptSizeFactor float32
	// Subscript and Superscript positions in relation to the base font
	superscriptPosition float32
	subscriptPosition   float32
	fontSize            float32
}

// NewCompositeTextLine constructs new composite text line object.
func NewCompositeTextLine(x, y float32) *CompositeTextLine {
	compositeTextLine := new(CompositeTextLine)
	compositeTextLine.X = 0
	compositeTextLine.Y = 1
	compositeTextLine.position[compositeTextLine.X] = x
	compositeTextLine.position[compositeTextLine.Y] = y
	compositeTextLine.current[compositeTextLine.X] = x
	compositeTextLine.current[compositeTextLine.Y] = y
	compositeTextLine.subscriptSizeFactor = 0.583
	compositeTextLine.superscriptSizeFactor = 0.583
	// Subscript and Superscript positions in relation to the base font
	compositeTextLine.superscriptPosition = 0.350
	compositeTextLine.subscriptPosition = 0.141
	return compositeTextLine
}

// SetFontSize sets the font size.
func (composite *CompositeTextLine) SetFontSize(fontSize float32) {
	composite.fontSize = fontSize
}

// GetFontSize gets the font size.
func (composite *CompositeTextLine) GetFontSize() float32 {
	return composite.fontSize
}

// SetSuperscriptFactor sets the superscript factor for this composite text line.
// @param superscript the superscript size factor.
func (composite *CompositeTextLine) SetSuperscriptFactor(superscript float32) {
	composite.superscriptSizeFactor = superscript
}

// GetSuperscriptFactor gets the superscript factor for this text line.
// @return superscript the superscript size factor.
func (composite *CompositeTextLine) GetSuperscriptFactor() float32 {
	return composite.superscriptSizeFactor
}

/**
 *  Sets the subscript factor for this composite text line.
 *
 *  @param subscript the subscript size factor.
 */
func (composite *CompositeTextLine) setSubscriptFactor(subscript float32) {
	composite.subscriptSizeFactor = subscript
}

// GetSubscriptFactor gets the subscript factor for this text line.
// @return subscript the subscript size factor.
func (composite *CompositeTextLine) GetSubscriptFactor() float32 {
	return composite.subscriptSizeFactor
}

// SetSuperscriptPosition sets the superscript position for this composite text line.
// @param superscriptPosition the superscript position.
func (composite *CompositeTextLine) SetSuperscriptPosition(superscriptPosition float32) {
	composite.superscriptPosition = superscriptPosition
}

// GetSuperscriptPosition gets the superscript position for this text line.
func (composite *CompositeTextLine) GetSuperscriptPosition() float32 {
	return composite.superscriptPosition
}

// SetSubscriptPosition sets the subscript position for this composite text line.
// @param subscriptPosition the subscript position.
func (composite *CompositeTextLine) SetSubscriptPosition(subscriptPosition float32) {
	composite.subscriptPosition = subscriptPosition
}

// GetSubscriptPosition gets the subscript position for this text line.
// @return subscriptPosition the subscript position.
func (composite *CompositeTextLine) GetSubscriptPosition() float32 {
	return composite.subscriptPosition
}

// AddComponent adds a new text line.
// Find the current font, current size and effects (normal, super or subscript)
// Set the position of the component to the starting stored as current position
// Set the size and offset based on effects
// Set the new current position
// @param component the component.
func (composite *CompositeTextLine) AddComponent(textLine *TextLine) {
	if textLine.GetTextEffect() == effect.Superscript {
		if composite.fontSize > 0.0 {
			textLine.GetFont().SetSize(composite.fontSize * composite.superscriptSizeFactor)
		}
		textLine.SetLocation(
			composite.current[composite.X],
			composite.current[composite.Y]-composite.fontSize*composite.superscriptPosition)
	} else if textLine.GetTextEffect() == effect.Subscript {
		if composite.fontSize > 0.0 {
			textLine.GetFont().SetSize(composite.fontSize * composite.subscriptSizeFactor)
		}
		textLine.SetLocation(
			composite.current[composite.X],
			composite.current[composite.Y]+composite.fontSize*composite.subscriptPosition)
	} else {
		if composite.fontSize > 0.0 {
			textLine.GetFont().SetSize(composite.fontSize)
		}
		textLine.SetLocation(composite.current[composite.X], composite.current[composite.Y])
	}
	composite.current[composite.X] += textLine.GetWidth()
	composite.textLines = append(composite.textLines, textLine)
}

// SetLocation loops through all the text lines and reset their location based on
// the new location set here.
// @param x the x coordinate.
// @param y the y coordinate.
func (composite *CompositeTextLine) SetLocation(x, y float32) {
	composite.position[composite.X] = x
	composite.position[composite.Y] = y
	composite.current[composite.X] = x
	composite.current[composite.Y] = y

	if composite.textLines == nil || len(composite.textLines) == 0 {
		return
	}

	for _, textLine := range composite.textLines {
		if textLine.GetTextEffect() == effect.Superscript {
			textLine.SetLocation(
				composite.current[composite.X],
				composite.current[composite.Y]-composite.fontSize*composite.superscriptPosition)
		} else if textLine.GetTextEffect() == effect.Subscript {
			textLine.SetLocation(
				composite.current[composite.X],
				composite.current[composite.Y]+composite.fontSize*composite.subscriptPosition)
		} else {
			textLine.SetLocation(composite.current[composite.X], composite.current[composite.Y])
		}
		composite.current[composite.X] += textLine.GetWidth()
	}
}

// GetPosition return the position of this composite text line.
func (composite *CompositeTextLine) GetPosition() [2]float32 {
	return composite.position
}

// GetTextLine return the nth entry in the TextLine array.
// @param index the index of the nth element.
func (composite *CompositeTextLine) GetTextLine(index int) *TextLine {
	if composite.textLines == nil || len(composite.textLines) == 0 {
		return nil
	}
	if index < 0 || index > len(composite.textLines)-1 {
		return nil
	}
	return composite.textLines[index]
}

// GetNumberOfTextLines returns the number of text lines.
func (composite *CompositeTextLine) GetNumberOfTextLines() int {
	return len(composite.textLines)
}

// GetMinMax returns the vertical coordinates of the top left and bottom right corners
// of the bounding box of this composite text line.
// @return the an array containing the vertical coordinates.
func (composite *CompositeTextLine) GetMinMax() []float32 {
	min := composite.position[composite.Y]
	max := composite.position[composite.Y]
	var cur float32

	for _, component := range composite.textLines {
		if component.GetTextEffect() == effect.Superscript {
			cur = (composite.position[composite.Y] - component.font.ascent) - composite.fontSize*composite.superscriptPosition
			if cur < min {
				min = cur
			}
		} else if component.GetTextEffect() == effect.Subscript {
			cur = (composite.position[composite.Y] + component.font.descent) + composite.fontSize*composite.subscriptPosition
			if cur > max {
				max = cur
			}
		} else {
			cur = composite.position[composite.Y] - component.font.ascent
			if cur < min {
				min = cur
			}
			cur = composite.position[composite.Y] + component.font.descent
			if cur > max {
				max = cur
			}
		}
	}

	return []float32{min, max}
}

// GetHeight returns the height of this CompositeTextLine.
func (composite *CompositeTextLine) GetHeight() float32 {
	yy := composite.GetMinMax()
	return yy[1] - yy[0]
}

// GetWidth returns the width of this CompositeTextLine.
func (composite *CompositeTextLine) GetWidth() float32 {
	return (composite.current[composite.X] - composite.position[composite.X])
}

// DrawOn draws this line on the specified page.
// @param page the page to draw this line on.
// @return x and y coordinates of the bottom right corner of this component.
// @throws Exception
func (composite *CompositeTextLine) DrawOn(page *Page) []float32 {
	var xMax float64
	var yMax float64
	// Loop through all the text lines and draw them on the page
	for _, textLine := range composite.textLines {
		xy := textLine.DrawOn(page)
		xMax = math.Max(xMax, float64(xy[0]))
		yMax = math.Max(yMax, float64(xy[1]))
	}
	return []float32{float32(xMax), float32(yMax)}
}
