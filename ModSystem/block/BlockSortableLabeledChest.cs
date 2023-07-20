namespace SortableStorage.ModSystem
{
    using System.Collections.Generic;
    using System.Linq;
    using Vintagestory.API.Client;
    using Vintagestory.API.Common;
    using Vintagestory.API.Util;

    public class BlockSortableLabeledChest : BlockGenericSortableTypedContainer, ITexPositionSource
    {
        private WorldInteraction[] interactions;

        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);

            if (api.Side != EnumAppSide.Client)
            { return; }

            this.interactions = ObjectCacheUtil.GetOrCreate(api, "signBlockInteractions", () =>
            {
                var stacksList = new List<ItemStack>();

                foreach (var collectible in api.World.Collectibles)
                {
                    if (collectible.Attributes?["pigment"].Exists == true)
                    {
                        stacksList.Add(new ItemStack(collectible));
                    }
                }

                return new WorldInteraction[] {
                    new WorldInteraction()
                    {
                        ActionLangCode = "blockhelp-sign-write",
                        HotKeyCode = "sneak",
                        MouseButton = EnumMouseButton.Right,
                        Itemstacks = stacksList.ToArray()
                    }
                };
            });
        }



        public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
        {
            return this.interactions.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
        }
    }
}
