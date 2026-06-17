package pdfjet

/**
 * container.go
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */

import (
	"math"

	"github.com/edragoev1/pdfjet/src/fastfloat"
)

// Container represents a drawable container that can hold other drawable elements.
// It supports positioning, scaling, and rotation.
type Container struct {
	X             float32    // The X coordinate of the container on the page.
	Y             float32    // The Y coordinate of the container on the page.
	Width         float32    // The width of the container.
	Height        float32    // The height of the container.
	RotateDegrees float32    // The rotation angle of the container in degrees.
	ScaleX        float32    // The scaling factor along the X-axis.
	ScaleY        float32    // The scaling factor along the Y-axis.
	Elements      []Drawable // The list of child drawable elements.
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
		Width:         width,
		Height:        height,
		RotateDegrees: 0,
		ScaleX:        1,
		ScaleY:        1,
		Elements:      []Drawable{},
	}
}

// SetPosition sets the position of the container on the page.
//
// x is the horizontal coordinate and y is the vertical coordinate.
func (c *Container) SetPosition(x, y float32) {
	c.X = x
	c.Y = y
}

// SetLocation sets the location of the container on the page.
//
// This is an alias for SetPosition.
//
// x is the horizontal coordinate and y is the vertical coordinate.
func (c *Container) SetLocation(x, y float32) {
	c.X = x
	c.Y = y
}

// SetRotation sets the rotation angle of the container in degrees.
//
// degrees specifies the angle to rotate counter-clockwise.
func (c *Container) SetRotation(degrees float64) {
	c.RotateDegrees = float32(degrees)
}

// SetRotationClockwise sets the clockwise rotation angle of the container in degrees.
//
// degrees specifies the angle to rotate clockwise.
func (c *Container) SetRotationClockwise(degrees float64) {
	c.RotateDegrees = float32(-degrees)
}

func (c *Container) GetRotationCenter() [2]float32 {
	return [2]float32{c.X + c.Width/2.0, c.Y + c.Height/2.0}
}

// SetRotationCounterClockwise sets the counter-clockwise rotation angle of the container in degrees.
//
// degrees specifies the angle to rotate counter-clockwise.
func (c *Container) SetRotationCounterClockwise(degrees float64) {
	c.RotateDegrees = float32(degrees)
}

// SetScaleFactor sets a uniform scaling factor for both X and Y axes.
//
// factor specifies the scaling factor to apply.
func (c *Container) SetScaleFactor(factor float32) {
	c.SetScaleFactorXY(factor, factor)
}

// SetScaleFactorXY sets non-uniform scaling factors for the X and Y axes.
//
// sx specifies the scaling factor along the X-axis.
// sy specifies the scaling factor along the Y-axis.
func (c *Container) SetScaleFactorXY(sx, sy float32) {
	c.ScaleX = sx
	c.ScaleY = sy
}

func (c *Container) AddBorder() {
	rect := new(Rect)
	rect.SetSize(c.Width, c.Height)
	c.Add(rect)
}

// Add adds a drawable element to this container.
//
// element is the Drawable object to add.
func (c *Container) Add(element Drawable) {
	c.Elements = append(c.Elements, element)
}

// RotateAroundCenter rotates a 2D point around a center point by the given degrees.
// Accepts []float32 slices where index 0 is X and index 1 is Y.
func RotateAroundCenter(point, center []float32, degrees float64) []float32 {
	// Convert degrees to radians
	rad := degrees * math.Pi / 180.0

	// Translate to center (point relative to center)
	dx := float64(point[0]) - float64(center[0])
	dy := float64(point[1]) - float64(center[1])

	// Apply rotation using 2D rotation matrix
	cos := math.Cos(rad)
	sin := math.Sin(rad)
	dxRot := dx*cos - dy*sin
	dyRot := dx*sin + dy*cos

	// Translate back
	nx := float64(center[0]) + dxRot
	ny := float64(center[1]) + dyRot

	return []float32{float32(nx), float32(ny)}
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
	page.appendString("q\n") // Save the graphics state

	// 1) Translate container to its final position
	page.appendString("1 0 0 1 ")
	page.appendByteArray(fastfloat.ToByteArray(c.X))
	page.appendByte(' ')
	page.appendByteArray(fastfloat.ToByteArray(-c.Y))
	page.appendString(" cm\n")

	cx := c.Width / 2
	cy := c.Height / 2

	// 2) Move origin to container center
	page.appendString("1 0 0 1 ")
	page.appendByteArray(fastfloat.ToByteArray(cx))
	page.appendByte(' ')
	page.appendByteArray(fastfloat.ToByteArray(page.height - cy))
	page.appendString(" cm\n")

	// 3) Rotate around container center
	rad := float64(c.RotateDegrees) * (math.Pi / 180.0)
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
	page.appendByteArray(fastfloat.ToByteArray(c.ScaleX))
	page.appendString(" 0 0 ")
	page.appendByteArray(fastfloat.ToByteArray(c.ScaleY))
	page.appendString(" 0 0 cm\n")

	// 5) Move origin back for child drawing
	page.appendString("1 0 0 1 ")
	page.appendByteArray(fastfloat.ToByteArray(-cx))
	page.appendByte(' ')
	page.appendByteArray(fastfloat.ToByteArray(-(page.height - cy)))
	page.appendString(" cm\n")

	// 6) Draw child elements
	for _, element := range c.Elements {
		_ = element.DrawOn(page)
	}

	page.appendString("Q\n") // Restore graphics state

	// Return bottom-right position of container
	return [2]float32{c.X + c.Width, c.Y + c.Height}
}
