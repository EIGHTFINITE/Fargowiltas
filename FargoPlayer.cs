using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.GameInput;
using Terraria.ID;
using Terraria.ModLoader;
using Fargowiltas.NPCs;
using System.Linq;
using Terraria.ModLoader.IO;
using Fargowiltas.Items;
using Terraria.GameContent.Events;
using System.IO;
using Fargowiltas.Common.Configs;

namespace Fargowiltas
{
    public class FargoPlayer : ModPlayer
    {
        public bool extractSpeed;
        public bool HasDrawnDebuffLayer;

        internal int originalSelectedItem;
        internal bool autoRevertSelectedItem;

        public float luckPotionBoost;
        public float ElementalAssemblerNearby;

        public float StatSheetMaxAscentMultiplier;
        public float StatSheetWingSpeed;
        public bool? CanHover = null;

        public int StationSoundCooldown;

        public int DeathCamTimer = 0;
        public int SpectatePlayer = 0;

        private readonly string[] tags =
        [
            "RedHusk",
            "OrangeBloodroot",
            "YellowMarigold",
            "LimeKelp",
            "GreenMushroom",
            "TealMushroom",
            "CyanHusk",
            "SkyBlueFlower",
            "BlueBerries",
            "PurpleMucos",
            "VioletHusk",
            "PinkPricklyPear",
            "BlackInk"
        ];

        public override void SyncPlayer(int toWho, int fromWho, bool newPlayer)
        {
            ModPacket packet = Mod.GetPacket();
            packet.Write((byte)9);
            packet.Write((byte)Player.whoAmI);
            packet.Send(toWho, fromWho);
        }

        public override void ResetEffects()
        {
            extractSpeed = false;
            HasDrawnDebuffLayer = false;
        }
        public override void ProcessTriggers(TriggersSet triggersSet)
        {

            if (Fargowiltas.HomeKey.JustPressed)
            {
                AutoUseMirror();
            }

            if (Fargowiltas.StatKey.JustPressed)
            {
                if (!Main.playerInventory)
                {
                    Main.playerInventory = true;
                }
                Fargowiltas.UserInterfaceManager.ToggleStatSheet();
            }
        }

        public override void PostUpdateBuffs()
        {
            if (FargoServerConfig.Instance.UnlimitedPotionBuffsOn120)
            {
                foreach (Item item in Player.bank.item)
                {
                    FargoGlobalItem.TryUnlimBuff(item, Player);
                }

                foreach (Item item in Player.bank2.item)
                {
                    FargoGlobalItem.TryUnlimBuff(item, Player);
                }
            }

            if (FargoServerConfig.Instance.PiggyBankAcc || FargoServerConfig.Instance.ModdedPiggyBankAcc)
            {
                foreach (Item item in Player.bank.item)
                {
                    FargoGlobalItem.TryPiggyBankAcc(item, Player);
                }

                foreach (Item item in Player.bank2.item)
                {
                    FargoGlobalItem.TryPiggyBankAcc(item, Player);
                }
            }
        }

