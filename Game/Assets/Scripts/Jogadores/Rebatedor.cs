using System;
using System.Collections.Generic;
using UnityEngine;

namespace BetsDeRua
{
    /// <summary>Os três jeitos de rebater.</summary>
    /// <summary>
    /// Forte = taco alto (bola no ar); Varrida = taco baixo (bola rasteira/baixa);
    /// Cavadinha = por cima (R1 + R2); Toque = curto (só a CPU usa).
    /// </summary>
    public enum TipoBatida { Forte, Toque, Cavadinha, Varrida }

    /// <summary>
    /// A batida do rebatedor que recebe o arremesso.
    /// O "momento certo" é quando a bola cruza a linha de contato: um plano de frente para
    /// o arremesso, um pouco à frente do rebatedor (a bola é rebatida antes de chegar na garrafa). A trajetória é prevista a cada frame, então
    /// se ele andar, o momento e o "X" de giz se ajustam.
    /// Controles: R2 forte (segura e solta), R1 toque, L2 cavadinha; analógico direito
    /// (ou mouse) dá a direção. Sem ninguém controlando, a CPU rebate (com erro de tempo).
    /// </summary>
    [RequireComponent(typeof(Jogador))]
    public class Rebatedor : MonoBehaviour
    {
        [SerializeField] private ConfigFisica config;
        [SerializeField] private ConfigPartida configPartida;
        [SerializeField] private Taco taco;
        [SerializeField] private Bola bola;
        [SerializeField] private AjudasBatida ajudas;
        [SerializeField] private ArcoForca arco;

        /// <summary>Avisa (tipo, km/h, erro em ms (+ = atrasou), perfeita?).</summary>
        public event Action<TipoBatida, float, float, bool> Rebateu;
        /// <summary>Avisa que o taco passou no vazio (motivo, erro em ms; NaN = fora do alcance).</summary>
        public event Action<string, float> Errou;
        /// <summary>Acertou a bola com o taco na altura errada: espirrou (motivo).</summary>
        public event Action<string> Espirrou;

        private Jogador jogador;
        private Camera cameraPrincipal;
        private readonly List<Vector3> previsao = new List<Vector3>();

        // Estado de um arremesso
        private bool jaGirou;
        private bool cruzou;              // a bola já passou da linha do rebatedor
        private float momentoArremesso;   // quando o arremesso saiu da mão
        private float momentoIdeal;       // Time.time do cruzamento (previsto, ou real depois que cruzou)
        private bool alcancavel;          // no cruzamento, a bola fica ao alcance do taco?
        private bool temCruzamento;       // a previsão encontrou um cruzamento
        private Vector3 pontoCruzamento;

        // Batida esperando a bola chegar (quem bate cedo, mas dentro da janela)
        private bool batidaPendente;
        private TipoBatida tipoPendente;
        private float cargaPendente;
        private float erroPendenteMs;

        private bool estavaAtivo;

        // Controles
        private float carga;
        private bool segurandoForte;

        // CPU
        private float erroCpuMs;
        private TipoBatida tipoCpu;
        private float cargaCpu;
        private Vector3 direcaoCpu;
        private bool tipoCpuDefinido;
        private TipoBatida tipoSegurado = TipoBatida.Forte;

        /// <summary>True enquanto este rebatedor pode rebater (é ele quem recebe o arremesso).</summary>
        public bool Ativo { get; set; }
        /// <summary>True depois que o arremesso saiu da mão (até ser rebatido ou passar).</summary>
        public bool BolaEmJogo { get; private set; }
        /// <summary>Direção (no chão) em que o arremesso vem.</summary>
        public Vector3 DirecaoChegada { get; private set; } = Vector3.left;
        public Jogador Jogador => jogador;
        /// <summary>True se é a CPU quem rebate (ninguém controla e o bot está ligado).</summary>
        public bool ControladoPelaCpu => jogador.Controlador == null && configPartida.rebatedorAutomatico;

        private void Awake()
        {
            jogador = GetComponent<Jogador>();
        }

        /// <summary>Começo de um lance: espera o arremesso.</summary>
        public void PrepararParaArremesso()
        {
            Ativo = true;
            BolaEmJogo = false;
            jaGirou = false;
            cruzou = false;
            batidaPendente = false;
            carga = 0f;
        }

