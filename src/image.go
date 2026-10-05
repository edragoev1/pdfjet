// image.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"bufio"
	"bytes"
	"encoding/hex"
	"io"
	"math"
	"os"
	"strconv"

	"github.com/edragoev1/pdfjet/v9/src/compliance"
	"github.com/edragoev1/pdfjet/v9/src/content"
	"github.com/edragoev1/pdfjet/v9/src/internal/device"
	"github.com/edragoev1/pdfjet/v9/src/internal/imagetype"
	"github.com/edragoev1/pdfjet/v9/src/structelem"
)

// Image describes an image object.
// The image type can be one of the following:
//
//	imagetype.JPG, imagetype.PNG or imagetype.BMP
//
// Please see Example_03 and Example_24.
type Image struct {
	objNumber int
	pdf       *PDF    // The PDF the image was added to, or nil for an image of an existing PDF
	x         float32 // Position of the image on the page
	y         float32
	w         float32 // Image width
	h         float32 // Image height
	// The pixels of the image across and down, which the image object is
	// written with: a float32 holds every whole number only up to 2^24.
	pixelWidth     int
	pixelHeight    int
	uri            string
	key            string
	degrees        int
	flipUpsideDown bool
	invertedInks   bool  // A CMYK JPEG that Adobe software wrote, with its inks inverted
	colorKeyMask   []int // The /Mask of the transparent color of a grayscale or truecolor PNG
	// The colors of a pixel of a PNG whose stream is its IDAT data, for the
	// /DecodeParms that undo the PNG filters of its rows, or 0.
	decodeColors   int
	palette        []byte // The colors of the /Indexed color space of a palette PNG, or nil
	language       string
	altDescription string
	actualText     string
}

// NewImageFromFile creates an image from the PNG, BMP or JPEG file at the specified path.
// It panics if the extension is not supported or the file cannot be opened.
func NewImageFromFile(pdf *PDF, filePath string) *Image {
	file, err := os.Open(filePath)
	if err != nil {
		panic(err)
	}
	defer func(file *os.File) {
		err := file.Close()
		if err != nil {
			panic("Error closing file: " + err.Error())
		}
	}(file)
	return NewImage(pdf, bufio.NewReader(file))
}

// NewImage the main constructor for the Image class.
//   - pdf: the PDF to which we add this image.
//   - inputStream: the input stream to read the image from.
func NewImage(pdf *PDF, reader io.Reader) *Image {
	buf := content.GetFromStream(reader)
	imageType := imageTypeOf(buf)
	reader = bytes.NewReader(buf)
	image := new(Image)
	image.pdf = pdf

	switch imageType {
	case imagetype.JPG:
		jpg, err := newJPGImageFromBytes(buf)
		if err != nil {
			panic(err)
		}
		data := jpg.getData()
		image.setPixels(int(jpg.width), int(jpg.height))
		if jpg.getColorComponents() == 1 {
			image.addImageToPDF(pdf, data, nil, imageType, device.Gray, 8)
		} else if jpg.getColorComponents() == 3 {
			image.addImageToPDF(pdf, data, nil, imageType, device.RGB, 8)
		} else if jpg.getColorComponents() == 4 {
			image.invertedInks = jpg.isAdobe()
			image.addImageToPDF(pdf, data, nil, imageType, device.CMYK, 8)
		}
		image.setPhysicalSize(jpg.GetPhysicalWidth(), jpg.GetPhysicalHeight())
	case imagetype.PNG:
		png := newPNGImage(reader)
		image.setPixels(png.w, png.h)
		image.colorKeyMask = png.GetColorKeyMask()
		image.decodeColors = png.decodeColors
		image.palette = png.palette
		colorSpace, bitsPerComponent := png.colorSpace()
		image.addImageToPDF(pdf, png.stream, png.GetAlpha(), imageType, colorSpace, bitsPerComponent)
		image.setPhysicalSize(png.physicalWidth, png.physicalHeight)
	case imagetype.BMP:
		bmp := newBMPImage(reader)
		data := bmp.getData()
		image.setPixels(bmp.w, bmp.h)
		image.addImageToPDF(pdf, data, bmp.getAlpha(), imageType, device.RGB, 8)
		image.setPhysicalSize(bmp.physicalWidth, bmp.physicalHeight)
	}

	return image
}

