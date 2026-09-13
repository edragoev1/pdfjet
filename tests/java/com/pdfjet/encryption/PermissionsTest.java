/*
 * PermissionsTest.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet.encryption;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertFalse;
import static org.junit.jupiter.api.Assertions.assertTrue;

import org.junit.jupiter.api.Test;

class PermissionsTest {
    @Test
    void userAccessValuesAreTheBitsOfTheStandard() {
        assertEquals(0, UserAccess.NONE.getValue());
        assertEquals(4, UserAccess.PRINT.getValue());
        assertEquals(8, UserAccess.MODIFY_CONTENTS.getValue());
        assertEquals(16, UserAccess.COPY_CONTENTS.getValue());
        assertEquals(32, UserAccess.MODIFY_ANNOTATIONS.getValue());
        assertEquals(256, UserAccess.FILL_FORM_FIELDS.getValue());
        assertEquals(512, UserAccess.EXTRACT_CONTENTS_FOR_ACCESSIBILITY.getValue());
        assertEquals(1024, UserAccess.ASSEMBLE_DOCUMENT.getValue());
        assertEquals(2048, UserAccess.PRINT_HIGH_QUALITY.getValue());
    }

    @Test
    void newPermissionsGrantNothing() {
        Permissions permissions = new Permissions();
        assertEquals(0, permissions.getAccess());
        assertFalse(permissions.canPrint());
        assertFalse(permissions.canCopyContents());
    }

    @Test
    void grantAndRevokeChangeOnlyTheirBits() {
        Permissions permissions = new Permissions()
                .grant(UserAccess.PRINT.getValue() | UserAccess.COPY_CONTENTS.getValue());
        assertEquals(20, permissions.getAccess());
        assertTrue(permissions.canPrint() && permissions.canCopyContents());
        permissions.revoke(UserAccess.PRINT.getValue());
        assertEquals(16, permissions.getAccess());
        assertFalse(permissions.canPrint());
        assertTrue(UserAccess.COPY_CONTENTS.isSetIn(permissions.getAccess()));
        assertFalse(UserAccess.PRINT.isSetIn(permissions.getAccess()));
    }

    @Test
    void rawFlagsKeepOnlyTheValidBits() {
        assertEquals(0xFFC, new Permissions(0xFFFFFFFF).getAccess());
        assertEquals(0, new Permissions(0x3).getAccess());
    }
}
