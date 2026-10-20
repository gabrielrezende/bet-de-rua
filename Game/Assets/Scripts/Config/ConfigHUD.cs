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

        [Tooltip("Mostra o círculo do alcance do taco no chão em volta do rebatedor da vez. Desligado no padrão " +
                 "(confundia com a marca de onde a bola vai).")]
        public bool mostrarAlcanceBatida = false;
        [Tooltip("Rebatedor controlado na zona de perigo: quantas vezes por segundo a seta vermelha pisca.")]
        public float piscadasPorSegundoPerigo = 3f;

        [Header("Tamanho na tela (marcadores e bola não crescem nas câmeras de perto)")]
        [Tooltip("Distância da câmera (m) em que marcadores e bola têm o tamanho configurado; mais perto, encolhem.")]
        public float distanciaReferenciaTela = 24f;
        [Tooltip("Abaixo desta distância (m) a bola aparece no tamanho de verdade; a partir da referência, no tamanho ampliado.")]
        public float distanciaBolaReal = 5f;

        [Header("Boneco: animação por código")]
        [Tooltip("Passadas por segundo andando devagar.")]
        public float passadasPorSegundoBase = 1.1f;
        [Tooltip("Passadas por segundo a mais para cada m/s de velocidade.")]
        public float passadasPorMetro = 0.32f;
        [Tooltip("Quanto a perna balança no sprint (graus).")]
        public float amplitudePernaMaxima = 50f;
        [Tooltip("Quanto o corpo inclina para a frente no sprint (graus).")]
        public float inclinacaoCorrendo = 15f;
        [Tooltip("Duração do gesto de soltar a bola (s).")]
        public float duracaoGestoLancar = 0.3f;
        [Tooltip("Duração do gesto de abaixar para pegar a bola (s).")]
        public float duracaoGestoPegar = 0.35f;

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
        [Tooltip("Pinta de vermelho a garrafa do rebatedor que está fora da zona. Desligado: o perigo aparece " +
                 "só no sinal em cima da cabeça do rebatedor (vermelho = fora da zona).")]
        public bool pintarGarrafaExposta = false;
        [Tooltip("Cor da garrafa quando o taco do dono está fora do chão (pode ser queimada).")]
        public Color corGarrafaExposta = new Color(0.95f, 0.2f, 0.15f);

        [Header("Poeira (bola quicando, garrafa caindo)")]
        [Tooltip("Material das partículas de poeira (criado pelo montador da cena).")]
        public Material materialPoeira;
        [Tooltip("Partículas num quique bem forte.")]
        public int particulasPoeira = 14;
        [Tooltip("Tamanho de cada nuvenzinha (m).")]
        public float tamanhoPoeira = 0.35f;
        [Tooltip("Quanto tempo a poeira fica no ar (s).")]
        public float duracaoPoeira = 1.4f;
        [Tooltip("Cor da poeira (alfa = quão forte).")]
        public Color corPoeira = new Color(0.62f, 0.58f, 0.52f, 0.35f);

        [Header("Rastro da bola (como o traço do golfe)")]
        [Tooltip("Desenha um rastro atrás da bola quando ela vai rápido (arremesso e batida).")]
        public bool rastroBola = true;
        [Tooltip("Material do rastro (criado pelo montador da cena).")]
        public Material materialRastro;
        [Tooltip("Velocidade mínima para o rastro aparecer (m/s).")]
        public float velocidadeMinimaRastro = 14f;
        [Tooltip("Quanto tempo o rastro fica no ar (s).")]
        public float duracaoRastro = 0.35f;
        [Tooltip("Largura do rastro perto da bola (m).")]
        public float larguraRastro = 0.05f;
        public Color corRastro = new Color(1f, 0.95f, 0.75f, 0.55f);

        [Header("Mensagens")]
        [Tooltip("Quanto tempo a mensagem do lance fica na tela (s).")]
        public float duracaoMensagem = 2f;
    }
}
