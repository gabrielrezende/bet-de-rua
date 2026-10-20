using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace BetsDeRua.Editor
{
    /// <summary>
    /// Prepara os modelos 3D do Poly Haven (CC0, Game/Ferramentas/baixar_polyhaven.py) para o jogo:
    /// cria um material URP para cada material do FBX a partir das texturas da pasta (cor, relevo,
    /// "arm" = oclusão/aspereza/metal, ou metal e aspereza separados, e transparência), liga no FBX
    /// e devolve o modelo pronto. Usado pelo construtor da rua (portas de enrolar, ar-condicionado,
    /// lixeira, pneu, bueiro, poste, caixa de luz...).
    /// </summary>
    public static class ImportadorModelos
    {
        public const string Pasta = "Assets/Modelos/PolyHaven";
        // Metal da rua é pintado, sujo e fosco; sem sonda de reflexo assada, metal puro reflete uma cor falsa
        private const float MetalMaximo = 0.35f;
        private const float LisuraMaxima = 0.6f;
        private static readonly Dictionary<string, GameObject> prontos = new Dictionary<string, GameObject>();

        [MenuItem("Bets/Preparar modelos Poly Haven")]
        public static string PrepararTodos()
        {
            prontos.Clear();
            var nomes = new List<string>();
            foreach (string d in Directory.GetDirectories(Pasta))
            {
                string id = Path.GetFileName(d);
                if (Modelo(id) != null) nomes.Add(id);
            }
            AssetDatabase.SaveAssets();
            return $"{nomes.Count} modelos: {string.Join(", ", nomes)}";
        }

        /// <summary>O modelo pronto (com materiais) pelo id do Poly Haven; null se não tiver sido baixado.</summary>
        public static GameObject Modelo(string id)
        {
            if (prontos.TryGetValue(id, out GameObject pronto) && pronto != null) return pronto;
            string caminho = $"{Pasta}/{id}/{id}.fbx";
            ModelImporter imp = AssetImporter.GetAtPath(caminho) as ModelImporter;
            if (imp == null) return null;
            string pastaTex = $"{Pasta}/{id}/textures";
            string pastaMat = $"{Pasta}/{id}/Materiais";
            if (!AssetDatabase.IsValidFolder(pastaMat)) AssetDatabase.CreateFolder($"{Pasta}/{id}", "Materiais");

            bool mudou = false;
            if (imp.animationType != ModelImporterAnimationType.None) { imp.animationType = ModelImporterAnimationType.None; mudou = true; }
            if (imp.importAnimation) { imp.importAnimation = false; mudou = true; }
            if (imp.materialImportMode != ModelImporterMaterialImportMode.ImportStandard) { imp.materialImportMode = ModelImporterMaterialImportMode.ImportStandard; mudou = true; }
            if (mudou) imp.SaveAndReimport();

            var nomesMat = AssetDatabase.LoadAllAssetsAtPath(caminho).OfType<Material>().Select(m => m.name).Distinct().ToList();
            foreach (var par in imp.GetExternalObjectMap()) if (par.Key.type == typeof(Material) && !nomesMat.Contains(par.Key.name)) nomesMat.Add(par.Key.name);
            string[] texturas = Directory.Exists(pastaTex) ? Directory.GetFiles(pastaTex).Where(f => !f.EndsWith(".meta")).Select(f => f.Replace('\\', '/')).ToArray() : new string[0];
            foreach (string nomeMat in nomesMat)
            {
                Material m = CriarMaterial(nomeMat, id, texturas, pastaMat);
                imp.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), nomeMat), m);
            }
            imp.SaveAndReimport();
            GameObject modelo = AssetDatabase.LoadAssetAtPath<GameObject>(caminho);
            prontos[id] = modelo;
            return modelo;
        }

        private static Material CriarMaterial(string nomeMat, string id, string[] texturas, string pastaMat)
        {
            string caminhoMat = $"{pastaMat}/{nomeMat}.mat";
            Material m = AssetDatabase.LoadAssetAtPath<Material>(caminhoMat);
            if (m == null)
            {
                m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(m, caminhoMat);
            }
            // As texturas têm o nome do material na frente (ex.: metal_trash_can_rust_diff_1k.jpg);
            // se não achar pelo material, usa as do modelo (id_diff...)
            string Achar(string tipo)
            {
                foreach (string prefixo in new[] { nomeMat, id })
                {
                    string t = texturas.FirstOrDefault(f => Path.GetFileName(f).StartsWith(prefixo + "_" + tipo + "_"));
                    if (t != null) return t;
                }
                return null;
            }
            Texture2D cor = Carregar(Achar("diff"), false);
            Texture2D relevo = Carregar(Achar("nor_gl"), true);
            string alfa = Achar("alpha") ?? Achar("opacity");
            if (cor != null && alfa != null) cor = JuntarAlfa(cor, Carregar(alfa, false), $"{pastaMat}/{nomeMat}_corAlfa.png");
            Texture2D metalLiso = MetalLiso(Achar("arm"), Achar("metal"), Achar("rough"), $"{pastaMat}/{nomeMat}_metalLiso.png");

            m.SetColor("_BaseColor", Color.white);
            if (cor != null) m.SetTexture("_BaseMap", cor);
            if (relevo != null) { m.SetTexture("_BumpMap", relevo); m.EnableKeyword("_NORMALMAP"); m.SetFloat("_BumpScale", 1f); }
            if (metalLiso != null)
            {
                m.SetTexture("_MetallicGlossMap", metalLiso);
                m.EnableKeyword("_METALLICSPECGLOSSMAP");
                m.SetFloat("_Smoothness", 1f);
            }
            else m.SetFloat("_Smoothness", 0.3f);
            if (alfa != null)
            {
                m.SetFloat("_AlphaClip", 1f);
                m.SetFloat("_Cutoff", 0.4f);
                m.EnableKeyword("_ALPHATEST_ON");
                m.SetFloat("_Cull", 0f);
                m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
            }
            m.enableInstancing = true;
            EditorUtility.SetDirty(m);
            return m;
        }

        private static Texture2D Carregar(string caminho, bool normal)
        {
            if (caminho == null) return null;
            if (AssetImporter.GetAtPath(caminho) is TextureImporter ti && normal && ti.textureType != TextureImporterType.NormalMap)
            {
                ti.textureType = TextureImporterType.NormalMap;
                ti.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(caminho);
        }

        private static Color[] Pixels(string caminho, int largura, int altura)
        {
            byte[] bytes = File.ReadAllBytes(caminho);
            Texture2D t = new Texture2D(2, 2);
            t.LoadImage(bytes);
            if (t.width != largura || t.height != altura)
            {
                RenderTexture rt = RenderTexture.GetTemporary(largura, altura);
                Graphics.Blit(t, rt);
                RenderTexture antes = RenderTexture.active;
                RenderTexture.active = rt;
                Texture2D r = new Texture2D(largura, altura, TextureFormat.RGBA32, false);
                r.ReadPixels(new Rect(0, 0, largura, altura), 0, 0);
                r.Apply();
                RenderTexture.active = antes;
                RenderTexture.ReleaseTemporary(rt);
                Object.DestroyImmediate(t);
                t = r;
            }
            Color[] px = t.GetPixels();
            Object.DestroyImmediate(t);
            return px;
        }

        /// <summary>URP lê metal no vermelho e lisura no alfa: monta essa textura a partir do "arm" ou de metal + aspereza.</summary>
        private static Texture2D MetalLiso(string arm, string metal, string aspereza, string destino)
        {
            if (arm == null && aspereza == null) return null;
            if (AssetDatabase.LoadAssetAtPath<Texture2D>(destino) != null) return AssetDatabase.LoadAssetAtPath<Texture2D>(destino);
            const int n = 512;
            Color[] saida = new Color[n * n];
            if (arm != null)
            {
                Color[] a = Pixels(arm, n, n);
                for (int i = 0; i < saida.Length; i++) saida[i] = new Color(a[i].b * MetalMaximo, 0f, 0f, (1f - a[i].g) * LisuraMaxima);
            }
            else
            {
                Color[] r = Pixels(aspereza, n, n);
                Color[] me = metal != null ? Pixels(metal, n, n) : null;
                for (int i = 0; i < saida.Length; i++) saida[i] = new Color((me != null ? me[i].r : 0f) * MetalMaximo, 0f, 0f, (1f - r[i].r) * LisuraMaxima);
            }
            return Salvar(saida, n, n, destino, false);
        }

        private static Texture2D JuntarAlfa(Texture2D cor, Texture2D alfa, string destino)
        {
            if (AssetDatabase.LoadAssetAtPath<Texture2D>(destino) != null) return AssetDatabase.LoadAssetAtPath<Texture2D>(destino);
            string cc = AssetDatabase.GetAssetPath(cor), ca = AssetDatabase.GetAssetPath(alfa);
            const int n = 1024;
            Color[] c = Pixels(cc, n, n), a = Pixels(ca, n, n);
            for (int i = 0; i < c.Length; i++) c[i].a = a[i].r;
            return Salvar(c, n, n, destino, true);
        }

        private static Texture2D Salvar(Color[] px, int l, int a, string destino, bool alfa)
        {
            Texture2D t = new Texture2D(l, a, TextureFormat.RGBA32, true);
            t.SetPixels(px);
            t.Apply();
            File.WriteAllBytes(destino, t.EncodeToPNG());
            Object.DestroyImmediate(t);
            AssetDatabase.ImportAsset(destino);
            TextureImporter ti = (TextureImporter)AssetImporter.GetAtPath(destino);
            ti.sRGBTexture = alfa; // cor com alfa é sRGB; metal/lisura é dado linear
            ti.alphaIsTransparency = alfa;
            ti.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(destino);
        }

        /// <summary>Tamanho do modelo (caixa que envolve tudo), em metros.</summary>
        public static Bounds Medir(GameObject modelo)
        {
            GameObject g = (GameObject)PrefabUtility.InstantiatePrefab(modelo);
            g.transform.position = Vector3.zero;
            try
            {
                Bounds? caixa = null;
                foreach (Renderer r in g.GetComponentsInChildren<Renderer>())
                {
                    if (caixa == null) caixa = r.bounds;
                    else { Bounds c = caixa.Value; c.Encapsulate(r.bounds); caixa = c; }
                }
                return caixa ?? new Bounds(Vector3.zero, Vector3.one);
            }
            finally { Object.DestroyImmediate(g); }
        }
    }
}
