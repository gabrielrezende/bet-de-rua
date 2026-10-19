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

        [Header("Sinal de zona do rebatedor (verde = seguro, vermelho = perigo)")]
        [Tooltip("Altura do sinal acima da cabeça (m). Fica abaixo do triângulo do jogador controlado.")]
        public float alturaSinalZona = 0.18f;
        [Tooltip("Tamanho (diâmetro) do sinal (m).")]
        public float tamanhoSinalZona = 0.22f;
        [Tooltip("Cor do sinal quando o rebatedor está seguro. Mudou? Rode Bets > Montar cena Prototipo.")]
        public Color corZonaSegura = new Color(0.2f, 0.9f, 0.3f);
        [Tooltip("Cor do sinal na zona de perigo. Mudou? Rode Bets > Montar cena Prototipo.")]
        public Color corZonaPerigo = new Color(0.95f, 0.2f, 0.15f);
    
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
        [Tooltip("Protótipo: mostra o caminho inteiro da bola no ar enquanto mira, com marcas onde ela quica " +
                 "(sem a curva do analógico). Bom para testar; desligue para o jogo de verdade.")]
        public bool mostrarTrajetoriaCompleta = true;
        [Tooltip("Quantos segundos de voo a prévia mostra.")]
        public float duracaoPrevia = 2f;
        [Tooltip("Distância em tempo (s) entre os pontos da linha da prévia. Menor = curva mais lisa.")]
        public float intervaloPontosPrevia = 0.03f;
        [Tooltip("Diâmetro da marca de quique no chão (m).")]
        public float tamanhoMarcaQuique = 0.35f;
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

        [Header("Ajudas da batida (REFERENCIAS_FIFA 9.1)")]
        [Tooltip("A sombra da bola no chão fica deste tamanho em relação à bola (visual).")]
        public float escalaSombraBola = 2f;
        [Tooltip("Acima desta altura (m) a sombra fica no tamanho mínimo.")]
        public float alturaSombraMinima = 6f;
        [Tooltip("Tamanho do 'X' amarelo onde a bola vai passar (m).")]
        public float tamanhoMarcaPassagem = 0.8f;
        [Tooltip("Espessura das linhas do 'X' (m).")]
        public float espessuraMarcaPassagem = 0.1f;
        [Tooltip("Espessura da linha do raio de alcance do taco (m).")]
        public float espessuraRaioAlcance = 0.04f;
        [Tooltip("Quanto tempo antes do momento certo o anel de tempo aparece na bola (s).")]
        public float duracaoAnelTempo = 0.5f;
        [Tooltip("Raio do anel de tempo quando aparece (m); ele encolhe até o tamanho da bola.")]
        public float raioMaximoAnelTempo = 1.2f;
        [Tooltip("Espessura do anel de tempo (m).")]
        public float espessuraAnelTempo = 0.06f;

        [Header("Garrafa exposta")]
        [Tooltip("Cor da garrafa quando o taco do dono está fora do chão (pode ser queimada).")]
        public Color corGarrafaExposta = new Color(0.95f, 0.2f, 0.15f);

        [Header("Mensagens")]
        [Tooltip("Quanto tempo a mensagem do lance fica na tela (s).")]
        public float duracaoMensagem = 2f;
    }
}
