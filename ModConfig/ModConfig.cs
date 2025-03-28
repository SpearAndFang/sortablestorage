namespace SortableStorage.ModConfig
{
    public class ModConfig
    {
        public static ModConfig Loaded { get; set; } = new ModConfig();

        public bool useVanillaTextures { get; set; } = false;
        public bool sortByDisplayName { get; set; } = false;
    }
}
