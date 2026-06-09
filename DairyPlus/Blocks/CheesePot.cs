using DairyPlus.BlockEntity;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace DairyPlus.Blocks
{
    public class BlockCheesePot : BlockGeneric, IIgnitable
    {
        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            BECheesePot? beCh = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BECheesePot;

            if (beCh != null)
            {
               beCh.OnPlayerRightClick(byPlayer, blockSel);
               return true;   
            }
            return false;
        }

        public EnumIgniteState OnTryIgniteBlock(EntityAgent byEntity, BlockPos pos, float secondsIgniting)
        {
            BECheesePot beCh = api.World.BlockAccessor.GetBlockEntity(pos) as BECheesePot;

            return beCh?.GetIgnitableState(secondsIgniting)
                ?? EnumIgniteState.NotIgnitable;
        }

        public void OnTryIgniteBlockOver(EntityAgent byEntity, BlockPos pos, float secondsIgniting, ref EnumHandling handling)
        {
            if (secondsIgniting < 3) return;

            BECheesePot be = api.World.BlockAccessor.GetBlockEntity(pos) as BECheesePot;

            if (be?.TryIgnite() == true)
            {
                handling = EnumHandling.PreventDefault;
            }
        }

        EnumIgniteState IIgnitable.OnTryIgniteStack(EntityAgent byEntity, BlockPos pos, ItemSlot slot, float secondsIgniting)
        {
            return EnumIgniteState.NotIgnitable;
        }
    }
}