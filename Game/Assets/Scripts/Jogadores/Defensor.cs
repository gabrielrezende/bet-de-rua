using System;
using UnityEngine;

namespace BetsDeRua
{
    /// <summary>
    /// Pegar a bola na defesa (GDD, "Dinâmica do lance", passo 4):
    /// - □ (X do Xbox) / R perto da bola (até raioPegar) = pega.
    ///   Antes dela quicar = "pegou no ar" (o ataque perde o taco).
    /// - □ com a bola um pouco mais longe (até alcanceMergulho) = carrinho (bola rasteira) ou
    ///   peixinho (bola quicando), mirando onde a bola vai estar: bote rápido, desliza e levanta.
    ///   Pegou no bote: segura a bola deslizando e só mira e lança depois de levantar.
    /// - Passe do colega chegando perto é pego sozinho.
    /// Quem decide se valeu é o ControladorLance (ouvindo o evento Pegou).
    /// </summary>
    [RequireComponent(typeof(Jogador))]
    public class Defensor : MonoBehaviour
    {
        [SerializeField] private ConfigFisica config;
        [SerializeField] private ConfigHUD configHUD;
        [SerializeField] private Bola bola;
        [Tooltip("Círculo de alcance normal (bola longe).")]
        [SerializeField] private Material materialAlcance;
        [Tooltip("Círculo de alcance aceso (bola ao alcance: aperte □).")]
        [SerializeField] private Material materialAoAlcance;

        /// <summary>Avisa (quem pegou, pegou no ar?).</summary>
        public event Action<Defensor, bool> Pegou;

        private Jogador jogador;
        private Arremessador comBola;
        private bool procurandoNoBote;
        private float tempoEsperandoChegar = float.MaxValue; // □ apertado com a bola vindo: pega quando ela chegar

        // Círculo no chão mostrando até onde dá para pegar a bola (só para quem uma pessoa controla)
        private const float AlturaCirculo = 0.018f;
        private MeshFilter circulo;
        private MeshRenderer desenhoCirculo;
        private Mesh malhaCirculo;
        private bool circuloAceso;

        /// <summary>O ControladorLance diz se a bola está solta no campo (dá para pegar).</summary>
        public bool BolaLivre { get; set; }
        /// <summary>Enquanto o arremesso está no ar, o □ de quem arremessou é o bate-pronto, não o carrinho.</summary>
        public bool BloquearDeslize { get; set; }
        public Jogador Jogador => jogador;
        public Arremessador ComBola => comBola;

        private void Awake()
        {
            jogador = GetComponent<Jogador>();
            comBola = GetComponent<Arremessador>();

            GameObject filho = new GameObject("CirculoPegar");
            filho.transform.SetParent(transform, false);
            desenhoCirculo = filho.AddComponent<MeshRenderer>();
            desenhoCirculo.sharedMaterial = materialAlcance;
            desenhoCirculo.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            circulo = filho.AddComponent<MeshFilter>();
            filho.SetActive(false);
        }

        private void LateUpdate()
        {
            // Círculo de alcance: aparece para quem você controla com a bola solta;
            // acende (amarelo e mais grosso) quando a bola está ao alcance do □
            bool mostrar = BolaLivre && jogador.Controlador != null && !jogador.Mergulhando;
            circulo.gameObject.SetActive(mostrar);
            if (!mostrar) return;
            bool aoAlcance = AoAlcance(config.raioPegar);
            if (malhaCirculo == null || aoAlcance != circuloAceso)
            {
                circuloAceso = aoAlcance;
                float espessura = configHUD.espessuraRaioAlcance * (aoAlcance ? 3f : 1f);
                malhaCirculo = MalhasProcedurais.AnelNoChao(config.raioPegar, espessura, malhaCirculo);
                circulo.sharedMesh = malhaCirculo;
                desenhoCirculo.sharedMaterial = aoAlcance ? materialAoAlcance : materialAlcance;
            }
            circulo.transform.SetPositionAndRotation(new Vector3(transform.position.x, AlturaCirculo, transform.position.z),
                                                     Quaternion.identity);
        }

        private void Update()
        {
            if (!BolaLivre) { procurandoNoBote = false; return; }

            ControleHumano humano = jogador.Controlador;
            if (humano != null && humano.Conectado)
            {
                // Pessoa: gatilho (R2/L2) pega; □ desliza (carrinho/peixinho) para parar a bola, sem pegar.
                // Deslizou e a bola ficou ao alcance: o gatilho pega ali mesmo, no chão.
                if (humano.ApertouPegarBola) PegarComGatilho();
                if (humano.ApertouPegar) Deslizar();
            }

            // □ apertado com a bola vindo na direção dele: pega sozinho quando ela entrar no alcance
            if (tempoEsperandoChegar < config.tempoEsperarBolaChegar)
            {
                tempoEsperandoChegar += Time.deltaTime;
                if (AoAlcance(config.raioPegar)) { tempoEsperandoChegar = float.MaxValue; Pegar(); return; }
            }

            // No bote do mergulho: pega se alcançar
            if (procurandoNoBote)
            {
                if (!jogador.NoBoteDoMergulho) procurandoNoBote = false;
                else if (AoAlcance(config.raioPegarNoMergulho)) Pegar();
            }

            // Passe do colega chegando: pega sozinho
            Jogador lancador = bola.UltimoLancador;
            if (lancador != null && lancador != jogador && lancador.Dupla == jogador.Dupla
                && AoAlcance(config.raioReceberPasse))
                Pegar();
        }