        public override void UpdateDead()
        {
            StationSoundCooldown = 0;
            if (FargoClientConfig.Instance.MultiplayerDeathSpectate && Player.dead && Main.netMode != NetmodeID.SinglePlayer && Main.player.Any(p => p != null && !p.dead && !p.ghost))
            {
                Spectate();
               
            }
        }
        public void FindNewSpectateTarget() => SpectatePlayer = SpectatePlayer = Main.player.First(ValidSpectateTarget).whoAmI;
        public bool ValidSpectateTarget(Player p) => p != null && !p.dead && !p.ghost;
        public void Spectate()
        {
            if (SpectatePlayer < 0 || SpectatePlayer > Main.maxPlayers)
                FindNewSpectateTarget();
            if (SpectatePlayer < 0 || SpectatePlayer > Main.maxPlayers)
                return;
            Player spectatePlayer = Main.player[SpectatePlayer];
            if (spectatePlayer == null || !spectatePlayer.active || spectatePlayer.dead || spectatePlayer.ghost)
            {
                FindNewSpectateTarget();
                spectatePlayer = Main.player[SpectatePlayer];
            }
                
            if (spectatePlayer == null || !spectatePlayer.active || spectatePlayer.dead || spectatePlayer.ghost)
                return;

            if (Main.mouseLeft && Main.mouseLeftRelease)
            {
                for (int i = 0; i < Main.maxPlayers + 1; i++)
                {
                    SpectatePlayer--;
                    if (SpectatePlayer < 0)
                        SpectatePlayer = Main.maxPlayers - 1;
                    if (ValidSpectateTarget(Main.player[SpectatePlayer]))
                        break;
                }
            }
            else if (Main.mouseRight && Main.mouseRightRelease)
            {
                for (int i = 0; i < Main.maxPlayers + 1; i++)
                {
                    SpectatePlayer++;
                    if (SpectatePlayer >= Main.maxPlayers)
                        SpectatePlayer = 0;
                    if (ValidSpectateTarget(Main.player[SpectatePlayer]))
                        break;
                }
            }
            spectatePlayer = Main.player[SpectatePlayer];

            Vector2 spectatePos = spectatePlayer.Center;
            if (Player.Center.Distance(spectatePos) > 2000)
            {
                DeathCamTimer++;
                if (DeathCamTimer > 60)
                {
                    Player.Center = spectatePos + spectatePos.DirectionTo(Player.Center) * 1000;
                    DeathCamTimer = 0;
                }

            }
            else
            {
                DeathCamTimer++;
                float lerp = DeathCamTimer / 200f;
                lerp = MathHelper.Clamp(lerp, 0, 1);
                Player.Center = Vector2.Lerp(Player.Center, spectatePos, lerp);
            }
        }
        public override void Kill(double damage, int hitDirection, bool pvp, PlayerDeathReason damageSource)
        {
            FindNewSpectateTarget();
        }
        public override void PostUpdateMiscEffects()
        {
            if (ElementalAssemblerNearby > 0)
            {
                ElementalAssemblerNearby -= 1;
                Player.alchemyTable = true;
            }
            if (StationSoundCooldown > 0)
                StationSoundCooldown--;

            if (Player.equippedWings == null)
                ResetStatSheetWings();

            ForceBiomes();
        }
        public override void ModifyHitByNPC(NPC npc, ref Player.HurtModifiers modifiers)
        {
            #region Stat Sliders
            FargoServerConfig config = FargoServerConfig.Instance;
            if (config.EnemyDamage != 1 || config.BossDamage != 1)
            {
                bool boss = config.BossDamage > config.EnemyDamage && // only relevant if boss health is higher than enemy health
                    (npc.boss || npc.type == NPCID.EaterofWorldsHead || npc.type == NPCID.EaterofWorldsBody || npc.type == NPCID.EaterofWorldsTail || (config.BossApplyToAllWhenAlive && FargoGlobalNPC.AnyBossAlive()));
                if (boss)
                    modifiers.FinalDamage *= config.BossDamage;
                else
                    modifiers.FinalDamage *= config.EnemyDamage;
            }
            #endregion
        }
        public void ResetStatSheetWings()
        {
            StatSheetMaxAscentMultiplier = 0;
            StatSheetWingSpeed = 0;
            CanHover = null;
        }

