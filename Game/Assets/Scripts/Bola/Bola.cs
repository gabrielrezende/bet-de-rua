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
            QuicouDesdeLancamento = posicao.y <= config.raioBola + 0.001f;
            UltimoLancador = lancador;
            AtualizarVisual();
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
            FisicaBola.Passo(config, ref posicao, ref velocidade, ref parada, dt);
            Parada = parada;
            if (posicao.y <= config.raioBola + 0.001f) QuicouDesdeLancamento = true;
            if (!Parada) ColidirComGarrafas(config.raioBola);
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
                if (deslocamento.sqrMagnitude >= distanciaMinima * distanciaMinima) continue;

                Vector3 normal = deslocamento.sqrMagnitude > 0.000001f
                    ? deslocamento.normalized
                    : -new Vector3(velocidade.x, 0f, velocidade.z).normalized;
                // Só conta se a bola estiver indo na direção da garrafa
                if (Vector3.Dot(velocidade, normal) >= 0f) continue;

                float alturaContato = Mathf.Clamp(posicao.y - baseGarrafa.y, 0f, garrafa.Altura);
                Vector3 ponto = baseGarrafa + normal * garrafa.Raio + Vector3.up * alturaContato;
                garrafa.ReceberBolada(velocidade, ponto);

                // A bola volta, perdendo velocidade
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
