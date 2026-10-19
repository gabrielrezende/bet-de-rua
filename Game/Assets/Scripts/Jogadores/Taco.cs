using UnityEngine;

namespace BetsDeRua
{
    /// <summary>
    /// O taco do rebatedor. Este objeto é o pivô na mão; o taco visível é filho dele.
    /// Taco no chão automático: na zona segura da base (ver ZonaDoTaco), o taco desce e
    /// encosta no chão (seguro, não pode ser queimado), mesmo andando. Saiu da zona,
    /// ele sobe para o ombro. Depois do giro da batida, volta ao chão sozinho se ainda estiver dentro.
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

        /// <summary>True quando o taco está encostado no chão dentro do círculo.</summary>
        public bool NoChao { get; private set; }

        private void Start()
        {
            NoChao = DeveFicarNoChao();
            anguloAtual = NoChao ? AnguloNoChao() : AnguloOmbro;
            Aplicar();
        }

        /// <summary>Faz o taco girar uma vez (a batida, acertando ou não).</summary>
        public void Girar()
        {
            tempoGiro = 0f;
        }

        private void Update()
        {
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
            float alvo = deveFicarNoChao ? AnguloNoChao() : AnguloOmbro;
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
            float cosseno = Mathf.Clamp(config.alturaMaoTaco / config.comprimentoTaco, -1f, 1f);
            return -Mathf.Acos(cosseno) * Mathf.Rad2Deg;
        }

        private void Aplicar()
        {
            transform.localRotation = Quaternion.Euler(anguloAtual, 0f, 0f);
        }
    }
}
