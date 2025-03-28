namespace SortableStorage.ModSystem
{
    using System;
    using Vintagestory.API.Client;
    using Vintagestory.API.Common;
    using Vintagestory.API.Config;
    using Vintagestory.API.Datastructures;
    using Vintagestory.API.MathTools;
    using Vintagestory.API.Server;
    using Vintagestory.GameContent;
    using Vintagestory.API.Util;

    public class BESortableLabeledChest : BEGenericSortableTypedContainer
    {
        private string text = "";
        private ChestLabelRenderer labelrenderer;
        private int color;
        private int tempColor;
        private ItemStack tempStack;
        private float fontSize = 20;
        private GuiDialogBlockEntityTextInput editDialog;
        public override float MeshAngle
        {
            get => base.MeshAngle;
            set
            {
                this.labelrenderer?.SetRotation(value);
                base.MeshAngle = value;
            }
        }

        public override string DialogTitle
        {
            get
            {
                if (this.text == null || this.text.Length == 0)
                { return Lang.Get("Chest Contents"); }
                else
                {
                    return this.text.Replace("\r", "").Replace("\n", " ").Substring(0, Math.Min(this.text.Length, 15));
                }
            }
        }

        public BESortableLabeledChest()
        { }

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            if (api is ICoreClientAPI)
            {
                this.labelrenderer = new ChestLabelRenderer(this.Pos, api as ICoreClientAPI);
                this.labelrenderer.SetRotation(this.MeshAngle);
                this.labelrenderer.SetNewText(this.text, this.color);
            }
        }

        public override bool OnPlayerRightClick(IPlayer byPlayer, BlockSelection blockSel)
        {
            if (byPlayer?.Entity?.Controls?.ShiftKey == true)
            {
                var hotbarSlot = byPlayer.InventoryManager.ActiveHotbarSlot;
                if (hotbarSlot?.Itemstack?.ItemAttributes?["pigment"]?["color"].Exists == true)
                {
                    var jobj = hotbarSlot.Itemstack.ItemAttributes["pigment"]["color"];
                    var r = jobj["red"].AsInt();
                    var g = jobj["green"].AsInt();
                    var b = jobj["blue"].AsInt();

                    this.tempColor = ColorUtil.ToRgba(255, r, g, b);
                    this.tempStack = hotbarSlot.TakeOut(1);
                    hotbarSlot.MarkDirty();

                    var tmpPos = new BlockPos(this.Pos.X, this.Pos.Y, this.Pos.Z, 0);

                    if (this.Api is ICoreServerAPI sapi)
                    {
                        sapi.Network.SendBlockEntityPacket(
                            (IServerPlayer)byPlayer,
                            tmpPos,
                            (int)EnumSignPacketId.OpenDialog
                        );
                    }

                    return true;
                }
            }

            return base.OnPlayerRightClick(byPlayer, blockSel);
        }


        public override void OnReceivedClientPacket(IPlayer player, int packetid, byte[] data)
        {
            if (packetid == (int)EnumSignPacketId.SaveText)
            {
                var packet = SerializerUtil.Deserialize<EditSignPacket>(data);
                this.text = packet.Text;

                this.fontSize = packet.FontSize;
                this.color = this.tempColor;

                this.MarkDirty(true);

                // Tell server to save this chunk to disk again
                //this.Api.World.BlockAccessor.GetChunkAtBlockPos(this.Pos.X, this.Pos.Y, this.Pos.Z).MarkModified(); //1.19 removed
                 
                this.Api.World.BlockAccessor.GetChunkAtBlockPos(new BlockPos(this.Pos.X, this.Pos.Y, this.Pos.Z, 0)).MarkModified(); //1.19

                // 85% chance to get back the item
                if (this.Api.World.Rand.NextDouble() < 0.85)
                {
                    player.InventoryManager.TryGiveItemstack(this.tempStack);
                }
            }

            if (packetid == (int)EnumSignPacketId.CancelEdit && this.tempStack != null)
            {
                player.InventoryManager.TryGiveItemstack(this.tempStack);
                this.tempStack = null;
            }

            base.OnReceivedClientPacket(player, packetid, data);
        }
          

        public override void OnReceivedServerPacket(int packetid, byte[] data)
        {
            if (packetid == (int)EnumSignPacketId.OpenDialog)
            {
                if (this.editDialog != null && this.editDialog.IsOpened())
                { return; }

                this.editDialog = new GuiDialogBlockEntityTextInput("Edit Label text", this.Pos, this.text, this.Api as ICoreClientAPI, new TextAreaConfig() { MaxWidth = 130, MaxHeight = 160 }.CopyWithFontSize(this.fontSize))
                {
                    OnTextChanged = this.DidChangeTextClientSide,
                    OnCloseCancel = () =>
                    {
                        this.labelrenderer?.SetNewText(this.text, this.color);
                        var tmpPos = new BlockPos(this.Pos.X, this.Pos.Y, this.Pos.Z, 0);
                        (this.Api as ICoreClientAPI).Network.SendBlockEntityPacket(tmpPos, (int)EnumSignPacketId.CancelEdit, null);
                    }
                };
                this.editDialog.TryOpen();
            }


            if (packetid == (int)EnumSignPacketId.NowText)
            {
                var packet = SerializerUtil.Deserialize<EditSignPacket>(data);
                if (this.labelrenderer != null)
                {
                    this.labelrenderer.fontSize = packet.FontSize;
                    this.labelrenderer.SetNewText(packet.Text, this.color);
                }
            }

            base.OnReceivedServerPacket(packetid, data);
        }
           

        private void DidChangeTextClientSide(string text)
        {
            if (this.editDialog == null)
            { return; }
            this.fontSize = this.editDialog.FontSize;
            this.labelrenderer.fontSize = this.fontSize;
            this.labelrenderer?.SetNewText(text, this.tempColor);
        }
        

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            base.FromTreeAttributes(tree, worldForResolving);
            this.color = tree.GetInt("color");
            this.text = tree.GetString("text");
            this.fontSize = tree.GetFloat("fontSize", 20);
            this.labelrenderer?.SetNewText(this.text, this.color);
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetInt("color", this.color);
            tree.SetString("text", this.text);
            tree.SetFloat("fontSize", this.fontSize);
        }

        public override void OnBlockRemoved()
        {
            base.OnBlockRemoved();
            if (this.labelrenderer != null)
            {
                this.labelrenderer.Dispose();
                this.labelrenderer = null;
            }
            this.editDialog?.TryClose();
            this.editDialog?.Dispose();
        }

        public override void OnBlockBroken(IPlayer byPlayer = null)
        {
            base.OnBlockBroken(byPlayer);
            this.labelrenderer?.Dispose();
        }

        public override void OnBlockUnloaded()
        {
            base.OnBlockUnloaded();
            this.labelrenderer?.Dispose();

            this.editDialog?.TryClose();
            this.editDialog?.Dispose();
        }
    }
}
