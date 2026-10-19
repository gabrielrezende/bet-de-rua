using UnityEngine;

namespace BetsDeRua
{
    public enum FaixaEtaria { Crianca, Adolescente, Adulto }
    public enum TipoCorpo { Magro, Medio, Forte }

    /// <summary>
    /// O personagem que a pessoa monta e leva para todas as partidas (GDD, "Seu personagem").
    /// No protótipo: idade, altura, tipo de corpo e cor da pele mudam o boneco e os atributos,
    /// sempre trocando uma vantagem por uma desvantagem (o forte bate mais forte e corre menos;
    /// o magro corre mais e tem menos força; o alto alcança mais com o taco). Rosto, cabelo,
    /// tatuagem e roupas entram quando houver modelo de verdade; os campos já estão aqui.
    /// Um arquivo por personagem em Assets/Config/Personagens.
    /// </summary>
    [CreateAssetMenu(fileName = "Personagem", menuName = "Bets de Rua/Personagem")]
    public class PerfilPersonagem : ScriptableObject
    {
        [Header("Quem é")]
        public string nome = "Jogador";
        public FaixaEtaria faixaEtaria = FaixaEtaria.Adulto;

        [Header("Corpo")]
        [Tooltip("Altura (m). Criança ~1,30; adolescente ~1,60; adulto ~1,75.")]
        [Range(1.1f, 2.0f)] public float altura = 1.75f;
        public TipoCorpo tipoCorpo = TipoCorpo.Medio;
        public Color corPele = new Color(0.78f, 0.6f, 0.45f);

        [Header("Visual (para quando houver modelo de verdade)")]
        public int rosto;
        public int cabelo;
        public Color corCabelo = new Color(0.15f, 0.1f, 0.07f);
        public int tatuagem;

        /// <summary>Altura de referência do jogo (os números de campo e alcance foram feitos para ela).</summary>
        public const float AlturaReferencia = 1.75f;

        /// <summary>Multiplica a velocidade de correr e o sprint.</summary>
        public float FatorVelocidade => tipoCorpo == TipoCorpo.Magro ? 1.05f : tipoCorpo == TipoCorpo.Forte ? 0.93f : 1f;

        /// <summary>Multiplica a força da batida e do arremesso.</summary>
        public float FatorForca => (tipoCorpo == TipoCorpo.Magro ? 0.93f : tipoCorpo == TipoCorpo.Forte ? 1.08f : 1f)
                                   * (faixaEtaria == FaixaEtaria.Crianca ? 0.9f : faixaEtaria == FaixaEtaria.Adolescente ? 0.96f : 1f);

        /// <summary>Multiplica o alcance do taco (quem é mais alto alcança mais longe).</summary>
        public float FatorAlcance => Mathf.Lerp(0.85f, 1.12f, Mathf.InverseLerp(1.2f, 2.0f, altura));

        /// <summary>Quanto o corpo é mais largo ou mais fino que o normal (só visual).</summary>
        public float FatorLargura => tipoCorpo == TipoCorpo.Magro ? 0.85f : tipoCorpo == TipoCorpo.Forte ? 1.25f : 1f;

        /// <summary>Nota geral (0 a 99), como no FIFA: resume velocidade, força e alcance.</summary>
        public int NotaGeral => Mathf.RoundToInt(Mathf.Clamp(70f + 60f * ((FatorVelocidade - 1f) + (FatorForca - 1f) + (FatorAlcance - 1f)), 40f, 99f));
    }
}
