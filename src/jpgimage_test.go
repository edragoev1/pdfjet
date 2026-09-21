// jpgimage_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"bytes"
	"os"
	"strings"
	"testing"

	"github.com/edragoev1/pdfjet/v9/src/letter"
)

// A CMYK JPEG that Adobe software wrote, as images/cmyk.jpg is, has an APP14
// marker and stores its inks inverted, so the image is written with a Decode
// array that inverts them back; one without the marker is written as it is.

// testWithoutAPP14 returns the JPEG without its APP14 segment.
func testWithoutAPP14(jpeg []byte) []byte {
	for i := 2; i+3 < len(jpeg); i++ {
		if jpeg[i] == 0xFF && jpeg[i+1] == 0xEE {
			end := i + 2 + (int(jpeg[i+2])<<8 | int(jpeg[i+3]))
			return append(append([]byte(nil), jpeg[:i]...), jpeg[end:]...)
		}
	}
	return jpeg
}

func testPDFWith(jpeg []byte) string {
	doc := testNewDoc()
	NewImage(doc.pdf, bytes.NewReader(jpeg)).DrawOn(NewPage(doc.pdf, letter.Portrait()))
	return string(doc.complete())
}

func TestJPGImageTheInksOfACmykJpegAreInvertedBackOnlyWhenAdobeSoftwareWroteIt(t *testing.T) {
	jpeg, err := os.ReadFile(testRepoPath(t, "images/cmyk.jpg"))
	if err != nil {
		t.Skip("the images directory is not here")
	}
	image, err := newJPGImage(bytes.NewReader(jpeg))
	if err != nil || !image.isAdobe() {
		t.Errorf("the Adobe marker is not found: %v", err)
	}
	if !strings.Contains(testPDFWith(jpeg), "/Decode [1.0 0.0 1.0 0.0 1.0 0.0 1.0 0.0]") {
		t.Error("the inks are not inverted back")
	}

	plain := testWithoutAPP14(jpeg)
	image, err = newJPGImage(bytes.NewReader(plain))
	if err != nil || image.isAdobe() {
		t.Errorf("an Adobe marker is found without one: %v", err)
	}
	raw := testPDFWith(plain)
	if !strings.Contains(raw, "/DeviceCMYK") || strings.Contains(raw, "/Decode [") {
		t.Error("an image without the marker has its inks inverted")
	}
}

// The frame header of the JPEG is what PDFjet reads it for, so a marker
// before it that is read as one more segment hides it.

// testJPEG returns a JPEG of 8 by 8 pixels: the SOI marker, the bytes given,
// and a frame header of the number of color components.
func testJPEG(before []byte, components int) []byte {
	jpeg := append([]byte{0xFF, 0xD8}, before...)
	length := 8 + 3*components
	jpeg = append(jpeg, 0xFF, 0xC0, byte(length>>8), byte(length), 8, 0, 8, 0, 8, byte(components))
	for i := 0; i < components; i++ {
		jpeg = append(jpeg, byte(i+1), 0x11, 0)
	}
	return jpeg
}

// testAPP14 returns an APP14 segment of the bytes.
func testAPP14(payload ...byte) []byte {
	return append([]byte{0xFF, 0xEE, byte((len(payload) + 2) >> 8), byte(len(payload) + 2)}, payload...)
}

// testAdobeAPP14 is the segment Adobe software writes: "Adobe", the version,
// two flags and the color transform.
var testAdobeAPP14 = testAPP14('A', 'd', 'o', 'b', 'e', 0, 100, 0, 0, 0, 0, 2)

func TestJPGImageTheMarkersWithoutAParameterSegmentAreNotReadAsSegments(t *testing.T) {
	for name, before := range map[string][]byte{
		"a stuffed 0xFF byte": {0xFF, 0x00, 0x30},
		"a restart marker":    {0xFF, 0xD3},
		"a TEM marker":        {0xFF, 0x01},
		"a nested SOI marker": {0xFF, 0xD8},
		"a fill byte":         {0xFF, 0xFF},
	} {
		image, err := newJPGImage(bytes.NewReader(testJPEG(before, 3)))
		if err != nil {
			t.Errorf("%s hides the frame header: %v", name, err)
		} else if image.getWidth() != 8 || image.getHeight() != 8 || image.getColorComponents() != 3 {
			t.Errorf("%s: %g by %g pixels of %d components, not 8 by 8 of 3",
				name, image.getWidth(), image.getHeight(), image.getColorComponents())
		}
	}
}

func TestJPGImageAJPEGThatEndsBeforeItsFrameHeaderFails(t *testing.T) {
	if _, err := newJPGImage(bytes.NewReader([]byte{0xFF, 0xD8, 0xFF, 0xD9})); err == nil {
		t.Error("a JPEG of no frame header is read")
	}
}