        /// <summary>O arremesso saiu da mão, vindo na direção 'velocidadeArremesso'.</summary>
        public void ArremessoSaiu(Vector3 velocidadeArremesso)
        {
            BolaEmJogo = true;
            momentoArremesso = Time.time;
            Vector3 horizontal = new Vector3(velocidadeArremesso.x, 0f, velocidadeArremesso.z);
            if (horizontal.sqrMagnitude > 0.0001f) DirecaoChegada = horizontal.normalized;
            SortearJogadaDaCpu();
        }

        /// <summary>O lance acabou para este rebatedor (rebateu, a bola passou ou ficou morta).</summary>
        public void Encerrar()
        {
            Ativo = false;
            BolaEmJogo = false;
            batidaPendente = false;
        }

        private void Update()
        {
            if (cameraPrincipal == null) cameraPrincipal = Camera.main;
            jogador.GatilhosOcupados = Ativo;
            if (!Ativo)
            {
                // As ajudas e o arco são divididos entre os rebatedores: só esconde
                // na hora em que este deixa de ser o ativo, para não apagar as do outro
                if (estavaAtivo)
                {
                    ajudas.EsconderTudo();
                    arco.Esconder();
                    estavaAtivo = false;
                }
                return;
            }
            estavaAtivo = true;

            ajudas.MostrarAlcance(transform.position);
            if (BolaEmJogo) AtualizarCruzamento();
            AtualizarAjudasDaBola();

            if (jogador.Controlador != null && jogador.Controlador.Conectado) LerControles(jogador.Controlador);
            else
            {
                arco.Esconder();
                if (ControladoPelaCpu) JogarComoCpu();
            }

            // Batida adiantada (dentro da janela): acontece quando a bola chega na linha
            if (batidaPendente && cruzou) AplicarBatida(tipoPendente, cargaPendente, erroPendenteMs);

            // A bola passou e a janela fechou sem batida: acabou a chance neste arremesso.
            // Também acaba se a bola parou ou demorou demais (ex.: o rebatedor saiu da frente e a
            // bola nunca cruzou a linha dele): senão a defesa ficaria esperando a batida para sempre
            bool desistiu = !batidaPendente && (bola.Parada || Time.time - momentoArremesso > config.tempoMaximoEsperaBatida);
            if (BolaEmJogo && (desistiu || cruzou && !batidaPendente && Time.time > momentoIdeal + config.janelaBoaMs / 1000f))
            {
                BolaEmJogo = false;
                ajudas.EsconderMarca();
                ajudas.EsconderAnel();
            }
        }

        /// <summary>Ponto da linha de contato: um pouco à frente do rebatedor, rumo ao arremesso.</summary>
        private Vector3 PontoDaLinhaDeContato => transform.position - DirecaoChegada * config.avancoContatoBatida;

        /// <summary>Prevê quando e onde a bola cruza a linha de contato.</summary>
        private void AtualizarCruzamento()
        {
            Vector3 normal = DirecaoChegada;
            Vector3 linha = PontoDaLinhaDeContato;
            float lado = Vector3.Dot(bola.Posicao - linha, normal);
            if (cruzou) return;
            if (lado >= 0f)
            {
                // Acabou de cruzar: congela o momento real
                cruzou = true;
                momentoIdeal = Time.time;
                return;
            }

            bola.PreverTrajetoria(config.tempoPrevisao, config.passoPrevisao, previsao);
            temCruzamento = false;
            for (int i = 1; i < previsao.Count; i++)
            {
                float antes = Vector3.Dot(previsao[i - 1] - linha, normal);
                float depois = Vector3.Dot(previsao[i] - linha, normal);
                if (antes < 0f && depois >= 0f)
                {
                    // Interpola dentro do passo para achar o instante exato
                    float fracao = antes / (antes - depois);
                    pontoCruzamento = Vector3.Lerp(previsao[i - 1], previsao[i], fracao);
                    momentoIdeal = Time.time + (i - 1 + fracao) * config.passoPrevisao;
                    Vector3 lateral = pontoCruzamento - linha; // distância para o lado, na linha de contato
                    lateral.y = 0f;
                    alcancavel = lateral.magnitude <= config.alcanceTaco
                                 && pontoCruzamento.y <= config.alturaMaximaAlcance;
                    temCruzamento = true;
                    break;
                }
            }
        }