        private void ForceBiomes()
        {
            if (FargoGlobalNPC.SpecificBossIsAlive(ref FargoGlobalNPC.eaterBoss, NPCID.EaterofWorldsHead)
                && Player.Distance(Main.npc[FargoGlobalNPC.eaterBoss].Center) < 3000)
            {
                Player.ZoneCorrupt = true;
            }

            if (FargoGlobalNPC.SpecificBossIsAlive(ref FargoGlobalNPC.brainBoss, NPCID.BrainofCthulhu)
                && Player.Distance(Main.npc[FargoGlobalNPC.brainBoss].Center) < 3000)
            {
                Player.ZoneCrimson = true;
            }

            if ((FargoGlobalNPC.SpecificBossIsAlive(ref FargoGlobalNPC.plantBoss, NPCID.Plantera)
                && Player.Distance(Main.npc[FargoGlobalNPC.plantBoss].Center) < 3000)
                || (FargoGlobalNPC.SpecificBossIsAlive(ref FargoGlobalNPC.beeBoss, NPCID.QueenBee)
                && Player.Distance(Main.npc[FargoGlobalNPC.beeBoss].Center) < 3000))
            {
                Player.ZoneJungle = true;
            }

            if (FargoServerConfig.Instance.Fountains)
            {
                switch (Main.SceneMetrics.ActiveFountainColor)
                {
                    case -1: //no fountain active
                        goto default;

                    case 0: //pure water, ocean
                        Player.ZoneBeach = true;
                        break;

                    case 2: //corrupt
                        Player.ZoneCorrupt = true;
                        break;

                    case 3: //jungle
                        Player.ZoneJungle = true;
                        break;

                    case 4: //hallow
                        if (Main.hardMode)
                            Player.ZoneHallow = true;
                        break;

                    case 5: //ice
                        Player.ZoneSnow = true;
                        break;

                    case 6: //oasis
                        goto case 12;

                    case 8: //cavern
                        goto default;

                    case 9: //blood fountain
                        goto default;

                    case 10: //crimson
                        Player.ZoneCrimson = true;
                        break;

                    case 12: //desert fountain
                        Player.ZoneDesert = true;
                        if (Player.Center.Y > 3200f)
                            Player.ZoneUndergroundDesert = true;
                        break;

                    default:
                        break;
                }
            }
        }

        public override void PostUpdate()
        {
            if (autoRevertSelectedItem)
            {
                if (Player.itemTime == 0 && Player.itemAnimation == 0)
                {
                    Player.selectedItem = originalSelectedItem;
                    autoRevertSelectedItem = false;
                }
            }
        }

        public override void ModifyLuck(ref float luck)
        {
            luck += luckPotionBoost;

            luckPotionBoost = 0; //look nowhere else works ok
        }
        public override void ModifyScreenPosition()
        {
            if (FargoClientConfig.Instance.MultiplayerDeathSpectate && Main.LocalPlayer.dead && Main.netMode != NetmodeID.SinglePlayer &&  Main.player.Any(p => p != null && !p.dead && !p.ghost))
            {
                Main.screenPosition = Player.Center - (new Vector2(Main.screenWidth, Main.screenHeight) / 2);
            }
        }
        public void AutoUseMirror()
        {
            int potionofReturn = -1;
            int recallPotion = -1;
            int magicMirror = -1;

            for (int i = 0; i < Player.inventory.Length; i++)
            {
                switch (Player.inventory[i].type)
                {
                    case ItemID.PotionOfReturn:
                        potionofReturn = i;
                        break;

                    case ItemID.RecallPotion:
                        recallPotion = i;
                        break;

                    case ItemID.MagicMirror:
                    case ItemID.IceMirror:
                    case ItemID.CellPhone:
                    case ItemID.Shellphone:
                        magicMirror = i;
                        break;
                }
            }

            if (potionofReturn != -1)
                QuickUseItemAt(potionofReturn);
            else if (recallPotion != -1)
                QuickUseItemAt(recallPotion);
            else if (magicMirror != -1)
                QuickUseItemAt(magicMirror);
        }

        public void QuickUseItemAt(int index, bool use = true)
        {
            if (!autoRevertSelectedItem && Player.selectedItem != index && Player.inventory[index].type != ItemID.None)
            {
                originalSelectedItem = Player.selectedItem;
                autoRevertSelectedItem = true;
                Player.selectedItem = index;
                Player.controlUseItem = true;
                if (use && CombinedHooks.CanUseItem(Player, Player.inventory[Player.selectedItem]))
                {
                    if (Player.whoAmI == Main.myPlayer)
                        Player.ItemCheck();
                }
            }
        }
    }   
}
