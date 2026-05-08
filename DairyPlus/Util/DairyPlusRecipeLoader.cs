using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.GameContent;
using Vintagestory.ServerMods;

namespace DairyPlus.Util
{
    public class DairyPlusRecipeLoader : ModSystem
    {
        public List<CheesePotRecipe> CheesePotRecipes = new();
        public override double ExecuteOrder() => 1;

        public override bool ShouldLoad(EnumAppSide forSide) => forSide == EnumAppSide.Server;

        public override void AssetsLoaded(ICoreAPI api)
        {
            if (api is not ICoreServerAPI serverApi)
            {
                return;
            }

            RecipeLoader.LoadRecipes<CheesePotRecipe>(serverApi, "cheese pot recipe", "recipes/cheesepot", false, (r) => serverApi.RegisterCheesePotRecipe(r as CheesePotRecipe));
            serverApi.World.Logger.StoryEvent(Lang.Get("loadin up some cheese"));
        }
    }
    public static class CheesePotApi
    {
        public static void RegisterCheesePotRecipe(this ICoreServerAPI api, CheesePotRecipe r)
        {
            api.ModLoader.GetModSystem<DairyPlusRecipeLoader>().CheesePotRecipes.Add(r);
        }
    }
}
