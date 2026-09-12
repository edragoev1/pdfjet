/*
 * Passwords.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;

namespace PDFjet.NET {
/// <summary>The user and owner passwords of an encrypted PDF. Please see Example_30.</summary>
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
