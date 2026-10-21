using UnityEngine;

namespace BetsDeRua
{
    /// <summary>
    /// Física própria e simples da bola (sem Rigidbody). O passo de gravidade, ar, quique,
    /// rolar e muros fica em FisicaBola (também usado na previsão); aqui ficam as garrafas.
    /// A simulação roda em passos pequenos para a bola rápida não atravessar a garrafa.
    /// </summary>
    public class Bola : MonoBehaviour
    {
        [SerializeField] private ConfigFisica config;
        [Tooltip("Garrafas que a bola pode acertar.")]
        [SerializeField] private Garrafa[] garrafas;
        [Tooltip("Corpos que a bola pode acertar quando CorposAtivos (os rebatedores, para a lancha).")]
        [SerializeField] private Jogador[] corpos;

        /// <summary>
        /// True enquanto a bola pode bater no corpo dos rebatedores (o arremesso, até a batida).
        /// Desliga sozinho no primeiro toque.
        /// </summary>
        public bool CorposAtivos { get; set; }
        /// <summary>A bola tocou no corpo de um jogador (e desviou). Usado para a lancha.</summary>
        public event System.Action<Jogador> TocouNoCorpo;
        private Garrafa garrafaAtras;

        /// <summary>
        /// O arremesso sai de trás da casinha de quem arremessa: essa garrafa não conta até a bola
        /// se afastar dela (senão a rasteira, rente ao chão, derrubava a própria garrafa).
        /// </summary>
        public void IgnorarGarrafaDeOrigem(Garrafa origem) => garrafaAtras = origem;

        /// <summary>Posição e velocidade da bola logo antes de bater no corpo (para saber se ia na garrafa).</summary>
        public Vector3 PosicaoAntesDoCorpo { get; private set; }
        public Vector3 VelocidadeAntesDoCorpo { get; private set; }

        // Curva do arremesso (analógico direito depois de soltar): empurra a bola de lado
        private Vector3 eixoCurva;
        private float intensidadeCurva;     // -1 a 1
        private float lateralAcumulada;     // quanto de velocidade de lado a curva já deu (m/s)

        // Posição "física" do centro da bola (o visual fica um pouco acima, por ser maior)
        private Vector3 posicao;
        private Vector3 velocidade;

        public Vector3 Posicao => posicao;
        public Vector3 Velocidade => velocidade;
        /// <summary>True quando a bola está parada no chão.</summary>
        public bool Parada { get; private set; } = true;
        /// <summary>True se a bola já encostou no chão desde o último lançamento (pegar antes = "no ar").</summary>
        public bool QuicouDesdeLancamento { get; private set; }
        /// <summary>Quem lançou a bola por último (arremesso, chute, passe ou batida). Null = ninguém.</summary>
        public Jogador UltimoLancador { get; private set; }
        /// <summary>
        /// True depois que a bola se afastou de onde foi lançada (ConfigFisica.distanciaLiberarPropriaBola).
        /// Antes disso, quem lançou não pode pegá-la de volta (senão o arremessador "pegava" a própria bola
        /// no quadro seguinte ao arremesso, quando ela saía fraca, ainda perto da mão).
        /// </summary>
        public bool AfastouDoLancamento { get; private set; } = true;
        private Vector3 pontoDeLancamento;

        private TrailRenderer rastro;

        /// <summary>O rastro atrás da bola rápida (como o traço da bola no golfe), só no desenho.</summary>
        private void AtualizarRastro(bool teleporte)
        {
            if (configHUD == null || !configHUD.rastroBola || configHUD.materialRastro == null) return;
            if (rastro == null)
            {
                rastro = gameObject.AddComponent<TrailRenderer>();
                rastro.sharedMaterial = configHUD.materialRastro;
                rastro.time = configHUD.duracaoRastro;
                rastro.minVertexDistance = 0.05f;
                rastro.widthCurve = AnimationCurve.Linear(0f, configHUD.larguraRastro, 1f, 0f);
                Gradient g = new Gradient();
                Color c = configHUD.corRastro;
                g.SetKeys(new[] { new GradientColorKey(c, 0f), new GradientColorKey(c, 1f) },
                          new[] { new GradientAlphaKey(c.a, 0f), new GradientAlphaKey(0f, 1f) });
                rastro.colorGradient = g;
                rastro.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                rastro.emitting = false;
            }
            if (teleporte) { rastro.Clear(); rastro.emitting = false; return; }
            rastro.emitting = !Parada && velocidade.magnitude >= configHUD.velocidadeMinimaRastro;
        }