func TestJPGImageAJPEGOfOtherThanEightBitsPerComponentFails(t *testing.T) {
	// A PDF image stream of DCTDecode data delivers eight bit samples, and
	// PDFjet writes 8 as the bits per component, so a 12-bit JPEG would be
	// drawn as noise rather than refused.
	for _, precision := range []byte{0, 12, 16} {
		jpeg := testJPEG(nil, 3)
		jpeg[6] = precision // After the SOI marker, the SOF0 marker and the length
		if _, err := newJPGImage(bytes.NewReader(jpeg)); err == nil {
			t.Errorf("a JPEG of %d bits per color component is read", precision)
		}
	}
	if _, err := newJPGImage(bytes.NewReader(testJPEG(nil, 3))); err != nil {
		t.Errorf("a JPEG of 8 bits per color component fails: %v", err)
	}
}

func TestJPGImageAFrameHeaderOfTheWrongLengthFails(t *testing.T) {
	// The frame header holds three bytes for each component after its eight,
	// which libjpeg checks; a reader a PDF is drawn with refuses an image of
	// another length, where MuPDF draws nothing and says "Bogus marker
	// length", so PDFjet does not embed one.
	for _, wrong := range []byte{17 - 3, 17 + 3} {
		jpeg := testJPEG(nil, 3)
		jpeg[5] = wrong // The low byte of the length of the frame header
		if _, err := newJPGImage(bytes.NewReader(jpeg)); err == nil {
			t.Errorf("a frame header of %d bytes, not 17, is read", wrong)
		}
	}
	if _, err := newJPGImage(bytes.NewReader(testJPEG(nil, 3))); err != nil {
		t.Errorf("a frame header of 17 bytes fails: %v", err)
	}
}

func TestJPGImageOnlyTheWholeAdobeAPP14SegmentMarksTheImage(t *testing.T) {
	image, err := newJPGImage(bytes.NewReader(testJPEG(testAdobeAPP14, 4)))
	if err != nil || !image.isAdobe() {
		t.Errorf("the Adobe segment does not mark the image: %v", err)
	}

	// Adobe's segment is twelve bytes; a shorter one is another APP14.
	image, err = newJPGImage(bytes.NewReader(testJPEG(testAPP14('A', 'd', 'o', 'b', 'e'), 4)))
	if err != nil || image.isAdobe() {
		t.Errorf("a segment of five bytes marks the image: %v", err)
	}
}

func TestJPGImageTheAdobeAPP14SegmentIsFoundAfterTheFrameHeader(t *testing.T) {
	// libjpeg reads the markers of a header to the scan, so an APP14 segment
	// between the frame header and the scan marks the image too; one after
	// the scan, which no header reads, does not.
	image, err := newJPGImage(bytes.NewReader(append(testJPEG(nil, 4), testAdobeAPP14...)))
	if err != nil || !image.isAdobe() {
		t.Errorf("the Adobe segment after the frame header does not mark the image: %v", err)
	}

	afterTheScan := append([]byte{0xFF, 0xDA, 0x00, 0x02}, testAdobeAPP14...)
	image, err = newJPGImage(bytes.NewReader(append(testJPEG(nil, 4), afterTheScan...)))
	if err != nil || image.isAdobe() {
		t.Errorf("the Adobe segment after the scan marks the image: %v", err)
	}
}

func TestJPGImageTheComponentsOfTheFrameHeaderAreNotReadAsMarkers(t *testing.T) {
	// The component specifications fill the rest of the frame header and can
	// hold any bytes, the 0xFF of a marker among them; here they are the
	// bytes of an Adobe APP14 segment, which is none.
	jpeg := testJPEG(nil, 4)
	copy(jpeg[len(jpeg)-12:], []byte{0xFF, 0xEE, 0x00, 0x0E, 'A', 'd', 'o', 'b', 'e', 0x00, 0x64, 0x00})
	jpeg = append(jpeg, 0, 0, 0, 0, 0xFF, 0xD9)
	image, err := newJPGImage(bytes.NewReader(jpeg))
	if err != nil || image.isAdobe() {
		t.Errorf("the components of the frame header marked the image: %v", err)
	}
}

func TestJPGImageAnotherAPP14SegmentDoesNotUnmarkAnAdobeImage(t *testing.T) {
	before := append(append([]byte(nil), testAdobeAPP14...),
		testAPP14('N', 'o', 't', ' ', 'A', 'd', 'o', 'b', 'e', 0, 0, 0)...)
	image, err := newJPGImage(bytes.NewReader(testJPEG(before, 4)))
	if err != nil || !image.isAdobe() {
		t.Errorf("the APP14 segment after Adobe's unmarks the image: %v", err)
	}
}
