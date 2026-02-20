using System.Collections.Generic;
using System.Linq;
using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;
using Vintagestory.GameContent.Mechanics;
using DairyPlus.GUI;
using DairyPlus.Inventory;


namespace DairyPlus.BlockEntity
{
    public class BlockEntityChurn : BlockEntityOpenableContainer
    {
       
        protected InventoryChurn inventory;
        // For how long the current ore has been grinding
        public float inputGrindTime;
        public float prevInputGrindTime;
        protected int nowOutputFace;
        protected GuiDialogChurn clientDialog;
        protected float prevSpeed = float.NaN;
        // Server side only
        protected Dictionary<string, long> playersGrinding = new Dictionary<string, long>();
        // Client and serverside
        protected int quantityPlayersGrinding;


        #region Getters
        /*
        public string Material
        {
            get { return Block.LastCodePart(); }
        }
        */

        public float GrindSpeed
        {
            get
            {
                if (quantityPlayersGrinding > 0) return 1f;

                //if (automated && mpc.Network != null) return mpc.TrueSpeed;

                return 0;
            }
        }

        #endregion

        #region Config

        // seconds it requires to melt the ore once beyond melting point
        public virtual float maxGrindingTime()
        {
            return 4;
        }

        public override string InventoryClassName
        {
            get { return "churn"; }
        }

        public virtual string DialogTitle
        {
            get { return Lang.Get("Churn"); }
        }

        public override InventoryBase Inventory
        {
            get { return inventory; }
        }

        #endregion


        public BlockEntityChurn()
        {
            inventory = new InventoryChurn(null, null);
            inventory.SlotModified += OnSlotModified;
        }

        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);

            inventory.LateInitialize("churn" + Pos.X + "/" + Pos.Y + "/" + Pos.Z, api);

            RegisterGameTickListener(Every100ms, 100);
            RegisterGameTickListener(Every500ms, 500);       
            
        }
       
