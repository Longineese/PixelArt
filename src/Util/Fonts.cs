using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using SpriteFontPlus;

namespace PixelArt {
    public static class Fonts {
        public static SpriteFont Arial;

        public static Dictionary<string, Dictionary<int, SpriteFont>> fontDict =
            new Dictionary<string, Dictionary<int, SpriteFont>>();
        
        public static void loadFonts(ContentManager Content) {
            Arial = Content.Load<SpriteFont>("Arial");
        }

        public static SpriteFont getFontSafe(string fontName, int size) {
            size = Math.Max(size, 1);
            if (fontDict.ContainsKey(fontName) && fontDict[fontName].ContainsKey(size)) {
                return fontDict[fontName][size];
            }
            addFont(fontName, size);

            return fontDict[fontName][size];
        }
        
        public static SpriteFont getFontRaw(string fontName, int size) {
            return fontDict[fontName][size];
        }

        public static void addFont(string fontName, int size) {

            if (fontDict.ContainsKey(fontName) && fontDict[fontName].ContainsKey(size)) return;
            
            var fontBakeResult = TtfFontBaker.Bake(File.ReadAllBytes(Paths.fontPath+fontName+".ttf"),
                size,
                1024,
                1024,
                new[]
                {
                    CharacterRange.BasicLatin,
                    CharacterRange.Latin1Supplement,
                    CharacterRange.LatinExtendedA,
                }
            );

            SpriteFont font = fontBakeResult.CreateSpriteFont(Main.getGraphicsDevice());

            if (!fontDict.ContainsKey(fontName)) { 
                fontDict[fontName] = new Dictionary<int, SpriteFont>();
            }

            fontDict[fontName][size] = font;
        }
    }
}