// NewImageForObjects adds this image to the existing PDF objects.
//   - objects: the map to which we add this image.
//   - inputStream: the input stream to read the image from.
func NewImageForObjects(objects *[]*PDFobj, reader io.Reader) *Image {
	buf := content.GetFromStream(reader)
	imageType := imageTypeOf(buf)
	reader = bytes.NewReader(buf)
	image := new(Image)

	switch imageType {
	case imagetype.JPG:
		jpg, err := newJPGImageFromBytes(buf)
		if err != nil {
			panic(err)
		}
		data := jpg.getData()
		image.setPixels(int(jpg.width), int(jpg.height))
		if jpg.getColorComponents() == 1 {
			image.addImageToObjects(objects, data, nil, imageType, device.Gray, 8)
		} else if jpg.getColorComponents() == 3 {
			image.addImageToObjects(objects, data, nil, imageType, device.RGB, 8)
		} else if jpg.getColorComponents() == 4 {
			image.invertedInks = jpg.isAdobe()
			image.addImageToObjects(objects, data, nil, imageType, device.CMYK, 8)
		}
		image.setPhysicalSize(jpg.GetPhysicalWidth(), jpg.GetPhysicalHeight())
	case imagetype.PNG:
		png := newPNGImage(reader)
		image.setPixels(png.w, png.h)
		image.colorKeyMask = png.GetColorKeyMask()
		image.decodeColors = png.decodeColors
		image.palette = png.palette
		colorSpace, bitsPerComponent := png.colorSpace()
		image.addImageToObjects(objects, png.stream, png.GetAlpha(), imageType, colorSpace, bitsPerComponent)
		image.setPhysicalSize(png.physicalWidth, png.physicalHeight)
	case imagetype.BMP:
		bmp := newBMPImage(reader)
		data := bmp.getData()
		image.setPixels(bmp.w, bmp.h)
		image.addImageToObjects(objects, data, bmp.getAlpha(), imageType, device.RGB, 8)
		image.setPhysicalSize(bmp.physicalWidth, bmp.physicalHeight)
	}

	return image
}

// NewImageFromPDFobj constructs new image from an existing PDF object.
func NewImageFromPDFobj(pdf *PDF, obj *PDFobj) *Image {
	image := new(Image)
	image.pdf = pdf

	width, err := strconv.ParseFloat(obj.GetValue("/Width"), 64)
	if err != nil {
		panic(err)
	}
	height, err := strconv.ParseFloat(obj.GetValue("/Height"), 64)
	if err != nil {
		panic(err)
	}
	image.setPixels(int(width), int(height))

	pdf.newObj()
	pdf.appendString("<<\n")
	pdf.appendString("/Type /XObject\n")
	pdf.appendString("/Subtype /Image\n")
	pdf.appendString("/Filter ")
	pdf.appendString(obj.GetValue("/Filter"))
	pdf.appendString("\n")
	pdf.appendString("/Width ")
	pdf.appendInteger(image.pixelWidth)
	pdf.appendString("\n")
	pdf.appendString("/Height ")
	pdf.appendInteger(image.pixelHeight)
	pdf.appendString("\n")
	colorSpace := obj.GetValue("/ColorSpace")
	if colorSpace != "" {
		pdf.appendString("/ColorSpace ")
		pdf.appendString(colorSpace)
		pdf.appendString("\n")
	}
	pdf.appendString("/BitsPerComponent ")
	pdf.appendString(obj.GetValue("/BitsPerComponent"))
	pdf.appendString("\n")
	decodeParms := obj.GetValue("/DecodeParms")
	if decodeParms != "" {
		pdf.appendString("/DecodeParms ")
		pdf.appendString(decodeParms)
		pdf.appendString("\n")
	}
	imageMask := obj.GetValue("/ImageMask")
	if imageMask != "" {
		pdf.appendString("/ImageMask ")
		pdf.appendString(imageMask)
		pdf.appendString("\n")
	}
	pdf.appendString("/Length ")
	pdf.appendInteger(len(obj.stream))
	pdf.appendString("\n")
	pdf.appendString(">>\n")
	pdf.appendString("stream\n")
	pdf.appendByteArray(obj.stream)
	pdf.appendString("\nendstream\n")
	pdf.endObj()
	pdf.images = append(pdf.images, image)
	image.objNumber = pdf.getObjNumber()

	return image
}

