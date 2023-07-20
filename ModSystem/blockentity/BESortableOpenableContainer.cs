namespace SortableStorage.ModSystem
{
    using System.IO;
    using System.Text;
    using Vintagestory.API.Client;
    using Vintagestory.API.Common;
    using Vintagestory.API.Datastructures;
    using Vintagestory.GameContent;

    public enum EnumBlockContainerPacketId
    { OpenInventory = 5000 }

    public abstract class BESortableOpenableContainer : BlockEntityContainer, IBlockEntityContainer
    {
        protected GuiDialogSortableBlockEntityInventory invDialog;

        public virtual AssetLocation OpenSound { get; set; } = new AssetLocation("game:sounds/block/chestopen");
        public virtual AssetLocation CloseSound { get; set; } = new AssetLocation("game:sounds/block/chestclose");

        public abstract bool OnPlayerRightClick(IPlayer byPlayer, BlockSelection blockSel);


        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            this.Inventory.LateInitialize(this.InventoryClassName + "-" + this.Pos.X + "/" + this.Pos.Y + "/" + this.Pos.Z, api);
            this.Inventory.ResolveBlocksOrItems();
            var os = this.Block.Attributes?["openSound"]?.AsString();
            var cs = this.Block.Attributes?["closeSound"]?.AsString();
            var opensound = os == null ? null : AssetLocation.Create(os, this.Block.Code.Domain);
            var closesound = cs == null ? null : AssetLocation.Create(cs, this.Block.Code.Domain);
            this.OpenSound = opensound ?? this.OpenSound;
            this.CloseSound = closesound ?? this.CloseSound;
        }


        public override void OnReceivedClientPacket(IPlayer player, int packetid, byte[] data)
        {
            if (packetid < 1000)
            {
                this.Inventory.InvNetworkUtil.HandleClientPacket(player, packetid, data);
                this.Api.World.BlockAccessor.GetChunkAtBlockPos(this.Pos.X, this.Pos.Y, this.Pos.Z).MarkModified();
                return;
            }

            if (packetid == (int)EnumBlockEntityPacketId.Close)
            {
                if (player.InventoryManager != null)
                { player.InventoryManager.CloseInventory(this.Inventory); }
            }
        }


        public override void OnReceivedServerPacket(int packetid, byte[] data)
        {
            var clientWorld = (IClientWorldAccessor)this.Api.World;

            if (packetid == (int)EnumBlockContainerPacketId.OpenInventory)
            {
                if (this.invDialog != null)
                {
                    if (this.invDialog?.IsOpened() == true)
                    { this.invDialog.TryClose(); }
                    this.invDialog?.Dispose();
                    this.invDialog = null;
                    return;
                }

                string dialogClassName;
                string dialogTitle;
                int cols;
                var tree = new TreeAttribute();

                using (var ms = new MemoryStream(data))
                {
                    var reader = new BinaryReader(ms);
                    dialogClassName = reader.ReadString();
                    dialogTitle = reader.ReadString();
                    cols = reader.ReadByte();
                    tree.FromBytes(reader);
                }

                this.Inventory.FromTreeAttributes(tree);
                this.Inventory.ResolveBlocksOrItems();

                this.invDialog = new GuiDialogSortableBlockEntityInventory(dialogTitle, this.Inventory, this.Pos, cols, this.Api as ICoreClientAPI);

                var block = this.Api.World.BlockAccessor.GetBlock(this.Pos, BlockLayersAccess.Default);
                var os = block.Attributes?["openSound"]?.AsString();
                var cs = block.Attributes?["closeSound"]?.AsString();
                var opensound = os == null ? null : AssetLocation.Create(os, block.Code.Domain);
                var closesound = cs == null ? null : AssetLocation.Create(cs, block.Code.Domain);

                this.invDialog.OpenSound = opensound ?? this.OpenSound;
                this.invDialog.CloseSound = closesound ?? this.CloseSound;
                this.invDialog.TryOpen();
            }

            if (packetid == (int)EnumBlockEntityPacketId.Close)
            {
                clientWorld.Player.InventoryManager.CloseInventory(this.Inventory);
                if (this.invDialog?.IsOpened() == true)
                { this.invDialog?.TryClose(); }
                this.invDialog?.Dispose();
                this.invDialog = null;
            }
        }


        public override void OnBlockUnloaded()
        {
            base.OnBlockUnloaded();
            if (this.invDialog?.IsOpened() == true)
            { this.invDialog?.TryClose(); }
            this.invDialog?.Dispose();
        }


        public override void OnBlockRemoved()
        {
            base.OnBlockRemoved();
            if (this.invDialog?.IsOpened() == true)
            { this.invDialog?.TryClose(); }
            this.invDialog?.Dispose();
        }


        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        { base.GetBlockInfo(forPlayer, dsc); }
    }
}
