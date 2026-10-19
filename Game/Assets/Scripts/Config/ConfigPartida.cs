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
        [Header("Partida")]
        [Tooltip("Meta de pontos. Chegando nela, a dupla entra em match point e precisa fechar.")]
        public int metaPontos = 10;

        [Tooltip("Rebatedor bot: abre a batida para os lados entre estes ângulos (graus), contando da rua. " +
                 "Ângulos grandes mandam a bola nas fachadas e ela volta rápido para a defesa.")]
        public float aberturaMinimaBatidaBot = 8f;
        public float aberturaMaximaBatidaBot = 32f;

        [Header("Níveis dos bots (cada time tem o seu)")]
        [Tooltip("Nível dos bots do Lado 1 (começa no taco).")]
        public NivelDificuldade nivelLado1 = NivelDificuldade.Medio;
        [Tooltip("Nível dos bots do Lado 2.")]
        public NivelDificuldade nivelLado2 = NivelDificuldade.Medio;
        [Tooltip("O talento de cada nível: Fácil, Médio e Difícil, nessa ordem.")]
        public NivelBot[] niveisBots =
        {
            new NivelBot("Fácil", erroBatidaMs: 270f, desvioArremesso: 1.1f, desvioChute: 0.5f, reacao: 0.4f, margemRiscoCorrida: -0.75f, acertoAlturaBatida: 0.7f) { variarLugarArremesso = 0.3f },
            new NivelBot("Médio", erroBatidaMs: 230f, desvioArremesso: 0.8f, desvioChute: 0.35f, reacao: 0.25f, margemRiscoCorrida: -0.5f, acertoAlturaBatida: 0.8f) { variarLugarArremesso = 1.2f },
            new NivelBot("Difícil", erroBatidaMs: 150f, desvioArremesso: 0.35f, desvioChute: 0.18f, reacao: 0.12f, margemRiscoCorrida: -0.1f, acertoAlturaBatida: 0.93f) { variarLugarArremesso = 2.5f },
        };

        [Header("Simulação (bots contra bots, sem ninguém jogando)")]
        [Tooltip("No fim da partida, começa outra sozinha (para simular várias partidas seguidas e ver o log).")]
        public bool reiniciarSozinhoNoFim = false;
        [Tooltip("Na simulação, alterna quem começa no taco a cada partida (para ser justo).")]
        public bool alternarQuemComecaNoTaco = true;

        [Header("Log da partida (Game/Logs/Partida.log)")]
        [Tooltip("Grava tudo o que acontece na partida, em ordem de tempo, para entender depois.")]
        public bool gravarLog = true;
        [Tooltip("De quantos em quantos segundos o log tira uma 'foto' de todos os jogadores e da bola.")]
        public float intervaloFotoLog = 0.5f;
        [Tooltip("Quanto vale cada cruzada validada. GDD: 1 no padrão, 2 como opção.")]
        public int pontosPorCruzada = 1;
        [Tooltip("Os dois rebatedores se encontrando no meio já cruzam os tacos, sem apertar botão. " +
                 "Desligado: precisa apertar X (A) / Espaço no anel.")]
        public bool cruzadaAutomatica = true;
        [Tooltip("Lancha: o arremesso bateu no corpo de um rebatedor (o da vez ou o parceiro). A bola segue em jogo. " +
                 "Com este tanto de lanchas a dupla do taco perde os tacos (troca de papel).")]
        public int lanchasParaPerderTaco = 3;
        [Tooltip("Lancha (regra de algumas regiões): o arremesso no corpo de um rebatedor conta contra o time do taco. " +
                 "Desligado no padrão: a bola só desvia e o jogo segue. A falta que vale é a queima errada " +
                 "(jogar na garrafa com o taco do dono na base = ponto para o time do taco).")]
        public bool contarLancha = false;
        [Tooltip("Com a lancha ligada: a bola no corpo do rebatedor DA VEZ (o que ia rebater) também conta. " +
                 "Desligado: só conta no parceiro parado na outra base.")]
        public bool lanchaNoRebatedorDaVez = false;
        [Tooltip("Corpo na frente: o arremesso bate no corpo de um rebatedor E ia acertar a garrafa = ele protegeu a garrafa " +
                 "com o corpo (só vale o taco). Conta uma falta para o time do taco.")]
        public bool faltaCorpoNaFrente = true;
        [Tooltip("Com este tanto de faltas de corpo na frente, o time do taco perde os tacos (troca de papel).")]
        public int faltasCorpoParaPerderTaco = 3;
        [Tooltip("Queimou errado: a defesa derrubou a garrafa com o taco do dono já no chão. " +
                 "O ataque ganha este tanto de pontos (0 = só não vale).")]
        public int pontosQueimaErrada = 1;
        [Tooltip("Depois de 'queimou errado' a garrafa volta de pé e fica este tempo (s) sem poder cair, " +
                 "para a mesma bola encostada não contar várias vezes.")]
        public float imunidadeGarrafaDePe = 0.6f;
        [Tooltip("Perdeu o taco em match point: os pontos caem pela metade (GDD, ligado no padrão).")]
        public bool caiMetadeAoPerderTacoNoMatchPoint = true;
        [Tooltip("Pegar a bola rebatida direto no ar, antes de quicar, tira os tacos do ataque. " +
                 "Desligado no protótipo: na rua quase toda bola chega quicando ou volta da parede.")]
        public bool pegarNoArPerdeTaco = false;

        [Header("Posições iniciais")]
        [Tooltip("Distância do rebatedor à garrafa, de lado (+Z), no começo do lance (m). Fica ao lado da área do taco, na zona segura.")]
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
        [Tooltip("Segurança: se o lance não acabar sozinho, acaba depois deste tempo (s).")]
        public float tempoMaximoBolaViva = 40f;

        [Header("Corrida e cruzada")]
        [Tooltip("Distância máxima entre os dois rebatedores para a cruzada valer (m). GDD: 1,5.")]
        public float distanciaCruzada = 1.5f;
        [Tooltip("O parceiro bot corre em espelho, deslocado para o lado este tanto (m), para os dois não se trombarem.")]
        public float afastamentoParceiroEspelho = 0.8f;
        [Tooltip("A defesa com a bola e os dois rebatedores seguros por este tempo (s) = fim do lance.")]
        public float tempoSeguroParaEncerrar = 1.5f;
        [Tooltip("Tempo mostrando QUEIMOU / PEGOU NO AR antes de trocar os papéis (s).")]
        public float tempoTrocaDePapel = 2.5f;

        [Header("Bots (CPU) — jogam a dupla sem ninguém")]
        [Tooltip("Modo assistir: ninguém entra no jogo e os 4 jogadores são bots. F2 liga e desliga com o jogo rodando.")]
        public bool soAssistir = false;
        [Tooltip("Se ninguém controla o arremessador, a CPU arremessa sozinha.")]
        public bool arremessadorAutomatico = true;
        [Tooltip("Quanto tempo a CPU espera com a bola antes de arremessar (s).")]
        public float esperaArremessoAutomatico = 2f;
        [Tooltip("Se ninguém controla o rebatedor que recebe a bola, a CPU tenta rebater.")]
        public bool rebatedorAutomatico = true;
        [Tooltip("Se ninguém controla um defensor, a CPU corre na bola, pega e chuta na garrafa exposta.")]
        public bool defesaAutomatica = true;
        [Tooltip("Se ninguém controla um rebatedor, a CPU corre e cruza (em espelho com você, ou sozinha).")]
        public bool corridaAutomatica = true;
        [Tooltip("Rebatedores bot só saem correndo se o defensor mais perto estiver a mais que isso da bola (m).")]
        public float folgaParaBotCorrer = 7f;
        [Tooltip("Rebatedores bot que decidiram ficar olham de novo a cada este tanto de segundos, enquanto a bola está solta.")]
        public float intervaloReavaliarCorrida = 1f;
        [Tooltip("Tempo (s) que a defesa leva, depois de chegar na bola, para pegar e o chute voar até a garrafa.")]
        public float tempoDefesaPegarEJogar = 1.1f;
        [Tooltip("Tempo (s) a mais na viagem do rebatedor: arrancar, acelerar e o taco descer na base.")]
        public float tempoArrancadaCorrida = 0.5f;
        [Tooltip("Defensor bot chuta na garrafa exposta quando chega a esta distância dela (m).")]
        public float distanciaChuteBot = 9f;
        [Tooltip("Defensor bot só chuta se a bola chegar na garrafa este tanto (s) antes do dono voltar para a base. " +
                 "Senão anda com a bola para perto da garrafa (errar daria tempo para o ataque cruzar).")]
        public float folgaParaChutarBot = 0.15f;
        [Tooltip("Tempo de reação (s) do defensor bot antes de soltar o chute (ele não enche o arco como a pessoa).")]
        public float reacaoChuteBot = 0.15f;
        [Tooltip("O arremessador bot só arremessa quando está a até esta distância (m) do lugar dele, atrás da garrafa " +
                 "(senão pode jogar a bola no próprio parceiro rebatedor).")]
        public float folgaLugarArremessoCpu = 0.5f;
        [Tooltip("Tempo de reação (s) que o defensor bot supõe que o rebatedor fora da zona leva para começar a voltar. " +
                 "Maior = o bot arrisca jogar na garrafa mais vezes.")]
        public float reacaoRebatedor = 0.3f;
        [Tooltip("Rebatedores bot correm em faixas separadas no meio do caminho; a esta distância (m) da base vão direto ao ponto.")]
        public float distanciaSairDaFaixa = 3f;
        [Tooltip("Parceiro bot de uma pessoa no taco: sai para a viagem quando a pessoa já está a esta distância (m) " +
                 "da base, rumo ao meio (ou correndo para o meio).")]
        public float distanciaParaParceiroSair = 2f;
        [Tooltip("Parceiro bot de uma pessoa no taco: a pessoa precisa estar correndo para o meio (ou de volta) com " +
                 "pelo menos esta velocidade (m/s) para ele sair (ou desistir junto).")]
        public float velocidadeParaParceiroSair = 2f;
        [Tooltip("Parceiro bot: tempo mínimo (s) entre uma decisão e outra (sair / desistir), para não ficar indo e voltando.")]
        public float intervaloDecisaoParceiro = 0.6f;
        [Tooltip("Apoio: com a bola a mais que isso (m) da casinha que ele guarda, o defensor bot que não vai " +
                 "na bola sai para o meio do caminho, para receber o passe.")]
        public float distanciaParaApoio = 8f;
        [Tooltip("Apoio: onde ele fica, entre a casinha que guarda (0) e a bola (1).")]
        [Range(0f, 1f)] public float fracaoApoio = 0.45f;

        [Header("Controles")]
        [Tooltip("Na defesa, trocar sozinho você para quem chega antes na bola (e para quem pegou a bola). " +
                 "Desligado = você fica com quem escolheu e troca com L1 (GDD: modo manual).")]
        public bool trocaAutomaticaNaDefesa = false;
        [Tooltip("Máximo de pessoas jogando ao mesmo tempo (controles + teclado).")]
        public int maximoJogadoresHumanos = 4;
    }
}
