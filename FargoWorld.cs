using Fargowiltas.NPCs;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.UI;
using Terraria.ModLoader.IO;
using Terraria.GameContent.Events;
using Fargowiltas.Common.Configs;

namespace Fargowiltas
{
    public class FargoWorld : ModSystem
    {
        internal static bool[] CurrentSpawnRateTile;

        // Do not change the order or name of any of these value names, it will fuck up loading. Any new additions should be added at the end.
        private readonly string[] tags =
        [
            "lumberjack",
            "betsy",
            "boss",
            "rareEnemy",
            "pinky",
            "undeadMiner",
            "tim",
            "doctorBones",
            "mimic",
            "wyvern",
            "runeWizard",
            "nymph",
            "moth",
            "rainbowSlime",
            "paladin",
            "medusa",
            "clown",
            "iceGolem",
            "sandElemental",
            "mothron",
            "mimicHallow",
            "mimicCorrupt",
            "mimicCrimson",
            "mimicJungle",
            "goblinSummoner",
            "flyingDutchman",
            "dungeonSlime",
            "pirateCaptain",
            "skeletonGun",
            "skeletonMage",
            "boneLee",
            "darkMage",
            "ogre",
            "headlessHorseman",
            "babyGuardian",
            "squirrel",
            "worm",
            "nailhead",
            "zombieMerman",
            "eyeFish",
            "bloodEel",
            "goblinShark",
            "dreadnautilus",
            "gnome",
            "redDevil",
            "goldenSlime",
            "goblinScout",
            "pumpking",
            "mourningWood",
            "iceQueen",
            "santank",
            "everscream"
       ];

        public override void PreWorldGen()
        {
            SetWorldBool(FargoServerConfig.Instance.DrunkWorld, ref Main.drunkWorld) ;
            SetWorldBool(FargoServerConfig.Instance.BeeWorld, ref Main.notTheBeesWorld);
            SetWorldBool(FargoServerConfig.Instance.WorthyWorld, ref Main.getGoodWorld);
            SetWorldBool(FargoServerConfig.Instance.CelebrationWorld, ref Main.tenthAnniversaryWorld);
            SetWorldBool(FargoServerConfig.Instance.ConstantWorld, ref Main.dontStarveWorld);
        }

        private void SetWorldBool(SeasonSelections toggle, ref bool flag)
        {
            switch (toggle)
            {
                case SeasonSelections.AlwaysOn:
                    flag = true;
                    break;
                case SeasonSelections.AlwaysOff:
                    flag = false;
                    break;
                case SeasonSelections.Normal:
                    break;
            }
        }

        private void ResetFlags()
        {
            CurrentSpawnRateTile = new bool[Main.netMode == NetmodeID.Server ? 255 : 1];
        }

        public override void OnWorldLoad()
        {
            ResetFlags();
        }

        public override void OnWorldUnload()
        {
            ResetFlags();
        }

        public override void NetReceive(BinaryReader reader)
        {
            Fargowiltas.SwarmActive = reader.ReadBoolean();
            Fargowiltas.HardmodeSwarmActive = reader.ReadBoolean();
            Fargowiltas.SwarmNoHyperActive = reader.ReadBoolean();
        }

        public override void NetSend(BinaryWriter writer)
        {
            writer.Write(Fargowiltas.SwarmActive);
            writer.Write(Fargowiltas.HardmodeSwarmActive);
            writer.Write(Fargowiltas.SwarmNoHyperActive);
        }

        public override void PostUpdateWorld()
        {
            // seasonals
            //SeasonSelections halloween = GetInstance<FargoConfig>().Halloween;
            //SeasonSelections xmas = GetInstance<FargoConfig>().Christmas;


            SetWorldBool(FargoServerConfig.Instance.Halloween, ref Main.halloween);
            SetWorldBool(FargoServerConfig.Instance.Christmas, ref Main.xMas);

            //seeds
            SetWorldBool(FargoServerConfig.Instance.DrunkWorld, ref Main.drunkWorld);
            SetWorldBool(FargoServerConfig.Instance.BeeWorld, ref Main.notTheBeesWorld);
            SetWorldBool(FargoServerConfig.Instance.WorthyWorld, ref Main.getGoodWorld);
            SetWorldBool(FargoServerConfig.Instance.CelebrationWorld, ref Main.tenthAnniversaryWorld);
            SetWorldBool(FargoServerConfig.Instance.ConstantWorld, ref Main.dontStarveWorld);

            // swarm reset in case something goes wrong
            if (Main.netMode != NetmodeID.MultiplayerClient && Fargowiltas.SwarmActive
                && NoBosses() && !NPC.AnyNPCs(NPCID.EaterofWorldsHead) && !NPC.AnyNPCs(NPCID.DungeonGuardian) && !NPC.AnyNPCs(NPCID.DD2DarkMageT1))
            {
                Fargowiltas.SwarmActive = false;
                Fargowiltas.HardmodeSwarmActive = false;
                Fargowiltas.SwarmNoHyperActive = false;
                FargoGlobalNPC.LastWoFIndex = -1;
                FargoGlobalNPC.WoFDirection = 0;
                if (Main.netMode == NetmodeID.Server)
                    NetMessage.SendData(MessageID.WorldData);
            }
        }

        public override void PreUpdateWorld()
        {
            bool rate = false;
            for (int i = 0; i < CurrentSpawnRateTile.Length; i++)
            {
                if (CurrentSpawnRateTile[i])
                {
                    Player player = Main.player[i];
                    if (player.active)
                    {
                        if (!player.dead)
                        {
                            rate = true;
                        }
                    }
                    else
                    {
                        CurrentSpawnRateTile[i] = false;
                    }
                }
            }

            if (rate)
            {
                Main.checkForSpawns += 81;
            }
        }

        private bool NoBosses() => Main.npc.All(i => !i.active || !i.boss);

        public override void UpdateUI(GameTime gameTime)
        {
            base.UpdateUI(gameTime);
            Fargowiltas.UserInterfaceManager.UpdateUI(gameTime);
        }

        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            base.ModifyInterfaceLayers(layers);
            Fargowiltas.UserInterfaceManager.ModifyInterfaceLayers(layers);
        }
    }
}