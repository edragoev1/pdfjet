# Security Policy

## Supported Versions

We actively support the latest version of PDFjet with security updates. Older versions may not receive patches.

| Version | Supported |
|---------|-----------|
| Latest  | ✅ Yes    |
| < Latest| ❌ No     |

## Reporting a Vulnerability

We take security vulnerabilities seriously. If you discover a security issue in PDFjet, please report it to us immediately.

### How to Report

1. **Email**: Send details to edragoev@protonmail.com
2. **GitHub Private Vulnerability Reporting**: If available, use the "Report a vulnerability" button on our GitHub repository

### What to Include

When reporting a vulnerability, please include:

- Description of the vulnerability
- Steps to reproduce (code samples are helpful)
- Potential impact
- Version(s) affected
- Any suggested fixes (if available)

### Response Timeline

We aim to respond to all security reports within:

- **Initial Response**: 48 hours
- **Status Update**: 7 days
- **Fix or Mitigation**: 30 days (depending on complexity)

### Disclosure Policy

We follow a coordinated disclosure process:

1. Report is received and acknowledged
2. We investigate and develop a fix
3. A new version is released
4. The vulnerability is publicly disclosed (with credit to the reporter)

## Security Best Practices

When using PDFjet in your applications:

- Always use the latest version for security patches
- Validate all user input before passing to PDF generation
- Implement appropriate file system permissions
- Use environment variables for sensitive configuration
- Consider running PDF generation in isolated environments

## Security Features

PDFjet includes several security features:

- Stream sanitization to prevent injection attacks
- Memory safety checks
- Input validation for all public APIs
- Protection against denial-of-service attacks (configurable limits)

## Acknowledgements

We thank the security researchers who have helped improve PDFjet's security:

## Contact

For general security questions or to report a vulnerability:

- **Email**: edragoev@protonmail.com
- **GitHub**: https://github.com/edragoev1/pdfjet

**Please DO NOT report security vulnerabilities through public GitHub issues.**