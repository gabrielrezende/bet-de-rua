using System.IO;
using UnityEditor;
using UnityEngine;

namespace BetsDeRua.Editor
{
    /// <summary>
    /// Deixa o jogo pronto para gerar a versão de Windows (para instalar no PC dos amigos):
    /// nome, versão, ícone (dois tacos em X e a garrafa), tela cheia, cenas na ordem certa.
    /// Menu "Bets > Preparar build" e "Bets > Gerar build Windows" (sai em Game/Builds/BetsDeRua).
    /// </summary>
    public static class ConfiguracaoBuild
    {
        public const string Versao = "0.5.0";
        public const string PastaSaida = "Builds/BetsDeRua";

        [MenuItem("Bets/Preparar build")]
        public static void Preparar()
        {
            PlayerSettings.companyName = "Bets de Rua";
            PlayerSettings.productName = "Bets de Rua";
            PlayerSettings.bundleVersion = Versao;
            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
            PlayerSettings.defaultIsNativeResolution = true;
            PlayerSettings.runInBackground = false;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.visibleInBackground = true;
            PlayerSettings.SplashScreen.showUnityLogo = false;
            Texture2D icone = CriarIcone();
            PlayerSettings.SetIcons(UnityEditor.Build.NamedBuildTarget.Unknown, new[] { icone }, IconKind.Any);
            MontadorMenu.AtualizarBuild();
            AssetDatabase.SaveAssets();
        }

        [MenuItem("Bets/Gerar build Windows")]
        public static string Gerar()
        {
            Preparar();
            string caminho = Path.Combine(PastaSaida, "BetsDeRua.exe");
            var opcoes = new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Scenes/Menu.unity", "Assets/Scenes/Prototipo.unity" },
                locationPathName = caminho,
                target = BuildTarget.StandaloneWindows64,
                // Sempre do zero: o cache já entregou uma cena velha (level0 "corrompido") depois de mudar scripts
                options = BuildOptions.CleanBuildCache,
            };
            var relatorio = BuildPipeline.BuildPlayer(opcoes);
            return $"{relatorio.summary.result} · {relatorio.summary.totalSize / (1024 * 1024)} MB · {relatorio.summary.totalTime}";
        }

        /// <summary>Ícone 256x256: fundo laranja redondo, dois tacos cruzados (o "TEC!") e a garrafa verde.</summary>
        private static Texture2D CriarIcone()
        {
            const int n = 256;
            Texture2D t = new Texture2D(n, n, TextureFormat.RGBA32, false);
            Vector2 c = new Vector2(n * 0.5f, n * 0.5f);
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    Vector2 p = new Vector2(x, y);
                    float d = Vector2.Distance(p, c);
                    Color cor = new Color(0, 0, 0, 0);
                    if (d < n * 0.48f) cor = Color.Lerp(new Color(1f, 0.6f, 0.1f), new Color(0.95f, 0.3f, 0.1f), y / (float)n);
                    if (d >= n * 0.44f && d < n * 0.48f) cor = new Color(0.1f, 0.1f, 0.12f);
                    // Tacos em X (pás de madeira)
                    foreach (float sinal in new[] { -1f, 1f })
                    {
                        Vector2 eixo = new Vector2(sinal, 1f).normalized;
                        Vector2 rel = p - c;
                        float ao = Vector2.Dot(rel, eixo);
                        float lado = Mathf.Abs(rel.x * eixo.y - rel.y * eixo.x);
                        float largura = ao > n * 0.12f ? 7f : 12f; // cabo fino em cima, pá larga embaixo
                        if (Mathf.Abs(ao) < n * 0.36f && lado < largura && d < n * 0.44f)
                            cor = ao > n * 0.12f ? new Color(0.12f, 0.12f, 0.14f) : new Color(0.93f, 0.78f, 0.5f);
                    }
                    // Garrafa no meio embaixo
                    Vector2 g = p - new Vector2(c.x, n * 0.22f);
                    if (Mathf.Abs(g.x) < 16f && g.y > -20f && g.y < 26f) cor = new Color(0.15f, 0.55f, 0.28f);
                    if (Mathf.Abs(g.x) < 7f && g.y >= 26f && g.y < 42f) cor = new Color(0.15f, 0.55f, 0.28f);
                    if (Mathf.Abs(g.x) < 8f && g.y >= 42f && g.y < 48f) cor = Color.white;
                    t.SetPixel(x, y, cor);
                }
            t.Apply();
            string caminho = "Assets/UI/Icone.png";
            File.WriteAllBytes(caminho, t.EncodeToPNG());
            Object.DestroyImmediate(t);
            AssetDatabase.ImportAsset(caminho);
            TextureImporter imp = (TextureImporter)AssetImporter.GetAtPath(caminho);
            imp.alphaIsTransparency = true;
            imp.mipmapEnabled = false;
            imp.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(caminho);
        }
    }
}
