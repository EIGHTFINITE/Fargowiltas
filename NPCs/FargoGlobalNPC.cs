using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.GameContent.Events;
using Terraria.GameContent.ItemDropRules;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Fargowiltas.Common.Configs;

namespace Fargowiltas.NPCs
{
    public class FargoGlobalNPC : GlobalNPC
    {
        internal static int[] Bosses = [ 
            NPCID.KingSlime,
            NPCID.EyeofCthulhu,
            NPCID.BrainofCthulhu,
            NPCID.QueenBee,
            NPCID.SkeletronHead,
            NPCID.QueenSlimeBoss,
            NPCID.TheDestroyer,
            NPCID.SkeletronPrime,
            NPCID.Retinazer,
            NPCID.Spazmatism,
            NPCID.Plantera,
            NPCID.Golem,
            NPCID.DukeFishron,
            NPCID.HallowBoss,
            NPCID.CultistBoss,
            NPCID.MoonLordCore,
            NPCID.MartianSaucerCore,
            NPCID.Pumpking,
            NPCID.IceQueen,
            NPCID.DD2Betsy,
            NPCID.DD2OgreT3,
            NPCID.IceGolem,
            NPCID.SandElemental,
            NPCID.Paladin,
            NPCID.Everscream,
            NPCID.MourningWood,
            NPCID.SantaNK1,
            NPCID.HeadlessHorseman,
            NPCID.PirateShip 
        ];

        public static int LastWoFIndex = -1;
        public static int WoFDirection = 0;

        internal bool PillarSpawn = true;
        internal bool SwarmActive;
        internal bool NoLoot = false;

        public static int eaterBoss = -1;
        public static int brainBoss = -1;
        public static int plantBoss = -1;
        public static int beeBoss = -1;

        public bool FirstFrame = true;

        public override bool InstancePerEntity => true;

        public override bool CanHitNPC(NPC npc, NPC target)/* tModPorter Suggestion: Return true instead of null */
        {
            if (target.friendly && FargoServerConfig.Instance.SaferBoundNPCs && (target.type == NPCID.BoundGoblin || target.type == NPCID.BoundMechanic || target.type == NPCID.BoundWizard || target.type == NPCID.BartenderUnconscious || target.type == NPCID.GolferRescue))
                return false;
            return base.CanHitNPC(npc, target);
        }
        public override bool PreAI(NPC npc)
        {
            if (FirstFrame)
            {
                FirstFrame = false;
                #region Stat Sliders
                FargoServerConfig config = FargoServerConfig.Instance;
                if ((config.EnemyHealth != 1 || config.BossHealth != 1) && !npc.townNPC && !npc.CountsAsACritter && npc.life > 10)
                {
                    float lifeFraction = npc.GetLifePercent();
                    bool boss = config.BossHealth > config.EnemyHealth && // only relevant if boss health is higher than enemy health
                        (npc.boss || npc.type == NPCID.EaterofWorldsHead || npc.type == NPCID.EaterofWorldsBody || npc.type == NPCID.EaterofWorldsTail || (config.BossApplyToAllWhenAlive && AnyBossAlive()));
                    if (boss)
                        npc.lifeMax = (int)Math.Round(npc.lifeMax * config.BossHealth);
                    else
                        npc.lifeMax = (int)Math.Round(npc.lifeMax * config.EnemyHealth);
                    npc.life = (int)Math.Round(npc.lifeMax * lifeFraction);
                }
                #endregion
            }
            if (npc.boss)
            {
                boss = npc.whoAmI;
            }

            if (npc.townNPC && npc.homeTileX == -1 && npc.homeTileY == -1)
            {
                bool hasRoom = WorldGen.TownManager.HasRoom(npc.type, out Point homePoint);
                if (hasRoom && homePoint.X > 0 && homePoint.Y > 0)
                {
                    int x = homePoint.X;
                    int y = homePoint.Y - 2;
                    WorldGen.moveRoom(x, y, npc.whoAmI);
                }
            }

            switch (npc.type)
            {
                case NPCID.EaterofWorldsHead:
                    eaterBoss = npc.whoAmI;
                    break;

                case NPCID.BrainofCthulhu:
                    brainBoss = npc.whoAmI;
                    break;

                case NPCID.Plantera:
                    plantBoss = npc.whoAmI;
                    break;

                case NPCID.QueenBee:
                    beeBoss = npc.whoAmI;
                    break;

                case NPCID.CultistBoss:
                    if (npc.ai[0] == -1 && npc.ai[1] == 1) //just after spawning
                    {
                        bool foundTabletNearby = Main.npc.Any(n => n.active && n.type == NPCID.CultistTablet && npc.Distance(n.Center) < 400);
                        if (!foundTabletNearby)
                        {
                            npc.ai[1] = 360;
                            npc.netUpdate = true;
                        }
                    }
                    break;

                case NPCID.MoonLordCore:
                    if (npc.ai[0] == 2)
                    {
                        int skipPoint = 600 - 60;
                        if (npc.ai[1] < skipPoint && npc.ai[1] % 60 == 30 && NPC.CountNPCS(npc.type) > 1)
                        {
                            npc.ai[1] = skipPoint;
                            npc.netUpdate = true;
                        }
                    }
                    break;

                default:
                    break;
            }

            return true;
        }

