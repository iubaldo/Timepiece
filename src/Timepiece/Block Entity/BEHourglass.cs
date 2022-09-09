using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Client;
using Vintagestory.API.Server;
using Vintagestory.GameContent;
using Vintagestory.GameContent.Mechanics;


namespace Timepiece
{
    class BlockEntityHourglass : BlockEntityOpenableContainer
    {
        internal InventoryHourglass inventory;
        GUIDialogBlockEntityHourglass clientDialog;
        float inputFallTime;
        float prevInputFallTime;
        float maxFallTime = 4f; // amount of time it takes
        float fallSpeed = 4f; // ticks by 4 per second

        
        public BlockEntityHourglass()
        {
            inventory = new InventoryHourglass(null, null, this);
            inventory.SlotModified += OnSlotModifid;
        }


        public override string InventoryClassName { get { return "hourglass"; } }

        public virtual string DialogTitle { get { return Lang.Get("Hourglass"); } }

        public override InventoryBase Inventory { get { return inventory; } }


        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);
            inventory.LateInitialize("hourglass-" + Pos.X + "/" + Pos.Y + "/" + Pos.Z, api);

            RegisterGameTickListener(Every100ms, 100);
            RegisterGameTickListener(Every500ms, 500);
        }

        private void Every100ms(float dt)
        {
            // Only tick on the server and merely sync to client
            if (CanFall())
            {
                inputFallTime += dt * fallSpeed;
                if (inputFallTime >= maxFallTime) // maxFallTime depends on type of grain in the hourglass
                {
                    Fall();
                    inputFallTime = 0f;
                }

                MarkDirty();
            }
        }

        // sync to client every 500ms
        private void Every500ms(float dt)
        {                                                                              // && inventory[0].Itemstack?.Collectible.GrindingProps != null
            if (Api.Side == EnumAppSide.Server && (prevInputFallTime != inputFallTime) && CanFall())  //don't spam update packets when empty, as inputGrindTime is irrelevant when empty
                MarkDirty();


            prevInputFallTime = inputFallTime;
        }

        private void OnSlotModifid(int slotid)
        {
            if (Api is ICoreClientAPI)
                clientDialog.Update(inputFallTime, maxFallTime);

            if (slotid == 0) // input slot
            {
                inputFallTime = 0.0f; //reset the progress to 0 if the item is removed.
                MarkDirty();

                if (clientDialog != null && clientDialog.IsOpened())
                {
                    clientDialog.SingleComposer.ReCompose();
                }
            }
        }

        // causes a grain from the top input to fall to the bottom output
        // assumes proper conditions are met - either output is empty or input and output grain types match
        private void Fall()
        {
            ItemStack grainStack = InputSlot.TakeOut(1);

            if (OutputSlot.Itemstack == null) // if output is empty, use the grain removed from input stack
                OutputSlot.Itemstack = grainStack;
            else // otherwise, just increment output stacksize      
                OutputSlot.Itemstack.StackSize++;

            InputSlot.MarkDirty();
            OutputSlot.MarkDirty();
        }

        // return true if input item is compatible with hourglass
        // allowed item types: grains, sand grains, pulvis sonus
        // same as InventoryHourglass.ItemSlotHourglass.IsValidGrain()
        public bool CanFall()
        {
            ItemStack stack = InputStack;
            if (stack == null)
                return false;

            string path = stack.Collectible.Code.Path; // use as unique identifier for items
            if (path.StartsWith("grain-") || path.Contains("sand_grains") || path.Contains("pulvis_sonus"))
                return true;
            else
                return false;
        }


        public override bool OnPlayerRightClick(IPlayer byPlayer, BlockSelection blockSel)
        {
            if (blockSel.SelectionBoxIndex == 1) return false;

            if (Api.World is IServerWorldAccessor)
            {
                ((ICoreServerAPI)Api).Network.SendBlockEntityPacket(
                    (IServerPlayer)byPlayer,
                    Pos.X, Pos.Y, Pos.Z,
                    (int)EnumBlockStovePacket.OpenGUI,
                    null
                );

                byPlayer.InventoryManager.OpenInventory(inventory);
                MarkDirty();
            }

            return true;
        }


        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            base.FromTreeAttributes(tree, worldForResolving);
            Inventory.FromTreeAttributes(tree.GetTreeAttribute("inventory"));

            if (Api != null)
            {
                Inventory.AfterBlocksLoaded(Api.World);
            }

            inputFallTime = tree.GetFloat("inputFallTime");

            if (Api?.Side == EnumAppSide.Client && clientDialog != null)
            {
                clientDialog.Update(inputFallTime, maxFallTime);
            }
        }


        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            ITreeAttribute invtree = new TreeAttribute();
            Inventory.ToTreeAttributes(invtree);
            tree["inventory"] = invtree;

            tree.SetFloat("inputFallTime", inputFallTime);
        }


        public override void OnReceivedClientPacket(IPlayer player, int packetid, byte[] data)
        {
            if (packetid < 1000)
            {
                Inventory.InvNetworkUtil.HandleClientPacket(player, packetid, data);

                // Tell server to save this chunk to disk again
                Api.World.BlockAccessor.GetChunkAtBlockPos(Pos.X, Pos.Y, Pos.Z).MarkModified();

                return;
            }

            if (packetid == (int)EnumBlockStovePacket.CloseGUI && player.InventoryManager != null)
            {
                player.InventoryManager.CloseInventory(Inventory);
            }
        }

        public override void OnReceivedServerPacket(int packetid, byte[] data)
        {
            if (packetid == (int)EnumBlockStovePacket.OpenGUI && (clientDialog == null || !clientDialog.IsOpened()))
            {
                clientDialog = new GUIDialogBlockEntityHourglass(DialogTitle, Inventory, Pos, Api as ICoreClientAPI);
                clientDialog.TryOpen();
                clientDialog.OnClosed += () => clientDialog = null;
                clientDialog.Update(inputFallTime, maxFallTime);
            }

            if (packetid == (int)EnumBlockEntityPacketId.Close)
            {
                IClientWorldAccessor clientWorld = (IClientWorldAccessor)Api.World;
                clientWorld.Player.InventoryManager.CloseInventory(Inventory);
            }
        }



        public ItemSlot InputSlot { get { return inventory[0]; } }

        public ItemSlot OutputSlot { get { return inventory[1]; } }

        public ItemStack InputStack
        {
            get { return inventory[0].Itemstack; }
            set { inventory[0].Itemstack = value; inventory[0].MarkDirty(); }
        }

        public ItemStack OutputStack
        {
            get { return inventory[1].Itemstack; }
            set { inventory[1].Itemstack = value; inventory[1].MarkDirty(); }
        }


        public override void OnStoreCollectibleMappings(Dictionary<int, AssetLocation> blockIdMapping, Dictionary<int, AssetLocation> itemIdMapping)
        {
            foreach (var slot in Inventory)
            {
                if (slot.Itemstack == null) continue;

                if (slot.Itemstack.Class == EnumItemClass.Item)
                {
                    itemIdMapping[slot.Itemstack.Item.Id] = slot.Itemstack.Item.Code;
                }
                else
                {
                    blockIdMapping[slot.Itemstack.Block.BlockId] = slot.Itemstack.Block.Code;
                }
            }
        }

        public override void OnLoadCollectibleMappings(IWorldAccessor worldForResolve, Dictionary<int, AssetLocation> oldBlockIdMapping, Dictionary<int, AssetLocation> oldItemIdMapping, int schematicSeed)
        {
            foreach (var slot in Inventory)
            {
                if (slot.Itemstack == null) continue;
                if (!slot.Itemstack.FixMapping(oldBlockIdMapping, oldItemIdMapping, worldForResolve))
                {
                    slot.Itemstack = null;
                }
            }
        }
    }
}
