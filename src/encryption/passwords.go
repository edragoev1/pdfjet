// Passwords.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package encryption

// Passwords holds the user and owner passwords of an encrypted PDF. A password
// is used as typed, in UTF-8, and at most 127 bytes of it are used. A password
// that is not set is empty, so the PDF opens without a prompt when the user
// password is not set.
type Passwords struct {
	userPassword  string
	ownerPassword string
}

// NewPasswords creates a new instance of Passwords.
func NewPasswords() *Passwords {
	return &Passwords{}
}

// SetUserPassword sets the user password.
func (p *Passwords) SetUserPassword(userPassword string) *Passwords {
	p.userPassword = userPassword
	return p
}

// SetOwnerPassword sets the owner password.
func (p *Passwords) SetOwnerPassword(ownerPassword string) *Passwords {
	p.ownerPassword = ownerPassword
	return p
}

// GetUserPassword returns the user password.
func (p *Passwords) GetUserPassword() string {
	return p.userPassword
}

// GetOwnerPassword returns the owner password.
func (p *Passwords) GetOwnerPassword() string {
	return p.ownerPassword
}
