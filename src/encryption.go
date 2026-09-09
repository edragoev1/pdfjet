// Encryption.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"bytes"
	"crypto/rand"
	"crypto/sha256"
	"crypto/sha512"
	"encoding/binary"

	"github.com/edragoev1/pdfjet/src/encryption"
)

// User represents user password keys
type User struct {
	U  []byte
	UE []byte
}

// Owner represents owner password keys
type Owner struct {
	O  []byte
	OE []byte
}

// Encryption handles PDF encryption functionality
type Encryption struct {
	fileEncryptionKey []byte
	objNumber         int
	stream            *bytes.Buffer
}

// NewEncryption creates a new encryption dictionary and adds it to the PDF
func NewEncryption(pdf *PDF,
	passwords *encryption.Passwords, permissions *encryption.Permissions) (*Encryption, error) {
	enc := &Encryption{}

	// Generate a random 256-bit (32-byte) File Encryption Key
	enc.fileEncryptionKey = make([]byte, 32)
	if _, err := rand.Read(enc.fileEncryptionKey); err != nil {
		return nil, err
	}

	enc.stream = bytes.NewBuffer(make([]byte, 0, 32768)) // 32 KB buffer

	userPassword := passwords.UserPassword
	if len(userPassword) > 127 {
		userPassword = userPassword[:127]
	}
	ownerPassword := passwords.OwnerPassword
	if len(ownerPassword) > 127 {
		ownerPassword = ownerPassword[:127]
	}

	userPasswordBytes := []byte(userPassword)
	ownerPasswordBytes := []byte(ownerPassword)

	user, err := enc.computeUserKeys(userPasswordBytes)
	if err != nil {
		return nil, err
	}

	owner, err := enc.computeOwnerKeys(ownerPasswordBytes, user.U)
	if err != nil {
		return nil, err
	}

	// Zero out sensitive data
	clearBytes(userPasswordBytes)
	clearBytes(ownerPasswordBytes)

	// Encryption Dictionary
	pdf.newobj()
	pdf.appendString("<<\n") // Begin Dictionary
	pdf.appendString("/Filter /Standard\n")
	pdf.appendString("/V 5\n") // Algorithm 2.A / 2.B
	pdf.appendString("/R 6\n") // Security revision 6
	pdf.appendString("/CF <<\n")
	pdf.appendString("/StdCF <<\n")
	pdf.appendString("/CFM /AESV3\n") // AESV3 = AES-256 in CBC
	pdf.appendString("/Length 32\n")  // 32 bytes = 256-bit file key
	pdf.appendString("/AuthEvent /DocOpen\n")
	pdf.appendString(">>\n")
	pdf.appendString(">>\n")
	pdf.appendString("/StmF /StdCF\n")
	pdf.appendString("/StrF /StdCF\n")

	pdf.appendString("/U <") // User Key (U)
	pdf.appendString(byteArrayToHexString(user.U))
	pdf.appendString(">\n")

	pdf.appendString("/O <") // Owner Key (O)
	pdf.appendString(byteArrayToHexString(owner.O))
	pdf.appendString(">\n")

	pdf.appendString("/UE <") // User Encryption Key (UE)
	pdf.appendString(byteArrayToHexString(user.UE))
	pdf.appendString(">\n")

	pdf.appendString("/OE <") // Owner Encryption Key (OE)
	pdf.appendString(byteArrayToHexString(owner.OE))
	pdf.appendString(">\n")

	pdf.appendString("/EncryptMetadata false\n")

	// Permissions flags
	pdf.appendString("/P ")
	pdf.appendString(intToString(permissions.GetRawValue()))
	pdf.appendString("\n")

	// Create the unencrypted block per Algorithm 10
	perms := createUnencryptedPermsBlock(permissions.GetRawValue())
	perms[8] = 'F' // for EncryptMetadata false
	perms[9] = 'a'
	perms[10] = 'd'
	perms[11] = 'b'
	perms[12] = '-'
	perms[13] = '-'
	perms[14] = '-'
	perms[15] = '-'

	// Encrypt permissions block
	encryptedPermsBlock, err := encryption.EncryptECB(perms, enc.fileEncryptionKey)
	if err != nil {
		return nil, err
	}

	pdf.appendString("/Perms <")
	pdf.appendString(byteArrayToHexString(encryptedPermsBlock))
	pdf.appendString(">\n")

	pdf.appendString(">>\n")
	pdf.endobj()

	enc.objNumber = pdf.getObjNumber()
	return enc, nil
}

// GetKey returns the file encryption key
func (enc *Encryption) GetKey() []byte {
	key := make([]byte, len(enc.fileEncryptionKey))
	copy(key, enc.fileEncryptionKey)
	return key
}

// GetObjNumber returns the object number
func (enc *Encryption) GetObjNumber() int {
	return enc.objNumber
}

