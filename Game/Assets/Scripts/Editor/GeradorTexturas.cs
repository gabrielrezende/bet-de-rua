using System.IO;
using UnityEditor;
using UnityEngine;

namespace BetsDeRua.Editor
{
    /// <summary>
    /// Gera as texturas da rua por código (sem fotos de fora) e salva em Assets/Texturas:
    /// asfalto gasto (rachaduras, remendos, manchas), calçada de concreto, reboco com sujeira
    /// embaixo, tijolo baiano, portão de enrolar, telha, giz (com borda falhada), pichação,
    /// folhagem e terra. Cada uma com um mapa de relevo (normal map) quando faz sentido.
    /// </summary>
    public static class GeradorTexturas
    {
        public const string Pasta = "Assets/Texturas";

        // ------------------------------------------------------------------ ruído

        private static float Ruido(float x, float y, int oitavas, float escala, int semente = 0)
        {
            float soma = 0f, amp = 1f, total = 0f, f = escala;
            for (int i = 0; i < oitavas; i++)
            {
                soma += Mathf.PerlinNoise(x * f + semente * 31.7f + i * 17.3f, y * f + semente * 13.1f + i * 7.9f) * amp;
                total += amp;
                amp *= 0.5f;
                f *= 2f;
            }
            return soma / total;
        }

        /// <summary>Ruído que repete nas bordas (a textura emenda sem costura).</summary>
        private static float RuidoRepetido(int x, int y, int tamanho, int oitavas, float escala, int semente = 0)
        {
            float u = x / (float)tamanho, v = y / (float)tamanho;
            float a = Ruido(u, v, oitavas, escala, semente);
            float b = Ruido(u - 1f, v, oitavas, escala, semente);
            float c = Ruido(u, v - 1f, oitavas, escala, semente);
            float d = Ruido(u - 1f, v - 1f, oitavas, escala, semente);
            return Mathf.Lerp(Mathf.Lerp(a, b, u), Mathf.Lerp(c, d, u), v);
        }

        // ------------------------------------------------------------------ salvar

        private static Texture2D Salvar(string nome, Color[] px, int tamanho, bool normal = false, bool alfa = false)
        {
            if (!AssetDatabase.IsValidFolder(Pasta)) AssetDatabase.CreateFolder("Assets", "Texturas");
            Texture2D t = new Texture2D(tamanho, tamanho, TextureFormat.RGBA32, false);
            t.SetPixels(px);
            t.Apply();
            string caminho = $"{Pasta}/{nome}.png";
            File.WriteAllBytes(caminho, t.EncodeToPNG());
            Object.DestroyImmediate(t);
            AssetDatabase.ImportAsset(caminho);
            TextureImporter imp = (TextureImporter)AssetImporter.GetAtPath(caminho);
            imp.textureType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            imp.wrapMode = TextureWrapMode.Repeat;
            imp.mipmapEnabled = true;
            imp.anisoLevel = 8;
            imp.alphaIsTransparency = alfa;
            imp.sRGBTexture = !normal;
            imp.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(caminho);
        }

        /// <summary>Normal map a partir de um mapa de altura (0..1).</summary>
        private static Texture2D Relevo(string nome, float[] altura, int tamanho, float forca)
        {
            Color[] px = new Color[tamanho * tamanho];
            for (int y = 0; y < tamanho; y++)
                for (int x = 0; x < tamanho; x++)
                {
                    float hl = altura[y * tamanho + (x - 1 + tamanho) % tamanho];
                    float hr = altura[y * tamanho + (x + 1) % tamanho];
                    float hd = altura[((y - 1 + tamanho) % tamanho) * tamanho + x];
                    float hu = altura[((y + 1) % tamanho) * tamanho + x];
                    Vector3 n = new Vector3((hl - hr) * forca, (hd - hu) * forca, 1f).normalized;
                    px[y * tamanho + x] = new Color(n.x * 0.5f + 0.5f, n.y * 0.5f + 0.5f, n.z * 0.5f + 0.5f, 1f);
                }
            return Salvar(nome, px, tamanho, true);
        }

        // ------------------------------------------------------------------ texturas

