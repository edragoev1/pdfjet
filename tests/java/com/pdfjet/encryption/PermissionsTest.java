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

import java.util.EnumSet;
import java.util.Set;
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
        assertTrue(permissions.getAccess().isEmpty());
        assertFalse(permissions.canPrint());
        assertFalse(permissions.canCopyContents());
    }

    @Test
    void grantAndRevokeChangeOnlyTheirBits() {
        Permissions permissions = new Permissions().grant(UserAccess.PRINT, UserAccess.COPY_CONTENTS);
        assertEquals(EnumSet.of(UserAccess.PRINT, UserAccess.COPY_CONTENTS), permissions.getAccess());
        assertEquals("Permissions: 0x14 (Raw Value: 20)", permissions.toString());
        assertTrue(permissions.canPrint() && permissions.canCopyContents());
        permissions.revoke(UserAccess.PRINT);
        assertEquals(EnumSet.of(UserAccess.COPY_CONTENTS), permissions.getAccess());
        assertFalse(permissions.canPrint());
        assertTrue(UserAccess.COPY_CONTENTS.isSetIn(permissions.getAccess()));
        assertFalse(UserAccess.PRINT.isSetIn(permissions.getAccess()));
        assertTrue(UserAccess.NONE.isSetIn(permissions.getAccess()));
    }

    @Test
    void setAccessReplacesThePermissionsAndGetAccessReturnsACopy() {
        Permissions permissions = new Permissions().grant(UserAccess.PRINT)
                .setAccess(EnumSet.of(UserAccess.COPY_CONTENTS, UserAccess.NONE));
        Set<UserAccess> access = permissions.getAccess();
        assertEquals(EnumSet.of(UserAccess.COPY_CONTENTS), access);
        access.add(UserAccess.PRINT);
        assertFalse(permissions.canPrint());
    }

    @Test
    void rawFlagsKeepOnlyTheValidBits() {
        assertEquals(EnumSet.complementOf(EnumSet.of(UserAccess.NONE)), new Permissions(0xFFFFFFFF).getAccess());
        assertEquals("Permissions: 0xFFC (Raw Value: 4092)", new Permissions(0xFFFFFFFF).toString());
        assertTrue(new Permissions(0x3).getAccess().isEmpty());
    }
}
