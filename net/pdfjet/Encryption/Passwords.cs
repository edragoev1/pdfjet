/*
 * Passwords.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;

namespace PDFjet.NET {
/// <summary>
/// The user and owner passwords of an encrypted PDF. A password is used as
/// typed, in UTF-8, and at most 127 bytes of it are used. Chrome does not cut
/// a longer password to its first 127 bytes, as other viewers do, and opens
/// the PDF only when just those are typed, so a password of 127 bytes or
/// fewer is the one to give. Please see Example_30.
/// </summary>
public class Passwords {
    private String userPassword = "";
    private String ownerPassword = "";

    /// <summary>Creates an empty set of passwords.</summary>
    public Passwords() {
    }

    /// <summary>Sets the user password, which is required to open the document.</summary>
    public Passwords SetUserPassword(String userPassword) {
        this.userPassword = userPassword;
        return this;
    }

    /// <summary>Sets the owner password, which opens the document with full access.</summary>
    public Passwords SetOwnerPassword(String ownerPassword) {
        this.ownerPassword = ownerPassword;
        return this;
    }

    /// <summary>Returns the user password.</summary>
    public String GetUserPassword() {
        return userPassword;
    }

    /// <summary>Returns the owner password.</summary>
    public String GetOwnerPassword() {
        return ownerPassword;
    }
}
}   // End of namespace PDFjet.NET