        public override void AI(NPC npc)
        {
            // Wack ghost saucers begone
            if (npc.type == NPCID.MartianSaucerCore && npc.dontTakeDamage)
            {
                npc.dontTakeDamage = false;
            }
        }

        public override void EditSpawnRate(Player player, ref int spawnRate, ref int maxSpawns)
        {
            FargoPlayer fargoPlayer = player.GetFargoPlayer();

            if (AnyBossAlive() && FargoServerConfig.Instance.BossZen && player.Distance(Main.npc[boss].Center) < 6000)
            {
                maxSpawns = 0;
            }
        }

        public override bool PreKill(NPC npc)
        {
            if (NoLoot)
            {
                return false;
            }

            if (Fargowiltas.SwarmActive && (npc.type == NPCID.BlueSlime || npc.type == NPCID.EaterofWorldsBody || npc.type == NPCID.EaterofWorldsTail || npc.type == NPCID.Creeper || (npc.type >= NPCID.PirateCorsair && npc.type <= NPCID.PirateCrossbower)))
            {
                return false;
            }

            if (SwarmActive && Fargowiltas.SwarmActive && Main.netMode != NetmodeID.MultiplayerClient)
            {
                switch (npc.type)
                {
                    case NPCID.KingSlime:
                        Swarm(npc, NPCID.KingSlime, NPCID.BlueSlime, ItemID.KingSlimeBossBag, ItemID.KingSlimeTrophy,  -1);
                        break;

                    case NPCID.EyeofCthulhu:
                        Swarm(npc, NPCID.EyeofCthulhu, NPCID.ServantofCthulhu, ItemID.EyeOfCthulhuBossBag, ItemID.EyeofCthulhuTrophy, -1);
                        break;

                    case NPCID.EaterofWorldsHead:
                        Swarm(npc, NPCID.EaterofWorldsHead, NPCID.EaterofWorldsTail, ItemID.EaterOfWorldsBossBag, ItemID.EaterofWorldsTrophy, -1);
                        break;

                    case NPCID.BrainofCthulhu:
                        Swarm(npc, NPCID.BrainofCthulhu, NPCID.Creeper, ItemID.BrainOfCthulhuBossBag, ItemID.BrainofCthulhuTrophy, -1);
                        break;

                    case NPCID.DD2DarkMageT1:
                        Swarm(npc, NPCID.DD2DarkMageT1, -1, ItemID.DefenderMedal, ItemID.BossTrophyDarkmage, -1);
                        break;

                    case NPCID.Deerclops:
                        Swarm(npc, NPCID.Deerclops, -1, ItemID.DeerclopsBossBag, ItemID.DeerclopsTrophy, -1);
                        break;

                    case NPCID.QueenBee:
                        Swarm(npc, NPCID.QueenBee, NPCID.BeeSmall, ItemID.QueenBeeBossBag, ItemID.QueenBeeTrophy, -1);
                        break;

                    case NPCID.SkeletronHead:
                        Swarm(npc, NPCID.SkeletronHead, -1, ItemID.SkeletronBossBag, ItemID.SkeletronTrophy, -1);
                        break;

                    case NPCID.WallofFlesh:
                        Swarm(npc, NPCID.WallofFlesh, NPCID.TheHungry, ItemID.WallOfFleshBossBag, ItemID.WallofFleshTrophy, -1);
                        break;

                    case NPCID.QueenSlimeBoss:
                        Swarm(npc, NPCID.QueenSlimeBoss, NPCID.QueenSlimeMinionPink, ItemID.QueenSlimeBossBag, ItemID.QueenSlimeTrophy, -1);
                        break;

                    case NPCID.TheDestroyer:
                        Swarm(npc, NPCID.TheDestroyer, NPCID.Probe, ItemID.DestroyerBossBag, ItemID.DestroyerTrophy, -1);
                        break;

                    case NPCID.Retinazer:
                        Swarm(npc, NPCID.Retinazer, -1, ItemID.TwinsBossBag, ItemID.RetinazerTrophy, -1);
                        break;

                    case NPCID.Spazmatism:
                        Swarm(npc, NPCID.Spazmatism, -1, -1, ItemID.SpazmatismTrophy, -1);
                        break;

                    case NPCID.SkeletronPrime:
                        Swarm(npc, NPCID.SkeletronPrime, -1, ItemID.SkeletronPrimeBossBag, ItemID.SkeletronPrimeTrophy, -1);
                        break;

                    case NPCID.Plantera:
                        Swarm(npc, NPCID.Plantera, NPCID.PlanterasHook, ItemID.PlanteraBossBag, ItemID.PlanteraTrophy, -1);
                        break;

                    case NPCID.Golem:
                        Swarm(npc, NPCID.Golem, NPCID.GolemHeadFree, ItemID.GolemBossBag, ItemID.GolemTrophy, -1);
                        break;

                    case NPCID.DD2Betsy:
                        Swarm(npc, NPCID.DD2Betsy, NPCID.DD2WyvernT3, ItemID.BossBagBetsy, ItemID.BossTrophyBetsy, -1);
                        break;

                    case NPCID.DukeFishron:
                        Swarm(npc, NPCID.DukeFishron, NPCID.Sharkron, ItemID.FishronBossBag, ItemID.DukeFishronTrophy, -1);
                        break;

                    case NPCID.HallowBoss:
                        Swarm(npc, NPCID.HallowBoss, -1, ItemID.FairyQueenBossBag, ItemID.FairyQueenTrophy, -1);
                        break;

                    case NPCID.CultistBoss:
                        Swarm(npc, NPCID.CultistBoss, -1, ItemID.CultistBossBag, ItemID.AncientCultistTrophy, -1);
                        return false; // no pillar spawn

                    case NPCID.MoonLordCore:
                        Swarm(npc, NPCID.MoonLordCore, NPCID.MoonLordFreeEye, ItemID.MoonLordBossBag, ItemID.MoonLordTrophy, -1);
                        break;

                    case NPCID.DungeonGuardian:
                        Swarm(npc, NPCID.DungeonGuardian, -1, -1, ItemID.BoneKey, -1);
                        break;
                }
            }

            return true;
        }

