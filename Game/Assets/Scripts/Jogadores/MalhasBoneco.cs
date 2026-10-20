using System.Collections.Generic;
using UnityEngine;

namespace BetsDeRua
{
    /// <summary>
    /// Formas arredondadas geradas por código para o boneco e o taco (sem modelo 3D de fora):
    /// "torno" (gira um perfil em volta do eixo Y, como um vaso no torno), esfera achatada,
    /// calota (pedaço de esfera, para cabelo e boné), anel e caixa arredondada.
    /// As malhas ficam guardadas: o mesmo formato é gerado uma vez só.
    /// </summary>
    public static class MalhasBoneco
    {
        private const int Lados = 18;
        private static readonly Dictionary<string, Mesh> guardadas = new Dictionary<string, Mesh>();

        /// <summary>
        /// Gira o perfil (pares y, raio) em volta do eixo Y. Raio 0 nas pontas fecha a forma.
        /// escalaX / escalaZ deixam o corte oval (ex.: tronco mais largo que fundo).
        /// </summary>
        public static Mesh Torno(string chave, IList<Vector2> perfil, float escalaX = 1f, float escalaZ = 1f, int lados = Lados)
        {
            string id = $"torno:{chave}:{escalaX:0.###}:{escalaZ:0.###}:{lados}";
            if (guardadas.TryGetValue(id, out Mesh pronta) && pronta != null) return pronta;

            var vertices = new List<Vector3>();
            var triangulos = new List<int>();
            for (int i = 0; i < perfil.Count; i++)
            {
                for (int j = 0; j < lados; j++)
                {
                    float a = j * Mathf.PI * 2f / lados;
                    vertices.Add(new Vector3(Mathf.Cos(a) * perfil[i].y * escalaX, perfil[i].x, Mathf.Sin(a) * perfil[i].y * escalaZ));
                }
            }
            for (int i = 0; i < perfil.Count - 1; i++)
            {
                for (int j = 0; j < lados; j++)
                {
                    int a = i * lados + j, b = i * lados + (j + 1) % lados;
                    int c = a + lados, d = b + lados;
                    // perfil de baixo para cima: virado para fora
                    bool subindo = perfil[i + 1].x >= perfil[i].x;
                    if (subindo) { triangulos.Add(a); triangulos.Add(c); triangulos.Add(b); triangulos.Add(b); triangulos.Add(c); triangulos.Add(d); }
                    else { triangulos.Add(a); triangulos.Add(b); triangulos.Add(c); triangulos.Add(b); triangulos.Add(d); triangulos.Add(c); }
                }
            }
            Mesh malha = new Mesh { name = id };
            malha.SetVertices(vertices);
            malha.SetTriangles(triangulos, 0);
            malha.RecalculateNormals();
            malha.RecalculateBounds();
            guardadas[id] = malha;
            return malha;
        }

        /// <summary>Esfera (raio 1) de -90 a +90 graus de latitude; use a escala do objeto para achatar.</summary>
        public static Mesh Esfera() => Calota(-90f, 90f);

        /// <summary>
        /// Pedaço de esfera de raio 1 entre duas latitudes (graus; 90 = topo). Ex.: Calota(10, 90) é
        /// um boné/cabelo que vai do topo até um pouco acima do meio.
        /// </summary>
        public static Mesh Calota(float latitudeDeBaixo, float latitudeDeCima, int aneis = 12)
        {
            var perfil = new List<Vector2>();
            for (int i = 0; i <= aneis; i++)
            {
                float lat = Mathf.Lerp(latitudeDeBaixo, latitudeDeCima, i / (float)aneis) * Mathf.Deg2Rad;
                perfil.Add(new Vector2(Mathf.Sin(lat), Mathf.Cos(lat)));
            }
            // Fecha embaixo (se não chegou no polo) com um "fundo" um pouco para dentro
            if (latitudeDeBaixo > -89f) perfil.Insert(0, new Vector2(perfil[0].x - 0.02f, 0f));
            return Torno($"calota:{latitudeDeBaixo:0}:{latitudeDeCima:0}", perfil);
        }

        /// <summary>
        /// Membro arredondado pendurado para baixo (de y=0 até y=-comprimento), mais grosso em
        /// cima (raioCima) e mais fino embaixo (raioBaixo), com as pontas fechadas em meia esfera.
        /// </summary>
        public static Mesh Membro(float comprimento, float raioCima, float raioBaixo, float escalaZ = 1f)
        {
            var perfil = new List<Vector2>();
            const int pontaPassos = 4;
            // Ponta de baixo (meia esfera)
            for (int i = 0; i <= pontaPassos; i++)
            {
                float a = (-90f + 90f * i / pontaPassos) * Mathf.Deg2Rad;
                perfil.Add(new Vector2(-comprimento + raioBaixo + Mathf.Sin(a) * raioBaixo, Mathf.Cos(a) * raioBaixo));
            }
            // Músculo: um pouco mais cheio no terço de cima
            perfil.Add(new Vector2(-comprimento * 0.55f, Mathf.Lerp(raioBaixo, raioCima, 0.65f) * 1.04f));
            perfil.Add(new Vector2(-comprimento * 0.3f, raioCima * 1.03f));
            // Ponta de cima
            for (int i = 0; i <= pontaPassos; i++)
            {
                float a = (90f * i / pontaPassos) * Mathf.Deg2Rad;
                perfil.Add(new Vector2(-raioCima + Mathf.Sin(a) * raioCima, Mathf.Cos(a) * raioCima));
            }
            return Torno($"membro:{comprimento:0.###}:{raioCima:0.###}:{raioBaixo:0.###}", perfil, 1f, escalaZ);
        }

