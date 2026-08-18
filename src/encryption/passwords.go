// Passwords.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package encryption

// Passwords holds user and owner password information for PDF encryption.
// This version uses exported fields for more idiomatic Go access.
type Passwords struct {
	UserPassword  string
	OwnerPassword string
}

// NewPasswords creates a new instance of Passwords.
func NewPasswords() *Passwords {
	return &Passwords{}
}

// SetPasswords sets both user and owner passwords at once.
func (p *Passwords) SetPasswords(userPassword, ownerPassword string) {
	p.UserPassword = userPassword
	p.OwnerPassword = ownerPassword
}
