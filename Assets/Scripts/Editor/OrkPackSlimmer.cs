using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace ARSurvival.EditorTools
{
    /// <summary>
    /// Shrinks the OrkDestroyer2 Asset Store pack (~870 MB) to what the game uses, so the repository
    /// stays small: removes unused animations and demo content, and converts the uncompressed 4K
    /// .tga textures to 1024 px .png. Texture .meta files are kept, so asset IDs (and every material
    /// reference) are unchanged. The full pack can be re-downloaded from the Asset Store.
    /// </summary>
    public static class OrkPackSlimmer
    {
        const string Root = "Assets/OrkDestroyer2";
        const int MaxSize = 1024;
        static readonly string[] KeepAnimations = { "idle1", "walk1", "gethit", "death1" };

        [MenuItem("Tools/AR Survival/Slim OrkDestroyer2 Pack")]
        public static void Run()
        {
            var log = new StringBuilder();
            long before = FolderBytes(Root);

            // 1) Unused animations, the pack's own controller, demo scene and display stand.
            foreach (var guid in AssetDatabase.FindAssets("", new[] { $"{Root}/Animation" }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var clip = Path.GetFileNameWithoutExtension(path).Replace("Ork_destroyer2@", "");
                if (path.EndsWith(".fbx") && !KeepAnimations.Contains(clip))
                    Delete(path, log);
            }
            foreach (var guid in AssetDatabase.FindAssets("t:AnimatorController t:Scene", new[] { Root }))
                Delete(AssetDatabase.GUIDToAssetPath(guid), log);
            Delete($"{Root}/Stand", log);

            // 2) Textures: drop any no material uses, downscale the rest to PNG.
            var usedGuids = new HashSet<string>();
            foreach (var guid in AssetDatabase.FindAssets("t:Material", new[] { $"{Root}/Materials" }))
                foreach (var dep in AssetDatabase.GetDependencies(AssetDatabase.GUIDToAssetPath(guid), false))
                    usedGuids.Add(AssetDatabase.AssetPathToGUID(dep));

            var converted = new List<string>();
            foreach (var tga in Directory.GetFiles($"{Root}/Textures", "*.tga"))
            {
                var path = tga.Replace('\\', '/');
                if (!usedGuids.Contains(AssetDatabase.AssetPathToGUID(path)))
                {
                    Delete(path, log);
                    continue;
                }
                ConvertToPng(path, log);
                converted.Add(path);
            }

            AssetDatabase.Refresh();
            log.AppendLine($"Converted {converted.Count} textures. Pack size {before / 1048576f:F0} MB -> {FolderBytes(Root) / 1048576f:F0} MB");
            Debug.Log("[AR Survival] Ork pack slimmed.\n" + log);
        }

        static void Delete(string path, StringBuilder log)
        {
            if (AssetDatabase.DeleteAsset(path))
                log.AppendLine($"deleted {path}");
        }

        /// <summary>Decodes the .tga, box-filters it down, writes a .png and moves the .meta across.</summary>
        public static void ConvertToPng(string tgaPath, StringBuilder log)
        {
            var pixels = ReadTga(tgaPath, out int w, out int h);
            int factor = Mathf.Max(1, Mathf.Max(w, h) / MaxSize);
            int nw = w / factor, nh = h / factor;
            var small = new Color32[nw * nh];
            int area = factor * factor;
            for (int y = 0; y < nh; y++)
                for (int x = 0; x < nw; x++)
                {
                    int r = 0, g = 0, b = 0, a = 0;
                    for (int dy = 0; dy < factor; dy++)
                    {
                        int row = (y * factor + dy) * w + x * factor;
                        for (int dx = 0; dx < factor; dx++)
                        {
                            var p = pixels[row + dx];
                            r += p.r; g += p.g; b += p.b; a += p.a;
                        }
                    }
                    small[y * nw + x] = new Color32((byte)(r / area), (byte)(g / area), (byte)(b / area), (byte)(a / area));
                }

            var tex = new Texture2D(nw, nh, TextureFormat.RGBA32, false);
            tex.SetPixels32(small);
            var pngPath = Path.ChangeExtension(tgaPath, ".png");
            File.WriteAllBytes(pngPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);

            File.Move(tgaPath + ".meta", pngPath + ".meta"); // keeps the GUID
            File.Delete(tgaPath);
            log.AppendLine($"{Path.GetFileName(tgaPath)} {w}x{h} -> {Path.GetFileName(pngPath)} {nw}x{nh}");
        }

        /// <summary>Minimal TGA reader: true-colour 24/32-bit, raw or RLE. Returns bottom-up rows.</summary>
        static Color32[] ReadTga(string path, out int w, out int h)
        {
            var data = File.ReadAllBytes(path);
            int idLength = data[0], colorMapType = data[1], imageType = data[2];
            int colorMapLength = data[5] | (data[6] << 8), colorMapEntry = data[7];
            w = data[12] | (data[13] << 8);
            h = data[14] | (data[15] << 8);
            int bpp = data[16], descriptor = data[17];
            if ((imageType != 2 && imageType != 10) || (bpp != 24 && bpp != 32))
                throw new System.NotSupportedException($"{path}: TGA type {imageType}, {bpp} bpp");

            int bytesPerPixel = bpp / 8;
            int offset = 18 + idLength + (colorMapType != 0 ? colorMapLength * colorMapEntry / 8 : 0);
            var pixels = new Color32[w * h];
            int count = w * h, i = 0;

            Color32 Read(ref int o)
            {
                byte b = data[o], g = data[o + 1], r = data[o + 2];
                byte a = bytesPerPixel == 4 ? data[o + 3] : (byte)255;
                o += bytesPerPixel;
                return new Color32(r, g, b, a);
            }

            if (imageType == 2)
            {
                for (; i < count; i++)
                    pixels[i] = Read(ref offset);
            }
            else
            {
                while (i < count)
                {
                    int header = data[offset++];
                    int run = (header & 0x7F) + 1;
                    if ((header & 0x80) != 0)
                    {
                        var p = Read(ref offset);
                        for (int k = 0; k < run; k++)
                            pixels[i++] = p;
                    }
                    else
                    {
                        for (int k = 0; k < run; k++)
                            pixels[i++] = Read(ref offset);
                    }
                }
            }

            // Bit 5 set = top-left origin; Unity textures are bottom-up.
            if ((descriptor & 0x20) != 0)
                for (int y = 0; y < h / 2; y++)
                    for (int x = 0; x < w; x++)
                        (pixels[y * w + x], pixels[(h - 1 - y) * w + x]) = (pixels[(h - 1 - y) * w + x], pixels[y * w + x]);
            return pixels;
        }

        static long FolderBytes(string folder) =>
            Directory.GetFiles(folder, "*", SearchOption.AllDirectories).Sum(f => new FileInfo(f).Length);
    }
}
