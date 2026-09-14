// container.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"math"

	"github.com/edragoev1/pdfjet/v9/src/internal/fastfloat"
)

// Container is a group of drawable elements that are moved, rotated and
// scaled together: shapes, text, images, annotations and nested containers.
// The elements are drawn into the page on every DrawOn.
//
// Use a Container to lay out a group once and place it on a page, or to
// rotate and scale elements that have no rotation of their own. Use a Stamp
// for content that repeats on many pages, like a header, a footer or a
// watermark: it is written once as a form XObject and each placement is a
// single operator. Please see Example_06 and Example_35.
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
	parent        *Container
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

// SetRotation sets the rotation angle of the container in degrees.
//
// degrees specifies the angle to rotate counter-clockwise.
func (c *Container) SetRotation(degrees float64) *Container {
	c.rotateDegrees = float32(degrees)
	return c
}

// GetRotationCenter returns the center of this container, which it rotates around.
func (c *Container) GetRotationCenter() [2]float32 {
	return [2]float32{c.x + c.width/2.0, c.y + c.height/2.0}
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
func (c *Container) SetBorderColor(borderColor int32) *Container {
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
	if child, ok := element.(*Container); ok {
		child.parent = c
	}
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
	page.SaveGraphicsState()

	// 1) Translate container to its final position
	page.appendString("1 0 0 1 ")
	page.appendByteArray(fastfloat.ToByteArray(c.x))
	page.appendByte(' ')
	page.appendByteArray(fastfloat.ToByteArray(-c.y))
	page.appendString(" cm\n")

	cx := c.width / 2
	cy := c.height / 2

	// 2) Move origin to container center
	page.appendString("1 0 0 1 ")
	page.appendByteArray(fastfloat.ToByteArray(cx))
	page.appendByte(' ')
	page.appendByteArray(fastfloat.ToByteArray(page.height - cy))
	page.appendString(" cm\n")

	// 3) Rotate around container center
	rad := float64(c.rotateDegrees) * (math.Pi / 180.0)
	cos := float32(math.Cos(rad))
	sin := float32(math.Sin(rad))
	page.appendByteArray(fastfloat.ToByteArray(cos))
	page.appendByte(' ')
	page.appendByteArray(fastfloat.ToByteArray(sin))
	page.appendByte(' ')
	page.appendByteArray(fastfloat.ToByteArray(-sin))
	page.appendByte(' ')
	page.appendByteArray(fastfloat.ToByteArray(cos))
	page.appendString(" 0 0 cm\n")

	// 4) Scale around container center
	page.appendByteArray(fastfloat.ToByteArray(c.scaleX))
	page.appendString(" 0 0 ")
	page.appendByteArray(fastfloat.ToByteArray(c.scaleY))
	page.appendString(" 0 0 cm\n")

	// 5) Move origin back for child drawing
	page.appendString("1 0 0 1 ")
	page.appendByteArray(fastfloat.ToByteArray(-cx))
	page.appendByte(' ')
	page.appendByteArray(fastfloat.ToByteArray(-(page.height - cy)))
	page.appendString(" cm\n")

	// 6) Draw children elements
	for _, element := range c.elements {
		if a, ok := element.(annotation); ok {
			annot := a.baseAnnotation()
			annot.point1[0] += c.x
			annot.point1[1] += c.y
			annot.point2[0] += c.x
			annot.point2[1] += c.y
			annot.container = c
			if c.parent != nil {
				annot.point1[0] += c.parent.x
				annot.point1[1] += c.parent.y
				annot.point2[0] += c.parent.x
				annot.point2[1] += c.parent.y
			}
			annot.rotate(float64(-c.rotateDegrees))
		}
		element.DrawOn(page)
	}

	page.RestoreGraphicsState()

	// Return bottom-right position of container
	return [2]float32{c.x + c.width, c.y + c.height}
}

// rotateAroundCenter rotates a point around a center by the given degrees and
// returns the rotated point.
func rotateAroundCenter(point, center [2]float32, degrees float64) [2]float32 {
	radians := degrees * math.Pi / 180.0
	cos := float32(math.Cos(radians))
	sin := float32(math.Sin(radians))

	dx := point[0] - center[0]
	dy := point[1] - center[1]

	return [2]float32{
		center[0] + (dx*cos - dy*sin),
		center[1] + (dx*sin + dy*cos),
	}
}
