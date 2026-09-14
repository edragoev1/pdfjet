/*
 * PermissionsTest.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using Xunit;

namespace PDFjet.NET {
/// <summary>
/// GetAccess returns the UserAccess flags, whose number is the /P value without
/// its reserved bits, as Java's getAccess returns it as an int.
/// </summary>
public class PermissionsTest {
    [Fact]
    public void UserAccessValuesAreTheBitsOfTheStandard() {
        Assert.Equal(0, (int) UserAccess.NONE);
        Assert.Equal(4, (int) UserAccess.PRINT);
        Assert.Equal(8, (int) UserAccess.MODIFY_CONTENTS);
        Assert.Equal(16, (int) UserAccess.COPY_CONTENTS);
        Assert.Equal(32, (int) UserAccess.MODIFY_ANNOTATIONS);
        Assert.Equal(256, (int) UserAccess.FILL_FORM_FIELDS);
        Assert.Equal(512, (int) UserAccess.EXTRACT_CONTENTS_FOR_ACCESSIBILITY);
        Assert.Equal(1024, (int) UserAccess.ASSEMBLE_DOCUMENT);
        Assert.Equal(2048, (int) UserAccess.PRINT_HIGH_QUALITY);
    }

    [Fact]
    public void NewPermissionsGrantNothing() {
        Permissions permissions = new Permissions();
        Assert.Equal(0u, (uint) permissions.GetAccess());
        Assert.False(permissions.CanPrint());
        Assert.False(permissions.CanCopyContents());
    }

    [Fact]
    public void GrantAndRevokeChangeOnlyTheirBits() {
        Permissions permissions = new Permissions().Grant(UserAccess.PRINT | UserAccess.COPY_CONTENTS);
        Assert.Equal(20u, (uint) permissions.GetAccess());
        Assert.True(permissions.CanPrint() && permissions.CanCopyContents());
        permissions.Revoke(UserAccess.PRINT);
        Assert.Equal(16u, (uint) permissions.GetAccess());
        Assert.False(permissions.CanPrint());
        Assert.True(permissions.GetAccess().HasFlag(UserAccess.COPY_CONTENTS));
        Assert.False(permissions.GetAccess().HasFlag(UserAccess.PRINT));
    }

    [Fact]
    public void RawFlagsKeepOnlyTheValidBits() {
        Assert.Equal(0xFFCu, (uint) new Permissions(unchecked((int) 0xFFFFFFFF)).GetAccess());
        Assert.Equal(0u, (uint) new Permissions(0x3).GetAccess());
    }
}
}
