using UnityEngine;

namespace BetsDeRua
{
    /// <summary>
    /// Todos os números de física e de campo do jogo num lugar só.
    /// Os scripts leem estes valores a cada frame, então dá para ajustar
    /// no Inspector com o jogo rodando e ver o efeito na hora.
    /// Atenção: mudanças feitas no Play mode ficam salvas (é um asset).
    /// </summary>
    [CreateAssetMenu(fileName = "ConfigFisica", menuName = "Bets de Rua/Config Fisica")]
    public class ConfigFisica : ScriptableObject
    {
        [Header("Campo (usado pelo menu Bets > Montar cena Prototipo)")]
        [Tooltip("Comprimento da rua jogável, de ponta a ponta (m). Eixo X.")]
        public float comprimentoCampo = 70f;
        [Tooltip("Largura total, de fachada a fachada (rua + as duas calçadas) (m). Eixo Z.")]
        public float larguraCampo = 16f;
        [Tooltip("Largura só do asfalto, entre os dois meios-fios (m). O resto até as fachadas é calçada.")]
        public float larguraRua = 11f;
        [Tooltip("Altura da calçada (o degrau do meio-fio) (m). A bola rolando bate no meio-fio; na altura da quina, pula.")]
        public float alturaCalcada = 0.15f;
        [Tooltip("Quanto da velocidade a bola guarda ao bater no meio-fio (0 a 1).")]
        [Range(0f, 1f)] public float quiqueMeioFio = 0.5f;
        [Tooltip("Altura das fachadas (casas, muros e portões) do lado do fundo da tela (m). Só visual: a bola sempre volta.")]
        public float alturaFachada = 3f;
        [Tooltip("Altura do muro do lado perto da câmera (m): baixo, para não tampar o campo. Só visual.")]
        public float alturaMuroPerto = 0.8f;
        [Tooltip("Distância entre o centro das duas bases (m).")]
        public float distanciaBases = 20f;
        [Tooltip("Área do taco (retângulo de giz na frente da garrafa, atravessando a rua): largura (m), " +
                 "de um lado ao outro da rua. Mudou algum desenho? Rode o menu Bets > Montar cena Prototipo.")]
        public float larguraAreaTaco = 1.4f;
        [Tooltip("Área do taco: profundidade (m), na direção do arremesso.")]
        public float profundidadeAreaTaco = 0.6f;
        [Tooltip("Lado da casinha de giz da garrafa (m): o quadradinho encostado atrás da área do taco, no meio.")]
        public float ladoCasinhaGarrafa = 0.35f;
        [Tooltip("Zona segura (invisível): folga em volta da área do taco e da casinha (m). Com o rebatedor aí " +
                 "dentro, o taco fica no chão sozinho e a garrafa está protegida; fora, a garrafa fica exposta.")]
        public float margemZonaSegura = 0.6f;
        [Tooltip("Largura da faixa do meio, onde a cruzada vale (m).")]
        public float larguraFaixaMeio = 3f;
        [Tooltip("Espessura das linhas de giz da faixa do meio (m).")]
        public float espessuraLinhaGiz = 0.08f;
        [Tooltip("Altura dos muros baixos nas bordas (m).")]
        public float alturaMuro = 0.6f;
        [Tooltip("Espessura dos muros (m).")]
        public float espessuraMuro = 0.3f;

        [Header("Gravidade e bola")]
        [Tooltip("Gravidade da bola (m/s²). A real é 9,8; mais baixa deixa a bola um tiquinho mais no ar.")]
        public float gravidade = 8.5f;
        [Tooltip("Raio real da bola de tênis (m), usado nas colisões.")]
        public float raioBola = 0.029f;
        [Tooltip("Massa da bola (kg).")]
        public float massaBola = 0.057f;
        [Tooltip("Quanto a bola aparece maior que o tamanho real, só no visual, para ler na câmera de longe.")]
        public float escalaVisualBola = 3f;
        [Tooltip("Resistência do ar: desaceleração = arrasto × velocidade². ~0,015 para bola de tênis.")]
        public float arrastoAr = 0.015f;