        /// <summary>Leitura da bola: "BAIXA" ou "NO AR" em cima do X de onde ela vai passar (só para quem joga).</summary>
        private void OnGUI()
        {
            if (!Ativo || !BolaEmJogo || cruzou || !temCruzamento || jogador.Controlador == null || cameraPrincipal == null) return;
            Vector3 tela = cameraPrincipal.WorldToScreenPoint(new Vector3(pontoCruzamento.x, 0f, pontoCruzamento.z));
            if (tela.z <= 0f) return;
            bool baixa = ChegaBaixa(pontoCruzamento.y);
            GUIStyle estilo = new GUIStyle(GUI.skin.label) { fontSize = 20, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            estilo.normal.textColor = baixa ? new Color(0.4f, 0.8f, 1f) : new Color(1f, 0.85f, 0.1f);
            string texto = baixa ? "BAIXA (L2)" : "NO AR (R2)";
            GUI.Label(new Rect(tela.x - 80f, Screen.height - tela.y + 8f, 160f, 30f), texto, estilo);
        }

        private void AtualizarAjudasDaBola()
        {
            if (!BolaEmJogo || cruzou || !temCruzamento)
            {
                ajudas.EsconderMarca();
                ajudas.EsconderAnel();
                return;
            }
            ajudas.MostrarMarca(pontoCruzamento);
            if (alcancavel && !jaGirou) ajudas.MostrarAnel(bola.transform.position, momentoIdeal - Time.time);
            else ajudas.EsconderAnel();
        }

        /// <summary>
        /// Leitura da bola (pesquisa: beisebol e críquete): além do tempo, quem rebate escolhe a altura do taco.
        /// R2 = taco alto (bola no ar), L2 = taco baixo (bola rasteira/baixa); segurar enche a força, soltar bate.
        /// Segurando R1 ao soltar o R2 = cavadinha (por cima do defensor).
        /// </summary>
        private void LerControles(ControleHumano humano)
        {
            bool alto = humano.SegurandoGatilhoForte;
            bool baixo = humano.SegurandoGatilhoPorCima;
            if ((alto || baixo) && !jaGirou)
            {
                if (!segurandoForte) tipoSegurado = alto ? TipoBatida.Forte : TipoBatida.Varrida;
                segurandoForte = true;
                carga = Mathf.Min(1f, carga + Time.deltaTime / Mathf.Max(0.05f, config.tempoEncherBatida));
                arco.Mostrar(transform.position, carga, false);
            }
            else
            {
                arco.Esconder();
                if (segurandoForte)
                {
                    segurandoForte = false;
                    TipoBatida tipo = tipoSegurado;
                    if (tipo == TipoBatida.Forte && humano.SegurandoGatilhoCurto) tipo = TipoBatida.Cavadinha;
                    // Soltou antes do arremesso sair: não conta, só zera a força
                    if (BolaEmJogo) Girar(tipo, carga);
                    carga = 0f;
                }
            }
        }

        /// <summary>A bola chega baixa (rasteira) neste ponto? Baixa pede taco baixo; no ar, taco alto.</summary>
        private bool ChegaBaixa(float altura) => altura <= config.alturaLimiteTacoBaixo;

        /// <summary>Este tipo de batida serve para a bola nesta altura? Perto do limite, os dois servem.</summary>
        private bool AlturaCerta(TipoBatida tipo, float altura)
        {
            float limite = config.alturaLimiteTacoBaixo, folga = config.toleranciaAlturaTaco;
            switch (tipo)
            {
                case TipoBatida.Varrida: return altura <= limite + folga;
                case TipoBatida.Toque: return true;
                default: return altura >= limite - folga;
            }
        }

        private void SortearJogadaDaCpu()
        {
            float e = NiveisBots.De(configPartida, jogador).erroBatidaMs;
            erroCpuMs = UnityEngine.Random.Range(-e, e);
            // O tipo (altura do taco) é escolhido quando a bola está chegando (ver JogarComoCpu)
            tipoCpuDefinido = false;
            cargaCpu = UnityEngine.Random.Range(0.5f, 1f);
            // Rebate para a frente, ao longo da rua, abrindo um pouco para os lados (longe de quem arremessou,
            // sem mandar direto nas fachadas)
            float abertura = UnityEngine.Random.Range(configPartida.aberturaMinimaBatidaBot, configPartida.aberturaMaximaBatidaBot)
                             * (UnityEngine.Random.value < 0.5f ? -1f : 1f);
            direcaoCpu = Quaternion.AngleAxis(abertura, Vector3.up) * -DirecaoChegada;
        }

        private void JogarComoCpu()
        {
            if (!BolaEmJogo || jaGirou || !temCruzamento && !cruzou) return;
            if (!alcancavel) return; // a CPU não tenta bola fora do alcance
            if (!tipoCpuDefinido && temCruzamento)
            {
                // Lê a altura em que a bola vai chegar; acerta a leitura conforme o nível
                bool leuCerto = UnityEngine.Random.value < NiveisBots.De(configPartida, jogador).acertoAlturaBatida;
                bool baixa = ChegaBaixa(pontoCruzamento.y);
                if (!leuCerto) baixa = !baixa;
                tipoCpu = baixa ? TipoBatida.Varrida : UnityEngine.Random.value < 0.25f ? TipoBatida.Cavadinha : TipoBatida.Forte;
                tipoCpuDefinido = true;
            }
            if (Time.time >= momentoIdeal + erroCpuMs / 1000f) Girar(tipoCpu, cargaCpu);
        }

        /// <summary>O taco gira: decide se acertou, errou ou se a batida espera a bola chegar.</summary>
        private void Girar(TipoBatida tipo, float cargaBatida)
        {
            if (jaGirou) return;
            jaGirou = true;
            taco.Girar();

            if (!temCruzamento && !cruzou)
            {
                Errou?.Invoke("Bateu no vazio", float.NaN);
                return;
            }
            float erroMs = (Time.time - momentoIdeal) * 1000f; // + = atrasou, - = adiantou
            if (!alcancavel)
            {
                Errou?.Invoke("Fora do alcance", erroMs);
                return;
            }
            if (Mathf.Abs(erroMs) > config.janelaBoaMs)
            {
                Errou?.Invoke(erroMs > 0f ? "Atrasou" : "Adiantou", erroMs);
                return;
            }

            if (cruzou) AplicarBatida(tipo, cargaBatida, erroMs);
            else
            {
                // Adiantado, mas dentro da janela: a bola é rebatida quando chegar na linha
                batidaPendente = true;
                tipoPendente = tipo;
                cargaPendente = cargaBatida;
                erroPendenteMs = erroMs;
            }
        }

        private void AplicarBatida(TipoBatida tipo, float cargaBatida, float erroMs)
        {
            batidaPendente = false;
            bool perfeita = Mathf.Abs(erroMs) <= config.janelaPerfeitaMs;

            // Fora do perfeito: mais fraca e um pouco torta, quanto mais longe do momento certo
            float fator = perfeita ? 1f : config.fatorBatidaBoa;
            float torto = perfeita ? 0f
                : UnityEngine.Random.Range(-1f, 1f) * config.erroDirecaoBatidaBoa
                  * Mathf.InverseLerp(config.janelaPerfeitaMs, config.janelaBoaMs, Mathf.Abs(erroMs));

            // Taco na altura errada: a bola espirra (vai fraca para trás ou para o lado), como na rua
            float alturaContato = bola.Posicao.y;
            if (!AlturaCerta(tipo, alturaContato))
            {
                Espirrar(tipo, alturaContato, erroMs);
                return;
            }

            float kmh, angulo;
            switch (tipo)
            {
                case TipoBatida.Varrida:
                    kmh = Mathf.Lerp(config.batidaVarridaMinimaKmh, config.batidaVarridaMaximaKmh, cargaBatida);
                    angulo = config.anguloBatidaVarrida; break;
                case TipoBatida.Toque:
                    kmh = config.batidaToqueKmh; angulo = config.anguloBatidaToque; break;
                case TipoBatida.Cavadinha:
                    kmh = config.batidaCavadinhaKmh; angulo = config.anguloBatidaCavadinha; break;
                default:
                    kmh = Mathf.Lerp(config.batidaForteMinimaKmh, config.batidaForteMaximaKmh, cargaBatida);
                    angulo = config.anguloBatidaForte; break;
            }
            kmh *= fator;

            Vector3 direcao = ParaAFrente(Quaternion.AngleAxis(torto, Vector3.up) * DirecaoDaBatida());
            float rapidez = kmh / 3.6f;
            float radianos = angulo * Mathf.Deg2Rad;
            Vector3 velocidade = direcao * (rapidez * Mathf.Cos(radianos)) + Vector3.up * (rapidez * Mathf.Sin(radianos));
            // Sai pelo menos do chão, para a bola não "grudar" rolando dentro do quique
            Vector3 saida = bola.Posicao;
            saida.y = Mathf.Max(saida.y, config.raioBola * 2f);
            bola.Lancar(saida, velocidade, jogador);

            BolaEmJogo = false;
            ajudas.EsconderMarca();
            ajudas.EsconderAnel();
            Rebateu?.Invoke(tipo, kmh, erroMs, perfeita);
        }

        /// <summary>Taco na altura errada: a bola espirra fraca para trás ou para o lado.</summary>
        private void Espirrar(TipoBatida tipo, float altura, float erroMs)
        {
            float abertura = UnityEngine.Random.Range(-config.aberturaEspirrada, config.aberturaEspirrada);
            Vector3 direcao = Quaternion.AngleAxis(abertura, Vector3.up) * DirecaoChegada; // segue para trás
            float rapidez = UnityEngine.Random.Range(config.espirradaMinimaKmh, config.espirradaMaximaKmh) / 3.6f;
            Vector3 velocidade = direcao * rapidez + Vector3.up * config.subidaEspirrada;
            Vector3 saida = bola.Posicao;
            saida.y = Mathf.Max(saida.y, config.raioBola * 2f);
            bola.Lancar(saida, velocidade, jogador);

            BolaEmJogo = false;
            ajudas.EsconderMarca();
            ajudas.EsconderAnel();
            Rebateu?.Invoke(tipo, rapidez * 3.6f, erroMs, false);
            string qual = tipo == TipoBatida.Varrida ? "taco baixo numa bola alta" : "taco alto numa bola baixa";
            Espirrou?.Invoke($"Espirrou! ({qual})");
        }

        /// <summary>
        /// A batida vai sempre para a frente (para o lado de quem arremessou), abrindo no máximo
        /// anguloMaximoBatida para os lados: rebater para trás não é jogada.
        /// </summary>
        private Vector3 ParaAFrente(Vector3 direcao)
        {
            Vector3 frente = -DirecaoChegada;
            frente.y = 0f;
            direcao.y = 0f;
            if (frente.sqrMagnitude < 0.0001f || direcao.sqrMagnitude < 0.0001f) return frente.normalized;
            float angulo = Vector3.SignedAngle(frente, direcao, Vector3.up);
            float limitado = Mathf.Clamp(angulo, -config.anguloMaximoBatida, config.anguloMaximoBatida);
            return Quaternion.AngleAxis(limitado, Vector3.up) * frente.normalized;
        }

        /// <summary>Para onde a bola vai: analógico direito / mouse; sem nada, de volta pela rua.</summary>
        private Vector3 DirecaoDaBatida()
        {
            ControleHumano humano = jogador.Controlador;
            if (humano == null) return direcaoCpu;

            if (humano.MiraPeloMouse)
            {
                if (humano.PontoDoMouseNoChao(cameraPrincipal, out Vector3 noChao))
                {
                    Vector3 rumo = noChao - transform.position;
                    rumo.y = 0f;
                    if (rumo.sqrMagnitude > 0.25f) return rumo.normalized;
                }
            }
            else
            {
                Vector2 analogico = humano.MiraAnalogica;
                if (analogico.magnitude > 0.3f) return new Vector3(analogico.x, 0f, analogico.y).normalized;
            }
            return -DirecaoChegada;
        }
    }
}
