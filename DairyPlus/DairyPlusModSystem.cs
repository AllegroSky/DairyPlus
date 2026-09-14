using DairyPlus.BlockEntity;
using DairyPlus.Blocks;
using DairyPlus.Items;
using DairyPlus.Util;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using HarmonyLib;

namespace DairyPlus;

public class DairyPlusModSystem : ModSystem
{
    
    public override void Start(ICoreAPI api)
    {
        api.RegisterItemClass(Mod.Info.ModID + ".skimcurd", typeof(ItemSkimCurd));

        api.RegisterBlockClass(Mod.Info.ModID + ".creamscoop", typeof(BlockCreamScoop));

        api.RegisterBlockClass(Mod.Info.ModID + ".churn", typeof(BlockChurn));
        api.RegisterBlockEntityClass(Mod.Info.ModID + ".bechurn", typeof(BlockEntityChurn));

        api.RegisterBlockClass(Mod.Info.ModID + ".cheesepot", typeof(BlockCheesePot));
        api.RegisterBlockEntityClass(Mod.Info.ModID + ".becheesepot", typeof(BECheesePot));

    }

    public override void StartServerSide(ICoreServerAPI api)
    {
        Mod.Logger.Notification("Hello from template mod server side: " + Lang.Get("dairyplus:hello"));
    }

    private Harmony harmony;
    private static bool patched;
    public override void StartClientSide(ICoreClientAPI api)
    {
        if (patched) return;
        patched = true;

        harmony ??= new Harmony("dairyplus");
        harmony.PatchAll();

    }

}
