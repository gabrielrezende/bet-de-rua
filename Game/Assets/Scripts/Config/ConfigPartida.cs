using UnityEngine;

namespace BetsDeRua
{
    /// <summary>
    /// Regras e arrumação da partida: onde cada jogador começa, cores das duplas.
    /// (As regras de pontos e rodízio entram aqui na Etapa 6.)
    /// </summary>
    [CreateAssetMenu(fileName = "ConfigPartida", menuName = "Bets de Rua/Config Partida")]
    public class ConfigPartida : ScriptableObject
    {
        [Header("Posições iniciais")]
        [Tooltip("Distância do rebatedor ao centro da garrafa, de lado, dentro do círculo (m).")]
        public float afastamentoRebatedor = 0.45f;
        [Tooltip("Quanto o arremessador fica atrás da garrafa da base dele (m).")]
        public float afastamentoArremessador = 1.2f;
        [Tooltip("Quanto o guardião fica atrás da garrafa do rebatedor (m). O GDD diz 3 a 5.")]
        public float distanciaGuardiao = 4f;

        [Header("Cores das duplas")]
        public Color corDuplaTaco = new Color(0.2f, 0.45f, 0.95f);
        public Color corDuplaBola = new Color(1f, 0.55f, 0.1f);

        [Header("Lance")]
        [Tooltip("Quanto tempo a bola fica morta antes de voltar para o arremessador (s).")]
        public float tempoBolaMorta = 1.5f;
        [Tooltip("Se a bola não parar sozinha, o lance acaba depois deste tempo (s).")]
        public float tempoMaximoBolaViva = 12f;

        [Header("Controles")]
        [Tooltip("Máximo de pessoas jogando ao mesmo tempo (controles + teclado).")]
        public int maximoJogadoresHumanos = 4;
    }
}
