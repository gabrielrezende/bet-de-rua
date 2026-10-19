using UnityEngine;

namespace BetsDeRua
{
    /// <summary>As duas duplas do jogo.</summary>
    public enum Dupla { Taco, Bola }

    /// <summary>O papel de cada jogador no lance.</summary>
    public enum Papel { Rebatedor, Arremessador, Guardiao }

    /// <summary>
    /// Um jogador (cápsula). Recebe a intenção de movimento de quem o controla
    /// (pessoa ou bot) e anda com aceleração e freada, para o movimento ter peso.
    /// Os muros e os outros jogadores seguram ele (CharacterController).
    /// Sem ninguém e sem bot mandando, ele freia e fica parado. Também sabe mergulhar
    /// (bote rápido para a bola, depois um tempinho caído).
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class Jogador : MonoBehaviour
    {
        [SerializeField] private ConfigFisica config;
        [SerializeField] private Dupla dupla;
        [SerializeField] private Papel papel;
        [Tooltip("Nome curto que aparece na tela.")]
        [SerializeField] private string nome = "Jogador";

        private CharacterController corpo;
        private Vector3 velocidade;
        private Vector2 entradaMovimento;
        private bool entradaSprint;
        private bool entradaDevagar;
        private bool entradaDoBotNesteFrame;

        // Mergulho / carrinho
        private float tempoMergulho = float.MaxValue;
        private Vector3 direcaoMergulho;
        private bool mergulhoDeCarrinho;
        private Transform[] partesDoCorpo;      // corpo e rosto, que deitam no mergulho
        private Vector3[] posicoesOriginais;
        private Quaternion[] rotacoesOriginais;

        public Dupla Dupla => dupla;
        public Papel Papel => papel;
        public string Nome => nome;
        /// <summary>Velocidade atual no chão (m/s).</summary>
        public Vector3 Velocidade => velocidade;
        /// <summary>Estamina do sprint, de 0 (vazia) a 1 (cheia).</summary>
        public float Estamina { get; private set; } = 1f;
        /// <summary>Quem controla este jogador agora (null = ninguém).</summary>
        public ControleHumano Controlador { get; set; }
        public bool EstaParado => velocidade.magnitude < config.velocidadeParadoJogador;
        /// <summary>
        /// Com a bola na mão: anda no máximo a velocidadeComBola, sem sprint (GDD),
        /// e R2/L2 ficam livres para arremessar.
        /// </summary>
        public bool ComBolaNaMao { get; set; }

        /// <summary>
        /// Quando true, R2/L2 servem para outra coisa (ex.: rebatedor com a bola vindo):
        /// sem sprint e sem "andar devagar".
        /// </summary>
        public bool GatilhosOcupados { get; set; }

        // Limite opcional: o jogador não passa da linha 'pontoLimite' no sentido 'direcaoLimite'
        private bool temLimiteFrente;
        private Vector3 pontoLimite;
        private Vector3 direcaoLimite;

        /// <summary>True durante o bote do mergulho (é quando ele alcança a bola).</summary>
        public bool NoBoteDoMergulho => tempoMergulho < config.duracaoBoteMergulho;
        /// <summary>True durante o mergulho inteiro (bote + caído): não anda nem pega.</summary>
        public bool Mergulhando => tempoMergulho < config.duracaoBoteMergulho + config.duracaoCaidoMergulho;

        private void Awake()
        {
            corpo = GetComponent<CharacterController>();
            // Sem isso, com muitos quadros por segundo cada passo fica menor que o mínimo
            // e o CharacterController não move nada (o jogador só vira, não anda)
            corpo.minMoveDistance = 0f;

            var partes = new System.Collections.Generic.List<Transform>();
            foreach (string nomeParte in new[] { "Corpo", "Rosto" })
            {
                Transform parte = transform.Find(nomeParte);
                if (parte != null) partes.Add(parte);
            }
            partesDoCorpo = partes.ToArray();
            posicoesOriginais = System.Array.ConvertAll(partesDoCorpo, t => t.localPosition);
            rotacoesOriginais = System.Array.ConvertAll(partesDoCorpo, t => t.localRotation);
        }

        /// <summary>Entrada vinda de um bot (CPU). Vale só para este frame.</summary>
        public void DefinirEntradaBot(Vector2 movimento, bool sprint)
        {
            DefinirEntrada(movimento, sprint, false);
            entradaDoBotNesteFrame = true;
        }

        /// <summary>
        /// Mergulho na direção dada: carrinho (bola rasteira: de pé na frente, deitado para trás)
        /// ou peixinho (bola quicando: de peito). Bote rápido, desliza no chão e levanta.
        /// </summary>
        public void Mergulhar(Vector3 direcao, bool carrinho)
        {
            if (Mergulhando) return;
            direcao.y = 0f;
            if (direcao.sqrMagnitude < 0.0001f) direcao = transform.forward;
            direcaoMergulho = direcao.normalized;
            mergulhoDeCarrinho = carrinho;
            OlharPara(direcaoMergulho);
            tempoMergulho = 0f;
        }

        /// <summary>Põe o jogador direto numa posição, parado (arrumar o campo entre lances).</summary>
        public void Teleportar(Vector3 posicao, Vector3 olhandoPara)
        {
            corpo.enabled = false;
            transform.position = posicao;
            corpo.enabled = true;
            OlharPara(olhandoPara);
            velocidade = Vector3.zero;
            tempoMergulho = float.MaxValue;
            DeitarCorpo(0f);
        }

        /// <summary>
        /// Diz para onde o jogador quer ir neste frame.
        /// movimento: analógico (x = direita da tela, y = fundo da tela), comprimento de 0 a 1.
        /// </summary>
        public void DefinirEntrada(Vector2 movimento, bool sprint, bool devagar)
        {
            entradaMovimento = Vector2.ClampMagnitude(movimento, 1f);
            entradaSprint = sprint;
            entradaDevagar = devagar;
        }

        private void Update()
        {
            if (Controlador == null && !entradaDoBotNesteFrame) DefinirEntrada(Vector2.zero, false, false);
            entradaDoBotNesteFrame = false;
            if (AtualizarMergulho()) return;
            if (ComBolaNaMao || GatilhosOcupados)
            {
                // Com a bola ou esperando a batida: sem sprint e sem "andar devagar"
                // (os gatilhos viram arremesso / batida)
                entradaSprint = false;
                entradaDevagar = false;
            }

            float dt = Time.deltaTime;
            float intensidade = entradaMovimento.magnitude;
            // A câmera olha a rua de lado: analógico para a direita = +X, para cima = +Z (fundo da rua)
            Vector3 direcao = intensidade > 0.001f
                ? new Vector3(entradaMovimento.x, 0f, entradaMovimento.y) / intensidade
                : Vector3.zero;

            bool sprintando = entradaSprint && intensidade > 0.1f && !entradaDevagar && Estamina > 0f;
            Vector3 velocidadeDesejada = direcao * VelocidadeAlvo(intensidade, sprintando);

            // Acelera quando a velocidade desejada vai no mesmo sentido; freia quando muda de direção ou para
            Vector3 diferenca = velocidadeDesejada - velocidade;
            bool acelerando = Vector3.Dot(velocidadeDesejada, velocidade) >= 0f
                              && velocidadeDesejada.magnitude > velocidade.magnitude;
            float taxa = acelerando ? config.aceleracaoJogador : config.frenagemJogador;
            velocidade += Vector3.ClampMagnitude(diferenca, taxa * dt);

            AtualizarEstamina(sprintando, dt);

            AplicarLimiteFrente(dt);

            // Move (com um empurrãozinho para baixo para ficar grudado no chão)
            Vector3 antes = transform.position;
            CollisionFlags batidas = corpo.Move((velocidade + Vector3.down) * dt);
            // Se bateu de lado num muro ou jogador, a velocidade passa a ser o que de fato andou
            // (assim ele desliza ao longo do muro em vez de continuar "empurrando")
            if ((batidas & CollisionFlags.Sides) != 0 && dt > 0f)
            {
                Vector3 andou = transform.position - antes;
                andou.y = 0f;
                velocidade = andou / dt;
            }

            // Vira o corpo para onde está indo
            if (direcao != Vector3.zero)
            {
                Quaternion alvo = Quaternion.LookRotation(direcao, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, alvo, config.velocidadeVirar * dt);
            }
            else if (papel == Papel.Rebatedor && DentroDoCirculo())
            {
                // Rebatedor parado na base: vira para a área do taco, para a ponta do taco
                // encostar no desenho de giz (e não ficar de costas, com o taco para fora)
                Vector3 paraArea = ZonaDoTaco.CentroDaArea(ZonaDoTaco.GarrafaMaisPerto(transform.position, config), config)
                                   - transform.position;
                paraArea.y = 0f;
                if (paraArea.sqrMagnitude > 0.01f)
                {
                    Quaternion alvo = Quaternion.LookRotation(paraArea, Vector3.up);
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, alvo, config.velocidadeVirar * dt);
                }
            }
        }