        /// <summary>Asfalto gasto: grão, pedrinhas, manchas de óleo, remendos e rachaduras. 1 textura = 4 m.</summary>
        public static void Asfalto(out Texture2D cor, out Texture2D relevo)
        {
            const int n = 512;
            System.Random r = new System.Random(7);
            Color[] px = new Color[n * n];
            float[] h = new float[n * n];
            // Remendos (retângulos mais escuros/lisos)
            var remendos = new RectInt[5];
            for (int i = 0; i < remendos.Length; i++)
                remendos[i] = new RectInt(r.Next(n), r.Next(n), 40 + r.Next(120), 30 + r.Next(90));
            // Rachaduras: caminhos aleatórios
            bool[] racha = new bool[n * n];
            for (int c = 0; c < 9; c++)
            {
                float x = r.Next(n), y = r.Next(n), ang = (float)(r.NextDouble() * Mathf.PI * 2);
                int passos = 60 + r.Next(220);
                for (int p = 0; p < passos; p++)
                {
                    ang += (float)(r.NextDouble() - 0.5) * 0.8f;
                    x += Mathf.Cos(ang);
                    y += Mathf.Sin(ang);
                    int ix = ((int)x % n + n) % n, iy = ((int)y % n + n) % n;
                    racha[iy * n + ix] = true;
                    if (r.NextDouble() < 0.4) racha[iy * n + (ix + 1) % n] = true;
                }
            }
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float grande = RuidoRepetido(x, y, n, 4, 3f, 1);
                    float grao = (float)r.NextDouble();
                    float pedra = grao > 0.93f ? 0.12f : grao < 0.05f ? -0.06f : 0f;
                    float v = 0.2f + grande * 0.09f + (grao - 0.5f) * 0.05f + pedra;
                    float altura = 0.5f + (grao - 0.5f) * 0.6f + pedra * 2f;
                    bool emRemendo = false;
                    foreach (RectInt rr in remendos)
                        if (x >= rr.xMin && x < rr.xMax && y >= rr.yMin && y < rr.yMax) emRemendo = true;
                    if (emRemendo) { v = v * 0.9f + 0.012f; altura = 0.5f + (grao - 0.5f) * 0.25f; }
                    // Mancha de óleo
                    float oleo = RuidoRepetido(x, y, n, 3, 6f, 5);
                    if (oleo > 0.68f) v *= Mathf.Lerp(1f, 0.7f, (oleo - 0.68f) / 0.2f);
                    if (racha[y * n + x]) { v *= 0.45f; altura = 0.1f; }
                    px[y * n + x] = new Color(v * 0.98f, v, v * 1.03f, 1f);
                    h[y * n + x] = altura;
                }
            cor = Salvar("Asfalto", px, n);
            relevo = Relevo("Asfalto_Relevo", h, n, 2.5f);
        }

