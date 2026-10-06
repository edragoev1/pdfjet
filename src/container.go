// container.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

// Floating point: every float multiplication in this file is wrapped in
// float32(...) or float64(...) on purpose, so that Go's arm64 compiler does
// not fuse it with an addition and round otherwise than amd64 and the other
// ports. Keep the wrapping; see "Floating point on ARM" in README.md.
// check-no-fma.sh fails if one is removed.

import (
	"github.com/edragoev1/pdfjet/v9/src/color"
	"math"
)

// Container is a group of drawable elements that are moved, rotated and
// scaled together: shapes, text, images, annotations, stamps and nested
// containers.
//
// What a container is good at: it takes anything that implements Drawable,
// so a group can hold an Image, a Table, a chart, a barcode, a link or
// another annotation, and each element keeps everything it can do. Each
// element also tags itself in a PDF/UA document, so a TextLine in a container
// is a paragraph of its own and an Image is a figure with its alternate
// description. Laying a group out once and placing it, rotated or scaled, is
// what it is for; elements that have no rotation of their own get one this
// way.
//
// What it costs: the elements are drawn into the page on every DrawOn, so a
// container on 100 pages writes its drawing 100 times over.
//
// Use a Stamp instead for content that repeats on many pages -- a header, a
// footer, a logo, a watermark -- which is written once as a form XObject and
// placed with a few bytes. Of a 200 by 50 point box with two lines of text, a
// container writes 367 bytes into every page and a stamp 83, so the stamp is
// the smaller of the two from the fourth page on: over 100 pages it saves
// 12,855 bytes of a 104,530 byte file, and over 500 pages 66,052 of 275,553.
// On one page the stamp is the larger, since its XObject costs about 300
// bytes of its own, and it draws only paths and text in an embedded font.
//
// Please see Example_06 and Example_35.
type Container struct {
	x             float32    // The X coordinate of the container on the page.
	y             float32    // The Y coordinate of the container on the page.
	width         float32    // The width of the container.
	height        float32    // The height of the container.
	rotateDegrees float32    // The rotation angle of the container in degrees.
	scaleX        float32    // The scaling factor along the X-axis.
	scaleY        float32    // The scaling factor along the Y-axis.
	elements      []Drawable // The list of child drawable elements.
	border        *Rect
}

// NewContainer creates a new container with the specified width and height.
//
// The container is initialized with:
//   - Rotation set to 0 degrees
//   - Scaling factors set to 1.0 for both axes
//   - An empty slice of drawable elements
func NewContainer(width, height float32) *Container {
	return &Container{
		width:         width,
		height:        height,
		rotateDegrees: 0,
		scaleX:        1,
		scaleY:        1,
		elements:      []Drawable{},
	}
}

// SetLocation sets the location of the container on the page.
//
// x is the horizontal coordinate and y is the vertical coordinate.
func (c *Container) SetLocation(x, y float32) Drawable {
	c.x = x
	c.y = y
	return c
}

// SetRotation rotates this container around its center: clockwise for a positive
// angle, as every rotation in PDFjet turns, and counterclockwise for a negative
// angle.
func (c *Container) SetRotation(degrees float64) *Container {
	// The rotation of the page turns counterclockwise.
	c.rotateDegrees = float32(-degrees)
	return c
}

// GetRotationCenter returns the center of this container, which it rotates around.
func (c *Container) GetRotationCenter() [2]float32 {
	return [2]float32{c.x + float32(c.width/2.0), c.y + float32(c.height/2.0)}
}

// ScaleBy sets a uniform scaling factor for both X and Y axes.
//
// factor specifies the scaling factor to apply.
func (c *Container) ScaleBy(factor float32) *Container {
	c.ScaleByWidthAndHeight(factor, factor)
	return c
}

// ScaleByWidthAndHeight sets non-uniform scaling factors for the X and Y axes.
//
// sx specifies the scaling factor along the X-axis.
// sy specifies the scaling factor along the Y-axis.
func (c *Container) ScaleByWidthAndHeight(sx, sy float32) *Container {
	c.scaleX = sx
	c.scaleY = sy
	return c
}

