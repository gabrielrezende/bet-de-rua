using System;
using UnityEngine;

namespace BetsDeRua
{
    /// <summary>Os jeitos de soltar a bola da mão.</summary>
    public enum TipoArremesso { Forte, Quicado, Pingo, Passe, Lancamento }

    /// <summary>Em que situação o jogador está com a bola.</summary>
    public enum ModoComBola
    {
        /// <summary>Arremesso do começo do lance: R2 forte, R1 quicado, L2 pingo na garrafa alvo.</summary>
        Arremesso,
        /// <summary>Defensor no campo: R2 chuta na garrafa, R1 passa, L2 lança por cima para o colega.</summary>
        Campo
    }

    /// <summary>
    /// O jogador da defesa com a bola na mão (docs/REFERENCIAS_FIFA.md, seções 3, 9.2 e 9.3),
    /// no esquema "twin-stick": o analógico esquerdo anda (4 m/s, sem sprint), o direito
    /// (ou o mouse) mira em volta da garrafa alvo, e os gatilhos soltam a bola.
    /// Forte, quicado e pingo enchem o arco de força (soltar na faixa verde); passe e
    /// lançamento saem num toque, direto no colega.
    /// No arremesso, sem ninguém controlando, a CPU arremessa sozinha; no campo, quem
    /// manda no bot é o CerebroBots (pelo ExecutarPeloBot).
    /// </summary>
    [RequireComponent(typeof(Jogador))]
    public class Arremessador : MonoBehaviour
    {
        [SerializeField] private ConfigFisica config;
        [SerializeField] private ConfigPartida configPartida;
        [Tooltip("Mira no chão (dividida entre os defensores: só quem está com a bola usa).")]
        [SerializeField] private MiraNoChao mira;
        [Tooltip("Arco de força (dividido entre os defensores).")]
        [SerializeField] private ArcoForca arco;

        private Jogador jogador;
        private Camera cameraPrincipal;
        private float carga;            // 0 a 1: quanto o arco encheu
        private bool carregando;
        private TipoArremesso tipoCarregando;
        private Vector3 pontoMira;
        private float esperaCpu;
        private bool mostrandoVisuais;  // para esconder a mira e o arco só uma vez, ao soltar a bola

        /// <summary>Avisa (quem, posição de saída, velocidade da bola, tipo, km/h na horizontal).</summary>
        public event Action<Arremessador, Vector3, Vector3, TipoArremesso, float> Arremessou;

        /// <summary>True enquanto a bola está na mão deste jogador.</summary>
        public bool ComBola { get; private set; }
        public ModoComBola Modo { get; private set; }
        /// <summary>Garrafa que ele está mirando.</summary>
        public Garrafa Alvo { get; set; }
        /// <summary>Colega para o passe e o lançamento (no campo).</summary>
        public Jogador Parceiro { get; set; }
        /// <summary>Garrafa da base dele: no arremesso, não pode passar à frente dela.</summary>
        public Garrafa GarrafaPropria { get; set; }
        public Jogador Jogador => jogador;

        /// <summary>Onde a bola fica na mão (e de onde sai).</summary>
        public Vector3 PosicaoMao => transform.position
                                     + Vector3.up * config.alturaSoltura
                                     + transform.forward * config.avancoSoltura
                                     + transform.right * config.ladoSoltura;

        private void Awake()
        {
            jogador = GetComponent<Jogador>();
        }

        /// <summary>Dá a bola para este jogador.</summary>
        public void ReceberBola(ModoComBola modo)
        {
            ComBola = true;
            Modo = modo;
            carregando = false;
            carga = 0f;
            esperaCpu = 0f;
            if (Alvo != null) pontoMira = Alvo.transform.position;
        }

        /// <summary>Tira a bola da mão sem arremessar (ex.: o lance acabou).</summary>
        public void TirarBola()
        {
            ComBola = false;
            carregando = false;
            jogador.ComBolaNaMao = false;
            jogador.RemoverLimiteFrente();
            EsconderVisuais();
        }

        private void Update()
        {
            if (cameraPrincipal == null) cameraPrincipal = Camera.main;
            jogador.ComBolaNaMao = ComBola;
            AtualizarLimite();

            ControleHumano humano = jogador.Controlador;
            bool humanoJogando = humano != null && humano.Conectado;
            if (!ComBola || Alvo == null || !humanoJogando)
            {
                // Sem ninguém (ou controle desconectado no meio): cancela a mira e a carga, sem soltar a bola
                carregando = false;
                carga = 0f;
                EsconderVisuais();
                if (ComBola && Alvo != null && humano == null && Modo == ModoComBola.Arremesso) ArremessarComoCpu();
                else esperaCpu = 0f;
                return;
            }
            esperaCpu = 0f;

            AtualizarMira(humano);
            AtualizarCarga(humano);
        }

