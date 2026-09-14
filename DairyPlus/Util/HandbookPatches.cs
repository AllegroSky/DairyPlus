using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace DairyPlus.Util
{
    [HarmonyPatch(typeof(CollectibleBehaviorHandbookTextAndExtraInfo), "addIngredientForInfo")]
    public static class CheesePotIngredientPatch
    {
        public static void Postfix(ref bool __result, ICoreClientAPI capi, ItemStack[] allStacks, ActionConsumable<string> openDetailPageFor, ItemStack stack, List<RichTextComponentBase> components, float marginTop, List<ItemStack> containers, List<ItemStack> fuels, List<ItemStack> molds, bool haveText)
        {
            var newComponents =
                CheesePotHandbookInfo.CheesePotIngredientForComponents(
                    capi,
                    stack,
                    openDetailPageFor
                );

            if (newComponents.Count == 0) return;

            if (!components.Any(comp =>
                (comp as RichTextComponent)?.DisplayText == Lang.Get("Ingredient for") + "\n"))
            {
                CollectibleBehaviorHandbookTextAndExtraInfo.AddHeading(components, capi, "Ingredient for", ref __result);

                components.Add(new ClearFloatTextComponent(capi, 2));
                components.AddRange(newComponents);
                components.Add(new ClearFloatTextComponent(capi, 3));
            }
            else
            {
                components.AddRange(newComponents);
            }
        }
    }
    
    [HarmonyPatch(typeof(CollectibleBehaviorHandbookTextAndExtraInfo), "addCreatedByInfo")]
    public static class CheesePotCreatedByPatch
    {
        public static void Postfix(ref bool __result, ICoreClientAPI capi, ItemStack[] allStacks, ActionConsumable<string> openDetailPageFor, ItemStack stack, List<RichTextComponentBase> components, float marginTop, List<ItemStack> containers, List<ItemStack> fuels, List<ItemStack> molds, bool haveText)
        {
            var newComponents = CheesePotHandbookInfo.CheesePotCreatedByComponents(capi, stack, openDetailPageFor);

            if (newComponents.Count == 0) return;

            if (!components.Any(comp => (comp as RichTextComponent)?.DisplayText == Lang.Get("dairyplus:cheesekettle-craftedby") + "\n"))
            {
                CollectibleBehaviorHandbookTextAndExtraInfo.AddHeading(components, capi, "dairyplus:cheesekettle-craftedby", ref __result);

                components.Add(new ClearFloatTextComponent(capi, 3));
                components.AddRange(newComponents);
            }
            else
            {
                components.AddRange(newComponents);
            }
        }
    }
}