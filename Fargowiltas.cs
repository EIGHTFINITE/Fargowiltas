using Fargowilta;
using Fargowiltas.Common.Configs;
using Fargowiltas.NPCs;
using Fargowiltas.UI;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.Chat;
using Terraria.GameContent.Events;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace Fargowiltas
{
    public class Fargowiltas : Mod
    {
        // Hotkeys
        public static ModKeybind HomeKey;

        public static ModKeybind StatKey;

        public static ModKeybind DashKey;

        public static ModKeybind SetBonusKey;

        public static UIManager UserInterfaceManager => Instance._userInterfaceManager;
        private UIManager _userInterfaceManager;

        // Swarms (Energized bosses) 
        public static bool SwarmActive;
        public static bool HardmodeSwarmActive;
        public static bool SwarmNoHyperActive;
        public static int SwarmItemsUsed;
        public static bool SwarmSetDefaults;
        public static int SwarmMinDamage
        { 
            get
            {
                float dmg;
                if (HardmodeSwarmActive)
                    dmg = 57 + 36 * SwarmItemsUsed;
                else
                    dmg = 46 + 3 * SwarmItemsUsed;
                if (Main.masterMode)
                    dmg /= 1.2f;
                return (int)dmg;
            }
                
        }

        // Mod loaded bools
        internal static Dictionary<string, bool> ModLoaded;
        internal static Dictionary<int, string> ModRareEnemies = [];
        internal static List<Action> ModEventActions = [];
        internal static List<Func<bool>> ModEventActiveFuncs = [];

        public List<StatSheetUI.Stat> ModStats;
        public List<StatSheetUI.PermaUpgrade> PermaUpgrades;

        private string[] mods;

        internal static Fargowiltas Instance;

        public override void Load()
        {
            Instance = this;

            ModStats = new();
            PermaUpgrades = new List<StatSheetUI.PermaUpgrade>
            {
                new(ContentSamples.ItemsByType[ItemID.AegisCrystal], () => Main.LocalPlayer.usedAegisCrystal),
                new(ContentSamples.ItemsByType[ItemID.AegisFruit], () => Main.LocalPlayer.usedAegisFruit),
                new(ContentSamples.ItemsByType[ItemID.ArcaneCrystal], () => Main.LocalPlayer.usedArcaneCrystal),
                new(ContentSamples.ItemsByType[ItemID.Ambrosia], () => Main.LocalPlayer.usedAmbrosia),
                new(ContentSamples.ItemsByType[ItemID.GummyWorm], () => Main.LocalPlayer.usedGummyWorm),
                new(ContentSamples.ItemsByType[ItemID.GalaxyPearl], () => Main.LocalPlayer.usedGalaxyPearl),
                new(ContentSamples.ItemsByType[ItemID.ArtisanLoaf], () => Main.LocalPlayer.ateArtisanBread),
            };

            HomeKey = KeybindLoader.RegisterKeybind(this, "Home", "Home");

            StatKey = KeybindLoader.RegisterKeybind(this, "Stat", "RightShift");

            DashKey = KeybindLoader.RegisterKeybind(this, "Dash", "C");

            SetBonusKey = KeybindLoader.RegisterKeybind(this, "SetBonus", "V");

            _userInterfaceManager = new UIManager();
            _userInterfaceManager.LoadUI();

            mods =
            [
                "FargowiltasSouls", // Fargo's Souls
                "FargowiltasSoulsDLC",
                "ThoriumMod",
                "CalamityMod",
                "MagicStorage",
                "WikiThis"
            ];

            ModLoaded = new Dictionary<string, bool>();
            foreach (string mod in mods)
            {
                ModLoaded.Add(mod, false);
            }

            // DD2 Banner Effect hack
            ItemID.Sets.BannerStrength = ItemID.Sets.Factory.CreateCustomSet(new ItemID.BannerEffect(1f));

            Terraria.On_Player.DoCommonDashHandle += OnVanillaDash;
            Terraria.On_Player.KeyDoubleTap += OnVanillaDoubleTapSetBonus;
            Terraria.On_Player.KeyHoldDown += OnVanillaHoldSetBonus;

            Terraria.On_Recipe.FindRecipes += FindRecipes_ElementalAssemblerGraveyardHack;
            Terraria.On_Player.HasUnityPotion += OnHasUnityPotion;
            Terraria.On_Player.TakeUnityPotion += OnTakeUnityPotion;
            Terraria.On_Player.DropTombstone += DisableTombstones;
        }

        private static IEnumerable<Item> GetWormholes(Player self) =>
            self.inventory
                .Concat(self.bank.item)
                .Concat(self.bank2.item)
                .Where(x => x.type == ItemID.WormholePotion);

        private static void OnTakeUnityPotion(Terraria.On_Player.orig_TakeUnityPotion orig, Player self)
        {
            var wormholes = GetWormholes(self).ToList();

            if (
                FargoServerConfig.Instance.UnlimitedPotionBuffsOn120
                && wormholes.Select(x => x.stack).Sum() >= 30
            )
            {
                return;
            }

            // Can't be empty as we're gated by HasUnityPotion
            Item pot = wormholes.First();

            pot.stack -= 1;

            if (pot.stack <= 0)
                pot.SetDefaults(0, false);
        }

        private static void DisableTombstones(Terraria.On_Player.orig_DropTombstone orig, Player self, long coinsOwned, NetworkText deathText, int hitDirection)
        {
            if (FargoServerConfig.Instance.DisableTombstones)
                return;

            orig(self, coinsOwned, deathText, hitDirection);
        }

        private static bool OnHasUnityPotion(Terraria.On_Player.orig_HasUnityPotion orig, Player self)
        {
            return GetWormholes(self).Select(x => x.stack).Sum() > 0;
        }

        private static void FindRecipes_ElementalAssemblerGraveyardHack(
            Terraria.On_Recipe.orig_FindRecipes orig,
            bool canDelayCheck)
        {
            bool oldZoneGraveyard = Main.LocalPlayer.ZoneGraveyard;

            if (!Main.gameMenu && Main.LocalPlayer.active && Main.LocalPlayer.GetModPlayer<FargoPlayer>().ElementalAssemblerNearby > 0)
                Main.LocalPlayer.ZoneGraveyard = true;

            orig(canDelayCheck);

            Main.LocalPlayer.ZoneGraveyard = oldZoneGraveyard;
        }

        public override void Unload()
        {
            Terraria.On_Player.DoCommonDashHandle -= OnVanillaDash;
            Terraria.On_Player.KeyDoubleTap -= OnVanillaDoubleTapSetBonus;
            Terraria.On_Player.KeyHoldDown -= OnVanillaHoldSetBonus;

            Terraria.On_Recipe.FindRecipes -= FindRecipes_ElementalAssemblerGraveyardHack;
            Terraria.On_Player.HasUnityPotion -= OnHasUnityPotion;
            Terraria.On_Player.TakeUnityPotion -= OnTakeUnityPotion;
            Terraria.On_Player.DropTombstone -= DisableTombstones;

            HomeKey = null;
            StatKey = null;
            mods = null;
            ModLoaded = null;

            Instance = null;
        }

        public override void PostSetupContent()
        {
            try
            {
                foreach (string mod in mods)
                {
                    ModLoaded[mod] = ModLoader.TryGetMod(mod, out Mod otherMod);
                }
            }
            catch (Exception e)
            {
                Logger.Error("Fargowiltas PostSetupContent Error: " + e.StackTrace + e.Message);
            }

            if (ModLoader.TryGetMod("Wikithis", out Mod wikithis) && !Main.dedServ)
            {
                wikithis.Call("AddModURL", this, "https://fargosmods.wiki.gg/wiki/{}");
            }
        }

        public override object Call(params object[] args)
        {
            try
            {
                string code = args[0].ToString();

                switch (code)
                {
                    case "AddIndestructibleTileType":
                        {
                            if (args[1].GetType() == typeof(int))
                            {
                                int tile = (int)args[1];
                                FargoSets.Tiles.InstaCannotDestroy[tile] = true;
                            }
                        }
                        break;
                    case "AddIndestructibleWallType":
                        {
                            if (args[1].GetType() == typeof(int))
                            {
                                int wall = (int)args[1];
                                FargoSets.Walls.InstaCannotDestroy[wall] = true;
                            }
                        }
                        break;
                    case "AddEvilAltar":
                        {
                            if (args[1].GetType() == typeof(int))
                            {
                                int tile = (int)args[1];
                                FargoSets.Tiles.EvilAltars[tile] = true;
                            }
                        }
                        break;
                    case "AddStat":
                        {
                            if (args[1].GetType() != typeof(int))
                                throw new Exception($"Call Error (Fargo Mutant Mod AddStat): args[1] must be of type int");
                            if (args[2].GetType() != typeof(Func<string>))
                                throw new Exception($"Call Error (Fargo Mutant Mod AddStat): args[2] must be of type Func<string>");

                            int itemID = (int)args[1];
                            Func<string> TextFunction = (Func<string>)args[2];
                            ModStats.Add(new StatSheetUI.Stat(itemID, TextFunction));
                        }
                        break;
                    case "AddPermaUpgrade":
                        {
                            if (args[1].GetType() != typeof(Item))
                                throw new Exception($"Call Error (Fargo Mutant Mod AddStat): args[1] must be of type Item");
                            if (args[2].GetType() != typeof(Func<bool>))
                                throw new Exception($"Call Error (Fargo Mutant Mod AddStat): args[2] must be of type Func<bool>");

                            Item item = (Item)args[1];
                            Func<bool> ConsumedFunction = (Func<bool>)args[2];
                            PermaUpgrades.Add(new StatSheetUI.PermaUpgrade(item, ConsumedFunction));
                        }
                        break;
                    case "SwarmActive":
                        return SwarmActive;

                    case "DoubleTapDashDisabled":
                        return FargoClientConfig.Instance.DoubleTapDashDisabled;
                }

            }
            catch (Exception e)
            {
                Logger.Error("Call Error: " + e.StackTrace + e.Message);
            }

            return base.Call(args);
        }

        public override void HandlePacket(BinaryReader reader, int whoAmI)
        {
            byte messageType = reader.ReadByte();

            switch (messageType)
            {
                // Regal statue
                case 1:
                    {
                        if (whoAmI >= 0 && whoAmI < FargoWorld.CurrentSpawnRateTile.Length)
                        {
                            FargoWorld.CurrentSpawnRateTile[whoAmI] = reader.ReadBoolean();
                        }                        
                    }
                    break;

                // Angler reset
                case 3:
                    if (Main.netMode == NetmodeID.Server)
                    {
                        Main.AnglerQuestSwap();
                    }
                    break;

                // Sync npc max life
                case 4:
                    {
                        int n = reader.ReadInt32();
                        int lifeMax = reader.ReadInt32();
                        if (Main.netMode == NetmodeID.MultiplayerClient && n >= 0 && n < Main.maxNPCs)
                            Main.npc[n].lifeMax = lifeMax;
                    }
                    break;

                    //client requested server to update world
                case 6:
                    if (Main.netMode == NetmodeID.Server)
                    {
                        NetMessage.SendData(MessageID.WorldData);
                    }
                    break;
            }
        }

        private static void OnVanillaDash(Terraria.On_Player.orig_DoCommonDashHandle orig, Terraria.Player player, out int dir, out bool dashing, Player.DashStartAction dashStartAction)
        {
            if (FargoClientConfig.Instance.DoubleTapDashDisabled)
            {
                player.dashTime = 0;
            }
                

            orig.Invoke(player, out dir, out dashing, dashStartAction);

            if (player.whoAmI == Main.myPlayer && DashKey.JustPressed && !player.CCed)
            {
                InputManager modPlayer = player.GetModPlayer<InputManager>();
                if (player.controlRight && player.controlLeft)
                {
                    dir = modPlayer.latestXDirPressed;
                }
                else if (player.controlRight)
                {
                    dir = 1;
                }
                else if (player.controlLeft)
                {
                    dir = -1;
                }
                if (dir == 0) // this + commented out below because changed to not have an effect when not holding any movement keys; primarily so it's affected by stun effects
                    return;
                player.direction = dir;
                dashing = true;
                if (player.dashTime > 0)
                {
                    player.dashTime--;
                }
                if (player.dashTime < 0)
                {
                    player.dashTime++;
                }
                if ((player.dashTime <= 0 && player.direction == -1) || (player.dashTime >= 0 && player.direction == 1))
                {
                    player.dashTime = 15;
                    return;
                }
                dashing = true;
                player.dashTime = 0;
                player.timeSinceLastDashStarted = 0;
                if (dashStartAction != null)
                    dashStartAction?.Invoke(dir);
            }

        }
        private static void OnVanillaDoubleTapSetBonus(On_Player.orig_KeyDoubleTap orig, Player player, int keyDir)
        {
            if (!FargoClientConfig.Instance.DoubleTapSetBonusDisabled || SetBonusKey.JustPressed)
            {
                orig.Invoke(player, keyDir);
            }
        }
        private static void OnVanillaHoldSetBonus(On_Player.orig_KeyHoldDown orig, Player player, int keyDir, int holdTime)
        {
            if (!FargoClientConfig.Instance.DoubleTapSetBonusDisabled || SetBonusKey.Current)
            {
                orig.Invoke(player, keyDir, holdTime);
            }
        }
    }
}

