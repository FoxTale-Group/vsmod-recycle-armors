using System;
using System.Collections.Generic;
using Vintagestory.API.Common;

namespace RecycleArmors {
    public class RecycleArmors : ModSystem {
        private static RecycleConfig Config { get; set; } = null!;

        public override void Start(ICoreAPI api) {
            base.Start(api);

            const string configFileName = "recycleArmorsConfig.json";

            try {
                Config = api.LoadModConfig<RecycleConfig>(configFileName);
            } catch (Exception e) {
                api.Logger.Error("Failed to load Recycle Armors config. Faling back to default values. Error: {0}", e);
            }

            if (Config != null) return;
            Config = new RecycleConfig();
            api.StoreModConfig(Config, configFileName);
        }

        public override void AssetsFinalize(ICoreAPI api) {
            base.AssetsFinalize(api);

            float returnRate = Config?.ReturnRate ?? 0.75f;

            foreach (GridRecipe recipe in api.World.GridRecipes)
            {
                if (recipe.Name == null || recipe.Name.Domain != "recyclearmors" || !recipe.Name.Path.Contains("recycle_")) continue;
                // Scale the bits output quantity
                if(recipe.Output?.ResolvedItemStack != null) {
                    float baseQuantity = recipe.Output.ResolvedItemStack.StackSize;
                    recipe.Output!.ResolvedItemStack.StackSize = (int)Math.Max(1, Math.Round(baseQuantity * returnRate));
                }
                // Scale the returned leather quantity
                foreach (var ingredient in recipe.ResolvedIngredients!)
                {
                    if (ingredient!.ReturnedStack?.ResolvedItemStack == null) continue;
                    float baseReturnQuantity = ingredient.ReturnedStack.ResolvedItemStack.StackSize;
                    ingredient.ReturnedStack!.ResolvedItemStack.StackSize = (int)Math.Max(1, Math.Round(baseReturnQuantity * returnRate));
                }
            }
        }
    }
}