        private void Awake()
        {
            // O visual fica acima da posição física; desfaz esse deslocamento
            float raioVisual = config.raioBola * config.escalaVisualBola;
            posicao = transform.position - Vector3.up * (raioVisual - config.raioBola);
            AtualizarVisual();
        }

        /// <summary>Põe a bola numa posição com uma velocidade inicial (m/s), lançada por 'lancador'.</summary>
        public void Lancar(Vector3 posicaoInicial, Vector3 velocidadeInicial, Jogador lancador = null)
        {
            posicao = posicaoInicial;
            velocidade = velocidadeInicial;
            Parada = false;
            AtualizarRastro(true); // sai da mão: não liga o rastro com o caminho até a mão
            QuicouDesdeLancamento = posicao.y <= FisicaBola.AlturaDoChao(config, posicao.z) + config.raioBola + 0.001f;
            UltimoLancador = lancador;
            pontoDeLancamento = posicaoInicial;
            AfastouDoLancamento = lancador == null;
            // A garrafa que fica ATRÁS de quem lança (a da casinha de onde ele arremessa) não conta
            // até a bola se afastar dela: senão a rasteira, que sai rente ao chão, derrubava a própria garrafa
            garrafaAtras = null;
            if (garrafas != null)
                foreach (Garrafa g in garrafas)
                {
                    if (g == null) continue;
                    Vector3 paraGarrafa = g.transform.position - posicaoInicial;
                    paraGarrafa.y = 0f;
                    if (paraGarrafa.magnitude < config.distanciaGarrafaAtras && Vector3.Dot(paraGarrafa, new Vector3(velocidadeInicial.x, 0f, velocidadeInicial.z)) < 0f)
                        garrafaAtras = g;
                }
            intensidadeCurva = 0f;
            lateralAcumulada = 0f;
            AtualizarVisual();
        }

        /// <summary>
        /// Bate-pronto: a bola "morre" no meio do caminho: perde velocidade e desce para quicar antes,
        /// chegando mais baixa (engana quem esperava a bola no ar).
        /// </summary>
        public void BatePronto(float fatorVelocidade, float descida)
        {
            Vector3 horizontal = new Vector3(velocidade.x, 0f, velocidade.z) * fatorVelocidade;
            velocidade = horizontal + Vector3.down * descida;
        }

        /// <summary>
        /// Curva: empurra a bola de lado, no eixo 'eixoLateral' (no chão), com 'intensidade'
        /// de -1 a 1. Há um limite de quanto ela pode desviar no total (ConfigFisica).
        /// </summary>
        public void DefinirCurva(Vector3 eixoLateral, float intensidade)
        {
            eixoLateral.y = 0f;
            eixoCurva = eixoLateral.sqrMagnitude > 0.0001f ? eixoLateral.normalized : Vector3.zero;
            intensidadeCurva = Mathf.Clamp(intensidade, -1f, 1f);
        }

        /// <summary>Para a bola numa posição.</summary>
        public void Colocar(Vector3 novaPosicao)
        {
            posicao = novaPosicao;
            velocidade = Vector3.zero;
            Parada = true;
            AtualizarVisual();
            AtualizarRastro(true);
        }

        private void Update()
        {
            if (!Parada)
            {
                // Divide o frame em passos pequenos
                float restante = Time.deltaTime;
                float passoMaximo = Mathf.Max(0.0005f, config.passoSimulacao);
                while (restante > 0f && !Parada)
                {
                    float passo = Mathf.Min(restante, passoMaximo);
                    Simular(passo);
                    restante -= passo;
                }
                if (!AfastouDoLancamento)
                {
                    Vector3 andou = posicao - pontoDeLancamento;
                    andou.y = 0f;
                    AfastouDoLancamento = andou.magnitude >= config.distanciaLiberarPropriaBola;
                }
            }
            AtualizarVisual();
            AtualizarRastro(false);
        }

