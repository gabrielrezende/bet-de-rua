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
        [Tooltip("Quanto vale cada cruzada validada. GDD: 1 no padrão, 2 como opção.")]
        public int pontosPorCruzada = 1;
        [Tooltip("Perdeu o taco em match point: os pontos caem pela metade (GDD, ligado no padrão).")]
        public bool caiMetadeAoPerderTacoNoMatchPoint = true;

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
        [Tooltip("A CPU mira até este tanto (m) para o lado da garrafa, ao acaso.")]
        public float desvioMiraAutomatico = 0.8f;
        [Tooltip("Se ninguém controla o rebatedor que recebe a bola, a CPU tenta rebater.")]
        public bool rebatedorAutomatico = true;
        [Tooltip("Erro de tempo da CPU na batida, para mais ou para menos (ms). 60 = acerta perfeito ~quase metade.")]
        public float erroTempoRebatedorAutomaticoMs = 110f;
        [Tooltip("Se ninguém controla um defensor, a CPU corre na bola, pega e chuta na garrafa exposta.")]
        public bool defesaAutomatica = true;
        [Tooltip("Se ninguém controla um rebatedor, a CPU corre e cruza (em espelho com você, ou sozinha).")]
        public bool corridaAutomatica = true;
        [Tooltip("Rebatedores bot só saem correndo se o defensor mais perto estiver a mais que isso da bola (m).")]
        public float folgaParaBotCorrer = 7f;
        [Tooltip("Defensor bot chuta na garrafa exposta quando chega a esta distância dela (m).")]
        public float distanciaChuteBot = 9f;
        [Tooltip("Tempo de reação do defensor bot antes de pegar ou chutar (s).")]
        public float reacaoBot = 0.25f;

        [Header("Controles")]
        [Tooltip("Máximo de pessoas jogando ao mesmo tempo (controles + teclado).")]
        public int maximoJogadoresHumanos = 4;
    }
}
