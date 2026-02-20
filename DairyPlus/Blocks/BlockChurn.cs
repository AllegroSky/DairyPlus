using DairyPlus.BlockEntity;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;
using Vintagestory.GameContent.Mechanics;

namespace DairyPlus.Blocks
{
    public class BlockChurn : BlockGeneric
    {

               // for mechpower

       // public override bool TryPlaceBlock(IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref string failureCode)
       // {
       //     bool ok = base.TryPlaceBlock(world, byPlayer, itemstack, blockSel, ref failureCode);
       //
       //     if (ok)
       //     {
       //         if (!tryConnect(world, byPlayer, blockSel.Position, BlockFacing.UP))
       //         {
       //             tryConnect(world, byPlayer, blockSel.Position, BlockFacing.DOWN);
       //         }
       //     }
       //     return ok;
       // }

      //  public override bool DoPartialSelection(IWorldAccessor world, BlockPos pos)
      //  {
      //      return true;
      //  }

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            BlockEntityChurn beChun = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityChurn;

            if (beChun != null && beChun.CanChurn() && (blockSel.SelectionBoxIndex == 1 || beChun.Inventory.openedByPlayerGUIds.Contains(byPlayer.PlayerUID)))
            {
                beChun.SetPlayerGrinding(byPlayer, true);
                return true;
            }

            return base.OnBlockInteractStart(world, byPlayer, blockSel);
        }

        public override bool OnBlockInteractStep(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            BlockEntityChurn beChun = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityChurn;

            if (beChun != null && (blockSel.SelectionBoxIndex == 1 || beChun.Inventory.openedByPlayerGUIds.Contains(byPlayer.PlayerUID)))
            {
                beChun.IsGrinding(byPlayer);
                return beChun.CanChurn();
            }

            return false;
        }

        public override void OnBlockInteractStop(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            BlockEntityChurn beChun = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityChurn;
            if (beChun != null)
            {
                beChun.SetPlayerGrinding(byPlayer, false);
            }

        }

        public override bool OnBlockInteractCancel(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, EnumItemUseCancelReason cancelReason)
        {
            BlockEntityChurn beChun = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityChurn;
            if (beChun != null)
            {
                beChun.SetPlayerGrinding(byPlayer, false);
            }


            return true;
        }
        /*
        public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer)
        {
            if (selection.SelectionBoxIndex == 0)
            {
                return new WorldInteraction[] {
                    new WorldInteraction()
                    {
                        ActionLangCode = "blockhelp-quern-addremoveitems",
                        MouseButton = EnumMouseButton.Right
                    }
                }.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
            }
            else
            {
                return new WorldInteraction[] {
                    new WorldInteraction()
                    {
                        ActionLangCode = "blockhelp-quern-grind",
                        MouseButton = EnumMouseButton.Right,
                        ShouldApply = (wi, bs, es) => {
                            BlockEntityChurn beChun = world.BlockAccessor.GetBlockEntity(bs.Position) as BlockEntityChurn;
                            return beChun != null && beChun.CanGrind();
                        }
                    }
                }.Append(base.GetPlacedBlockInteractionHelp(world, selection, forPlayer));
            }
        }
       
        

        public override void DidConnectAt(IWorldAccessor world, BlockPos pos, BlockFacing face)
        {

        }


        public override bool HasMechPowerConnectorAt(IWorldAccessor world, BlockPos pos, BlockFacing face, BlockMPBase forBlock)
        {
            return face == BlockFacing.UP || face == BlockFacing.DOWN;
        }
        */     
    }
}