        private void LateUpdate()
        {
            // Depois do movimento do frame: o corpo fica sempre virado para a mira
            if (ComBola && Alvo != null && jogador.Controlador != null && jogador.Controlador.Conectado)
            {
                jogador.OlharPara(pontoMira - transform.position);
                mira.Mostrar(PosicaoMao, pontoMira, Alvo.transform.position);
                mostrandoVisuais = true;
            }
        }

        private void EsconderVisuais()
        {
            if (!mostrandoVisuais) return;
            mira.Esconder();
            arco.Esconder();
            mostrandoVisuais = false;
        }

        /// <summary>No arremesso, o jogador não passa à frente da própria garrafa (rumo ao alvo).</summary>
        private void AtualizarLimite()
        {
            if (!ComBola || Modo != ModoComBola.Arremesso || Alvo == null || GarrafaPropria == null)
            {
                jogador.RemoverLimiteFrente();
                return;
            }
            Vector3 rumoAoAlvo = Alvo.transform.position - GarrafaPropria.transform.position;
            rumoAoAlvo.y = 0f;
            Vector3 linha = GarrafaPropria.transform.position + rumoAoAlvo.normalized * config.avancoMaximoComBola;
            jogador.DefinirLimiteFrente(linha, rumoAoAlvo);
        }

        private void AtualizarMira(ControleHumano humano)
        {
            Vector3 centro = Alvo.transform.position;
            if (humano.MiraPeloMouse)
            {
                // Mouse apontando para o céu: mira no meio da garrafa
                pontoMira = centro;
                if (humano.PontoDoMouseNoChao(cameraPrincipal, out Vector3 noChao))
                {
                    Vector3 desvio = Vector3.ClampMagnitude(new Vector3(noChao.x - centro.x, 0f, noChao.z - centro.z),
                                                            config.raioMira);
                    pontoMira = centro + desvio;
                }
            }
            else
            {
                // Analógico direito solto = mira no meio da garrafa; empurrado = até raioMira para o lado
                Vector2 analogico = Vector2.ClampMagnitude(humano.MiraAnalogica, 1f);
                pontoMira = centro + new Vector3(analogico.x, 0f, analogico.y) * config.raioMira;
            }
            pontoMira.y = 0f;
        }

        private void AtualizarCarga(ControleHumano humano)
        {
            bool segurando = BotaoSegurado(humano, out TipoArremesso tipoSegurado);

            if (!carregando && segurando)
            {
                carregando = true;
                tipoCarregando = tipoSegurado;
                carga = 0f;
            }

            if (!carregando)
            {
                arco.Esconder();
                return;
            }

            bool usaArco = tipoCarregando != TipoArremesso.Passe && tipoCarregando != TipoArremesso.Lancamento;
            if (BotaoDoTipo(humano, tipoCarregando))
            {
                carga = Mathf.Min(1f, carga + Time.deltaTime / Mathf.Max(0.05f, config.tempoEncherForca));
                if (usaArco)
                {
                    arco.Mostrar(transform.position, carga);
                    mostrandoVisuais = true;
                }
                return;
            }

            // Soltou o botão: solta a bola
            carregando = false;
            arco.Esconder();
            Soltar(tipoCarregando, carga);
        }

        /// <summary>
        /// Ninguém no arremessador: a CPU espera um pouco, mira perto da garrafa (com um
        /// desvio ao acaso) e arremessa um tipo sorteado, soltando dentro da faixa verde.
        /// </summary>
        private void ArremessarComoCpu()
        {
            if (!configPartida.arremessadorAutomatico) return;
            jogador.OlharPara(Alvo.transform.position - transform.position);
            esperaCpu += Time.deltaTime;
            if (esperaCpu < configPartida.esperaArremessoAutomatico) return;
            esperaCpu = 0f;

            float desvio = UnityEngine.Random.Range(-configPartida.desvioMiraAutomatico, configPartida.desvioMiraAutomatico);
            float sorteio = UnityEngine.Random.value;
            TipoArremesso tipo = sorteio < 0.5f ? TipoArremesso.Forte : sorteio < 0.75f ? TipoArremesso.Quicado : TipoArremesso.Pingo;
            ExecutarPeloBot(tipo, UnityEngine.Random.Range(config.inicioFaixaVerde, config.fimFaixaVerde),
                            Alvo.transform.position + Vector3.forward * desvio);
        }

        /// <summary>Um bot solta a bola: tipo, força do arco e ponto mirado no chão.</summary>
        public void ExecutarPeloBot(TipoArremesso tipo, float cargaBot, Vector3 pontoMirado)
        {
            if (!ComBola) return;
            pontoMira = new Vector3(pontoMirado.x, 0f, pontoMirado.z);
            jogador.OlharPara(pontoMira - transform.position);
            Soltar(tipo, cargaBot);
        }