        public override void OnKill(NPC npc)
        {
            switch (npc.type)
            {
                case NPCID.DD2OgreT2:
                case NPCID.DD2OgreT3:
                    if (!DD2Event.Ongoing)
                    {
                        if (Main.rand.NextBool(14))
                            Item.NewItem(npc.GetSource_Loot(), npc.Hitbox, ItemID.BossMaskOgre);

                        Item.NewItem(npc.GetSource_Loot(), npc.Hitbox, Main.rand.Next(new int[] { ItemID.ApprenticeScarf, ItemID.SquireShield, ItemID.HuntressBuckler, ItemID.MonkBelt, ItemID.DD2SquireDemonSword, ItemID.MonkStaffT1, ItemID.MonkStaffT2, ItemID.BookStaff, ItemID.DD2PhoenixBow, ItemID.DD2PetGhost }));

                        Item.NewItem(npc.GetSource_Loot(), npc.Hitbox, ItemID.GoldCoin, Main.rand.Next(4, 7));
                    }
                    break;

                case NPCID.DD2DarkMageT1:
                case NPCID.DD2DarkMageT3:
                    if (!DD2Event.Ongoing)
                    {
                        if (Main.rand.NextBool(14))
                            Item.NewItem(npc.GetSource_Loot(), npc.Hitbox, ItemID.BossMaskDarkMage);

                        if (Main.rand.NextBool(10))
                            Item.NewItem(npc.GetSource_Loot(), npc.Hitbox, Main.rand.NextBool() ? ItemID.WarTable : ItemID.WarTableBanner);

                        if (Main.rand.NextBool(6))
                            Item.NewItem(npc.GetSource_Loot(), npc.Hitbox, Main.rand.Next(new int[] { ItemID.DD2PetGato, ItemID.DD2PetDragon }));
                    }
                    break;

                case NPCID.HeadlessHorseman:
                    if (FargoUtils.ActuallyNight && !Main.pumpkinMoon)
                    {
                        if (Main.rand.NextBool(10))
                            Item.NewItem(npc.GetSource_Loot(), npc.Hitbox, ItemID.JackOLanternMask);
                    }
                    break;

                case NPCID.MourningWood:
                    if (FargoUtils.ActuallyNight && !Main.pumpkinMoon)
                    {
                        Item.NewItem(npc.GetSource_Loot(), npc.Hitbox, ItemID.SpookyWood, 30);

                        Item.NewItem(npc.GetSource_Loot(), npc.Hitbox, Main.rand.Next(new int[] {
                            ItemID.SpookyHook,
                            ItemID.SpookyTwig,
                            ItemID.StakeLauncher,
                            ItemID.CursedSapling,
                            ItemID.NecromanticScroll,
                            Main.expertMode ? ItemID.WitchBroom : ItemID.SpookyWood
                        }));
                    }
                    break;

                case NPCID.Pumpking:
                    if (FargoUtils.ActuallyNight && !Main.pumpkinMoon)
                    {
                        Item.NewItem(npc.GetSource_Loot(), npc.Hitbox, Main.rand.Next(new int[] {
                            ItemID.TheHorsemansBlade,
                            ItemID.BatScepter,
                            ItemID.BlackFairyDust,
                            ItemID.SpiderEgg,
                            ItemID.RavenStaff,
                            ItemID.CandyCornRifle,
                            ItemID.JackOLanternLauncher,
                            ItemID.ScytheWhip
                        }));
                    }
                    break;

                case NPCID.Everscream:
                    if (FargoUtils.ActuallyNight && !Main.snowMoon)
                    {
                        Item.NewItem(npc.GetSource_Loot(), npc.Hitbox, Main.rand.Next(new int[] {
                            ItemID.ChristmasTreeSword,
                            ItemID.ChristmasHook,
                            ItemID.Razorpine,
                            ItemID.FestiveWings
                        }));
                    }
                    break;

                case NPCID.SantaNK1:
                    if (FargoUtils.ActuallyNight && !Main.snowMoon)
                    {
                        Item.NewItem(npc.GetSource_Loot(), npc.Hitbox, Main.rand.Next(new int[] {
                            ItemID.ElfMelter,
                            ItemID.ChainGun
                        }));
                    }
                    break;

                case NPCID.IceQueen:
                    if (FargoUtils.ActuallyNight && !Main.snowMoon)
                    {
                        Item.NewItem(npc.GetSource_Loot(), npc.Hitbox, Main.rand.Next(new int[] {
                            ItemID.BlizzardStaff,
                            ItemID.SnowmanCannon,
                            ItemID.NorthPole,
                            ItemID.BabyGrinchMischiefWhistle,
                            ItemID.ReindeerBells
                        }));
                    }
                    break;

                default:
                    break;
            }
        }