// setPixels sets the pixels of the image across and down, which it is drawn
// at, a pixel to a point, unless the file asks for another size.
func (image *Image) setPixels(width, height int) {
	image.pixelWidth = width
	image.pixelHeight = height
	image.w = float32(width)
	image.h = float32(height)
}

// setPhysicalSize draws the image at the size the file asks for, when it asks
// for one: the pHYs chunk of a PNG, the JFIF density of a JPEG or the pixels
// per metre of a BMP. The width and the height are the pixels of the image until here,
// which is what the image object of the PDF is written with, and are the size
// it is drawn at from here on.
func (image *Image) setPhysicalSize(width, height float32) {
	if width > 0.0 && height > 0.0 {
		image.w = width
		image.h = height
	}
}

// SetLocation sets the location of this image on the page to (x, y).
//
//   - x: the x coordinate of the top left corner of the image.
//   - y: the y coordinate of the top left corner of the image.
func (image *Image) SetLocation(x, y float32) Drawable {
	image.x = x
	image.y = y
	return image
}

// ScaleBy scales this image by the specified factor.
//   - factor: the factor used to scale the image.
func (image *Image) ScaleBy(factor float32) *Image {
	image.w = float32(image.w * factor)
	image.h = float32(image.h * factor)
	return image
}

// ScaleByWidthAndHeight scales this image by the specified width and height factor.
//
// Author: Pieter Libin, pieter@emweb.be
//
//   - widthFactor: the factor used to scale the width of the image
//   - heightFactor: the factor used to scale the height of the image
func (image *Image) ScaleByWidthAndHeight(widthFactor, heightFactor float32) *Image {
	image.w = float32(image.w * widthFactor)
	image.h = float32(image.h * heightFactor)
	return image
}

// ResizeWidth resizes the image to the specified width.
func (image *Image) ResizeWidth(width float32) *Image {
	factor := width / image.GetWidth()
	return image.ScaleByWidthAndHeight(factor, factor)
}

// ResizeHeight resizes the image to the specified height.
func (image *Image) ResizeHeight(height float32) *Image {
	factor := height / image.GetHeight()
	return image.ScaleByWidthAndHeight(factor, factor)
}

// SetURIAction sets the URI for the "click box" action.
//   - uri: the URI
func (image *Image) SetURIAction(uri string) *Image {
	image.uri = uri
	return image
}

// SetGoToAction sets the destination key for the action.
//   - key: the destination name.
func (image *Image) SetGoToAction(key string) *Image {
	image.key = key
	return image
}

// SetRotation rotates this image clockwise by 0, 90, 180 or 270 degrees, as every
// rotation in PDFjet turns. It panics on any other angle.
func (image *Image) SetRotation(degrees int) *Image {
	if degrees != 0 && degrees != 90 && degrees != 180 && degrees != 270 {
		panic("The rotation angle must be 0, 90, 180 or 270")
	}
	image.degrees = degrees
	return image
}

// SetAltDescription sets the alternate description of this image.
//   - altDescription: the alternate description of the image.
//
// Returns this Image.
func (image *Image) SetAltDescription(altDescription string) *Image {
	image.altDescription = altDescription
	return image
}

// SetActualText sets the actual text for this image.
//   - actualText: the actual text for the image.
//
// Returns this Image.
func (image *Image) SetActualText(actualText string) *Image {
	image.actualText = actualText
	return image
}

// SetLanguage sets the language of this image, for example "en-US".
func (image *Image) SetLanguage(language string) *Image {
	image.language = language
	return image
}

