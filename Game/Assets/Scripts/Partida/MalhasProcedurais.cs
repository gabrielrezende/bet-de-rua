using System.Collections.Generic;
using UnityEngine;

namespace BetsDeRua
{
    /// <summary>
    /// Formas simples geradas por código para marcas na tela e no chão:
    /// anel deitado no chão, pedaço de anel de pé (arco de força) e linha tracejada no chão.
    /// </summary>
    public static class MalhasProcedurais
    {
        private const int SegmentosAnelCompleto = 48;

        /// <summary>Anel deitado no chão (plano XZ), centrado na origem.</summary>
        public static Mesh AnelNoChao(float raio, float espessura, Mesh reaproveitar = null)
        {
            float dentro = Mathf.Max(0f, raio - espessura * 0.5f);
            float fora = raio + espessura * 0.5f;
            var vertices = new List<Vector3>();
            var triangulos = new List<int>();
            for (int i = 0; i <= SegmentosAnelCompleto; i++)
            {
                float angulo = i * Mathf.PI * 2f / SegmentosAnelCompleto;
                Vector3 direcao = new Vector3(Mathf.Cos(angulo), 0f, Mathf.Sin(angulo));
                vertices.Add(direcao * dentro);
                vertices.Add(direcao * fora);
                if (i > 0) AdicionarQuad(triangulos, vertices.Count - 4);
            }
            return Preencher(reaproveitar, "AnelNoChao", vertices, triangulos);
        }

        /// <summary>
        /// Pedaço de anel de pé (plano XY), como um velocímetro. Ângulos em graus,
        /// medidos como no relógio de matemática (0 = direita, 90 = em cima).
        /// </summary>
        public static Mesh SetorDeAnel(float raioDentro, float raioFora, float anguloInicio, float anguloFim,
                                       Mesh reaproveitar = null)
        {
            var vertices = new List<Vector3>();
            var triangulos = new List<int>();
            float abertura = Mathf.Abs(anguloFim - anguloInicio);
            int segmentos = Mathf.Max(1, Mathf.CeilToInt(abertura / 360f * SegmentosAnelCompleto));
            for (int i = 0; i <= segmentos; i++)
            {
                float angulo = Mathf.Lerp(anguloInicio, anguloFim, (float)i / segmentos) * Mathf.Deg2Rad;
                Vector3 direcao = new Vector3(Mathf.Cos(angulo), Mathf.Sin(angulo), 0f);
                vertices.Add(direcao * raioDentro);
                vertices.Add(direcao * raioFora);
                if (i > 0) AdicionarQuad(triangulos, vertices.Count - 4);
            }
            return Preencher(reaproveitar, "SetorDeAnel", vertices, triangulos);
        }

        /// <summary>Linha tracejada no chão de 'inicio' até 'fim' (coordenadas do mundo, y ignorado).</summary>
        public static Mesh LinhaTracejada(Vector3 inicio, Vector3 fim, float comprimentoTraco, float espaco,
                                          float largura, float alturaChao, Mesh reaproveitar = null)
        {
            var vertices = new List<Vector3>();
            var triangulos = new List<int>();
            Vector3 trecho = fim - inicio;
            trecho.y = 0f;
            float total = trecho.magnitude;
            if (total > 0.01f && comprimentoTraco > 0.01f)
            {
                Vector3 direcao = trecho / total;
                Vector3 lado = Vector3.Cross(Vector3.up, direcao) * (largura * 0.5f);
                Vector3 baseChao = new Vector3(inicio.x, alturaChao, inicio.z);
                for (float d = 0f; d < total; d += comprimentoTraco + Mathf.Max(0.01f, espaco))
                {
                    Vector3 a = baseChao + direcao * d;
                    Vector3 b = baseChao + direcao * Mathf.Min(total, d + comprimentoTraco);
                    vertices.Add(a - lado);
                    vertices.Add(a + lado);
                    vertices.Add(b - lado);
                    vertices.Add(b + lado);
                    AdicionarQuad(triangulos, vertices.Count - 4);
                }
            }
            return Preencher(reaproveitar, "LinhaTracejada", vertices, triangulos);
        }

        /// <summary>Disco deitado no chão (plano XZ), raio 0,5 (diâmetro 1): a escala dá o tamanho.</summary>
        public static Mesh DiscoNoChao(Mesh reaproveitar = null)
        {
            var vertices = new List<Vector3> { Vector3.zero };
            var triangulos = new List<int>();
            for (int i = 0; i <= SegmentosAnelCompleto; i++)
            {
                float angulo = i * Mathf.PI * 2f / SegmentosAnelCompleto;
                vertices.Add(new Vector3(Mathf.Cos(angulo), 0f, Mathf.Sin(angulo)) * 0.5f);
                if (i > 0)
                {
                    triangulos.Add(0);
                    triangulos.Add(i + 1);
                    triangulos.Add(i);
                }
            }
            return Preencher(reaproveitar, "DiscoNoChao", vertices, triangulos);
        }

        /// <summary>"X" deitado no chão (plano XZ), centrado na origem.</summary>
        public static Mesh XisNoChao(float tamanho, float espessura, Mesh reaproveitar = null)
        {
            var vertices = new List<Vector3>();
            var triangulos = new List<int>();
            float meio = tamanho * 0.5f;
            foreach (Vector3 eixo in new[] { new Vector3(1f, 0f, 1f).normalized, new Vector3(1f, 0f, -1f).normalized })
            {
                Vector3 lado = Vector3.Cross(Vector3.up, eixo) * (espessura * 0.5f);
                vertices.Add(-eixo * meio - lado);
                vertices.Add(-eixo * meio + lado);
                vertices.Add(eixo * meio - lado);
                vertices.Add(eixo * meio + lado);
                AdicionarQuad(triangulos, vertices.Count - 4);
            }
            return Preencher(reaproveitar, "XisNoChao", vertices, triangulos);
        }

        /// <summary>Dois triângulos ligando os 4 vértices a partir de 'primeiro' (pares dentro/fora).</summary>
        private static void AdicionarQuad(List<int> triangulos, int primeiro)
        {
            triangulos.Add(primeiro);
            triangulos.Add(primeiro + 2);
            triangulos.Add(primeiro + 1);
            triangulos.Add(primeiro + 1);
            triangulos.Add(primeiro + 2);
            triangulos.Add(primeiro + 3);
        }

        private static Mesh Preencher(Mesh malha, string nome, List<Vector3> vertices, List<int> triangulos)
        {
            if (malha == null) malha = new Mesh { name = nome };
            malha.Clear();
            malha.SetVertices(vertices);
            malha.SetTriangles(triangulos, 0);
            malha.RecalculateNormals();
            malha.RecalculateBounds();
            return malha;
        }
    }
}
