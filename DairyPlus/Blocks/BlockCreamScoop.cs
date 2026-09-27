using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.GameContent;

namespace DairyPlus.Blocks
{
    public class BlockCreamScoop : BlockLiquidContainerTopOpened
    {
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
            
            // liquid type validation
            if (liqslot.Itemstack.Item.Code.Path != "separatingmilk") return;

                //has enough milk + is mutiple of 10
            if (curLitres < 10f || curLitres % 10f != 0f)
            {
                (api as ICoreClientAPI)?.TriggerIngameError(this, "wrong amount",
                    Lang.Get("Need 10, 20, 30, 40, or 50L of Settled Milk to skim Cream from"));
                return;
            }

                // check if scoop filled
            float scoopLitres = GetCurrentLitres(slot.Itemstack);

            if (scoopLitres > 0f)
            {
                (api as ICoreClientAPI)?.TriggerIngameError(this, "full",
                    Lang.Get("Empty your scoop first."));
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
            //Wait for "processing time"
            return secondsUsed < 1.5f;
        }


        public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            if (secondsUsed < 1.5f || blockSel == null) return;

            // define variables again 
            var beba = api.World.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityBarrel;
            var liqslot = beba?.Inventory[1];

            //verify nothing changed and skimming still valid
            if (beba == null || liqslot == null || liqslot.Empty) return;
            if (liqslot.Itemstack.Item.Code.Path != "separatingmilk") return;

            var milkProps = GetContainableProps(liqslot?.Itemstack);
            float itemsPerLitre = milkProps?.ItemsPerLitre ?? 1f;
            int batchLitres = (int)(liqslot.Itemstack.StackSize / itemsPerLitre);
            if (batchLitres < 10 || batchLitres % 10 != 0) return;

            //math
            int creamLitres = batchLitres / 5;
            int skimLitres = batchLitres - creamLitres;
            // makes server do this

            if (api.World.Side == EnumAppSide.Server)
            {
                // Fill scoop 
                Item creamItem = api.World.GetItem(new AssetLocation("dairyplus", "cream"));
                if (creamItem != null)
                {
                    ItemStack source = new ItemStack(creamItem, 9999);
                    TransitionableProperties creamPerishProps = GetPerishProps(source);
                    CarryOverFreshness(api, liqslot, source, creamPerishProps);
                    TryPutLiquid(slot.Itemstack, source, creamLitres);
                }

                // remove milk add skim milk 
                Item skimItem = api.World.GetItem(new AssetLocation("dairyplus", "skimmilk"));
                if (skimItem != null)
                {
                    ItemStack skimStack = new ItemStack(skimItem, skimLitres * (int)itemsPerLitre);
                    TransitionableProperties skimPerishProps = GetPerishProps(skimStack);
                    CarryOverFreshness(api, liqslot, skimStack, skimPerishProps);
                    liqslot.TakeOut((int)(batchLitres * itemsPerLitre));
                    liqslot.Itemstack = skimStack;
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
        //freshness checker helper
        private TransitionableProperties GetPerishProps(ItemStack stack)
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