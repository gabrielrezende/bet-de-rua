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

        public bool Pronta => corpoMasculino != null && controladorJogador != null;
    }
}
