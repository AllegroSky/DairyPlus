using Cairo;
using DairyPlus.BlockEntity;
using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace DairyPlus.GUI
{
    public class GuiDialogCheesePot : GuiDialogBlockEntity
    {
        protected override double FloatyDialogPosition => 0.75;
        long lastRedrawMs;
        float processing;
        float maxProcessing;

        public GuiDialogCheesePot(string DialogTitle, InventoryBase Inventory, BlockPos BlockEntityPosition, ICoreClientAPI capi)
            : base(DialogTitle, Inventory, BlockEntityPosition, capi)
        {
            if (IsDuplicate) return;

            capi.World.Player.InventoryManager.OpenInventory(Inventory);

            SetupDialog();
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

            ElementBounds potBounds = ElementBounds.Fixed(0, 0, 300, 250);
           
            // input/output slot
            ElementBounds inputSlots = ElementStdBounds.SlotGrid(EnumDialogArea.None, 70, 30, 3, 1 );
            ElementBounds outputSlots = ElementStdBounds.SlotGrid(EnumDialogArea.None, 95, 140, 2, 1);

            ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
            bgBounds.BothSizing = ElementSizing.FitToChildren;
            bgBounds.WithChildren(potBounds);

            // dialog
            ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.RightMiddle)
            .WithFixedAlignmentOffset(-GuiStyle.DialogToScreenPadding, 0);

            ClearComposers();
            SingleComposer = capi.Gui
                .CreateCompo("cheesepot-" + BlockEntityPosition, dialogBounds)
                .AddShadedDialogBG(bgBounds)
                .AddDialogTitleBar(DialogTitle, OnTitleBarClose)
                .BeginChildElements(bgBounds)

                    // Input slots (0–2)
                    .AddItemSlotGrid(Inventory, SendInvPacket, 3, new int[] { 0, 1, 2 }, inputSlots, "inputSlots")

                    // Output slots (3–4)
                    .AddItemSlotGrid(Inventory, SendInvPacket, 2, new int[] { 3, 4 }, outputSlots, "outputSlots")

                    // progress bar
                    .AddDynamicCustomDraw(potBounds, ProgressBarDraw, "progressBar")

                .EndChildElements()
                .Compose();
            lastRedrawMs = capi.ElapsedMilliseconds;
        }

        public void Update(float processing, float maxProcessing)
        {
            this.processing = processing;
            this.maxProcessing = maxProcessing;

            if (!IsOpened()) return;

            if (capi.ElapsedMilliseconds - lastRedrawMs > 100)
            {
                {
                    if (SingleComposer != null)
                        SingleComposer.GetCustomDraw("progressBar").Redraw();
                    lastRedrawMs = capi.ElapsedMilliseconds;
                }
            }
            return;
        }
        // Progress Shape
        private void ProgressBarDraw(Context ctx, ImageSurface surface, ElementBounds currentBounds)
        {
            double x = 30;
            double y = 110;
            double width = 200;
            double height = 20;
            double processRel = processing / maxProcessing; //fraction progress
            processRel = GameMath.Clamp(processRel, 0, 1);

            ctx.Save();
            // outline
            ctx.Rectangle(x, y, width, height);
            ctx.SetSourceRGBA(0.16, 0.13, 0.11, 1.0);
            ctx.LineWidth = GuiElement.scaled(2);
            ctx.Stroke();

            // filling
            ctx.Rectangle(x, y, width * processRel, height);
            ctx.SetSourceRGBA(1.0, 0.95, 0.8, 1.0);
            ctx.Fill();

            ctx.Restore();
        }
        private void SendInvPacket(object packet)
        {
            capi.Network.SendBlockEntityPacket(BlockEntityPosition.X, BlockEntityPosition.Y, BlockEntityPosition.Z, packet);
        }

        private void OnTitleBarClose()
        {
            TryClose();
        }
        public override void OnGuiOpened()
        {
            base.OnGuiOpened();
        }
        public override void OnGuiClosed()
        {
            SingleComposer.GetSlotGrid("inputSlots").OnGuiClosed(capi);
            SingleComposer.GetSlotGrid("outputSlots").OnGuiClosed(capi);

            base.OnGuiClosed();
        }
    }
}