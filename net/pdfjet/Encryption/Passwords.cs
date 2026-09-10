/**
 * Passwords.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;

namespace PDFjet.NET {
public class Passwords {
    private String userPassword;
    private String ownerPassword;

    public Passwords() {
    }

    public Passwords SetUserPassword(String userPassword) {
        this.userPassword = userPassword;
        return this;
    }

    public Passwords SetOwnerPassword(String ownerPassword) {
        this.ownerPassword = ownerPassword;
        return this;
    }

    public String GetUserPassword() {
        return userPassword;
    }

    public String GetOwnerPassword() {
        return ownerPassword;
    }
}
}   // End of namespace PDFjet.NET
