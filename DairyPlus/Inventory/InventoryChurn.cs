using System;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace DairyPlus.Inventory
{
    public class InventoryChurn : InventoryBase, ISlotProvider
    {
        // inventory with one input and two outputs

        ItemSlot[] slots;
        public ItemSlot[] Slots => slots;


        public InventoryChurn(string inventoryID, ICoreAPI api) : base(inventoryID, api)
        {
            // slot 0 = input
            // slot 1 = output butter
            slots = GenEmptySlots(2);
        }

        public InventoryChurn(string className, string instanceID, ICoreAPI api) : base(className, instanceID, api)
        {
            slots = GenEmptySlots(2);
        }

        public override int Count => 2;

        public override ItemSlot this[int slotId]
        {
            get
            {
                if (slotId < 0 || slotId >= Count) throw new ArgumentOutOfRangeException(nameof(slotId));
                return slots[slotId];
            }
            set
            {
                if (slotId < 0 || slotId >= Count) throw new ArgumentOutOfRangeException(nameof(slotId));
                if (value == null) throw new ArgumentNullException(nameof(value));
                slots[slotId] = value;
            }
        }

        //saving+loading
        public override void FromTreeAttributes(ITreeAttribute tree)
        {
            slots = SlotsFromTreeAttributes(tree, slots);
        }
        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            SlotsToTreeAttributes(slots, tree);
        }
        //slot type
        protected override ItemSlot NewSlot(int i)
        {
            if (i == 0)
            {
                {
                    var slot = new ItemSlotWatertight(this);
                    slot.capacityLitres = 12;
                    return slot;
                }
            }
            return new ItemSlotWatertight(this);
            if (i == 1) return new ItemSlotOutput(this);
            throw new ArgumentOutOfRangeException(nameof(i), "Invalid");
        }

        // Add cream to inputslot restriction
        public override float GetSuitability(ItemSlot sourceSlot, ItemSlot targetSlot, bool isMerge)
        {
            if (sourceSlot?.Itemstack == null) return 0f;

            if (targetSlot == slots[0] &&
                sourceSlot.Itemstack.Collectible.Code.Equals(new AssetLocation("dairyplus", "cream")))
            {
                return 4f;
            }
            return base.GetSuitability(sourceSlot, targetSlot, isMerge);
        
        }
    }
}
