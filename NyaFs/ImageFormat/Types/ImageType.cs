using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.ImageFormat.Types
{
	/*
	 * Image Types
	 *
	 * "Standalone Programs" are directly runnable in the environment
	 *	provided by U-Boot; it is expected that (if they behave
	 *	well) you can continue to work in U-Boot after return from
	 *	the Standalone Program.
	 * "OS Kernel Images" are usually images of some Embedded OS which
	 *	will take over control completely. Usually these programs
	 *	will install their own set of exception handlers, device
	 *	drivers, set up the MMU, etc. - this means, that you cannot
	 *	expect to re-enter U-Boot except by resetting the CPU.
	 * "RAMDisk Images" are more or less just data blocks, and their
	 *	parameters (address, size) are passed to an OS kernel that is
	 *	being started.
	 * "Multi-File Images" contain several images, typically an OS
	 *	(Linux) kernel image and one or more data images like
	 *	RAMDisks. This construct is useful for instance when you want
	 *	to boot over the network using BOOTP etc., where the boot
	 *	server provides just a single image file, but you want to get
	 *	for instance an OS kernel and a RAMDisk image.
	 *
	 *	"Multi-File Images" start with a list of image sizes, each
	 *	image size (in bytes) specified by an "uint32_t" in network
	 *	byte order. This list is terminated by an "(uint32_t)0".
	 *	Immediately after the terminating 0 follow the images, one by
	 *	one, all aligned on "uint32_t" boundaries (size rounded up to
	 *	a multiple of 4 bytes - except for the last file).
	 *
	 * "Firmware Images" are binary images containing firmware (like
	 *	U-Boot or FPGA images) which usually will be programmed to
	 *	flash memory.
	 *
	 * "Script files" are command sequences that will be executed by
	 *	U-Boot's command interpreter; this feature is especially
	 *	useful when you configure U-Boot to use a real shell (hush)
	 *	as command interpreter (=> Shell Scripts).
	 *
	 * The following are exposed to uImage header.
	 * New IDs *MUST* be appended at the end of the list and *NEVER*
	 * inserted for backward compatibility.
	 */

	public enum ImageType
	{
		IH_TYPE_INVALID = 0,    /* Invalid Image		*/
		IH_TYPE_STANDALONE,     /* Standalone Program		*/
		IH_TYPE_KERNEL,         /* OS Kernel Image		*/
		IH_TYPE_RAMDISK,        /* RAMDisk Image		*/
		IH_TYPE_MULTI,          /* Multi-File Image		*/
		IH_TYPE_FIRMWARE,       /* Firmware Image		*/
		IH_TYPE_SCRIPT,         /* Script file			*/
		IH_TYPE_FILESYSTEM,     /* Filesystem Image (any type)	*/
		IH_TYPE_FLATDT,         /* Binary Flat Device Tree Blob	*/
		IH_TYPE_KWBIMAGE,       /* Kirkwood Boot Image		*/
		IH_TYPE_IMXIMAGE,       /* Freescale IMXBoot Image	*/
		IH_TYPE_UBLIMAGE,       /* Davinci UBL Image		*/
		IH_TYPE_OMAPIMAGE,      /* TI OMAP Config Header Image	*/
		IH_TYPE_AISIMAGE,       /* TI Davinci AIS Image		*/
		/* OS Kernel Image, can run from any load address */
		IH_TYPE_KERNEL_NOLOAD,
		IH_TYPE_PBLIMAGE,       /* Freescale PBL Boot Image	*/
		IH_TYPE_MXSIMAGE,       /* Freescale MXSBoot Image	*/
		IH_TYPE_GPIMAGE,        /* TI Keystone GPHeader Image	*/
		IH_TYPE_ATMELIMAGE,     /* ATMEL ROM bootable Image	*/
		IH_TYPE_SOCFPGAIMAGE,       /* Altera SOCFPGA CV/AV Preloader */
		IH_TYPE_X86_SETUP,      /* x86 setup.bin Image		*/
		IH_TYPE_LPC32XXIMAGE,       /* x86 setup.bin Image		*/
		IH_TYPE_LOADABLE,       /* A list of typeless images	*/
		IH_TYPE_RKIMAGE,        /* Rockchip Boot Image		*/
		IH_TYPE_RKSD,           /* Rockchip SD card		*/
		IH_TYPE_RKSPI,          /* Rockchip SPI image		*/
		IH_TYPE_ZYNQIMAGE,      /* Xilinx Zynq Boot Image */
		IH_TYPE_ZYNQMPIMAGE,        /* Xilinx ZynqMP Boot Image */
		IH_TYPE_ZYNQMPBIF,      /* Xilinx ZynqMP Boot Image (bif) */
		IH_TYPE_FPGA,           /* FPGA Image */
		IH_TYPE_VYBRIDIMAGE,    /* VYBRID .vyb Image */
		IH_TYPE_TEE,            /* Trusted Execution Environment OS Image */
		IH_TYPE_FIRMWARE_IVT,       /* Firmware Image with HABv4 IVT */
		IH_TYPE_PMMC,            /* TI Power Management Micro-Controller Firmware */
		IH_TYPE_STM32IMAGE,     /* STMicroelectronics STM32 Image */
		IH_TYPE_SOCFPGAIMAGE_V1,    /* Altera SOCFPGA A10 Preloader	*/
		IH_TYPE_MTKIMAGE,       /* MediaTek BootROM loadable Image */
		IH_TYPE_IMX8MIMAGE,     /* Freescale IMX8MBoot Image	*/
		IH_TYPE_IMX8IMAGE,      /* Freescale IMX8Boot Image	*/
		IH_TYPE_COPRO,          /* Coprocessor Image for remoteproc*/
		IH_TYPE_SUNXI_EGON,     /* Allwinner eGON Boot Image */
		IH_TYPE_SUNXI_TOC0,     /* Allwinner TOC0 Boot Image */

		IH_TYPE_COUNT,          /* Number of image types */
	};
}
