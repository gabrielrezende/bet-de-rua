using UnityEngine;

namespace BetsDeRua
{
    /// <summary>
    /// Números da câmera de transmissão ("Transmissão alta", ver docs/REFERENCIAS_FIFA.md).
    /// Ajustáveis no Inspector com o jogo rodando.
    /// </summary>
    [CreateAssetMenu(fileName = "ConfigCamera", menuName = "Bets de Rua/Config Camera")]
    public class ConfigCamera : ScriptableObject
    {
        [Header("Transmissão alta")]
        [Tooltip("Altura da câmera em relação ao chão (m).")]
        public float altura = 14f;
        [Tooltip("Distância na horizontal da câmera até a linha central do campo (m). " +
                 "Com 14 m de altura dá ~35° de inclinação.")]
        public float distancia = 20f;
        [Tooltip("Campo de visão vertical (graus).")]
        public float campoVisao = 40f;
        [Tooltip("Tempo (s) que a câmera leva para alcançar a bola. Maior = mais suave.")]
        public float suavizacao = 0.3f;
        [Tooltip("Quanto a câmera pode mostrar além dos muros do fundo (m). " +
                 "A câmera para de deslizar quando chega nesse limite.")]
        public float margemAlemDoMuro = 1.5f;
        [Tooltip("Distância máxima que a câmera desenha (m).")]
        public float distanciaDesenho = 300f;
    }
}