/*
        public override void CreateBehaviors(Block block, IWorldAccessor worldForResolve)
        {
            base.CreateBehaviors(block, worldForResolve);

            mpc = GetBehavior<BEBehaviorMPConsumer>();
            if (mpc != null)
            {
                mpc.OnConnected = () => {
                    automated = true;
                    quantityPlayersGrinding = 0;  
                };

                mpc.OnDisconnected = () => {
                    automated = false;
                    
                };
            }
        }
*/

        public void IsGrinding(IPlayer byPlayer)
        {
            SetPlayerGrinding(byPlayer, true);
        }

        protected void Every100ms(float dt)
        {
            float grindSpeed = GrindSpeed;

            if (Api.Side == EnumAppSide.Client)
            {
                prevSpeed = float.NaN;

                return;
            }


            // Only tick on the server and merely sync to client
            if (CanChurn() && grindSpeed > 0)
            {
                inputGrindTime += dt * grindSpeed;

                if (inputGrindTime >= maxGrindingTime())
                {
                    grindInput();
                    inputGrindTime = 0;
                }

                MarkDirty();
            }
        }

        protected void grindInput()
        {
              ItemStack butterStack = new ItemStack(
               Api.World.GetItem(new AssetLocation("dairyplus:butter-unsalted")),
                  1 );

            if (OutputSlot.Itemstack == null)
            {
                OutputSlot.Itemstack = butterStack;
            }
            else
            {
                int mergableQuantity = OutputSlot.Itemstack.Collectible.GetMergableQuantity(OutputSlot.Itemstack, butterStack, EnumMergePriority.AutoMerge);

                if (mergableQuantity > 0)
                {
                    OutputSlot.Itemstack.StackSize += butterStack.StackSize;
                }
                /*
                else
                {
                    BlockFacing face = BlockFacing.HORIZONTALS[nowOutputFace];
                    nowOutputFace = (nowOutputFace + 1) % 4;
                    Block block = Api.World.BlockAccessor.GetBlock(Pos.AddCopy(face));
                    if (block.Replaceable < 6000) return;
                    var position = Pos.ToVec3d().Add(0.5 + face.Normalf.X * 0.7, 0.75, 0.5 + face.Normalf.Z * 0.7);
                    Api.World.SpawnItemEntity(butterStack, position, new Vec3d(face.Normalf.X * 0.02f, 0, face.Normalf.Z * 0.02f));
                }*/
            }

            InputSlot.TakeOut(1);
            InputSlot.MarkDirty();
            OutputSlot.MarkDirty();
        }


        // Sync to client every 500ms
        protected void Every500ms(float dt)
        {
            if (Api.Side == EnumAppSide.Server && (GrindSpeed > 0 || prevInputGrindTime != inputGrindTime) && inventory[0].Itemstack != null)  //don't spam update packets when empty
            {
                MarkDirty();
            }

            prevInputGrindTime = inputGrindTime;


            foreach (var val in playersGrinding)
            {
                long ellapsedMs = Api.World.ElapsedMilliseconds;
                if (ellapsedMs - val.Value > 1000)
                {
                    playersGrinding.Remove(val.Key);
                    break;
                }
            }
        }


        public void SetPlayerGrinding(IPlayer player, bool playerGrinding)
        {
     
                if (playerGrinding)
                {
                    playersGrinding[player.PlayerUID] = Api.World.ElapsedMilliseconds;
                }
                else
                {
                    playersGrinding.Remove(player.PlayerUID);
                }

                quantityPlayersGrinding = playersGrinding.Count;
           
            updateGrindingState();
        }

        bool beforeGrinding;
        void updateGrindingState()
        {
            if (Api?.World == null) return;

            bool nowGrinding = quantityPlayersGrinding > 0;

            if (nowGrinding != beforeGrinding)
            {
           
            }

            beforeGrinding = nowGrinding;
        }




        protected void OnSlotModified(int slotid)
        {
            if (Api is ICoreClientAPI)
            {
                clientDialog.Update(inputGrindTime, maxGrindingTime());
            }

            if (slotid == 0)
            {
                if (InputSlot.Empty)
                {
                    inputGrindTime = 0.0f; // reset the progress to 0 if the item is removed.
                }
                MarkDirty();

                if (clientDialog != null && clientDialog.IsOpened())
                {
                    clientDialog.SingleComposer.ReCompose();
                }
            }
        }

        /*
        public bool CanGrind()
        {
            GrindingProperties grindProps = InputGrindProps;
            if (grindProps == null) return false;
            return true;
        }
        */



        #region Events

        public override bool OnPlayerRightClick(IPlayer byPlayer, BlockSelection blockSel)
        {
            if (blockSel.SelectionBoxIndex == 1) return false;

            if (Api.Side == EnumAppSide.Client)
            {
                toggleInventoryDialogClient(byPlayer, () => {
                    clientDialog = new GuiDialogChurn(DialogTitle, Inventory, Pos, Api as ICoreClientAPI);
                    clientDialog.Update(inputGrindTime, maxGrindingTime());
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


            inputGrindTime = tree.GetFloat("inputGrindTime");
            nowOutputFace = tree.GetInt("nowOutputFace");

            if (worldForResolving.Side == EnumAppSide.Client)
            {
                List<int> clientIds = new List<int>((tree["clientIdsGrinding"] as IntArrayAttribute).value);

                quantityPlayersGrinding = clientIds.Count;

                string[] playeruids = playersGrinding.Keys.ToArray();

                foreach (var uid in playeruids)
                {
                    IPlayer plr = Api.World.PlayerByUid(uid);

                    if (!clientIds.Contains(plr.ClientId))
                    {
                        playersGrinding.Remove(uid);
                    }
                    else
                    {
                        clientIds.Remove(plr.ClientId);
                    }
                }

                for (int i = 0; i < clientIds.Count; i++)
                {
                    IPlayer plr = worldForResolving.AllPlayers.FirstOrDefault(p => p.ClientId == clientIds[i]);
                    if (plr != null) playersGrinding.Add(plr.PlayerUID, worldForResolving.ElapsedMilliseconds);
                }

                updateGrindingState();
            }


            if (Api?.Side == EnumAppSide.Client && clientDialog != null)
            {
                clientDialog.Update(inputGrindTime, maxGrindingTime());
            }
        }



        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            ITreeAttribute invtree = new TreeAttribute();
            Inventory.ToTreeAttributes(invtree);
            tree["inventory"] = invtree;

            tree.SetFloat("inputGrindTime", inputGrindTime);
            tree.SetInt("nowOutputFace", nowOutputFace);
            List<int> vals = new List<int>();
            foreach (var val in playersGrinding)
            {
                IPlayer plr = Api.World.PlayerByUid(val.Key);
                if (plr == null) continue;
                vals.Add(plr.ClientId);
            }


            tree["clientIdsGrinding"] = new IntArrayAttribute(vals.ToArray());
        }




        public override void OnBlockRemoved()
        {
            base.OnBlockRemoved();

            clientDialog?.TryClose();
        }


        #endregion

        #region Helper getters


        public ItemSlot InputSlot
        {
            get { return inventory[0]; }
        }

        public ItemSlot OutputSlot
        {
            get { return inventory[1]; }
        }

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

        /*
        public GrindingProperties InputGrindProps
        {
            get
            {
                ItemSlot slot = inventory[0];
                if (slot.Itemstack == null) return null;
                return slot.Itemstack.Collectible.GetGrindingProperties(Api.World, slot.Itemstack);
            }
        }
        */

        public bool CanChurn()
        {
            ItemStack stack = InputSlot.Itemstack;
            if (stack == null) return false;

            return stack.Collectible.Code.Path.Contains("cream");
        }

        #endregion


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

