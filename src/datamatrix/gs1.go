// gs1.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package datamatrix

import (
	"strconv"
	"strings"
)

// gs1Separator is GS, which ends a field of no set length when another follows.
const gs1Separator = "\x1d"

// gs1PredefinedLengths are the lengths, of the Application Identifier and its
// data together, of the fields that the first two digits of their Application
// Identifier give a set length, as the GS1 General Specifications list them.
// Their data is digits, and they need no separator after them.
var gs1PredefinedLengths = map[string]int{
	"00": 20, "01": 16, "02": 16, "03": 16, "04": 18,
	"11": 8, "12": 8, "13": 8, "14": 8, "15": 8, "16": 8, "17": 8, "18": 8, "19": 8,
	"20": 4, "31": 10, "32": 10, "33": 10, "34": 10, "35": 10, "36": 10, "41": 16,
}

// gs1Characters are the characters of the data of a field, GS1's character
// set 82, less the parentheses, which enclose the Application Identifiers.
const gs1Characters = "!\"%&'*+,-./0123456789:;<=>?ABCDEFGHIJKLMNOPQRSTUVWXYZ_abcdefghijklmnopqrstuvwxyz"

// gs1ElementString returns the element string of the GS1 data written as
// people read it: the Application Identifiers without their parentheses, each
// followed by its data, and GS after a field of no set length that another
// follows. It panics if the data is not GS1; see NewGS1DataMatrix.
func gs1ElementString(str string) string {
	if !strings.HasPrefix(str, "(") {
		panic("GS1 data is Application Identifiers in parentheses, each followed by its data, such as (01)09506000134352(17)261231!")
	}
	var sb strings.Builder
	rest := str
	for rest != "" {
		end := strings.IndexByte(rest, ')')
		if end < 0 {
			panic("GS1 data is Application Identifiers in parentheses, each followed by its data, such as (01)09506000134352(17)261231!")
		}
		ai := rest[1:end]
		rest = rest[end+1:]
		next := strings.IndexByte(rest, '(')
		if next < 0 {
			next = len(rest)
		}
		data := rest[:next]
		rest = rest[next:]
		checkGS1Field(ai, data)

		sb.WriteString(ai)
		sb.WriteString(data)
		if _, set := gs1PredefinedLengths[ai[:2]]; !set && rest != "" {
			sb.WriteString(gs1Separator)
		}
	}
	return sb.String()
}

// checkGS1Field panics if the field is not one GS1 allows.
func checkGS1Field(ai, data string) {
	if len(ai) < 2 || len(ai) > 4 || !gs1Digits(ai) {
		panic("The Application Identifier (" + ai + ") is not two to four digits!")
	}
	if data == "" {
		panic("The Application Identifier (" + ai + ") has no data!")
	}
	for i := 0; i < len(data); i++ {
		if strings.IndexByte(gs1Characters, data[i]) < 0 {
			panic("The data of (" + ai + ") has a character that GS1 does not allow!")
		}
	}
	if len(data) > 90 {
		panic("The data of (" + ai + ") is longer than 90 characters!")
	}
	if length, set := gs1PredefinedLengths[ai[:2]]; set && (len(ai)+len(data) != length || !gs1Digits(data)) {
		panic("The data of (" + ai + ") must be " + strconv.Itoa(length-len(ai)) + " digits!")
	}
	if (ai == "00" || ai == "01" || ai == "02" || (len(ai) == 3 && ai[:2] == "41" && ai[2] <= '7')) &&
		!gs1CheckDigitIsRight(data) {
		panic("The check digit of (" + ai + ") is wrong!")
	}
}

func gs1Digits(s string) bool {
	for i := 0; i < len(s); i++ {
		if s[i] < '0' || s[i] > '9' {
			return false
		}
	}
	return true
}

// gs1CheckDigitIsRight returns true if the last digit of the number is its
// check digit: the digits before it weighted 3 and 1 in turn from the right,
// and the check digit what takes their sum to a multiple of ten.
func gs1CheckDigitIsRight(number string) bool {
	sum := 0
	for i := len(number) - 2; i >= 0; i-- {
		digit := int(number[i] - '0')
		if (len(number)-2-i)%2 == 0 {
			digit *= 3
		}
		sum += digit
	}
	return int(number[len(number)-1]-'0') == (10-sum%10)%10
}
