// decryptor.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"bytes"
	"crypto/aes"
	"crypto/cipher"
	"crypto/md5"
	"crypto/rc4"
	"crypto/sha256"
	"crypto/sha512"
	"encoding/hex"
	"errors"
	"fmt"
	"slices"
	"strconv"
	"strings"

	"github.com/edragoev1/pdfjet/src/decompressor"
)

var passwordPadding = []byte{
	0x28, 0xBF, 0x4E, 0x5E, 0x4E, 0x75, 0x8A, 0x41,
	0x64, 0x00, 0x4E, 0x56, 0xFF, 0xFA, 0x01, 0x08,
	0x2E, 0x2E, 0x00, 0xB6, 0xD0, 0x68, 0x3E, 0x80,
	0x2F, 0x0C, 0xA9, 0xFE, 0x64, 0x53, 0x69, 0x7A,
}

// The methods of the crypt filters.
const (
	cryptNone = iota
	cryptRC4
	cryptAES128
	cryptAES256
)

// decryptor decrypts the strings and streams of a PDF that is encrypted with
// the standard security handler and an empty user password, like PDFs that
// open without a password but restrict printing or copying. RC4 and AES-128
// (revisions 2 to 4) and AES-256 (revisions 5 and 6) are supported.
type decryptor struct {
	objNumber       int // The object number of the encryption dictionary
	key             []byte
	streamMethod    int
	stringMethod    int
	encryptMetadata bool
}

// getDecryptor returns the decryptor of the PDF, or nil when it is not
// encrypted. The trailer is the cross-reference stream object when there is
// no trailer.
func getDecryptor(trailer *PDFobj, objects []*PDFobj) (*decryptor, error) {
	if trailer == nil {
		return nil, nil
	}
	i := slices.Index(trailer.dict, "/Encrypt")
	if i == -1 || i+1 >= len(trailer.dict) {
		return nil, nil
	}
	var encrypt *PDFobj
	if trailer.dict[i+1] == "<<" {
		// The dictionary is in the trailer, so there is no object to skip.
		encrypt = NewPDFobj()
		encrypt.number = -1
		level := 0
		for j := i + 1; j < len(trailer.dict); j++ {
			token := trailer.dict[j]
			encrypt.dict = append(encrypt.dict, token)
			if token == "<<" {
				level++
			} else if token == ">>" {
				level--
				if level == 0 {
					break
				}
			}
		}
	} else {
		for _, obj := range objects {
			if strconv.Itoa(obj.number) == trailer.dict[i+1] {
				encrypt = obj // The last one is the newest.
			}
		}
	}
	if encrypt == nil {
		return nil, errors.New("The encryption dictionary of the PDF was not found.")
	}
	var id []byte
	i = slices.Index(trailer.dict, "/ID")
	if i != -1 && i+2 < len(trailer.dict) && trailer.dict[i+1] == "[" {
		id = toBytes(trailer.dict[i+2])
	}
	return newDecryptor(encrypt, id)
}

func newDecryptor(encrypt *PDFobj, id []byte) (*decryptor, error) {
	d := &decryptor{objNumber: encrypt.number}
	if encrypt.getValue("/Filter") != "/Standard" {
		return nil, errors.New("The security handler of the PDF is not supported: " +
			encrypt.getValue("/Filter"))
	}
	v := getInt(encrypt, "/V")
	r := getInt(encrypt, "/R")
	d.encryptMetadata = encrypt.getValue("/EncryptMetadata") != "false"
	if v == 1 || v == 2 {
		d.streamMethod = cryptRC4
		d.stringMethod = cryptRC4
	} else if v == 4 || v == 5 {
		d.streamMethod = getCryptMethod(encrypt, encrypt.getValue("/StmF"))
		d.stringMethod = getCryptMethod(encrypt, encrypt.getValue("/StrF"))
	} else {
		return nil, fmt.Errorf("The encryption of the PDF is not supported: /V %d", v)
	}
	u := toBytes(encrypt.getValue("/U"))
	var err error
	if r == 5 || r == 6 {
		d.key, err = getAES256Key(r, u, toBytes(encrypt.getValue("/UE")))
	} else if r >= 2 && r <= 4 {
		length := getInt(encrypt, "/Length") / 8
		if v == 1 || r == 2 {
			length = 5
		} else if v == 4 {
			length = 16
		}
		d.key, err = getRC4Key(r, max(5, min(length, 16)),
			toBytes(encrypt.getValue("/O")), u, getInt(encrypt, "/P"), id, d.encryptMetadata)
	} else {
		return nil, fmt.Errorf("The encryption of the PDF is not supported: /R %d", r)
	}
	if err != nil {
		return nil, err
	}
	return d, nil
}

