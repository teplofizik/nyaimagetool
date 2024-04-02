using NyaFs.Processor;
using NyaFs.Processor.Scripting;
using NyaFs.Processor.Scripting.Templates;
using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFsIncmEncrypt
{
    class IncmFileProcessScriptStep : FileProcessScriptStep
    {
        static string[] Paths =
        {
            "etc",
            "bin",
            "sbin",
            "usr/bin",
            "usr/sbin"
        };

        public IncmFileProcessScriptStep(string Name) : base(Name, Paths)
        {

        }

        /// <summary>
        /// Check is need to process file
        /// </summary>
        /// <param name="Path"></param>
        /// <returns></returns>
        protected override bool IsNeedProcessFile(string Path)
        {
            if (Path.StartsWith("etc/inittab"))
                return false;
            if (Path == "sbin/init")
                return false;
            if (Path == "bin/busybox")
                return false;

            return true;
        }
    }
}
