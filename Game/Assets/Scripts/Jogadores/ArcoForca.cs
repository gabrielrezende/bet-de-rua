using UnityEngine;

namespace BetsDeRua
{
    /// <summary>
    /// Arco de força ao lado do jogador (docs/REFERENCIAS_FIFA.md, seção 9.3), como um
    /// velocímetro sempre virado para a câmera. Enche enquanto o botão está segurado;
    /// a faixa verde marca a hora certa de soltar. Branco antes da faixa, verde dentro,
    /// vermelho depois (bola desgovernada).
    /// </summary>
    public class ArcoForca : MonoBehaviour
    {
        [SerializeField] private ConfigHUD configHUD;
        [SerializeField] private ConfigFisica configFisica;
        [SerializeField] private Material materialFundo;
        [SerializeField] private Material materialFaixa;
        [SerializeField] private Material materialPreenchimento;

        // Pequenos deslocamentos na direção da câmera para cada camada ficar por cima da anterior
        private const float CamadaFaixa = -0.005f;
        private const float CamadaPreenchimento = -0.01f;

        private MeshFilter fundo;
        private MeshFilter faixa;
        private MeshFilter preenchimento;
        private MeshRenderer desenhoPreenchimento;
        private MaterialPropertyBlock propriedades;
        private Mesh malhaFundo, malhaFaixa, malhaPreenchimento;

        private void Awake()
        {
            fundo = CriarFilho("Fundo", materialFundo, 0f);
            faixa = CriarFilho("FaixaVerde", materialFaixa, CamadaFaixa);
            preenchimento = CriarFilho("Preenchimento", materialPreenchimento, CamadaPreenchimento);
            desenhoPreenchimento = preenchimento.GetComponent<MeshRenderer>();
            propriedades = new MaterialPropertyBlock();
            Esconder();
        }

        private MeshFilter CriarFilho(string nome, Material material, float camada)
        {
            GameObject filho = new GameObject(nome);
            filho.transform.SetParent(transform, false);
            filho.transform.localPosition = new Vector3(0f, 0f, camada);
            MeshRenderer desenho = filho.AddComponent<MeshRenderer>();
            desenho.sharedMaterial = material;
            desenho.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return filho.AddComponent<MeshFilter>();
        }

        /// <summary>Mostra o arco ao lado de quem está nos 'pes', cheio até 'carga' (0 a 1).</summary>
        public void Mostrar(Vector3 pes, float carga)
        {
            gameObject.SetActive(true);
            Camera cameraPrincipal = Camera.main;
            if (cameraPrincipal != null)
            {
                transform.rotation = cameraPrincipal.transform.rotation;
                transform.position = pes + Vector3.up * configHUD.alturaArco
                                     + cameraPrincipal.transform.right * configHUD.ladoArco;
            }

            float raio = configHUD.raioArco;
            float espessura = configHUD.espessuraArco;
            float inicio = configFisica.inicioFaixaVerde;
            float fim = configFisica.fimFaixaVerde;

            malhaFundo = MalhasProcedurais.SetorDeAnel(raio - espessura, raio, Angulo(0f), Angulo(1f), malhaFundo);
            fundo.sharedMesh = malhaFundo;
            // A faixa verde fica por fora do arco, como uma marca na borda
            malhaFaixa = MalhasProcedurais.SetorDeAnel(raio, raio + espessura * 0.5f, Angulo(inicio), Angulo(fim), malhaFaixa);
            faixa.sharedMesh = malhaFaixa;
            malhaPreenchimento = MalhasProcedurais.SetorDeAnel(raio - espessura * 0.85f, raio - espessura * 0.15f,
                                                               Angulo(0f), Angulo(Mathf.Max(0.001f, carga)), malhaPreenchimento);
            preenchimento.sharedMesh = malhaPreenchimento;

            Color cor = carga > fim ? configHUD.corArcoPassou
                      : carga >= inicio ? configHUD.corArcoNaFaixa
                      : configHUD.corArcoNormal;
            // Recria se o Unity recarregou os scripts com o jogo rodando (variáveis internas zeram)
            if (propriedades == null) propriedades = new MaterialPropertyBlock();
            if (desenhoPreenchimento == null) desenhoPreenchimento = preenchimento.GetComponent<MeshRenderer>();
            propriedades.SetColor("_BaseColor", cor);
            desenhoPreenchimento.SetPropertyBlock(propriedades);
        }

        public void Esconder()
        {
            gameObject.SetActive(false);
        }

        /// <summary>Converte 0..1 do arco em ângulo: começa embaixo à esquerda e gira no sentido do relógio.</summary>
        private float Angulo(float fracao)
        {
            float abertura = configHUD.aberturaArco;
            return 90f + abertura * 0.5f - abertura * fracao;
        }
    }
}
