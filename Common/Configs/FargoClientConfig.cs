﻿using System.ComponentModel;
using System.Runtime.Serialization;
using Terraria;
using Terraria.ModLoader.Config;

namespace Fargowiltas.Common.Configs
{
    public sealed class FargoClientConfig : ModConfig
    {
        public override ConfigScope Mode => ConfigScope.ClientSide;

        [DefaultValue(true)]
        public bool ExpandedTooltips;

        [DefaultValue(false)]
        public bool DoubleTapDashDisabled;

        [DefaultValue(false)]
        public bool DoubleTapSetBonusDisabled;

        [DefaultValue(1f)]
        [Slider]
        public float TransparentFriendlyProjectiles;

        [DefaultValue(1f)]
        [Slider]
        public float DebuffOpacity;

        [DefaultValue(0.75f)]
        [Slider]
        public float DebuffFaderRatio;
        

        [OnDeserialized]
        internal void OnDeserializedMethod(StreamingContext context)
        {
            TransparentFriendlyProjectiles = Utils.Clamp(TransparentFriendlyProjectiles, 0f, 1f);
        }
    }
}
