using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

/// <summary>
/// Repairs TextMeshPro at build time.
///
/// AssetRipper exported the TMP shaders as dummy placeholders that keep the
/// original shader names. Those names shadow the real built-in TMP shaders, so
/// Shader.Find() returned the dummy and every glyph was drawn as a plain quad
/// (black squares). The dummies are renamed first, then the real built-in
/// shaders are resolved and assigned to every TMP material.
///
/// The exported LiberationSans SDF atlas contains Latin glyphs only, so a
/// dynamic Cyrillic font asset is generated from LiberationSans.ttf and added
/// as a fallback of the main font.
/// </summary>
public static class ArmorSimTmpFix
{
    private const string MainFontPath = "Assets/Resources/fonts & materials/LiberationSans SDF.asset";
    private const string CyrillicFontPath = "Assets/Resources/fonts & materials/LiberationSans Cyrillic SDF.asset";
    private const string SourceFontPath = "Assets/Font/LiberationSans.ttf";

    private const string DistanceFieldShaderName = "TextMeshPro/Mobile/Distance Field";
    private const string SpriteShaderName = "TextMeshPro/Sprite";

    private const string DummyDistanceFieldName = "ArmorSim/Dummy/TextMeshPro/Mobile/Distance Field";
    private const string DummySpriteName = "ArmorSim/Dummy/TextMeshPro/Sprite";

    public static void Apply()
    {
        RenameDummyShader("Assets/Shader/TextMeshPro_Mobile_Distance Field.shader", DummyDistanceFieldName);
        RenameDummyShader("Assets/Shader/TextMeshPro_Sprite.shader", DummySpriteName);

        Shader distanceField = Shader.Find(DistanceFieldShaderName);
        Shader spriteShader = Shader.Find(SpriteShaderName);

        Debug.Log("[ArmorSimTmpFix] distance field shader: " + Describe(distanceField));
        Debug.Log("[ArmorSimTmpFix] sprite shader: " + Describe(spriteShader));

        int replaced = ReassignMaterials("Assets/Resources/fonts & materials", distanceField);
        replaced += ReassignMaterial("Assets/Material/TextMeshPro_Sprite.mat", spriteShader);
        Debug.Log("[ArmorSimTmpFix] Re-pointed TMP materials to real shaders: " + replaced);

        TMP_FontAsset cyrillic = EnsureCyrillicFont(distanceField);

        TMP_FontAsset main = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(MainFontPath);
        if (main != null && cyrillic != null)
        {
            if (main.fallbackFontAssetTable == null)
            {
                main.fallbackFontAssetTable = new System.Collections.Generic.List<TMP_FontAsset>();
            }
            if (!main.fallbackFontAssetTable.Contains(cyrillic))
            {
                main.fallbackFontAssetTable.Add(cyrillic);
                EditorUtility.SetDirty(main);
            }
        }
        else if (main == null)
        {
            Debug.LogWarning("[ArmorSimTmpFix] Main font asset was not found at " + MainFontPath);
        }

        AssetDatabase.SaveAssets();
    }

    private static string Describe(Shader shader)
    {
        if (shader == null)
        {
            return "<null>";
        }
        string path = AssetDatabase.GetAssetPath(shader);
        return shader.name + " (" + (string.IsNullOrEmpty(path) ? "built-in" : path) + ")";
    }

    private static void RenameDummyShader(string path, string newName)
    {
        if (!File.Exists(path))
        {
            return;
        }

        string text = File.ReadAllText(path);
        if (text.IndexOf(newName, StringComparison.Ordinal) >= 0)
        {
            return;
        }

        string renamed = Regex.Replace(text, "^Shader\\s+\"[^\"]+\"", "Shader \"" + newName + "\"", RegexOptions.Multiline);
        if (renamed == text)
        {
            return;
        }

        File.WriteAllText(path, renamed);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        Debug.Log("[ArmorSimTmpFix] Renamed dummy shader " + path + " -> " + newName);
    }

    private static int ReassignMaterials(string folder, Shader shader)
    {
        if (shader == null)
        {
            return 0;
        }

        int changed = 0;
        string[] guids = AssetDatabase.FindAssets("t:Material", new[] { folder });
        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            if (ReassignMaterial(path, shader))
            {
                changed++;
            }
        }
        return changed;
    }

    private static bool ReassignMaterial(string path, Shader shader)
    {
        if (shader == null)
        {
            return false;
        }

        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null || material.shader == shader)
        {
            return false;
        }

        material.shader = shader;
        EditorUtility.SetDirty(material);
        return true;
    }

    private static TMP_FontAsset EnsureCyrillicFont(Shader shader)
    {
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(CyrillicFontPath);

        if (font == null)
        {
            Font source = AssetDatabase.LoadAssetAtPath<Font>(SourceFontPath);
            if (source == null)
            {
                Debug.LogWarning("[ArmorSimTmpFix] Source font not found at " + SourceFontPath);
                return null;
            }

            font = TMP_FontAsset.CreateFontAsset(
                source,
                80,
                8,
                GlyphRenderMode.SDFAA,
                2048,
                2048,
                AtlasPopulationMode.Dynamic,
                false);

            if (font == null)
            {
                Debug.LogWarning("[ArmorSimTmpFix] Could not create the Cyrillic TMP font asset.");
                return null;
            }

            font.name = "LiberationSans Cyrillic SDF";
            AssetDatabase.CreateAsset(font, CyrillicFontPath);
        }

        if (shader != null && font.material != null)
        {
            font.material.shader = shader;
        }

        string missing;
        font.TryAddCharacters(BuildCharset(), out missing);
        if (!string.IsNullOrEmpty(missing))
        {
            Debug.Log("[ArmorSimTmpFix] Cyrillic font is missing glyphs: " + missing);
        }

        // Glyphs are rendered into the atlas only after TryAddCharacters, so
        // the textures must be persisted afterwards.
        for (int i = 0; i < font.atlasTextures.Length; i++)
        {
            Texture2D atlas = font.atlasTextures[i];
            if (atlas == null)
            {
                continue;
            }
            atlas.name = "Atlas " + i;
            atlas.hideFlags = HideFlags.None;
            EditorUtility.SetDirty(atlas);
            if (!AssetDatabase.IsSubAsset(atlas))
            {
                AssetDatabase.AddObjectToAsset(atlas, font);
            }
        }

        if (font.material != null)
        {
            font.material.name = "Material";
            font.material.hideFlags = HideFlags.None;
            EditorUtility.SetDirty(font.material);
            if (!AssetDatabase.IsSubAsset(font.material))
            {
                AssetDatabase.AddObjectToAsset(font.material, font);
            }
        }

        EditorUtility.SetDirty(font);
        return font;
    }

    private static string BuildCharset()
    {
        StringBuilder builder = new StringBuilder(256);

        // Russian alphabet, upper and lower case.
        builder.Append("АБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯ");
        builder.Append("абвгдеёжзийклмнопрстуфхцчшщъыьэюя");

        // Printable ASCII.
        for (char c = ' '; c <= '~'; c++)
        {
            builder.Append(c);
        }

        // Punctuation and symbols used by the game UI.
        builder.Append("«»—–…±×÷≤≥≈→←↑↓•·°µΩ√№");

        return builder.ToString();
    }
}