        [Header("Quique e rolar")]
        [Tooltip("Quanto da velocidade vertical sobra a cada quique no asfalto (0 a 1).")]
        [Range(0f, 1f)] public float quiqueAsfalto = 0.7f;
        [Tooltip("Quanto da velocidade horizontal sobra a cada quique no chão (0 a 1). Atrito no contato.")]
        [Range(0f, 1f)] public float atritoQuique = 0.9f;
        [Tooltip("Quanto da velocidade sobra ao bater no muro (0 a 1).")]
        [Range(0f, 1f)] public float quiqueMuro = 0.55f;
        [Tooltip("Abaixo desta velocidade vertical (m/s) a bola para de quicar e passa a rolar.")]
        public float velocidadeMinimaQuique = 0.6f;
        [Tooltip("Desaceleração da bola rolando no asfalto (m/s²).")]
        public float atritoRolar = 1.2f;
        [Tooltip("Abaixo desta velocidade (m/s) a bola rolando para.")]
        public float velocidadeParada = 0.05f;
        [Tooltip("Maior passo de simulação da bola (s). Menor = colisões mais precisas.")]
        public float passoSimulacao = 0.004f;

        [Header("Garrafa (casinha)")]
        [Tooltip("Altura da garrafa (m).")]
        public float alturaGarrafa = 0.3f;
        [Tooltip("Diâmetro da garrafa (m).")]
        public float diametroGarrafa = 0.1f;
        [Tooltip("Massa da garrafa (kg).")]
        public float massaGarrafa = 0.3f;
        [Tooltip("Velocidade mínima da bola para derrubar a garrafa (km/h).")]
        public float velocidadeDerrubaKmh = 15f;
        [Tooltip("Multiplica o empurrão que a bola dá na garrafa ao derrubar.")]
        public float forcaDerrubada = 1f;
        [Tooltip("Bolada fraca (não derruba): quanto da velocidade a bola guarda ao voltar da garrafa (0 a 1).")]
        [Range(0f, 1f)] public float quiqueGarrafa = 0.4f;
        [Tooltip("Curva: depois de soltar o arremesso, o analógico direito empurra a bola de lado com esta aceleração (m/s²).")]
        public float aceleracaoCurva = 6f;
        [Tooltip("Curva: o máximo de velocidade de lado que ela pode ganhar no total (m/s). Limita o quanto a bola " +
                 "desvia, para continuar dando para acertar a garrafa.")]
        public float velocidadeLateralMaximaCurva = 1.5f;
        [Tooltip("Lancha: quanto da velocidade a bola guarda ao desviar no corpo do rebatedor (0 a 1).")]
        [Range(0f, 1f)] public float quiqueCorpo = 0.3f;
        [Tooltip("Bolada que derruba: a bola segue em frente guardando esta parte da velocidade (0 a 1).")]
        [Range(0f, 1f)] public float velocidadeQueSegueAoDerrubar = 0.6f;
        [Tooltip("Raspão: se a bola passar a até esta distância da garrafa (m, de borda a borda), ela balança sem cair.")]
        public float distanciaRaspao = 0.08f;
        [Tooltip("Raspão: velocidade mínima da bola para fazer a garrafa balançar (km/h).")]
        public float velocidadeRaspaoKmh = 25f;
        [Tooltip("Raspão: o balanço é esta fração do balanço de bolada fraca (0 a 1).")]
        [Range(0f, 1f)] public float fracaoBalancoRaspao = 0.5f;
        [Tooltip("Inclinação máxima do balanço de 'quase!' quando a bola é fraca (graus).")]
        public float anguloBalanco = 10f;
        [Tooltip("Duração do balanço de 'quase!' (s).")]
        public float duracaoBalanco = 0.7f;

