using Fargowiltas.Items.Tiles;
using System.Linq;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace Fargowiltas.Content.Buffs
{
    public class BigSuckBuff : ModBuff
    {
        public override void SetStaticDefaults()
        {
            Main.buffNoSave[Type] = true;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            player.GetFargoPlayer().bigSuck = true;
        }
    }
}