using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.GameContent;

namespace DairyPlus.Inventory
{
    public class InventoryCheesePot : InventoryBase
    {
        ItemSlot[] slots;
        public ItemSlot[] Slots => slots;


        public InventoryCheesePot(string inventoryID, ICoreAPI api) : base(inventoryID, api)
        {
            // slot 0-2 = input
            // slot 3-4 = output 
            slots = GenEmptySlots(5);
        }

        public InventoryCheesePot(string className, string instanceID, ICoreAPI api) : base(className, instanceID, api)
        {
            slots = GenEmptySlots(5);
        }

        public override int Count => 5;

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
                ArgumentNullException.ThrowIfNull(value);

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
            return new ItemSlotWatertight(this)
            {
                capacityLitres = 30
            };
        }
    }
}