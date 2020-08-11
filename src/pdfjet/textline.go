package pdfjet

/**
 * textline.go
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
	"color"
	"effect"
	"math"
)

// TextLine is used to create text line objects.
type TextLine struct {
	text               string
	x, y, xBox, yBox   float32
	font, fallbackFont *Font
	trailingSpace      bool
	uri, key           *string
	underline          bool
	strikeout          bool
	underlineTTS       string
	strikeoutTTS       string
	degrees            int
	color              uint32
	textEffect         int
	verticalOffset     float32
	language           string
	altDescription     string
	actualText         string
	uriLanguage        string
	uriAltDescription  string
	uriActualText      string
}

// NewTextLine is constructor for creating text line objects.
// @param font the font to use.
// @param text the text.
func NewTextLine(font *Font, text string) *TextLine {
	textLine := new(TextLine)
	textLine.font = font
	textLine.text = text
	textLine.trailingSpace = true
	textLine.underlineTTS = "underline"
	textLine.strikeoutTTS = "strikeout"
	textLine.color = color.Black
	textLine.textEffect = effect.Normal
	textLine.verticalOffset = 0.0
	textLine.altDescription = text
	textLine.actualText = text
	return textLine
}

// SetText sets the text.
// @param text the text.
// @return this TextLine.
func (textLine *TextLine) SetText(text string) *TextLine {
	textLine.text = text
	if textLine.altDescription == "" {
		textLine.altDescription = text
	}
	if textLine.actualText == "" {
		textLine.actualText = text
	}
	return textLine
}

// GetText returns the text.
func (textLine *TextLine) GetText() string {
	return textLine.text
}

// SetLocation sets the location where this text line will be drawn on the page.
// @param x the x coordinate of the text line.
// @param y the y coordinate of the text line.
// @return this TextLine.
func (textLine *TextLine) SetLocation(x, y float32) *TextLine {
	textLine.x = x
	textLine.y = y
	return textLine
}

// SetFont sets the font to use for this text line.
// @param font the font to use.
// @return this TextLine.
func (textLine *TextLine) SetFont(font *Font) *TextLine {
	textLine.font = font
	return textLine
}

// GetFont gets the font to use for this text line.
// @return font the font to use.
func (textLine *TextLine) GetFont() *Font {
	return textLine.font
}

// SetFontSize sets the font size to use for this text line.
// @param fontSize the fontSize to use.
// @return this TextLine.
func (textLine *TextLine) SetFontSize(fontSize float32) *TextLine {
	textLine.font.SetSize(fontSize)
	return textLine
}

// SetFallbackFont sets the fallback font.
// @param fallbackFont the fallback font.
// @return this TextLine.
func (textLine *TextLine) SetFallbackFont(fallbackFont *Font) *TextLine {
	textLine.fallbackFont = fallbackFont
	return textLine
}

// SetFallbackFontSize sets the fallback font size to use for this text line.
// @param fallbackFontSize the fallback font size.
// @return this TextLine.
func (textLine *TextLine) SetFallbackFontSize(fallbackFontSize float32) *TextLine {
	textLine.fallbackFont.SetSize(fallbackFontSize)
	return textLine
}

// GetFallbackFont returns the fallback font.
// @return the fallback font.
func (textLine *TextLine) GetFallbackFont() *Font {
	return textLine.fallbackFont
}

// SetColor sets the color for this text line.
// @param color the color is specified as an integer.
// @return this TextLine.
func (textLine *TextLine) SetColor(color uint32) *TextLine {
	textLine.color = color
	return textLine
}

// SetColorRGB sets the pen color.
// @param color the color. See the Color class for predefined values or define your own using 0x00RRGGBB packed integers.
// @return this TextLine.
func (textLine *TextLine) SetColorRGB(color []uint32) *TextLine {
	textLine.color = color[0]<<16 | color[1]<<8 | color[2]
	return textLine
}

// GetColor returns the text line color.
// @return the text line color.
func (textLine *TextLine) GetColor() uint32 {
	return textLine.color
}

// GetDestinationY returns the y coordinate of the destination.
// @return the y coordinate of the destination.
func (textLine *TextLine) GetDestinationY() float32 {
	return textLine.y - textLine.font.GetSize()
}

// GetWidth returns the width of this TextLine.
// @return the width.
func (textLine *TextLine) GetWidth() float32 {
	return textLine.font.StringWidth(textLine.fallbackFont, textLine.text)
}

// GetHeight returns the height of this TextLine.
// @return the height.
func (textLine *TextLine) GetHeight() float32 {
	return textLine.font.GetHeight()
}

// SetURIAction sets the URI for the "click text line" action.
// @param uri the URI
// @return this TextLine.
func (textLine *TextLine) SetURIAction(uri *string) *TextLine {
	textLine.uri = uri
	return textLine
}

// GetURIAction returns the action URI.
// @return the action URI.
func (textLine *TextLine) GetURIAction() *string {
	return textLine.uri
}

// SetGoToAction sets the destination key for the action.
// @param key the destination name.
// @return this TextLine.
func (textLine *TextLine) SetGoToAction(key *string) *TextLine {
	textLine.key = key
	return textLine
}

// GetGoToAction returns the GoTo action string.
// @return the GoTo action string.
func (textLine *TextLine) GetGoToAction() *string {
	return textLine.key
}

// SetUnderline sets the underline variable.
// If the value of the underline variable is 'true' - the text is underlined.
// @param underline the underline flag.
// @return this TextLine.
func (textLine *TextLine) SetUnderline(underline bool) *TextLine {
	textLine.underline = underline
	return textLine
}

// GetUnderline returns the underline flag.
// @return the underline flag.
func (textLine *TextLine) GetUnderline() bool {
	return textLine.underline
}

// SetStrikeout sets the strike variable.
// If the value of the strike variable is 'true' - a strike line is drawn through the text.
// @param strikeout the strikeout flag.
// @return this TextLine.
func (textLine *TextLine) SetStrikeout(strikeout bool) *TextLine {
	textLine.strikeout = strikeout
	return textLine
}

// GetStrikeout returns the strikeout flag.
// @return the strikeout flag.
func (textLine *TextLine) GetStrikeout() bool {
	return textLine.strikeout
}

// SetTextDirection sets the direction in which to draw the text.
// @param degrees the number of degrees.
// @return this TextLine.
func (textLine *TextLine) SetTextDirection(degrees int) *TextLine {
	textLine.degrees = degrees
	return textLine
}

// GetTextDirection returns the text direction.
// @return the text direction.
func (textLine *TextLine) GetTextDirection() int {
	return textLine.degrees
}

// SetTextEffect sets the text effect.
// @param textEffect Effect.NORMAL, Effect.SUBSCRIPT or Effect.SUPERSCRIPT.
// @return this TextLine.
func (textLine *TextLine) SetTextEffect(textEffect int) *TextLine {
	textLine.textEffect = textEffect
	return textLine
}

// GetTextEffect returns the text effect.
// @return the text effect.
func (textLine *TextLine) GetTextEffect() int {
	return textLine.textEffect
}

// SetVerticalOffset sets the vertical offset of the text.
// @param verticalOffset the vertical offset.
// @return this TextLine.
func (textLine *TextLine) SetVerticalOffset(verticalOffset float32) *TextLine {
	textLine.verticalOffset = verticalOffset
	return textLine
}

// GetVerticalOffset returns the vertical text offset.
// @return the vertical text offset.
func (textLine *TextLine) GetVerticalOffset() float32 {
	return textLine.verticalOffset
}

// SetTrailingSpace sets the trailing space after this text line when used in paragraph.
// @param trailingSpace the trailing space.
// @return this TextLine.
func (textLine *TextLine) SetTrailingSpace(trailingSpace bool) *TextLine {
	textLine.trailingSpace = trailingSpace
	return textLine
}

// GetTrailingSpace returns the trailing space.
// @return the trailing space.
func (textLine *TextLine) GetTrailingSpace() bool {
	return textLine.trailingSpace
}

// SetLanguage sets the language.
func (textLine *TextLine) SetLanguage(language string) *TextLine {
	textLine.language = language
	return textLine
}

// GetLanguage gets the language.
func (textLine *TextLine) GetLanguage() string {
	return textLine.language
}

// SetAltDescription sets the alternate description of this text line.
// @param altDescription the alternate description of the text line.
// @return this TextLine.
func (textLine *TextLine) SetAltDescription(altDescription string) *TextLine {
	textLine.altDescription = altDescription
	return textLine
}

// GetAltDescription gets the alternate description of this text line.
func (textLine *TextLine) GetAltDescription() string {
	return textLine.altDescription
}

// SetActualText sets the actual text for this text line.
// @param actualText the actual text for the text line.
// @return this TextLine.
func (textLine *TextLine) SetActualText(actualText string) *TextLine {
	textLine.actualText = actualText
	return textLine
}

// GetActualText gets the actual text.
func (textLine *TextLine) GetActualText() string {
	return textLine.actualText
}

// SetURILanguage sets the URI language.
func (textLine *TextLine) SetURILanguage(uriLanguage string) *TextLine {
	textLine.uriLanguage = uriLanguage
	return textLine
}

// SetURIAltDescription sets the URI alternative description.
func (textLine *TextLine) SetURIAltDescription(uriAltDescription string) *TextLine {
	textLine.uriAltDescription = uriAltDescription
	return textLine
}

// SetURIActualText sets the URI actual text.
func (textLine *TextLine) SetURIActualText(uriActualText string) *TextLine {
	textLine.uriActualText = uriActualText
	return textLine
}

// PlaceInAtZeroZero places this text line in the specified box at location (0.0, 0.0)
func (textLine *TextLine) PlaceInAtZeroZero(box *Box) *TextLine {
	textLine.PlaceIn(box, 0.0, 0.0)
	return textLine
}

// PlaceIn places this text line in the box at the specified offset.
// @param box the specified box.
// @param xOffset the x offset from the top left corner of the box.
// @param yOffset the y offset from the top left corner of the box.
// @return this TextLine.
func (textLine *TextLine) PlaceIn(box *Box, xOffset, yOffset float32) *TextLine {
	textLine.xBox = box.x + xOffset
	textLine.yBox = box.y + yOffset
	return textLine
}

// DrawOn draws this text line on the specified page if the draw parameter is true.
// @param page the page to draw this text line on.
// @param draw if draw is false - no action is performed.
func (textLine *TextLine) DrawOn(page *Page) []float32 {
	if page == nil || textLine.text == "" {
		return []float32{textLine.x, textLine.y}
	}

	page.SetTextDirection(textLine.degrees)

	textLine.x += textLine.xBox
	textLine.y += textLine.yBox

	page.SetBrushColor(textLine.color)
	page.AddBMC("Span", textLine.language, textLine.altDescription, textLine.actualText)
	page.DrawString(textLine.font, textLine.fallbackFont, textLine.text, textLine.x, textLine.y)
	page.AddEMC()

	radians := float64(math.Pi) * float64(textLine.degrees) / float64(180.0)
	if textLine.underline {
		page.SetPenWidth(textLine.font.underlineThickness)
		page.SetPenColor(textLine.color)
		lineLength := textLine.font.StringWidth(textLine.fallbackFont, textLine.text)
		xAdjust := textLine.font.underlinePosition*float32(math.Sin(radians)) + textLine.verticalOffset
		yAdjust := textLine.font.underlinePosition*float32(math.Cos(radians)) + textLine.verticalOffset
		x2 := textLine.x + lineLength*float32(math.Cos(radians))
		y2 := textLine.y - lineLength*float32(math.Sin(radians))
		page.AddBMC("Span", textLine.language, textLine.underlineTTS, textLine.underlineTTS)
		page.MoveTo(textLine.x+xAdjust, textLine.y+yAdjust)
		page.LineTo(x2+xAdjust, y2+yAdjust)
		page.StrokePath()
		page.AddEMC()
	}

	if textLine.strikeout {
		page.SetPenWidth(textLine.font.underlineThickness)
		page.SetPenColor(textLine.color)
		lineLength := textLine.font.StringWidth(textLine.fallbackFont, textLine.text)
		xAdjust := (textLine.font.bodyHeight / 4.0) * float32(math.Sin(radians))
		yAdjust := (textLine.font.bodyHeight / 4.0) * float32(math.Cos(radians))
		x2 := textLine.x + lineLength*float32(math.Cos(radians))
		y2 := textLine.y - lineLength*float32(math.Sin(radians))
		page.AddBMC("Span", textLine.language, textLine.strikeoutTTS, textLine.strikeoutTTS)
		page.MoveTo(textLine.x-xAdjust, textLine.y-yAdjust)
		page.LineTo(x2-xAdjust, y2-yAdjust)
		page.StrokePath()
		page.AddEMC()
	}

	if textLine.uri != nil || textLine.key != nil {
		page.AddAnnotation(NewAnnotation(
			textLine.uri,
			textLine.key, // The destination name
			textLine.x,
			textLine.y-textLine.font.ascent,
			textLine.x+textLine.font.StringWidth(textLine.fallbackFont, textLine.text),
			textLine.y+textLine.font.descent,
			textLine.uriLanguage,
			textLine.uriAltDescription,
			textLine.uriActualText))
	}

	page.SetTextDirection(0)

	length := textLine.font.StringWidth(textLine.fallbackFont, textLine.text)
	xMax := math.Max(float64(textLine.x), float64(textLine.x)+float64(length)*math.Cos(radians))
	yMax := math.Max(float64(textLine.y), float64(textLine.y)-float64(length)*math.Sin(radians))

	return []float32{float32(xMax), float32(yMax)}
}

func (textLine *TextLine) advance(leading float32) float32 {
	textLine.y += leading
	return textLine.y
}
