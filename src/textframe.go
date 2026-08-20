// textframe.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"fmt"
	"regexp"
	"strings"

	"github.com/edragoev1/pdfjet/src/color"
	"github.com/edragoev1/pdfjet/src/single"
)

// TextFrame Please see Example_47
type TextFrame struct {
	f1          *Font
	x           float32
	y           float32
	w           float32
	h           float32
	leading     float32
	border      bool
	borderColor int32
	paragraphs  [][]string
}

func NewTextFrame(f1 *Font, inputList []string) *TextFrame {
	// Clone the input list
	list := make([]string, len(inputList))
	copy(list, inputList)

	tf := &TextFrame{
		f1:          f1,
		leading:     f1.GetAscent(f1.GetSize()) + f1.GetDescent(f1.GetSize()),
		borderColor: color.Blue,
		paragraphs:  make([][]string, 0),
	}

	// Reverse the list (like Java's Collections.reverse)
	for i, j := 0, len(list)-1; i < j; i, j = i+1, j-1 {
		list[i], list[j] = list[j], list[i]
	}

	// Tokenize paragraphs
	re := regexp.MustCompile(`\s+`)
	for _, text := range list {
		split := re.Split(strings.TrimSpace(text), -1)
		tokens := make([]string, 0)
		for _, token := range split {
			if token != "" {
				tokens = append(tokens, token)
			}
		}
		// Reverse tokens (like Java's Collections.reverse)
		for i, j := 0, len(tokens)-1; i < j; i, j = i+1, j-1 {
			tokens[i], tokens[j] = tokens[j], tokens[i]
		}
		tf.paragraphs = append(tf.paragraphs, tokens)
	}

	return tf
}

func (tf *TextFrame) SetLocation(x, y float32) *TextFrame {
	tf.x = x
	tf.y = y
	return tf
}

func (tf *TextFrame) SetWidth(w float32) *TextFrame {
	tf.w = w
	return tf
}

func (tf *TextFrame) SetHeight(h float32) *TextFrame {
	tf.h = h
	return tf
}

func (tf *TextFrame) GetWidth() float32 {
	return tf.w
}

func (tf *TextFrame) GetHeight() float32 {
	return tf.h
}

func (tf *TextFrame) SetPosition(x, y float32) {
	tf.SetLocation(x, y)
}

func (tf *TextFrame) SetBorder(border bool) {
	tf.border = border
}

func (tf *TextFrame) SetBorderColor(borderColor int32) {
	tf.borderColor = borderColor
}

func (tf *TextFrame) HasMoreText() bool {
	return len(tf.paragraphs) > 0
}

func (tf *TextFrame) drawBorder(page *Page) {
	if tf.border {
		rect := NewRect(tf.x, tf.y, tf.w, tf.h)
		rect.SetBorderColor(tf.borderColor)
		rect.DrawOn(page)
	}
}

func (tf *TextFrame) DrawOn(page *Page) ([]float32, error) {
	if page == nil {
		return nil, fmt.Errorf("page cannot be nil")
	}

	yText := tf.y + tf.f1.GetAscent(tf.f1.GetSize())
	for len(tf.paragraphs) > 0 {
		tokens := tf.paragraphs[len(tf.paragraphs)-1]
		tf.paragraphs = tf.paragraphs[:len(tf.paragraphs)-1]

		var textLine *TextLine
		sb := strings.Builder{}
		var token string

		for len(tokens) > 0 {
			if yText+tf.f1.GetDescent(tf.f1.GetSize()) < tf.y+tf.h {
				token = tokens[len(tokens)-1]
				tokens = tokens[:len(tokens)-1]

				if tf.f1.StringWidth(tf.f1.GetSize(), sb.String()+token) < tf.w {
					sb.WriteString(token)
					sb.WriteString(single.Space)
				} else {
					textLine = NewTextLine(tf.f1, strings.TrimSpace(sb.String()))
					textLine.SetLocation(tf.x, yText)
					textLine.DrawOn(page)
					sb.Reset()
					tokens = append(tokens, token)
					yText += tf.leading
				}
			} else {
				tf.paragraphs = append(tf.paragraphs, tokens)
				tf.drawBorder(page)
				return []float32{tf.x + tf.w, tf.y + tf.h}, nil
			}
		}

		if strings.TrimSpace(sb.String()) != "" {
			textLine = NewTextLine(tf.f1, strings.TrimSpace(sb.String()))
			textLine.SetLocation(tf.x, yText)
			textLine.DrawOn(page)
			yText += tf.leading
		}
		yText += tf.leading
	}

	tf.drawBorder(page)
	return []float32{tf.x + tf.w, tf.y + tf.h}, nil
}
