using UnityEngine;

namespace BetsDeRua
{
    /// <summary>
    /// Números das informações na tela e marcadores (ver docs/REFERENCIAS_FIFA.md, seções 2 e 4).
    /// </summary>
    [CreateAssetMenu(fileName = "ConfigHUD", menuName = "Bets de Rua/Config HUD")]
    public class ConfigHUD : ScriptableObject
    {
        [Header("Marcador do jogador controlado")]
        [Tooltip("Altura do triângulo acima da cabeça (m).")]
        public float alturaTrianguloAcimaCabeca = 0.45f;
        [Tooltip("Tamanho do triângulo (m).")]
        public float tamanhoTriangulo = 0.4f;
        [Tooltip("Velocidade de giro do triângulo (graus/s).")]
        public float giroTriangulo = 120f;
        [Tooltip("Raio do círculo de giz no chão sob o jogador ativo (m).")]
        public float raioCirculoGiz = 0.55f;
        [Tooltip("Espessura da linha do círculo de giz (m).")]
        public float espessuraCirculoGiz = 0.06f;
        [Tooltip("Duração da piscada quando o jogador passa a ser controlado (s).")]
        public float duracaoPiscar = 0.4f;
        [Tooltip("Quanto o marcador cresce na piscada (1 = nada).")]
        public float escalaPiscar = 1.6f;
    }
}
