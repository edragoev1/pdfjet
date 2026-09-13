/**
 * PermissionsTests.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Testing
@testable import PDFjet

@Suite struct PermissionsTests {
    @Test func userAccessValuesAreTheBitsOfTheStandard() {
        #expect(UserAccess.NONE.getValue() == 0)
        #expect(UserAccess.PRINT.getValue() == 4)
        #expect(UserAccess.MODIFY_CONTENTS.getValue() == 8)
        #expect(UserAccess.COPY_CONTENTS.getValue() == 16)
        #expect(UserAccess.MODIFY_ANNOTATIONS.getValue() == 32)
        #expect(UserAccess.FILL_FORM_FIELDS.getValue() == 256)
        #expect(UserAccess.EXTRACT_CONTENTS_FOR_ACCESSIBILITY.getValue() == 512)
        #expect(UserAccess.ASSEMBLE_DOCUMENT.getValue() == 1024)
        #expect(UserAccess.PRINT_HIGH_QUALITY.getValue() == 2048)
    }

    @Test func newPermissionsGrantNothing() {
        let permissions = Permissions()
        #expect(permissions.getAccess() == 0)
        #expect(!permissions.canPrint())
        #expect(!permissions.canCopyContents())
    }

    @Test func grantAndRevokeChangeOnlyTheirBits() {
        let permissions = Permissions()
                .grant(UserAccess.PRINT.getValue() | UserAccess.COPY_CONTENTS.getValue())
        #expect(permissions.getAccess() == 20)
        #expect(permissions.canPrint() && permissions.canCopyContents())
        _ = permissions.revoke(UserAccess.PRINT.getValue())
        #expect(permissions.getAccess() == 16)
        #expect(!permissions.canPrint())
        #expect(UserAccess.COPY_CONTENTS.isSetIn(permissions.getAccess()))
        #expect(!UserAccess.PRINT.isSetIn(permissions.getAccess()))
    }

    @Test func rawFlagsKeepOnlyTheValidBits() {
        #expect(Permissions(0xFFFFFFFF).getAccess() == 0xFFC)
        #expect(Permissions(0x3).getAccess() == 0)
    }
}