        /// <summary>Calçada de placas de concreto (60 cm) com juntas, manchas e uma ou outra placa quebrada. 1 textura = 2,4 m.</summary>
        public static void Calcada(out Texture2D cor, out Texture2D relevo)
        {
            const int n = 512;
            const int placa = n / 4;
            System.Random r = new System.Random(11);
            Color[] px = new Color[n * n];
            float[] h = new float[n * n];
            float[] tomPlaca = new float[16];
            for (int i = 0; i < 16; i++) tomPlaca[i] = (float)(r.NextDouble() - 0.5) * 0.08f;
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    int px_ = x % placa, py = y % placa;
                    bool junta = px_ < 3 || py < 3;
                    int indice = (y / placa) * 4 + x / placa;
                    float ruido = RuidoRepetido(x, y, n, 5, 6f, 2);
                    float v = 0.62f + tomPlaca[indice] + (ruido - 0.5f) * 0.12f + (float)(r.NextDouble() - 0.5) * 0.04f;
                    float altura = 0.6f + (ruido - 0.5f) * 0.3f;
                    if (junta) { v *= 0.55f; altura = 0.15f; }
                    float sujeira = RuidoRepetido(x, y, n, 3, 2f, 9);
                    if (sujeira > 0.62f) v *= 0.85f;
                    px[y * n + x] = new Color(v * 1.02f, v, v * 0.95f, 1f);
                    h[y * n + x] = altura;
                }
            cor = Salvar("Calcada", px, n);
            relevo = Relevo("Calcada_Relevo", h, n, 3f);
        }

        /// <summary>Reboco pintado (cinza claro, a cor vem do material) com textura de massa e sujeira escorrida embaixo. 1 textura = 3 m.</summary>
        public static void Reboco(out Texture2D cor, out Texture2D relevo)
        {
            const int n = 512;
            System.Random r = new System.Random(3);
            Color[] px = new Color[n * n];
            float[] h = new float[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float massa = RuidoRepetido(x, y, n, 5, 10f, 4);
                    float manchas = RuidoRepetido(x, y, n, 3, 2.5f, 8);
                    float v = 0.88f + (massa - 0.5f) * 0.12f + (float)(r.NextDouble() - 0.5) * 0.03f;
                    if (manchas > 0.6f) v -= (manchas - 0.6f) * 0.35f;
                    // Sujeira de chuva: mais escuro perto do chão (embaixo da textura) e escorrido
                    float baixo = 1f - y / (float)n;
                    float escorrido = Mathf.PerlinNoise(x * 0.08f, 0.5f);
                    v -= Mathf.Pow(Mathf.Clamp01(baixo - 0.6f) * 2.5f, 2f) * 0.25f * (0.6f + escorrido * 0.6f);
                    px[y * n + x] = new Color(v, v * 0.98f, v * 0.95f, 1f);
                    h[y * n + x] = massa;
                }
            cor = Salvar("Reboco", px, n);
            relevo = Relevo("Reboco_Relevo", h, n, 4f);
        }

        /// <summary>Tijolo baiano (bloco cerâmico laranja) com argamassa escorrida. 1 textura = 2 m.</summary>
        public static void Tijolo(out Texture2D cor, out Texture2D relevo)
        {
            const int n = 512;
            System.Random r = new System.Random(5);
            Color[] px = new Color[n * n];
            float[] h = new float[n * n];
            const int alturaBloco = 48, larguraBloco = 96;
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    int fileira = y / alturaBloco;
                    int xx = (x + (fileira % 2) * larguraBloco / 2) % n;
                    bool argamassa = (y % alturaBloco) < 6 || (xx % larguraBloco) < 6;
                    int bloco = fileira * 10 + xx / larguraBloco;
                    float tom = Mathf.PerlinNoise(bloco * 0.73f, 0.3f) - 0.5f;
                    float ruido = RuidoRepetido(x, y, n, 4, 12f, 6);
                    Color c = new Color(0.72f + tom * 0.15f, 0.36f + tom * 0.08f, 0.2f) * (0.85f + ruido * 0.3f);
                    float altura = 0.7f + ruido * 0.2f;
                    if (argamassa)
                    {
                        float v = 0.62f + (float)(r.NextDouble() - 0.5) * 0.08f;
                        c = new Color(v, v * 0.97f, v * 0.9f);
                        altura = 0.2f;
                    }
                    px[y * n + x] = new Color(c.r, c.g, c.b, 1f);
                    h[y * n + x] = altura;
                }
            cor = Salvar("Tijolo", px, n);
            relevo = Relevo("Tijolo_Relevo", h, n, 4f);
        }

        /// <summary>Portão de enrolar (lâminas horizontais de metal pintado, um pouco enferrujado). 1 textura = 1 m.</summary>
        public static void PortaoEnrolar(out Texture2D cor, out Texture2D relevo)
        {
            const int n = 256;
            Color[] px = new Color[n * n];
            float[] h = new float[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float lamina = (y % 16) / 16f;
                    float perfil = Mathf.Sin(lamina * Mathf.PI);
                    float ferrugem = RuidoRepetido(x, y, n, 4, 5f, 12);
                    float v = 0.7f + perfil * 0.25f;
                    Color c = new Color(v, v, v);
                    if (ferrugem > 0.66f) c = Color.Lerp(c, new Color(0.55f, 0.3f, 0.15f), (ferrugem - 0.66f) * 2.5f);
                    px[y * n + x] = new Color(c.r, c.g, c.b, 1f);
                    h[y * n + x] = perfil;
                }
            cor = Salvar("PortaoEnrolar", px, n);
            relevo = Relevo("PortaoEnrolar_Relevo", h, n, 6f);
        }

        /// <summary>Telha cerâmica (ondas) para os telhados. 1 textura = 2 m.</summary>
        public static void Telha(out Texture2D cor)
        {
            const int n = 256;
            Color[] px = new Color[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float onda = Mathf.Abs(Mathf.Sin(x / (float)n * Mathf.PI * 10f));
                    float fileira = (y % 32) / 32f;
                    float ruido = RuidoRepetido(x, y, n, 3, 6f, 14);
                    float v = 0.55f + onda * 0.3f - fileira * 0.15f + (ruido - 0.5f) * 0.2f;
                    px[y * n + x] = new Color(0.78f * v + 0.1f, 0.38f * v + 0.05f, 0.24f * v + 0.03f, 1f);
                }
            cor = Salvar("Telha", px, n);
        }

        /// <summary>Giz: branco com falhas (transparente onde o giz não pegou), para as marcas no asfalto.</summary>
        public static Texture2D Giz()
        {
            const int n = 256;
            System.Random r = new System.Random(21);
            Color[] px = new Color[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float riscos = Mathf.PerlinNoise(x * 0.9f, y * 0.06f);
                    float falha = RuidoRepetido(x, y, n, 4, 8f, 22);
                    float a = Mathf.Clamp01((riscos * 0.55f + falha * 0.6f + (float)r.NextDouble() * 0.25f) - 0.35f) * 1.6f;
                    float v = 0.92f + (float)r.NextDouble() * 0.08f;
                    px[y * n + x] = new Color(v, v, v * 0.98f, Mathf.Clamp01(a));
                }
            return Salvar("Giz", px, n, false, true);
        }

        /// <summary>
        /// Pichação/grafite: 3 a 5 "letras" feitas de traços grossos (como tag de muro), com
        /// preenchimento colorido em degradê, contorno escuro e um brilho branco; fundo transparente.
        /// 'semente' muda as letras e as cores.
        /// </summary>
        public static Texture2D Pichacao(int semente)
        {
            const int n = 256;
            System.Random r = new System.Random(100 + semente);
            Color[] paleta =
            {
                new Color(0.95f, 0.25f, 0.45f), new Color(0.15f, 0.75f, 0.95f), new Color(1f, 0.82f, 0.15f),
                new Color(0.35f, 0.9f, 0.4f), new Color(0.65f, 0.3f, 0.95f), new Color(1f, 0.5f, 0.15f), new Color(0.1f, 0.1f, 0.12f)
            };
            Color corA = paleta[r.Next(paleta.Length)], corB = paleta[r.Next(paleta.Length)];
            bool soPreto = semente % 3 == 2; // pichação de spray preto, fina, sem contorno
            if (soPreto) corA = corB = new Color(0.08f, 0.08f, 0.1f);
            // Traços das letras: cada letra ocupa uma "caixa" e tem de 2 a 4 traços
            var tracos = new System.Collections.Generic.List<Vector4>();
            int letras = 3 + r.Next(3);
            float largura = (n - 40f) / letras;
            for (int l = 0; l < letras; l++)
            {
                float x0 = 20f + l * largura, y0 = n * 0.3f + r.Next(-12, 12), y1 = n * 0.72f + r.Next(-12, 12);
                int nTracos = 2 + r.Next(3);
                Vector2 p = new Vector2(x0 + (float)r.NextDouble() * largura * 0.3f, y0);
                for (int k = 0; k < nTracos; k++)
                {
                    Vector2 q = new Vector2(x0 + (float)r.NextDouble() * largura * 0.85f, k % 2 == 0 ? y1 : y0 + (float)r.NextDouble() * 20f);
                    tracos.Add(new Vector4(p.x, p.y, q.x, q.y));
                    p = q;
                }
            }
            float grossura = soPreto ? 5f : 11f + r.Next(5);
            Color[] px = new Color[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float d = float.MaxValue;
                    Vector2 ponto = new Vector2(x, y);
                    foreach (Vector4 t in tracos)
                    {
                        Vector2 a = new Vector2(t.x, t.y), b = new Vector2(t.z, t.w);
                        Vector2 ab = b - a;
                        float u = Mathf.Clamp01(Vector2.Dot(ponto - a, ab) / Mathf.Max(0.001f, ab.sqrMagnitude));
                        d = Mathf.Min(d, Vector2.Distance(ponto, a + ab * u));
                    }
                    Color c = new Color(0, 0, 0, 0);
                    if (d < grossura)
                    {
                        c = Color.Lerp(corA, corB, y / (float)n) * (0.9f + Mathf.PerlinNoise(x * 0.05f, y * 0.05f) * 0.2f);
                        if (!soPreto && d < grossura * 0.35f && Mathf.PerlinNoise(x * 0.08f + semente, y * 0.08f) > 0.62f) c = Color.white;
                        c.a = 0.95f;
                    }
                    else if (!soPreto && d < grossura + 4f) c = new Color(0.06f, 0.06f, 0.08f, 0.95f);
                    px[y * n + x] = c;
                }
            return Salvar($"Pichacao{semente}", px, n, false, true);
        }

        /// <summary>Folhagem (verde com variação, para as copas das árvores e os matinhos).</summary>
        public static Texture2D Folhagem()
        {
            const int n = 256;
            System.Random r = new System.Random(31);
            Color[] px = new Color[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float f = RuidoRepetido(x, y, n, 5, 14f, 32);
                    float v = 0.55f + f * 0.6f + (float)(r.NextDouble() - 0.5) * 0.15f;
                    px[y * n + x] = new Color(0.25f * v, 0.5f * v, 0.18f * v, 1f);
                }
            return Salvar("Folhagem", px, n);
        }

        /// <summary>Terra batida com tufos de grama (fora da rua, atrás do muro baixo).</summary>
        public static Texture2D Terra()
        {
            const int n = 512;
            System.Random r = new System.Random(41);
            Color[] px = new Color[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float f = RuidoRepetido(x, y, n, 5, 5f, 42);
                    float g = RuidoRepetido(x, y, n, 4, 9f, 43);
                    Color terra = new Color(0.45f, 0.33f, 0.22f) * (0.8f + f * 0.35f);
                    Color grama = new Color(0.3f, 0.45f, 0.18f) * (0.75f + f * 0.4f);
                    Color c = Color.Lerp(terra, grama, Mathf.SmoothStep(0.45f, 0.65f, g));
                    c *= 0.92f + (float)r.NextDouble() * 0.12f;
                    px[y * n + x] = new Color(c.r, c.g, c.b, 1f);
                }
            return Salvar("Terra", px, n);
        }
    }
}
