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
    
        [Header("Mira do arremesso")]
        [Tooltip("Raio do círculo amarelo em volta da garrafa alvo (m).")]
        public float raioCirculoAlvo = 0.6f;
        [Tooltip("Espessura do círculo amarelo (m).")]
        public float espessuraCirculoAlvo = 0.06f;
        [Tooltip("Comprimento de cada traço da linha pontilhada (m).")]
        public float comprimentoTraco = 0.4f;
        [Tooltip("Espaço entre os traços (m).")]
        public float espacoTraco = 0.3f;
        [Tooltip("Largura da linha pontilhada (m).")]
        public float larguraTraco = 0.07f;
        [Tooltip("Comprimento máximo da linha de mira (m). Curta = mostra só a direção, sem entregar " +
                 "o ponto exato para quem rebate (todo mundo vê a mesma tela no sofá). 0 = linha inteira.")]
        public float comprimentoMaximoLinha = 5f;

        [Header("Arco de força")]
        [Tooltip("Raio do arco de força (m).")]
        public float raioArco = 0.8f;
        [Tooltip("Espessura do arco (m).")]
        public float espessuraArco = 0.18f;
        [Tooltip("Abertura do arco (graus). 240 = como um velocímetro.")]
        public float aberturaArco = 240f;
        [Tooltip("Altura do centro do arco em relação ao chão (m).")]
        public float alturaArco = 1.3f;
        [Tooltip("Quanto o arco fica para o lado do jogador, na tela (m). Negativo = à esquerda.")]
        public float ladoArco = -1.3f;
        public Color corArcoNormal = Color.white;
        public Color corArcoNaFaixa = new Color(0.3f, 1f, 0.3f);
        public Color corArcoPassou = new Color(1f, 0.25f, 0.2f);

        [Header("Mensagens")]
        [Tooltip("Quanto tempo a mensagem do lance fica na tela (s).")]
        public float duracaoMensagem = 2f;
    }
}
