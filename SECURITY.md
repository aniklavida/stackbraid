# Security policy

## Supported versions

StackBraid has no public release yet, so no version is supported.

## Reporting a vulnerability

Do not open a public issue for a suspected vulnerability, a leaked credential, or anything containing private data. GitHub private vulnerability reporting must be enabled before v1.0.

Include the affected commit, reproduction steps, impact and sanitized evidence. Never include a real token, connection string or key.

## Security model

- Secrets stay outside the repository. `.env` is never committed.
- Authentication lives in the backend, not in a third-party vendor, so a user is never forced into an external identity provider.
- Access tokens are held in memory; refresh tokens use httpOnly cookies. Browser local storage is readable by any cross-site scripting flaw and is not used for tokens.
- Admin capability is a role within one application, not a separately deployed surface.
- External, destructive and irreversible actions require explicit approval.
- Every shipped dependency is licence-audited, because users inherit it.
