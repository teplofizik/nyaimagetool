using System;
using System.Collections.Generic;
using System.Text;

namespace CpioLib.Types
{
    [Flags]
    public enum CpioModeFlags
    {
        C_IRUSR = 0000400, // Read by owner
        C_IWUSR = 0000200, // Write by owner
        C_IXUSR = 0000100, // Execute by owner
        C_IRGRP = 0000040, // Read by group.
        C_IWGRP = 0000020, // Write by group
        C_IXGRP = 0000010, // Execute by group
        C_IROTH = 0000004, // Read by others
        C_IWOTH = 0000002, // Write by others
        C_IXOTH = 0000001, // Execute by others
        C_ISUID = 0004000, // Set user ID
        C_ISGID = 0002000, // Set group ID
        C_ISVTX = 0001000 // On directories, restricted deletion flag
    }
}