        /// <summary>Anel (toro) deitado no plano XZ.</summary>
        public static Mesh Anel(float raio, float grossura, int lados = 20, int voltas = 8)
        {
            string id = $"anel:{raio:0.###}:{grossura:0.###}";
            if (guardadas.TryGetValue(id, out Mesh pronta) && pronta != null) return pronta;
            var vertices = new List<Vector3>();
            var triangulos = new List<int>();
            for (int i = 0; i < lados; i++)
            {
                float a = i * Mathf.PI * 2f / lados;
                Vector3 centro = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * raio;
                for (int j = 0; j < voltas; j++)
                {
                    float b = j * Mathf.PI * 2f / voltas;
                    vertices.Add(centro + (centro.normalized * Mathf.Cos(b) + Vector3.up * Mathf.Sin(b)) * grossura);
                }
            }
            for (int i = 0; i < lados; i++)
                for (int j = 0; j < voltas; j++)
                {
                    int a = i * voltas + j, b = ((i + 1) % lados) * voltas + j;
                    int c = i * voltas + (j + 1) % voltas, d = ((i + 1) % lados) * voltas + (j + 1) % voltas;
                    triangulos.Add(a); triangulos.Add(c); triangulos.Add(b);
                    triangulos.Add(b); triangulos.Add(c); triangulos.Add(d);
                }
            Mesh malha = new Mesh { name = id };
            malha.SetVertices(vertices);
            malha.SetTriangles(triangulos, 0);
            malha.RecalculateNormals();
            guardadas[id] = malha;
            return malha;
        }

        /// <summary>
        /// Peça chata: um contorno convexo (x = largura, y = comprimento) extrudado com 'espessura'
        /// no eixo Z, com frente, verso e laterais (a pá do taco).
        /// </summary>
        public static Mesh Extrusao(string chave, IList<Vector2> contorno, float espessura)
        {
            string id = $"extrusao:{chave}:{espessura:0.###}";
            if (guardadas.TryGetValue(id, out Mesh pronta) && pronta != null) return pronta;
            var v = new List<Vector3>();
            var tri = new List<int>();
            int n = contorno.Count;
            float e = espessura * 0.5f;
            Vector2 centro = Vector2.zero;
            foreach (Vector2 p in contorno) centro += p;
            centro /= n;
            foreach (float lado in new[] { e, -e })
            {
                int b = v.Count;
                v.Add(new Vector3(centro.x, centro.y, lado));
                foreach (Vector2 p in contorno) v.Add(new Vector3(p.x, p.y, lado));
                for (int i = 0; i < n; i++)
                {
                    int a1 = b + 1 + i, a2 = b + 1 + (i + 1) % n;
                    if (lado > 0f) { tri.Add(b); tri.Add(a1); tri.Add(a2); }
                    else { tri.Add(b); tri.Add(a2); tri.Add(a1); }
                }
            }
            // Laterais (vértices próprios para a quina ficar marcada)
            for (int i = 0; i < n; i++)
            {
                Vector2 p1 = contorno[i], p2 = contorno[(i + 1) % n];
                int b = v.Count;
                v.Add(new Vector3(p1.x, p1.y, e)); v.Add(new Vector3(p2.x, p2.y, e));
                v.Add(new Vector3(p2.x, p2.y, -e)); v.Add(new Vector3(p1.x, p1.y, -e));
                tri.Add(b); tri.Add(b + 2); tri.Add(b + 1);
                tri.Add(b); tri.Add(b + 3); tri.Add(b + 2);
            }
            Mesh m = new Mesh { name = id };
            m.SetVertices(v);
            m.SetTriangles(tri, 0);
            m.RecalculateNormals();
            m.RecalculateBounds();
            guardadas[id] = m;
            return m;
        }

        /// <summary>Disco fino (aba do chapéu): anel cheio entre raioDentro e raioFora, com espessura.</summary>
        public static Mesh Aba(float raioDentro, float raioFora, float espessura)
        {
            var perfil = new List<Vector2>
            {
                new Vector2(-espessura * 0.5f, raioDentro),
                new Vector2(-espessura * 0.5f, raioFora),
                new Vector2(0f, raioFora + espessura * 0.4f),
                new Vector2(espessura * 0.5f, raioFora),
                new Vector2(espessura * 0.5f, raioDentro),
            };
            return Torno($"aba:{raioDentro:0.###}:{raioFora:0.###}:{espessura:0.###}", perfil, 1f, 1f, 28);
        }
    }
}
