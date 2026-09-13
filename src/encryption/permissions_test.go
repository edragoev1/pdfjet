// permissions_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package encryption

import "testing"

func TestPermissionsUserAccessValuesAreTheBitsOfTheStandard(t *testing.T) {
	want := map[UserAccess]uint32{
		None: 0, Print: 4, ModifyContents: 8, CopyContents: 16, ModifyAnnotations: 32,
		FillFormFields: 256, ExtractContentsForAccessibility: 512, AssembleDocument: 1024,
		PrintHighQuality: 2048,
	}
	for access, value := range want {
		if uint32(access) != value {
			t.Errorf("%v: %d", access, uint32(access))
		}
	}
}

func TestPermissionsNewPermissionsGrantNothing(t *testing.T) {
	permissions := NewPermissions()
	if permissions.GetAccess() != 0 || permissions.CanPrint() || permissions.CanCopyContents() {
		t.Error("new permissions grant something")
	}
}

func TestPermissionsGrantAndRevokeChangeOnlyTheirBits(t *testing.T) {
	permissions := NewPermissions().Grant(Print | CopyContents)
	if permissions.GetAccess() != 20 || !permissions.CanPrint() || !permissions.CanCopyContents() {
		t.Errorf("granted %d", permissions.GetAccess())
	}
	permissions.Revoke(Print)
	if permissions.GetAccess() != 16 || permissions.CanPrint() {
		t.Errorf("revoked %d", permissions.GetAccess())
	}
	if !permissions.GetAccess().Has(CopyContents) || permissions.GetAccess().Has(Print) {
		t.Error("wrong Has")
	}
}

func TestPermissionsRawFlagsKeepOnlyTheValidBits(t *testing.T) {
	if got := NewPermissionsFromUint32(0xFFFFFFFF).GetAccess(); got != 0xFFC {
		t.Errorf("all bits: %d", got)
	}
	if got := NewPermissionsFromInt(0x3).GetAccess(); got != 0 {
		t.Errorf("low bits: %d", got)
	}
}
