using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace ARSurvival.EditorTools
{
    /// <summary>
    /// Generates the "ocean night" cartoon UI art in code (so there are no third-party images):
    /// rounded pills/panels, glowing sky flowers, a wave divider, vignette, joystick ring
    /// and hit marker; plus TextMesh Pro font assets for the bundled fonts and an outlined title
    /// material.
    /// </summary>
    public static class UIAssets
    {
        const string SpriteFolder = "Assets/Art/UI";
        const string FontFolder = "Assets/Art/Fonts";
        const string TitleMaterialPath = FontFolder + "/LuckiestGuy Outline.mat";

        public static Sprite Pill => Load("Pill");
        public static Sprite Panel => Load("Panel");
        public static Sprite PanelFrame => Load("PanelFrame");
        public static Sprite SkyFlower => Load("SkyFlower");
        public static Sprite Wave => Load("Wave");
        public static Sprite Vignette => Load("Vignette");
        public static Sprite Ring => Load("Ring");
        public static Sprite Disc => Load("Disc");
        public static Sprite HitMarker => Load("HitMarker");

        public static TMP_FontAsset TitleFont => AssetDatabase.LoadAssetAtPath<TMP_FontAsset>($"{FontFolder}/LuckiestGuy SDF.asset");
        public static Material TitleMaterial => AssetDatabase.LoadAssetAtPath<Material>(TitleMaterialPath);
        public static TMP_FontAsset BodyFont => AssetDatabase.LoadAssetAtPath<TMP_FontAsset>($"{FontFolder}/Sniglet SDF.asset");

        // Sprite name -> 9-slice border in pixels (0 = not sliced).
        static readonly (string name, int border, bool tiled)[] Sprites =
        {
            ("Pill", 63, false), ("Panel", 46, false), ("PanelFrame", 46, false), ("SkyFlower", 0, false),
            ("Wave", 0, true), ("Vignette", 0, false), ("Ring", 0, false),
            ("Disc", 0, false), ("HitMarker", 0, false),
        };

        public static void GenerateAll()
        {
            RemoveOldThemeAssets();
            Directory.CreateDirectory(SpriteFolder);

            Write("Pill", 128, 128, (x, y) => White(RoundedRectAlpha(x, y, 128, 63f)));
            Write("Panel", 128, 128, (x, y) => White(RoundedRectAlpha(x, y, 128, 44f)));
            Write("PanelFrame", 128, 128, (x, y) =>
            {
                float d = RoundedRectDistance(x, y, 128, 44f);
                return White(Mathf.Clamp01(0.5f - (Mathf.Abs(d + 3f) - 3f)));
            });

            // Five-petal outlined "sky flower" with a soft glow and a ring in the middle.
            Write("SkyFlower", 256, 256, (x, y) =>
            {
                float px = x + 0.5f - 128f, py = y + 0.5f - 128f;
                float rho = Mathf.Sqrt(px * px + py * py), theta = Mathf.Atan2(py, px);
                const float r = 112f;
                float petal = r * (0.62f + 0.38f * Mathf.Abs(Mathf.Cos(2.5f * theta)));
                float outline = Stroke(Mathf.Abs(rho - petal), 7f);
                float centre = Stroke(Mathf.Abs(rho - 26f), 6f);
                float glow = 0.35f * Mathf.Exp(-Mathf.Pow(Mathf.Min(Mathf.Abs(rho - petal), Mathf.Abs(rho - 26f)) / 14f, 2f));
                return White(Mathf.Max(Mathf.Max(outline, centre), glow));
            });

            // Gentle wave, filled below the crest; tiles horizontally.
            Write("Wave", 64, 32, (x, y) =>
            {
                float crest = 16f + 9f * Mathf.Cos(2f * Mathf.PI * (x + 0.5f) / 64f);
                return White(Mathf.Clamp01(crest - (y + 0.5f) + 0.5f));
            }, TextureWrapMode.Repeat);

            Write("Vignette", 256, 256, (x, y) =>
            {
                float dx = (x + 0.5f) / 128f - 1f, dy = (y + 0.5f) / 128f - 1f;
                float d = Mathf.Sqrt(dx * dx + dy * dy) / 1.2f;
                return White(Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.45f, 1f, d)));
            });
            Write("Ring", 256, 256, (x, y) => White(Mathf.Clamp01(1f - Mathf.Abs(Radius(x, y, 256) - 0.92f) / 0.05f)));
            Write("Disc", 256, 256, (x, y) => White(Mathf.Clamp01((1f - Radius(x, y, 256)) * 40f)));
            Write("HitMarker", 128, 128, (x, y) =>
            {
                float px = x - 63.5f, py = y - 63.5f;
                float along = Mathf.Max(Mathf.Abs(px), Mathf.Abs(py));
                bool stroke = along > 22f && along < 60f && Mathf.Abs(Mathf.Abs(px) - Mathf.Abs(py)) < 7f;
                return White(stroke ? 1f : 0f);
            });

            AssetDatabase.Refresh();
            foreach (var (name, border, tiled) in Sprites)
                ConfigureSprite(name, border, tiled);

            CreateFont("LuckiestGuy-Regular.ttf", "LuckiestGuy SDF");
            CreateFont("Sniglet-Regular.ttf", "Sniglet SDF");
            CreateTitleMaterial();
            AssetDatabase.SaveAssets();
        }

        /// <summary>Assets from earlier theme iterations that are no longer used.</summary>
        static void RemoveOldThemeAssets()
        {
            string[] old =
            {
                $"{FontFolder}/BlackOpsOne SDF.asset", $"{FontFolder}/BlackOpsOne-Regular.ttf", $"{FontFolder}/OFL-BlackOpsOne.txt",
                $"{FontFolder}/Rajdhani-Bold SDF.asset", $"{FontFolder}/Rajdhani-Bold.ttf",
                $"{FontFolder}/Rajdhani-SemiBold SDF.asset", $"{FontFolder}/Rajdhani-SemiBold.ttf", $"{FontFolder}/OFL-Rajdhani.txt",
                $"{SpriteFolder}/HazardStripes.png", $"{SpriteFolder}/Bubble.png",
                $"{FontFolder}/Sniglet-ExtraBold SDF.asset", $"{FontFolder}/Sniglet-ExtraBold.ttf", // too quirky to read at small sizes
            };
            foreach (var path in old)
                AssetDatabase.DeleteAsset(path);
        }

        // --- drawing helpers ---

        static Color White(float alpha) => new(1f, 1f, 1f, alpha);

        static float Stroke(float distance, float halfWidth) => Mathf.Clamp01(halfWidth - distance + 0.5f);

        static float Radius(int x, int y, int size)
        {
            float half = size / 2f;
            float dx = (x + 0.5f - half) / half, dy = (y + 0.5f - half) / half;
            return Mathf.Sqrt(dx * dx + dy * dy);
        }

        /// <summary>Signed distance (pixels) to a rounded square filling the texture; negative inside.</summary>
        static float RoundedRectDistance(int x, int y, int size, float radius)
        {
            float half = size / 2f - 1f;
            float qx = Mathf.Abs(x + 0.5f - size / 2f) - (half - radius);
            float qy = Mathf.Abs(y + 0.5f - size / 2f) - (half - radius);
            float outside = new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude;
            return outside + Mathf.Min(Mathf.Max(qx, qy), 0f) - radius;
        }

        static float RoundedRectAlpha(int x, int y, int size, float radius) =>
            Mathf.Clamp01(0.5f - RoundedRectDistance(x, y, size, radius));

        static void Write(string name, int w, int h, Func<int, int, Color> pixel, TextureWrapMode wrap = TextureWrapMode.Clamp)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = wrap };
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    tex.SetPixel(x, y, pixel(x, y));
            File.WriteAllBytes($"{SpriteFolder}/{name}.png", tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
        }

        static void ConfigureSprite(string name, int border, bool tiled)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath($"{SpriteFolder}/{name}.png");
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.wrapMode = tiled ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            importer.spriteBorder = new Vector4(border, border, border, border);
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect; // required for sliced/tiled images
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }

        static Sprite Load(string name) => AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteFolder}/{name}.png");

        /// <summary>
        /// Dynamic SDF font asset, pre-filled with printable ASCII so normal text needs no runtime
        /// glyph generation. Single atlas, saved with its material as sub-assets.
        /// </summary>
        static void CreateFont(string ttf, string assetName)
        {
            var path = $"{FontFolder}/{assetName}.asset";
            if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path) != null)
                return;

            var font = AssetDatabase.LoadAssetAtPath<Font>($"{FontFolder}/{ttf}");
            var asset = TMP_FontAsset.CreateFontAsset(font, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024,
                AtlasPopulationMode.Dynamic, false);
            asset.name = assetName;
            AssetDatabase.CreateAsset(asset, path);
            asset.atlasTextures[0].name = assetName + " Atlas";
            AssetDatabase.AddObjectToAsset(asset.atlasTextures[0], asset);
            asset.material.name = assetName + " Material";
            AssetDatabase.AddObjectToAsset(asset.material, asset);

            var ascii = new char[95];
            for (int i = 0; i < ascii.Length; i++)
                ascii[i] = (char)(32 + i);
            asset.TryAddCharacters(new string(ascii), out _);
            EditorUtility.SetDirty(asset);
        }

        /// <summary>Title text style: thick navy outline plus a drop shadow, like a cartoon logo.</summary>
        static void CreateTitleMaterial()
        {
            var baseMaterial = TitleFont.material;
            var material = AssetDatabase.LoadAssetAtPath<Material>(TitleMaterialPath);
            if (material == null)
            {
                material = new Material(baseMaterial);
                AssetDatabase.CreateAsset(material, TitleMaterialPath);
            }
            material.CopyPropertiesFromMaterial(baseMaterial);
            material.EnableKeyword("OUTLINE_ON");
            material.EnableKeyword("UNDERLAY_ON");
            material.SetFloat("_OutlineWidth", 0.28f);
            material.SetColor("_OutlineColor", new Color(0.03f, 0.08f, 0.24f));
            material.SetColor("_UnderlayColor", new Color(0.02f, 0.04f, 0.14f, 0.85f));
            material.SetFloat("_UnderlayOffsetX", 0.55f);
            material.SetFloat("_UnderlayOffsetY", -0.75f);
            material.SetFloat("_UnderlayDilate", 0.3f);
            material.SetFloat("_UnderlaySoftness", 0.05f);
            material.SetFloat("_FaceDilate", 0.05f);
            EditorUtility.SetDirty(material);
        }
    }
}