        /// <summary>Gatilho da pessoa: pega se estiver ao alcance (maior se estiver deslizando); a bola vindo, pega quando chegar.</summary>
        private void PegarComGatilho()
        {
            if (!BolaLivre) return;
            float raio = jogador.Mergulhando ? config.raioPegarNoMergulho : config.raioPegar;
            if (AoAlcance(raio)) { Pegar(); return; }
            if (!jogador.Mergulhando && VaiChegarPerto()) tempoEsperandoChegar = 0f;
        }

        /// <summary>□ da pessoa: carrinho (bola rasteira) ou peixinho (bola quicando) para onde a bola vai estar. Não pega sozinho.</summary>
        private void Deslizar()
        {
            if (!BolaLivre || BloquearDeslize || jogador.Mergulhando) return;
            Vector3 ponto = PontoDoBote();
            jogador.Mergulhar(ponto - transform.position, ponto.y <= config.alturaMaximaCarrinho);
        }

        /// <summary>
        /// Tenta pegar (os bots usam: pega perto, espera a bola chegar ou mergulha e pega no bote). Perto: pega.
        /// Um pouco longe: mergulha (carrinho ou peixinho) para ONDE A BOLA VAI ESTAR,
        /// não para onde ela está agora. Longe demais: nada.
        /// </summary>
        public void TentarPegar()
        {
            if (!BolaLivre || jogador.Mergulhando) return;
            if (AoAlcance(config.raioPegar)) { Pegar(); return; }

            Vector3 rumoAgora = bola.Posicao - transform.position;
            rumoAgora.y = 0f;
            if (rumoAgora.magnitude > config.alcanceMergulho || bola.Posicao.y > config.alturaMaximaPegar) return;

            // A bola vem e vai passar perto dele: não precisa se jogar, pega quando ela chegar
            if (VaiChegarPerto())
            {
                tempoEsperandoChegar = 0f;
                return;
            }

            Vector3 ponto = PontoDoBote();
            Vector3 rumo = ponto - transform.position;
            rumo.y = 0f;
            jogador.Mergulhar(rumo, ponto.y <= config.alturaMaximaCarrinho);
            procurandoNoBote = true;
        }

        private readonly System.Collections.Generic.List<Vector3> previsao = new System.Collections.Generic.List<Vector3>();

        /// <summary>True se, no tempo de espera, a bola entra no alcance normal do □ (sem mergulho).</summary>
        private bool VaiChegarPerto()
        {
            bola.PreverTrajetoria(config.tempoEsperarBolaChegar, PassoPrevisaoBote, previsao);
            foreach (Vector3 p in previsao)
            {
                Vector3 d = p - transform.position;
                d.y = 0f;
                if (d.magnitude <= config.raioPegar && p.y <= config.alturaMaximaPegar) return true;
            }
            return false;
        }
        private const float PassoPrevisaoBote = 0.02f;

        /// <summary>
        /// Onde mirar o bote: o primeiro ponto da trajetória da bola que o corpo alcança no
        /// tempo do bote (ele sai a velocidadeMergulho e pega a até raioPegarNoMergulho).
        /// Se nenhum der, o ponto em que a bola passa mais perto.
        /// </summary>
        private Vector3 PontoDoBote()
        {
            bola.PreverTrajetoria(config.duracaoBoteMergulho, PassoPrevisaoBote, previsao);
            Vector3 maisPerto = bola.Posicao;
            float menorFolga = float.MaxValue;
            for (int i = 0; i < previsao.Count; i++)
            {
                Vector3 p = previsao[i];
                if (p.y > config.alturaMaximaPegar) continue;
                float t = i * PassoPrevisaoBote;
                Vector3 d = p - transform.position;
                d.y = 0f;
                float folga = d.magnitude - (config.velocidadeMergulho * t + config.raioPegarNoMergulho);
                if (folga <= 0f) return p;
                if (folga < menorFolga) { menorFolga = folga; maisPerto = p; }
            }
            return maisPerto;
        }

        /// <summary>True se a bola está a até 'raio' metros (no chão) e numa altura que dá para pegar.</summary>
        public bool AoAlcance(float raio)
        {
            Vector3 rumo = bola.Posicao - transform.position;
            rumo.y = 0f;
            return rumo.magnitude <= raio && bola.Posicao.y <= config.alturaMaximaPegar;
        }

        private void Pegar()
        {
            procurandoNoBote = false;
            bool noAr = !bola.QuicouDesdeLancamento;
            Sons.Tocar(TipoSom.Pegou, 0.55f);
            Vibracao.Pulsar(Jogador, 0.25f, 0.4f, 0.08f);
            Pegou?.Invoke(this, noAr);
        }
    }
}
