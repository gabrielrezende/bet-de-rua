using System;
using UnityEngine;

namespace BetsDeRua
{
    /// <summary>Os três jeitos de arremessar.</summary>
    public enum TipoArremesso { Forte, Quicado, Pingo }

    /// <summary>
    /// Arremesso do jogador com a bola na mão (docs/REFERENCIAS_FIFA.md, seções 3, 9.2 e 9.3),
    /// no esquema "twin-stick": o analógico esquerdo anda (sem passar à frente da própria
    /// garrafa), o direito (ou o mouse) mira em volta da garrafa alvo, e segurar
    /// R2 / R1 / L2 enche o arco de força. Ao soltar, calcula a velocidade da bola e
    /// avisa pelo evento Arremessou.
    /// </summary>
    [RequireComponent(typeof(Jogador))]
    public class Arremessador : MonoBehaviour
    {
        [SerializeField] private ConfigFisica config;
        [SerializeField] private MiraNoChao mira;
        [SerializeField] private ArcoForca arco;

        private Jogador jogador;
        private Camera cameraPrincipal;
        private float carga;            // 0 a 1: quanto o arco encheu
        private bool carregando;
        private TipoArremesso tipoCarregando;
        private Vector3 pontoMira;

        /// <summary>Avisa (posição de saída, velocidade da bola, tipo, km/h na horizontal).</summary>
        public event Action<Vector3, Vector3, TipoArremesso, float> Arremessou;

        /// <summary>True enquanto a bola está na mão deste jogador.</summary>
        public bool ComBola { get; set; }
        /// <summary>Garrafa que ele está mirando.</summary>
        public Garrafa Alvo { get; set; }
        /// <summary>Garrafa da base dele: com a bola na mão, não pode passar à frente dela.</summary>
        public Garrafa GarrafaPropria { get; set; }
        public Jogador Jogador => jogador;

        /// <summary>Onde a bola fica na mão (e de onde sai no arremesso).</summary>
        public Vector3 PosicaoMao => transform.position
                                     + Vector3.up * config.alturaSoltura
                                     + transform.forward * config.avancoSoltura
                                     + transform.right * config.ladoSoltura;

        private void Awake()
        {
            jogador = GetComponent<Jogador>();
        }

        private void Update()
        {
            if (cameraPrincipal == null) cameraPrincipal = Camera.main;
            jogador.ComBolaNaMao = ComBola;
            AtualizarLimite();

            ControleHumano humano = jogador.Controlador;
            // Sem ninguém (ou controle desconectado no meio): cancela a mira e a carga, sem arremessar
            if (!ComBola || Alvo == null || humano == null || !humano.Conectado)
            {
                carregando = false;
                carga = 0f;
                mira.Esconder();
                arco.Esconder();
                return;
            }

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
            }
        }

        /// <summary>Com a bola na mão, o jogador não passa à frente da própria garrafa (rumo ao alvo).</summary>
        private void AtualizarLimite()
        {
            if (!ComBola || Alvo == null || GarrafaPropria == null)
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

            if (BotaoDoTipo(humano, tipoCarregando))
            {
                carga = Mathf.Min(1f, carga + Time.deltaTime / Mathf.Max(0.05f, config.tempoEncherForca));
                arco.Mostrar(transform.position, carga);
                return;
            }

            // Soltou o botão: arremessa
            carregando = false;
            arco.Esconder();
            Arremessar(tipoCarregando, carga);
        }

        private static bool BotaoSegurado(ControleHumano humano, out TipoArremesso tipo)
        {
            if (humano.SegurandoArremessoForte) { tipo = TipoArremesso.Forte; return true; }
            if (humano.SegurandoArremessoQuicado) { tipo = TipoArremesso.Quicado; return true; }
            if (humano.SegurandoArremessoPingo) { tipo = TipoArremesso.Pingo; return true; }
            tipo = TipoArremesso.Forte;
            return false;
        }

        private static bool BotaoDoTipo(ControleHumano humano, TipoArremesso tipo)
        {
            switch (tipo)
            {
                case TipoArremesso.Quicado: return humano.SegurandoArremessoQuicado;
                case TipoArremesso.Pingo: return humano.SegurandoArremessoPingo;
                default: return humano.SegurandoArremessoForte;
            }
        }

        private void Arremessar(TipoArremesso tipo, float cargaFinal)
        {
            Vector3 saida = PosicaoMao;

            // Velocidade na horizontal: cresce com o arco até o fim da faixa verde
            float fracao = Mathf.Clamp01(cargaFinal / Mathf.Max(0.01f, config.fimFaixaVerde));
            float kmh;
            switch (tipo)
            {
                case TipoArremesso.Quicado: kmh = Mathf.Lerp(config.quicadoMinimaKmh, config.quicadoMaximaKmh, fracao); break;
                case TipoArremesso.Pingo: kmh = Mathf.Lerp(config.pingoMinimaKmh, config.pingoMaximaKmh, fracao); break;
                default: kmh = Mathf.Lerp(config.forteMinimaKmh, config.forteMaximaKmh, fracao); break;
            }
            float rapidezHorizontal = kmh / 3.6f;

            Vector3 horizontal = pontoMira - saida;
            horizontal.y = 0f;
            float distancia = horizontal.magnitude;
            Vector3 direcao = distancia > 0.01f ? horizontal / distancia : transform.forward;

            // Quanto a bola precisa subir para chegar no alvo, conforme o tipo
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
                default: // reto no meio da garrafa
                    subida = Trajetoria.AcharSubida(config, distancia, rapidezHorizontal, meioGarrafa - saida.y);
                    break;
            }

            // Precisão: perfeita na faixa verde, um pouco torta antes, desgovernada depois
            float erroGraus = 0f;
            if (cargaFinal > config.fimFaixaVerde)
            {
                float sinal = UnityEngine.Random.value < 0.5f ? -1f : 1f;
                erroGraus = sinal * UnityEngine.Random.Range(config.erroDesgovernadaMinimo, config.erroDesgovernadaMaximo);
                subida *= 1f + config.subidaExtraDesgovernada * UnityEngine.Random.Range(0.5f, 1f);
            }
            else if (cargaFinal < config.inicioFaixaVerde)
            {
                float falta = 1f - cargaFinal / Mathf.Max(0.01f, config.inicioFaixaVerde);
                erroGraus = UnityEngine.Random.Range(-1f, 1f) * config.erroMaximoAntesDaFaixa * falta;
            }
            direcao = Quaternion.AngleAxis(erroGraus, Vector3.up) * direcao;

            Vector3 velocidade = direcao * rapidezHorizontal + Vector3.up * subida;
            ComBola = false;
            jogador.ComBolaNaMao = false;
            jogador.RemoverLimiteFrente();
            mira.Esconder();
            Arremessou?.Invoke(saida, velocidade, tipo, kmh);
        }
    }
}
