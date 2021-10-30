using System.Collections.Generic;
using System.IO;
using System.Linq;
using FontExtension;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MonoMSDF.Text
{
    public sealed class TextRenderer
    {
        private const string LargeTextTechnique = "LargeText";
        private const string SmallTextTechnique = "SmallText";

        private readonly Effect Effect;
        private readonly FieldFont Font;
        private readonly GraphicsDevice Device;
        private readonly Quad Quad;       
        private readonly Dictionary<char, GlyphRenderInfo> Cache;        
      
        public TextRenderer(Effect effect, FieldFont font, GraphicsDevice device)
        {
            this.Effect = effect;
            this.Font = font;
            this.Device = device;

            this.Quad = new Quad();
            this.Cache = new Dictionary<char, GlyphRenderInfo>();

            this.ForegroundColor = Color.White;
            this.EnableKerning = true;
            this.OptimizeForTinyText = false;
            this.PositiveYIsDown = false;
            this.PositionByTop = false;
        }

        public Color ForegroundColor { get; set; }
        public bool EnableKerning { get; set; }

        /// <summary>
        /// Disables text anti-aliasing which might cause blurry text when the text is rendered tiny
        /// </summary>
        public bool OptimizeForTinyText { get; set; }
        /// <summary>
        /// Flip the Y axis, so that positive Y is down. It is up to you to provide a wvp matrix where that makes sense.
        /// </summary>
        public bool PositiveYIsDown { get; set; }
        /// <summary>
        /// Adjust the text's position such that the given position is the top, rather than the baseline.
        /// </summary>
        public bool PositionByTop { get; set; }

        public void Render(string text, Matrix worldViewProjection, Vector2? position = null, float scale = 1)
        {
            if (string.IsNullOrEmpty(text))
                return;

            var sequence = text.Select(GetRenderInfo).ToArray();
            var textureWidth = sequence[0].Texture.Width;
            var textureHeight = sequence[0].Texture.Height;

            this.Effect.Parameters["WorldViewProjection"].SetValue(worldViewProjection);
            this.Effect.Parameters["PxRange"].SetValue(this.Font.PxRange);
            this.Effect.Parameters["TextureSize"].SetValue(new Vector2(textureWidth, textureHeight));
            this.Effect.Parameters["ForegroundColor"].SetValue(ForegroundColor.ToVector4());

            if (this.OptimizeForTinyText)
            {
                this.Effect.CurrentTechnique = this.Effect.Techniques[SmallTextTechnique];
            }
            else
            {
                this.Effect.CurrentTechnique = this.Effect.Techniques[LargeTextTechnique];
            }

            int yFlip = PositiveYIsDown ? -1 : 1;
            float lineHeight = 32 * scale;
            Vector2 penStart = position == null ? Vector2.Zero : (Vector2)position;
			if (PositionByTop)
			{
				penStart.Y -= lineHeight * yFlip;
			}
            Vector2 pen = penStart;
            for (var i = 0; i < sequence.Length; i++)
            {
                var current = sequence[i];

                this.Effect.Parameters["GlyphTexture"].SetValue(current.Texture);
                this.Effect.CurrentTechnique.Passes[0].Apply();

                var glyphHeight = textureHeight * (1.0f / current.Metrics.Scale) * scale;
                var glyphWidth = textureWidth * (1.0f / current.Metrics.Scale) * scale;

                var left = pen.X - current.Metrics.Translation.X * scale;
                var bottom = pen.Y - current.Metrics.Translation.Y * yFlip * scale;

                var right = left + glyphWidth;
                var top = bottom + glyphHeight * yFlip;

                if (!char.IsWhiteSpace(current.Character))
                {
                    this.Quad.Render(this.Device, new Vector2(left, bottom), new Vector2(right, top));
                }

                pen.X += current.Metrics.Advance * scale;

                if (this.EnableKerning && i < sequence.Length - 1)
                {
                    var next = sequence[i + 1];

                    var pair = this.Font.KerningPairs.FirstOrDefault(
                        x => x.Left == current.Character && x.Right == next.Character);

                    if (pair != null)
                    {

                        pen.X += pair.Advance * scale;
                    }

                }
				if (current.Character == '\n')
				{
                    pen.X = penStart.X;
                    pen.Y += lineHeight;
				}
            }            
        }

        private GlyphRenderInfo GetRenderInfo(char c)
        {
            if(this.Cache.TryGetValue(c, out var value))
            {
                return value;
            }

            var unit = LoadRenderInfo(c);
            this.Cache.Add(c, unit);
            return unit;
        }

        private GlyphRenderInfo LoadRenderInfo(char c)
        {
            var glyph = this.Font.GetGlyph(c);
            using (var stream = new MemoryStream(glyph.Bitmap))
            {
                var texture = Texture2D.FromStream(this.Device, stream);
                var unit = new GlyphRenderInfo(c, texture, glyph.Metrics);
                               

                return unit;
            }
        }
    }
}
