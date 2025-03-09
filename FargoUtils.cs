using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Terraria;
using Terraria.Audio;
using Terraria.Chat;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace Fargowiltas
{
    internal static class FargoUtils
    {
        public static readonly BindingFlags UniversalBindingFlags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;

        public static bool EternityMode => Fargowiltas.ModLoaded["FargowiltasSouls"] && (bool) ModLoader.GetMod("FargowiltasSouls").Call("EternityMode");
        public static bool HasAnyItem(this Player player, params int[] itemIDs) => itemIDs.Any(itemID => player.HasItem(itemID));

        public static bool ActuallyNight => !Main.dayTime || Main.remixWorld;
        public static FargoPlayer GetFargoPlayer(this Player player) => player.GetModPlayer<FargoPlayer>();

        public static void AddWithCondition<T>(this List<T> list, T type, bool condition)
        {
            if (condition)
            {
                list.Add(type);
            }
        }
        public static void AddDebuffImmunities(this NPC npc, List<int> debuffs)
        {
            foreach (int buffType in debuffs)
            {
                NPCID.Sets.SpecificDebuffImmunity[npc.type][buffType] = true;
            }
        }

        public static void PrintText(string text)
        {
            PrintText(text, Color.White);
        }

        public static void PrintText(string text, Color color)
        {
            if (Main.netMode == NetmodeID.SinglePlayer)
            {
                Main.NewText(text, color);
            }
            else if (Main.netMode == NetmodeID.Server)
            {
                ChatHelper.BroadcastChatMessage(NetworkText.FromLiteral(text), color);
            }
        }

        public static void PrintText(string text, int r, int g, int b) => PrintText(text, new Color(r, g, b));

        public static void PrintLocalization(string fargoKey, params object[] args) => PrintText(Language.GetTextValue($"Mods.Fargowiltas.{fargoKey}", args));

        public static void PrintLocalization(string fargoKey, Color color, params object[] args) => PrintText(Language.GetTextValue($"Mods.Fargowiltas.{fargoKey}", args), color);
    }
}