// SetBorderColor sets the 0xRRGGBB color of the border around this container.
// color.Transparent leaves it unchanged.
func (c *Container) SetBorderColor(borderColor int32) *Container {
	if borderColor == color.Transparent {
		return c
	}
	if c.border == nil {
		c.border = NewRect(0.0, 0.0, c.width, c.height)
		c.Add(c.border)
	}
	c.border.SetBorderColor(borderColor)
	return c
}

// SetBorderColorRGB sets the color of the border around this container from
// red, green and blue values.
func (c *Container) SetBorderColorRGB(rgbColor [3]float32) *Container {
	if c.border == nil {
		c.border = NewRect(0.0, 0.0, c.width, c.height)
		c.Add(c.border)
	}
	c.border.SetBorderColorRGB(rgbColor)
	return c
}

// Add adds a drawable element to this container.
//
// element is the Drawable object to add.
func (c *Container) Add(element Drawable) *Container {
	c.elements = append(c.elements, element)
	return c
}

// DrawOn draws the container and all child elements onto the given page.
//
// The transformations applied are:
//  1. Translate container to its final position on the page
//  2. Move origin to the center of the container
//  3. Rotate around the container center
//  4. Scale around the container center
//  5. Move origin back for child drawing
//
// Returns a slice containing the bottom-right position of the container.
// Returns an error if drawing fails.
func (c *Container) DrawOn(page *Page) [2]float32 {
	if page == nil || c.scaleX == 0 || c.scaleY == 0 {
		return [2]float32{c.x + c.width, c.y + c.height} // Measured, or nothing to paint.
	}
	page.SaveGraphicsState()

	// 1) Translate container to its final position
	page.appendString("1 0 0 1 ")
	page.appendFloat32(c.x)
	page.appendByte(' ')
	page.appendFloat32(-c.y)
	page.appendString(" cm\n")

	cx := float32(c.width / 2)
	cy := float32(c.height / 2)

	// 2) Move origin to container center
	page.appendString("1 0 0 1 ")
	page.appendFloat32(cx)
	page.appendByte(' ')
	page.appendFloat32(page.height - cy)
	page.appendString(" cm\n")

	// 3) Rotate around container center
	rad := float64(float64(c.rotateDegrees) * (math.Pi / 180.0))
	cos := float32(math.Cos(rad))
	sin := float32(math.Sin(rad))
	page.appendRotation(cos, sin)

	// 4) Scale around container center
	page.appendFloat32(c.scaleX)
	page.appendString(" 0 0 ")
	page.appendFloat32(c.scaleY)
	page.appendString(" 0 0 cm\n")

	// 5) Move origin back for child drawing
	page.appendString("1 0 0 1 ")
	page.appendFloat32(-cx)
	page.appendByte(' ')
	page.appendFloat32(-(page.height - cy))
	page.appendString(" cm\n")

	// 6) Draw children elements. What the page does not move with the cm
	// operators above, the rectangles of the links and of the annotations and
	// the bounding boxes of the figures, it moves with the same transform, from
	// the top left corner of the page: turned and scaled around the center of
	// the container, and moved to its location.
	m0 := float32(c.scaleX * cos)
	m1 := float32(-c.scaleX * sin)
	m2 := float32(c.scaleY * sin)
	m3 := float32(c.scaleY * cos)
	centerX := c.x + cx
	centerY := c.y + cy
	saved := page.pushTransform([6]float32{m0, m1, m2, m3, centerX - float32(m0*cx) - float32(m2*cy), centerY - float32(m1*cx) - float32(m3*cy)})
	for _, element := range c.elements {
		element.DrawOn(page)
	}
	page.popTransform(saved)

	page.RestoreGraphicsState()

	// Return bottom-right position of container
	return [2]float32{c.x + c.width, c.y + c.height}
}