        [Header("Jogadores: velocidades")]
        [Tooltip("Velocidade andando (m/s): analógico leve, ou L2/LT / Alt.")]
        public float velocidadeAndar = 2f;
        [Tooltip("Velocidade correndo (m/s): analógico todo.")]
        public float velocidadeCorrer = 5f;
        [Tooltip("Velocidade no sprint (m/s): R1/RB ou Shift.")]
        public float velocidadeSprint = 7f;
        [Tooltip("Até quanto do analógico (0 a 1) conta como andar. Acima disso vai acelerando até correr.")]
        [Range(0.1f, 0.9f)] public float limiteAnalogicoAndar = 0.5f;
        [Tooltip("Quanto a velocidade cresce por segundo ao acelerar (m/s²).")]
        public float aceleracaoJogador = 14f;
        [Tooltip("Quanto a velocidade cai por segundo ao frear ou mudar de direção (m/s²). " +
                 "Mudar de direção bruscamente custa um tempinho, como meio passo.")]
        public float frenagemJogador = 18f;
        [Tooltip("Rapidez para virar o corpo (graus/s).")]
        public float velocidadeVirar = 720f;

        [Tooltip("Velocidade máxima com a bola na mão (m/s), sem sprint (GDD).")]
        public float velocidadeComBola = 4f;
        [Tooltip("Com a bola na mão, quanto o arremessador pode ir à frente da própria garrafa, rumo ao alvo (m). 0 = não passa dela.")]
        public float avancoMaximoComBola = 0f;

        [Header("Jogadores: estamina do sprint")]
        [Tooltip("Quantos segundos de sprint seguido com a estamina cheia.")]
        public float duracaoSprint = 6f;
        [Tooltip("Quantos segundos para recarregar a estamina do zero até cheia.")]
        public float recargaSprint = 4f;

        [Header("Jogadores: corpo e taco")]
        [Tooltip("Altura da cápsula do jogador (m).")]
        public float alturaJogador = 1.75f;
        [Tooltip("Raio da cápsula do jogador (m).")]
        public float raioJogador = 0.3f;
        [Tooltip("Comprimento do taco (m).")]
        public float comprimentoTaco = 0.73f;
        [Tooltip("Espessura do taco (m). O real tem ~5 cm; mais grosso para ver de longe.")]
        public float espessuraTaco = 0.1f;
        [Tooltip("Altura da mão que segura o taco (m). Precisa ser menor que o comprimento do taco para ele alcançar o chão.")]
        public float alturaMaoTaco = 0.7f;
        [Tooltip("Abaixo desta velocidade (m/s) o rebatedor conta como parado (taco desce no círculo).")]
        public float velocidadeParadoJogador = 0.2f;
        [Tooltip("Tempo (s) para o taco subir ou descer.")]
        public float tempoMovimentoTaco = 0.12f;
        [Tooltip("Fora da zona segura o taco levanta este tanto (graus) a partir do chão: a ponta sai do chão, sem ir para o ombro.")]
        public float anguloTacoLevantado = 25f;
        [Tooltip("Cruzada: quanto tempo (s) os dois ficam com os tacos levantados e batendo em X.")]
        public float duracaoBaterTacos = 0.35f;
        [Tooltip("Rebatedor se jogando (□ correndo): a ponta do taco esticado alcança a base até esta distância (m) à frente.")]
        public float alcanceTacoNoMergulho = 1.4f;
        [Tooltip("Cruzada: quanto o taco sobe ao bater no outro (1 = 45 graus; maior = mais em pé).")]
        public float alturaBaterTacos = 1.2f;

        [Header("Arremesso: soltura")]
        [Tooltip("Altura da mão na hora de soltar a bola (m).")]
        public float alturaSoltura = 1.4f;
        [Tooltip("Quanto à frente do corpo a bola sai (m).")]
        public float avancoSoltura = 0.35f;
        [Tooltip("Quanto para o lado direito do corpo a bola sai (m).")]
        public float ladoSoltura = 0.25f;

        [Header("Arremesso: arco de força")]
        [Tooltip("Segundos segurando o botão para o arco encher de 0 até o fim.")]
        public float tempoEncherForca = 1.2f;
        [Tooltip("Onde começa a faixa verde no arco (0 a 1).")]
        [Range(0f, 1f)] public float inicioFaixaVerde = 0.72f;
        [Tooltip("Onde termina a faixa verde no arco (0 a 1). Soltar depois disso = bola desgovernada.")]
        [Range(0f, 1f)] public float fimFaixaVerde = 0.86f;

