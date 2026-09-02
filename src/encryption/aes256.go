// aes256.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package encryption

import (
	"crypto/aes"
	"crypto/cipher"
	"crypto/rand"
	"errors"
	"io"
)

// AES256 provides methods for AES-256 encryption in various modes.
type AES256 struct{}

// EncryptWithZeroIV encrypts a 32-byte File Encryption Key (FEK) with AES-256-CBC,
// using a zero IV and no padding.
//
// fileEncryptionKey: 32-byte FEK to encrypt.
// key: 32-byte key used for AES-256 encryption.
// Returns: The encrypted 32-byte File Encryption Key.
func (a *AES256) EncryptWithZeroIV(fileEncryptionKey, key []byte) ([]byte, error) {
	if fileEncryptionKey == nil || len(fileEncryptionKey) != 32 {
		return nil, errors.New("file Encryption Key must be 32 bytes long")
	}
	if key == nil || len(key) != 32 {
		return nil, errors.New("the encryption key must be 32 bytes long")
	}

	// Create AES cipher
	block, err := aes.NewCipher(key)
	if err != nil {
		return nil, err
	}

	// Create zero IV (16 bytes)
	iv := make([]byte, aes.BlockSize)

	// Encrypt the data using CBC mode
	ciphertext := make([]byte, len(fileEncryptionKey))
	mode := cipher.NewCBCEncrypter(block, iv)
	mode.CryptBlocks(ciphertext, fileEncryptionKey)

	return ciphertext, nil
}

// Encrypt encrypts the provided data using AES-256 in CBC mode with a randomly generated IV.
// The resulting byte array is structured as: [16-byte IV][encrypted data].
//
// data: The data to be encrypted.
// key: The 32-byte encryption key for AES-256.
// Returns: A byte array containing the IV prepended to the AES-256-CBC encrypted data.
func (a *AES256) Encrypt(data, key []byte) ([]byte, error) {
	if key == nil || len(key) != 32 {
		return nil, errors.New("the encryption key must be 32 bytes long")
	}

	// Create AES cipher
	block, err := aes.NewCipher(key)
	if err != nil {
		return nil, err
	}

	// Generate random IV
	iv := make([]byte, aes.BlockSize)
	if _, err := io.ReadFull(rand.Reader, iv); err != nil {
		return nil, err
	}

	// Pad the data to be a multiple of the block size
	paddedData := pkcs7Pad(data, aes.BlockSize)

	// Encrypt the data
	ciphertext := make([]byte, len(iv)+len(paddedData))
	copy(ciphertext[:aes.BlockSize], iv)

	mode := cipher.NewCBCEncrypter(block, iv)
	mode.CryptBlocks(ciphertext[aes.BlockSize:], paddedData)

	return ciphertext, nil
}

// EncryptECB encrypts data using AES-256 in ECB mode with zero IV and no padding.
// Required when encrypting the Perms (permissions).
//
// data: The data to be encrypted.
// key: The 32-byte encryption key for AES-256.
// Returns: The encrypted data.
func (a *AES256) EncryptECB(data, key []byte) ([]byte, error) {
	if key == nil || len(key) != 32 {
		return nil, errors.New("the encryption key must be 32 bytes long")
	}

	// Create AES cipher
	block, err := aes.NewCipher(key)
	if err != nil {
		return nil, err
	}

	// ECB mode requires that data length is a multiple of block size
	if len(data)%aes.BlockSize != 0 {
		return nil, errors.New("data length must be a multiple of the block size for ECB mode")
	}

	// Encrypt each block separately (ECB mode)
	ciphertext := make([]byte, len(data))
	for i := 0; i < len(data); i += aes.BlockSize {
		block.Encrypt(ciphertext[i:i+aes.BlockSize], data[i:i+aes.BlockSize])
	}

	return ciphertext, nil
}

// pkcs7Pad pads the data to the specified block size using PKCS#7 padding.
func pkcs7Pad(data []byte, blockSize int) []byte {
	padding := blockSize - (len(data) % blockSize)
	padText := make([]byte, padding)
	for i := range padText {
		padText[i] = byte(padding)
	}
	return append(data, padText...)
}

// EncryptWithZeroIV helper function for backward compatibility - static method style
func EncryptWithZeroIV(fileEncryptionKey, key []byte) ([]byte, error) {
	aes256 := &AES256{}
	return aes256.EncryptWithZeroIV(fileEncryptionKey, key)
}

func Encrypt(data, key []byte) ([]byte, error) {
	aes256 := &AES256{}
	return aes256.Encrypt(data, key)
}

func EncryptECB(data, key []byte) ([]byte, error) {
	aes256 := &AES256{}
	return aes256.EncryptECB(data, key)
}
