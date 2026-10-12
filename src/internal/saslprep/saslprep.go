// saslprep.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

// Package saslprep prepares a password as SASLprep, RFC 4013, does, which
// PDF 2.0 asks of the password of a document encrypted with AES-256
// (ISO 32000-2, 7.6.4.3.3, algorithm 2.A): the non-ASCII spaces mapped to a
// space, the characters "commonly mapped to nothing" left out, the text in
// normalization form KC, and the prohibited characters and a mix of right to
// left and left to right text refused. The password is a query in the words
// of stringprep, RFC 3454: a character Unicode 3.2 did not assign is kept.
// The tables are those of Unicode 3.2, which both RFCs fix, in tables.go.
package saslprep

import (
	"errors"
	"fmt"
	"sort"
	"unicode/utf8"
)

// Prepare returns the password prepared, or an error that names the
// character that SASLprep prohibits, or that says its text mixes right to
// left with left to right.
func Prepare(password string) (string, error) {
	mapped := make([]rune, 0, len(password))
	for _, c := range password {
		if inRanges(space, c) {
			mapped = append(mapped, ' ')
		} else if !inRanges(nothing, c) {
			mapped = append(mapped, c)
		}
	}
	prepared := nfkc(mapped)
	hasRandAL, hasL := false, false
	for _, c := range prepared {
		if inRanges(prohibited, c) {
			return "", fmt.Errorf("The password has the character U+%04X, which PDF 2.0 does not allow in one.", c)
		}
		hasRandAL = hasRandAL || inRanges(randal, c)
		hasL = hasL || inRanges(lcat, c)
	}
	// Right to left text has no left to right character, and starts and ends
	// with a right to left one (RFC 3454, 6).
	if hasRandAL && (hasL || !inRanges(randal, prepared[0]) || !inRanges(randal, prepared[len(prepared)-1])) {
		return "", errors.New("The password mixes right to left with left to right text, which PDF 2.0 does not allow in one.")
	}
	return string(prepared), nil
}

// PasswordBytes returns the bytes of the password as PDF 2.0 asks of
// AES-256: prepared, in UTF-8, at most 127 of them.
func PasswordBytes(password string) ([]byte, error) {
	prepared, err := Prepare(password)
	if err != nil {
		return nil, err
	}
	return Truncate(prepared), nil
}

// Truncate returns the bytes of the password, at most 127, as PDF 2.0 asks,
// cut before a character that would not fit whole.
func Truncate(password string) []byte {
	end := 0
	for i, c := range password {
		if i+utf8.RuneLen(c) > 127 {
			break
		}
		end = i + utf8.RuneLen(c)
	}
	return []byte(password[:end])
}

// inRanges returns true if the character is in one of the ranges, first and
// last, in order.
func inRanges(ranges []int32, c rune) bool {
	i := sort.Search(len(ranges)/2, func(i int) bool { return ranges[2*i+1] >= c })
	return i < len(ranges)/2 && ranges[2*i] <= c
}

// The Hangul syllables, which decompose and compose by arithmetic.
const (
	sBase  = 0xAC00
	lBase  = 0x1100
	vBase  = 0x1161
	tBase  = 0x11A7
	lCount = 19
	vCount = 21
	tCount = 28
	nCount = vCount * tCount
	sCount = lCount * nCount
)

// nfkc returns the characters in normalization form KC: decomposed, the
// combining marks of each run put in the order of their classes, and
// composed again, as UAX #15 of Unicode 3.2 says.
func nfkc(chars []rune) []rune {
	decomposed := make([]rune, 0, len(chars))
	for _, c := range chars {
		decomposed = appendDecomposition(decomposed, c)
	}
	// The marks of each run of non-zero classes, in the order of their
	// classes, the marks of one class kept in the order they are.
	for i := 0; i < len(decomposed); {
		if combiningClass(decomposed[i]) == 0 {
			i++
			continue
		}
		j := i
		for j < len(decomposed) && combiningClass(decomposed[j]) != 0 {
			j++
		}
		run := decomposed[i:j]
		sort.SliceStable(run, func(a, b int) bool { return combiningClass(run[a]) < combiningClass(run[b]) })
		i = j
	}
	return compose(decomposed)
}

// appendDecomposition appends the full compatibility decomposition of the
// character.
func appendDecomposition(out []rune, c rune) []rune {
	if c >= sBase && c < sBase+sCount {
		s := c - sBase
		out = append(out, lBase+s/nCount, vBase+(s%nCount)/tCount)
		if t := s % tCount; t != 0 {
			out = append(out, tBase+t)
		}
		return out
	}
	i := sort.Search(len(decompCodes), func(i int) bool { return decompCodes[i] >= c })
	if i < len(decompCodes) && decompCodes[i] == c {
		for _, d := range decompValues[decompStarts[i]:decompStarts[i+1]] {
			out = append(out, d)
		}
		return out
	}
	return append(out, c)
}

// combiningClass returns the canonical combining class of the character.
func combiningClass(c rune) int32 {
	i := sort.Search(len(ccc)/2, func(i int) bool { return ccc[2*i] >= c })
	if i < len(ccc)/2 && ccc[2*i] == c {
		return ccc[2*i+1]
	}
	return 0
}

// composeTo returns what the pair composes to, or -1.
func composeTo(first, second rune) rune {
	if first >= lBase && first < lBase+lCount && second >= vBase && second < vBase+vCount {
		return sBase + ((first-lBase)*vCount+(second-vBase))*tCount
	}
	if first >= sBase && first < sBase+sCount && (first-sBase)%tCount == 0 &&
		second > tBase && second < tBase+tCount {
		return first + (second - tBase)
	}
	key := int64(first)*0x110000 + int64(second)
	i := sort.Search(len(composeKeys), func(i int) bool { return composeKeys[i] >= key })
	if i < len(composeKeys) && composeKeys[i] == key {
		return composeValues[i]
	}
	return -1
}

// compose composes the decomposed characters, each with the last starter
// before it when nothing between them blocks it, as the sample code of UAX #15
// does: a mark is blocked by a mark of its class or a higher one before it.
func compose(chars []rune) []rune {
	if len(chars) == 0 {
		return chars
	}
	out := []rune{chars[0]}
	starter := 0 // The index in out of the last starter
	lastClass := combiningClass(chars[0])
	if lastClass != 0 {
		lastClass = 256 // A mark first has no starter to compose with
	}
	for _, c := range chars[1:] {
		class := combiningClass(c)
		if composite := composeTo(out[starter], c); composite != -1 && (lastClass < class || lastClass == 0) {
			out[starter] = composite
			continue
		}
		if class == 0 {
			starter = len(out)
		}
		lastClass = class
		out = append(out, c)
	}
	return out
}
