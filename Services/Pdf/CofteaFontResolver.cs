using System.Collections.Concurrent;
using System.IO;
using Microsoft.Maui.Storage;
using PdfSharpCore.Fonts;

namespace Coftea_Capstone.Services.Pdf
{
    public sealed class CofteaFontResolver : IFontResolver
    {
        public static readonly CofteaFontResolver Instance = new();

        private const string RegularFace = "OpenSans#Regular";
        private const string BoldFace = "OpenSans#Semibold";

        private readonly ConcurrentDictionary<string, byte[]> _fontCache = new();

        private CofteaFontResolver()
        {
        }

        public string DefaultFontName => "OpenSans";

        public FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic)
        {
            var name = familyName?.ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(name) || name.Contains("opensans") || name.Contains("helvetica"))
            {
                return new FontResolverInfo(isBold ? BoldFace : RegularFace);
            }

            return new FontResolverInfo(isBold ? BoldFace : RegularFace);
        }

        public byte[] GetFont(string faceName)
        {
            return faceName switch
            {
                BoldFace => LoadFont("OpenSans-Semibold.ttf"),
                RegularFace => LoadFont("OpenSans-Regular.ttf"),
                _ => LoadFont("OpenSans-Regular.ttf")
            };
        }

        private byte[] LoadFont(string fileName)
        {
            return _fontCache.GetOrAdd(fileName, name =>
            {
                var candidates = new[]
                {
                    name,
                    $"Fonts/{name}",
                    $"fonts/{name}"
                };

                foreach (var candidate in candidates)
                {
                    try
                    {
                        using var stream = FileSystem.OpenAppPackageFileAsync(candidate).GetAwaiter().GetResult();
                        using var memory = new MemoryStream();
                        stream.CopyTo(memory);
                        return memory.ToArray();
                    }
                    catch (FileNotFoundException)
                    {
                        // try next candidate
                    }
                }

                throw new FileNotFoundException($"Could not locate font '{name}' in app package. Ensure the font file is marked as a MauiFont in Resources/Fonts.");
            });
        }
    }
}

