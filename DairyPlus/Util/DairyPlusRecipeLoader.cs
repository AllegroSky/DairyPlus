using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
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

        private ICoreAPI? api;

        public List<CheesePotRecipe> GetRecipesForOutput(ItemStack stack)
        {
            return CheesePotRecipes.FindAll(recipe =>
                recipe.Outputs != null &&
                recipe.Outputs.Any(output =>
                    output.ResolvedItemStack?.Equals(api.World, stack, GlobalConstants.IgnoredStackAttributes ) == true
                )
            );
        }
        public List<CheesePotRecipe> GetRecipesUsing(ItemStack stack)
        {
            return CheesePotRecipes.FindAll(recipe =>
                recipe.Ingredients != null &&
                recipe.Ingredients.Any(ingredient =>
                    ingredient.ResolvedItemStack?.Equals( api.World, stack, GlobalConstants.IgnoredStackAttributes ) == true )
            );
        }

        public override void Start(ICoreAPI api)
        {
            this.api = api;

            CheesePotRecipes =
                api.RegisterRecipeRegistry<
                    RecipeRegistryGeneric<CheesePotRecipe>
                >("cheesepotrecipes").Recipes;
        }

        public override bool ShouldLoad(EnumAppSide forSide)
            {
            return true;
            }

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