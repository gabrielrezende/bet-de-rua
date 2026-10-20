using UnityEngine;

namespace BetsDeRua
{
    /// <summary>
    /// Monta o corpo do jogador a partir da ficha do personagem quando o jogo começa (antes dos
    /// outros scripts, que procuram os pivôs do corpo) e remonta quando o personagem muda
    /// (ex.: o seu personagem passa para o outro lado na troca de papel).
    /// </summary>
    [DefaultExecutionOrder(-200)]
    [RequireComponent(typeof(Jogador))]
    public class AparenciaBoneco : MonoBehaviour
    {
        [SerializeField] private ConfigFisica config;
        [Tooltip("Cor do time no começo (camisa e boné). Depois quem pinta é o ControladorLance.")]
        [SerializeField] private Material materialTime;
        [Tooltip("Corpos de verdade (com animação de captura). Vazio = boneco de peças gerado por código.")]
        [SerializeField] private BibliotecaCorpos biblioteca;

        private Jogador jogador;
        private bool montado;
        private Material timeAtual;

        /// <summary>True quando o personagem usa o corpo de verdade (Animator).</summary>
        public bool CorpoReal => biblioteca != null && biblioteca.Pronta;
        /// <summary>A cor do time que o personagem veste agora.</summary>
        public Material MaterialTime => timeAtual != null ? timeAtual : materialTime;

        /// <summary>Veste a camisa e o boné com a cor do time.</summary>
        public void Pintar(Material time)
        {
            if (time == null) return;
            timeAtual = time;
            if (CorpoReal) ConstrutorCorpoReal.PintarTime(transform, time, biblioteca);
            else
                foreach (MeshRenderer peca in GetComponentsInChildren<MeshRenderer>(true))
                    if (peca.name.StartsWith("Time_")) peca.sharedMaterial = time;
        }

        private void Awake()
        {
            jogador = GetComponent<Jogador>();
            Montar(null);
        }

        /// <summary>Monta (ou remonta) o corpo. 'novoPerfil' troca o personagem; null mantém o atual.</summary>
        public void Montar(PerfilPersonagem novoPerfil)
        {
            if (jogador == null) jogador = GetComponent<Jogador>();
            if (novoPerfil != null) jogador.DefinirPerfil(novoPerfil);
            PerfilPersonagem perfil = jogador.Perfil;
            Material time = MaterialTime;
            timeAtual = time;

            Animator corpoAnimado = null;
            if (CorpoReal) corpoAnimado = ConstrutorCorpoReal.Montar(transform, perfil, time, biblioteca);
            else ConstrutorBoneco.Montar(transform, perfil, time);
            jogador.CorpoReal = CorpoReal;
            AnimadorBoneco animadorPecas = GetComponent<AnimadorBoneco>();
            if (animadorPecas != null) animadorPecas.enabled = !CorpoReal;
            AnimadorCorpo animadorReal = GetComponent<AnimadorCorpo>();
            if (CorpoReal)
            {
                if (animadorReal == null) animadorReal = gameObject.AddComponent<AnimadorCorpo>();
                animadorReal.enabled = true;
                animadorReal.DefinirConfig(config);
                animadorReal.Religar(corpoAnimado);
            }
            else if (animadorReal != null) animadorReal.enabled = false;

            float altura = jogador.Altura;
            CharacterController corpo = GetComponent<CharacterController>();
            if (corpo != null)
            {
                corpo.height = altura;
                corpo.center = new Vector3(0f, altura * 0.5f, 0f);
            }

            Taco taco = GetComponentInChildren<Taco>(true);
            if (taco != null)
            {
                float escala = altura / PerfilPersonagem.AlturaReferencia;
                taco.transform.localPosition = new Vector3(config.raioJogador * 0.9f, config.alturaMaoTaco * escala, config.raioJogador * 0.6f);
                ConstrutorBoneco.MontarTaco(taco.transform, config.comprimentoTaco,
                                            perfil != null ? perfil.corFitaTaco : new Color(0.1f, 0.1f, 0.1f));
            }

            if (montado)
            {
                jogador.ReconhecerCorpo();
                AnimadorBoneco animador = GetComponent<AnimadorBoneco>();
                if (animador != null && !CorpoReal) animador.Religar();
            }
            montado = true;
        }
    }
}
