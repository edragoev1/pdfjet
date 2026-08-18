// aes128.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package encryption

import (
	"crypto/aes"
	"crypto/cipher"
	"errors"
)

// AES128 provides methods for AES-128 encryption.
type AES128 struct{}

// EncryptK1 encrypts K1 with AES-128-CBC, no padding.
//
// K1: Data to encrypt (must be a multiple of 16 bytes).
// key: Exactly 16 bytes – the AES-128 key.
// iv: Exactly 16 bytes – the initialization vector.
// Returns: The ciphertext.
func (a *AES128) EncryptK1(K1, key, iv []byte) ([]byte, error) {
	// ---------- basic argument validation ----------
	if K1 == nil || len(K1) == 0 {
		return nil, errors.New("K1 cannot be null or empty")
	}

	if key == nil || len(key) != 16 {
		return nil, errors.New("Key must be exactly 16 bytes (AES-128)")
	}

	if iv == nil || len(iv) != 16 {
		return nil, errors.New("IV must be exactly 16 bytes")
	}

	// Check that K1 length is a multiple of block size for no-padding mode
	if len(K1)%aes.BlockSize != 0 {
		return nil, errors.New("K1 length must be a multiple of 16 bytes for no-padding mode")
	}

	// ---------- AES-128-CBC, no padding ----------
	block, err := aes.NewCipher(key)
	if err != nil {
		return nil, err
	}

	// ---------- encrypt in one shot ----------
	ciphertext := make([]byte, len(K1))
	mode := cipher.NewCBCEncrypter(block, iv)
	mode.CryptBlocks(ciphertext, K1)

	return ciphertext, nil
}

// EncryptK1 helper function for backward compatibility.
func EncryptK1(K1, key, iv []byte) ([]byte, error) {
	aes128 := &AES128{}
	return aes128.EncryptK1(K1, key, iv)
}
