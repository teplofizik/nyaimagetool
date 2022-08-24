using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.ImageFormat.Types
{

	/*
	 * Compression Types
	 *
	 * The following are exposed to uImage header.
	 * New IDs *MUST* be appended at the end of the list and *NEVER*
	 * inserted for backward compatibility.
	 */
	public enum CompressionType
	{
		IH_COMP_NONE = 0,   /*  No	 Compression Used	*/
		IH_COMP_GZIP,           /* gzip	 Compression Used	*/
		IH_COMP_BZIP2,          /* bzip2 Compression Used	*/
		IH_COMP_LZMA,           /* lzma  Compression Used	*/
		IH_COMP_LZO,            /* lzo   Compression Used	*/
		IH_COMP_LZ4,            /* lz4   Compression Used	*/
		IH_COMP_ZSTD,           /* zstd   Compression Used	*/

		IH_COMP_COUNT,
	};
}
