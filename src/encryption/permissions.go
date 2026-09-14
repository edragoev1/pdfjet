// Permissions.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package encryption

import (
	"fmt"
	"strings"
)

// UserAccess represents the user access permissions for an encrypted PDF document as defined in
// ISO 32000-2 (PDF 2.0) Table 22. Permissions are stored as flags in a 32-bit integer,
// whose bit positions are numbered from 1, the low-order bit.
type UserAccess uint32

const (
	// None No permissions (default)
	None UserAccess = 0

	// Print Bit position: 3
	Print UserAccess = 1 << 2 // 4

	// ModifyContents Bit position: 4
	ModifyContents UserAccess = 1 << 3 // 8

	// CopyContents Bit position: 5
	CopyContents UserAccess = 1 << 4 // 16

	// ModifyAnnotations Bit position: 6
	ModifyAnnotations UserAccess = 1 << 5 // 32

	// FillFormFields Bit position: 9
	FillFormFields UserAccess = 1 << 8 // 256

	// ExtractContentsForAccessibility Bit position: 10
	ExtractContentsForAccessibility UserAccess = 1 << 9 // 512

	// AssembleDocument Bit position: 11
	AssembleDocument UserAccess = 1 << 10 // 1024

	// PrintHighQuality Bit position: 12
	PrintHighQuality UserAccess = 1 << 11 // 2048
)

// String returns a string representation of the UserAccess flags
func (ua UserAccess) String() string {
	if ua == None {
		return "None"
	}

	var permissions []string
	if Print.IsSetIn(ua) {
		permissions = append(permissions, "Print")
	}
	if ModifyContents.IsSetIn(ua) {
		permissions = append(permissions, "ModifyContents")
	}
	if CopyContents.IsSetIn(ua) {
		permissions = append(permissions, "CopyContents")
	}
	if ModifyAnnotations.IsSetIn(ua) {
		permissions = append(permissions, "ModifyAnnotations")
	}
	if FillFormFields.IsSetIn(ua) {
		permissions = append(permissions, "FillFormFields")
	}
	if ExtractContentsForAccessibility.IsSetIn(ua) {
		permissions = append(permissions, "ExtractContentsForAccessibility")
	}
	if AssembleDocument.IsSetIn(ua) {
		permissions = append(permissions, "AssembleDocument")
	}
	if PrintHighQuality.IsSetIn(ua) {
		permissions = append(permissions, "PrintHighQuality")
	}

	return strings.Join(permissions, " | ")
}

// IsSetIn reports whether this permission is set in the flags.
func (ua UserAccess) IsSetIn(flags UserAccess) bool {
	return flags&ua == ua
}

// Permissions encapsulates the user access permissions for a PDF document as specified in
// ISO 32000-2, Table 22. Provides a type-safe interface to manipulate and query
// the permissions flags.
type Permissions struct {
	permissionsFlags uint32
}

// validBitsMask defines the valid bits (3-12) that can be set in the permissions flag.
// Bits outside this range are reserved and must be zero.
const validBitsMask uint32 = 0b1111_1111_1100 // Hex: 0xFFC

// NewPermissions creates a new instance of Permissions with no permissions granted.
func NewPermissions() *Permissions {
	return &Permissions{permissionsFlags: 0}
}

// NewPermissionsFromInt creates permissions from the /P value of an encryption
// dictionary, keeping only its permission bits.
func NewPermissionsFromInt(rawFlags int) *Permissions {
	return &Permissions{permissionsFlags: uint32(rawFlags) & validBitsMask}
}

// GetAccess returns the permissions as UserAccess flags
func (p *Permissions) GetAccess() UserAccess {
	return UserAccess(p.permissionsFlags)
}

// SetAccess sets the permissions using UserAccess flags
func (p *Permissions) SetAccess(access UserAccess) *Permissions {
	p.permissionsFlags = uint32(access) & validBitsMask
	return p
}

// CanPrint returns true if the user can print the document
// (possibly at low quality, unless CanPrintHighQuality() is true).
func (p *Permissions) CanPrint() bool {
	return Print.IsSetIn(p.GetAccess())
}

// CanModifyContents returns true if the user can modify the document's contents.
func (p *Permissions) CanModifyContents() bool {
	return ModifyContents.IsSetIn(p.GetAccess())
}

// CanCopyContents returns true if the user can copy or extract content.
func (p *Permissions) CanCopyContents() bool {
	return CopyContents.IsSetIn(p.GetAccess())
}

// CanModifyAnnotations returns true if the user can add or modify annotations and form fields.
// This is primarily for legacy PDF support.
func (p *Permissions) CanModifyAnnotations() bool {
	return ModifyAnnotations.IsSetIn(p.GetAccess())
}

// CanFillFormFields returns true if the user can fill interactive form fields.
func (p *Permissions) CanFillFormFields() bool {
	return FillFormFields.IsSetIn(p.GetAccess())
}

// CanExtractForAccessibility returns true if the user can extract content for accessibility.
func (p *Permissions) CanExtractForAccessibility() bool {
	return ExtractContentsForAccessibility.IsSetIn(p.GetAccess())
}

// CanAssembleDocument returns true if the user can assemble the document (manipulate pages).
func (p *Permissions) CanAssembleDocument() bool {
	return AssembleDocument.IsSetIn(p.GetAccess())
}

// CanPrintHighQuality returns true if the user can print the document at high quality.
func (p *Permissions) CanPrintHighQuality() bool {
	return PrintHighQuality.IsSetIn(p.GetAccess())
}

// Grant grants the specified permissions. The other permissions stay as they are.
func (p *Permissions) Grant(permissions UserAccess) *Permissions {
	p.permissionsFlags |= uint32(permissions)
	// Re-apply mask to ensure no invalid bits were set
	p.permissionsFlags &= validBitsMask
	return p
}

// Revoke revokes the specified permissions. The other permissions stay as they are.
func (p *Permissions) Revoke(permissions UserAccess) *Permissions {
	p.permissionsFlags &^= uint32(permissions)
	p.permissionsFlags &= validBitsMask
	return p
}

// String returns a string that represents the current permissions for debugging purposes.
func (p *Permissions) String() string {
	return fmt.Sprintf("Permissions: %s (Raw Value: %d)", p.GetAccess(), p.permissionsFlags)
}