        /// <summary>Cuida do mergulho. Retorna true se o jogador está mergulhando (não anda normal).</summary>
        private bool AtualizarMergulho()
        {
            if (!Mergulhando)
            {
                if (tempoMergulho != float.MaxValue) { tempoMergulho = float.MaxValue; DeitarCorpo(0f); }
                return false;
            }
            float dt = Time.deltaTime;
            tempoMergulho += dt;
            // Bote: sai rápido na direção da bola. Depois desliza no chão, perdendo velocidade
            float tempoNoChao = tempoMergulho - config.duracaoBoteMergulho;
            float rapidez = NoBoteDoMergulho
                ? config.velocidadeMergulho
                : Mathf.Max(0f, config.velocidadeMergulho - config.freioDeslizada * tempoNoChao);
            velocidade = direcaoMergulho * rapidez;
            corpo.Move((velocidade + Vector3.down) * dt);

            // Corpo: deita rápido no bote; no fim do tempo caído, vai levantando
            float deitado = Mathf.Clamp01(tempoMergulho / Mathf.Max(0.01f, config.duracaoBoteMergulho * 0.5f));
            float duracaoTotal = config.duracaoBoteMergulho + config.duracaoCaidoMergulho;
            float inicioLevantar = duracaoTotal - config.duracaoCaidoMergulho * config.fracaoLevantar;
            if (tempoMergulho > inicioLevantar)
                deitado *= 1f - Mathf.Clamp01((tempoMergulho - inicioLevantar) / Mathf.Max(0.01f, duracaoTotal - inicioLevantar));
            // Carrinho deita para trás (pés na frente); peixinho deita para a frente (de peito)
            DeitarCorpo(deitado * config.anguloMergulho * (mergulhoDeCarrinho ? -1f : 1f));
            return true;
        }