// DrawOn draws this image on the specified page.
//   - page: the page to draw this image on.
//
// Returns x and y coordinates of the bottom right corner of this component.
func (image *Image) DrawOn(page *Page) [2]float32 {
	if page == nil {
		return [2]float32{image.x + image.w, image.y + image.h} // Measured, not drawn
	}
	if image.pdf != nil && page.pdf != image.pdf {
		page.pdf.fail("The image belongs to another PDF.")
		return [2]float32{image.x + image.w, image.y + image.h}
	}
	if image.w == 0 || image.h == 0 {
		return [2]float32{image.x + image.w, image.y + image.h} // A zero size image paints nothing.
	}
	// A linked image is a Figure in the Link its annotation joins
	var link *structElement
	if image.uri != "" || image.key != "" {
		link = page.beginLink()
	}
	page.AddBDC(structelem.Figure, image.language, image.actualText, image.altDescription)
	page.SaveGraphicsState()

	switch image.degrees {
	case 0:
		page.appendFloat32(image.w)
		page.appendString(" ")
		page.appendFloat32(0.0)
		page.appendString(" ")
		page.appendFloat32(0.0)
		page.appendString(" ")
		page.appendFloat32(image.h)
		page.appendString(" ")
		page.appendFloat32(image.x)
		page.appendString(" ")
		page.appendFloat32(page.height - (image.y + image.h))
		page.appendString(" cm\n")
	case 90:
		page.appendFloat32(image.h)
		page.appendString(" ")
		page.appendFloat32(0.0)
		page.appendString(" ")
		page.appendFloat32(0.0)
		page.appendString(" ")
		page.appendFloat32(image.w)
		page.appendString(" ")
		page.appendFloat32(image.x)
		page.appendString(" ")
		page.appendFloat32(page.height - image.y)
		page.appendString(" cm\n")
		page.appendString("0 -1 1 0 0 0 cm\n")
	case 180:
		page.appendFloat32(image.w)
		page.appendString(" ")
		page.appendFloat32(0.0)
		page.appendString(" ")
		page.appendFloat32(0.0)
		page.appendString(" ")
		page.appendFloat32(image.h)
		page.appendString(" ")
		page.appendFloat32(image.x + image.w)
		page.appendString(" ")
		page.appendFloat32(page.height - image.y)
		page.appendString(" cm\n")
		page.appendString("-1 0 0 -1 0 0 cm\n")
	case 270:
		page.appendFloat32(image.h)
		page.appendString(" ")
		page.appendFloat32(0.0)
		page.appendString(" ")
		page.appendFloat32(0.0)
		page.appendString(" ")
		page.appendFloat32(image.w)
		page.appendString(" ")
		page.appendFloat32(image.x + image.h)
		page.appendString(" ")
		page.appendFloat32(page.height - (image.y + image.w))
		page.appendString(" cm\n")
		page.appendString("0 1 -1 0 0 0 cm\n")
	}

	if image.flipUpsideDown {
		page.appendString("1 0 0 -1 0 1 cm\n")
	}

	page.appendString("/Im")
	page.appendInteger(image.objNumber)
	page.appendString(" Do\n")

	page.RestoreGraphicsState()

	// Turned a quarter of the way, the image is as wide as it is tall unturned
	if image.degrees == 90 || image.degrees == 270 {
		page.SetFigureBoundingBox(image.x, image.y, image.h, image.w)
	} else {
		page.SetFigureBoundingBox(image.x, image.y, image.w, image.h)
	}
	page.AddEMC()

	page.endLink(link)
	if image.uri != "" || image.key != "" {
		// The link covers the image as it is drawn, turned or not.
		w, h := image.w, image.h
		if image.degrees == 90 || image.degrees == 270 {
			w, h = h, w
		}
		page.addAnnotation(&annotationObject{
			annotationType: annotationLink,
			x1:             image.x,
			y1:             image.y,
			x2:             image.x + w,
			y2:             image.y + h,
			vertices:       nil,
			opacity:        0.0,
			title:          "",
			contents:       "",
			uri:            image.uri,
			key:            image.key, // The destination name
			language:       image.language,
			actualText:     image.actualText,
			altDescription: image.altDescription,
			linkElement:    link,
		})
	}

	return [2]float32{image.x + image.w, image.y + image.h}
}

// GetWidth returns the width of this image when drawn on the page.
// The scaling is taken into account.
// Returns w - the width of this image.
func (image *Image) GetWidth() float32 {
	return image.w
}

// GetHeight returns the height of this image when drawn on the page.
// The scaling is taken into account.
// Returns h - the height of this image.
func (image *Image) GetHeight() float32 {
	return image.h
}

