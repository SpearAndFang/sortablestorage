namespace SortableStorage.ModSystem
{
    using Vintagestory.API.Client;
    using Vintagestory.API.Common;
    //using Vintagestory.API.Common.Entities;
    using Vintagestory.API.MathTools;
    using Vintagestory.API.Util;
    using Vintagestory.GameContent;

    public abstract class ItemPileablePatched : Item
    {
        protected abstract AssetLocation PileBlockCode { get; }

        public virtual bool IsPileable => true;

        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
        {
            if (!this.IsPileable)
            {
                base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);
                return;
            }

            if (blockSel == null || byEntity?.World == null || !byEntity.Controls.Sneak)
            {
                base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);
                return;
            }

            var onBlockPos = blockSel.Position;

            IPlayer byPlayer = null;
            if (byEntity is EntityPlayer player)
            { byPlayer = byEntity.World.PlayerByUid(player.PlayerUID); }
            if (byPlayer == null)
            { return; }

            if (!byEntity.World.Claims.TryAccess(byPlayer, blockSel.Position, EnumBlockAccessFlags.BuildOrBreak))
            {
                return;
            }

            var be = byEntity.World.BlockAccessor.GetBlockEntity(onBlockPos);

            if (be is BlockEntityLabeledChest || be is BESortableLabeledChest || be is BlockEntitySignPost || be is BlockEntitySign || be is BlockEntityBloomery || be is BlockEntityFirepit || be is BlockEntityForge || be is BlockEntityCrate)
            { return; }

            if (be is IBlockEntityItemPile)
            {
                var pile = (IBlockEntityItemPile)be;
                if (pile.OnPlayerInteract(byPlayer))
                {
                    handling = EnumHandHandling.PreventDefaultAction;

                    ((byEntity as EntityPlayer)?.Player as IClientPlayer)?.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
                    return;
                }
            }

            be = byEntity.World.BlockAccessor.GetBlockEntity(onBlockPos.AddCopy(blockSel.Face));
            if (be is IBlockEntityItemPile)
            {
                var pile = (IBlockEntityItemPile)be;
                if (pile.OnPlayerInteract(byPlayer))
                {
                    handling = EnumHandHandling.PreventDefaultAction;

                    ((byEntity as EntityPlayer)?.Player as IClientPlayer)?.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
                    return;
                }
            }

            var block = byEntity.World.GetBlock(this.PileBlockCode);
            if (block == null)
            { return; }
            var pos = onBlockPos.Copy();
            if (byEntity.World.BlockAccessor.GetBlock(pos, BlockLayersAccess.Default).Replaceable < 6000)
            { pos.Add(blockSel.Face); }

            var ok = ((IBlockItemPile)block).Construct(slot, byEntity.World, pos, byPlayer);

            var collisionBoxes = byEntity.World.BlockAccessor.GetBlock(pos, BlockLayersAccess.Default).GetCollisionBoxes(byEntity.World.BlockAccessor, pos);

            if (collisionBoxes != null && collisionBoxes.Length > 0 && CollisionTester.AabbIntersect(collisionBoxes[0], pos.X, pos.Y, pos.Z, byPlayer.Entity.SelectionBox, byPlayer.Entity.SidedPos.XYZ))
            {
                byPlayer.Entity.SidedPos.Y += collisionBoxes[0].Y2 - (byPlayer.Entity.SidedPos.Y - (int)byPlayer.Entity.SidedPos.Y);
            }

            if (ok)
            {
                handling = EnumHandHandling.PreventDefaultAction;
                ((byEntity as EntityPlayer)?.Player as IClientPlayer)?.TriggerFpAnimation(EnumHandInteract.HeldItemInteract);
            }
            else
            {
                base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);
            }


        }


        public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot)
        {
            if (!this.IsPileable)
            {
                return base.GetHeldInteractionHelp(inSlot);
            }

            return new WorldInteraction[]
            {
                new WorldInteraction
                {
                    HotKeyCode = "sneak",
                    ActionLangCode = "heldhelp-place",
                    MouseButton = EnumMouseButton.Right
                }
            }.Append(base.GetHeldInteractionHelp(inSlot));
        }


    }
}