        /// <summary>Inclina o corpo para a frente, girando a partir dos pés.</summary>
        private void DeitarCorpo(float graus)
        {
            Quaternion giro = Quaternion.Euler(graus, 0f, 0f);
            for (int i = 0; i < partesDoCorpo.Length; i++)
            {
                partesDoCorpo[i].localPosition = giro * posicoesOriginais[i];
                partesDoCorpo[i].localRotation = giro * rotacoesOriginais[i];
            }
        }

        private float VelocidadeAlvo(float intensidade, bool sprintando)
        {
            if (intensidade <= 0.001f) return 0f;
            if (ComBolaNaMao) return Mathf.Min(VelocidadeSemBola(intensidade, false), config.velocidadeComBola);
            return VelocidadeSemBola(intensidade, sprintando);
        }

        private float VelocidadeSemBola(float intensidade, bool sprintando)
        {
            if (sprintando) return config.velocidadeSprint;
            if (entradaDevagar) return config.velocidadeAndar * Mathf.Min(1f, intensidade / config.limiteAnalogicoAndar);

            // Analógico leve: anda; empurrando mais: vai acelerando até correr
            float limite = config.limiteAnalogicoAndar;
            if (intensidade <= limite) return config.velocidadeAndar * intensidade / limite;
            float t = (intensidade - limite) / (1f - limite);
            return Mathf.Lerp(config.velocidadeAndar, config.velocidadeCorrer, t);
        }

        private void AtualizarEstamina(bool sprintando, float dt)
        {
            if (sprintando)
                Estamina = Mathf.Max(0f, Estamina - dt / Mathf.Max(0.01f, config.duracaoSprint));
            else
                Estamina = Mathf.Min(1f, Estamina + dt / Mathf.Max(0.01f, config.recargaSprint));
        }

        /// <summary>Impede o jogador de passar da linha que fica em 'ponto', no sentido 'direcao'.</summary>
        public void DefinirLimiteFrente(Vector3 ponto, Vector3 direcao)
        {
            direcao.y = 0f;
            if (direcao.sqrMagnitude < 0.0001f) { RemoverLimiteFrente(); return; }
            temLimiteFrente = true;
            pontoLimite = ponto;
            direcaoLimite = direcao.normalized;
        }

        public void RemoverLimiteFrente() => temLimiteFrente = false;

        /// <summary>Corta a velocidade que levaria o jogador para além da linha do limite.</summary>
        private void AplicarLimiteFrente(float dt)
        {
            if (!temLimiteFrente || dt <= 0f) return;
            float folga = Vector3.Dot(pontoLimite - transform.position, direcaoLimite); // quanto falta até a linha
            float avanco = Vector3.Dot(velocidade, direcaoLimite) * dt;
            if (avanco > Mathf.Max(0f, folga))
                velocidade -= direcaoLimite * ((avanco - Mathf.Max(0f, folga)) / dt);
        }

        /// <summary>Vira o corpo, na hora, para uma direção no chão.</summary>
        public void OlharPara(Vector3 direcao)
        {
            direcao.y = 0f;
            if (direcao.sqrMagnitude > 0.0001f) transform.rotation = Quaternion.LookRotation(direcao, Vector3.up);
        }

        /// <summary>True se o jogador está na zona segura da garrafa em 'posicaoGarrafa' (ver ZonaDoTaco).</summary>
        public bool DentroDoCirculoDe(Vector3 posicaoGarrafa)
        {
            return ZonaDoTaco.NaZonaSegura(transform.position, posicaoGarrafa, config);
        }

        /// <summary>True se o jogador está na zona segura de alguma base.</summary>
        public bool DentroDoCirculo()
        {
            return ZonaDoTaco.NaZonaSeguraDeAlguma(transform.position, config);
        }
    }
}
