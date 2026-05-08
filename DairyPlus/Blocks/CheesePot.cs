using DairyPlus.BlockEntity;
using Vintagestory.API.Common;

namespace DairyPlus.Blocks
{
    public class CheesePot : BlockGeneric
    {
        public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
        {
            BECheesePot? beCpo = world.BlockAccessor.GetBlockEntity(blockSel.Position) as BECheesePot;

            if (beCpo != null)
            {
               beCpo.OnPlayerRightClick(byPlayer, blockSel);
               return true;   
            }
            return false;
        }
    }
}