        public override void ModifyNPCLoot(NPC npc, NPCLoot npcLoot)
        {
            switch (npc.type)
            {
                case NPCID.ZombieEskimo:
                case NPCID.ArmedZombieEskimo:
                case NPCID.Penguin:
                case NPCID.IceSlime:
                case NPCID.SpikedIceSlime:
                    npcLoot.Add(ItemDropRule.OneFromOptions(20, ItemID.EskimoHood, ItemID.EskimoCoat, ItemID.EskimoPants));
                    break;

                case NPCID.GreekSkeleton:
                    npcLoot.RemoveWhere(rule => rule is CommonDrop drop && (drop.itemId == ItemID.GladiatorHelmet || drop.itemId == ItemID.GladiatorBreastplate || drop.itemId == ItemID.GladiatorLeggings));
                    npcLoot.Add(ItemDropRule.OneFromOptions(10, ItemID.GladiatorHelmet, ItemID.GladiatorBreastplate, ItemID.GladiatorLeggings));
                    break;

                case NPCID.Merchant:
                    npcLoot.Add(ItemDropRule.Common(ItemID.MiningShirt, 8));
                    npcLoot.Add(ItemDropRule.Common(ItemID.MiningPants, 8));
                    break;

                case NPCID.Nurse:
                    npcLoot.Add(ItemDropRule.Common(ItemID.LifeCrystal, 5));
                    break;

                case NPCID.Demolitionist:
                    npcLoot.Add(ItemDropRule.Common(ItemID.Dynamite, 2, 5, 5));
                    break;

                case NPCID.Dryad:
                    npcLoot.Add(ItemDropRule.Common(ItemID.HerbBag, 3));
                    break;

                case NPCID.DD2Bartender:
                    npcLoot.Add(ItemDropRule.Common(ItemID.Ale, 2, 4, 4));
                    break;

                case NPCID.Cyborg:
                    npcLoot.Add(ItemDropRule.Common(ItemID.NanoBullet, 4, 30, 30));
                    break;

                case NPCID.Clothier:
                    npcLoot.Add(ItemDropRule.Common(ItemID.Skull, 20));
                    break;

                case NPCID.Mechanic:
                    npcLoot.Add(ItemDropRule.Common(ItemID.Wire, 5, 40, 40));
                    break;

                case NPCID.Wizard:
                    npcLoot.Add(ItemDropRule.Common(ItemID.FallenStar, 5, 5, 5));
                    break;

                case NPCID.TaxCollector:
                    npcLoot.Add(ItemDropRule.Common(ItemID.GoldCoin, 8, 10, 10));
                    break;

                case NPCID.Truffle:
                    npcLoot.Add(ItemDropRule.Common(ItemID.MushroomStatue, 8));
                    break;

                case NPCID.Angler:
                    npcLoot.Add(ItemDropRule.OneFromOptions(2, ItemID.OldShoe, ItemID.TinCan, ItemID.FishingSeaweed));
                    break;


                case NPCID.DD2OgreT2:
                case NPCID.DD2OgreT3:
                    npcLoot.Add(ItemDropRule.Common(ItemID.DefenderMedal, 1, 20, 20));
                    break;

                case NPCID.DD2DarkMageT1:
                case NPCID.DD2DarkMageT3:
                    npcLoot.Add(ItemDropRule.Common(ItemID.DefenderMedal, 1, 5, 5));
                    break;

                case NPCID.Raven:
                    npcLoot.Add(ItemDropRule.Common(ItemID.GoodieBag));
                    break;

                case NPCID.SlimeRibbonRed:
                case NPCID.SlimeRibbonGreen:
                case NPCID.SlimeRibbonWhite:
                case NPCID.SlimeRibbonYellow:
                    npcLoot.Add(ItemDropRule.Common(ItemID.Present));
                    break;

                case NPCID.BloodZombie:
                    npcLoot.Add(ItemDropRule.OneFromOptions(200, ItemID.BladedGlove, ItemID.BloodyMachete));
                    break;

                case NPCID.Clown:
                    npcLoot.Add(ItemDropRule.Common(ItemID.Bananarang));
                    break;

                case NPCID.MoonLordCore:
                    npcLoot.Add(ItemDropRule.Common(ItemID.MoonLordLegs, 100));
                    break;
            }

            base.ModifyNPCLoot(npc, npcLoot);
        }


