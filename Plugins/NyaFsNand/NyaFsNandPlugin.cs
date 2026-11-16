using NyaFs.Processor.Scripting;
using NyaFs.Processor.Scripting.Plugins;

namespace NyaFsNand
{
    /// <summary>
    /// Command plugin: `nandinfo` reports geometry, bad blocks, regions and UBI volumes
    /// for raw NAND dumps and logical MTD images.
    /// </summary>
    public class NyaFsNandPlugin : CommandPlugin
    {
        public NyaFsNandPlugin() : base("nand")
        {
        }

        public override ScriptStepGenerator[] GetGenerators() =>
            new ScriptStepGenerator[] { new Commands.NandInfo() };
    }
}
