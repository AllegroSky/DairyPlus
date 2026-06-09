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
        public CheesePotRecipe currentRecipe;

        public float prevTemperature = 20;
        public float potTemperature = 20;

        public int maxTemperature;
        public float fuelBurnTime;
        public float maxFuelBurnTime;

        public bool CanIgniteFuel;
        public bool IsBurning => fuelBurnTime > 0;

        public const float RecipeMinTemp = 60f;
        public const float MaxPotTemp = 130f;

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
            RegisterGameTickListener(Every500ms, 500);
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
                clientDialog.Update(progress, maxProgress, potTemperature, fuelBurnTime, maxFuelBurnTime);
            }

            potTemperature = tree.GetFloat("temperature", 20);
            maxTemperature = tree.GetInt("maxTemperature");
            fuelBurnTime = tree.GetFloat("fuelBurnTime");
            maxFuelBurnTime = tree.GetFloat("maxFuelBurnTime");
            CanIgniteFuel = tree.GetBool("canIgniteFuel");
        }
        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            ITreeAttribute invtree = new TreeAttribute();
            Inventory.ToTreeAttributes(invtree);
            tree["inventory"] = invtree;

            tree.SetFloat("progress", progress);
            tree.SetFloat("maxProgress", maxProgress);

            tree.SetFloat("temperature", potTemperature);
            tree.SetInt("maxTemperature", maxTemperature);
            tree.SetFloat("fuelBurnTime", fuelBurnTime);
            tree.SetFloat("maxFuelBurnTime", maxFuelBurnTime);
            tree.SetBool("canIgniteFuel", CanIgniteFuel);
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
            inventory[2],
            inventory[3]
        };

        public ItemSlot[] OutputSlots => new ItemSlot[]
        {
            inventory[4],
            inventory[5]
        };
        public ItemSlot FuelSlot => inventory[6];

        public ItemStack FuelStack
        {
            get => FuelSlot.Itemstack;
            set => FuelSlot.Itemstack = value;
        }

        public CombustibleProperties FuelProps
        {
            get
            {
                if (FuelSlot.Empty) return null;
                return FuelSlot.Itemstack.Collectible.GetCombustibleProperties(Api.World,FuelSlot.Itemstack,null);
            }
        }

        public bool TryIgnite()
        {
            Api.Logger.Notification("TryIgnite called");
            if (IsBurning) return false;
            if (!CanBurnFuel()) return false;

            CanIgniteFuel = true;
            IgniteFuel();

            return true;
        }

        public void IgniteFuel()
        {
            IgniteWithFuel(FuelSlot.Itemstack);

            FuelSlot.Itemstack.StackSize--;

            if (FuelSlot.Itemstack.StackSize <= 0)
            {
                FuelSlot.Itemstack = null;
            }

            FuelSlot.MarkDirty();
        }

        public void IgniteWithFuel(ItemStack stack)
        {
            CombustibleProperties props =
                stack.Collectible.GetCombustibleProperties(Api.World, stack, null);

            maxFuelBurnTime =
                fuelBurnTime =
                props.BurnDuration * 3f;

            maxTemperature =
                (int)Math.Min(MaxPotTemp, props.BurnTemperature);

            MarkDirty(true);
        }

        public float ChangeTemperature(float fromTemp, float toTemp, float dt)
        {
            float diff = Math.Abs(fromTemp - toTemp);

            dt = dt + dt * (diff / 28);

            if (diff < dt)
            {
                return toTemp;
            }

            if (fromTemp > toTemp)
            {
                dt = -dt;
            }

            if (Math.Abs(fromTemp - toTemp) < 1)
            {
                return toTemp;
            }

            return fromTemp + dt;
        }

        private bool CanBurnFuel()
        {
            var props = FuelProps;

            return props != null
                && props.BurnTemperature > 0;
        }

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

            UpdateHeat(dt);
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
        private void Every500ms(float dt)
        {
            if (Api is ICoreServerAPI &&
                (IsBurning || prevTemperature != potTemperature))
            {
                MarkDirty();
            }

            prevTemperature = potTemperature;
        }
        private void UpdateHeat(float dt)
        {
            if (IsBurning)
            {
                fuelBurnTime -= dt;

                if (fuelBurnTime <= 0)
                {
                    fuelBurnTime = 0;
                    maxFuelBurnTime = 0;
                }

                potTemperature =
                    ChangeTemperature(
                        potTemperature,
                        maxTemperature,
                        dt
                    );
            }
            else
            {
                potTemperature = ChangeTemperature(potTemperature,EnvironmentTemperature(), dt);
            }

            if (potTemperature >= 60)
            {
                CanIgniteFuel = true;
            }
            if (potTemperature < 60)
            {
                CanIgniteFuel = false;
            }


            if (!IsBurning && CanIgniteFuel && CanBurnFuel())
            {
                IgniteFuel();
            }
        }
        public EnumIgniteState GetIgnitableState(float secondsIgniting)
        {
            if (FuelSlot.Empty) return EnumIgniteState.NotIgnitablePreventDefault;
            if (IsBurning) return EnumIgniteState.NotIgnitablePreventDefault;

            return secondsIgniting > 3
                ? EnumIgniteState.IgniteNow
                : EnumIgniteState.Ignitable;
        }

        private int EnvironmentTemperature()
        {
            return (int)Api.World.BlockAccessor
                .GetClimateAt(Pos,EnumGetClimateMode.ForSuppliedDate_TemperatureOnly,Api.World.Calendar.TotalDays).Temperature;
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

            if (potTemperature >= RecipeMinTemp)
            {
                progress += dt;
            }
            else
            {
                progress = Math.Max(0, progress - dt);
            }

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