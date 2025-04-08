namespace SortableStorage.ModSystem
{
    using Vintagestory.API.Common;
    using Vintagestory.API.Client;
    using SortableStorage.ModConfig;
    using HarmonyLib;


    public class SortableStorageSystem : ModSystem
    {
        const string harmonyId = "com.spearandfang.sortablestorage";

        private readonly Harmony harmony = new Harmony(harmonyId);

        public override bool ShouldLoad(EnumAppSide forSide)
        { return true; }

        public override void Dispose()
        {
            harmony.UnpatchAll(harmonyId);
            base.Dispose();
        }


        public override void StartPre(ICoreAPI api)
        {
            var cfgFileName = "sortablestorage.json";
            try
            {
                ModConfig fromDisk;
                if ((fromDisk = api.LoadModConfig<ModConfig>(cfgFileName)) == null)
                { api.StoreModConfig(ModConfig.Loaded, cfgFileName); }
                else
                { ModConfig.Loaded = fromDisk; }
            }
            catch
            { api.StoreModConfig(ModConfig.Loaded, cfgFileName); }

            api.World.Config.SetBool("useVanillaTextures", ModConfig.Loaded.useVanillaTextures);
            api.World.Config.SetBool("sortByDisplayName", ModConfig.Loaded.sortByDisplayName);
            base.StartPre(api);
        }


        public override void Start(ICoreAPI api)
        {
            base.Start(api);

            if (!Harmony.HasAnyPatches(harmonyId)) harmony.PatchAllUncategorized();

            api.World.Logger.Event("started 'Sortable Storage' mod");
            api.RegisterBlockClass("BlockGenericSortableTypedContainer", typeof(BlockGenericSortableTypedContainer));
            api.RegisterBlockClass("BlockSortableLabeledChest", typeof(BlockSortableLabeledChest));
            api.RegisterBlockClass("BlockGenericSortableTypedContainerTrunk", typeof(BlockGenericSortableTypedContainerTrunk));

            api.RegisterBlockBehaviorClass("SortableContainer", typeof(BlockBehaviorSortableContainer));

            api.RegisterBlockEntityClass("BEGenericSortableTypedContainer", typeof(BEGenericSortableTypedContainer));
            api.RegisterBlockEntityClass("BESortableLabeledChest", typeof(BESortableLabeledChest));
            api.RegisterBlockEntityClass("BESortableOpenableContainer", typeof(BESortableOpenableContainer));
        }

    }
}
