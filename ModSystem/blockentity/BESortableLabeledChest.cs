namespace SortableStorage.ModSystem
{
    using System;
    //using System.IO; //1.18 removed
    using Vintagestory.API.Client;
    using Vintagestory.API.Common;
    using Vintagestory.API.Config;
    using Vintagestory.API.Datastructures;
    using Vintagestory.API.MathTools;
    using Vintagestory.API.Server;
    using Vintagestory.GameContent;
    using Vintagestory.API.Util; //1.18 added

    public class BESortableLabeledChest : BEGenericSortableTypedContainer
    {
        private string text = "";
        private ChestLabelRenderer labelrenderer;
        private int color;
        private int tempColor;
        private ItemStack tempStack;
        //private GuiDialogBlockEntityTextInput textdialog; //1.18 removed
        private float fontSize = 20; //1.18 added
        private GuiDialogBlockEntityTextInput editDialog; //1.18 added
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

                    if (this.Api is ICoreServerAPI sapi)
                    {
                        sapi.Network.SendBlockEntityPacket(
                            (IServerPlayer)byPlayer,
                            this.Pos.X, this.Pos.Y, this.Pos.Z,
                            (int)EnumSignPacketId.OpenDialog
                        );
                    }

                    return true;
                }
            }

            return base.OnPlayerRightClick(byPlayer, blockSel);
        }
        // 1.18 replaced
        /*
        public override bool OnPlayerRightClick(IPlayer byPlayer, BlockSelection blockSel)
        {
            if (byPlayer?.Entity?.Controls?.Sneak == true)
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

                    if (this.Api.World is IServerWorldAccessor)
                    {
                        byte[] data;
                        using (var ms = new MemoryStream())
                        {
                            var writer = new BinaryWriter(ms);
                            writer.Write("BlockEntityTextInput");
                            writer.Write("Edit chest label text");
                            writer.Write(this.text);
                            data = ms.ToArray();
                        }

                        ((ICoreServerAPI)this.Api).Network.SendBlockEntityPacket(
                            (IServerPlayer)byPlayer,
                            this.Pos.X, this.Pos.Y, this.Pos.Z,
                            (int)EnumSignPacketId.OpenDialog,
                            data
                        );
                    }
                    return true;
                }
            }
            return base.OnPlayerRightClick(byPlayer, blockSel);
        }
        */
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
                this.Api.World.BlockAccessor.GetChunkAtBlockPos(this.Pos.X, this.Pos.Y, this.Pos.Z).MarkModified();

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

        // 1.18 replaced
        /*
        public override void OnReceivedClientPacket(IPlayer player, int packetid, byte[] data)
        {
            if (packetid == (int)EnumSignPacketId.SaveText)
            {
                using (var ms = new MemoryStream(data))
                {
                    var reader = new BinaryReader(ms);
                    this.text = reader.ReadString();
                    if (this.text == null)
                        this.text = "";
                }

                this.color = this.tempColor;
                this.MarkDirty(true);
                this.Api.World.BlockAccessor.GetChunkAtBlockPos(this.Pos.X, this.Pos.Y, this.Pos.Z).MarkModified();

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
        */


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
                        (this.Api as ICoreClientAPI).Network.SendBlockEntityPacket(this.Pos.X, this.Pos.Y, this.Pos.Z, (int)EnumSignPacketId.CancelEdit, null);
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

        // 1.18 replaced
        /*
        public override void OnReceivedServerPacket(int packetid, byte[] data)
        {
            if (packetid == (int)EnumSignPacketId.OpenDialog)
            {
                using (var ms = new MemoryStream(data))
                {
                    var reader = new BinaryReader(ms);
                    var dialogClassName = reader.ReadString();
                    var dialogTitle = reader.ReadString();
                    this.text = reader.ReadString();
                    if (this.text == null)
                        this.text = "";

                    var clientWorld = (IClientWorldAccessor)this.Api.World;
                    this.textdialog = new GuiDialogBlockEntityTextInput(dialogTitle, this.Pos, this.text, this.Api as ICoreClientAPI, 115, 4)
                    {
                        OnTextChanged = this.DidChangeTextClientSide,
                        OnCloseCancel = () =>
                        {
                            this.labelrenderer?.SetNewText(this.text, this.color);
                            (this.Api as ICoreClientAPI).Network.SendBlockEntityPacket(this.Pos.X, this.Pos.Y, this.Pos.Z, (int)EnumSignPacketId.CancelEdit, null);
                        }
                    };
                    this.textdialog.TryOpen();
                }
            }

            if (packetid == (int)EnumSignPacketId.NowText)
            {
                using (var ms = new MemoryStream(data))
                {
                    var reader = new BinaryReader(ms);
                    this.text = reader.ReadString();
                    if (this.text == null)
                    { this.text = ""; }

                    if (this.labelrenderer != null)
                    { this.labelrenderer.SetNewText(this.text, this.color); }
                }
            }
            base.OnReceivedServerPacket(packetid, data);
        }
        */

        private void DidChangeTextClientSide(string text)
        {
            if (this.editDialog == null)
            { return; }
            this.fontSize = this.editDialog.FontSize;
            this.labelrenderer.fontSize = this.fontSize;
            this.labelrenderer?.SetNewText(text, this.tempColor);
        }
        // 1.18 replaced
        /*
        private void DidChangeTextClientSide(string text)
        { this.labelrenderer?.SetNewText(text, this.tempColor); }
        */

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            base.FromTreeAttributes(tree, worldForResolving);
            this.color = tree.GetInt("color");
            this.text = tree.GetString("text");
            this.fontSize = tree.GetFloat("fontSize", 20); //1.18 added
            this.labelrenderer?.SetNewText(this.text, this.color);
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetInt("color", this.color);
            tree.SetString("text", this.text);
            tree.SetFloat("fontSize", this.fontSize); //1.18 added
        }

        public override void OnBlockRemoved()
        {
            base.OnBlockRemoved();
            if (this.labelrenderer != null)
            {
                this.labelrenderer.Dispose();
                this.labelrenderer = null;
            }
            //this.textdialog?.TryClose(); //1.18 removed
            //this.textdialog?.Dispose(); //1.18 removed
            this.editDialog?.TryClose(); //1.18 added
            this.editDialog?.Dispose(); //1.18 added
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

            //this.textdialog?.TryClose(); //1.18 removed
            //this.textdialog?.Dispose(); //1.18 removed
            this.editDialog?.TryClose(); //1.18 added
            this.editDialog?.Dispose(); //1.18 added
        }
    }
}
