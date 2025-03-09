﻿using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Fargowiltas.Items.Misc
{
    public class MapViewer : ModItem
    {
        public override string Texture => "Terraria/Images/Map_4";

        public override void SetStaticDefaults()
        {
            // DisplayName.SetDefault("The Ancient Master's Map of the Lost King's Great Ancestors");
            // Tooltip.SetDefault("Reveals the map");
            Terraria.GameContent.Creative.CreativeItemSacrificesCatalog.Instance.SacrificeCountNeededByItemId[Type] = 1;
        }

        public override void SetDefaults()
        {
            Item.width = 20;
            Item.height = 20;
            Item.value = Item.sellPrice(0, 0, 2);
            Item.rare = ItemRarityID.White;
            Item.useAnimation = 30;
            Item.useTime = 30;
            Item.useStyle = ItemUseStyleID.Shoot;
        }

        public override Nullable<bool> UseItem(Player player)/* tModPorter Suggestion: Return null instead of false */
        {
            if (Main.netMode != NetmodeID.MultiplayerClient)
            {
                for (int i = 0; i < Main.maxTilesX; i++)
                {
                    for (int j = 0; j < Main.maxTilesY; j++)
                    {
                        if (WorldGen.InWorld(i, j))
                        {
                            Main.Map.Update(i, j, 255);
                        }
                    }
                }

                Main.refreshMap = true;
            }
            else
            {
                Point center = Main.LocalPlayer.Center.ToTileCoordinates();
                int range = 300;
                for (int i = center.X - range / 2; i < center.X + range / 2; i++)
                {
                    for (int j = center.Y - range / 2; j < center.Y + range / 2; j++)
                    {
                        if (WorldGen.InWorld(i, j))
                        {
                            Main.Map.Update(i, j, 255);
                        }
                    }
                }

                Main.refreshMap = true;
            }

            return true;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.TrifoldMap)
                .AddIngredient(ItemID.Goggles)
                .AddIngredient(ItemID.Sunglasses)
                .AddIngredient(ItemID.SuspiciousLookingEye)
                .AddIngredient(ItemID.MechanicalEye)
                .AddTile(TileID.WorkBenches)
                .Register();
        }
    }
}