// getCryptMethod returns the method of the crypt filter with the name in the
// /CF dictionary.
func getCryptMethod(encrypt *PDFobj, name string) int {
	dict := encrypt.dict
	level := 0
	for i := slices.Index(dict, "/CF") + 1; i > 0 && i < len(dict); i++ {
		token := dict[i]
		if token == "<<" {
			level++
		} else if token == ">>" {
			level--
			if level == 0 {
				break
			}
		} else if level == 1 && token == name {
			for j := i + 1; j+1 < len(dict) && dict[j] != ">>"; j++ {
				if dict[j] == "/CFM" {
					switch dict[j+1] {
					case "/V2":
						return cryptRC4
					case "/AESV2":
						return cryptAES128
					case "/AESV3":
						return cryptAES256
					}
					break
				}
			}
			break
		}
	}
	return cryptNone // Like /Identity, the default.
}

// getRC4Key computes the key of revisions 2 to 4 from the password with
// algorithm 2 of ISO 32000-2, and checks the password against /U with
// algorithms 4 and 5.
func getRC4Key(r, length int, o, u []byte, p int, id []byte, encryptMetadata bool) ([]byte, error) {
	h := md5.New()
	h.Write(passwordPadding)
	h.Write(o)
	h.Write([]byte{byte(p), byte(p >> 8), byte(p >> 16), byte(p >> 24)})
	h.Write(id)
	if r >= 4 && !encryptMetadata {
		h.Write([]byte{0xff, 0xff, 0xff, 0xff})
	}
	hash := h.Sum(nil)
	if r >= 3 {
		for i := 0; i < 50; i++ {
			sum := md5.Sum(hash[:length])
			hash = sum[:]
		}
	}
	key := slices.Clone(hash[:length])
	var check []byte
	n := 16
	if r == 2 {
		check = rc4Crypt(key, passwordPadding)
		n = 32
	} else {
		sum := md5.Sum(append(slices.Clone(passwordPadding), id...))
		check = rc4Crypt(key, sum[:])
		for i := 1; i <= 19; i++ {
			k := make([]byte, length)
			for j := range k {
				k[j] = key[j] ^ byte(i)
			}
			check = rc4Crypt(k, check)
		}
	}
	if len(u) < n || !bytes.Equal(check[:n], u[:n]) {
		return nil, errors.New("The PDF can only be opened with a password, which is not supported.")
	}
	return key, nil
}

// getAES256Key gets the key of revisions 5 and 6 from /UE with algorithms
// 2.A and 11 of ISO 32000-2, after checking the password against /U.
func getAES256Key(r int, u, ue []byte) ([]byte, error) {
	if len(u) < 48 || len(ue) < 32 {
		return nil, errors.New("The encryption dictionary of the PDF is not valid.")
	}
	if !bytes.Equal(getHash(r, u[32:40]), u[:32]) {
		return nil, errors.New("The PDF can only be opened with a password, which is not supported.")
	}
	return aesDecrypt(getHash(r, u[40:48]), make([]byte, 16), ue[:32]), nil
}

// getHash returns the hash of the empty password and the salt, which is
// SHA-256 in revision 5, and algorithm 2.B of ISO 32000-2 in revision 6.
func getHash(r int, salt []byte) []byte {
	sum := sha256.Sum256(salt)
	k := sum[:]
	if r == 5 {
		return k
	}
	for round := 1; ; round++ {
		k1 := bytes.Repeat(k, 64)
		block, _ := aes.NewCipher(k[:16])
		e := make([]byte, len(k1))
		cipher.NewCBCEncrypter(block, k[16:32]).CryptBlocks(e, k1)
		total := 0
		for _, b := range e[:16] {
			total += int(b)
		}
		switch total % 3 {
		case 0:
			s := sha256.Sum256(e)
			k = s[:]
		case 1:
			s := sha512.Sum384(e)
			k = s[:]
		default:
			s := sha512.Sum512(e)
			k = s[:]
		}
		if round >= 64 && int(e[len(e)-1]) <= round-32 {
			break
		}
	}
	return k[:32]
}

// decryptStrings decrypts the strings in the dictionary of the object, which
// become hexadecimal strings.
func (d *decryptor) decryptStrings(obj *PDFobj) {
	if d.stringMethod == cryptNone {
		return
	}
	for i, token := range obj.dict {
		if strings.HasPrefix(token, "(") || (strings.HasPrefix(token, "<") && token != "<<") {
			obj.dict[i] = "<" + hex.EncodeToString(d.decrypt(toBytes(token), d.stringMethod, obj)) + ">"
		}
	}
}

