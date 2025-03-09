using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.ModLoader;
using Terraria.UI;

namespace Fargowiltas.Common.Systems
{
    public class InstaDrawPlayer : ModPlayer
    {
        public bool Draw = false;
        public Vector2 DrawPosition = Vector2.Zero;
        public Vector2 Scale = Vector2.Zero;

        public override void ResetEffects()
        {
            Draw = false;
            DrawPosition = Vector2.Zero;
            Scale = Vector2.Zero;
        }
        public override void UpdateDead()
        {
            ResetEffects();
        }
    }
    public class InstaVisual : ModSystem
    {
        public enum DrawOrigin
        {
            Center,
            TopLeft,
            Top,
            TopRight,
            Left,
            Right,
            BottomLeft,
            Bottom,
            BottomRight
        }
        public static void DrawInstaVisual(Player player, Vector2 drawPosition, Vector2 scale, DrawOrigin drawOrigin = DrawOrigin.Center)
        {
            InstaDrawPlayer drawPlayer = player.GetModPlayer<InstaDrawPlayer>();
            drawPlayer.Draw = true;
            drawPlayer.DrawPosition = drawPosition;
            Vector2 right = Vector2.UnitX * (scale.X * 8 - 16);
            Vector2 left = -Vector2.UnitX * scale.X * 8;
            Vector2 y = Vector2.UnitY * scale.Y * 8;
            drawPlayer.DrawPosition -= drawOrigin switch
            {
                DrawOrigin.TopLeft => left - y,
                DrawOrigin.Top => -y,
                DrawOrigin.TopRight => right - y,
                DrawOrigin.Left => left,
                DrawOrigin.Right => right,
                DrawOrigin.BottomLeft => left + y,
                DrawOrigin.Bottom => y,
                DrawOrigin.BottomRight => right + y,
                _ => Vector2.Zero
            };
            drawPlayer.Scale = scale;
        }
        public override void ModifyInterfaceLayers(List<GameInterfaceLayer> layers)
        {
            int index = layers.FindIndex(layer => layer.Name.Equals("Vanilla: Interface Logic 1"));
            if (index != -1)
            {
                layers.Insert(index, new LegacyGameInterfaceLayer(
                    "Fargowiltas: Insta Item Visual",
                    delegate {
                        DrawVisual(Main.spriteBatch);
                        return true;
                    },
                    InterfaceScaleType.Game)
                );
            }
        }
        public static void DrawVisual(SpriteBatch spriteBatch)
        {
            InstaDrawPlayer drawPlayer = Main.LocalPlayer.GetModPlayer<InstaDrawPlayer>();
            if (drawPlayer.Draw)
            {
                Texture2D texture = ModContent.Request<Texture2D>("Fargowiltas/Assets/InstaVisualSquare").Value;
                Vector2 drawPos = drawPlayer.DrawPosition.ToTileCoordinates().ToWorldCoordinates() - Main.screenPosition - Vector2.One * 8;
                drawPos -= drawPlayer.Scale * 8f;
                if (drawPlayer.Scale.X % 2 != 0)
                    drawPos.X += 8f;
                if (drawPlayer.Scale.Y % 2 != 0)
                    drawPos.Y += 8f;
                spriteBatch.Draw(texture, drawPos, null, Color.Black with { A = 100 }, 0f, Vector2.Zero, drawPlayer.Scale, SpriteEffects.None, 0f);
            }
        }
    }
}
