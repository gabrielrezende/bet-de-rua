using UnityEngine;

namespace BetsDeRua
{
    /// <summary>
    /// Ajudas para ler o tempo da batida na vista lateral (docs/REFERENCIAS_FIFA.md, 9.1):
    /// - raio de alcance do taco: círculo fino em volta do rebatedor;
    /// - "X" amarelo no chão onde a bola vai cruzar a linha do rebatedor (amarelo, e não
    ///   giz branco, porque ele cai em cima do círculo branco da base);
    /// - anel de tempo: encolhe em volta da bola e fecha no momento perfeito.
    /// </summary>
    public class AjudasBatida : MonoBehaviour
    {
        [SerializeField] private ConfigHUD configHUD;
        [SerializeField] private ConfigFisica configFisica;
        [SerializeField] private Material materialGiz;
        [SerializeField] private Material materialAnel;

        // Alturas pequenas para as marcas não "piscarem" com o chão
        private const float AlturaAlcance = 0.012f;
        private const float AlturaMarca = 0.016f;

        private MeshFilter alcance;
        private MeshFilter marca;
        private MeshFilter anel;
        private Mesh malhaAlcance, malhaMarca, malhaAnel;

        private void Awake()
        {
            alcance = CriarFilho("RaioAlcance", materialGiz);
            marca = CriarFilho("MarcaPassagem", materialAnel);
            anel = CriarFilho("AnelTempo", materialAnel);
            EsconderTudo();
        }

        private MeshFilter CriarFilho(string nome, Material material)
        {
            GameObject filho = new GameObject(nome);
            filho.transform.SetParent(transform, false);
            MeshRenderer desenho = filho.AddComponent<MeshRenderer>();
            desenho.sharedMaterial = material;
            desenho.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return filho.AddComponent<MeshFilter>();
        }

        /// <summary>Círculo de alcance do taco em volta dos pés do rebatedor.</summary>
        public void MostrarAlcance(Vector3 pes)
        {
            alcance.gameObject.SetActive(true);
            malhaAlcance = MalhasProcedurais.AnelNoChao(configFisica.alcanceTaco, configHUD.espessuraRaioAlcance, malhaAlcance);
            alcance.sharedMesh = malhaAlcance;
            alcance.transform.SetPositionAndRotation(new Vector3(pes.x, AlturaAlcance, pes.z), Quaternion.identity);
        }

        /// <summary>"X" no chão em 'ponto'.</summary>
        public void MostrarMarca(Vector3 ponto)
        {
            marca.gameObject.SetActive(true);
            malhaMarca = MalhasProcedurais.XisNoChao(configHUD.tamanhoMarcaPassagem, configHUD.espessuraMarcaPassagem, malhaMarca);
            marca.sharedMesh = malhaMarca;
            marca.transform.SetPositionAndRotation(new Vector3(ponto.x, AlturaMarca, ponto.z), Quaternion.identity);
        }

        /// <summary>
        /// Anel de tempo em volta da bola (sempre virado para a câmera).
        /// 'tempoRestante' = segundos até o momento perfeito; o anel fecha no zero.
        /// </summary>
        public void MostrarAnel(Vector3 centroBola, float tempoRestante)
        {
            float duracao = Mathf.Max(0.05f, configHUD.duracaoAnelTempo);
            if (tempoRestante > duracao || tempoRestante < 0f) { anel.gameObject.SetActive(false); return; }

            anel.gameObject.SetActive(true);
            float raioBolaVisual = configFisica.raioBola * configFisica.escalaVisualBola;
            float raio = Mathf.Lerp(raioBolaVisual * 1.3f, configHUD.raioMaximoAnelTempo, tempoRestante / duracao);
            float espessura = configHUD.espessuraAnelTempo;
            malhaAnel = MalhasProcedurais.SetorDeAnel(raio, raio + espessura, 0f, 360f, malhaAnel);
            anel.sharedMesh = malhaAnel;
            Camera cameraPrincipal = Camera.main;
            Quaternion virado = cameraPrincipal != null ? cameraPrincipal.transform.rotation : Quaternion.identity;
            anel.transform.SetPositionAndRotation(centroBola, virado);
        }

        public void EsconderAlcance() { if (alcance != null) alcance.gameObject.SetActive(false); }
        public void EsconderMarca() { if (marca != null) marca.gameObject.SetActive(false); }
        public void EsconderAnel() { if (anel != null) anel.gameObject.SetActive(false); }

        public void EsconderTudo()
        {
            EsconderAlcance();
            EsconderMarca();
            EsconderAnel();
        }
    }
}
