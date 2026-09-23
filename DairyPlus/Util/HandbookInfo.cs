using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace DairyPlus.Util
{
    public static class CheesePotHandbookInfo
    {
        public static List<RichTextComponentBase> CheesePotIngredientForComponents(ICoreClientAPI capi, ItemStack stack, ActionConsumable<string> openDetailPageFor)
        {
            var loader = capi.ModLoader.GetModSystem<DairyPlusRecipeLoader>();

            var recipes = loader.GetRecipesUsing(stack);

            if (recipes.Count == 0) return [];

            List<RichTextComponentBase> components = [];

            List<ItemStack> outputStacks = [];

            foreach (var recipe in recipes)
            {
                if (recipe.Outputs == null) continue;

                foreach (var output in recipe.Outputs)
                {
                    if (output?.ResolvedItemStack == null) continue;

                    if (!outputStacks.Any(existing => existing.Equals( capi.World, output.ResolvedItemStack, GlobalConstants.IgnoredStackAttributes )))
                    {
                        outputStacks.Add(output.ResolvedItemStack);
                    }
                }
            }

            while (outputStacks.Count > 0)
            {
                ItemStack dstack = outputStacks[0];
                outputStacks.RemoveAt(0);

                components.Add(new SlideshowItemstackTextComponent(capi, dstack, outputStacks, 40, EnumFloat.Inline, cs => openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs))));
            }

            return components;
        }


        public static List<RichTextComponentBase> CheesePotCreatedByComponents(ICoreClientAPI capi, ItemStack stack, ActionConsumable<string> openDetailPageFor)
        {
            var loader = capi.ModLoader.GetModSystem<DairyPlusRecipeLoader>();

            CheesePotRecipe[] recipes =
            [
                .. loader.GetRecipesForOutput(stack)
            ];

            if (recipes.Length == 0) return [];

            List<RichTextComponentBase> components = [];

            var verticalSpace = new ClearFloatTextComponent(capi, 7);

            bool firstRecipe = true;

            foreach (var recipe in recipes)
            {
                if (recipe.Ingredients == null || recipe.Outputs == null)
                {
                    continue;
                }

                if (!firstRecipe)
                {
                    components.Add(verticalSpace);
                }

                firstRecipe = false;

                bool firstItem = true;

                foreach (var ing in recipe.Ingredients)
                {
                    if (ing?.ResolvedItemStack == null)
                    {
                        continue;
                    }

                    ItemStack[] inputs =
                    [
                        ing.ResolvedItemStack.Clone()
                    ];

                    inputs[0].StackSize = ing.Quantity;

                    if (!firstItem)
                    {
                        components.Add(
                            new RichTextComponent(capi, " + ", CairoFont.WhiteMediumText())
                            {
                                VerticalAlign = EnumVerticalAlign.Middle
                            }
                        );
                    }

                    components.Add(new SlideshowItemstackTextComponent(capi, inputs, 40, EnumFloat.Inline, cs => openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs)))
                        {
                            ShowStackSize = true,
                            PaddingRight = 0
                        }
                    );

                    firstItem = false;
                }

                components.Add(new RichTextComponent(capi, " = ", CairoFont.WhiteMediumText())
                    {
                        VerticalAlign = EnumVerticalAlign.Middle
                    }
                );

                bool firstOutput = true;

                foreach (var output in recipe.Outputs)
                {
                    if (output?.ResolvedItemStack == null)
                    {
                        continue;
                    }

                    if (!firstOutput)
                    {
                        components.Add(new RichTextComponent(capi, " + ", CairoFont.WhiteMediumText())
                            {
                                VerticalAlign = EnumVerticalAlign.Middle
                            }
                        );
                    }

                    components.Add(new SlideshowItemstackTextComponent( capi, output.ResolvedItemStack, [], 40, EnumFloat.Inline, cs => openDetailPageFor(GuiHandbookItemStackPage.PageCodeForStack(cs)))
                        {
                            ShowStackSize = true,
                            PaddingRight = 0
                        }
                    );

                    firstOutput = false;
                }
            }

            components.Add(verticalSpace);

            return components;
        }
    }
}