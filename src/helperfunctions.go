package pdfjet

/**
 * helperfunctions.go
 *
©2025 PDFjet Software

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

import (
	"fmt"
	"io"
	"strconv"
)

// insertStringAt inserts the string s1 into a1 at the specified index
func insertStringAt(a1 []string, s1 string, index int) []string {
	a2 := make([]string, 0)
	a2 = append(a2, a1[:index]...)
	a2 = append(a2, s1)
	a2 = append(a2, a1[index:]...)
	return a2
}

// insertArrayAt inserts the array a2 into a1 at the specified index
func insertArrayAt(a1, a2 []string, index int) []string {
	a3 := make([]string, 0)
	a3 = append(a3, a1[:index]...)
	a3 = append(a3, a2...)
	a3 = append(a3, a1[index:]...)
	return a3
}

func appendInteger(a1 *[]byte, value int) {
	*a1 = append(*a1, []byte(strconv.Itoa(value))...)
}

func appendFloat32(a1 *[]byte, value float32) {
	*a1 = append(*a1, []byte(strconv.FormatFloat(float64(value), 'f', 3, 32))...)
}

func appendString(a1 *[]byte, s1 string) {
	*a1 = append(*a1, []byte(s1)...)
}

func appendByte(a1 *[]byte, b1 byte) {
	*a1 = append(*a1, b1)
}

func appendByteArray(a1 *[]byte, a2 []byte) {
	*a1 = append(*a1, a2...)
}

func appendByteArraySlice(a1 *[]byte, a2 []byte, offset, length int) {
	*a1 = append(*a1, a2[offset:offset+length]...)
}

func getUint8(r io.Reader) uint8 {
	buf := make([]byte, 1)
	io.ReadFull(r, buf)
	return buf[0]
}

func getUint16(r io.Reader) uint16 {
	buf := make([]byte, 2)
	io.ReadFull(r, buf)
	return uint16(buf[0])<<8 | uint16(buf[1])
}

func getUint24(r io.Reader) uint32 {
	buf := make([]byte, 3)
	io.ReadFull(r, buf)
	return uint32(buf[0])<<16 | uint32(buf[1])<<8 | uint32(buf[2])
}

func getUint32(r io.Reader) uint32 {
	buf := make([]byte, 4)
	io.ReadFull(r, buf)
	return uint32(buf[0])<<24 | uint32(buf[1])<<16 | uint32(buf[2])<<8 | uint32(buf[3])
}

func getInt16(r io.Reader) int16 {
	buf := make([]byte, 2)
	io.ReadFull(r, buf)
	return int16(buf[0])<<8 | int16(buf[1])
}

func getInt32(r io.Reader) int32 {
	buf := make([]byte, 4)
	io.ReadFull(r, buf)
	return int32(buf[0])<<24 | int32(buf[1])<<16 | int32(buf[2])<<8 | int32(buf[3])
}

func toHexString(code int) string {
	return fmt.Sprintf("%04X", code)
}

func skipNBytes(reader io.Reader, n int) {
	getNBytes(reader, n)
}

func getNBytes(r io.Reader, n int) []byte {
	buf := make([]byte, n)
	io.ReadFull(r, buf)
	return buf
}

func formatFloat32(value float32) []byte {
	return []byte(strconv.FormatFloat(float64(value), 'f', 3, 32))
}
