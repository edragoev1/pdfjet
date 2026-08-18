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
// ISO 32000-2 (PDF 2.0) Table 22. Permissions are stored as flags in a 32-bit integer.

type UserAccess uint32

const (
	// None No permissions (default)
	None UserAccess = 0

	// Print Bit position: 3
	Print UserAccess = 1 << 3 // 8

	// ModifyContents Bit position: 4
	ModifyContents UserAccess = 1 << 4 // 16

	// CopyContents Bit position: 5
	CopyContents UserAccess = 1 << 5 // 32

	// ModifyAnnotations Bit position: 6
	ModifyAnnotations UserAccess = 1 << 6 // 64

	// FillFormFields Bit position: 9
	FillFormFields UserAccess = 1 << 9 // 512

	// ExtractContentsForAccessibility Bit position: 10
	ExtractContentsForAccessibility UserAccess = 1 << 10 // 1024

	// AssembleDocument Bit position: 11
	AssembleDocument UserAccess = 1 << 11 // 2048

	// PrintHighQuality Bit position: 12
	PrintHighQuality UserAccess = 1 << 12 // 4096
)

// String returns a string representation of the UserAccess flags
func (ua UserAccess) String() string {
	if ua == None {
		return "None"
	}

	var permissions []string
	if ua.Has(Print) {
		permissions = append(permissions, "Print")
	}
	if ua.Has(ModifyContents) {
		permissions = append(permissions, "ModifyContents")
	}
	if ua.Has(CopyContents) {
		permissions = append(permissions, "CopyContents")
	}
	if ua.Has(ModifyAnnotations) {
		permissions = append(permissions, "ModifyAnnotations")
	}
	if ua.Has(FillFormFields) {
		permissions = append(permissions, "FillFormFields")
	}
	if ua.Has(ExtractContentsForAccessibility) {
		permissions = append(permissions, "ExtractContentsForAccessibility")
	}
	if ua.Has(AssembleDocument) {
		permissions = append(permissions, "AssembleDocument")
	}
	if ua.Has(PrintHighQuality) {
		permissions = append(permissions, "PrintHighQuality")
	}

	return strings.Join(permissions, " | ")
}

// Has checks if the specified permission is set
func (ua UserAccess) Has(permission UserAccess) bool {
	return ua&permission == permission
}

// Add adds the specified permission(s)
func (ua UserAccess) Add(permission UserAccess) UserAccess {
	return ua | permission
}

// Remove removes the specified permission(s)
func (ua UserAccess) Remove(permission UserAccess) UserAccess {
	return ua &^ permission
}

// Permissions encapsulates the user access permissions for a PDF document as specified in
// ISO 32000-2, Table 22. Provides a type-safe interface to manipulate and query
// the permissions flags.
type Permissions struct {
	permissionsFlags uint32
}

// ValidBitsMask defines the valid bits (3-12) that can be set in the permissions flag.
// Bits outside this range are reserved and must be zero.
const ValidBitsMask uint32 = 0b1_1111_1111_1000 // Hex: 0xFFF8

// NewPermissions creates a new instance of Permissions with no permissions granted.
func NewPermissions() *Permissions {
	return &Permissions{permissionsFlags: 0}
}

// NewPermissionsFromInt creates a new instance of Permissions from the raw 32-bit integer value
// found in the PDF encryption dictionary's /P key. Invalid bits (outside positions 3-12) are masked out.
func NewPermissionsFromInt(rawFlags int) *Permissions {
	return NewPermissionsFromUint32(uint32(rawFlags))
}

// NewPermissionsFromUint32 creates a new instance of Permissions from the raw 32-bit integer value
// found in the PDF encryption dictionary's /P key. Invalid bits (outside positions 3-12) are masked out.
func NewPermissionsFromUint32(rawFlags uint32) *Permissions {
	return &Permissions{permissionsFlags: rawFlags & ValidBitsMask}
}

// GetAccess returns the permissions as UserAccess flags
func (p *Permissions) GetAccess() UserAccess {
	return UserAccess(p.permissionsFlags)
}

// SetAccess sets the permissions using UserAccess flags
func (p *Permissions) SetAccess(access UserAccess) {
	p.permissionsFlags = uint32(access) & ValidBitsMask
}

// GetRawValue returns the raw 32-bit integer value of the permissions flags.
// This value is suitable for writing to the /P key in a PDF encryption dictionary.
// All reserved bits are guaranteed to be zero.
func (p *Permissions) GetRawValue() uint32 {
	return p.permissionsFlags
}

// CanPrint returns true if the user can print the document
// (possibly at low quality, unless CanPrintHighQuality() is true).
func (p *Permissions) CanPrint() bool {
	return p.GetAccess().Has(Print)
}

// CanModifyContents returns true if the user can modify the document's contents.
func (p *Permissions) CanModifyContents() bool {
	return p.GetAccess().Has(ModifyContents)
}

// CanCopyContents returns true if the user can copy or extract content.
func (p *Permissions) CanCopyContents() bool {
	return p.GetAccess().Has(CopyContents)
}

// CanModifyAnnotations returns true if the user can add or modify annotations and form fields.
// This is primarily for legacy PDF support.
func (p *Permissions) CanModifyAnnotations() bool {
	return p.GetAccess().Has(ModifyAnnotations)
}

// CanFillFormFields returns true if the user can fill interactive form fields.
func (p *Permissions) CanFillFormFields() bool {
	return p.GetAccess().Has(FillFormFields)
}

// CanExtractForAccessibility returns true if the user can extract content for accessibility.
func (p *Permissions) CanExtractForAccessibility() bool {
	return p.GetAccess().Has(ExtractContentsForAccessibility)
}

// CanAssembleDocument returns true if the user can assemble the document (manipulate pages).
func (p *Permissions) CanAssembleDocument() bool {
	return p.GetAccess().Has(AssembleDocument)
}

// CanPrintHighQuality returns true if the user can print the document at high quality.
func (p *Permissions) CanPrintHighQuality() bool {
	return p.GetAccess().Has(PrintHighQuality)
}

// SetPermissions sets or clears the specified permissions.
func (p *Permissions) SetPermissions(permissions UserAccess, grant bool) {
	if grant {
		p.permissionsFlags |= uint32(permissions)
	} else {
		p.permissionsFlags &^= uint32(permissions)
	}
	// Re-apply mask to ensure no invalid bits were set
	p.permissionsFlags &= ValidBitsMask
}

// String returns a string that represents the current permissions for debugging purposes.
func (p *Permissions) String() string {
	return fmt.Sprintf("Permissions: %s (Raw Value: %d)", p.GetAccess(), p.GetRawValue())
}
