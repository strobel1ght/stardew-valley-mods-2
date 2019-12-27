﻿using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using System.Collections.Generic;
using xTile.ObjectModel;

namespace ShopTileFramework
{
    class ModEntry : Mod
    {
        public static IModHelper helper;
        public static IMonitor monitor;
        public static IJsonAssetsApi JsonAssets;
        private Dictionary<string, Shop> Shops { get; set; }
        public override void Entry(IModHelper h)
        {
            //make helper and monitor static so they can be accessed in other classes
            helper = h;
            monitor = Monitor;

            Shops = new Dictionary<string, Shop>();

            helper.Events.Input.ButtonPressed += Input_ButtonPressed;
            helper.Events.GameLoop.DayStarted += GameLoop_DayStarted;
            helper.Events.GameLoop.GameLaunched += GameLoop_GameLaunched;

            LoadContentPacks();
        }

        private void GameLoop_GameLaunched(object sender, StardewModdingAPI.Events.GameLaunchedEventArgs e)
        {
            JsonAssets = helper.ModRegistry.GetApi<IJsonAssetsApi>("spacechase0.JsonAssets");

            if (JsonAssets == null)
            {
                Monitor.Log("Json Assets API not detected. Custom JA items will not be added to shops.",
                    LogLevel.Info);
            }
        }

        private void GameLoop_DayStarted(object sender, StardewModdingAPI.Events.DayStartedEventArgs e)
        {
            //refresh the stock of each store every day
            Monitor.Log($"Refreshing stock for all custom shops...", LogLevel.Debug);
            foreach (Shop Store in Shops.Values)
            {
                Store.UpdateItemPriceAndStock();
            }
        }

        private void Input_ButtonPressed(object sender, StardewModdingAPI.Events.ButtonPressedEventArgs e)
        {
            //context and button check
            if (!Context.CanPlayerMove || e.Button.IsActionButton() || e.Button == SButton.MouseRight)
                return;

            //check if clicked tile is near the player
            var clickedTile = Helper.Input.GetCursorPosition().Tile;
            if (!IsClickWithinReach(clickedTile))
                return;

            //check if there is a tile property on Buildings layer
            var tileProperty = GetTileProperty(Game1.currentLocation, "Buildings" ,clickedTile);
            if (tileProperty == null)
                return;

            //check if there is a Shop property on clicked tile
            tileProperty.TryGetValue("Shop", out PropertyValue shopProperty);
            if (shopProperty == null)
                return;

            //Extract the tile property value
            string ShopName = shopProperty.ToString();
            if (Shops.ContainsKey(ShopName))
            {
                helper.Input.Suppress(e.Button);
                Shops[ShopName].DisplayStore();
            } else
            {
                Monitor.Log($"A Shop tile was clicked, but a shop by the name \"{ShopName}\" " +
                    $"was not found.",LogLevel.Debug);
            }

        }
        private static IPropertyCollection GetTileProperty(GameLocation map, string layer, Vector2 tile)
        {
            if (map == null)
                return null;

            var checkTile = map.Map.GetLayer(layer).Tiles[(int)tile.X, (int)tile.Y];

            if (checkTile == null)
                return null;

            return checkTile.Properties;
        }

        private void LoadContentPacks()
        {
            monitor.Log("Adding Content Packs...", LogLevel.Info);
            foreach (IContentPack contentPack in helper.ContentPacks.GetOwned())
            {
                if (!contentPack.HasFile("shops.json"))
                {
                    monitor.Log($"No shops.json found from the mod {contentPack.Manifest.UniqueID}. " +
                        $"Skipping pack.", LogLevel.Warn);
                }
                else
                {
                    ContentModel data = contentPack.ReadJsonFile<ContentModel>("shops.json");
                    Monitor.Log($"{contentPack.Manifest.Name} by {contentPack.Manifest.Author}| " +
                        $"{contentPack.Manifest.Version} | {contentPack.Manifest.Description}", LogLevel.Info);
                    foreach (ShopPack s in data.Shops)
                    {
                        if (Shops.ContainsKey(s.ShopName))
                        {
                            monitor.Log($"{contentPack.Manifest.UniqueID} is trying to add the shop " +
                                $"\"{s.ShopName}\", but a shop of this name has already been added. " +
                                $"It will not be added.", LogLevel.Warn);

                        } else
                        {
                            Shops.Add(s.ShopName, new Shop(s, contentPack));
                        }
                    }
                }

            }
        }

        private bool IsClickWithinReach(Vector2 tile)
        {
            var playerPosition = Game1.player.Position;
            var playerTile = new Vector2(playerPosition.X / 64, playerPosition.Y / 64);

            if (tile.X < (playerTile.X - 1.5) || tile.X > (playerTile.X + 1.5))
                return false;

            if (tile.Y < (playerTile.Y - 1.5) || tile.Y > (playerTile.Y + 1.5))
                return false;

            return true;
        }
    }

    public interface IJsonAssetsApi
    {
        List<string> GetAllCropsFromContentPack(string cp);
        List<string> GetAllFruitTreesFromContentPack(string cp);
        List<string> GetAllBigCraftablesFromContentPack(string cp);
        List<string> GetAllHatsFromContentPack(string cp);
        List<string> GetAllWeaponsFromContentPack(string cp);
        List<string> GetAllClothingFromContentPack(string cp);

        int GetObjectId(string name);
        int GetCropId(string name);
        int GetFruitTreeId(string name);
        int GetBigCraftableId(string name);
        int GetHatId(string name);
        int GetWeaponId(string name);
        int GetClothingId(string name);
    }
}
