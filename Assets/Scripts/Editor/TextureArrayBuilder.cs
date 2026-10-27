using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class TextureArrayBuilder
{
    [MenuItem("Assets/Eyes/Texture2DArray da seleção")]
    static void Build()
    {
        var texs = Selection.GetFiltered<Texture2D>(SelectionMode.Assets)
                            .OrderBy(t => t.name).ToArray();
        if (texs.Length == 0) { Debug.LogError("Selecione os PNGs primeiro."); return; }

        var f = texs[0];
        foreach (var t in texs)
            if (t.width != f.width || t.height != f.height || t.format != f.format || t.mipmapCount != f.mipmapCount)
            { Debug.LogError($"{t.name} tem tamanho/formato/mips diferente de {f.name}."); return; }

        var arr = new Texture2DArray(f.width, f.height, texs.Length, f.format, f.mipmapCount > 1)
        { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };

        for (int i = 0; i < texs.Length; i++)
            Graphics.CopyTexture(texs[i], 0, arr, i);

        string dir = Path.GetDirectoryName(AssetDatabase.GetAssetPath(f));
        string path = AssetDatabase.GenerateUniqueAssetPath($"{dir}/{f.name}_Array.asset");
        AssetDatabase.CreateAsset(arr, path);
        Debug.Log($"Array criada: {path} ({texs.Length} fatias)");
    }
}