// decryptStream returns the decrypted stream of the object.
func (d *decryptor) decryptStream(obj *PDFobj, stream []byte) []byte {
	if !d.encryptMetadata && obj.getValue("/Type") == "/Metadata" {
		return stream
	}
	return d.decrypt(stream, d.streamMethod, obj)
}

// decrypt decrypts with a key for each object in revisions 2 to 4, which is
// algorithm 1 of ISO 32000-2, and with the file key in revisions 5 and 6.
func (d *decryptor) decrypt(data []byte, method int, obj *PDFobj) []byte {
	if method == cryptNone {
		return data
	}
	objectKey := d.key
	if method != cryptAES256 {
		number := obj.number
		generation := 0
		if len(obj.dict) > 1 {
			if value, err := strconv.Atoi(obj.dict[1]); err == nil {
				generation = value
			}
		}
		h := md5.New()
		h.Write(d.key)
		h.Write([]byte{byte(number), byte(number >> 8), byte(number >> 16),
			byte(generation), byte(generation >> 8)})
		if method == cryptAES128 {
			h.Write([]byte("sAlT"))
		}
		objectKey = h.Sum(nil)[:min(len(d.key)+5, 16)]
	}
	if method == cryptRC4 {
		return rc4Crypt(objectKey, data)
	}
	// The data starts with the initialization vector, and is padded to whole blocks.
	if len(data) < 32 {
		return []byte{}
	}
	decrypted := aesDecrypt(objectKey, data[:16], data[16:16+(len(data)-16)/16*16])
	padding := int(decrypted[len(decrypted)-1])
	if padding >= 1 && padding <= 16 {
		return decrypted[:len(decrypted)-padding]
	}
	return decrypted
}

func aesDecrypt(key, iv, data []byte) []byte {
	block, err := aes.NewCipher(key)
	if err != nil {
		return nil
	}
	decrypted := make([]byte, len(data))
	cipher.NewCBCDecrypter(block, iv).CryptBlocks(decrypted, data)
	return decrypted
}

func rc4Crypt(key, data []byte) []byte {
	c, err := rc4.NewCipher(key)
	if err != nil {
		return data
	}
	result := make([]byte, len(data))
	c.XORKeyStream(result, data)
	return result
}

// getInt returns the integer value of the key, or 0. /P can be written as an
// unsigned number.
func getInt(obj *PDFobj, key string) int {
	value, err := strconv.ParseInt(obj.getValue(key), 10, 64)
	if err != nil {
		return 0
	}
	return int(int32(value))
}

// toBytes returns the bytes of a literal string like (a\)b) or of a
// hexadecimal string like <612962>.
func toBytes(token string) []byte {
	result := make([]byte, 0, len(token))
	if strings.HasPrefix(token, "<") {
		high := -1
		for i := 1; i < len(token); i++ {
			digit := decompressor.HexValue(int(token[i]))
			if digit == -1 {
				continue
			}
			if high == -1 {
				high = digit
			} else {
				result = append(result, byte(high<<4|digit))
				high = -1
			}
		}
		if high != -1 {
			result = append(result, byte(high<<4))
		}
	} else if strings.HasPrefix(token, "(") {
		end := len(token)
		if strings.HasSuffix(token, ")") {
			end--
		}
		for i := 1; i < end; i++ {
			c := token[i]
			if c == '\\' && i+1 < end {
				i++
				c = token[i]
				if k := strings.IndexByte("nrtbf", c); k != -1 {
					result = append(result, "\n\r\t\b\f"[k])
				} else if c >= '0' && c <= '7' {
					value := int(c - '0')
					for n := 1; n < 3 && i+1 < end && token[i+1] >= '0' && token[i+1] <= '7'; n++ {
						i++
						value = value*8 + int(token[i]-'0')
					}
					result = append(result, byte(value))
				} else if c == '\r' {
					// A backslash at the end of a line continues the string.
					if i+1 < end && token[i+1] == '\n' {
						i++
					}
				} else if c != '\n' {
					result = append(result, c) // Like \( \) and \\
				}
			} else if c == '\r' {
				// An end of line in a string is a line feed.
				if i+1 < end && token[i+1] == '\n' {
					i++
				}
				result = append(result, '\n')
			} else {
				result = append(result, c)
			}
		}
	}
	return result
}