        private void Simular(float dt)
        {
            bool parada = Parada;
            AplicarCurva(dt);
            Vector3 antes = velocidade;
            FisicaBola.Passo(config, ref posicao, ref velocidade, ref parada, dt, true);
            Parada = parada;
            // Som: quique no chão (a velocidade vertical virou) e batida na parede (a horizontal virou)
            if (antes.y < -0.8f && velocidade.y > 0f)
            {
                Sons.Tocar(TipoSom.Quique, Mathf.Clamp01(-antes.y / 7f) * 0.9f);
                PoeiraBola.Soltar(new Vector3(posicao.x, FisicaBola.AlturaDoChao(config, posicao.z), posicao.z), Mathf.Clamp01((-antes.y - 1.5f) / 6f), configHUD);
            }
            if ((antes.x * velocidade.x < 0f && Mathf.Abs(antes.x) > 1.5f) || (antes.z * velocidade.z < 0f && Mathf.Abs(antes.z) > 1.5f))
                Sons.Tocar(TipoSom.Parede, Mathf.Clamp01(new Vector2(antes.x, antes.z).magnitude / 12f));
            if (posicao.y <= FisicaBola.AlturaDoChao(config, posicao.z) + config.raioBola + 0.001f) QuicouDesdeLancamento = true;
            // A garrafa usa o tamanho que se VÊ da bola (ela aparece maior que a real, para ler de longe):
            // encostou na tela, derrubou
            if (!Parada) ColidirComGarrafas(config.raioBola * config.escalaVisualBola);
            if (!Parada && CorposAtivos) ColidirComCorpos(config.raioBola);
        }

        private void AplicarCurva(float dt)
        {
            if (Mathf.Approximately(intensidadeCurva, 0f) || eixoCurva == Vector3.zero) return;
            float limite = config.velocidadeLateralMaximaCurva;
            float dv = intensidadeCurva * config.aceleracaoCurva * dt;
            float nova = Mathf.Clamp(lateralAcumulada + dv, -limite, limite);
            velocidade += eixoCurva * (nova - lateralAcumulada);
            lateralAcumulada = nova;
        }

        /// <summary>Corpo = cilindro em pé (raio e altura do jogador). Bateu: desvia e avisa.</summary>
        private void ColidirComCorpos(float raio)
        {
            if (corpos == null) return;
            foreach (Jogador corpo in corpos)
            {
                if (corpo == null) continue;
                Vector3 pe = corpo.transform.position;
                if (posicao.y > pe.y + corpo.Altura + raio) continue;

                Vector3 deslocamento = posicao - pe;
                deslocamento.y = 0f;
                float distanciaMinima = config.raioJogador + raio;
                if (deslocamento.sqrMagnitude >= distanciaMinima * distanciaMinima) continue;

                Vector3 normal = deslocamento.sqrMagnitude > 0.000001f
                    ? deslocamento.normalized
                    : -new Vector3(velocidade.x, 0f, velocidade.z).normalized;
                if (Vector3.Dot(velocidade, normal) >= 0f) continue;

                PosicaoAntesDoCorpo = posicao;
                VelocidadeAntesDoCorpo = velocidade;
                // Desvia no corpo, perdendo força, e segue em jogo
                velocidade = Vector3.Reflect(velocidade, normal) * config.quiqueCorpo;
                posicao = new Vector3(pe.x, posicao.y, pe.z) + normal * distanciaMinima;
                CorposAtivos = false;
                TocouNoCorpo?.Invoke(corpo);
                return;
            }
        }

        /// <summary>
        /// Prevê por onde a bola vai passar (sem contar as garrafas), a partir de agora.
        /// Preenche 'pontos' com uma posição a cada 'passo' segundos, por até 'duracao' segundos.
        /// </summary>
        public void PreverTrajetoria(float duracao, float passo, System.Collections.Generic.List<Vector3> pontos)
        {
            pontos.Clear();
            Vector3 p = posicao, v = velocidade;
            bool parada = Parada;
            pontos.Add(p);
            int passos = Mathf.CeilToInt(duracao / Mathf.Max(0.001f, passo));
            float subPasso = Mathf.Max(0.0005f, config.passoSimulacao);
            for (int i = 0; i < passos && !parada; i++)
            {
                // Mesmos passos pequenos da bola de verdade, para a previsão bater com o real
                for (float t = 0f; t < passo && !parada; t += subPasso)
                    FisicaBola.Passo(config, ref p, ref v, ref parada, Mathf.Min(subPasso, passo - t));
                pontos.Add(p);
            }
        }

