// helperfunctions.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"io"
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

func getInt32(r io.Reader) int32 {
	buf := make([]byte, 4)
	io.ReadFull(r, buf)
	return int32(buf[0])<<24 | int32(buf[1])<<16 | int32(buf[2])<<8 | int32(buf[3])
}

// toHexString formats code as 4 uppercase hex digits, zero-padded.
// Callers only ever pass 16-bit CIDs/GIDs (0 - 0xFFFF).
// This used to go through fmt.Sprintf("%04X", code), which is fine
// for occasional use but far too slow when called tens of thousands
// of times while building a CJK font's ToUnicode CMap.
func toHexString(code int) string {
	b := [4]byte{
		hexDigits[(code>>12)&0xF],
		hexDigits[(code>>8)&0xF],
		hexDigits[(code>>4)&0xF],
		hexDigits[code&0xF],
	}
	return string(b[:])
}

func skipNBytes(reader io.Reader, n int) {
	getNBytes(reader, n)
}

func getNBytes(r io.Reader, n int) []byte {
	buf := make([]byte, n)
	io.ReadFull(r, buf)
	return buf
}
