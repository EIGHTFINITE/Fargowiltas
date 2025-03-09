using Fargowiltas.Projectiles;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.Chat;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace Fargowiltas.Items.Summons.Deviantt
{
    public class PinkSlimeCrown : ModItem  //BaseSummon
    {
        //public override int NPCType => NPCID.Pinky;

        //public override string NPCName => "Pinky";

        public override void SetStaticDefaults()
        {
            base.SetStaticDefaults();
            // DisplayName.SetDefault("Pink Slime Crown");
            // Tooltip.SetDefault("Summons Pinky");
        }

        public override void SetDefaults()
        {
            Item.width = 20;
            Item.height = 20;
            Item.maxStack = 20;
            Item.value = Item.sellPrice(0, 0, 2);
            Item.rare = ItemRarityID.LightRed;
            Item.useAnimation = 30;
            Item.useTime = 30;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.consumable = true;
        }

        public override Nullable<bool> UseItem(Player player)/* tModPorter Suggestion: Return null instead of false */
        {
            int n = NPC.NewNPC(NPC.GetBossSpawnSource(player.whoAmI), (int)player.position.X + Main.rand.Next(-800, 800), (int)player.position.Y + Main.rand.Next(-800, -250), NPCID.BlueSlime);
            Main.npc[n].SetDefaults(NPCID.Pinky);

            SoundEngine.PlaySound(SoundID.Roar, player.position);

            LocalizedText text = Language.GetText("Announcement.HasAwoken");
            string pinky = Lang.GetNPCNameValue(NPCID.Pinky);

            if (Main.netMode == NetmodeID.Server)
            {
                ChatHelper.BroadcastChatMessage(text.ToNetworkText(pinky), new Color(175, 75, 255));
            }
            else
            {
                Main.NewText(text.Format(pinky), new Color(175, 75, 255));
            }

            return true;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.SlimeCrown)
                .AddIngredient(ItemID.PinkDye)
                .AddTile(TileID.DyeVat)
                .Register();
        }
    }
}