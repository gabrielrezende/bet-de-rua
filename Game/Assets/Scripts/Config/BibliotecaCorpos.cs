using UnityEngine;

namespace BetsDeRua
{
    /// <summary>
    /// Tudo o que é preciso para montar os personagens de verdade (corpo com esqueleto humano,
    /// animações de captura de movimento): os corpos masculino e feminino, os cabelos, a barba,
    /// as sobrancelhas, as roupas geradas a partir do corpo (camiseta, regata, manga longa,
    /// bermuda, calção, calça, tênis, cano alto), as texturas e os controladores de animação.
    /// Criada pelo menu "Bets > Preparar corpos de verdade" (Editor/PreparadorCorpos).
    /// Arte: Quaternius (Universal Base Characters + Universal Animation Library), licença CC0.
    /// </summary>
    [CreateAssetMenu(fileName = "BibliotecaCorpos", menuName = "Bets de Rua/Biblioteca de corpos")]
    public class BibliotecaCorpos : ScriptableObject
    {
        [Header("Corpos (modelo com Animator e Avatar humanoide)")]
        public GameObject corpoMasculino;
        public GameObject corpoFeminino;

        [Header("Cabelos (mesmo esqueleto do corpo)")]
        public GameObject cabeloRaspado;
        public GameObject cabeloRaspadoFeminino;
        public GameObject cabeloCurto;
        public GameObject cabeloLongo;
        public GameObject cabeloCoque;
        public GameObject barba;
        public GameObject sobrancelhasMasculinas;
        public GameObject sobrancelhasFemininas;

        [Header("Roupas geradas (masculino, feminino)")]
        public Mesh[] camiseta = new Mesh[2];
        public Mesh[] regata = new Mesh[2];
        public Mesh[] mangaLonga = new Mesh[2];
        public Mesh[] bermuda = new Mesh[2];
        public Mesh[] calcao = new Mesh[2];
        public Mesh[] calca = new Mesh[2];
        public Mesh[] tenis = new Mesh[2];
        public Mesh[] tenisCanoAlto = new Mesh[2];
        public Mesh[] meia = new Mesh[2];

        [Header("Texturas")]
        public Texture2D peleClaraMasculina, peleEscuraMasculina, normalMasculina;
        public Texture2D peleClaraFeminina, peleEscuraFeminina, normalFeminina;
        public Texture2D olhos;
        public Texture2D cabelo1, cabelo1Normal, cabelo2, cabelo2Normal;
        public Texture2D tecido;

        [Header("Animação")]
        public RuntimeAnimatorController controladorJogador;
        public RuntimeAnimatorController controladorTorcida;

        [Tooltip("Altura do corpo-base (m): o personagem é escalado a partir dela.")]
        public float alturaBase = 1.82f;

        /// <summary>Uma pessoa realista gerada no MakeHuman (já vestida: pele, olhos, cabelo, camiseta, bermuda, tênis).</summary>
        [System.Serializable]
        public class Pessoa
        {
            public GameObject modelo;
            public Genero genero;
            public FaixaEtaria faixa;
            [Tooltip("Tom de pele de 0 (clara) a 1 (escura).")]
            [Range(0f, 1f)] public float tomPele;
            [Tooltip("Cabelo comprido (preso ou solto)?")]
            public bool cabeloComprido;
            [Tooltip("Altura do modelo como veio do MakeHuman (m), medida na importação.")]
            public float alturaModelo = 1.75f;
        }

        [Header("Pessoas realistas (MakeHuman, CC0) — Editor/ImportadorPessoas")]
        [Tooltip("Usa as pessoas do MakeHuman no lugar do corpo de peças (se houver uma que combine).")]
        public bool usarPessoas = true;
        [Tooltip("Giro do modelo para olhar para a frente do jogador (graus).")]
        public float giroPessoas = 180f; // o modelo do MakeHuman olha para -Z, como o corpo de peças
        public Pessoa[] pessoas = new Pessoa[0];

        [Header("Visual estilizado (Shaders/PersonagemEstilizado)")]
        [Tooltip("Pele, camisa, bermuda e tênis das pessoas com o shader estilizado (luz em faixas, contorno). Desligado: material realista do MakeHuman.")]
        public bool usarEstilizado = true;
        [Tooltip("O shader estilizado (referência aqui para entrar no .exe).")]
        public Shader shaderEstilizado;

        /// <summary>
        /// A pessoa que mais combina com a ficha: mesmo gênero e faixa de idade, tom de pele mais
        /// perto e o tipo de cabelo; entre as parecidas, escolhe pelo nome (sempre a mesma pessoa).
        /// </summary>
        public Pessoa EscolherPessoa(PerfilPersonagem perfil)
        {
            if (!usarPessoas || pessoas == null || pessoas.Length == 0) return null;
            Genero genero = perfil != null ? perfil.genero : Genero.Masculino;
            FaixaEtaria faixa = perfil != null ? perfil.faixaEtaria : FaixaEtaria.Adulto;
            float tom = perfil != null ? 1f - Mathf.Clamp01(perfil.corPele.grayscale * 1.6f - 0.15f) : 0.5f;
            bool comprido = perfil != null && (perfil.cabelo == EstiloCabelo.Longo || perfil.cabelo == EstiloCabelo.Trancas);
            int sorte = perfil != null && !string.IsNullOrEmpty(perfil.nome) ? Mathf.Abs(perfil.nome.GetHashCode()) : 0;
            Pessoa melhor = null;
            float menor = float.MaxValue;
            for (int i = 0; i < pessoas.Length; i++)
            {
                Pessoa p = pessoas[i];
                if (p == null || p.modelo == null) continue;
                float custo = Mathf.Abs(p.tomPele - tom) * 2f + (p.cabeloComprido != comprido ? 0.6f : 0f)
                              + (p.genero != genero ? 10f : 0f) + (p.faixa != faixa ? 5f : 0f)
                              + ((i + sorte) % 7) * 0.01f;
                if (custo < menor) { menor = custo; melhor = p; }
            }
            return melhor;
        }

        public bool Pronta => (corpoMasculino != null || (pessoas != null && pessoas.Length > 0)) && controladorJogador != null;
    }
}
