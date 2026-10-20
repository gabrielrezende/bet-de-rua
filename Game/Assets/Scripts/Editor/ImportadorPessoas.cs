using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace BetsDeRua.Editor
{
    /// <summary>
    /// Traz as pessoas geradas no MakeHuman (Game/Ferramentas/Blender/gerar_pessoas.py) para o jogo:
    /// configura cada FBX como Humanoid (as mesmas animações de sempre servem), tira os materiais
    /// para fora do FBX (para o jogo poder pintar a camisa), deixa cabelo, sobrancelha e cílios com
    /// recorte (transparência), mede a altura e registra a pessoa na BibliotecaCorpos.
    /// Nome do arquivo: Genero_Faixa_Tom_Cabelo_N.fbx, ex.: M_Adulto_80_Curto_1 (tom 0 a 100, 100 = pele escura).
    /// Menu "Bets > Importar pessoas MakeHuman".
    /// </summary>
    public static class ImportadorPessoas
    {
        public const string Pasta = "Assets/Personagens/MakeHuman";
        private const string CaminhoBiblioteca = "Assets/Config/BibliotecaCorpos.asset";

        [MenuItem("Bets/Importar pessoas MakeHuman")]
        public static string Importar()
        {
            if (!AssetDatabase.IsValidFolder(Pasta)) return "sem pasta " + Pasta;
            // Texturas de relevo ("..._norm") marcadas como normal map (senão a bermuda fica manchada)
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { Pasta }))
            {
                string c = AssetDatabase.GUIDToAssetPath(guid);
                if (!Path.GetFileName(c).ToLowerInvariant().Contains("norm")) continue;
                if (AssetImporter.GetAtPath(c) is TextureImporter ti && ti.textureType != TextureImporterType.NormalMap)
                {
                    ti.textureType = TextureImporterType.NormalMap;
                    ti.SaveAndReimport();
                }
            }
            var lista = new List<BibliotecaCorpos.Pessoa>();
            foreach (string arquivo in Directory.GetFiles(Pasta, "*.fbx"))
            {
                string caminho = arquivo.Replace('\\', '/');
                string nome = Path.GetFileNameWithoutExtension(caminho);
                string[] partes = nome.Split('_');
                if (partes.Length < 4) continue;

                Configurar(caminho, nome);
                GameObject modelo = AssetDatabase.LoadAssetAtPath<GameObject>(caminho);
                if (modelo == null) continue;
                lista.Add(new BibliotecaCorpos.Pessoa
                {
                    modelo = modelo,
                    genero = partes[0] == "F" ? Genero.Feminino : Genero.Masculino,
                    faixa = partes[1] == "Crianca" ? FaixaEtaria.Crianca : partes[1] == "Adolescente" ? FaixaEtaria.Adolescente : FaixaEtaria.Adulto,
                    tomPele = Mathf.Clamp01(int.TryParse(partes[2], out int tom) ? tom / 100f : 0.5f),
                    cabeloComprido = partes[3] == "Comprido",
                    alturaModelo = Medir(modelo),
                });
            }
            BibliotecaCorpos b = AssetDatabase.LoadAssetAtPath<BibliotecaCorpos>(CaminhoBiblioteca);
            if (b == null) return "sem BibliotecaCorpos";
            b.pessoas = lista.ToArray();
            if (b.shaderEstilizado == null) b.shaderEstilizado = Shader.Find("Bets/PersonagemEstilizado");
            EditorUtility.SetDirty(b);
            AssetDatabase.SaveAssets();
            var resumo = new System.Text.StringBuilder($"{lista.Count} pessoas: ");
            foreach (var p in lista) resumo.Append($"{p.modelo.name} ({p.alturaModelo:0.00} m) ");
            return resumo.ToString();
        }

        private static void Configurar(string caminho, string nome)
        {
            ModelImporter imp = (ModelImporter)AssetImporter.GetAtPath(caminho);
            bool mudou = false;
            if (imp.animationType != ModelImporterAnimationType.Human)
            {
                imp.animationType = ModelImporterAnimationType.Human;
                imp.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                mudou = true;
            }
            if (imp.importAnimation) { imp.importAnimation = false; mudou = true; }
            if (imp.importBlendShapes) { imp.importBlendShapes = false; mudou = true; }
            // As texturas ficam numa pasta só (Textures), dividida entre todas as pessoas
            if (imp.materialSearch != ModelImporterMaterialSearch.Everywhere) { imp.materialSearch = ModelImporterMaterialSearch.Everywhere; mudou = true; }
            // Ligações com materiais que não existem mais (apagados para refazer) atrapalham a extração
            foreach (var par in imp.GetExternalObjectMap())
                if (par.Value == null) { imp.RemoveRemap(par.Key); mudou = true; }
            if (mudou) imp.SaveAndReimport();

            // Texturas e materiais para fora do FBX (editáveis)
            string pastaMateriais = $"{Pasta}/Materiais/{nome}";
            if (!AssetDatabase.IsValidFolder($"{Pasta}/Materiais")) AssetDatabase.CreateFolder(Pasta, "Materiais");
            if (!AssetDatabase.IsValidFolder(pastaMateriais)) AssetDatabase.CreateFolder($"{Pasta}/Materiais", nome);
            AssetDatabase.Refresh();
            foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(caminho))
            {
                if (!(o is Material m)) continue;
                string destino = $"{pastaMateriais}/{m.name.Replace(':', '_').Replace('/', '_')}.mat";
                if (AssetDatabase.LoadAssetAtPath<Material>(destino) == null)
                {
                    string erro = AssetDatabase.ExtractAsset(m, destino);
                    if (!string.IsNullOrEmpty(erro)) Debug.LogWarning($"[ImportadorPessoas] {m.name}: {erro}");
                }
            }
            AssetDatabase.WriteImportSettingsIfDirty(caminho);
            AssetDatabase.ImportAsset(caminho, ImportAssetOptions.ForceUpdate);

            // Cabelo, sobrancelha e cílios: recorte pela transparência da textura, dos dois lados
            foreach (string guid in AssetDatabase.FindAssets("t:Material", new[] { pastaMateriais }))
            {
                Material m = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
                string n = m.name.ToLowerInvariant();
                bool recorte = n.StartsWith("cabelo") || n.StartsWith("sobrancelha") || n.StartsWith("cilios");
                if (m.shader == null || !m.shader.name.StartsWith("Universal Render Pipeline")) m.shader = Shader.Find("Universal Render Pipeline/Lit");
                if (recorte)
                {
                    m.SetFloat("_Surface", 0f);
                    m.SetFloat("_AlphaClip", 1f);
                    m.SetFloat("_Cutoff", 0.35f);
                    m.EnableKeyword("_ALPHATEST_ON");
                    m.SetFloat("_Cull", 0f);
                    m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
                }
                m.SetFloat("_Smoothness", n.StartsWith("olhos") || n.Contains("high-poly") ? 0.85f : n.StartsWith("pele") ? 0.35f : recorte ? 0.25f : 0.2f);
                EditorUtility.SetDirty(m);
            }
            AssetDatabase.SaveAssets();
        }

        /// <summary>Altura do modelo (do chão ao topo da cabeça, sem cabelo), na pose de importação.</summary>
        private static float Medir(GameObject modelo)
        {
            GameObject g = Object.Instantiate(modelo);
            try
            {
                Bounds? caixa = null;
                foreach (Renderer r in g.GetComponentsInChildren<Renderer>())
                {
                    string n = r.name.ToLowerInvariant();
                    if (n.StartsWith("cabelo")) continue;
                    if (caixa == null) caixa = r.bounds;
                    else { Bounds c = caixa.Value; c.Encapsulate(r.bounds); caixa = c; }
                }
                // Os pés ficam no chão (y = 0) no MakeHuman: a altura é o topo da cabeça
                return caixa.HasValue ? caixa.Value.max.y - g.transform.position.y : 1.75f;
            }
            finally { Object.DestroyImmediate(g); }
        }
    }
}