        public override bool CheckDead(NPC npc)
        {
            switch (npc.type)
            {
                // Avoid lunar event with cultist summon
                case NPCID.CultistBoss:
                    if (!PillarSpawn)
                    {
                        for (int i = 0; i < Main.maxNPCs; i++)
                        {
                            NPC npc2 = Main.npc[i];
                            NPC.LunarApocalypseIsUp = false;

                            if (npc2.type == NPCID.LunarTowerNebula || npc2.type == NPCID.LunarTowerSolar || npc2.type == NPCID.LunarTowerStardust || npc2.type == NPCID.LunarTowerVortex)
                            {
                                NPC.TowerActiveSolar = true;
                                npc2.active = false;
                            }

                            NPC.TowerActiveSolar = false;
                        }
                    }
                    break;
                default:
                    break;
            }

            if (npc.type == NPCID.DD2Betsy)
            {
                FargoUtils.PrintText(Language.GetTextValue("Announcement.HasBeenDefeated_Single", Lang.GetNPCNameValue(NPCID.DD2Betsy)), new Color(175, 75, 0));
            }

            return true;
        }

        public override void ModifyHitByProjectile(NPC npc, Projectile projectile, ref NPC.HitModifiers modifiers)
        {
            if (FargoServerConfig.Instance.RottenEggs && projectile.type == ProjectileID.RottenEgg && npc.townNPC)
            {
                modifiers.FinalDamage *= 20;
                //damage *= 20;
            }
        }

