using Fargowiltas.Common.Configs;
using Fargowiltas.NPCs;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace Fargowiltas.Tiles
{
    public class FargoGlobalPylon : GlobalPylon
    {
        public override bool? ValidTeleportCheck_PreAnyDanger(TeleportPylonInfo pylonInfo)
        {
            if (FargoServerConfig.Instance.PylonsIgnoreEvents && !FargoGlobalNPC.AnyBossAlive())
                return true;
            
            return base.ValidTeleportCheck_PreAnyDanger(pylonInfo);
        }
    }
}