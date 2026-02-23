using System;
using Cairo;
using DairyPlus.BlockEntity;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace DairyPlus.GUI
{
    public class GuiDialogChurn : GuiDialogBlockEntity
    {
        long lastRedrawMs;
        float inputGrindTime;
        float maxGrindTime;
        protected override double FloatyDialogPosition => 0.75;

        public GuiDialogChurn(string DialogTitle, InventoryBase Inventory, BlockPos BlockEntityPosition, ICoreClientAPI capi)
            : base(DialogTitle, Inventory, BlockEntityPosition, capi)
        {
            if (IsDuplicate) return;

            capi.World.Player.InventoryManager.OpenInventory(Inventory);

            SetupDialog();
        }

        private void OnInventorySlotModified(int slotid)
        {
            // Direct call can cause InvalidOperationException
            capi.Event.EnqueueMainThreadTask(SetupDialog, "setupchurndlg");
        }

        void SetupDialog()
        {
            ItemSlot hoveredSlot = capi.World.Player.InventoryManager.CurrentHoveredSlot;
            if (hoveredSlot != null && hoveredSlot.Inventory == Inventory)
            {
                capi.Input.TriggerOnMouseLeaveSlot(hoveredSlot);
            }
            else
            {
                hoveredSlot = null;
            }

            ElementBounds churnBounds = ElementBounds.Fixed(0, 0, 210, 180);

            // inventiry slots
            ElementBounds creamBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, 0, 30, 1, 1);
            ElementBounds butterBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, 195, 30, 1, 1);
            ElementBounds milkBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, 125, 120, 1, 1);

            //liquid bars
            ElementBounds leftBar = ElementBounds.Fixed(1, 90, 40, 120);
            ElementBounds rightBar = ElementBounds.Fixed(196, 90, 40, 120);

            // 10 pixel padding
            ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
            bgBounds.BothSizing = ElementSizing.FitToChildren;
            bgBounds.WithChildren(churnBounds);

            // 3. Finally Dialog
            ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.RightMiddle)
            .WithFixedAlignmentOffset(-GuiStyle.DialogToScreenPadding, 0);

            ClearComposers();
            SingleComposer = capi.Gui
                .CreateCompo("blockentitychurn" + BlockEntityPosition, dialogBounds)
                .AddShadedDialogBG(bgBounds)
                .AddDialogTitleBar(DialogTitle, OnTitleBarClose)
                .BeginChildElements(bgBounds)

                    // inventory slots 
                    .AddDynamicCustomDraw(churnBounds, OnBgDraw, "drawChurn")
                    .AddItemSlotGrid(Inventory, SendInvPacket, 1, new int[] { 0 }, creamBounds, "creamSlot")
                    .AddItemSlotGrid(Inventory, SendInvPacket, 1, new int[] { 2 }, butterBounds, "butterslot")
                    .AddItemSlotGrid(Inventory, SendInvPacket, 1, new int[] { 1 }, milkBounds, "milkslot")

                    // liquid bars
                    .AddInset(leftBar.ForkBoundingParent(2, 2, 2, 2), 2)
                    .AddDynamicCustomDraw(leftBar, LiquidBarDrawLeft, "leftBar")

                    .AddInset(rightBar.ForkBoundingParent(2, 2, 2, 2), 2)
                    .AddDynamicCustomDraw(rightBar, LiquidBarDrawRight, "rightBar")

                .EndChildElements()
                .Compose();

            lastRedrawMs = capi.ElapsedMilliseconds;
        }

        // blockentity update

        public void Update(float inputGrindTime, float maxGrindTime)
        {
            this.inputGrindTime = inputGrindTime;
            this.maxGrindTime = maxGrindTime;

            if (!IsOpened()) return;
            if (SingleComposer == null) return;

            if (capi.ElapsedMilliseconds - lastRedrawMs > 100)
            {
                SingleComposer.GetCustomDraw("drawChurn")?.Redraw();
                lastRedrawMs = capi.ElapsedMilliseconds;
            }
                return;
        }

        //Churn Progress Shape
        public void DrawChurnShape(Context ctx)
        {
            ctx.MoveTo(65, 180);
            ctx.LineTo(74, 110);
            ctx.LineTo(86, 110);
            ctx.LineTo(90, 50);
            ctx.LineTo(98, 50);
            ctx.LineTo(94, 110);
            ctx.LineTo(106, 110);
            ctx.LineTo(115, 180);
            ctx.ClosePath();
        }

               // fill bars
        private void LiquidBarDrawLeft(Context ctx, ImageSurface surface, ElementBounds currentBounds)
        {
            ItemSlot liquidSlot = Inventory[0];
            if (liquidSlot.Empty) return;

            BlockEntityChurn bechurn = capi.World.BlockAccessor.GetBlockEntity(BlockEntityPosition) as BlockEntityChurn;
            float itemsPerLitre = 1f;
            int capacity = bechurn.CapacityLitres;

            WaterTightContainableProps props = BlockLiquidContainerBase.GetContainableProps(liquidSlot.Itemstack);
            if (props != null)
            {
                itemsPerLitre = props.ItemsPerLitre;
            }

            float fullnessRelative = liquidSlot.StackSize / itemsPerLitre / capacity;

            double offY = (1 - fullnessRelative) * currentBounds.InnerHeight;

            ctx.Rectangle(0, offY, currentBounds.InnerWidth, currentBounds.InnerHeight - offY);

            CompositeTexture tex = props?.Texture ?? liquidSlot.Itemstack.Collectible.Attributes?["inContainerTexture"].AsObject<CompositeTexture>(null, liquidSlot.Itemstack.Collectible.Code.Domain);
            if (tex != null)
            {
                ctx.Save();
                Matrix m = ctx.Matrix;
                m.Scale(GuiElement.scaled(3), GuiElement.scaled(3));
                ctx.Matrix = m;

                AssetLocation loc = tex.Base.Clone().WithPathAppendixOnce(".png");
                GuiElement.fillWithPattern(capi, ctx, loc, true, false, tex.Alpha);

                ctx.Restore();
            }
        }

        private void LiquidBarDrawRight(Context ctx, ImageSurface surface, ElementBounds currentBounds)
        {
            ItemSlot liquidSlot = Inventory[1];
            if (liquidSlot.Empty) return;

            BlockEntityChurn bechurn = capi.World.BlockAccessor.GetBlockEntity(BlockEntityPosition) as BlockEntityChurn;
            float itemsPerLitre = 1f;
            int capacity = bechurn.CapacityLitres;

            WaterTightContainableProps props = BlockLiquidContainerBase.GetContainableProps(liquidSlot.Itemstack);
            if (props != null)
            {
                itemsPerLitre = props.ItemsPerLitre;
            }

            float fullnessRelative = liquidSlot.StackSize / itemsPerLitre / capacity;

            double offY = (1 - fullnessRelative) * currentBounds.InnerHeight;

            ctx.Rectangle(0, offY, currentBounds.InnerWidth, currentBounds.InnerHeight - offY);

            CompositeTexture tex = props?.Texture ?? liquidSlot.Itemstack.Collectible.Attributes?["inContainerTexture"].AsObject<CompositeTexture>(null, liquidSlot.Itemstack.Collectible.Code.Domain);
            if (tex != null)
            {
                ctx.Save();
                Matrix m = ctx.Matrix;
                m.Scale(GuiElement.scaled(3), GuiElement.scaled(3));
                ctx.Matrix = m;

                AssetLocation loc = tex.Base.Clone().WithPathAppendixOnce(".png");
                GuiElement.fillWithPattern(capi, ctx, loc, true, false, tex.Alpha);

                ctx.Restore();
            }
        }
        private void OnBgDraw(Context ctx, ImageSurface surface, ElementBounds currentBounds)
        {
            ctx.Save();

            double dx = inputGrindTime / maxGrindTime;
            double shapeHeight = currentBounds.InnerHeight;

            // outline
            DrawChurnShape(ctx);
            ctx.SetSourceRGBA(0, 0, 0, 1); // black
            ctx.LineWidth = GuiElement.scaled(2);
            ctx.Stroke();

            // clipping mask
            DrawChurnShape(ctx);
            ctx.Clip();
            // Fill
            ctx.Rectangle(0, shapeHeight * (1 - dx), currentBounds.InnerWidth, shapeHeight * dx);
            ctx.Clip();
            // Vertical gradient
            LinearGradient gradient = new LinearGradient(0, shapeHeight, 0, 0);
            gradient.AddColorStop(0, new Color(0, 0.9, 0.8, 1));  // bottom
            gradient.AddColorStop(1, new Color(0.2, 0.9, 0.5, 1)); // top
            ctx.SetSource(gradient);
            DrawChurnShape(ctx);

            gradient.Dispose();
            ctx.Restore();
        }


        private void SendInvPacket(object p)
        {
            capi.Network.SendBlockEntityPacket(BlockEntityPosition.X, BlockEntityPosition.Y, BlockEntityPosition.Z, p);
        }


        private void OnTitleBarClose()
        {
            TryClose();
        }


        public override void OnGuiOpened()
        {
            base.OnGuiOpened();
            Inventory.SlotModified += OnInventorySlotModified;
        }

        public override void OnGuiClosed()
        {
            Inventory.SlotModified -= OnInventorySlotModified;

            SingleComposer.GetSlotGrid("creamSlot").OnGuiClosed(capi);
            SingleComposer.GetSlotGrid("milkslot").OnGuiClosed(capi);
            SingleComposer.GetSlotGrid("butterslot").OnGuiClosed(capi);

            base.OnGuiClosed();
        }
    }
}
