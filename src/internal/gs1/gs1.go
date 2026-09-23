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

// digitalLinkKeys are the primary keys of GS1 Digital Link and their
// qualifiers, in the order the path has them: each list is a place in the
// path, and the qualifiers in it are alternatives.
var digitalLinkKeys = map[string][][]string{
	"00": {}, "01": {{"22"}, {"10"}, {"21"}}, "253": {}, "255": {}, "401": {}, "402": {},
	"414": {{"254", "7040"}}, "417": {{"7040"}}, "8003": {}, "8004": {},
	"8006": {{"22"}, {"10"}, {"21"}}, "8010": {{"8011"}}, "8013": {},
	"8017": {{"8019"}}, "8018": {{"8019"}},
}

// DigitalLink returns the GS1 Digital Link of the GS1 data at the domain, the
// web address an ordinary QR code carries: the domain, the primary key of the
// data and its qualifiers in the path, in the order of the standard, and the
// other fields in the query, in their order. A value is percent-encoded where
// a web address needs it. It panics if the domain does not start with https://
// or http://, if the data is not GS1, see Parse, or if it has no primary key or
// two, or two qualifiers that are alternatives.
func DigitalLink(domain, str string) string {
	rest := strings.TrimPrefix(strings.TrimPrefix(domain, "https://"), "http://")
	if rest == domain || rest == "" || rest == "/" {
		panic("The domain of a GS1 Digital Link starts with https:// or http://, such as https://id.gs1.org!")
	}
	fields := Parse(str)
	key := -1
	for i, field := range fields {
		if _, ok := digitalLinkKeys[field.AI]; ok {
			if key >= 0 {
				panic("A GS1 Digital Link has one primary key, not (" + fields[key].AI + ") and (" + field.AI + ")!")
			}
			key = i
		}
	}
	if key < 0 {
		panic("A GS1 Digital Link needs a primary key, such as a GTIN (01), an SSCC (00) or a GLN (414)!")
	}

	var sb strings.Builder
	sb.WriteString(strings.TrimSuffix(domain, "/"))
	sb.WriteString("/" + fields[key].AI + "/" + percentEncode(fields[key].Data))
	used := map[int]bool{key: true}
	for _, place := range digitalLinkKeys[fields[key].AI] {
		found := -1
		for i, field := range fields {
			for _, qualifier := range place {
				if field.AI != qualifier {
					continue
				}
				if found >= 0 {
					panic("The qualifiers (" + fields[found].AI + ") and (" + field.AI + ") of (" +
						fields[key].AI + ") cannot be together!")
				}
				found = i
			}
		}
		if found >= 0 {
			sb.WriteString("/" + fields[found].AI + "/" + percentEncode(fields[found].Data))
			used[found] = true
		}
	}
	separator := "?"
	for i, field := range fields {
		if !used[i] {
			sb.WriteString(separator + field.AI + "=" + percentEncode(field.Data))
			separator = "&"
		}
	}
	return sb.String()
}

// percentEncode returns the value with each character but the unreserved ones
// of a web address, the letters, the digits and "-._~", as % and its two
// hexadecimal digits.
func percentEncode(value string) string {
	const hex = "0123456789ABCDEF"
	var sb strings.Builder
	for i := 0; i < len(value); i++ {
		c := value[i]
		if c >= 'A' && c <= 'Z' || c >= 'a' && c <= 'z' || c >= '0' && c <= '9' || strings.IndexByte("-._~", c) >= 0 {
			sb.WriteByte(c)
		} else {
			sb.WriteByte('%')
			sb.WriteByte(hex[c>>4])
			sb.WriteByte(hex[c&15])
		}
	}
	return sb.String()
}
