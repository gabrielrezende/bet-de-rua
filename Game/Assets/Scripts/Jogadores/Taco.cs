using UnityEngine;

namespace BetsDeRua
{
    /// <summary>
    /// O taco do rebatedor. Este objeto é o pivô na mão; o taco visível é filho dele.
    /// Taco no chão automático: na zona segura da base (ver ZonaDoTaco), o taco desce e
    /// encosta no chão (seguro, não pode ser queimado), mesmo andando. Saiu da zona,
    /// ele levanta um pouco (a ponta sai do chão). Depois do giro da batida, volta ao chão sozinho se ainda estiver dentro.
    /// </summary>
    public class Taco : MonoBehaviour
    {
        [SerializeField] private ConfigFisica config;
        [SerializeField] private Jogador dono;

        // Ângulos do pivô em torno do eixo X (para a frente é negativo)
        private const float AnguloOmbro = 150f; // para cima e para trás

        // Giro da batida: o taco deitado varre da direita (atrás) para a esquerda (frente)
        private const float AnguloDeitado = -90f;
        private const float GiroInicio = 110f;
        private const float GiroFim = -80f;

        private float anguloAtual;
        private float velocidadeAngulo;
        private float tempoGiro = float.MaxValue;
        private float tempoBaterTacos = float.MaxValue;
        private Vector3 rumoBaterTacos;

        /// <summary>True quando o taco está encostado no chão dentro do círculo.</summary>
        public bool NoChao { get; private set; }

        private void Start()
        {
            NoChao = DeveFicarNoChao();
            anguloAtual = NoChao ? AnguloNoChao() : AnguloNoChao() - config.anguloTacoLevantado;
            Aplicar();
        }

        /// <summary>True durante o giro da batida; FracaoGiro vai de 0 a 1 (para o boneco girar junto).</summary>
        public bool Girando => tempoGiro < config.duracaoGiroTaco;
        public float FracaoGiro => Mathf.Clamp01(tempoGiro / Mathf.Max(0.01f, config.duracaoGiroTaco));
        /// <summary>True enquanto os tacos batem em X na cruzada.</summary>
        public bool BatendoTacos => tempoBaterTacos < config.duracaoBaterTacos;

        /// <summary>Faz o taco girar uma vez (a batida, acertando ou não).</summary>
        public void Girar()
        {
            Sons.Tocar(TipoSom.GiroTaco, 0.5f);
            tempoGiro = 0f;
        }

        /// <summary>
        /// Cruzada: levanta o taco na direção de 'paraOnde' (o parceiro) e bate no taco dele,
        /// os dois cruzando em X no alto. Só visual: o ponto já foi contado.
        /// </summary>
        public void BaterNoOutroTaco(Vector3 paraOnde)
        {
            Sons.Tocar(TipoSom.Tec, 0.9f, 0.03f);
            paraOnde.y = 0f;
            rumoBaterTacos = paraOnde.sqrMagnitude > 0.0001f ? paraOnde.normalized : transform.forward;
            tempoBaterTacos = 0f;
        }

        private void Update()
        {
            if (tempoBaterTacos < config.duracaoBaterTacos)
            {
                tempoBaterTacos += Time.deltaTime;
                // O taco (que fica pendurado para baixo do pivô) aponta para cima e para o parceiro
                Vector3 apontar = (rumoBaterTacos + Vector3.up * config.alturaBaterTacos).normalized;
                transform.rotation = Quaternion.FromToRotation(Vector3.down, apontar);
                anguloAtual = AnguloNoChao() - config.anguloTacoLevantado;
                NoChao = false;
                return;
            }
            if (tempoGiro < config.duracaoGiroTaco)
            {
                tempoGiro += Time.deltaTime;
                float t = Mathf.Clamp01(tempoGiro / Mathf.Max(0.01f, config.duracaoGiroTaco));
                transform.localRotation = Quaternion.Euler(AnguloDeitado, Mathf.Lerp(GiroInicio, GiroFim, t), 0f);
                anguloAtual = AnguloOmbro; // depois do giro, volta pelo ombro
                NoChao = false;
                return;
            }

            bool deveFicarNoChao = DeveFicarNoChao();
            // Fora da zona: o taco levanta só um pouco (ponta acima do chão), não vai para o ombro
            float alvo = deveFicarNoChao ? AnguloNoChao() : AnguloNoChao() - config.anguloTacoLevantado;
            anguloAtual = Mathf.SmoothDamp(anguloAtual, alvo, ref velocidadeAngulo, config.tempoMovimentoTaco);
            // Só conta como "no chão" quando chegou lá de verdade
            NoChao = deveFicarNoChao && Mathf.Abs(anguloAtual - alvo) < 2f;
            Aplicar();
        }

        /// <summary>Taco no chão: sempre que o dono está dentro do círculo (pessoa ou bot).</summary>
        private bool DeveFicarNoChao()
        {
            return dono.DentroDoCirculo();
        }

        /// <summary>
        /// Inclinação para a frente que faz a ponta do taco encostar no chão,
        /// a partir da altura da mão.
        /// </summary>
        private float AnguloNoChao()
        {
            // Altura da mão de quem segura (muda com a altura do personagem)
            float cosseno = Mathf.Clamp(transform.localPosition.y / config.comprimentoTaco, -1f, 1f);
            return -Mathf.Acos(cosseno) * Mathf.Rad2Deg;
        }

        private void Aplicar()
        {
            transform.localRotation = Quaternion.Euler(anguloAtual, 0f, 0f);
        }
    }
}
