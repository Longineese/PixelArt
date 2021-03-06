﻿using Microsoft.Xna.Framework;

namespace PixelArt {
    public static class Colors {

        public static Color background = new Color(64, 64, 64), panel = new Color(30, 30, 30),
            erased = new Color(1F,1F,1F,0F), canvasBack = new Color(192, 192, 192), 
            exportBack = Color.Lerp(background, panel, 0.5F)
            , exportMid = Color.Lerp(background, panel, 0.25F),
            error = new Color(228, 70, 70);

    }
}