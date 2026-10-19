using UnityEngine;

namespace BetsDeRua
{
    /// <summary>
    /// O taco do rebatedor. Este objeto é o pivô na mão; o taco visível é filho dele.
    /// Taco no chão automático: parado dentro do círculo, o taco desce e encosta
    /// no chão (seguro). Andando ou fora do círculo, ele sobe para o ombro.
    /// </summary>
    public class Taco : MonoBehaviour
    {
        [SerializeField] private ConfigFisica config;
        [SerializeField] private Jogador dono;

        // Ângulos do pivô em torno do eixo X (para a frente é negativo)
        private const float AnguloOmbro = 150f; // para cima e para trás

        private float anguloAtual;
        private float velocidadeAngulo;

        /// <summary>True quando o taco está encostado no chão dentro do círculo.</summary>
        public bool NoChao { get; private set; }

        private void Start()
        {
            NoChao = DeveFicarNoChao();
            anguloAtual = NoChao ? AnguloNoChao() : AnguloOmbro;
            Aplicar();
        }

        private void Update()
        {
            bool deveFicarNoChao = DeveFicarNoChao();
            float alvo = deveFicarNoChao ? AnguloNoChao() : AnguloOmbro;
            anguloAtual = Mathf.SmoothDamp(anguloAtual, alvo, ref velocidadeAngulo, config.tempoMovimentoTaco);
            // Só conta como "no chão" quando chegou lá de verdade
            NoChao = deveFicarNoChao && Mathf.Abs(anguloAtual - alvo) < 2f;
            Aplicar();
        }

        private bool DeveFicarNoChao()
        {
            return dono.EstaParado && dono.DentroDoCirculo();
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