func (image *Image) addSoftMask(pdf *PDF, data []byte, colorSpace string, bitsPerComponent int) {
	pdf.newObj()
	pdf.appendString("<<\n")
	pdf.appendString("/Type /XObject\n")
	pdf.appendString("/Subtype /Image\n")
	pdf.appendString("/Filter /FlateDecode\n")
	pdf.appendString("/Width ")
	pdf.appendInteger(image.pixelWidth)
	pdf.appendString("\n")
	pdf.appendString("/Height ")
	pdf.appendInteger(image.pixelHeight)
	pdf.appendString("\n")
	pdf.appendString("/ColorSpace /")
	pdf.appendString(colorSpace)
	pdf.appendString("\n")
	pdf.appendString("/BitsPerComponent ")
	pdf.appendInteger(bitsPerComponent)
	pdf.appendString("\n")

	buf := data
	if pdf.encryption != nil {
		buf = pdf.encryption.encrypt(data)
	}
	pdf.appendString("/Length ")
	pdf.appendInteger(len(buf))
	pdf.appendString("\n")
	pdf.appendString(">>\n")
	pdf.appendString("stream\n")
	pdf.appendByteArray(buf)
	pdf.appendString("\nendstream\n")
	pdf.endObj()
	image.objNumber = pdf.getObjNumber()
}

func (image *Image) addImageToPDF(
	pdf *PDF,
	data []byte,
	alpha []byte,
	imageType imagetype.ImageType,
	colorSpace string,
	bitsPerComponent int) {
	if !image.isPDFA(pdf, alpha, colorSpace, bitsPerComponent) {
		return
	}
	if alpha != nil {
		image.addSoftMask(pdf, alpha, device.Gray, 8)
	}
	pdf.newObj()
	pdf.appendString("<<\n")
	pdf.appendString("/Type /XObject\n")
	pdf.appendString("/Subtype /Image\n")
	switch imageType {
	case imagetype.JPG:
		pdf.appendString("/Filter /DCTDecode\n")
	case imagetype.PNG, imagetype.BMP:
		pdf.appendString("/Filter /FlateDecode\n")
		if alpha != nil {
			pdf.appendString("/SMask ")
			pdf.appendInteger(image.objNumber)
			pdf.appendString(" 0 R\n")
		} else if image.colorKeyMask != nil {
			pdf.appendString("/Mask [")
			for i, value := range image.colorKeyMask {
				if i > 0 {
					pdf.appendString(" ")
				}
				pdf.appendInteger(value)
			}
			pdf.appendString("]\n")
		}
	}
	pdf.appendString("/Width ")
	pdf.appendInteger(image.pixelWidth)
	pdf.appendString("\n")
	pdf.appendString("/Height ")
	pdf.appendInteger(image.pixelHeight)
	pdf.appendString("\n")
	if image.palette != nil {
		palette := image.palette
		if pdf.encryption != nil {
			palette = pdf.encryption.encrypt(palette)
		}
		pdf.appendString("/ColorSpace [/Indexed /DeviceRGB ")
		pdf.appendInteger(len(image.palette)/3 - 1)
		pdf.appendString(" <")
		pdf.appendString(hex.EncodeToString(palette))
		pdf.appendString(">]\n")
	} else {
		pdf.appendString("/ColorSpace /")
		pdf.appendString(colorSpace)
		pdf.appendString("\n")
	}
	pdf.appendString("/BitsPerComponent ")
	pdf.appendInteger(bitsPerComponent)
	pdf.appendString("\n")
	if colorSpace == device.CMYK && image.invertedInks {
		// Adobe software, Photoshop among them, stores the inks inverted.
		pdf.appendString("/Decode [1.0 0.0 1.0 0.0 1.0 0.0 1.0 0.0]\n")
	}
	if image.decodeColors != 0 {
		// The rows of a PNG, each with the filter type of its PNG filter.
		pdf.appendString("/DecodeParms <</Predictor 15 /Colors ")
		pdf.appendInteger(image.decodeColors)
		pdf.appendString(" /BitsPerComponent ")
		pdf.appendInteger(bitsPerComponent)
		pdf.appendString(" /Columns ")
		pdf.appendInteger(image.pixelWidth)
		pdf.appendString(">>\n")
	}

	buf := data
	if pdf.encryption != nil {
		buf = pdf.encryption.encrypt(data)
	}
	pdf.appendString("/Length ")
	pdf.appendInteger(len(buf))
	pdf.appendString("\n")
	pdf.appendString(">>\n")
	pdf.appendString("stream\n")
	pdf.appendByteArray(buf)
	pdf.appendString("\nendstream\n")
	pdf.endObj()
	pdf.images = append(pdf.images, image)
	image.objNumber = pdf.getObjNumber()
}

