using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace RecycleArmors {
    public class RecycleArmors : ModSystem {
        private static RecycleConfig Config { get; set; } = null!;
        
        public override double ExecuteOrder() => 0.1;

        public override void StartPre(ICoreAPI api) {
            base.StartPre(api);

            const string configFileName = "recycleArmorsConfig.json";

            try {
                Config = api.LoadModConfig<RecycleConfig>(configFileName);
            } catch (Exception e) {
                api.Logger.Error("Failed to load Recycle Armors config. Faling back to default values. Error: {0}", e);
            }

            if (Config == null) Config = new RecycleConfig();
            
            api.StoreModConfig(Config, configFileName);
        }

        public override void AssetsLoaded(ICoreAPI api) {
            base.AssetsLoaded(api);
            
            float toolDurabilityCostMult = Config?.ToolDurabilityCostMultiplier ?? 1.0f;
            api.Logger.Event($"[RecycleArmors] LOADED TOOL DURABILITY COST MULT: {toolDurabilityCostMult}");
            
            foreach (IAsset asset in api.Assets.GetMany("recipes/grid/")) {
                if (asset.Location.Domain != "recyclearmors" || !asset.Location.Path.Contains("recycle_")) continue;

                try {
                    // Read the file as a raw text string
                    string json = asset.ToText();
                    JObject root = JObject.Parse(json);
                    
                    if (root["ingredients"] != null) {
                        // Loop through every ingredient defined in the JSON 
                        foreach (var prop in root["ingredients"].Children<JProperty>()) {
                            JToken ingredient = prop.Value;
                            
                            // Scale tool durability cost
                            if (ingredient["isTool"] != null && ingredient["isTool"].Value<bool>() == true) {
                                float baseDur = ingredient["toolDurabilityCost"]?.Value<float>() ?? 1f;
                                int newDur = (int)Math.Max(1, Math.Round(baseDur * toolDurabilityCostMult));
                                
                                ingredient["toolDurabilityCost"] = newDur;
                                api.Logger.Event($"[RecycleArmors] JSON REWRITE {asset.Location}: {prop.Name} durability set to {newDur}");
                            }
                        }
                    }

                    // Write the modified JSON text back into the virtual file!
                    asset.Data = Encoding.UTF8.GetBytes(root.ToString());

                } catch (Exception e) {
                    api.Logger.Error($"[RecycleArmors] Failed to modify raw JSON for {asset.Location}: {e}");
                }
            
            }
        }

        public override void AssetsFinalize(ICoreAPI api) {
            base.AssetsFinalize(api);

            float returnRate = Config?.ReturnRate ?? 0.75f;
            api.Logger.Event($"[RecycleArmors] LOADED RETURN RATE: {returnRate}");
            
            
            foreach (GridRecipe recipe in api.World.GridRecipes)
            {
                if (recipe.Name == null || recipe.Name.Domain != "recyclearmors" || !recipe.Name.Path.Contains("recycle_")) continue;
                
                // Scale the bits output quantity
                if(recipe.Output?.ResolvedItemStack != null) {
                    float baseQuantity = recipe.Output.ResolvedItemStack.StackSize;
                    recipe.Output!.ResolvedItemStack.StackSize = (int)Math.Max(1, Math.Round(baseQuantity * returnRate));
                }
                
                // Grab all recipe ingredients
                var allIngredients = new List<CraftingRecipeIngredient>();
                if (recipe.ResolvedIngredients != null) allIngredients.AddRange(recipe.ResolvedIngredients.OfType<CraftingRecipeIngredient>());
                if (recipe.Ingredients != null) allIngredients.AddRange(recipe.Ingredients.Values.OfType<CraftingRecipeIngredient>());

                foreach (var ingredient in allIngredients.Distinct())
                {
                    // Scale the returned leather quantity
                    if (ingredient.ReturnedStack?.ResolvedItemStack != null) {
                        float baseReturnQuantity = ingredient.ReturnedStack.ResolvedItemStack.StackSize; 
                        ingredient.ReturnedStack.ResolvedItemStack.StackSize = (int)Math.Max(1, Math.Round(baseReturnQuantity * returnRate));
                    }
                }
            }
        }
    }
}