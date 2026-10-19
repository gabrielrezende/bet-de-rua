using System;
using System.Collections.Generic;
using UnityEngine;

namespace BetsDeRua
{
    /// <summary>Os três jeitos de rebater.</summary>
    public enum TipoBatida { Forte, Toque, Cavadinha }

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

        private Jogador jogador;
        private Camera cameraPrincipal;
        private readonly List<Vector3> previsao = new List<Vector3>();

        // Estado de um arremesso
        private bool jaGirou;
        private bool cruzou;              // a bola já passou da linha do rebatedor
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
        private bool curtoAntes, porCimaAntes;

        // CPU
        private float erroCpuMs;
        private TipoBatida tipoCpu;
        private float cargaCpu;
        private Vector3 direcaoCpu;

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

            // A bola passou e a janela fechou sem batida: acabou a chance neste arremesso
            if (BolaEmJogo && cruzou && !batidaPendente && Time.time > momentoIdeal + config.janelaBoaMs / 1000f)
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

        private void LerControles(ControleHumano humano)
        {
            // R2: segura enche a força, solta bate
            if (humano.SegurandoGatilhoForte && !jaGirou)
            {
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
                    // Soltou antes do arremesso sair: não conta, só zera a força
                    if (BolaEmJogo) Girar(TipoBatida.Forte, carga);
                    carga = 0f;
                }
            }

            // R1 e L2: batem na hora em que aperta
            bool curto = humano.SegurandoGatilhoCurto;
            bool porCima = humano.SegurandoGatilhoPorCima;
            if (BolaEmJogo && curto && !curtoAntes) Girar(TipoBatida.Toque, 0f);
            else if (BolaEmJogo && porCima && !porCimaAntes) Girar(TipoBatida.Cavadinha, 0f);
            curtoAntes = curto;
            porCimaAntes = porCima;
        }

        private void SortearJogadaDaCpu()
        {
            float e = configPartida.erroTempoRebatedorAutomaticoMs;
            erroCpuMs = UnityEngine.Random.Range(-e, e);
            float sorteio = UnityEngine.Random.value;
            tipoCpu = sorteio < 0.6f ? TipoBatida.Forte : sorteio < 0.8f ? TipoBatida.Toque : TipoBatida.Cavadinha;
            cargaCpu = UnityEngine.Random.Range(0.5f, 1f);
            // Rebate para a frente, abrindo até 60° para os lados
            direcaoCpu = Quaternion.AngleAxis(UnityEngine.Random.Range(-60f, 60f), Vector3.up) * -DirecaoChegada;
        }

        private void JogarComoCpu()
        {
            if (!BolaEmJogo || jaGirou || !temCruzamento && !cruzou) return;
            if (!alcancavel) return; // a CPU não tenta bola fora do alcance
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

            float kmh, angulo;
            switch (tipo)
            {
                case TipoBatida.Toque:
                    kmh = config.batidaToqueKmh; angulo = config.anguloBatidaToque; break;
                case TipoBatida.Cavadinha:
                    kmh = config.batidaCavadinhaKmh; angulo = config.anguloBatidaCavadinha; break;
                default:
                    kmh = Mathf.Lerp(config.batidaForteMinimaKmh, config.batidaForteMaximaKmh, cargaBatida);
                    angulo = config.anguloBatidaForte; break;
            }
            kmh *= fator;

            Vector3 direcao = Quaternion.AngleAxis(torto, Vector3.up) * DirecaoDaBatida();
            float rapidez = kmh / 3.6f;
            float radianos = angulo * Mathf.Deg2Rad;
            Vector3 velocidade = direcao * (rapidez * Mathf.Cos(radianos)) + Vector3.up * (rapidez * Mathf.Sin(radianos));
            // Sai pelo menos do chão, para a bola não "grudar" rolando dentro do quique
            Vector3 saida = bola.Posicao;
            saida.y = Mathf.Max(saida.y, config.raioBola * 2f);
            bola.Lancar(saida, velocidade);

            BolaEmJogo = false;
            ajudas.EsconderMarca();
            ajudas.EsconderAnel();
            Rebateu?.Invoke(tipo, kmh, erroMs, perfeita);
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
