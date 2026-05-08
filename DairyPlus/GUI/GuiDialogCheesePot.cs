using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace DairyPlus.GUI
{
    public class GuiDialogCheesePot : GuiDialogBlockEntity
    {
        protected override double FloatyDialogPosition => 0.75;
        long lastRedrawMs;

        public GuiDialogCheesePot(string DialogTitle, InventoryBase Inventory, BlockPos BlockEntityPosition, ICoreClientAPI capi)
            : base(DialogTitle, Inventory, BlockEntityPosition, capi)
        {
            if (IsDuplicate) return;

            capi.World.Player.InventoryManager.OpenInventory(Inventory);

            SetupDialog();
        }
        private void OnInventorySlotModified(int slotid)
        {
            // Direct call can cause InvalidOperationException
            capi.Event.EnqueueMainThreadTask(SetupDialog, "setupcheesepotdlg");
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

            ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
            bgBounds.BothSizing = ElementSizing.FitToChildren;

            //input slot
            ElementBounds inputSlots = ElementStdBounds.SlotGrid(
                EnumDialogArea.None,
                0, 30,
                3, 1
            );

            //output slots
            ElementBounds outputSlots = ElementStdBounds.SlotGrid(
                EnumDialogArea.None,
                0, 90,
                2, 1
            );

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

                .EndChildElements()
                .Compose();
            lastRedrawMs = capi.ElapsedMilliseconds;
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
            Inventory.SlotModified += OnInventorySlotModified;
        }
        public override void OnGuiClosed()
        {
            Inventory.SlotModified -= OnInventorySlotModified;

            SingleComposer.GetSlotGrid("inputSlots").OnGuiClosed(capi);
            SingleComposer.GetSlotGrid("outputSlots").OnGuiClosed(capi);

            base.OnGuiClosed();
        }
    }
}