        [Header("Arremesso: velocidades (km/h, na horizontal)")]
        [Tooltip("○ (B) forte: velocidade com o arco vazio.")]
        public float forteMinimaKmh = 55f;
        [Tooltip("○ (B) forte: velocidade soltando no fim da faixa verde.")]
        public float forteMaximaKmh = 90f;
        public float quicadoMinimaKmh = 45f;
        public float quicadoMaximaKmh = 65f;
        public float pingoMinimaKmh = 32f;
        public float pingoMaximaKmh = 45f;

        [Header("Arremesso: trajetória e mira")]
        [Tooltip("X (A) quicado: no máximo quantos metros antes do alvo a bola quica (mais perto se precisar, para não passar por cima).")]
        public float distanciaQuique = 1.5f;
        [Tooltip("△ (Y) pingo: altura em que a bola chega no alvo, caindo por cima (m).")]
        public float alturaChegadaPingo = 0.25f;
        [Tooltip("Até quantos metros da garrafa o analógico leva a mira.")]
        public float raioMira = 2f;
        [Tooltip("Curva do analógico na mira: 1 = linear; maior = mais fino perto do centro (2 = metade do curso " +
                 "move só 1/4 do raioMira).")]
        public float curvaMiraAnalogico = 2.2f;
        [Tooltip("Volta a mira fina no analógico direito (esquerda/direita da garrafa). Desligado no padrão: a bola vai " +
                 "no rumo da garrafa e a habilidade é a força e o tipo.")]
        public bool miraFinaNoAnalogico = false;
        [Tooltip("Arremessar andando tira a precisão: erro de direção (graus) para cada m/s de velocidade ao soltar. " +
                 "Parado = preciso; andando = arriscado, mas dá para buscar outro ângulo.")]
        public float erroArremessoPorVelocidade = 0.6f;
        [Tooltip("Bate-pronto (□ depois de soltar o arremesso): a bola guarda esta parte da velocidade e desce...")]
        [Range(0.2f, 1f)] public float velocidadeBatePronto = 0.7f;
        [Tooltip("...com esta velocidade para baixo (m/s): quica no meio do caminho e chega mais baixa.")]
        public float descidaBatePronto = 6f;
        [Tooltip("Força = distância. Soltando com o arco vazio, a bola vai só esta fração da distância até o alvo (cai curta).")]
        public float fatorDistanciaSemForca = 0.45f;
        [Tooltip("Força = distância. Com o arco cheio (passou do verde), a bola vai esta vezes a distância até o alvo (isola).")]
        public float fatorDistanciaForcaMaxima = 1.9f;
        [Tooltip("Rasteira (R2 + analógico direito para baixo): velocidade mínima e máxima (km/h).")]
        public float rasteiraMinimaKmh = 35f;
        public float rasteiraMaximaKmh = 70f;
        [Tooltip("Maior velocidade para cima (m/s) que um arremesso pode ter: a bola isola, mas sem subir demais.")]
        public float subidaMaximaArremesso = 9f;
        [Tooltip("Rasteira: quanto (0 a 1) o analógico direito precisa estar para baixo, segurando o R2.")]
        [Range(0.1f, 0.95f)] public float limiteAnalogicoRasteira = 0.5f;
        [Tooltip("Liga o sorteio de erro de direção fora da faixa verde (o jeito antigo). Desligado: a força só " +
                 "muda a distância, sem sorte.")]
        public bool erroAleatorioForaDaFaixa = false;
        [Tooltip("Erro máximo de direção soltando antes da faixa verde (graus). " +
                 "A garrafa é fina: 0,3° já erra por ~10 cm a 20 m.")]
        public float erroMaximoAntesDaFaixa = 0.5f;
        [Tooltip("Erro de direção da bola desgovernada, mínimo e máximo (graus).")]
        public float erroDesgovernadaMinimo = 3f;
        public float erroDesgovernadaMaximo = 7f;
        [Tooltip("Quanto a bola desgovernada sobe a mais (0,3 = 30% mais subida).")]
        public float subidaExtraDesgovernada = 0.3f;

