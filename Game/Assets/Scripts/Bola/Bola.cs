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
            QuicouDesdeLancamento = posicao.y <= FisicaBola.AlturaDoChao(config, posicao.z) + config.raioBola + 0.001f;
            UltimoLancador = lancador;
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
            }
            AtualizarVisual();
        }

        private void Simular(float dt)
        {
            bool parada = Parada;
            AplicarCurva(dt);
            FisicaBola.Passo(config, ref posicao, ref velocidade, ref parada, dt);
            Parada = parada;
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
                if (posicao.y > pe.y + config.alturaJogador + raio) continue;

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

        private void AtualizarVisual()
        {
            if (config == null) return;
            float raioVisual = config.raioBola * config.escalaVisualBola;
            transform.localScale = Vector3.one * (raioVisual * 2f);
            // Sobe o visual para a bola "grande" não afundar no chão
            transform.position = posicao + Vector3.up * (raioVisual - config.raioBola);
        }
    }
}
