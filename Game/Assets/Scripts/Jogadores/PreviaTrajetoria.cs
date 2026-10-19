using System.Collections.Generic;
using UnityEngine;

namespace BetsDeRua
{
    /// <summary>
    /// Prévia do arremesso para os testes do protótipo: o caminho inteiro da bola no ar
    /// (mesma física da bola de verdade, sem a curva do analógico) e uma marca em cada
    /// lugar onde ela vai quicar. Liga e desliga no ConfigHUD (mostrarTrajetoriaCompleta).
    /// Dividida entre os defensores: só quem está com a bola usa.
    /// </summary>
    public class PreviaTrajetoria : MonoBehaviour
    {
        [SerializeField] private ConfigFisica configFisica;
        [SerializeField] private ConfigHUD configHUD;
        [SerializeField] private Material material;

        private const int MaximoQuiques = 3;
        private const float AlturaMarca = 0.015f;

        private LineRenderer linha;
        private readonly List<Vector3> pontos = new List<Vector3>();
        private readonly Transform[] marcasQuique = new Transform[MaximoQuiques];

        private void Awake()
        {
            linha = gameObject.AddComponent<LineRenderer>();
            linha.sharedMaterial = material;
            linha.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            linha.useWorldSpace = true;
            linha.numCapVertices = 2;
            for (int i = 0; i < MaximoQuiques; i++)
            {
                GameObject marca = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                marca.name = $"Quique{i + 1}";
                Destroy(marca.GetComponent<Collider>());
                marca.transform.SetParent(transform, false);
                marca.GetComponent<MeshRenderer>().sharedMaterial = material;
                marca.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                marcasQuique[i] = marca.transform;
            }
            Esconder();
        }

        /// <summary>Desenha o caminho da bola saindo de 'saida' com 'velocidade' (m/s).</summary>
        public void Mostrar(Vector3 saida, Vector3 velocidade)
        {
            if (!configHUD.mostrarTrajetoriaCompleta) { Esconder(); return; }

            pontos.Clear();
            Vector3 p = saida, v = velocidade;
            bool parada = false;
            int quiques = 0;
            float passo = Mathf.Max(0.0005f, configFisica.passoSimulacao);
            float proximoPonto = 0f;
            pontos.Add(p);
            for (float t = 0f; t < configHUD.duracaoPrevia && !parada; t += passo)
            {
                float vyAntes = v.y;
                FisicaBola.Passo(configFisica, ref p, ref v, ref parada, passo);
                // Quique: vinha descendo e passou a subir, encostada no chão
                if (vyAntes < 0f && v.y > 0f && p.y <= configFisica.raioBola + 0.05f && quiques < MaximoQuiques)
                {
                    Transform marca = marcasQuique[quiques++];
                    marca.gameObject.SetActive(true);
                    marca.position = new Vector3(p.x, AlturaMarca, p.z);
                    marca.localScale = new Vector3(configHUD.tamanhoMarcaQuique, 0.002f, configHUD.tamanhoMarcaQuique);
                }
                if (t >= proximoPonto)
                {
                    pontos.Add(p);
                    proximoPonto += configHUD.intervaloPontosPrevia;
                }
            }
            for (int i = quiques; i < MaximoQuiques; i++) marcasQuique[i].gameObject.SetActive(false);

            linha.enabled = true;
            linha.startWidth = linha.endWidth = configHUD.larguraTraco;
            linha.positionCount = pontos.Count;
            linha.SetPositions(pontos.ToArray());
        }

        public void Esconder()
        {
            if (linha != null) linha.enabled = false;
            foreach (Transform marca in marcasQuique)
                if (marca != null) marca.gameObject.SetActive(false);
        }
    }
}
