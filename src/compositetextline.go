// compositetextline.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"math"
	"strings"

	"github.com/edragoev1/pdfjet/v9/src/scriptposition"
)

// CompositeTextLine constructs composite text line objects.
// This class was designed and implemented by Jon T. Swanson, Ph.D.
// Refactored and integrated into the project by Eugene Dragoev - 1st of June 2020.
type CompositeTextLine struct {
	x         int
	y         int
	textLines []*TextLine
	// The font size each component had when it was added. It is the base of
	// the script size and offset of that component when this composite text
	// line has no font size of its own.
	fontSizes []float32
	position  [2]float32
	current   [2]float32
	// Subscript and Superscript size factors
	subscriptFactor   float32
	superscriptFactor float32
	// Subscript and Superscript positions in relation to the base font
	superscriptPosition float32
	subscriptPosition   float32
	fontSize            float32
}

// NewCompositeTextLine constructs new composite text line object.
func NewCompositeTextLine(x, y float32) *CompositeTextLine {
	compositeTextLine := new(CompositeTextLine)
	compositeTextLine.x = 0
	compositeTextLine.y = 1
	compositeTextLine.position[compositeTextLine.x] = x
	compositeTextLine.position[compositeTextLine.y] = y
	compositeTextLine.current[compositeTextLine.x] = x
	compositeTextLine.current[compositeTextLine.y] = y
	compositeTextLine.subscriptFactor = 0.583
	compositeTextLine.superscriptFactor = 0.583
	// Subscript and Superscript positions in relation to the base font
	compositeTextLine.superscriptPosition = 0.350
	compositeTextLine.subscriptPosition = 0.141
	return compositeTextLine
}

// SetFontSize sets the font size.
func (composite *CompositeTextLine) SetFontSize(fontSize float32) *CompositeTextLine {
	composite.fontSize = fontSize
	composite.layoutComponents()
	return composite
}

// GetFontSize gets the font size.
func (composite *CompositeTextLine) GetFontSize() float32 {
	return composite.fontSize
}

// SetSuperscriptFactor sets the superscript factor for this composite text line.
//   - superscript: the superscript size factor.
func (composite *CompositeTextLine) SetSuperscriptFactor(superscript float32) *CompositeTextLine {
	composite.superscriptFactor = superscript
	composite.layoutComponents()
	return composite
}

// GetSuperscriptFactor gets the superscript factor for this text line.
// Returns superscript the superscript size factor.
func (composite *CompositeTextLine) GetSuperscriptFactor() float32 {
	return composite.superscriptFactor
}

/**
 *  Sets the subscript factor for this composite text line.
 *
 *   - subscript: the subscript size factor.
 */
func (composite *CompositeTextLine) SetSubscriptFactor(subscript float32) *CompositeTextLine {
	composite.subscriptFactor = subscript
	composite.layoutComponents()
	return composite
}

// GetSubscriptFactor gets the subscript factor for this text line.
// Returns subscript the subscript size factor.
func (composite *CompositeTextLine) GetSubscriptFactor() float32 {
	return composite.subscriptFactor
}

// SetSuperscriptPosition sets the superscript position for this composite text line.
//   - superscriptPosition: the superscript position.
func (composite *CompositeTextLine) SetSuperscriptPosition(superscriptPosition float32) *CompositeTextLine {
	composite.superscriptPosition = superscriptPosition
	composite.layoutComponents()
	return composite
}

// GetSuperscriptPosition gets the superscript position for this text line.
func (composite *CompositeTextLine) GetSuperscriptPosition() float32 {
	return composite.superscriptPosition
}

// SetSubscriptPosition sets the subscript position for this composite text line.
//   - subscriptPosition: the subscript position.
func (composite *CompositeTextLine) SetSubscriptPosition(subscriptPosition float32) *CompositeTextLine {
	composite.subscriptPosition = subscriptPosition
	composite.layoutComponents()
	return composite
}

// GetSubscriptPosition gets the subscript position for this text line.
// Returns subscriptPosition the subscript position.
func (composite *CompositeTextLine) GetSubscriptPosition() float32 {
	return composite.subscriptPosition
}

// AddComponent adds a new text line.
// Find the current font, current size and effects (normal, super or subscript)
// Set the position of the component to the starting stored as current position
// Set the size and offset based on effects
// Set the new current position
//   - component: the component.
func (composite *CompositeTextLine) AddComponent(textLine *TextLine) *CompositeTextLine {
	composite.textLines = append(composite.textLines, textLine)
	composite.fontSizes = append(composite.fontSizes, textLine.GetFontSize())
	composite.place(textLine, composite.baseFontSize(len(composite.textLines)-1))
	return composite
}

// AddFormula adds the components of a chemical formula, in the specified font.
//
// The digits that follow an element or a closing bracket are subscripts, as
// the 2 of H2O and the 6, 12 and 6 of C6H12O6; a run of digits and signs after
// a circumflex is a superscript, as the charge of Ca^2+ and SO4^2-; and
// everything else is drawn on the baseline, including a digit that begins the
// formula, as the 2 of 2H2O.
//   - font: the font of the formula.
//   - formula: the formula, for example "C6H12O6" or "SO4^2-".
func (composite *CompositeTextLine) AddFormula(font *Font, formula string) *CompositeTextLine {
	var buf strings.Builder
	runes := []rune(formula)
	i := 0
	for i < len(runes) {
		ch := runes[i]
		if ch == '^' {
			composite.addRun(font, &buf, scriptposition.Normal)
			i++
			for i < len(runes) && isDigitOrSign(runes[i]) {
				buf.WriteRune(runes[i])
				i++
			}
			composite.addRun(font, &buf, scriptposition.Superscript)
		} else if isDigit(ch) && followsAnElement(runes, i) {
			composite.addRun(font, &buf, scriptposition.Normal)
			for i < len(runes) && isDigit(runes[i]) {
				buf.WriteRune(runes[i])
				i++
			}
			composite.addRun(font, &buf, scriptposition.Subscript)
		} else {
			buf.WriteRune(ch)
			i++
		}
	}
	composite.addRun(font, &buf, scriptposition.Normal)
	return composite
}

