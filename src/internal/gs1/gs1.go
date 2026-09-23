// gs1.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

// Package gs1 reads GS1 data as people write it, each Application
// Identifier in parentheses and its data after it, for the barcodes that
// carry it: GS1 DataMatrix and GS1-128.
package gs1

import (
	"strconv"
	"strings"
)

// predefinedLengths are the lengths, of the Application Identifier and its
// data together, of the fields that the first two digits of their Application
// Identifier give a set length, as the GS1 General Specifications list them.
// Their data is digits, and they need no separator after them.
var predefinedLengths = map[string]int{
	"00": 20, "01": 16, "02": 16, "03": 16, "04": 18,
	"11": 8, "12": 8, "13": 8, "14": 8, "15": 8, "16": 8, "17": 8, "18": 8, "19": 8,
	"20": 4, "31": 10, "32": 10, "33": 10, "34": 10, "35": 10, "36": 10, "41": 16,
}

// characters are the characters of the data of a field, GS1's character
// set 82, less the parentheses, which enclose the Application Identifiers.
const characters = "!\"%&'*+,-./0123456789:;<=>?ABCDEFGHIJKLMNOPQRSTUVWXYZ_abcdefghijklmnopqrstuvwxyz"

// Field is an Application Identifier and its data, and whether a separator
// follows it in a barcode: after a field of no set length that another
// follows.
type Field struct {
	AI        string
	Data      string
	Separator bool
}

// Parse returns the fields of the GS1 data written as people read it. It
// panics if the data is not GS1: an Application Identifier that is not two to
// four digits, or with no data; a character GS1 does not allow, a parenthesis
// among them; data longer than 90 characters; data of a field of set length,
// such as the GTIN of (01) or the date of (17), that is not that many digits;
// or a wrong check digit of an SSCC (00), a GTIN (01) or (02), or a GLN (410)
// to (417).
func Parse(str string) []Field {
	if !strings.HasPrefix(str, "(") {
		panic("GS1 data is Application Identifiers in parentheses, each followed by its data, such as (01)09506000134352(17)261231!")
	}
	fields := make([]Field, 0)
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
		checkField(ai, data)
		_, set := predefinedLengths[ai[:2]]
		fields = append(fields, Field{AI: ai, Data: data, Separator: !set && rest != ""})
	}
	return fields
}

// checkField panics if the field is not one GS1 allows.
func checkField(ai, data string) {
	if len(ai) < 2 || len(ai) > 4 || !digits(ai) {
		panic("The Application Identifier (" + ai + ") is not two to four digits!")
	}
	if data == "" {
		panic("The Application Identifier (" + ai + ") has no data!")
	}
	for i := 0; i < len(data); i++ {
		if strings.IndexByte(characters, data[i]) < 0 {
			panic("The data of (" + ai + ") has a character that GS1 does not allow!")
		}
	}
	if len(data) > 90 {
		panic("The data of (" + ai + ") is longer than 90 characters!")
	}
	if length, set := predefinedLengths[ai[:2]]; set && (len(ai)+len(data) != length || !digits(data)) {
		panic("The data of (" + ai + ") must be " + strconv.Itoa(length-len(ai)) + " digits!")
	}
	if (ai == "00" || ai == "01" || ai == "02" || (len(ai) == 3 && ai[:2] == "41" && ai[2] <= '7')) &&
		!checkDigitIsRight(data) {
		panic("The check digit of (" + ai + ") is wrong!")
	}
}

func digits(s string) bool {
	for i := 0; i < len(s); i++ {
		if s[i] < '0' || s[i] > '9' {
			return false
		}
	}
	return true
}

// checkDigitIsRight returns true if the last digit of the number is its
// check digit: the digits before it weighted 3 and 1 in turn from the right,
// and the check digit what takes their sum to a multiple of ten.
func checkDigitIsRight(number string) bool {
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