        private bool BotaoSegurado(ControleHumano humano, out TipoArremesso tipo)
        {
            bool campo = Modo == ModoComBola.Campo;
            if (humano.SegurandoGatilhoForte) { tipo = TipoArremesso.Forte; return true; }
            if (humano.SegurandoGatilhoCurto) { tipo = campo ? TipoArremesso.Passe : TipoArremesso.Quicado; return true; }
            if (humano.SegurandoGatilhoPorCima) { tipo = campo ? TipoArremesso.Lancamento : TipoArremesso.Pingo; return true; }
            tipo = TipoArremesso.Forte;
            return false;
        }

        private static bool BotaoDoTipo(ControleHumano humano, TipoArremesso tipo)
        {
            switch (tipo)
            {
                case TipoArremesso.Quicado:
                case TipoArremesso.Passe: return humano.SegurandoGatilhoCurto;
                case TipoArremesso.Pingo:
                case TipoArremesso.Lancamento: return humano.SegurandoGatilhoPorCima;
                default: return humano.SegurandoGatilhoForte;
            }
        }

        private void Soltar(TipoArremesso tipo, float cargaFinal)
        {
            Vector3 saida = PosicaoMao;
            bool passeParaColega = tipo == TipoArremesso.Passe || tipo == TipoArremesso.Lancamento;

            // Passe e lançamento vão direto no colega; o resto vai no ponto mirado
            Vector3 destino = passeParaColega && Parceiro != null ? Parceiro.transform.position : pontoMira;
            Vector3 horizontal = destino - saida;
            horizontal.y = 0f;
            float distancia = Mathf.Max(0.5f, horizontal.magnitude);
            Vector3 direcao = horizontal.sqrMagnitude > 0.0001f ? horizontal.normalized : transform.forward;

            // Velocidade na horizontal: o arco enche até o fim da faixa verde (passes: fixa)
            float fracao = Mathf.Clamp01(cargaFinal / Mathf.Max(0.01f, config.fimFaixaVerde));
            float kmh;
            switch (tipo)
            {
                case TipoArremesso.Quicado: kmh = Mathf.Lerp(config.quicadoMinimaKmh, config.quicadoMaximaKmh, fracao); break;
                case TipoArremesso.Pingo: kmh = Mathf.Lerp(config.pingoMinimaKmh, config.pingoMaximaKmh, fracao); break;
                case TipoArremesso.Passe: kmh = config.passeKmh; break;
                case TipoArremesso.Lancamento: kmh = config.lancamentoKmh; break;
                default: kmh = Mathf.Lerp(config.forteMinimaKmh, config.forteMaximaKmh, fracao); break;
            }
            float rapidezHorizontal = kmh / 3.6f;

            // Quanto a bola precisa subir para chegar no destino, conforme o tipo
            float meioGarrafa = config.alturaGarrafa * 0.5f;
            float subida;
            switch (tipo)
            {
                case TipoArremesso.Quicado: // quica no chão antes e chega no meio da garrafa
                    subida = Trajetoria.AcharSubidaQuicado(config, distancia, rapidezHorizontal, saida.y,
                                                           meioGarrafa, config.distanciaQuique);
                    break;
                case TipoArremesso.Pingo: // alto, cai por cima
                    subida = Trajetoria.AcharSubida(config, distancia, rapidezHorizontal, config.alturaChegadaPingo - saida.y);
                    break;
                case TipoArremesso.Passe:
                    subida = Trajetoria.AcharSubida(config, distancia, rapidezHorizontal, config.alturaChegadaPasse - saida.y);
                    break;
                case TipoArremesso.Lancamento:
                    subida = Trajetoria.AcharSubida(config, distancia, rapidezHorizontal, config.alturaChegadaLancamento - saida.y);
                    break;
                default: // reto no meio da garrafa
                    subida = Trajetoria.AcharSubida(config, distancia, rapidezHorizontal, meioGarrafa - saida.y);
                    break;
            }

            // Precisão (só no que usa o arco): perfeita na faixa verde, um pouco torta antes, desgovernada depois
            float erroGraus = 0f;
            if (!passeParaColega && cargaFinal > config.fimFaixaVerde)
            {
                float sinal = UnityEngine.Random.value < 0.5f ? -1f : 1f;
                erroGraus = sinal * UnityEngine.Random.Range(config.erroDesgovernadaMinimo, config.erroDesgovernadaMaximo);
                subida *= 1f + config.subidaExtraDesgovernada * UnityEngine.Random.Range(0.5f, 1f);
            }
            else if (!passeParaColega && cargaFinal < config.inicioFaixaVerde)
            {
                float falta = 1f - cargaFinal / Mathf.Max(0.01f, config.inicioFaixaVerde);
                erroGraus = UnityEngine.Random.Range(-1f, 1f) * config.erroMaximoAntesDaFaixa * falta;
            }
            direcao = Quaternion.AngleAxis(erroGraus, Vector3.up) * direcao;

            Vector3 velocidade = direcao * rapidezHorizontal + Vector3.up * subida;
            TirarBola();
            Arremessou?.Invoke(this, saida, velocidade, tipo, kmh);
        }
    }
}