        [Header("Batida: alcance e tempo")]
        [Tooltip("Até quantos metros do rebatedor, para o lado, o taco alcança a bola.")]
        public float alcanceTaco = 1.5f;
        [Tooltip("Quanto à frente do rebatedor (rumo ao arremesso) fica o ponto de contato (m). " +
                 "A bola é rebatida aí, antes de chegar na garrafa.")]
        public float avancoContatoBatida = 0.6f;
        [Tooltip("Bola mais alta que isso (m) passa por cima do alcance do taco.")]
        public float alturaMaximaAlcance = 1.6f;
        [Tooltip("Janela da batida perfeita: até quantos ms antes ou depois do momento certo.")]
        public float janelaPerfeitaMs = 50f;
        [Tooltip("Janela da batida boa (ms). Fora dela, o taco passa no vazio.")]
        public float janelaBoaMs = 120f;
        [Tooltip("A batida vai sempre para a frente: no máximo este ângulo (graus) para os lados, contando da direção de volta pela rua.")]
        public float anguloMaximoBatida = 80f;
        [Tooltip("Depois deste tempo (s) do arremesso, ou com a bola parada, o rebatedor desiste de esperar a bola " +
                 "(e a defesa já pode ir buscar).")]
        public float tempoMaximoEsperaBatida = 3f;
        [Tooltip("A garrafa atrás de quem lança (até esta distância, m) não conta até a bola se afastar dela (a rasteira não derruba a própria garrafa).")]
        public float distanciaGarrafaAtras = 1.6f;
        [Tooltip("Quem lançou a bola só pode pegá-la de volta depois que ela se afastar esta distância (m) de onde saiu. " +
                 "Evita o arremessador pegar a própria bola logo depois de soltar.")]
        public float distanciaLiberarPropriaBola = 2.5f;
        [Tooltip("Rebatedor esperando o arremesso: 0 = olha para a área do taco, 1 = olha para quem arremessa.")]
        [Range(0f, 1f)] public float diagonalRebatedor = 0.5f;
        [Tooltip("Por quantos segundos à frente a trajetória da bola é prevista.")]
        public float tempoPrevisao = 3f;
        [Tooltip("De quanto em quanto tempo (s) a previsão guarda um ponto.")]
        public float passoPrevisao = 0.01f;

        [Header("Batida: força e direção")]
        [Tooltip("Segundos segurando R2 para a batida forte encher toda a força.")]
        public float tempoEncherBatida = 0.7f;
        [Tooltip("R2 forte: velocidade com a força vazia e cheia (km/h). GDD: perfeita sai a 80-110.")]
        public float batidaForteMinimaKmh = 70f;
        public float batidaForteMaximaKmh = 110f;
        [Tooltip("R1 toque: velocidade (km/h). GDD: 30-45, rasteira e curta.")]
        public float batidaToqueKmh = 38f;
        [Tooltip("Taco baixo (L2, varrida): velocidade mínima e máxima (km/h) e ângulo (graus): rasteira forte.")]
        public float batidaVarridaMinimaKmh = 45f;
        public float batidaVarridaMaximaKmh = 85f;
        public float anguloBatidaVarrida = 2f;
        [Tooltip("Bola passando até esta altura (m) na linha da batida é 'baixa' (pede taco baixo, L2); acima, 'no ar' (taco alto, R2).")]
        public float alturaLimiteTacoBaixo = 0.13f;
        [Tooltip("Perto do limite (± este tanto, m), os dois tacos servem.")]
        public float toleranciaAlturaTaco = 0.05f;
        [Tooltip("Espirrada (taco na altura errada): velocidade (km/h), abertura para os lados (graus) e subida (m/s).")]
        public float espirradaMinimaKmh = 10f;
        public float espirradaMaximaKmh = 25f;
        public float aberturaEspirrada = 60f;
        public float subidaEspirrada = 1.5f;
        [Tooltip("L2 cavadinha: velocidade (km/h). Vai longe e alto, mas arrisca ser pega no ar.")]
        public float batidaCavadinhaKmh = 55f;
        [Tooltip("Ângulo de subida de cada batida (graus). GDD: cavadinha 35-50.")]
        public float anguloBatidaForte = 18f;
        public float anguloBatidaToque = 3f;
        public float anguloBatidaCavadinha = 42f;
        [Tooltip("Batida boa (fora da perfeita) sai com esta fração da velocidade.")]
        [Range(0f, 1f)] public float fatorBatidaBoa = 0.8f;
        [Tooltip("Batida boa sai torta até este tanto (graus), quanto mais longe do perfeito.")]
        public float erroDirecaoBatidaBoa = 12f;
        [Tooltip("Duração do giro do taco na batida (s).")]
        public float duracaoGiroTaco = 0.18f;

