using System;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

/// <summary>
/// Repairs TextMeshPro at build time:
///  - the shaders exported by AssetRipper are dummy placeholders (no SDF math),
///    so every TMP material is re-pointed at the real shader from com.unity.ugui;
///  - the exported LiberationSans SDF atlas contains Latin glyphs only, so a
///    dynamic Cyrillic-capable font asset is generated from LiberationSans.ttf
///    and registered as a fallback of the main font.
/// </summary>
public static class ArmorSimTmpFix
{
    private const string MainFontPath = "Assets/Resources/fonts & materials/LiberationSans SDF.asset";
    private const string CyrillicFontPath = "Assets/Resources/fonts & materials/LiberationSans Cyrillic SDF.asset";
    private const string SourceFontPath = "Assets/Font/LiberationSans.ttf";

    public static void Apply()
    {
        Shader distanceField = FindPackageShader("TextMeshPro/Mobile/Distance Field");
        Shader spriteShader = FindPackageShader("TextMeshPro/Sprite");

        if (distanceField == null)
        {
            Debug.LogWarning("[ArmorSimTmpFix] TextMeshPro/Mobile/Distance Field shader was not found in com.unity.ugui.");
        }

        TMP_FontAsset cyrillic = EnsureCyrillicFont(distanceField);
        ReassignMaterials(distanceField, spriteShader);

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

    private static void ReassignMaterials(Shader distanceField, Shader spriteShader)
    {
        string[] guids = AssetDatabase.FindAssets("t:Material", new[] { "Assets" });
        int replaced = 0;

        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null || material.shader == null)
            {
                continue;
            }

            string shaderName = material.shader.name;
            Shader replacement = null;
            if (shaderName == "TextMeshPro/Mobile/Distance Field")
            {
                replacement = distanceField;
            }
            else if (shaderName == "TextMeshPro/Sprite")
            {
                replacement = spriteShader;
            }

            if (replacement != null && material.shader != replacement)
            {
                material.shader = replacement;
                EditorUtility.SetDirty(material);
                replaced++;
            }
        }

        Debug.Log("[ArmorSimTmpFix] Re-pointed TMP materials to package shaders: " + replaced);
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

    private static Shader FindPackageShader(string shaderName)
    {
        string[] paths = AssetDatabase.GetAllAssetPaths();
        for (int i = 0; i < paths.Length; i++)
        {
            string path = paths[i];
            if (path.IndexOf("com.unity.ugui", StringComparison.OrdinalIgnoreCase) < 0)
            {
                continue;
            }
            if (!path.EndsWith(".shader", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(path);
            if (shader != null && shader.name == shaderName)
            {
                return shader;
            }
        }

        return Shader.Find(shaderName);
    }
}