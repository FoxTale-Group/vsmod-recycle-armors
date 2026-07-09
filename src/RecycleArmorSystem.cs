using System;
using System.Collections.Generic;
using Vintagestory.API.Common;

namespace RecycleArmors {
    public class RecycleArmors : ModSystem {
        public static RecycleConfig config { get; private set; } = null!;

        public override void Start(ICoreAPI api) {
            base.Start(api);

            string configFileName = "recycleArmorsConfig.json";

            try {
                config = api.LoadModConfig<RecycleConfig>(configFileName);
            } catch (Exception e) {
                api.Logger.Error("Failed to load Recycle Armors config. Faling back to default values. Error: {0}", e);
            }

            if (config == null) {
                config = new RecycleConfig();
                api.StoreModConfig(config, configFileName);
            }
        }

        public override void AssetsFinalize(ICoreAPI api) {
            base.AssetsFinalize(api);

            float returnRate = config?.ReturnRate ?? 0.75f;

            foreach (GridRecipe recipe in api.World.GridRecipes) {
                if (recipe.Name != null && recipe.Name.Domain == "recyclearmors" && recipe.Name.Path.Contains("recycle_"))
                {
                    // Scale the bits output quantity
                    if(recipe.Output?.ResolvedItemStack != null) {
                        float baseQuantity = recipe.Output.ResolvedItemStack.StackSize;
                        recipe.Output!.ResolvedItemStack.StackSize = (int)Math.Max(1, Math.Round(baseQuantity * returnRate));
                    }
                    // Scale the returned leather quantity
                    foreach (var ingredient in recipe.ResolvedIngredients) {
                        if(ingredient.ReturnedStack?.ResolvedItemStack != null) {
                            float baseReturnQuantity = ingredient.ReturnedStack.ResolvedItemStack.StackSize;
                            ingredient.ReturnedStack!.ResolvedItemStack.StackSize = (int)Math.Max(1, Math.Round(baseReturnQuantity * returnRate));
                        }
                    }
                }
            }
        }
    }
}