// addRun adds what the buffer holds as one component, and empties the buffer.
func (composite *CompositeTextLine) addRun(
	font *Font, buf *strings.Builder, position scriptposition.ScriptPosition) {
	if buf.Len() == 0 {
		return
	}
	component := NewTextLine(font, buf.String())
	component.SetScriptPosition(position)
	buf.Reset()
	composite.AddComponent(component)
}

func isDigit(ch rune) bool {
	return ch >= '0' && ch <= '9'
}

func isDigitOrSign(ch rune) bool {
	return isDigit(ch) || ch == '+' || ch == '-'
}

// followsAnElement reports whether the digit counts the atoms of the element
// or the group before it, rather than beginning the formula.
func followsAnElement(runes []rune, index int) bool {
	if index == 0 {
		return false
	}
	ch := runes[index-1]
	return (ch >= 'A' && ch <= 'Z') || (ch >= 'a' && ch <= 'z') || ch == ')' || ch == ']'
}

// baseFontSize returns the base of the script size and offset of the
// component: the font size of this composite text line, or the one the
// component came with.
func (composite *CompositeTextLine) baseFontSize(index int) float32 {
	if composite.fontSize > 0.0 {
		return composite.fontSize
	}
	return composite.fontSizes[index]
}

// place puts the component at the current position, at the size its script
// position asks for, and moves the current position past it. The size goes on
// the TextLine: DrawOn uses the line's own font size, so resizing the shared
// Font here would have no effect.
func (composite *CompositeTextLine) place(textLine *TextLine, base float32) {
	if textLine.GetScriptPosition() == scriptposition.Superscript {
		textLine.SetFontSize(base * composite.superscriptFactor)
		textLine.SetLocation(
			composite.current[composite.x],
			composite.current[composite.y]-base*composite.superscriptPosition)
	} else if textLine.GetScriptPosition() == scriptposition.Subscript {
		textLine.SetFontSize(base * composite.subscriptFactor)
		textLine.SetLocation(
			composite.current[composite.x],
			composite.current[composite.y]+base*composite.subscriptPosition)
	} else {
		textLine.SetFontSize(base)
		textLine.SetLocation(composite.current[composite.x], composite.current[composite.y])
	}
	composite.current[composite.x] += textLine.GetWidth()
}

// layoutComponents places every component again, from the location of this
// composite text line, after the location, the font size or a script setting
// changed.
func (composite *CompositeTextLine) layoutComponents() {
	composite.current[composite.x] = composite.position[composite.x]
	composite.current[composite.y] = composite.position[composite.y]
	for i, textLine := range composite.textLines {
		composite.place(textLine, composite.baseFontSize(i))
	}
}

// SetLocation loops through all the text lines and reset their location based on
// the new location set here.
//   - x: the x coordinate.
//   - y: the y coordinate.
func (composite *CompositeTextLine) SetLocation(x, y float32) Drawable {
	composite.position[composite.x] = x
	composite.position[composite.y] = y
	composite.layoutComponents()
	return composite
}

// GetLocation return the position of this composite text line.
func (composite *CompositeTextLine) GetLocation() [2]float32 {
	return composite.position
}

// GetTextLine return the nth entry in the TextLine array.
//   - index: the index of the nth element.
func (composite *CompositeTextLine) GetTextLine(index int) *TextLine {
	if len(composite.textLines) == 0 {
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

// GetMinMaxY returns the vertical coordinates of the top left and bottom right corners
// of the bounding box of this composite text line.
// Returns the array containing the vertical coordinates.
func (composite *CompositeTextLine) GetMinMaxY() [2]float32 {
	minValue := composite.position[composite.y]
	maxValue := composite.position[composite.y]

	// Each component is measured where it is drawn, with the font size it is
	// drawn at, which a script position makes smaller than the base.
	for _, component := range composite.textLines {
		baseline := component.GetLocation()[1]
		top := baseline - component.font.GetAscent(component.GetFontSize())
		bottom := baseline + component.font.GetDescent(component.GetFontSize())
		if top < minValue {
			minValue = top
		}
		if bottom > maxValue {
			maxValue = bottom
		}
	}

	return [2]float32{minValue, maxValue}
}

// GetHeight returns the height of this CompositeTextLine.
func (composite *CompositeTextLine) GetHeight() float32 {
	yy := composite.GetMinMaxY()
	return yy[1] - yy[0]
}

// GetWidth returns the width of this CompositeTextLine.
func (composite *CompositeTextLine) GetWidth() float32 {
	var width float32
	for _, component := range composite.textLines {
		width += component.GetWidth()
	}
	return width
}

// DrawOn draws this line on the specified page.
//   - page: the page to draw this line on.
//
// Returns x and y coordinates of the bottom right corner of this component.
func (composite *CompositeTextLine) DrawOn(page *Page) [2]float32 {
	// A composite text line with no component reaches its own location.
	xMax := float64(composite.position[composite.x])
	yMax := float64(composite.position[composite.y])
	// Loop through all the text lines and draw them on the page
	for _, textLine := range composite.textLines {
		xy := textLine.DrawOn(page)
		xMax = math.Max(xMax, float64(xy[0]))
		yMax = math.Max(yMax, float64(xy[1]))
	}
	return [2]float32{float32(xMax), float32(yMax)}
}
