# NyaFsSwupdate / SWUpdate

This plugin reads SWUpdate packages (`.swu`) used by Innosilicon and Canaan
Avalon Nano3 firmware. A package is a CPIO newc/CRC archive containing
`sw-description`, its optional RSA signature, and image/file payloads.

Sources:
- SWUpdate format: https://github.com/sbabic/swupdate
- Avalon Nano3 firmware/samples: https://lacerta.es/nanojb/
- Innosilicon `sw-description`/SWU layout: vendor firmware samples

The plugin is not limited to a `rootfs.ubi` entry anymore: any valid CPIO
package with `sw-description` is accepted, and every archive entry is exposed to
the normal recursive pipeline (UBI/UBIFS, uImage, AUP, scripts, DTB, ...).

Writing keeps the original package metadata and replaces only dirty payloads;
`sw-description` SHA-256 fields and the CPIO checksum are refreshed, and
`sw-description.sig` is re-signed when `swu.sign-key` is configured.
