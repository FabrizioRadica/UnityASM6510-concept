// Author: Fabrizio Radica
// Version: 1.0
// Description: Unity OnGUI renderer for the Mini6510 text screen.
//              Rendering order: Background → Raster Effects → Copper Bars → Character Layer → Sprites → Border.

using UnityEngine;

namespace Mini6510
{
    public class Renderer6510
    {
        private const int CHAR_PIXEL_W = 8;
        private const int CHAR_PIXEL_H = 8;
        private const int BORDER_PX    = 32;

        private readonly Memory       _mem;
        private readonly VideoRAM     _video;
        private readonly ColorRAM     _colorRAM;
        private readonly CharacterSet _charset;
        private readonly RasterSystem _raster;
        private readonly SpriteSystem _sprites;

        // Character texture cache
        private readonly Texture2D[] _charTexCache = new Texture2D[256];
        private readonly byte[]      _charBitmapBuf = new byte[8];
        private bool _charCacheDirty = true;

        // Pixel textures
        private Texture2D _onPixel;
        private Texture2D _offPixel;

        // Screen dimensions (set at init, recalculated on resize)
        private int _screenW;
        private int _screenH;

        public Renderer6510(Memory mem, VideoRAM video, ColorRAM colorRAM,
                             CharacterSet charset, RasterSystem raster, SpriteSystem sprites)
        {
            _mem      = mem;
            _video    = video;
            _colorRAM = colorRAM;
            _charset  = charset;
            _raster   = raster;
            _sprites  = sprites;

            _onPixel  = MakeTexture(Color.white);
            _offPixel = MakeTexture(new Color(0, 0, 0, 0));

            // Rebuild char cache when charset RAM is written
            mem.OnBorderColorWrite += _ => { };
            mem.OnBgColorWrite     += _ => { };
        }

        public void MarkCharCacheDirty() => _charCacheDirty = true;

        public void Draw()
        {
            int w = Screen.width;
            int h = Screen.height;

            // ── Compute rects ─────────────────────────────────────────────────────
            float aspect     = 320f / 200f;
            float drawH      = h;
            float drawW      = drawH * aspect;
            if (drawW > w) { drawW = w; drawH = drawW / aspect; }

            float ox = (w - drawW) * 0.5f;
            float oy = (h - drawH) * 0.5f;

            float borderPxX = BORDER_PX * (drawW / 320f);
            float borderPxY = BORDER_PX * (drawH / 200f);

            Rect outerRect = new Rect(ox, oy, drawW, drawH);
            Rect innerRect = new Rect(ox + borderPxX, oy + borderPxY,
                                      drawW - borderPxX * 2f,
                                      drawH - borderPxY * 2f);

            // ── 1. Background ─────────────────────────────────────────────────────
            Color32 bgColor = _colorRAM.GetBackground();
            GUI.color = bgColor;
            GUI.DrawTexture(innerRect, Texture2D.whiteTexture);

            // ── 2. Raster Effects (Copper Bars) ───────────────────────────────────
            _raster.ScreenHeight  = innerRect.height;
            _raster.ScreenOffsetY = innerRect.y;
            _raster.Draw(innerRect);

            // ── 4. Character Layer ────────────────────────────────────────────────
            GUI.color = Color.white;
            DrawCharacterLayer(innerRect);

            // ── 5. Sprites ────────────────────────────────────────────────────────
            _sprites.Draw(innerRect);

            // ── 6. Border ─────────────────────────────────────────────────────────
            Color32 borderColor = _colorRAM.GetBorder();
            DrawBorder(outerRect, innerRect, borderColor);
        }

        private void DrawCharacterLayer(Rect innerRect)
        {
            if (_charCacheDirty)
            {
                RebuildCharCache();
                _charCacheDirty = false;
            }

            float cellW = innerRect.width  / VideoRAM.COLS;
            float cellH = innerRect.height / VideoRAM.ROWS;

            for (int row = 0; row < VideoRAM.ROWS; row++)
            {
                for (int col = 0; col < VideoRAM.COLS; col++)
                {
                    byte charCode = _video.GetChar(col, row);
                    Color32 fg    = _colorRAM.GetForeground(col, row);
                    Color32 bg    = _colorRAM.GetBackground();

                    float px = innerRect.x + col * cellW;
                    float py = innerRect.y + row * cellH;

                    DrawChar(charCode, fg, bg, new Rect(px, py, cellW, cellH));
                }
            }
        }

        private void DrawChar(byte charCode, Color32 fg, Color32 bg, Rect cell)
        {
            _charset.GetCharBitmap(charCode, _charBitmapBuf);

            float pw = cell.width  / CHAR_PIXEL_W;
            float ph = cell.height / CHAR_PIXEL_H;

            for (int row = 0; row < CHAR_PIXEL_H; row++)
            {
                byte b = _charBitmapBuf[row];
                for (int bit = 7; bit >= 0; bit--)
                {
                    bool on   = (b & (1 << bit)) != 0;
                    int  col  = 7 - bit;
                    float px  = cell.x + col * pw;
                    float py  = cell.y + row * ph;

                    GUI.color = on ? fg : bg;
                    GUI.DrawTexture(new Rect(px, py, pw, ph), Texture2D.whiteTexture);
                }
            }

            GUI.color = Color.white;
        }

        private void DrawBorder(Rect outer, Rect inner, Color32 color)
        {
            GUI.color = color;
            // Top
            GUI.DrawTexture(new Rect(outer.x, outer.y, outer.width, inner.y - outer.y), Texture2D.whiteTexture);
            // Bottom
            float botY = inner.y + inner.height;
            GUI.DrawTexture(new Rect(outer.x, botY, outer.width, outer.y + outer.height - botY), Texture2D.whiteTexture);
            // Left
            GUI.DrawTexture(new Rect(outer.x, inner.y, inner.x - outer.x, inner.height), Texture2D.whiteTexture);
            // Right
            float rightX = inner.x + inner.width;
            GUI.DrawTexture(new Rect(rightX, inner.y, outer.x + outer.width - rightX, inner.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        private void RebuildCharCache()
        {
            var buf = new byte[8];
            for (int c = 0; c < 256; c++)
            {
                _charset.GetCharBitmap((byte)c, buf);
                // Cache is only used for dirty detection; actual drawing is inline per-cell
                if (_charTexCache[c] != null)
                    Object.Destroy(_charTexCache[c]);
                _charTexCache[c] = null;
            }
        }

        private static Texture2D MakeTexture(Color c)
        {
            var t = new Texture2D(1, 1);
            t.SetPixel(0, 0, c);
            t.Apply();
            return t;
        }
    }
}
