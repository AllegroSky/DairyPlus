using DairyPlus.BlockEntity;
using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace DairyPlus.Blocks
{
    public class BlockCheesePot : BlockGeneric, IIgnitable
    {
        public static readonly byte[] CheesePotLight = new byte[] { 0, 2, 10 };

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
                //particle stuff
        private Random rand = new Random();
        public static SimpleParticleProperties fireParticles = new SimpleParticleProperties(2, 6, ColorUtil.ColorFromRgba(225, 225, 100, 0), new Vec3d(), new Vec3d(), new Vec3f(0, 0.08f, 0), new Vec3f(0, 0.2f, 0), 0.14f, 0f, 0.1f, 0.30f, EnumParticleModel.Quad)
        {
            AddPos = new Vec3d(0.6, 0.08, 0.6),
            SizeEvolve = new EvolvingNatFloat( EnumTransformFunction.LINEAR, 0.1f ),
            OpacityEvolve = new EvolvingNatFloat( EnumTransformFunction.LINEAR, -255 ),
            LightEmission = ColorUtil.ToRgba(255, 255, 80, 30 ),
            WindAffected = true,
            WithTerrainCollision = false,
            SelfPropelled = false
        };

        public static SimpleParticleProperties bubbleParticles = new SimpleParticleProperties(1, 1, ColorUtil.ColorFromRgba(225, 225, 225, 225), new Vec3d(), new Vec3d(), new Vec3f(0, 0.25f, 0), new Vec3f(0, 0.35f, 0), 0.1f, 0.12f, 0.3f, 0.8f, EnumParticleModel.Cube)
        {
            AddPos = new Vec3d(0.6, 0, 0.6),
            SizeEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEAR, 0.8f),
            WindAffected = false,
            WithTerrainCollision = false,
            SelfPropelled = false
        };

        public static SimpleParticleProperties steamParticles = new SimpleParticleProperties(0, 1, ColorUtil.ColorFromRgba(225, 225, 225, 225), new Vec3d(), new Vec3d(), new Vec3f(0, 0.2f, 0), new Vec3f(0, 0.4f, 0), 1f, -0.03f, 0.1f, 0.3f, EnumParticleModel.Quad)
        {
            AddPos = new Vec3d(0.3, 0, 0.3),
            SizeEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEAR, 3f),
            OpacityEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEAR, -255),
            WindAffected = true,
            WithTerrainCollision = true,
            SelfPropelled = false
        };


        public override bool ShouldReceiveClientParticleTicks(IWorldAccessor world, IPlayer player, BlockPos pos, out bool isWindAffected)
            {
            isWindAffected = true; return true;
            }

        public override void OnAsyncClientParticleTick(IAsyncParticleManager manager, BlockPos pos, float windAffectednessAtPos, float secondsTicking)
        {
            BECheesePot? beche = api.World.BlockAccessor.GetBlockEntity(pos) as BECheesePot;

            if (beche != null && beche.IsBurning)
            {
                fireParticles.MinPos.Set( pos.X + 0.2, pos.Y, pos.Z + 0.2 );
                fireParticles.Color = ColorUtil.ToRgba( 255, 255, rand.Next(20, 220), 0 );

                manager.Spawn(fireParticles);
            }

            if (beche != null && beche.syncedContentsState == "boiling")
            {
                bubbleParticles.MinPos.Set(pos.X + 0.2, pos.Y + 0.68, pos.Z + 0.2);
                bubbleParticles.Color = ColorUtil.ToRgba(255, 210, 210, rand.Next(170, 190));
                manager.Spawn(bubbleParticles);

                steamParticles.MinPos.Set(pos.X + 0.2, pos.Y + 0.68, pos.Z + 0.2);
                steamParticles.Color = ColorUtil.ToRgba(150, 210, 210, rand.Next(170, 190));
                manager.Spawn(steamParticles);
            }

        }
        public override byte[] GetLightHsv(IBlockAccessor blockAccessor, BlockPos pos, ItemStack stack = null)
        {
            if (pos == null)
            {
                return base.GetLightHsv(blockAccessor, pos, stack);
            }

            BECheesePot? beche = blockAccessor.GetBlockEntity(pos) as BECheesePot;

            if (beche != null && beche.IsBurning)
            {
                return CheesePotLight;
            }

            return base.GetLightHsv(blockAccessor, pos, stack);
        }
        public override void OnBlockBroken(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)
        {
            BECheesePot? beche = world.BlockAccessor.GetBlockEntity(pos) as BECheesePot;

            if (beche != null && beche.IsBurning)
            {world.BlockAccessor.RemoveBlockLight(BlockCheesePot.CheesePotLight, pos);}

            base.OnBlockBroken(world, pos, byPlayer, dropQuantityMultiplier);
        }
    }
}