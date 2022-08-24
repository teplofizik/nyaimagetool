using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.ImageFormat.Types
{
	/*
	 * CPU Architecture Codes (supported by Linux)
	 *
	 * The following are exposed to uImage header.
	 * New IDs *MUST* be appended at the end of the list and *NEVER*
	 * inserted for backward compatibility.
	 */
	public enum CPU {
		IH_ARCH_INVALID = 0,    /* Invalid CPU	*/
		IH_ARCH_ALPHA,          /* Alpha	*/
		IH_ARCH_ARM,            /* ARM		*/
		IH_ARCH_I386,           /* Intel x86	*/
		IH_ARCH_IA64,           /* IA64		*/
		IH_ARCH_MIPS,           /* MIPS		*/
		IH_ARCH_MIPS64,         /* MIPS	 64 Bit */
		IH_ARCH_PPC,            /* PowerPC	*/
		IH_ARCH_S390,           /* IBM S390	*/
		IH_ARCH_SH,             /* SuperH	*/
		IH_ARCH_SPARC,          /* Sparc	*/
		IH_ARCH_SPARC64,        /* Sparc 64 Bit */
		IH_ARCH_M68K,           /* M68K		*/
		IH_ARCH_NIOS,           /* Nios-32	*/
		IH_ARCH_MICROBLAZE,     /* MicroBlaze   */
		IH_ARCH_NIOS2,          /* Nios-II	*/
		IH_ARCH_BLACKFIN,       /* Blackfin	*/
		IH_ARCH_AVR32,          /* AVR32	*/
		IH_ARCH_ST200,          /* STMicroelectronics ST200  */
		IH_ARCH_SANDBOX,        /* Sandbox architecture (test only) */
		IH_ARCH_NDS32,          /* ANDES Technology - NDS32  */
		IH_ARCH_OPENRISC,       /* OpenRISC 1000  */
		IH_ARCH_ARM64,          /* ARM64	*/
		IH_ARCH_ARC,            /* Synopsys DesignWare ARC */
		IH_ARCH_X86_64,         /* AMD x86_64, Intel and Via */
		IH_ARCH_XTENSA,         /* Xtensa	*/
		IH_ARCH_RISCV,          /* RISC-V */

		IH_ARCH_COUNT,
	};

}
