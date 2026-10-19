using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace BetsDeRua.Editor
{
    /// <summary>
    /// Configura a importação dos personagens de verdade (Quaternius, CC0, em Assets/Personagens):
    /// corpos e animações como Humanoid (qualquer animação serve em qualquer corpo, como no
    /// Mixamo), cabelos presos ao osso da cabeça, texturas de pele/normal. Também lista as
    /// animações disponíveis.
    /// </summary>
    public static class ImportadorPersonagens
    {
        public const string Pasta = "Assets/Personagens";

        [MenuItem("Bets/Configurar personagens importados")]
        public static string Configurar()
        {
            int n = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:Model", new[] { Pasta }))
            {
                string caminho = AssetDatabase.GUIDToAssetPath(guid);
                ModelImporter imp = AssetImporter.GetAtPath(caminho) as ModelImporter;
                if (imp == null) continue;
                bool animacao = caminho.Contains("/Animacoes/");
                bool cabelo = caminho.Contains("/Cabelos/");
                imp.globalScale = 1f;
                imp.materialImportMode = ModelImporterMaterialImportMode.None;
                imp.importBlendShapes = false;
                imp.animationType = cabelo ? ModelImporterAnimationType.Generic : ModelImporterAnimationType.Human;
                imp.avatarSetup = cabelo ? ModelImporterAvatarSetup.NoAvatar : ModelImporterAvatarSetup.CreateFromThisModel;
                imp.importAnimation = animacao;
                if (animacao)
                {
                    // Animações no lugar (quem move o corpo é o CharacterController do jogo)
                    var clipes = imp.defaultClipAnimations;
                    foreach (var c in clipes)
                    {
                        string nome = c.name.ToLowerInvariant();
                        bool loop = nome.Contains("idle") || nome.Contains("walk") || nome.Contains("jog") || nome.Contains("run")
                                    || nome.Contains("sprint") || nome.Contains("loop") || nome.Contains("crouch");
                        c.loopTime = loop;
                        c.lockRootRotation = true;
                        c.lockRootHeightY = true;
                        c.lockRootPositionXZ = true;
                        c.keepOriginalOrientation = true;
                        c.keepOriginalPositionY = true;
                        c.keepOriginalPositionXZ = true;
                    }
                    imp.clipAnimations = clipes;
                }
                imp.SaveAndReimport();
                n++;
            }
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { Pasta }))
            {
                string caminho = AssetDatabase.GUIDToAssetPath(guid);
                TextureImporter t = AssetImporter.GetAtPath(caminho) as TextureImporter;
                if (t == null) continue;
                bool normal = caminho.Contains("Normal");
                bool aspereza = caminho.Contains("Roughness");
                t.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
                t.sRGBTexture = !(normal || aspereza);
                t.maxTextureSize = 2048;
                t.SaveAndReimport();
            }
            AssetDatabase.SaveAssets();
            return $"{n} modelos configurados";
        }

        /// <summary>Lista as animações (nome e duração) dos arquivos de animação.</summary>
        public static string ListarAnimacoes()
        {
            var linhas = new List<string>();
            foreach (string guid in AssetDatabase.FindAssets("t:Model", new[] { Pasta + "/Animacoes" }))
            {
                string caminho = AssetDatabase.GUIDToAssetPath(guid);
                foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(caminho))
                    if (o is AnimationClip c && !c.name.StartsWith("__preview"))
                        linhas.Add($"{System.IO.Path.GetFileNameWithoutExtension(caminho)}:{c.name}:{c.length:0.00}");
            }
            return string.Join("\n", linhas.OrderBy(l => l));
        }
    }
}
