using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Media;
using PdfSharp.Fonts;

namespace MortuaryApp.Helpers;

public class CustomFontResolver : IFontResolver
{
    private readonly Dictionary<string, (string path, int index)> _cache = new(StringComparer.OrdinalIgnoreCase);

    public FontResolverInfo? ResolveTypeface(string familyName, bool isBold, bool isItalic)
    {
        var key = $"{familyName}|{(isBold ? "B" : "R")}{(isItalic ? "I" : "N")}";

        if (!_cache.ContainsKey(key))
        {
            try
            {
                var ff = new FontFamily(familyName);
                foreach (var tf in ff.GetTypefaces())
                {
                    if (tf.TryGetGlyphTypeface(out var glyph) && glyph.FontUri.IsFile)
                    {
                        _cache[key] = (glyph.FontUri.LocalPath, 0);
                        break;
                    }
                }
            }
            catch { }

            if (!_cache.ContainsKey(key))
            {
                var fallback = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Fonts), "seguiui.ttf");
                _cache[key] = (fallback, 0);
            }
        }

        return new FontResolverInfo(key, isBold, isItalic);
    }

    public byte[]? GetFont(string faceName)
    {
        if (_cache.TryGetValue(faceName, out var entry) && File.Exists(entry.path))
            return File.ReadAllBytes(entry.path);
        return null;
    }
}
