// jpgimage_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"bytes"
	"errors"
	"math"
	"os"
	"path/filepath"
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
// a frame header of the number of color components, and a scan of them and
// the EOI marker, which a JPEG cut short has not.
func testJPEG(before []byte, components int) []byte {
	jpeg := append([]byte{0xFF, 0xD8}, before...)
	length := 8 + 3*components
	jpeg = append(jpeg, 0xFF, 0xC0, byte(length>>8), byte(length), 8, 0, 8, 0, 8, byte(components))
	for i := 0; i < components; i++ {
		jpeg = append(jpeg, byte(i+1), 0x11, 0)
	}
	return append(jpeg, testScan(components)...)
}

// testScan returns a scan of the color components, its header and two bytes
// of data, and the EOI marker.
func testScan(components int) []byte {
	length := 6 + 2*components
	scan := []byte{0xFF, 0xDA, byte(length >> 8), byte(length), byte(components)}
	for i := 0; i < components; i++ {
		scan = append(scan, byte(i+1), 0)
	}
	return append(scan, 0, 63, 0, 0x12, 0x34, 0xFF, 0xD9)
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
	jpeg := testJPEG(nil, 4)
	header := jpeg[:len(jpeg)-len(testScan(4))]
	between := append(append(append([]byte(nil), header...), testAdobeAPP14...), testScan(4)...)
	image, err := newJPGImage(bytes.NewReader(between))
	if err != nil || !image.isAdobe() {
		t.Errorf("the Adobe segment after the frame header does not mark the image: %v", err)
	}

	afterTheScan := append(append([]byte(nil), header...), 0xFF, 0xDA, 0x00, 0x02)
	afterTheScan = append(append(afterTheScan, testAdobeAPP14...), 0xFF, 0xD9)
	image, err = newJPGImage(bytes.NewReader(afterTheScan))
	if err != nil || image.isAdobe() {
		t.Errorf("the Adobe segment after the scan marks the image: %v", err)
	}
}

