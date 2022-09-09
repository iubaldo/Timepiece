using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Client;
using Vintagestory.GameContent;


namespace Timepiece
{
    class InventoryHourglass : InventoryBase, ISlotProvider
    {
        ItemSlot[] slots;
        public ItemSlot[] Slots { get { return slots; } }
        BlockEntityHourglass hourglass;


        public InventoryHourglass(string inventoryID, ICoreAPI api) : base(inventoryID, api)
        {
            // slot 0 = top input
            // slot 1 = bottom output
            slots = GenEmptySlots(2);
        }

        public InventoryHourglass(string inventoryID, ICoreAPI api, BlockEntityHourglass hourglass) : base(inventoryID, api)
        {
            // slot 0 = top input
            // slot 1 = bottom output
            slots = GenEmptySlots(2);
            this.hourglass = hourglass;
        }


        public override int Count { get { return 2; } }


        public override ItemSlot this[int slotId]
        {
            get
            {
                if (slotId < 0 || slotId >= Count) return null;
                return slots[slotId];
            }
            set
            {
                if (slotId < 0 || slotId >= Count) throw new ArgumentOutOfRangeException(nameof(slotId));
                if (value == null) throw new ArgumentNullException(nameof(value));
                slots[slotId] = value;
            }
        }


        public override void FromTreeAttributes(ITreeAttribute tree)
        {
            slots = SlotsFromTreeAttributes(tree, slots);
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            SlotsToTreeAttributes(slots, tree);
        }

        protected override ItemSlot NewSlot(int i)
        {
            return new ItemSlotHourglass(this, hourglass);
        }

        public override float GetSuitability(ItemSlot sourceSlot, ItemSlot targetSlot, bool isMerge)
        {
            if (targetSlot == slots[0] && ((ItemSlotHourglass) slots[0]).IsValidGrain(sourceSlot.Itemstack)) return 4f;

            return base.GetSuitability(sourceSlot, targetSlot, isMerge);
        }


        // itemslot that only accepts valid hourglass inputs - grains, sand grains, pulvis sonus
        public class ItemSlotHourglass : ItemSlot
        {
            public ItemSlotHourglass(InventoryBase inventory, BlockEntityHourglass hourglass) : base(inventory)
            {
                MaxSlotStackSize = 100;
            }

            public override bool CanHold(ItemSlot sourceSlot)
            {
                return base.CanHold(sourceSlot) && IsValidGrain(sourceSlot.Itemstack);
            }

            public override bool CanTakeFrom(ItemSlot sourceSlot, EnumMergePriority priority = EnumMergePriority.AutoMerge)
            {
                return base.CanTakeFrom(sourceSlot, priority) && IsValidGrain(sourceSlot.Itemstack);
            }

            public bool IsValidGrain(ItemStack stack)
            {
                if (stack == null)
                    return false;

                string path = stack.Collectible.Code.Path; // use as unique identifier for items
                if (path.StartsWith("grain-") || path.Contains("sand_grains") || path.Contains("pulvis_sonus"))
                    return true;
                else
                    return false;
            }
        }
    }
}
