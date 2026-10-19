using UnityEngine;

namespace BetsDeRua
{
    /// <summary>
    /// Sombra da bola no chão, sempre visível (docs/REFERENCIAS_FIFA.md, 9.1): mostra a
    /// profundidade melhor que a própria bola na vista lateral. Fica embaixo da bola e
    /// diminui um pouco quanto mais alto ela está.
    /// </summary>
    public class SombraBola : MonoBehaviour
    {
        [SerializeField] private ConfigFisica configFisica;
        [SerializeField] private ConfigHUD configHUD;
        [SerializeField] private Bola bola;

        private const float AlturaSombra = 0.008f;
        private const float TamanhoMinimo = 0.5f; // fração do tamanho cheio, bem no alto

        private void Awake()
        {
            GetComponent<MeshFilter>().sharedMesh = MalhasProcedurais.DiscoNoChao();
        }

        private void LateUpdate()
        {
            Vector3 p = bola.Posicao;
            float altura = Mathf.Max(0f, p.y - configFisica.raioBola);
            float fracao = Mathf.Lerp(1f, TamanhoMinimo, altura / Mathf.Max(0.1f, configHUD.alturaSombraMinima));
            float diametro = configFisica.raioBola * configFisica.escalaVisualBola * 2f * configHUD.escalaSombraBola * fracao;
            transform.SetPositionAndRotation(new Vector3(p.x, AlturaSombra, p.z), Quaternion.identity);
            transform.localScale = new Vector3(diametro, 1f, diametro);
        }
    }
}
