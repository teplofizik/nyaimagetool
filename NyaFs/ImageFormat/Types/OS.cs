using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.ImageFormat.Types
{
	/// <summary>
	/// Operating System Codes
	/// The following are exposed to uImage header.
	/// New IDs *MUST* be appended at the end of the list and *NEVER*
	/// inserted for backward compatibility.
	/// </summary>
	public enum OS
	{
		IH_OS_INVALID = 0,  /* Invalid OS	*/
		IH_OS_OPENBSD,          /* OpenBSD	*/
		IH_OS_NETBSD,           /* NetBSD	*/
		IH_OS_FREEBSD,          /* FreeBSD	*/
		IH_OS_4_4BSD,           /* 4.4BSD	*/
		IH_OS_LINUX,            /* Linux	*/
		IH_OS_SVR4,         /* SVR4		*/
		IH_OS_ESIX,         /* Esix		*/
		IH_OS_SOLARIS,          /* Solaris	*/
		IH_OS_IRIX,         /* Irix		*/
		IH_OS_SCO,          /* SCO		*/
		IH_OS_DELL,         /* Dell		*/
		IH_OS_NCR,          /* NCR		*/
		IH_OS_LYNXOS,           /* LynxOS	*/
		IH_OS_VXWORKS,          /* VxWorks	*/
		IH_OS_PSOS,         /* pSOS		*/
		IH_OS_QNX,          /* QNX		*/
		IH_OS_U_BOOT,           /* Firmware	*/
		IH_OS_RTEMS,            /* RTEMS	*/
		IH_OS_ARTOS,            /* ARTOS	*/
		IH_OS_UNITY,            /* Unity OS	*/
		IH_OS_INTEGRITY,        /* INTEGRITY	*/
		IH_OS_OSE,          /* OSE		*/
		IH_OS_PLAN9,            /* Plan 9	*/
		IH_OS_OPENRTOS,     /* OpenRTOS	*/
		IH_OS_ARM_TRUSTED_FIRMWARE,     /* ARM Trusted Firmware */
		IH_OS_TEE,          /* Trusted Execution Environment */
		IH_OS_OPENSBI,          /* RISC-V OpenSBI */
		IH_OS_EFI,          /* EFI Firmware (e.g. GRUB2) */

		IH_OS_COUNT,
	};


}
