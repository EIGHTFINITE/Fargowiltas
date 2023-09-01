using Terraria;
using Terraria.Enums;
using Terraria.ModLoader;

namespace Fargowiltas.Items.Tiles
{
    public class SiblingPylon : ModItem
    {
        public override void SetDefaults()
        {
            Item.DefaultToPlaceableTile(ModContent.TileType<SiblingPylonTile>());
            Item.SetShopValues(ItemRarityColor.Blue1, Item.buyPrice(gold: 10));
        }
    }
}