        [Header("Defesa: pegar a bola e mergulhar")]
        [Tooltip("Até quantos metros da bola (no chão) □ pega ela.")]
        public float raioPegar = 1f;
        [Tooltip("Bola mais alta que isso (m) não dá para pegar.")]
        public float alturaMaximaPegar = 2.2f;
        [Tooltip("Com a bola entre raioPegar e esta distância (m), □ vira mergulho / carrinho.")]
        public float alcanceMergulho = 3.5f;
        [Tooltip("Velocidade do bote do mergulho (m/s).")]
        public float velocidadeMergulho = 8f;
        [Tooltip("Duração do bote do mergulho (s): é quando ele pode alcançar a bola.")]
        public float duracaoBoteMergulho = 0.35f;
        [Tooltip("Tempo caído depois do mergulho (s), sem poder andar. O risco de mergulhar.")]
        public float duracaoCaidoMergulho = 0.6f;
        [Tooltip("No bote, pega a bola a até esta distância (m).")]
        public float raioPegarNoMergulho = 1.3f;
        [Tooltip("□ com a bola vindo: se ela vai entrar no alcance normal dentro deste tempo (s), ele espera e pega " +
                 "sem se jogar. Só mergulha se a bola vai passar longe.")]
        public float tempoEsperarBolaChegar = 0.6f;
        [Tooltip("Quanto o corpo deita no mergulho (graus).")]
        public float anguloMergulho = 75f;
        [Tooltip("Bola abaixo desta altura (m) = carrinho (de pé na frente, deitado para trás); acima = peixinho (de peito).")]
        public float alturaMaximaCarrinho = 0.35f;
        [Tooltip("Depois do bote, a deslizada no chão: quanto a velocidade cai por segundo (m/s²). Menor = desliza mais longe.")]
        public float freioDeslizada = 16f;
        [Tooltip("Parte final do tempo caído (0 a 1) em que ele vai levantando o corpo.")]
        [Range(0.1f, 1f)] public float fracaoLevantar = 0.4f;

        [Header("Defesa: com a bola na mão")]
        [Tooltip("No campo, segundos para o arco do chute encher (mais rápido que o arremesso: pegar, virar e já chutar).")]
        public float tempoEncherChute = 0.6f;
        [Tooltip("R1 passe: velocidade na horizontal (km/h). Baixo, quase rasteiro.")]
        public float passeKmh = 45f;
        [Tooltip("L2 lançamento: velocidade na horizontal (km/h). Por cima.")]
        public float lancamentoKmh = 38f;
        [Tooltip("Altura em que o passe chega no colega (m).")]
        public float alturaChegadaPasse = 1f;
        [Tooltip("Altura em que o lançamento chega no colega (m).")]
        public float alturaChegadaLancamento = 1.3f;
        [Tooltip("Passe do colega é pego sozinho quando a bola chega a até esta distância (m).")]
        public float raioReceberPasse = 1.4f;