// isPDFA reports whether a PDF/A document can hold the image, and fails the
// document when it cannot. Its output intent is sRGB, an RGB profile, so its
// images are gray or RGB and not CMYK, as ISO 19005 asks of a device color
// space. PDF/A-1 is PDF 1.4, which has no soft masks, and 8 bits per
// component at most. Any other document holds any image.
func (image *Image) isPDFA(pdf *PDF, alpha []byte, colorSpace string, bitsPerComponent int) bool {
	level := pdf.compliance
	if level == compliance.PDF_1_7 || level == compliance.PDF_UA_1 {
		return true
	}
	pdfA1 := level == compliance.PDF_A_1A || level == compliance.PDF_A_1B
	if colorSpace == device.CMYK {
		pdf.fail("A document of " + level.String() + " cannot hold a CMYK image: " +
			"its output intent is sRGB, so its images are gray or RGB.")
		return false
	}
	if pdfA1 && alpha != nil {
		pdf.fail("A document of " + level.String() + " cannot hold an image with transparency: " +
			"PDF/A-1 has no soft masks, so its images are opaque.")
		return false
	}
	if pdfA1 && bitsPerComponent > 8 {
		pdf.fail("A document of " + level.String() + " cannot hold an image of " +
			strconv.Itoa(bitsPerComponent) + " bits per component: PDF/A-1 has 8 at most.")
		return false
	}
	return true
}

func (image *Image) addSoftMaskToObjects(
	objects *[]*PDFobj,
	data []byte,
	colorSpace string,
	bitsPerComponent int) {
	obj := newPDFobj()
	obj.dict = append(obj.dict, "<<")
	obj.dict = append(obj.dict, "/Type")
	obj.dict = append(obj.dict, "/XObject")
	obj.dict = append(obj.dict, "/Subtype")
	obj.dict = append(obj.dict, "/Image")
	obj.dict = append(obj.dict, "/Filter")
	obj.dict = append(obj.dict, "/FlateDecode")
	obj.dict = append(obj.dict, "/Width")
	obj.dict = append(obj.dict, strconv.Itoa(image.pixelWidth))
	obj.dict = append(obj.dict, "/Height")
	obj.dict = append(obj.dict, strconv.Itoa(image.pixelHeight))
	obj.dict = append(obj.dict, "/ColorSpace")
	obj.dict = append(obj.dict, "/"+colorSpace)
	obj.dict = append(obj.dict, "/BitsPerComponent")
	obj.dict = append(obj.dict, strconv.Itoa(bitsPerComponent))
	obj.dict = append(obj.dict, "/Length")
	obj.dict = append(obj.dict, strconv.Itoa(len(data)))
	obj.dict = append(obj.dict, ">>")
	obj.setStream(data)
	obj.number = len(*objects) + 1
	*objects = append(*objects, obj)
	image.objNumber = obj.number
}

