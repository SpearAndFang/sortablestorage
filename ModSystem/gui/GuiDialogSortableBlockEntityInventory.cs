namespace SortableStorage.ModSystem
{
    using System;
    using Vintagestory.API.Client;
    using Vintagestory.API.Common;
    using Vintagestory.API.MathTools;
    using System.Collections.Generic;
    using Vintagestory.API.Config;
    using Cairo;
    using SortableStorage.ModConfig;

    //using System.Diagnostics;

    public class GuiDialogSortableBlockEntityInventory : GuiDialogBlockEntity
    {
        private readonly EnumPosFlag screenPos;
        private readonly LoadedTexture sortButtonTexture;
        public override double DrawOrder => 0.2;
        public bool sortByDisplayName = ModConfig.Loaded.sortByDisplayName;

        private readonly ElementBounds sortButtonBounds;
        public GuiDialogSortableBlockEntityInventory(string dialogTitle, InventoryBase inventory, BlockPos blockEntityPos, int cols, ICoreClientAPI capi)
            : base(dialogTitle, inventory, blockEntityPos, capi)
        {
            if (this.IsDuplicate)
            { return; }

            var elemToDlgPad = GuiStyle.ElementToDialogPadding;
            var pad = GuiElementItemSlotGridBase.unscaledSlotPadding;
            var rows = (int)Math.Ceiling(inventory.Count / (float)cols);
            var visibleRows = Math.Min(rows, 7);

            var slotGridBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, pad, pad, cols, visibleRows);
            var fullGridBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0, 0, cols, rows);
            var insetBounds = slotGridBounds.ForkBoundingParent(6, 6, 6, 6);

            this.sortButtonBounds = ElementBounds.Fixed(EnumDialogArea.RightFixed, -58, 4, 10, 26);
            this.sortButtonTexture = capi.Gui.LoadSvg(new AssetLocation("sortablestorage:textures/sort.svg"), 50, 50, 50, 50, ColorUtil.ColorFromRgba(255, 255, 255, 255));
            this.screenPos = this.GetFreePos("smallblockgui");

            if (visibleRows < rows)
            {
                var clippingBounds = slotGridBounds.CopyOffsetedSibling();
                clippingBounds.fixedHeight -= 3;

                var dialogBounds = insetBounds
                    .ForkBoundingParent(elemToDlgPad, elemToDlgPad + 30, elemToDlgPad + 20, elemToDlgPad)
                    .WithFixedAlignmentOffset(this.IsRight(this.screenPos) ? -GuiStyle.DialogToScreenPadding : GuiStyle.DialogToScreenPadding, 0)
                    .WithAlignment(this.IsRight(this.screenPos) ? EnumDialogArea.RightMiddle : EnumDialogArea.LeftMiddle);

                if (!capi.Settings.Bool["immersiveMouseMode"])
                {
                    dialogBounds.fixedOffsetY += (dialogBounds.fixedHeight + 10) * this.YOffsetMul(this.screenPos);
                }

                var scrollbarBounds = ElementStdBounds.VerticalScrollbar(insetBounds).WithParent(dialogBounds);

                this.SingleComposer = capi.Gui
                    .CreateCompo("blockentityinventory" + blockEntityPos, dialogBounds)
                    .AddShadedDialogBG(ElementBounds.Fill)
                    .AddDialogTitleBar(dialogTitle, this.CloseIconPressed)
                    .AddInset(insetBounds)
                    .AddSortButtonIcon(capi, this.sortButtonTexture, this.OnSortClick, this.sortButtonBounds, null)
                    .AddVerticalScrollbar(this.OnNewScrollbarvalue, scrollbarBounds, "scrollbar")
                    .BeginClip(clippingBounds)
                    .AddItemSlotGrid(inventory, this.DoSendPacket, cols, fullGridBounds, "slotgrid")
                    .EndClip()
                    .Compose();

                this.SingleComposer.GetScrollbar("scrollbar").SetHeights(
                    (float)slotGridBounds.fixedHeight,
                    (float)(fullGridBounds.fixedHeight + pad)
                );

            }
            else
            {
                var dialogBounds = insetBounds
                    .ForkBoundingParent(elemToDlgPad, elemToDlgPad + 30, elemToDlgPad, elemToDlgPad)
                    .WithFixedAlignmentOffset(this.IsRight(this.screenPos) ? -GuiStyle.DialogToScreenPadding : GuiStyle.DialogToScreenPadding, 0)
                    .WithAlignment(this.IsRight(this.screenPos) ? EnumDialogArea.RightMiddle : EnumDialogArea.LeftMiddle);

                if (!capi.Settings.Bool["immersiveMouseMode"])
                {
                    dialogBounds.fixedOffsetY += (dialogBounds.fixedHeight + 10) * this.YOffsetMul(this.screenPos);
                }

                this.SingleComposer = capi.Gui
                    .CreateCompo("blockentityinventory" + blockEntityPos, dialogBounds)
                    .AddShadedDialogBG(ElementBounds.Fill)
                    .AddDialogTitleBar(dialogTitle, this.CloseIconPressed)
                    .AddInset(insetBounds)
                    .AddItemSlotGrid(inventory, this.DoSendPacket, cols, slotGridBounds, "slotgrid")
                    .AddSortButtonIcon(capi, this.sortButtonTexture, this.OnSortClick, this.sortButtonBounds, null)
                    .Compose();
            }
            this.SingleComposer.UnfocusOwnElements();

        }


        internal class StringComparer : IComparer<string>
        {
            public int Compare(string a, string b)
            { return a.CompareTo(b); }
        }


        private SortedDictionary<string, List<ItemSlot>> BuildDictionary(InventoryBase inventory)
        {
            var dictionary = new SortedDictionary<string, List<ItemSlot>>(new StringComparer());
            foreach (var itemSlot in inventory)
            {
                if (itemSlot.Itemstack != null)
                {
                    string sortName;
                    if (sortByDisplayName)
                    {
                        sortName = itemSlot.Itemstack.Collectible.GetHeldItemName(itemSlot.Itemstack);
                    }
                    else
                    {
                        sortName = itemSlot.Itemstack.Collectible.Code.Path;
                    }
                    if (!dictionary.TryGetValue(Lang.Get(sortName), out var slotlist))
                    {
                        slotlist = new List<ItemSlot>();
                        dictionary.Add(Lang.Get(sortName), slotlist);
                    }
                    slotlist.Add(itemSlot);
                }
            }
            return dictionary;
        }


        private SortedDictionary<string, List<ItemSlot>> MergeStacks(SortedDictionary<string, List<ItemSlot>> dictionary)
        {
            var world = this.capi.World;
            // merge stacks that are less than maxStackSize
            foreach (var entry in dictionary.Values)
            {
                var tempSlotList = new List<ItemSlot>();
                ItemSlot tempSlot = null;
                int maxStackSize;

                foreach (var itemSlot in entry)
                {
                    maxStackSize = itemSlot.Itemstack.Collectible.MaxStackSize;
                    if (itemSlot.StackSize < maxStackSize)
                    {
                        if (tempSlot == null)
                        { tempSlot = itemSlot; }
                        else
                        {
                            var itemStackMoveOperation = new ItemStackMoveOperation(world, EnumMouseButton.Left, 0, EnumMergePriority.AutoMerge, itemSlot.StackSize);
                            var packetClient = world.Player.InventoryManager.TryTransferTo(itemSlot, tempSlot, ref itemStackMoveOperation);
                            itemSlot.TryPutInto(world, tempSlot, itemStackMoveOperation.MovedQuantity);
                            this.capi.Network.SendPacketClient(packetClient);

                            maxStackSize = tempSlot.Itemstack.Collectible.MaxStackSize;
                            if (tempSlot.StackSize >= maxStackSize)
                            { tempSlot = null; }

                            if (itemSlot.Itemstack == null)
                            { tempSlotList.Add(itemSlot); }
                            else if (tempSlot == null)
                            { tempSlot = itemSlot; }
                        }
                    }
                }
                foreach (var item in tempSlotList)
                { entry.Remove(item); }
            }
            return dictionary;
        }


        private void CollapseStacks(SortedDictionary<string, List<ItemSlot>> dictionary)
        {
            // move inventory up and finalize
            var cnt = 0;
            foreach (var entry in dictionary.Values)
            {
                foreach (var itemSlot in entry)
                {
                    var tmpItemSlot = this.Inventory[cnt];
                    var prevcnt = cnt;
                    cnt++;
                    if (tmpItemSlot != itemSlot)
                    {
                        var itemstack = tmpItemSlot.Itemstack;
                        if ((itemstack?.Collectible) != itemSlot.Itemstack.Collectible)
                        {
                            if (tmpItemSlot.Itemstack != null)
                            {
                                string sortName;
                                if (sortByDisplayName)
                                {
                                    sortName = tmpItemSlot.Itemstack.Collectible.GetHeldItemName(tmpItemSlot.Itemstack);
                                }
                                else
                                {
                                    sortName = tmpItemSlot.Itemstack.Collectible.Code.Path;
                                }
                                dictionary.TryGetValue(Lang.Get(sortName), out var tmpEntry);

                                var tmpcnt = 0;
                                while (tmpEntry[tmpcnt] != tmpItemSlot)
                                { tmpcnt++; }
                                tmpEntry[tmpcnt] = itemSlot;
                            }
                            var packetClient = this.Inventory.TryFlipItems(prevcnt, itemSlot);
                            this.capi.Network.SendPacketClient(packetClient);
                        }
                    }
                }
            }
        }


        private void OnSortClick(int num)
        {
            //build dictionary
            try
            {
                var sortedDictionary = this.BuildDictionary(this.Inventory);
                // merge stacks that are less than maxStackSize
                sortedDictionary = this.MergeStacks(sortedDictionary);
                // move inventory up and finalize
                this.CollapseStacks(sortedDictionary);
            }
            catch
            {
                // sort failed for some reason
            }
            //repeat
            try
            {
                var sortedDictionary = this.BuildDictionary(this.Inventory);
                // merge stacks that are less than maxStackSize
                sortedDictionary = this.MergeStacks(sortedDictionary);
                // move inventory up and finalize
                this.CollapseStacks(sortedDictionary);
            }
            catch
            {
                // sort failed for some reason
            }
            return;
        }


        public override void OnGuiClosed()
        {
            base.OnGuiClosed();
            this.sortButtonTexture.Dispose();
            this.FreePos("smallblockgui", this.screenPos);
        }


        public override void OnGuiOpened()
        {
            base.OnGuiOpened();
            this.OccupyPos("smallblockgui", this.screenPos);
        }
    }


    public class GuiElementIconItemGrid : GuiElement
    {
        private readonly LoadedTexture texture;
        public Action<int> onSortClick;
        public Action<int> OnSlotOver;
        public static double unscaledButtonSize = 21;

        public override bool Focusable => true;

        public GuiElementIconItemGrid(ICoreClientAPI capi, LoadedTexture texture, Action<int> onSortClick, ElementBounds bounds) : base(capi, bounds)
        {
            this.texture = texture;
            this.onSortClick = onSortClick;
            this.Bounds.fixedHeight = GuiElementItemSlotGridBase.unscaledSlotPadding + unscaledButtonSize;
            this.Bounds.fixedWidth = GuiElementItemSlotGridBase.unscaledSlotPadding + unscaledButtonSize;
        }

        public override void ComposeElements(Context ctx, ImageSurface surface)
        { }

        public override void RenderInteractiveElements(float deltaTime)
        {
            var slotPadding = scaled(GuiElementItemSlotGridBase.unscaledSlotPadding);
            var slotWidth = scaled(unscaledButtonSize);
            var slotHeight = scaled(unscaledButtonSize);

            var dx = this.api.Input.MouseX - (int)this.Bounds.absX;
            var dy = this.api.Input.MouseY - (int)this.Bounds.absY;
            var iconColor = new Vec4f(0.9f, 0.8f, 0.8f, 0.9f);
            var iconBackColor = new Vec4f(0.2f, 0.1f, 0.1f, 0.9f);
            var hoverColor = new Vec4f(1f, 0.05f, 0.05f, 0.7f);

            if (this.texture != null)
            {
                this.api.Render.Render2DTexture(this.texture.TextureId, (float)this.Bounds.renderX + 2, (float)this.Bounds.renderY + 2, (float)slotWidth, (float)slotHeight, 50, iconBackColor);

                this.api.Render.Render2DTexture(this.texture.TextureId, (float)this.Bounds.renderX, (float)this.Bounds.renderY, (float)slotWidth, (float)slotHeight, 50, iconColor);
            }

            var over = dx >= 0 && dy >= 0 && dx < slotWidth + slotPadding && dy < slotHeight + slotPadding;
            if (over)
            {
                this.api.Render.Render2DTexture(this.texture.TextureId, (float)this.Bounds.renderX, (float)this.Bounds.renderY, (float)slotWidth, (float)slotHeight, 50, hoverColor);
                if (over)
                { this.OnSlotOver?.Invoke(0); }
            }
        }

        public override void OnMouseDownOnElement(ICoreClientAPI api, MouseEvent args)
        {
            base.OnMouseDownOnElement(api, args);
            var dx = api.Input.MouseX - (int)this.Bounds.absX;
            var dy = api.Input.MouseY - (int)this.Bounds.absY;
            var slotPadding = scaled(GuiElementItemSlotGridBase.unscaledSlotPadding);
            var slotWidth = scaled(unscaledButtonSize);
            var slotHeight = scaled(unscaledButtonSize);
            this.onSortClick?.Invoke(0);
        }

        public override void Dispose()
        { base.Dispose(); }
    }


    public static partial class GuiComposerHelpers
    {
        public static GuiComposer AddSortButtonIcon(this GuiComposer composer, ICoreClientAPI capi, LoadedTexture texture, Action<int> onSortClick, ElementBounds bounds, string key = null)
        {
            if (!composer.Composed)
            {
                composer.AddInteractiveElement(new GuiElementIconItemGrid(composer.Api, texture, onSortClick, bounds), key);
            }
            return composer;
        }
    }
}
