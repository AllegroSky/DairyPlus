using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace DairyPlus.Items
{
    public class ItemSkimCurd : Item
    {
        public string AnimationCode { get; set; } = "squeezehoneycomb";

        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
        {
            if (blockSel == null) return;

            if (blockSel != null)
            {
                var block = api.World.BlockAccessor.GetBlock(blockSel.Position);
                if (block.Attributes?.IsTrue("pieFormingSurface") == true)
                {
                    handling = EnumHandHandling.PreventDefault;
                    if (byEntity.World.Side == EnumAppSide.Client)
                    {
                        byEntity.Controls.HandUse = EnumHandInteract.HeldItemInteract;

                        return;
                    }
                }
                base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);
            }
        }
        public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            if (blockSel == null) return false;
            var block = api.World.BlockAccessor.GetBlock(blockSel.Position);
            if (block.Attributes?.IsTrue("pieFormingSurface") != true) return false;
            if (byEntity.World is IClientWorldAccessor) byEntity.StartAnimation(AnimationCode);

            //Wait for "processing time"
            return secondsUsed < 2f;
        }
        public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            byEntity.StopAnimation(AnimationCode);

            if (secondsUsed < 2f || blockSel == null) return;

            var block = api.World.BlockAccessor.GetBlock(blockSel.Position);
            if (block.Attributes?.IsTrue("pieFormingSurface") != true) return;

            if (api.World.Side == EnumAppSide.Server)
            {
                TransitionableProperties perishProps = GetPerishProps(slot.Itemstack);

                ItemStack newStack = new ItemStack(api.World.GetItem(new AssetLocation("dairyplus:mozzaball")), 1);
                CarryOverFreshness(api, slot, newStack, perishProps);

                if (byEntity is EntityPlayer entityPlayer)
                {
                    IPlayer player = api.World.PlayerByUid(entityPlayer.PlayerUID);

                    if (player != null)
                    {
                        if (!player.InventoryManager.TryGiveItemstack(newStack))
                        {
                            api.World.SpawnItemEntity(newStack, entityPlayer.Pos.XYZ);
                        }
                    }
                }
                slot.TakeOut(1);
                slot.MarkDirty();
            }
            
        }
        public override bool OnHeldInteractCancel(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, EnumItemUseCancelReason cancelReason)
        {
            byEntity.StopAnimation(AnimationCode);
            return base.OnHeldInteractCancel(secondsUsed, slot, byEntity, blockSel, entitySel, cancelReason);
        }

        //freshness checker helper
        private TransitionableProperties? GetPerishProps(ItemStack stack)
        {
            var props = stack?.Collectible?.GetTransitionableProperties(api.World, stack, null);
            if (props == null) return null;

            foreach (var p in props)
            {
                if (p.Type == EnumTransitionType.Perish)
                {
                    return p;
                }
            }
            return null;
        }
    }
}