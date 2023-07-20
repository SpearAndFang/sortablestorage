namespace SortableStorage.ModSystem
{
    using Vintagestory.API.Common;
    using Vintagestory.API.MathTools;

    public class BlockBehaviorSortableContainer : BlockBehavior
    {
        public BlockBehaviorSortableContainer(Block block) : base(block)
        { }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref EnumHandling handling)
        {
            //remove this if statement
            /*
            if (!byPlayer.Entity.Controls.Sneak)
            {
                handling = EnumHandling.PreventDefault;
                return false;
            }
            */
            handling = EnumHandling.PreventSubsequent;
            var entity = world.BlockAccessor.GetBlockEntity(blockSel.Position);
            if (entity is BESortableOpenableContainer beContainer)
            {
                return beContainer.OnPlayerRightClick(byPlayer, blockSel);
            }
            return false;
        }

        public override void OnBlockRemoved(IWorldAccessor world, BlockPos pos, ref EnumHandling handling)
        {
            handling = EnumHandling.PassThrough;
            var entity = world.BlockAccessor.GetBlockEntity(pos);
            if (entity is BESortableOpenableContainer container)
            {
                var players = world.AllOnlinePlayers;
                for (var i = 0; i < players.Length; i++)
                {
                    if (players[i].InventoryManager.HasInventory(container.Inventory))
                    {
                        players[i].InventoryManager.CloseInventory(container.Inventory);
                    }
                }
            }
        }
    }
}
