namespace SortableStorage.ModSystem
{
    using Vintagestory.API.Common;
    using Vintagestory.API.Client;
    using HarmonyLib;


    public class SortableStorageSystem : ModSystem
    {
        private readonly Harmony harmony = new Harmony("com.spearandfang.sortablestorage");

        public override bool ShouldLoad(EnumAppSide forSide)
        { return true; }

        public override void StartClientSide(ICoreClientAPI api)
        {
            base.StartClientSide(api);
            var SSBlockGetPlacedBlockInfoOriginal = typeof(Block).GetMethod(nameof(Block.GetPlacedBlockInfo));
            var SSBlockGetPlacedBlockInfoPostfix = typeof(SS_BlockGetPlacedBlockInfo_Patch).GetMethod(nameof(SS_BlockGetPlacedBlockInfo_Patch.SSBlockGetPlacedBlockInfoPostfix));
            this.harmony.Patch(SSBlockGetPlacedBlockInfoOriginal, postfix: new HarmonyMethod(SSBlockGetPlacedBlockInfoPostfix));

        }

        public override void Dispose()
        {
            var SSBlockGetPlacedBlockInfoOriginal = typeof(Block).GetMethod(nameof(Block.GetPlacedBlockInfo));
            this.harmony.Unpatch(SSBlockGetPlacedBlockInfoOriginal, HarmonyPatchType.Postfix, "*");
            base.Dispose();
        }

        public override void Start(ICoreAPI api)
        {
            base.Start(api);
            api.World.Logger.Event("started 'Sortable Storage' mod");
            api.RegisterBlockClass("BlockGenericSortableTypedContainer", typeof(BlockGenericSortableTypedContainer));
            api.RegisterBlockClass("BlockSortableLabeledChest", typeof(BlockSortableLabeledChest));
            api.RegisterBlockClass("BlockGenericSortableTypedContainerTrunk", typeof(BlockGenericSortableTypedContainerTrunk));

            api.RegisterBlockBehaviorClass("SortableContainer", typeof(BlockBehaviorSortableContainer));

            api.RegisterBlockEntityClass("BEGenericSortableTypedContainer", typeof(BEGenericSortableTypedContainer));
            api.RegisterBlockEntityClass("BESortableLabeledChest", typeof(BESortableLabeledChest));
            api.RegisterBlockEntityClass("BESortableOpenableContainer", typeof(BESortableOpenableContainer));

            api.RegisterItemClass("ItemCoalPatched", typeof(ItemCoalPatched));
            api.RegisterItemClass("ItemPileablePatched", typeof(ItemPileablePatched));
        }


        public class SS_BlockGetPlacedBlockInfo_Patch
        {
            [HarmonyPostfix]
            public static void SSBlockGetPlacedBlockInfoPostfix(ref string __result, IPlayer forPlayer)
            {
                var domain = forPlayer.Entity?.BlockSelection?.Block?.Code?.Domain;
                if (domain != null)
                {
                    if (domain == "sortablestorage")
                    {
                        __result += "\n\n<font color=\"#E8DAEF\"><i>Sortable Storage</i></font>\n\n";
                    }
                }
            }
        }

    }
}
