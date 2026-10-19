using System;
using UnityEngine;

namespace BetsDeRua
{
    /// <summary>Os jeitos de soltar a bola da mão.</summary>
    public enum TipoArremesso { Forte, Quicado, Pingo, Passe, Lancamento, Rasteira }

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
        [Tooltip("Prévia do caminho inteiro da bola (dividida entre os defensores).")]
        [SerializeField] private PreviaTrajetoria previa;

        private Jogador jogador;
        private Camera cameraPrincipal;
        private float carga;            // 0 a 1: quanto o arco encheu
        private bool carregando;
        private TipoArremesso tipoCarregando;
        private Vector3 pontoMira;
        private float esperaCpu;
        private bool mostrandoVisuais;
        private bool esperandoSoltarGatilhos;  // acabou de pegar com o gatilho: só carrega depois de soltar  // para esconder a mira e o arco só uma vez, ao soltar a bola

        /// <summary>Avisa (quem, posição de saída, velocidade da bola, tipo, km/h na horizontal).</summary>
        public event Action<Arremessador, Vector3, Vector3, TipoArremesso, float> Arremessou;

        /// <summary>True enquanto enche o arco (o boneco leva o braço para trás).</summary>
        public bool Carregando => carregando;
        public float Carga => carga;

        /// <summary>True enquanto a bola está na mão deste jogador.</summary>
        public bool ComBola { get; private set; }
        public ModoComBola Modo { get; private set; }
        /// <summary>Garrafa que ele está mirando.</summary>
        public Garrafa Alvo { get; set; }
        /// <summary>Colega para o passe e o lançamento (no campo).</summary>
        public Jogador Parceiro { get; set; }
        /// <summary>Onde a CPU precisa estar para arremessar (null = de onde estiver). Quem define é o ControladorLance.</summary>
        public Vector3? LugarDoArremessoCpu { get; set; }
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
            esperandoSoltarGatilhos = true;
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
            // No campo dá para correr com a bola (R1); no arremesso, não (anda até 4 m/s)
            jogador.PodeCorrerComBola = ComBola && Modo == ModoComBola.Campo;
            AtualizarLimite();

