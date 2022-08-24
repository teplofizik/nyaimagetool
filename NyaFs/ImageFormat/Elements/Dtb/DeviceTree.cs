using NyaFs.FlattenedDeviceTree;
using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.ImageFormat.Elements.Dtb
{
    public class DeviceTree
    {
        /// <summary>
        /// Image information, arch or supported os
        /// </summary>
        public Types.ImageInfo Info = new Types.ImageInfo();

        /// <summary>
        /// Loaded device tree
        /// </summary>
        public FlattenedDeviceTree.FlattenedDeviceTree DevTree = new FlattenedDeviceTree.FlattenedDeviceTree();

        /// <summary>
        /// Is device tree loaded
        /// </summary>
        public bool Loaded => DevTree.Root.Nodes.Count > 0;

        public DeviceTree()
        {

        }
    }
}