// computeHash computes a hash value based on the provided password, salt and user key U
func (enc *Encryption) computeHash(password, salt, U []byte) ([]byte, error) {
	// Take the SHA-256 hash of the original input
	K := sha256.Sum256(concatenate(password, salt, U))
	currentK := K[:]

	// Perform the following steps (a)-(d) 64 times or more:
	round := 0
	for {
		round++
		// a) Make a new string, K1, consisting of 64 repetitions
		K1, err := enc.computeK1(password, currentK, U)
		if err != nil {
			return nil, err
		}

		// b) Encrypt K1 with AES-128 (CBC, no padding)
		tempKey := make([]byte, 16)
		copy(tempKey, currentK[:16])
		tempIV := make([]byte, 16)
		copy(tempIV, currentK[16:32])

		E, err := encryption.EncryptK1(K1, tempKey, tempIV)
		if err != nil {
			return nil, err
		}

		// c) & d) Determine hash algorithm and compute hash
		algorithm := enc.nextHashAlgorithm(E)
		switch algorithm {
		case 0:
			hash := sha256.Sum256(E)
			currentK = hash[:]
		case 1:
			hash := sha512.Sum384(E)
			currentK = hash[:]
		case 2:
			hash := sha512.Sum512(E)
			currentK = hash[:]
		}

		// Termination check (For rounds 64+ only)
		if round >= 64 && E[len(E)-1] <= byte(round-32) {
			break
		}
	}

	finalOutput := make([]byte, 32)
	copy(finalOutput, currentK[:32])
	return finalOutput, nil
}

// computeK1 creates K1 consisting of 64 repetitions of password, K, U
func (enc *Encryption) computeK1(password, K, U []byte) ([]byte, error) {
	enc.stream.Reset()
	for i := 0; i < 64; i++ {
		if _, err := enc.stream.Write(password); err != nil {
			return nil, err
		}
		if _, err := enc.stream.Write(K); err != nil {
			return nil, err
		}
		if _, err := enc.stream.Write(U); err != nil {
			return nil, err
		}
	}
	return enc.stream.Bytes(), nil
}

// nextHashAlgorithm determines the next hash algorithm to use
func (enc *Encryption) nextHashAlgorithm(E []byte) int {
	if len(E) < 16 {
		panic("The input array must be at least 16 bytes long")
	}
	sum := 0
	for i := 0; i < 16; i++ {
		sum += int(E[i])
	}
	return sum % 3
}

// toHex converts bytes to hexadecimal string
func byteArrayToHexString(bytes []byte) string {
	const hexChars = "0123456789abcdef"
	hex := make([]byte, len(bytes)*2)
	for i, b := range bytes {
		hex[i*2] = hexChars[b>>4]
		hex[i*2+1] = hexChars[b&0x0F]
	}
	return string(hex)
}

// computeUserKeys computes user password keys (Algorithm 8)
func (enc *Encryption) computeUserKeys(userPasswordBytes []byte) (*User, error) {
	randomBytes := make([]byte, 16)
	if _, err := rand.Read(randomBytes); err != nil {
		return nil, err
	}

	userValidationSalt := randomBytes[:8]
	userKeySalt := randomBytes[8:16]

	hash, err := enc.computeHash(userPasswordBytes, userValidationSalt, []byte{})
	if err != nil {
		return nil, err
	}

	U := concatenate(hash, userValidationSalt, userKeySalt)

	hash, err = enc.computeHash(userPasswordBytes, userKeySalt, []byte{})
	if err != nil {
		return nil, err
	}

	UE, err := encryption.EncryptWithZeroIV(enc.fileEncryptionKey, hash)
	if err != nil {
		return nil, err
	}

	return &User{U: U, UE: UE}, nil
}

// computeOwnerKeys computes owner password keys (Algorithm 9)
func (enc *Encryption) computeOwnerKeys(ownerPasswordBytes, U []byte) (*Owner, error) {
	randomBytes := make([]byte, 16)
	if _, err := rand.Read(randomBytes); err != nil {
		return nil, err
	}

	ownerValidationSalt := randomBytes[:8]
	ownerKeySalt := randomBytes[8:16]

	hash, err := enc.computeHash(ownerPasswordBytes, ownerValidationSalt, U)
	if err != nil {
		return nil, err
	}

	O := concatenate(hash, ownerValidationSalt, ownerKeySalt)

	hash, err = enc.computeHash(ownerPasswordBytes, ownerKeySalt, U)
	if err != nil {
		return nil, err
	}

	OE, err := encryption.EncryptWithZeroIV(enc.fileEncryptionKey, hash)
	if err != nil {
		return nil, err
	}

	return &Owner{O: O, OE: OE}, nil
}

// concatenate combines three byte arrays
func concatenate(array1, array2, array3 []byte) []byte {
	result := make([]byte, len(array1)+len(array2)+len(array3))
	copy(result, array1)
	copy(result[len(array1):], array2)
	copy(result[len(array1)+len(array2):], array3)
	return result
}

// createUnencryptedPermsBlock creates the unencrypted permissions block for Algorithm 10
func createUnencryptedPermsBlock(permissionsValue uint32) []byte {
	// Extend the 32-bit permission to 64 bits with upper 32 bits set to 1
	extendedPermissions := uint64(0xFFFF_FFFF_0000_0000) | uint64(permissionsValue)

	// Get the 8 bytes of the permission in little-endian order
	permsBlock := make([]byte, 16)
	binary.LittleEndian.PutUint64(permsBlock, extendedPermissions)

	// Bytes 8-15 remain 0 for now (will be filled with validation code)
	return permsBlock
}

// clearBytes zeros out sensitive data
func clearBytes(b []byte) {
	for i := range b {
		b[i] = 0
	}
}

// intToString converts integer to string
func intToString(n uint32) string {
	if n == 0 {
		return "0"
	}

	var buf [20]byte // Enough for 64-bit numbers
	i := len(buf)
	for n > 0 {
		i--
		buf[i] = byte('0' + n%10)
		n /= 10
	}
	return string(buf[i:])
}
