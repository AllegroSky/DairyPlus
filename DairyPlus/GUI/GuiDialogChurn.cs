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

            ElementBounds churnBounds = ElementBounds.Fixed(0, 0, 180, 160);

            // inventiry slots
            ElementBounds creamBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, 60, 35, 1, 1);
            ElementBounds butterBounds = ElementStdBounds.SlotGrid(EnumDialogArea.None, 60, 100, 1, 1);

            //liquid bar
            ElementBounds liquidBar = ElementBounds.Fixed(1, 30, 40, 120);

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
                    .AddItemSlotGrid(Inventory, SendInvPacket, 1, new int[] { 1 }, butterBounds, "butterslot")

                    // liquid bar
                    .AddInset(liquidBar.ForkBoundingParent(2, 2, 2, 2), 2)
                    .AddDynamicCustomDraw(liquidBar, LiquidBarDraw, "liquidBar")

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

            if (capi.ElapsedMilliseconds - lastRedrawMs > 500)
            {
                {
                    if (SingleComposer != null)
                        SingleComposer.GetCustomDraw("drawChurn").Redraw();
                    lastRedrawMs = capi.ElapsedMilliseconds;
                }
            }
                return;
        }

        //Churn Progress Shape
        public void DrawChurnShape(Context ctx)
        {
            ctx.MoveTo(125, 152);
            ctx.LineTo(133, 89);
            ctx.LineTo(144, 89);
            ctx.LineTo(148, 35);
            ctx.LineTo(155, 35);
            ctx.LineTo(151, 89);
            ctx.LineTo(162, 89);
            ctx.LineTo(170, 152);
            ctx.ClosePath();
        }

               // fill bar
        private void LiquidBarDraw(Context ctx, ImageSurface surface, ElementBounds currentBounds)
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
       
        private void OnBgDraw(Context ctx, ImageSurface surface, ElementBounds currentBounds)
        {
            double churnRel = inputGrindTime / maxGrindTime; //fraction progress
            double minY = 35;
            double maxY = 152;
            double minX = 125;
            double maxX = 170;
            double clipY = maxY - (maxY - minY) * churnRel;

            ctx.Save();

            // outline
            DrawChurnShape(ctx);
            ctx.SetSourceRGBA(0.16, 0.13, 0.11, 1.0);
            ctx.LineWidth = GuiElement.scaled(2);
            ctx.Stroke();

            // clipping mask
            DrawChurnShape(ctx);
            ctx.Clip ();
            ctx.Rectangle(minX, clipY, maxX - minX, maxY - clipY);
            ctx.ClipPreserve();

            // Fill 
            ctx.SetSourceRGBA(1.0, 0.95, 0.8, 1.0);
            ctx.Paint();

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
            SingleComposer.GetSlotGrid("butterslot").OnGuiClosed(capi);

            base.OnGuiClosed();
        }
    }
}
