using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.Xna.Framework.Content;

namespace FontExtension
{
    public class FieldFont
    {
        [ContentSerializer] private readonly Dictionary<char, FieldGlyph> Glyphs;
        [ContentSerializer] private readonly string NameBackend;
        [ContentSerializer] private readonly float PxRangeBackend;
        [ContentSerializer] private readonly float LineHeightBackend;
        [ContentSerializer] private readonly Dictionary<(char, char), float> KerningBackend;
        [ContentSerializer] private readonly byte[] BitmapBackend;

        public FieldFont(string name, IEnumerable<FieldGlyph> glyphs, Dictionary<(char, char), float> kerning, float pxRange, float lineHeight, byte[] bitmap)
        {
            this.NameBackend = name;
            this.PxRangeBackend = pxRange;
            this.LineHeightBackend = lineHeight;
            this.KerningBackend = kerning;

            this.Glyphs = new Dictionary<char, FieldGlyph>(glyphs.Count());
            foreach (var glyph in glyphs)
            {
                this.Glyphs.Add(glyph.Character, glyph);
            }
            this.BitmapBackend = bitmap;
        }
        /// <summary>
        /// Parse a json file and combine it with bitmap bytes
        /// </summary>
        /// <param name="jsonFile">Filename for json from msdf-atlast-gen</param>
        /// <param name="bitmap">A byte array of bitmap data</param>
        /// <returns>FieldFont</returns>
        public static FieldFont FromJsonAndBitmapBytes(string jsonFile, byte[] bitmap)
		{
            string name = Path.GetFileNameWithoutExtension(jsonFile);
            var jdoc = JsonDocument.Parse(File.ReadAllText(jsonFile));
            float lineHeight = jdoc.RootElement.GetProperty("metrics").GetProperty("lineHeight").GetSingle();
            var jAtlas = jdoc.RootElement.GetProperty("atlas");
            float range = jAtlas.GetProperty("distanceRange").GetSingle();
            float width = jAtlas.GetProperty("width").GetSingle();
            float height = jAtlas.GetProperty("height").GetSingle();
            var jGlyphs = jdoc.RootElement.GetProperty("glyphs");
            List<FieldGlyph> glyphs = new List<FieldGlyph>(jGlyphs.GetArrayLength());
            foreach (var glyphElement in jGlyphs.EnumerateArray())
            {
                char c = (char)glyphElement.GetProperty("unicode").GetInt32();
                float adv = glyphElement.GetProperty("advance").GetSingle();
                float planeBot = 0;
                float planeTop = 0;
                float planeLeft = 0;
                float planeRight = 0;
                if (glyphElement.TryGetProperty("planeBounds", out JsonElement planeBounds))
                {
                    planeBot = planeBounds.GetProperty("bottom").GetSingle();
                    planeTop = planeBounds.GetProperty("top").GetSingle();
                    planeLeft = planeBounds.GetProperty("left").GetSingle();
                    planeRight = planeBounds.GetProperty("right").GetSingle();
                }
                float atlasBot = 0;
                float atlasTop = 0;
                float atlasLeft = 0;
                float atlasRight = 0;
                if (glyphElement.TryGetProperty("atlasBounds", out JsonElement atlasBounds))
                {
                    atlasBot = atlasBounds.GetProperty("bottom").GetSingle() / height;
                    atlasTop = atlasBounds.GetProperty("top").GetSingle() / height;
                    atlasLeft = atlasBounds.GetProperty("left").GetSingle() / width;
                    atlasRight = atlasBounds.GetProperty("right").GetSingle() / width;
                }
                glyphs.Add(new FieldGlyph(c, adv, planeLeft, planeRight, planeTop, planeBot, atlasLeft, atlasRight, atlasTop, atlasBot));
            }
            var jKerning = jdoc.RootElement.GetProperty("kerning");
            Dictionary<(char, char), float> kerning = new Dictionary<(char, char), float>(jKerning.GetArrayLength());
            foreach (var kernElement in jKerning.EnumerateArray())
            {
                char c1 = (char)kernElement.GetProperty("unicode1").GetInt32();
                char c2 = (char)kernElement.GetProperty("unicode2").GetInt32();
                float kernAdv = kernElement.GetProperty("advance").GetSingle();
                kerning.Add((c1, c2), kernAdv);
            }
            return new FieldFont(name, glyphs, kerning, range, lineHeight, bitmap);
        }

        /// <summary>
        /// Name of the font
        /// </summary>
        public string Name => this.NameBackend;
        
        /// <summary>
        /// Distance field effect range in pixels
        /// </summary>
        public float PxRange => this.PxRangeBackend;

        /// <summary>
        /// Kerning pairs available in this font
        /// </summary>
        public Dictionary<(char, char), float> Kerning => this.KerningBackend;

        /// <summary>
        /// Lineheight for this font
        /// </summary>
        public float LineHeight => this.LineHeightBackend;

        /// <summary>
        /// Distance field atlas for this font
        /// </summary>
        public byte[] Bitmap => this.BitmapBackend; 

        /// <summary>
        /// Characters supported by this font
        /// </summary>
        [ContentSerializerIgnore]
        public IEnumerable<char> SupportedCharacters => this.Glyphs.Keys;
       
        /// <summary>
        /// Returns the glyph for the given character, or throws an exception when the glyph is not supported by this font
        /// </summary>        
        public FieldGlyph GetGlyph(char c)
        {
            if (this.Glyphs.TryGetValue(c, out FieldGlyph glyph))
            {
                return glyph;
            }
            if (this.Glyphs.TryGetValue('?', out FieldGlyph backupGlyph))
            {
                return backupGlyph;
            }
            throw new InvalidOperationException($"Character '{c}' not found in font {this.Name}. Did you forget to include it in the character ranges?");
        }
    }
}
