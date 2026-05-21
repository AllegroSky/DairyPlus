using DairyPlus.GUI;
using DairyPlus.Inventory;
using DairyPlus.Util;
using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace DairyPlus.BlockEntity
{
    public class BECheesePot : BlockEntityOpenableContainer
    {
        public int CapacityLitres { get; set; } = 30;
        protected InventoryCheesePot inventory;
        protected GuiDialogCheesePot clientDialog;
        private float progress = 0f;
        private float maxProgress = 1f;
        private CheesePotRecipe currentRecipe;

        public override string InventoryClassName
        {
            get { return "cheesepot"; }
        }

        public virtual string DialogTitle
        {
            get { return Lang.Get("Cheese Pot"); }
        }

        public override InventoryBase Inventory
        {
            get { return inventory; }
        }

        public BECheesePot()
        {
            inventory = new InventoryCheesePot(null, null);
            inventory.SlotModified += OnSlotModified;
        }

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);

            inventory.LateInitialize("cheesepot-" + Pos.X + "/" + Pos.Y + "/" + Pos.Z, api);

            RegisterGameTickListener(Every100ms, 100);
        }


        protected void OnSlotModified(int slotid)
        {
            MarkDirty(true);
        }


        public override bool OnPlayerRightClick(IPlayer byPlayer, BlockSelection blockSel)
        {
            if (Api.Side == EnumAppSide.Client)
            {
                toggleInventoryDialogClient(byPlayer, () =>
                {
                    clientDialog = new GuiDialogCheesePot(DialogTitle, Inventory, Pos, Api as ICoreClientAPI);
                    return clientDialog;
                });
            }

            return true;
        }

        public override void OnReceivedClientPacket(IPlayer player, int packetid, byte[] data)
        {
            base.OnReceivedClientPacket(player, packetid, data);
        }

        public override void OnReceivedServerPacket(int packetid, byte[] data)
        {
            base.OnReceivedServerPacket(packetid, data);

            if (packetid == (int)EnumBlockEntityPacketId.Close)
            {
                (Api.World as IClientWorldAccessor).Player.InventoryManager.CloseInventory(Inventory);
                invDialog?.TryClose();
                invDialog?.Dispose();
                invDialog = null;
            }
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            base.FromTreeAttributes(tree, worldForResolving);
            Inventory.FromTreeAttributes(tree.GetTreeAttribute("inventory"));

            if (Api != null)
            {
                Inventory.AfterBlocksLoaded(Api.World);
            }

            progress = tree.GetFloat("progress");
            maxProgress = tree.GetFloat("maxProgress");
            if (Api?.Side == EnumAppSide.Client && clientDialog != null)
            {
                clientDialog.Update(progress, maxProgress);
            }
        }
        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            ITreeAttribute invtree = new TreeAttribute();
            Inventory.ToTreeAttributes(invtree);
            tree["inventory"] = invtree;

            tree.SetFloat("progress", progress);
            tree.SetFloat("maxProgress", maxProgress);
        }

        public override void OnBlockRemoved()
        {
            base.OnBlockRemoved();
            clientDialog?.TryClose();
        }


        //slots

        public ItemSlot[] InputSlots => new ItemSlot[]
        {
            inventory[0],
            inventory[1],
            inventory[2]
        };

        public ItemSlot[] OutputSlots => new ItemSlot[]
        {
            inventory[3],
            inventory[4]
        };

        public CheesePotRecipe FindMatchingRecipe(out int outputSize)
        {
            outputSize = 0;

            var loader = Api.ModLoader.GetModSystem<DairyPlusRecipeLoader>();
            if (loader?.CheesePotRecipes == null) return null;

            foreach (var recipe in loader.CheesePotRecipes)
            {
                if (recipe.Matches(InputSlots, out outputSize))
                {
                    return recipe;
                }
            }

            outputSize = 0;
            return null;
        }

        private void Every100ms(float dt)
        {
            if (Api.Side == EnumAppSide.Client) return;

            UpdateRecipe();

            if (!ValidateRecipe())
            {
                progress = 0;
                MarkDirty();
                return;
            }

            ProcessRecipe(dt);

            MarkDirty();
        }

        private void UpdateRecipe()
        {
            if (currentRecipe == null)
            {
                currentRecipe = FindMatchingRecipe(out int _);
                progress = 0;
            }

            if (currentRecipe != null)
            {
                maxProgress = (float)currentRecipe.ProcessingTime;
            }
        }
        private bool ValidateRecipe()
        {
            if (currentRecipe == null) return false;

            if (!currentRecipe.Matches(InputSlots, out _))
            {
                currentRecipe = null;
                progress = 0;
                return false;
            }
            return true;
        }
        private void ProcessRecipe(float dt)
        {
            if (currentRecipe == null) return;

            progress += dt;

            if (progress < maxProgress) return;
            currentRecipe.TryCraftNow(Api, currentRecipe.ProcessingTime, InputSlots, OutputSlots);

            //cleanup
            progress = 0;
            currentRecipe = null;
            MarkDirty(true);
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
                slot.Itemstack?.Collectible.OnStoreCollectibleMappings(Api.World, slot, blockIdMapping, itemIdMapping);
            }
        }

        public override void OnLoadCollectibleMappings(IWorldAccessor worldForResolve, Dictionary<int, AssetLocation> oldBlockIdMapping, Dictionary<int, AssetLocation> oldItemIdMapping, int schematicSeed, bool resolveImports)
        {
            foreach (var slot in Inventory)
            {
                if (slot.Itemstack == null) continue;
                if (!slot.Itemstack.FixMapping(oldBlockIdMapping, oldItemIdMapping, worldForResolve))
                {
                    slot.Itemstack = null;
                }
                slot.Itemstack?.Collectible.OnLoadCollectibleMappings(worldForResolve, slot, oldBlockIdMapping, oldItemIdMapping, resolveImports);
            }
        }

    }   
}