func TestJPGImageTheComponentsOfTheFrameHeaderAreNotReadAsMarkers(t *testing.T) {
	// The component specifications fill the rest of the frame header and can
	// hold any bytes, the 0xFF of a marker among them; here they are the
	// bytes of an Adobe APP14 segment, which is none.
	jpeg := testJPEG(nil, 4)
	scan := len(testScan(4))
	copy(jpeg[len(jpeg)-scan-12:], []byte{0xFF, 0xEE, 0x00, 0x0E, 'A', 'd', 'o', 'b', 'e', 0x00, 0x64, 0x00})
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

func TestJPGImageTheJfifDensityGivesTheSizeTheImageIsDrawnAt(t *testing.T) {
	// The JFIF segment of a JPEG holds the pixel density and the unit it is
	// in, and says how large the image is meant to be. cmyk.jpg is 1200 by
	// 800 pixels at 300 dots per inch, which is 288 by 192 points.
	path := testRepoPath(t, "images/cmyk.jpg")
	file, err := os.Open(path)
	if err != nil {
		t.Fatal(err)
	}
	jpg, err := newJPGImage(file)
	file.Close()
	if err != nil {
		t.Fatal(err)
	}
	if jpg.getWidth() != 1200 || jpg.getHeight() != 800 {
		t.Fatalf("pixels %v x %v", jpg.getWidth(), jpg.getHeight())
	}
	if math.Abs(float64(jpg.GetPhysicalWidth())-288.0) > 0.01 ||
		math.Abs(float64(jpg.GetPhysicalHeight())-192.0) > 0.01 {
		t.Errorf("physical size %v x %v", jpg.GetPhysicalWidth(), jpg.GetPhysicalHeight())
	}
	file, err = os.Open(path)
	if err != nil {
		t.Fatal(err)
	}
	defer file.Close()
	image := NewImage(testNewPDF(), file)
	if math.Abs(float64(image.GetWidth())-288.0) > 0.01 ||
		math.Abs(float64(image.GetHeight())-192.0) > 0.01 {
		t.Errorf("the image is drawn %v x %v, not the size it asks for",
			image.GetWidth(), image.GetHeight())
	}
}

func TestJPGImageAJfifDensityThatGivesNoSizeIsPassedOver(t *testing.T) {
	// Unit 0 of the JFIF segment is the ratio of the two axes and says
	// nothing about how large the image is, and a JPEG with no JFIF segment
	// at all gives no size either.
	for _, c := range []struct{ what, name string }{
		{"a ratio is not a size", "images/italy-admin.jpg"},
		{"there is no JFIF segment", "images/gr-map.jpg"},
	} {
		file, err := os.Open(testRepoPath(t, c.name))
		if err != nil {
			t.Fatal(err)
		}
		jpg, err := newJPGImage(file)
		file.Close()
		if err != nil {
			t.Fatal(err)
		}
		if jpg.GetPhysicalWidth() != 0.0 || jpg.GetPhysicalHeight() != 0.0 {
			t.Errorf("%s: physical size %v x %v", c.what,
				jpg.GetPhysicalWidth(), jpg.GetPhysicalHeight())
		}
	}
}

// A JPEG cut short, as in an upload or a copy, is refused: one that ends in
// its image data, or before it; not one whose thumbnail, in an APP1 segment
// before the scan, ends with its own EOI marker, nor one with data after
// its end, which cameras append (5 October 2026).
func TestJPGImageCutShortIsRefused(t *testing.T) {
	whole := testJPEG(nil, 3)
	header := whole[:len(whole)-len(testScan(3))]
	for name, jpeg := range map[string][]byte{
		"in its image data":   whole[:len(whole)-2],
		"before its scan":     header,
		"in its frame header": whole[:10],
	} {
		if _, err := newJPGImage(bytes.NewReader(jpeg)); err == nil {
			t.Errorf("cut short %s: taken", name)
		}
	}
	if _, err := newJPGImage(bytes.NewReader(whole[:len(whole)-2])); !errors.Is(err, errJPEGCutShort) {
		t.Errorf("cut short in its image data: %v", err)
	}
	// A thumbnail's EOI before the scan does not end the image
	thumbnail := []byte{0xFF, 0xE1, 0x00, 0x08, 'E', 'x', 'i', 'f', 0xFF, 0xD9}
	cut := testJPEG(thumbnail, 3)
	if _, err := newJPGImage(bytes.NewReader(cut[:len(cut)-2])); !errors.Is(err, errJPEGCutShort) {
		t.Errorf("cut short, a thumbnail's EOI before the scan: %v", err)
	}
	if _, err := newJPGImage(bytes.NewReader(cut)); err != nil {
		t.Errorf("whole, with a thumbnail's EOI before the scan: %v", err)
	}
	// Data after the end
	if _, err := newJPGImage(bytes.NewReader(append(append([]byte(nil), whole...), "trailer"...))); err != nil {
		t.Errorf("data after the end: %v", err)
	}
}

// Every JPEG of the examples is read, none taken for one cut short.
func TestJPGImageTheJPEGsOfTheExamplesAreWhole(t *testing.T) {
	paths, _ := filepath.Glob("../images/*.jp*g")
	more, _ := filepath.Glob("../images/*/*.jp*g")
	paths = append(paths, more...)
	if len(paths) == 0 {
		t.Skip("no JPEGs in ../images")
	}
	for _, path := range paths {
		data, err := os.ReadFile(path)
		if err != nil {
			t.Fatal(err)
		}
		if _, err := newJPGImage(bytes.NewReader(data)); err != nil {
			t.Errorf("%s: %v", filepath.Base(path), err)
		}
	}
	t.Logf("%d JPEGs read", len(paths))
}

// A JPEG whose only fault is a missing end-of-image marker, its scans whole,
// is drawn, the marker added; one that stops in its scan is still refused.
func TestJPGImageWithoutItsEndMarkerIsDrawnWhenItsScanIsWhole(t *testing.T) {
	for _, name := range []string{"restart.jpg", "orientation-1.jpg"} {
		data, err := os.ReadFile(testRepoPath(t, "tests/data/jpeg/"+name))
		if err != nil {
			t.Fatal(err)
		}
		if !bytes.HasSuffix(data, []byte{0xFF, 0xD9}) {
			t.Fatalf("%s: no end marker", name)
		}
		without := data[:len(data)-2]
		jpg, err := newJPGImage(bytes.NewReader(without))
		if err != nil {
			t.Errorf("%s without its end marker: %v", name, err)
			continue
		}
		if !bytes.Equal(jpg.getData(), data) {
			t.Errorf("%s without its end marker: the end marker is not added", name)
		}
		if pdf := testPDFWith(without); !strings.Contains(pdf, "/Subtype /Image") {
			t.Errorf("%s without its end marker: not drawn", name)
		}
		for _, cut := range []int{len(data) / 2, len(data) - 40} {
			if _, err := newJPGImage(bytes.NewReader(data[:cut])); !errors.Is(err, errJPEGCutShort) {
				t.Errorf("%s cut in its scan at %d of %d: %v", name, cut, len(data), err)
			}
		}
	}
}