        private void ColidirComGarrafas(float raio)
        {
            if (garrafas == null) return;
            foreach (Garrafa garrafa in garrafas)
            {
                if (garrafa == null || garrafa.Derrubada) continue;
                if (garrafa == garrafaAtras)
                {
                    // Ignora até a bola passar da garrafa (no sentido em que ela vai) com folga
                    Vector3 rel = posicao - garrafa.transform.position;
                    rel.y = 0f;
                    Vector3 rumo = new Vector3(velocidade.x, 0f, velocidade.z);
                    float passou = rumo.sqrMagnitude > 0.01f ? Vector3.Dot(rel, rumo.normalized) : rel.magnitude;
                    if (passou < config.distanciaGarrafaAtras * 0.4f) continue;
                    garrafaAtras = null;
                }

                // Garrafa em pé = cilindro vertical com a base no chão
                Vector3 baseGarrafa = garrafa.transform.position;
                if (posicao.y > baseGarrafa.y + garrafa.Altura + raio) continue;

                Vector3 deslocamento = posicao - baseGarrafa;
                deslocamento.y = 0f;
                float distanciaMinima = garrafa.Raio + raio;
                if (deslocamento.sqrMagnitude >= distanciaMinima * distanciaMinima)
                {
                    // Passou raspando e rápida: a garrafa balança sem cair
                    float distanciaRaspao = distanciaMinima + config.distanciaRaspao;
                    if (deslocamento.sqrMagnitude < distanciaRaspao * distanciaRaspao
                        && velocidade.magnitude * 3.6f >= config.velocidadeRaspaoKmh)
                        garrafa.ReceberRaspao(velocidade);
                    continue;
                }

                Vector3 normal = deslocamento.sqrMagnitude > 0.000001f
                    ? deslocamento.normalized
                    : -new Vector3(velocidade.x, 0f, velocidade.z).normalized;
                // Só conta se a bola estiver indo na direção da garrafa
                if (Vector3.Dot(velocidade, normal) >= 0f) continue;

                float alturaContato = Mathf.Clamp(posicao.y - baseGarrafa.y, 0f, garrafa.Altura);
                Vector3 ponto = baseGarrafa + normal * garrafa.Raio + Vector3.up * alturaContato;
                if (garrafa.ReceberBolada(velocidade, ponto))
                {
                    // Derrubou: a garrafa sai da frente e a bola segue, perdendo parte da força
                    velocidade *= config.velocidadeQueSegueAoDerrubar;
                    continue;
                }

                // Bolada fraca: a bola volta, perdendo velocidade
                velocidade = Vector3.Reflect(velocidade, normal) * config.quiqueGarrafa;
                posicao = new Vector3(baseGarrafa.x, posicao.y, baseGarrafa.z) + normal * distanciaMinima;
            }
        }

        [Tooltip("Para a bola ter o tamanho de verdade de perto e ampliado de longe (só o desenho).")]
        [SerializeField] private ConfigHUD configHUD;

        private void AtualizarVisual()
        {
            if (config == null) return;
            // Só o desenho muda com a distância da câmera; a física (garrafa, corpo) usa sempre o mesmo tamanho
            float ampliar = config.escalaVisualBola;
            Camera cam = Camera.main;
            if (cam != null && configHUD != null)
            {
                float d = Vector3.Distance(cam.transform.position, posicao);
                ampliar = Mathf.Lerp(1f, config.escalaVisualBola, Mathf.InverseLerp(configHUD.distanciaBolaReal, configHUD.distanciaReferenciaTela, d));
            }
            float raioVisual = config.raioBola * ampliar;
            transform.localScale = Vector3.one * (raioVisual * 2f);
            // Sobe o visual para a bola "grande" não afundar no chão
            transform.position = posicao + Vector3.up * (raioVisual - config.raioBola);
        }
    }
}
