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

        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            BlockEntityChurn beChun = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityChurn;

            if (beChun != null)
            {
                if (blockSel.SelectionBoxIndex == 1)
                {
                    beChun.SetPlayerGrinding(byPlayer, true);
                    return true;
                }
                else
                {
                    beChun.OnPlayerRightClick(byPlayer, blockSel);
                    return true;
                }
            }
            return false;
        }

        public override bool OnBlockInteractStep(float secondsUsed, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            BlockEntityChurn beChun = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BlockEntityChurn;

            if (beChun != null && (blockSel.SelectionBoxIndex == 1))
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
    }
}