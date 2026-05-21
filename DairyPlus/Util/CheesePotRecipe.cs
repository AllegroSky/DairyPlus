using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Vintagestory.API;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace DairyPlus.Util
{
    [DocumentAsJson]
    public class CheesePotRecipe : RecipeBase, IByteSerializable, IConcreteCloneable<CheesePotRecipe>
    {
        [DocumentAsJson("Required")]
        public string? Code { get; set; }

        [DocumentAsJson("Required")]
        public CheesePotIngredient[]? Ingredients { get; set; }

        [DocumentAsJson("Required")]
        public CheesePotOutputStack[]? Outputs { get; set; }

        [DocumentAsJson("Required")]
        public double ProcessingTime { get; set; }

        public override IEnumerable<IRecipeIngredient> RecipeIngredients => Ingredients;
        public override IRecipeOutput RecipeOutput => null; // doing this manually



        public override void OnParsed(IWorldAccessor world)
        {
            if (Ingredients == null) return;

            int ingredientIndex = 1;
            foreach (CheesePotIngredient ingredient in Ingredients)
            {
                ingredient.Id ??= ingredientIndex++.ToString();
            }
        }
        public bool Matches(ItemSlot[] inputSlots, out int outputStackSize)
        {
            outputStackSize = 0;

            List<(ItemSlot slot, CheesePotIngredient ingredient)> matched = PairInput(inputSlots);
            if (matched.Count == 0)
            {
                return false;
            }

            outputStackSize = GetOutputSize(matched);

            return outputStackSize >= 0;
        }

        public bool Matches(IPlayer forPlayer, ItemSlot[] inputSlots, out int outputStackSize)
        {
            outputStackSize = 0;

            if (!forPlayer.Entity.Api.Event.TriggerMatchesRecipe(forPlayer, this, inputSlots))
            {
                return false;
            }

            return Matches(inputSlots, out outputStackSize);
        }

        public bool TryCraftNow(ICoreAPI api, double curProcessingTime, ItemSlot[] inputSlots, ItemSlot[] outputSlot)
        {
            if (ProcessingTime > 0 && curProcessingTime < ProcessingTime)
            {
                return false;
            }

            List<(ItemSlot slot, CheesePotIngredient ingredient)> matched = PairInput(inputSlots);

            int outputStackSize = GetOutputSize(matched);
            if (outputStackSize < 0 || Outputs == null || Outputs.Length == 0 || Outputs[0].ResolvedItemStack == null)
            {
                return false;
            }

            List<ItemStack> craftedStacks = new();

            foreach (var output in Outputs)
            {
                if (output?.ResolvedItemStack == null) continue;

                ItemStack stack = output.ResolvedItemStack.Clone();

                stack.StackSize = (int)(output.StackSize * (outputStackSize / (double)Outputs[0].StackSize));

                CarryOverFreshness(api, stack, inputSlots);
                craftedStacks.Add(stack);
            }

            int emptySlots = 0;

            foreach (var slot in outputSlot)
            {
                if (slot.Empty) emptySlots++;
            }

            if (emptySlots < craftedStacks.Count)
            {
                return false; 
            }

            foreach (ItemStack stack in craftedStacks)
            {
                foreach (var slot in outputSlot)
                {
                    if (slot.Empty)
                    {
                        slot.Itemstack = stack;
                        slot.MarkDirty();
                        break;
                    }
                }
            }


            foreach ((ItemSlot slot, CheesePotIngredient ingredient) in matched)
            {
                slot.Itemstack = null;
                slot.MarkDirty();
            }

            ItemSlot input1 = inputSlots[0];
            ItemSlot input2 = inputSlots[1];
            ItemSlot input3 = inputSlots[2];
            ItemSlot output1 = outputSlot[0];
            ItemSlot output2 = outputSlot[1];

            return true;
        }

        public override void ToBytes(BinaryWriter writer)
        {
            base.ToBytes(writer);

            if (Code == null || Ingredients == null || Outputs == null)
            {
                throw new InvalidOperationException("Cannot serialize cheesepot recipes: some of the properties are null");
            }

            writer.Write(Code);
            writer.Write(Ingredients.Length);
            for (int i = 0; i < Ingredients.Length; i++)
            {
                Ingredients[i].ToBytes(writer);
            }

            writer.Write(Outputs.Length);
            for (int i = 0; i < Outputs.Length; i++)
            {
                Outputs[i].ToBytes(writer);
            }

            writer.Write(ProcessingTime);
        }

        public override void FromBytes(BinaryReader reader, IWorldAccessor resolver)
        {
            base.FromBytes(reader, resolver);

            Code = reader.ReadString();
            CheesePotIngredient[] Ingredients = new CheesePotIngredient[reader.ReadInt32()];
            this.Ingredients = Ingredients;

            for (int i = 0; i < Ingredients.Length; i++)
            {
                var ingredient = new CheesePotIngredient();
                ingredient.FromBytes(reader, resolver);
                ingredient.Resolve(resolver, "CheesePot Recipe (FromBytes)", this);
                Ingredients[i] = ingredient;
            }

            int count = reader.ReadInt32();
            Outputs = new CheesePotOutputStack[count];

            for (int i = 0; i < count; i++)
            {
                var output = new CheesePotOutputStack();
                output.FromBytes(reader, resolver.ClassRegistry);
                output.Resolve(resolver, "CheesePot Recipe (FromBytes)");
                Outputs[i] = output;
            }

            ProcessingTime = reader.ReadDouble();
        }

        public override bool Resolve(IWorldAccessor world, string sourceForErrorLogging)
        {
            bool resolved = true;

            if (Ingredients == null || Outputs == null)
            {
                world.Logger.Error($"Cannot resolve CheesePot recipe '{Name}', either Ingredients or Output are not specified");
                return false;
            }

            foreach (CheesePotIngredient ingredient in Ingredients)
            {
                resolved &= ingredient.Resolve(world, sourceForErrorLogging, this);
            }

            foreach (var output in Outputs)
            {
                resolved &= output.Resolve(world, sourceForErrorLogging);
            }

            return resolved;
        }
        public override CheesePotRecipe Clone()
        {
            CheesePotRecipe recipe = new();

            CloneTo(recipe);

            return recipe;
        }

        protected override void CloneTo(object recipe)
        {
            base.CloneTo(recipe);

            if (recipe is not CheesePotRecipe cheesepotRecipe)
            {
                throw new ArgumentException("CloneTo should take object of same class or it subclass");
            }

            if (Outputs != null)
            {
                cheesepotRecipe.Outputs = new CheesePotOutputStack[Outputs.Length];
                for (int i = 0; i < Outputs.Length; i++)
                {
                    cheesepotRecipe.Outputs[i] = Outputs[i].Clone();
                }
            }
            cheesepotRecipe.ProcessingTime = ProcessingTime;
            cheesepotRecipe.Code = Code;
            if (Ingredients != null)
            {
                cheesepotRecipe.Ingredients = new CheesePotIngredient[Ingredients.Length];
                for (int i = 0; i < Ingredients.Length; i++)
                {
                    cheesepotRecipe.Ingredients[i] = Ingredients[i].Clone();
                }
            }
        }

        protected virtual List<(ItemSlot slot, CheesePotIngredient ingredient)> PairInput(ItemSlot[] inputSlots)
        {
            int stackCount = 0;
            foreach (ItemSlot inputSlot in inputSlots)
            {
                if (!inputSlot.Empty)
                {
                    stackCount++;
                }
            }

            if (Ingredients == null || stackCount != Ingredients.Length)
            {
                return [];
            }

            List<(ItemSlot slot, CheesePotIngredient ingredient)> matched = [];
            List<CheesePotIngredient> ingredientsToProcess = Ingredients.ToList();

            foreach (ItemSlot inputSlot in inputSlots)
            {
                if (inputSlot.Itemstack == null) continue;

                CheesePotIngredient? ingredient = ingredientsToProcess.Find(ingredient => MatchStackToIngredient(inputSlot.Itemstack, ingredient));
                if (ingredient != null)
                {
                    matched.Add((inputSlot, ingredient));
                    ingredientsToProcess.Remove(ingredient);
                }
                else
                {
                    return [];
                }
            }

            // We're missing ingredients
            if (matched.Count < Ingredients.Length)
            {
                return [];
            }

            return matched;
        }
        protected virtual int GetOutputSize(List<(ItemSlot slot, CheesePotIngredient ingredient)> matched)
        {
            int multiplier = -1;

            foreach ((ItemSlot slot, CheesePotIngredient ingredient) in matched)
            {
                if (ingredient.Quantity <= 0) return -1;

                int thisMul = slot.StackSize / ingredient.Quantity;

                // no remainders
                if (slot.StackSize % ingredient.Quantity != 0) return -1;

                if (multiplier == -1)
                {
                    multiplier = thisMul;
                }
                else if (multiplier != thisMul)
                {
                    return -1;
                }
            }

            if (multiplier <= 0) return -1;

            return Outputs[0].StackSize * multiplier;
        }
        protected virtual void CarryOverFreshness(ICoreAPI api, ItemStack mixedStack, ItemSlot[] inputSlots)
        {
            TransitionableProperties[] props = mixedStack.Collectible.GetTransitionableProperties(api.World, mixedStack, null);
            TransitionableProperties? perishProps = props?.FirstOrDefault(p => p.Type == EnumTransitionType.Perish);

            if (perishProps != null)
            {
                CollectibleObject.CarryOverFreshness(api, inputSlots, [mixedStack], perishProps);
            }
        }

    }




    // INGREDIENTS




    [DocumentAsJson]
    public class CheesePotIngredient : CraftingRecipeIngredient
    {

        /// If the ingredient is a liquid, will use this value instead of CraftingRecipeIngredient.Quantity
        [DocumentAsJson("Optional", "None")]
        public float Litres = -1;

        public override CheesePotIngredient Clone()
        {
            CheesePotIngredient result = new();

            CloneTo(result);

            return result;
        }

        public override void FromBytes(BinaryReader reader, IWorldAccessor resolver)
        {
            base.FromBytes(reader, resolver);
            Litres = reader.ReadSingle();
        }

        public override void ToBytes(BinaryWriter writer)
        {
            base.ToBytes(writer);
            writer.Write(Litres);
        }

        public override bool Resolve(IWorldAccessor world, string sourceForErrorLogging)
        {
            if (!base.Resolve(world, sourceForErrorLogging))
            {
                return false;
            }

            ResolveLiquidProperties(world, sourceForErrorLogging);

            return true;
        }

        protected virtual void ResolveLiquidProperties(IWorldAccessor world, string sourceForErrorLogging)
        {
            WaterTightContainableProps? liquidProperties = BlockLiquidContainerBase.GetContainableProps(ResolvedItemStack);

            if (liquidProperties == null) return;
            if (Litres > 0)
            {
                Quantity = (int)(liquidProperties.ItemsPerLitre * Litres);
            }

        }
        protected override void CloneTo(object cloneTo)
        {
            base.CloneTo(cloneTo);

            if (cloneTo is CheesePotIngredient ingredient)
            {
                ingredient.Litres = Litres;
            }
        }
    }


    // OUTPUT

    [DocumentAsJson]
    public class CheesePotOutputStack : JsonItemStack, IConcreteCloneable<CheesePotOutputStack>
    {
        [DocumentAsJson("Optional", "0")]
        public float Litres;

        public override void FromBytes(BinaryReader reader, IClassRegistryAPI instancer)
        {
            base.FromBytes(reader, instancer);

            Litres = reader.ReadSingle();
        }
        public override void ToBytes(BinaryWriter writer)
        {
            base.ToBytes(writer);

            writer.Write(Litres);
        }

        public override bool Resolve(IWorldAccessor world, string sourceForErrorLogging)
        {
            if (!base.Resolve(world, sourceForErrorLogging))
            {
                return false;
            }

            ResolveLiquidProperties(world, sourceForErrorLogging);

            return true;
        }

        public override CheesePotOutputStack Clone()
        {
            CheesePotOutputStack result = new();

            CloneTo(result);

            return result;
        }



        protected override void CloneTo(object stack)
        {
            base.CloneTo(stack);

            if (stack is CheesePotOutputStack cheesepotOutput)
            {
                cheesepotOutput.Litres = Litres;
            }
        }
        protected virtual void ResolveLiquidProperties(IWorldAccessor world, string sourceForErrorLogging)
        {
            WaterTightContainableProps? liquidProperties = BlockLiquidContainerBase.GetContainableProps(ResolvedItemStack);
            if (liquidProperties != null)
            {
                if (Litres < 0)
                {
                    if (Quantity > 0)
                    {
                        world.Logger.Warning($"({sourceForErrorLogging}) Cheesepot recipe output {Code} does not define a litres attribute but a stacksize, will assume stacksize=litres for backwards compatibility.");
                        Litres = Quantity;
                    }
                    else
                    {
                        Litres = 1;
                    }

                }

                Quantity = (int)(liquidProperties.ItemsPerLitre * Litres);
            }
        }


    }
}
