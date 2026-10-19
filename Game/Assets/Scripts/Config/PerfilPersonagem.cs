using UnityEngine;

namespace BetsDeRua
{
    public enum FaixaEtaria { Crianca, Adolescente, Adulto }
    public enum TipoCorpo { Magro, Medio, Forte }
    public enum EstiloCabelo { Careca, Raspado, Curto, BlackPower, Moicano, Longo, Trancas }
    public enum Chapeu { Nada, Bone, BoneParaTras, Bucket, Touca }
    public enum Barba { Nada, Bigode, Cavanhaque, Cheia }
    public enum Camisa { Camiseta, Regata, MangaLonga }
    public enum Bermuda { Bermuda, Calcao, Calca }
    public enum Calcado { Tenis, TenisCanoAlto, Chinelo }
    public enum Acessorio { Nada, Oculos, Corrente, Munhequeira }

    /// <summary>
    /// Estilo de jogo (como as posições do FIFA): define as barras de atributo. Todos os estilos
    /// somam o mesmo total de pontos, então nenhum é simplesmente melhor: só joga diferente.
    /// A aparência (idade, corpo, roupa) não muda os atributos, é só visual.
    /// </summary>
    public enum EstiloJogo { Equilibrado, Rebatedor, Corredor, Lancador, Defensor }

    /// <summary>
    /// O personagem que a pessoa monta e leva para todas as partidas (GDD, "Seu personagem").
    /// Aparência: idade, altura, corpo, pele, cabelo, barba, boné, roupa, calçado e acessório.
    /// Jogo: o estilo de jogo dá as barras (velocidade, força, precisão, alcance, fôlego).
    /// Um arquivo por personagem em Assets/Config/Personagens (os bots); o personagem da pessoa
    /// fica salvo em JSON no computador dela (ver PersonagensSalvos).
    /// </summary>
    [CreateAssetMenu(fileName = "Personagem", menuName = "Bets de Rua/Personagem")]
    public class PerfilPersonagem : ScriptableObject
    {
        [Header("Quem é")]
        public string nome = "Jogador";
        public FaixaEtaria faixaEtaria = FaixaEtaria.Adulto;
        public EstiloJogo estilo = EstiloJogo.Equilibrado;

        [Header("Corpo")]
        [Tooltip("Altura (m). Criança ~1,30; adolescente ~1,60; adulto ~1,75.")]
        [Range(1.1f, 2.0f)] public float altura = 1.75f;
        public TipoCorpo tipoCorpo = TipoCorpo.Medio;
        public Color corPele = new Color(0.78f, 0.6f, 0.45f);

        [Header("Cabeça")]
        public EstiloCabelo cabelo = EstiloCabelo.Curto;
        public Color corCabelo = new Color(0.12f, 0.08f, 0.05f);
        public Barba barba = Barba.Nada;
        public Chapeu chapeu = Chapeu.Bone;

        [Header("Roupa (a camisa e o boné seguem a cor do time na partida)")]
        public Camisa camisa = Camisa.Camiseta;
        public Bermuda bermuda = Bermuda.Bermuda;
        public Color corBermuda = new Color(0.16f, 0.18f, 0.24f);
        public Calcado calcado = Calcado.Tenis;
        public Color corCalcado = new Color(0.95f, 0.95f, 0.93f);
        public Acessorio acessorio = Acessorio.Nada;
        public bool tatuagem;

        [Header("Taco")]
        public Color corFitaTaco = new Color(0.1f, 0.1f, 0.1f);

        /// <summary>Altura de referência do jogo (os números de campo e alcance foram feitos para ela).</summary>
        public const float AlturaReferencia = 1.75f;

        // ------------------------------------------------------------------ atributos (0 a 99)

        /// <summary>Barras do estilo: velocidade, força, precisão, alcance, fôlego (somam sempre 375).</summary>
        public static int[] BarrasDoEstilo(EstiloJogo estilo)
        {
            switch (estilo)
            {
                case EstiloJogo.Rebatedor: return new[] { 68, 90, 72, 85, 60 };
                case EstiloJogo.Corredor: return new[] { 92, 62, 70, 66, 85 };
                case EstiloJogo.Lancador: return new[] { 70, 82, 90, 63, 70 };
                case EstiloJogo.Defensor: return new[] { 80, 66, 75, 82, 72 };
                default: return new[] { 75, 75, 75, 75, 75 };
            }
        }

        public static readonly string[] NomesBarras = { "Velocidade", "Força", "Precisão", "Alcance", "Fôlego" };

        public int[] Barras => BarrasDoEstilo(estilo);

        // Barra 75 = normal (fator 1); cada ponto acima ou abaixo muda um pouco
        private static float Fator(int barra, float porPonto) => 1f + (barra - 75) * porPonto;

        /// <summary>Multiplica a velocidade de correr e o sprint.</summary>
        public float FatorVelocidade => Fator(Barras[0], 0.004f);

        /// <summary>Multiplica a força da batida e do arremesso.</summary>
        public float FatorForca => Fator(Barras[1], 0.005f);

        /// <summary>Multiplica a precisão (menos erro no arremesso e no chute). 1 = normal.</summary>
        public float FatorPrecisao => Fator(Barras[2], 0.01f);

        /// <summary>Multiplica o alcance do taco e do mergulho.</summary>
        public float FatorAlcance => Fator(Barras[3], 0.005f);

        /// <summary>Multiplica quanto dura o sprint.</summary>
        public float FatorFolego => Fator(Barras[4], 0.008f);

        /// <summary>Quanto o corpo é mais largo ou mais fino que o normal (só visual).</summary>
        public float FatorLargura => tipoCorpo == TipoCorpo.Magro ? 0.86f : tipoCorpo == TipoCorpo.Forte ? 1.22f : 1f;

        /// <summary>Nota geral (0 a 99), como no FIFA: a média das barras.</summary>
        public int NotaGeral
        {
            get
            {
                int soma = 0;
                foreach (int b in Barras) soma += b;
                return Mathf.RoundToInt(soma / 5f);
            }
        }

        /// <summary>Altura padrão de cada idade (o criador ajusta a partir daqui).</summary>
        public static float AlturaPadrao(FaixaEtaria idade) =>
            idade == FaixaEtaria.Crianca ? 1.32f : idade == FaixaEtaria.Adolescente ? 1.62f : 1.75f;

        /// <summary>Faixa de altura permitida para cada idade.</summary>
        public static Vector2 FaixaAltura(FaixaEtaria idade) =>
            idade == FaixaEtaria.Crianca ? new Vector2(1.2f, 1.45f)
            : idade == FaixaEtaria.Adolescente ? new Vector2(1.45f, 1.8f)
            : new Vector2(1.55f, 2.0f);

        public PerfilPersonagem Clonar()
        {
            PerfilPersonagem copia = CreateInstance<PerfilPersonagem>();
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(this), copia);
            copia.name = name;
            return copia;
        }
    }
}
