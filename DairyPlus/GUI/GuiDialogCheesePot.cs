using Cairo;
using DairyPlus.BlockEntity;
using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
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
        float potTemperature;
        private float fuelBurnTime;
        private float maxFuelBurnTime;

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

            ElementBounds potBounds = ElementBounds.Fixed(0, 0, 260, 210);
           
            // input/output slot
            ElementBounds inputSlots = ElementStdBounds.SlotGrid(EnumDialogArea.None, 30, 50, 4, 1 );
            ElementBounds outputSlots = ElementStdBounds.SlotGrid(EnumDialogArea.None, 130, 140, 2, 1 );
            ElementBounds fuelSlot = ElementStdBounds.SlotGrid(EnumDialogArea.None, 50, 160, 1, 1 );

            ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
            bgBounds.BothSizing = ElementSizing.FitToChildren;
            bgBounds.WithChildren(potBounds);

            ElementBounds recipeBounds = ElementBounds.Fixed(0, 25, 260, 20);

            // dialog
            ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.RightMiddle)
            .WithFixedAlignmentOffset(-GuiStyle.DialogToScreenPadding, 0);

            ClearComposers();
            SingleComposer = capi.Gui
                .CreateCompo("cheesepot-" + BlockEntityPosition, dialogBounds)
                .AddShadedDialogBG(bgBounds)
                .AddDialogTitleBar(DialogTitle, OnTitleBarClose)
                .BeginChildElements(bgBounds)

                    // Input slots (0–3)
                    .AddItemSlotGrid(Inventory, SendInvPacket, 4, [0, 1, 2, 3], inputSlots, "inputSlots")

                    // Output slots (4–5)
                    .AddItemSlotGrid(Inventory, SendInvPacket, 2, [4, 5], outputSlots, "outputSlots")

                    // fuel slot (6)
                    .AddItemSlotGrid(Inventory, SendInvPacket, 1, [6], fuelSlot, "fuelSlot")

                    // progress bar
                    .AddDynamicCustomDraw(potBounds, ProgressBarDraw, "progressBar")
                    .AddDynamicCustomDraw(potBounds, FuelBarDraw, "fuelBar")

                    .AddDynamicText("", CairoFont.WhiteDetailText(), fuelSlot.RightCopy(-45, -20), "temperature")
                    .AddDynamicText(GetRecipeText(), CairoFont.WhiteDetailText().WithOrientation(EnumTextOrientation.Center), recipeBounds, "recipeText")

                .EndChildElements()
                .Compose();
            lastRedrawMs = capi.ElapsedMilliseconds;
        }

        string GetRecipeText()
        {
            BECheesePot beCh = capi.World.BlockAccessor.GetBlockEntity(BlockEntityPosition) as BECheesePot;

            if (beCh.currentRecipe == null)
            {
                return Lang.Get("dairyplus:recipe-none");
            }

            var outputs = beCh.currentRecipe.Outputs;

            if (outputs.Length == 1)
            {
                return Lang.Get(
                    "dairyplus:recipe-oneoutput",
                    outputs[0].ResolvedItemStack.GetName()
                );
            }

            return Lang.Get(
                "dairyplus:recipe-twooutput",
                outputs[0].ResolvedItemStack.GetName(),
                outputs[1].ResolvedItemStack.GetName()
            );
        }



        public void Update(float processing, float maxProcessing, float potTemperature, float fuelBurnTime, float MaxFuelBurnTime)
        {
            this.processing = processing;
            this.maxProcessing = maxProcessing;
            this.potTemperature = potTemperature;
            this.fuelBurnTime = fuelBurnTime;
            this.maxFuelBurnTime = MaxFuelBurnTime;

            if (!IsOpened()) return;

            string tempText;
            tempText = $"{potTemperature:0}°C";

            SingleComposer?
                .GetDynamicText("temperature")
                ?.SetNewText(tempText);

            if (capi.ElapsedMilliseconds - lastRedrawMs > 500)
            {
                {
                    if (SingleComposer != null)
                    {
                        SingleComposer.GetCustomDraw("progressBar").Redraw();
                        SingleComposer?.GetCustomDraw("fuelBar").Redraw();
                    }

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
            ctx.LineWidth = 1;
            ctx.Stroke();

            // filling
            ctx.Rectangle(x, y + 1, width * processRel, height - 2);
            ctx.SetSourceRGBA(1.0, 0.95, 0.8, 1.0);
            ctx.Fill();

            ctx.Restore();
        }
        private void FuelBarDraw(Context ctx, ImageSurface surface, ElementBounds currentBounds)
        {
            double x = 30;
            double y = 137;
            double width = 16;
            double height = 70;
            double burnRel = fuelBurnTime / maxFuelBurnTime;
            burnRel = GameMath.Clamp(burnRel, 0, 1);

            ctx.Save();
            // outline
            ctx.Rectangle(x, y, width, height);
            ctx.SetSourceRGBA(0.16, 0.13, 0.11, 1.0);
            ctx.LineWidth = 1;
            ctx.Stroke();

            // filling
            double fillHeight = height * burnRel;
            double fillY = y + (height - fillHeight);

            ctx.Rectangle(x + 1, fillY, width - 2, fillHeight);
            ctx.SetSourceRGBA(1.0, 0.2, 0.2, 1.0);
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