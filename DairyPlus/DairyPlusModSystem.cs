using Vintagestory.API.Client;
using Vintagestory.API.Server;
using Vintagestory.API.Config;
using Vintagestory.API.Common;
using DairyPlus.Blocks;
using DairyPlus.BlockEntity;
using DairyPlus.Items;

namespace DairyPlus;

public class DairyPlusModSystem : ModSystem
{

    // Called on server and client
    // Useful for registering block/entity classes on both sides
    public override void Start(ICoreAPI api)
    {

        api.RegisterBlockClass(Mod.Info.ModID + ".creamscoop", typeof(BlockCreamScoop));

        api.RegisterBlockClass(Mod.Info.ModID + ".churn", typeof(BlockChurn)); 
        api.RegisterBlockEntityClass(Mod.Info.ModID + ".bechurn", typeof(BlockEntityChurn));

    }

    public override void StartServerSide(ICoreServerAPI api)
    {
        Mod.Logger.Notification("Hello from template mod server side: " + Lang.Get("dairyplus:hello"));
    }

    public override void StartClientSide(ICoreClientAPI api)
    {
        Mod.Logger.Notification("Hello from template mod client side: " + Lang.Get("dairyplus:hello"));
    }

}
