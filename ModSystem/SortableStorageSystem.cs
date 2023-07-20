namespace SortableStorage.ModSystem
{
    using Vintagestory.API.Common;

    public class SortableStorageSystem : ModSystem
    {
        public override bool ShouldLoad(EnumAppSide forSide)
        { return true; }



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
    }
}
