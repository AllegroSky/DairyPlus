using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.GameContent;

namespace DairyPlus.Items
{
    public class BlockCreamScoop : BlockLiquidContainerTopOpened
    {

        public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
        {
            failureCode = "unplaceable";
            return false;
        }

        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling)

        {         //has to be a barrel
            if (blockSel == null) return;

            var beba = api.World.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityBarrel;
            var liqslot = beba?.Inventory[1];
                 //not empty
            if (beba == null || liqslot.Empty) return;

                 //has milk
            var milkProps = GetContainableProps(liqslot.Itemstack);
            float itemsPerLitre = milkProps?.ItemsPerLitre ?? 1f;
            float curLitres = liqslot.Itemstack.StackSize / itemsPerLitre;
            if (liqslot.Itemstack.Item?.Code?.Path != "dairyplus:item/liquid/separatingmilk") return;

                //has enough milk
            if (curLitres < 50f)
            {
                (api as ICoreClientAPI)?.TriggerIngameError(this, "notenough",
                    Lang.Get("Need 50L of Settled Milk to skim Cream from"));
                handHandling = EnumHandHandling.PreventDefault;
                return;
            }

                // check if scoop filled
            float scoopLitres = GetCurrentLitres(slot.Itemstack);

            if (scoopLitres > 0f)
            {
                (api as ICoreClientAPI)?.TriggerIngameError(this, "full",
                    Lang.Get("Empty your scoop first."));
                handHandling = EnumHandHandling.PreventDefault;
                return;
            }

                //tell game this is held interraction
            if (byEntity.World.Side == EnumAppSide.Client)
            {
                byEntity.Controls.HandUse = EnumHandInteract.HeldItemInteract;
            }

            handHandling = EnumHandHandling.PreventDefault;
        }
    

        public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            //Wait 2ish seconds of "processing time"
            return secondsUsed < 1.7f;
        }


        public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            if (secondsUsed < 1.7f || blockSel == null) return;

            // define variables again 
            var beba = api.World.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityBarrel;
            var liqslot = beba?.Inventory[1];
            var milkProps = GetContainableProps(liqslot?.Itemstack);
            float itemsPerLitre = milkProps?.ItemsPerLitre ?? 1f;
            float curLitres = (liqslot?.Itemstack != null)
             ? liqslot.Itemstack.StackSize / itemsPerLitre : 0f;

            // makes server do this

            if (api.World.Side == EnumAppSide.Server)
            {
                // Remove milk
                liqslot.TakeOut((int)(50f * itemsPerLitre));

                // Fill scoop 
                Item creamItem = api.World.GetItem(new AssetLocation("dairyplus", "cream"));
                if (creamItem != null)
                {
                    ItemStack source = new ItemStack(creamItem, 999999);
                    TryPutLiquid(slot.Itemstack, source, 10f);
                }

                // add skim milk 
                Item skimItem = api.World.GetItem(new AssetLocation("dairyplus", "skimmilk"));
                if (skimItem != null)
                {
                    liqslot.Itemstack = new ItemStack(skimItem, (int)(40f * itemsPerLitre));
                }

                // tell client to update

                beba.MarkDirty(true);
                slot.MarkDirty();
            }
                // stop progress bar
            if (byEntity.World.Side == EnumAppSide.Client)
            {
                byEntity.Controls.HandUse = EnumHandInteract.None;
            }
        }

    }
}