        public override void OnChatButtonClicked(NPC npc, bool firstButton)
        {
            // No angler check enables luiafk compatibility
            if (FargoServerConfig.Instance.AnglerQuestInstantReset && Main.anglerQuestFinished)
            {
                if (Main.netMode == NetmodeID.SinglePlayer)
                {
                    Main.AnglerQuestSwap();
                }
                else if (Main.netMode == NetmodeID.MultiplayerClient)
                {
                    // Broadcast swap request to server
                    var netMessage = Mod.GetPacket();
                    netMessage.Write((byte)3);
                    netMessage.Send();
                }
            }
        }

        private void Swarm(NPC npc, int boss, int minion, int bossbag, int trophy, int reward)
        {
            if (bossbag >= 0 && bossbag != ItemID.DefenderMedal)
            {
                int stack = (Fargowiltas.SwarmItemsUsed * 5) - 1;
                if (npc.type == NPCID.CultistBoss)
                    stack += 1;
                npc.DropItemInstanced(npc.Center, npc.Size, bossbag, itemStack: stack);
            }
            else if (bossbag >= 0 && bossbag == ItemID.DefenderMedal)
            {
                npc.DropItemInstanced(npc.Center, npc.Size, bossbag, itemStack: 5 * ((Fargowiltas.SwarmItemsUsed * 5) - 1));
            }

            // Drop swarm reward for every 10 items used
            if (Fargowiltas.SwarmItemsUsed >= 10 && reward > 0)
                Item.NewItem(npc.GetSource_Loot(), npc.Hitbox, reward, Stack: Fargowiltas.SwarmItemsUsed / 10);
                    

            //drop trophy for every 3 items
            if (Fargowiltas.SwarmItemsUsed >= 3 && trophy > 0)
                Item.NewItem(npc.GetSource_Loot(), npc.Hitbox, trophy, Stack: Fargowiltas.SwarmItemsUsed / 3);

            if (minion != -1)
            {
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    if (Main.npc[i].active && Main.npc[i].type == minion)
                    {
                        Main.npc[i].SimpleStrikeNPC(Main.npc[i].lifeMax, -Main.npc[i].direction, true, 0, null, false, 0, true);
                        //Main.npc[i].StrikeNPCNoInteraction(Main.npc[i].lifeMax, 0f, -Main.npc[i].direction, true);
                    }
                }
            }
        }

        public static void SpawnWalls(Player player)
        {
            int startingPos;

            if (LastWoFIndex == -1)
            {
                startingPos = (int)player.position.X;
            }
            else
            {
                startingPos = (int)Main.npc[LastWoFIndex].position.X;
            }

            Vector2 pos = player.position;

            if (WoFDirection == 0)
            {
                //1 is to the right, -1 is left
                WoFDirection = ((player.position.X / 16) > (Main.maxTilesX / 2)) ? 1 : -1;
            }

            int wof = NPC.NewNPC(NPC.GetBossSpawnSource(Main.myPlayer), startingPos + (400 * WoFDirection), (int)pos.Y, NPCID.WallofFlesh, 0);
            Main.npc[wof].GetGlobalNPC<FargoGlobalNPC>().SwarmActive = true;

            LastWoFIndex = wof;
        }

        public static bool SpecificBossIsAlive(ref int bossID, int bossType)
        {
            if (bossID != -1)
            {
                if (Main.npc[bossID].active && Main.npc[bossID].type == bossType)
                {
                    return true;
                }
                else
                {
                    bossID = -1;
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        public static int boss = -1;

        public static bool AnyBossAlive()
        {
            if (boss == -1)
                return false;

            NPC npc = Main.npc[boss];

            if (npc.active && npc.type != NPCID.MartianSaucerCore && (npc.boss || npc.type == NPCID.EaterofWorldsHead))
                return true;
            boss = -1;
            return false;
        }
    }
}