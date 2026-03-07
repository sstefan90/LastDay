using UnityEngine;

namespace LastDay.Utilities
{
    /// <summary>
    /// Thin static wrapper around Unity's Cursor API.
    /// Call SetHoverCursor(texture) on pointer-enter and ResetCursor() on pointer-exit.
    /// If texture is null a procedural 32×32 pointer arrow is used as a fallback so
    /// the cursor always changes even before art assets are assigned in the Inspector.
    /// </summary>
    public static class CursorHelper
    {
        // Reasonable tip-of-finger hotspot for the Hand3 asset (pixels from top-left)
        private static readonly Vector2 DefaultHotspot = new Vector2(8f, 4f);

        private static Texture2D _fallback;

        /// <summary>
        /// Switch to the hover cursor. Pass the serialized Texture2D from the component;
        /// if null the procedural fallback is used automatically.
        /// </summary>
        public static void SetHoverCursor(Texture2D cursor, Vector2 hotspot)
        {
            Cursor.SetCursor(cursor != null ? cursor : GetFallback(), hotspot, CursorMode.Auto);
        }

        /// <summary>Overload that uses the default hotspot.</summary>
        public static void SetHoverCursor(Texture2D cursor)
        {
            SetHoverCursor(cursor, DefaultHotspot);
        }

        /// <summary>Restore the OS default cursor.</summary>
        public static void ResetCursor()
        {
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        }

        // ── Procedural fallback ────────────────────────────────────────────

        private static Texture2D GetFallback()
        {
            if (_fallback != null) return _fallback;
            _fallback = BuildFallbackTexture();
            return _fallback;
        }

        private static Texture2D BuildFallbackTexture()
        {
            const int S = 32;
            var tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
            var px  = new Color[S * S];

            // Start fully transparent
            for (int i = 0; i < px.Length; i++) px[i] = Color.clear;

            // Simple upward-pointing arrow — outline in white, 1-px stroke
            // Shaft: x=13..14, y=0..19
            for (int y = 0; y < 20; y++)
            {
                px[y * S + 13] = Color.white;
                px[y * S + 14] = Color.white;
            }
            // Arrowhead: y=19..28, widening triangle
            for (int y = 19; y < 29; y++)
            {
                int spread = y - 19;
                int left  = 13 - spread;
                int right = 14 + spread;
                if (left  >= 0) px[y * S + left]  = Color.white;
                if (right < S)  px[y * S + right] = Color.white;
                // fill interior
                for (int x = left + 1; x < right; x++)
                    if (x >= 0 && x < S) px[y * S + x] = Color.white;
            }

            tex.SetPixels(px);
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            return tex;
        }
    }
}