        [Header("Corpo do rebatedor (movimento)")]
        [Tooltip("Parte do giro do taco que o quadril acompanha na batida (0 a 1). Os pés ficam plantados.")]
        [Range(0f, 1f)] public float torcaoQuadril = 0.3f;
        [Tooltip("Parte do giro do taco que o tronco (coluna e peito) acompanha, somada à do quadril (0 a 1).")]
        [Range(0f, 1f)] public float torcaoTronco = 0.45f;
        [Tooltip("Quanto o quadril desce na guarda (m): joelhos dobrados, pronto para bater.")]
        public float agachamentoGuarda = 0.09f;
        [Tooltip("Quanto o quadril vai para trás na guarda (m), como o golfista se preparando: o tronco inclina por cima e os joelhos dobram pouco.")]
        public float recuarQuadrilGuarda = 0.07f;
        [Tooltip("Quanto o tronco inclina para a frente na guarda (graus).")]
        public float inclinacaoGuarda = 16f;
        [Tooltip("Segundos para o corpo destorcer e voltar à guarda depois da batida (o 'follow-through').")]
        public float tempoVoltarDaBatida = 0.45f;
        [Tooltip("Quanto da guarda já vem 'carregada' para trás antes de bater (graus de torção).")]
        public float carregarGuarda = 18f;
        [Tooltip("Usa a rebatida gravada de uma pessoa de verdade (captura da CMU) na batida. Desligado: o giro é todo feito por código.")]
        public bool usarCapturaNaBatida = true;
        [Tooltip("Em que segundo da captura a batida começa quando o taco gira (o fim do 'carregar').")]
        public float inicioBatidaCaptura = 0.42f;
        [Tooltip("Rapidez da captura até o contato (o giro do jogo é mais curto que o da gravação).")]
        public float rapidezBatidaCaptura = 1.6f;
        [Tooltip("Segundos de batida acelerada; depois a finalização toca na rapidez normal.")]
        public float tempoBatidaRapida = 0.14f;

        [Header("Guarda do bets (taco com a ponta no chão, as duas mãos no cabo)")]
        [Tooltip("Traz o corpo da gravação de volta para cima do jogador na guarda (0 a 1). A captura de beisebol anda uns 40 cm para o lado e o braço não alcançava o taco.")]
        [Range(0f, 1f)] public float centralizarCorpoGuarda = 1f;
        [Tooltip("Onde fica a pega (mãos) na guarda: à frente do peito (m).")]
        public float pegaGuardaFrente = 0.14f;
        [Tooltip("Onde fica a pega na guarda: abaixo da linha dos ombros (m). Cintura = ~0.45.")]
        public float pegaGuardaAbaixo = 0.42f;
        [Tooltip("Onde fica a pega na guarda: para o lado direito do peito (m) (destro: um pouco para a direita).")]
        public float pegaGuardaLado = 0.03f;
        [Tooltip("Onde a ponta do taco encosta no chão na guarda: à frente do jogador (m).")]
        public float pontaGuardaFrente = 0.55f;
        [Tooltip("Onde a ponta do taco encosta no chão na guarda: para a direita (m).")]
        public float pontaGuardaLado = 0.12f;
        [Tooltip("Inclinação do tronco para a frente na guarda do bets (graus), como o golfista no 'address': quadril para trás, braços caindo retos.")]
        public float inclinacaoGuardaBets = 22f;
        [Tooltip("Quanto o quadril desce na guarda do bets (m): joelhos dobrados.")]
        public float agachamentoGuardaBets = 0.07f;
        [Tooltip("Distância entre as duas mãos no cabo (m).")]
        public float distanciaEntreMaos = 0.09f;
        [Tooltip("Largura entre os pés de quem está parado, em fração da altura do quadril (0,42 ≈ largura dos ombros). " +
                 "A animação de base abria demais as pernas.")]
        [Range(0.2f, 0.8f)] public float larguraPes = 0.42f;

        [Header("Torcida")]
        [Tooltip("A torcida abre caminho: quem está assistindo se afasta quando um jogador chega a esta distância (m).")]
        public float distanciaTorcidaAbrir = 1.8f;
        [Tooltip("Quanto quem está assistindo se afasta, no máximo (m).")]
        public float passoTorcida = 1.3f;
    }
}
