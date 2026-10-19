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
        [Header("Transmissão alta (posição mais perto possível)")]
        [Tooltip("Altura da câmera quando está o mais perto possível (m).")]
        public float altura = 14f;
        [Tooltip("Distância na horizontal até a linha central do campo, o mais perto possível (m). " +
                 "Com 14 m de altura dá ~35° de inclinação.")]
        public float distancia = 20f;
        [Tooltip("Campo de visão vertical (graus).")]
        public float campoVisao = 40f;
        [Tooltip("Quanto a câmera levanta o olhar acima do centro do campo (graus): a rua fica mais embaixo na tela e aparecem mais casas.")]
        public float inclinarParaCima = 7f;

        [Header("Enquadramento: os 4 jogadores e a bola sempre na tela")]
        [Tooltip("Folga além do jogador ou da bola mais na ponta (m).")]
        public float margemEnquadramento = 3f;
        [Tooltip("Quanto a câmera pode se afastar no máximo (2 = duas vezes mais longe que o mais perto). " +
                 "Se nem assim couber todo mundo, os rebatedores têm prioridade e a bola vira uma seta na borda.")]
        public float afastamentoMaximo = 2f;
        [Tooltip("Quanto a câmera pode mostrar além dos muros do fundo (m).")]
        public float margemAlemDoMuro = 1.5f;
        [Tooltip("Tempo (s) para a câmera deslizar de lado. Maior = mais suave.")]
        public float suavizacao = 0.3f;
        [Tooltip("Tempo (s) para a câmera se afastar ou aproximar.")]
        public float suavizacaoZoom = 0.6f;

        [Tooltip("Distância máxima que a câmera desenha (m).")]
        public float distanciaDesenho = 300f;
    }
}
