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
        [Tooltip("Comprimento do campo jogável, de muro a muro (m). Eixo X.")]
        public float comprimentoCampo = 50f;
        [Tooltip("Largura do campo jogável (m). Eixo Z.")]
        public float larguraCampo = 14f;
        [Tooltip("Distância entre o centro das duas bases (m).")]
        public float distanciaBases = 20f;
        [Tooltip("Diâmetro do círculo da base (m). O real tem 0,6; ampliado para ler na tela.")]
        public float diametroBase = 1.2f;
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
        public float escalaVisualBola = 5f;
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
        [Tooltip("Quanto da velocidade a bola guarda ao bater na garrafa (0 a 1).")]
        [Range(0f, 1f)] public float quiqueGarrafa = 0.4f;
        [Tooltip("Inclinação máxima do balanço de 'quase!' quando a bola é fraca (graus).")]
        public float anguloBalanco = 10f;
        [Tooltip("Duração do balanço de 'quase!' (s).")]
        public float duracaoBalanco = 0.7f;

        [Header("Lançador de teste (só para a Etapa 1)")]
        [Tooltip("Velocidade do lançamento forte de teste (km/h).")]
        public float velocidadeTesteForteKmh = 70f;
        [Tooltip("Velocidade da bola fraca de teste, rolando no chão (km/h). Chega abaixo do limite da garrafa.")]
        public float velocidadeTesteFracoKmh = 7f;
        [Tooltip("Altura de onde a bola de teste sai (m).")]
        public float alturaSaidaTeste = 1.2f;
        [Tooltip("De quantos metros da garrafa sai a bola fraca rolando (m).")]
        public float distanciaTesteFraco = 1f;
    }
}