            ControleHumano humano = jogador.Controlador;
            bool humanoJogando = humano != null && humano.Conectado;
            // Pegou no mergulho: só mira e lança depois de levantar
            if (jogador.Mergulhando)
            {
                carregando = false;
                carga = 0f;
                EsconderVisuais();
                return;
            }
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
            if (ComBola && Alvo != null && jogador.Controlador != null && jogador.Controlador.Conectado && !jogador.Mergulhando)
            {
                jogador.OlharPara(pontoMira - transform.position);
                mira.Mostrar(PosicaoMao, pontoMira, Alvo.transform.position);
                // Prévia do caminho inteiro: só nos lances que usam o arco (não no passe)
                bool lanceDeArco = !carregando || (tipoCarregando != TipoArremesso.Passe && tipoCarregando != TipoArremesso.Lancamento);
                if (previa != null && lanceDeArco)
                {
                    PreviaDoLancamento(out Vector3 saida, out Vector3 velocidade);
                    previa.Mostrar(saida, velocidade);
                }
                else if (previa != null) previa.Esconder();
                mostrandoVisuais = true;
            }
        }

        private void EsconderVisuais()
        {
            if (!mostrandoVisuais) return;
            mira.Esconder();
            arco.Esconder();
            if (previa != null) previa.Esconder();
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
                // Sem mira fina (pesquisa: a habilidade fica na força e no tipo, não em acertar o analógico):
                // a bola vai no rumo da garrafa. O analógico direito fica para a curva depois de soltar
                // e para a rasteira (R2 + para baixo).
                pontoMira = config.miraFinaNoAnalogico
                    ? centro + DireitaDeQuemArremessa(centro) * (Mathf.Sign(humano.MiraAnalogica.x)
                        * Mathf.Pow(Mathf.Abs(Mathf.Clamp(humano.MiraAnalogica.x, -1f, 1f)), Mathf.Max(1f, config.curvaMiraAnalogico)) * config.raioMira)
                    : centro;
            }
            pontoMira.y = 0f;
        }

        /// <summary>A direita de quem arremessa, olhando de onde ele está para 'paraOnde' (no chão).</summary>
        private Vector3 DireitaDeQuemArremessa(Vector3 paraOnde)
        {
            Vector3 frente = paraOnde - transform.position;
            frente.y = 0f;
            if (frente.sqrMagnitude < 0.0001f) frente = transform.forward;
            return Vector3.Cross(Vector3.up, frente.normalized);
        }

        /// <summary>
        /// O tipo que sai de fato: no arremesso, R2 com o analógico direito para baixo vira rasteira.
        /// </summary>
        private TipoArremesso TipoEfetivo(TipoArremesso tipo, ControleHumano humano)
        {
            if (tipo == TipoArremesso.Forte && Modo == ModoComBola.Arremesso && humano != null
                && humano.MiraAnalogica.y < -config.limiteAnalogicoRasteira)
                return TipoArremesso.Rasteira;
            return tipo;
        }

        private void AtualizarCarga(ControleHumano humano)
        {
            // Pegou a bola apertando o gatilho: o mesmo aperto não pode virar arremesso
            if (esperandoSoltarGatilhos)
            {
                if (humano.SegurandoAlgumGatilho) return;
                esperandoSoltarGatilhos = false;
            }
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
                // No campo o arco enche mais rápido: pegar, virar e já chutar
                float tempoEncher = Modo == ModoComBola.Campo ? config.tempoEncherChute : config.tempoEncherForca;
                carga = Mathf.Min(1f, carga + Time.deltaTime / Mathf.Max(0.05f, tempoEncher));
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
            Soltar(TipoEfetivo(tipoCarregando, humano), carga);
        }

        /// <summary>
        /// Ninguém no arremessador: a CPU espera um pouco, mira perto da garrafa (com um
        /// desvio ao acaso) e arremessa um tipo sorteado, soltando dentro da faixa verde.
        /// </summary>
        private void ArremessarComoCpu()
        {
            if (!configPartida.arremessadorAutomatico) return;
            // Ainda andando para o lugar do arremesso (sem bola morta ninguém é teleportado): espera chegar
            if (!jogador.EstaParado) { esperaCpu = 0f; return; }
            // Ainda longe do lugar do arremesso (atrás da garrafa, fora da linha do parceiro): espera chegar
            if (LugarDoArremessoCpu.HasValue)
            {
                Vector3 falta = LugarDoArremessoCpu.Value - transform.position;
                falta.y = 0f;
                if (falta.magnitude > configPartida.folgaLugarArremessoCpu) { esperaCpu = 0f; return; }
            }
            jogador.OlharPara(Alvo.transform.position - transform.position);
            esperaCpu += Time.deltaTime;
            if (esperaCpu < configPartida.esperaArremessoAutomatico) return;
            esperaCpu = 0f;

            float maximo = NiveisBots.De(configPartida, jogador).desvioArremesso;
            float desvio = UnityEngine.Random.Range(-maximo, maximo);
            float sorteio = UnityEngine.Random.value;
            // Variar a altura de chegada é o que engana quem rebate: reto, quicado, pingo e rasteira
            TipoArremesso tipo = sorteio < 0.35f ? TipoArremesso.Forte : sorteio < 0.6f ? TipoArremesso.Quicado
                               : sorteio < 0.8f ? TipoArremesso.Rasteira : TipoArremesso.Pingo;
            ExecutarPeloBot(tipo, UnityEngine.Random.Range(config.inicioFaixaVerde, config.fimFaixaVerde),
                            Alvo.transform.position + Vector3.forward * desvio);
        }

        /// <summary>Um bot solta a bola: tipo, força do arco e ponto mirado no chão.</summary>
        public void ExecutarPeloBot(TipoArremesso tipo, float cargaBot, Vector3 pontoMirado)
        {
            if (!ComBola || jogador.Mergulhando) return; // caído do mergulho: levanta primeiro
            pontoMira = new Vector3(pontoMirado.x, 0f, pontoMirado.z);
            jogador.OlharPara(pontoMira - transform.position);
            Soltar(tipo, cargaBot);
        }

        /// <summary>
        /// Arremesso: R2 reto (forte), L2 sempre quicando, R1 pingo (por cima).
        /// Campo: R2 chute na garrafa, L2 passe para o colega; o R1 fica para correr com a bola.
        /// </summary>
        private bool BotaoSegurado(ControleHumano humano, out TipoArremesso tipo)
        {
            bool campo = Modo == ModoComBola.Campo;
            if (humano.SegurandoGatilhoForte) { tipo = TipoArremesso.Forte; return true; }
            if (humano.SegurandoGatilhoPorCima) { tipo = campo ? TipoArremesso.Passe : TipoArremesso.Quicado; return true; }
            if (!campo && humano.SegurandoGatilhoCurto) { tipo = TipoArremesso.Pingo; return true; }
            tipo = TipoArremesso.Forte;
            return false;
        }

        private static bool BotaoDoTipo(ControleHumano humano, TipoArremesso tipo)
        {
            switch (tipo)
            {
                case TipoArremesso.Quicado:
                case TipoArremesso.Lancamento:
                case TipoArremesso.Passe: return humano.SegurandoGatilhoPorCima;
                case TipoArremesso.Pingo: return humano.SegurandoGatilhoCurto;
                default: return humano.SegurandoGatilhoForte;
            }
        }

        private void Soltar(TipoArremesso tipo, float cargaFinal)
        {
            CalcularLancamento(tipo, cargaFinal, true, out Vector3 saida, out Vector3 velocidade, out float kmh);
            TirarBola();
            Arremessou?.Invoke(this, saida, velocidade, tipo, kmh);
        }

        /// <summary>
        /// A conta do lançamento: de onde a bola sai, com que velocidade e quantos km/h.
        /// comErro = true sorteia o desvio de soltar fora da faixa verde (o lançamento de verdade);
        /// false dá o caminho "limpo" (a prévia da trajetória na mira).
        /// </summary>
        private void CalcularLancamento(TipoArremesso tipo, float cargaFinal, bool comErro,
                                        out Vector3 saida, out Vector3 velocidade, out float kmh)
        {
            saida = PosicaoMao;
            bool passeParaColega = tipo == TipoArremesso.Passe || tipo == TipoArremesso.Lancamento;

            // Passe e lançamento vão direto no colega; o resto vai no ponto mirado
            Vector3 destino = passeParaColega && Parceiro != null ? Parceiro.transform.position : pontoMira;
            Vector3 horizontal = destino - saida;
            horizontal.y = 0f;
            float distancia = Mathf.Max(0.5f, horizontal.magnitude);
            Vector3 direcao = horizontal.sqrMagnitude > 0.0001f ? horizontal.normalized : transform.forward;

            // Velocidade na horizontal: o arco enche até o fim da faixa verde (passes: fixa)
            float fracao = Mathf.Clamp01(cargaFinal / Mathf.Max(0.01f, config.fimFaixaVerde));
            switch (tipo)
            {
                case TipoArremesso.Quicado: kmh = Mathf.Lerp(config.quicadoMinimaKmh, config.quicadoMaximaKmh, fracao); break;
                case TipoArremesso.Pingo: kmh = Mathf.Lerp(config.pingoMinimaKmh, config.pingoMaximaKmh, fracao); break;
                case TipoArremesso.Passe: kmh = config.passeKmh; break;
                case TipoArremesso.Lancamento: kmh = config.lancamentoKmh; break;
                case TipoArremesso.Rasteira: kmh = Mathf.Lerp(config.rasteiraMinimaKmh, config.rasteiraMaximaKmh, fracao); break;
                default: kmh = Mathf.Lerp(config.forteMinimaKmh, config.forteMaximaKmh, fracao); break;
            }
            if (!passeParaColega) kmh *= jogador.FatorForca;
            float rapidezHorizontal = kmh / 3.6f;

            // Força = distância: na faixa verde a bola chega no ponto mirado; antes, cai curta;
            // depois, isola (vai por cima e cai lá longe). Sem sorteio: o que a prévia mostra é o que sai.
            if (!passeParaColega) distancia *= FatorDistancia(cargaFinal);

            // Quanto a bola precisa subir para chegar no destino, conforme o tipo
            float meioGarrafa = config.alturaGarrafa * 0.5f;
            float subida;
            switch (tipo)
            {
                case TipoArremesso.Quicado: // quica no chão antes e chega no meio da garrafa
                    subida = Trajetoria.AcharSubidaQuicado(config, distancia, rapidezHorizontal, saida.y,
                                                           meioGarrafa, config.distanciaQuique);
                    break;
                case TipoArremesso.Rasteira: // abaixa e solta rente ao chão: a bola sai rolando
                    saida = new Vector3(saida.x, config.raioBola + 0.01f, saida.z);
                    subida = 0f;
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
            if (comErro && config.erroAleatorioForaDaFaixa && !passeParaColega && cargaFinal > config.fimFaixaVerde)
            {
                float sinal = UnityEngine.Random.value < 0.5f ? -1f : 1f;
                erroGraus = sinal * UnityEngine.Random.Range(config.erroDesgovernadaMinimo, config.erroDesgovernadaMaximo);
                subida *= 1f + config.subidaExtraDesgovernada * UnityEngine.Random.Range(0.5f, 1f);
            }
            else if (comErro && config.erroAleatorioForaDaFaixa && !passeParaColega && cargaFinal < config.inicioFaixaVerde)
            {
                float falta = 1f - cargaFinal / Mathf.Max(0.01f, config.inicioFaixaVerde);
                erroGraus = UnityEngine.Random.Range(-1f, 1f) * config.erroMaximoAntesDaFaixa * falta;
            }
            // Arremessar andando tira a precisão: quanto mais rápido, mais torta a bola sai
            if (comErro && !passeParaColega)
                erroGraus += UnityEngine.Random.Range(-1f, 1f) * config.erroArremessoPorVelocidade * jogador.Velocidade.magnitude;
            direcao = Quaternion.AngleAxis(erroGraus, Vector3.up) * direcao;

            // Teto na subida: com força demais a bola isola, mas não vai para o espaço
            subida = Mathf.Min(subida, config.subidaMaximaArremesso);
            velocidade = direcao * rapidezHorizontal + Vector3.up * subida;
        }

        /// <summary>
        /// Quanto a distância muda com a força: 1 dentro da faixa verde; menos antes (bola curta);
        /// mais depois (bola isolada).
        /// </summary>
        private float FatorDistancia(float cargaFinal)
        {
            if (cargaFinal < config.inicioFaixaVerde)
                return Mathf.Lerp(config.fatorDistanciaSemForca, 1f, cargaFinal / Mathf.Max(0.01f, config.inicioFaixaVerde));
            if (cargaFinal > config.fimFaixaVerde)
                return Mathf.Lerp(1f, config.fatorDistanciaForcaMaxima,
                                  (cargaFinal - config.fimFaixaVerde) / Mathf.Max(0.01f, 1f - config.fimFaixaVerde));
            return 1f;
        }

        /// <summary>
        /// Prévia (para a mira): a velocidade de saída "limpa" do tipo que está sendo carregado,
        /// ou do forte no meio da faixa verde quando ainda não segurou nada.
        /// </summary>
        private void PreviaDoLancamento(out Vector3 saida, out Vector3 velocidade)
        {
            TipoArremesso tipo = TipoEfetivo(carregando ? tipoCarregando : TipoArremesso.Forte, jogador.Controlador);
            float cargaPrevia = carregando ? carga : (config.inicioFaixaVerde + config.fimFaixaVerde) * 0.5f;
            CalcularLancamento(tipo, cargaPrevia, false, out saida, out velocidade, out _);
        }
    }
}
