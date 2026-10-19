using UnityEngine;

namespace BetsDeRua
{
    /// <summary>
    /// Física própria e simples da bola (sem Rigidbody): gravidade, resistência do ar,
    /// quique no chão, rolar com atrito, bater nos muros e nas garrafas.
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

        private void Awake()
        {
            // O visual fica acima da posição física; desfaz esse deslocamento
            float raioVisual = config.raioBola * config.escalaVisualBola;
            posicao = transform.position - Vector3.up * (raioVisual - config.raioBola);
            AtualizarVisual();
        }

        /// <summary>Põe a bola numa posição com uma velocidade inicial (m/s).</summary>
        public void Lancar(Vector3 posicaoInicial, Vector3 velocidadeInicial)
        {
            posicao = posicaoInicial;
            velocidade = velocidadeInicial;
            Parada = false;
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
            float raio = config.raioBola;
            bool noChao = posicao.y <= raio + 0.0001f && velocidade.y <= 0f;

            // Gravidade (só no ar) e resistência do ar
            if (!noChao) velocidade.y -= config.gravidade * dt;
            float rapidez = velocidade.magnitude;
            velocidade -= velocidade * (config.arrastoAr * rapidez * dt);

            // Rolando: atrito do chão freia a bola
            if (noChao)
            {
                velocidade.y = 0f;
                Vector3 horizontal = new Vector3(velocidade.x, 0f, velocidade.z);
                float rapidezH = horizontal.magnitude;
                float novaRapidez = rapidezH - config.atritoRolar * dt;
                if (novaRapidez <= config.velocidadeParada)
                {
                    velocidade = Vector3.zero;
                    posicao.y = raio;
                    Parada = true;
                    return;
                }
                velocidade = horizontal * (novaRapidez / rapidezH);
            }

            posicao += velocidade * dt;

            ColidirComChao(raio);
            ColidirComMuros(raio);
            ColidirComGarrafas(raio);
        }

        private void ColidirComChao(float raio)
        {
            if (posicao.y >= raio) return;
            posicao.y = raio;
            if (velocidade.y >= 0f) return;

            if (-velocidade.y > config.velocidadeMinimaQuique)
            {
                // Quique: perde parte da velocidade vertical e um pouco da horizontal
                velocidade.y = -velocidade.y * config.quiqueAsfalto;
                velocidade.x *= config.atritoQuique;
                velocidade.z *= config.atritoQuique;
            }
            else
            {
                // Quique fraco demais: começa a rolar
                velocidade.y = 0f;
            }
        }

        private void ColidirComMuros(float raio)
        {
            // Por enquanto a bola sempre volta no limite do campo.
            // ("Bola no mato", por cima do muro, fica para depois.)
            float limiteX = config.comprimentoCampo * 0.5f - raio;
            float limiteZ = config.larguraCampo * 0.5f - raio;

            if (Mathf.Abs(posicao.x) > limiteX && posicao.x * velocidade.x > 0f)
            {
                posicao.x = Mathf.Sign(posicao.x) * limiteX;
                velocidade.x = -velocidade.x;
                velocidade *= config.quiqueMuro;
            }
            if (Mathf.Abs(posicao.z) > limiteZ && posicao.z * velocidade.z > 0f)
            {
                posicao.z = Mathf.Sign(posicao.z) * limiteZ;
                velocidade.z = -velocidade.z;
                velocidade *= config.quiqueMuro;
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