func (image *Image) addImageToObjects(
	objects *[]*PDFobj,
	data []byte,
	alpha []byte,
	imageType imagetype.ImageType,
	colorSpace string,
	bitsPerComponent int) {
	if alpha != nil {
		image.addSoftMaskToObjects(objects, alpha, device.Gray, 8)
	}

	obj := newPDFobj()
	obj.dict = append(obj.dict, "<<")
	obj.dict = append(obj.dict, "/Type")
	obj.dict = append(obj.dict, "/XObject")
	obj.dict = append(obj.dict, "/Subtype")
	obj.dict = append(obj.dict, "/Image")
	switch imageType {
	case imagetype.JPG:
		obj.dict = append(obj.dict, "/Filter")
		obj.dict = append(obj.dict, "/DCTDecode")
	case imagetype.PNG, imagetype.BMP:
		obj.dict = append(obj.dict, "/Filter")
		obj.dict = append(obj.dict, "/FlateDecode")
		if alpha != nil {
			obj.dict = append(obj.dict, "/SMask")
			obj.dict = append(obj.dict, strconv.Itoa(image.objNumber))
			obj.dict = append(obj.dict, "0")
			obj.dict = append(obj.dict, "R")
		} else if image.colorKeyMask != nil {
			obj.dict = append(obj.dict, "/Mask", "[")
			for _, value := range image.colorKeyMask {
				obj.dict = append(obj.dict, strconv.Itoa(value))
			}
			obj.dict = append(obj.dict, "]")
		}
	}
	obj.dict = append(obj.dict, "/Width")
	obj.dict = append(obj.dict, strconv.Itoa(image.pixelWidth))
	obj.dict = append(obj.dict, "/Height")
	obj.dict = append(obj.dict, strconv.Itoa(image.pixelHeight))
	obj.dict = append(obj.dict, "/ColorSpace")
	if image.palette != nil {
		obj.dict = append(obj.dict, "[", "/Indexed", "/DeviceRGB")
		obj.dict = append(obj.dict, strconv.Itoa(len(image.palette)/3-1))
		obj.dict = append(obj.dict, "<"+hex.EncodeToString(image.palette)+">", "]")
	} else {
		obj.dict = append(obj.dict, "/"+colorSpace)
	}
	obj.dict = append(obj.dict, "/BitsPerComponent")
	obj.dict = append(obj.dict, strconv.Itoa(bitsPerComponent))
	if colorSpace == device.CMYK && image.invertedInks {
		// Adobe software, Photoshop among them, stores the inks inverted.
		obj.dict = append(obj.dict, "/Decode")
		obj.dict = append(obj.dict, "[")
		obj.dict = append(obj.dict, "1.0")
		obj.dict = append(obj.dict, "0.0")
		obj.dict = append(obj.dict, "1.0")
		obj.dict = append(obj.dict, "0.0")
		obj.dict = append(obj.dict, "1.0")
		obj.dict = append(obj.dict, "0.0")
		obj.dict = append(obj.dict, "1.0")
		obj.dict = append(obj.dict, "0.0")
		obj.dict = append(obj.dict, "]")
	}
	if image.decodeColors != 0 {
		// The rows of a PNG, each with the filter type of its PNG filter.
		obj.dict = append(obj.dict, "/DecodeParms", "<<")
		obj.dict = append(obj.dict, "/Predictor", "15")
		obj.dict = append(obj.dict, "/Colors", strconv.Itoa(image.decodeColors))
		obj.dict = append(obj.dict, "/BitsPerComponent", strconv.Itoa(bitsPerComponent))
		obj.dict = append(obj.dict, "/Columns", strconv.Itoa(image.pixelWidth))
		obj.dict = append(obj.dict, ">>")
	}
	obj.dict = append(obj.dict, "/Length")
	obj.dict = append(obj.dict, strconv.Itoa(len(data)))
	obj.dict = append(obj.dict, ">>")
	obj.setStream(data)
	obj.number = len(*objects) + 1
	*objects = append(*objects, obj)
	image.objNumber = obj.number
}

// ResizeToFit resizes an image so it would fit on a page.
func (image *Image) ResizeToFit(page *Page, keepAspectRatio bool) {
	if keepAspectRatio {
		image.ScaleBy(float32(math.Min(
			float64((page.GetWidth()-image.x)/image.w),
			float64((page.GetHeight()-image.y)/image.h))))
	} else {
		image.ScaleByWidthAndHeight((page.GetWidth()-image.x)/image.w, (page.GetHeight()-image.y)/image.h)
	}
}

// SetFlipUpsideDown sets whether this image is drawn upside down.
func (image *Image) SetFlipUpsideDown(flipUpsideDown bool) *Image {
	image.flipUpsideDown = flipUpsideDown
	return image
}

// imageTypeOf returns the type of the image from its first bytes, and panics
// when the image is not a PNG, JPEG or BMP file.
func imageTypeOf(buf []byte) imagetype.ImageType {
	if len(buf) >= 4 && buf[0] == 0x89 && buf[1] == 'P' && buf[2] == 'N' && buf[3] == 'G' {
		return imagetype.PNG
	}
	if len(buf) >= 2 && buf[0] == 0xFF && buf[1] == 0xD8 {
		return imagetype.JPG
	}
	if len(buf) >= 2 && buf[0] == 'B' && buf[1] == 'M' {
		return imagetype.BMP
	}
	panic("The image is not a PNG, JPEG or BMP file.")
}
