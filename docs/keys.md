# Key profiles

Keys used for image signing and encryption are kept outside images and scripts.
Profiles are managed with:

```text
keys list
keys import <profile> <private-key-file> <certificate-file>
keys import-symmetric <profile> <secret-file>
keys use <profile>
keys remove <profile>
```

A profile contains the key kind (asymmetric/symmetric), usage, algorithm,
fingerprint, public certificate and protected private/symmetric material.
The default store is under the user profile:

- Windows: `%APPDATA%\NyaFs\keys`
- Linux: `~/.config/nyafs/keys`

On Windows private material is protected with DPAPI (`CurrentUser`). Other
platforms currently reject secure key import until a libsecret/0600 provider is
implemented.

`keys import` requires a certificate; its SHA-256 fingerprint is used to match
an embedded image certificate to a private signing key. `keys import-symmetric`
stores an opaque secret for format-specific encryption/decryption.

Private keys are never written into an image. FIT/BMU/Android plugins will
consume `IKeyStore` for verification, re-signing and re-encryption; those
integrations are separate from the profile storage.
