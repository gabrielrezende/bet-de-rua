using UnityEditor;
using UnityEngine;

namespace BetsDeRua.Editor
{
    /// <summary>
    /// Na importação das pessoas do MakeHuman (Assets/Personagens/MakeHuman), afasta a camiseta do corpo
    /// alguns milímetros (empurra cada vértice na direção da normal). A bermuda ou a calça, que ficam
    /// quase no mesmo lugar na cintura, vazavam por cima da camiseta e apareciam como manchas pretas
    /// na barra e nas costas. Feito uma vez só, na importação (nada roda no jogo).
    /// </summary>
    public class PosProcessadorPessoas : AssetPostprocessor
    {
        private const string Pasta = "Assets/Personagens/MakeHuman/";
        /// <summary>Folga da camiseta, em fração da altura da peça (0,02 de uma camiseta de 60 cm ≈ 1,2 cm).</summary>
        private const float FolgaCamisa = 0.02f;

        // Mudar este número faz o Unity reimportar os modelos com a regra nova
        public override uint GetVersion() => 2;

        private void OnPostprocessModel(GameObject modelo)
        {
            if (!assetPath.StartsWith(Pasta)) return;
            foreach (SkinnedMeshRenderer r in modelo.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (!r.name.ToLowerInvariant().StartsWith("camisa") || r.sharedMesh == null) continue;
                Mesh malha = r.sharedMesh;
                Vector3[] vertices = malha.vertices;
                Vector3[] normais = malha.normals;
                if (normais == null || normais.Length != vertices.Length) continue;
                float folga = malha.bounds.size.y * FolgaCamisa;
                for (int i = 0; i < vertices.Length; i++) vertices[i] += normais[i] * folga;
                malha.vertices = vertices;
                malha.RecalculateBounds();
            }
        }
    }
}
