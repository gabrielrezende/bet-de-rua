using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace BetsDeRua
{
    /// <summary>O que cada dispositivo apertou neste quadro (lido uma vez por quadro).</summary>
    public class EntradaMenu
    {
        public InputDevice dispositivo;
        public Vector2Int direcao;
        public bool confirmar, voltar, start, abaEsquerda, abaDireita, quadrado, triangulo, qualquer;
        public Vector2 analogicoDireito;
        public float zoom;

        public bool AlgumaCoisa => direcao != Vector2Int.zero || confirmar || voltar || start || abaEsquerda || abaDireita
                                   || quadrado || triangulo || qualquer;
    }

    /// <summary>Uma tela do menu (abertura, início, escolha de lado, criador...).</summary>
    public abstract class Tela
    {
        protected MenuPrincipal menu;
        public VisualElement Raiz { get; protected set; }

        public void Iniciar(MenuPrincipal dono)
        {
            menu = dono;
            Raiz = new VisualElement();
            Raiz.style.position = Position.Absolute;
            Raiz.style.left = Raiz.style.right = Raiz.style.top = Raiz.style.bottom = 0;
            Montar();
        }

        protected abstract void Montar();
        public virtual void AoAbrir() { }
        public virtual void AoFechar() { }
        /// <summary>'todas' = cada dispositivo; 'juntas' = o primeiro que apertou algo (para telas de um dono só).</summary>
        public abstract void Atualizar(List<EntradaMenu> todas, EntradaMenu juntas);
        /// <summary>Onde a câmera do fundo fica nesta tela (posição e para onde olha).</summary>
        public virtual MenuPrincipal.Enquadramento Camera => MenuPrincipal.Enquadramento.Rua;
        /// <summary>A vitrine (o boneco do fundo) aparece nesta tela?</summary>
        public virtual bool MostrarVitrine => true;
    }

    /// <summary>
    /// O menu do jogo, no estilo dos jogos de esporte (FIFA, NBA 2K): abertura "APERTE START",
    /// início com abas (L1/R1) e blocos grandes, escolha de lado com os controles, criador de
    /// personagem com prévia 3D, online (grupo e amigos), ajustes e "como jogar".
    /// O fundo é uma rua 3D com o seu personagem; a câmera muda de lugar em cada tela.
    /// Tudo funciona com controle (um ou vários), teclado ou mouse.
    /// </summary>
    public class MenuPrincipal : MonoBehaviour
    {
        public enum Enquadramento { Rua, Personagem, Abertura }

        [SerializeField] private UIDocument documento;
        [SerializeField] private StyleSheet estilo;
        [SerializeField] private Transform vitrine;
        [SerializeField] private Camera cameraFundo;
        [SerializeField] private Material materialTime;
        [SerializeField] private ConfigFisica configFisica;
        [Tooltip("Corpos de verdade; vazio = boneco de peças.")]
        [SerializeField] private BibliotecaCorpos biblioteca;

        [Header("Câmera do fundo (posição, alvo) para cada tela")]
        [SerializeField] private Vector3 cameraRuaPosicao = new Vector3(-3.2f, 1.45f, -3.6f);
        [SerializeField] private Vector3 cameraRuaAlvo = new Vector3(-0.9f, 1.05f, 0f);
        [SerializeField] private Vector3 cameraPersonagemPosicao = new Vector3(-1.05f, 1.2f, -2.6f);
        [SerializeField] private Vector3 cameraPersonagemAlvo = new Vector3(-0.35f, 0.95f, 0f);
        [SerializeField] private Vector3 cameraAberturaPosicao = new Vector3(4f, 2.2f, -7f);
        [SerializeField] private Vector3 cameraAberturaAlvo = new Vector3(0f, 1.3f, 0f);

        private readonly Stack<Tela> pilha = new Stack<Tela>();
        private VisualElement raiz;
        private VisualElement camadaTelas;
        private VisualElement sombraEsquerda;

        private static Texture2D SombraHorizontal()
        {
            Texture2D t = new Texture2D(256, 4, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int x = 0; x < 256; x++)
            {
                float a = Mathf.Lerp(0.78f, 0f, Mathf.SmoothStep(0f, 1f, x / 255f));
                for (int y = 0; y < 4; y++) t.SetPixel(x, y, new Color(0.03f, 0.04f, 0.07f, a));
            }
            t.Apply();
            return t;
        }
        private readonly List<EntradaMenu> entradas = new List<EntradaMenu>();
        private PoseVitrine pose;
        private float giroVitrine;
        private float zoomVitrine;
        private bool carregando;

        public VisualElement RaizUI => raiz;
        public float GiroVitrine { get => giroVitrine; set => giroVitrine = value; }
        public float ZoomVitrine { get => zoomVitrine; set => zoomVitrine = Mathf.Clamp01(value); }

        private void Start()
        {
            Time.timeScale = 1f;
            AjustesJogo.Aplicar();
            raiz = documento.rootVisualElement;
            raiz.styleSheets.Add(estilo);
            raiz.AddToClassList("raiz");
            // Sombra do lado esquerdo (onde ficam os menus), para o texto ler bem em cima da rua clara
            sombraEsquerda = new VisualElement { pickingMode = PickingMode.Ignore };
            sombraEsquerda.style.position = Position.Absolute;
            sombraEsquerda.style.left = 0;
            sombraEsquerda.style.top = 0;
            sombraEsquerda.style.bottom = 0;
            sombraEsquerda.style.width = Length.Percent(72);
            sombraEsquerda.style.backgroundImage = SombraHorizontal();
            raiz.Add(sombraEsquerda);
            camadaTelas = new VisualElement { pickingMode = PickingMode.Ignore };
            camadaTelas.style.flexGrow = 1;
            raiz.Add(camadaTelas);

            if (vitrine != null) pose = vitrine.GetComponent<PoseVitrine>();
            MostrarPersonagem(PersonagensSalvos.Pegar(0));

            // Voltando de uma partida: vai direto para o início (sem a abertura)
            if (SessaoDeJogo.DispositivoPrincipal != null) Abrir(new TelaInicio());
            else Abrir(new TelaAbertura());
        }

        // ------------------------------------------------------------------ telas

        public void Abrir(Tela tela)
        {
            if (pilha.Count > 0)
            {
                pilha.Peek().AoFechar();
                pilha.Peek().Raiz.RemoveFromHierarchy();
            }
            tela.Iniciar(this);
            pilha.Push(tela);
            camadaTelas.Add(tela.Raiz);
            tela.AoAbrir();
        }

        /// <summary>Troca a tela de cima por outra (sem empilhar: "voltar" não volta para ela).</summary>
        public void Trocar(Tela tela)
        {
            if (pilha.Count > 0)
            {
                Tela velha = pilha.Pop();
                velha.AoFechar();
                velha.Raiz.RemoveFromHierarchy();
            }
            Abrir(tela);
        }

        public void Voltar()
        {
            if (pilha.Count <= 1) return;
            Sons.Tocar(TipoSom.MenuVoltar, 0.6f, 0f);
            Tela velha = pilha.Pop();
            velha.AoFechar();
            velha.Raiz.RemoveFromHierarchy();
            Tela anterior = pilha.Peek();
            camadaTelas.Add(anterior.Raiz);
            anterior.AoAbrir();
        }

        // ------------------------------------------------------------------ vitrine (o boneco do fundo)

        /// <summary>Monta o personagem na vitrine (o fundo do menu).</summary>
        public void MostrarPersonagem(PerfilPersonagem perfil)
        {
            if (vitrine == null) return;
            bool real = biblioteca != null && biblioteca.Pronta;
            if (real)
            {
                Animator a = ConstrutorCorpoReal.Montar(vitrine, perfil, materialTime, biblioteca);
                a.SetBool("Guarda", false);
                VitrineIK ik = a.gameObject.AddComponent<VitrineIK>();
                ik.vitrine = vitrine;
                ik.cameraFundo = cameraFundo;
            }
            else ConstrutorBoneco.Montar(vitrine, perfil, materialTime);
            if (pose != null) pose.enabled = !real;
            Transform pivo = vitrine.Find("MaoTaco");
            if (pivo == null)
            {
                pivo = new GameObject("MaoTaco").transform;
                pivo.SetParent(vitrine, false);
            }
            float escala = perfil.altura / PerfilPersonagem.AlturaReferencia;
            pivo.localPosition = new Vector3(0.05f, configFisica.alturaMaoTaco * escala, 0.3f);
            ConstrutorBoneco.MontarTaco(pivo, configFisica.comprimentoTaco, perfil.corFitaTaco);
            if (pose != null) pose.Religar();
        }

        // ------------------------------------------------------------------ partida

        /// <summary>Fecha o menu e abre a partida com o que foi escolhido.</summary>
        public void ComecarPartida(ModoDeJogo modo, List<PessoaNaPartida> pessoas)
        {
            if (carregando) return;
            carregando = true;
            SessaoDeJogo.Configurar(modo, pessoas);
            StartCoroutine(Carregar());
        }

        private IEnumerator Carregar()
        {
            VisualElement tela = UI.Caixa("sobreposicao");
            Label texto = UI.Texto("CARREGANDO A RUA...", "titulo");
            tela.Add(texto);
            raiz.Add(tela);
            yield return null;
            yield return null;
            AsyncOperation op = SceneManager.LoadSceneAsync(SessaoDeJogo.CenaPartida);
            while (!op.isDone) yield return null;
        }

        public void Sair()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // ------------------------------------------------------------------ quadro a quadro

        private void Update()
        {
            if (carregando || pilha.Count == 0) return;
            LerEntradas(out EntradaMenu juntas);
            Tela atual = pilha.Peek();
            sombraEsquerda.style.display = atual.Camera == Enquadramento.Abertura ? DisplayStyle.None : DisplayStyle.Flex;
            atual.Atualizar(entradas, juntas);

            // Vitrine: gira com o analógico direito, aproxima com os gatilhos
            if (juntas != null)
            {
                giroVitrine -= juntas.analogicoDireito.x * 140f * Time.deltaTime;
                zoomVitrine = Mathf.Clamp01(zoomVitrine + juntas.zoom * Time.deltaTime * 1.2f);
            }
            if (vitrine != null)
            {
                vitrine.gameObject.SetActive(atual.MostrarVitrine);
                vitrine.localRotation = Quaternion.Slerp(vitrine.localRotation, Quaternion.Euler(0f, 200f + giroVitrine, 0f), Time.deltaTime * 8f);
            }
            MoverCamera(atual.Camera);
        }

        private void LerEntradas(out EntradaMenu juntas)
        {
            entradas.Clear();
            juntas = null;
            foreach (InputDevice d in ControlesMenu.Dispositivos())
            {
                EntradaMenu e = new EntradaMenu
                {
                    dispositivo = d,
                    direcao = ControlesMenu.Direcao(d),
                    confirmar = ControlesMenu.Confirmar(d),
                    voltar = ControlesMenu.Voltar(d),
                    start = d is Gamepad g && g.startButton.wasPressedThisFrame,
                    abaEsquerda = ControlesMenu.AbaEsquerda(d),
                    abaDireita = ControlesMenu.AbaDireita(d),
                    quadrado = ControlesMenu.Quadrado(d),
                    triangulo = ControlesMenu.Triangulo(d),
                    qualquer = ControlesMenu.QualquerBotao(d),
                    analogicoDireito = ControlesMenu.AnalogicoDireito(d),
                    zoom = ControlesMenu.Zoom(d),
                };
                entradas.Add(e);
                if (juntas == null && e.AlgumaCoisa) juntas = e;
            }
            // Analógico e zoom contam mesmo sem "apertar" nada
            if (juntas == null)
            {
                foreach (EntradaMenu e in entradas)
                    if (e.analogicoDireito.sqrMagnitude > 0.04f || Mathf.Abs(e.zoom) > 0.1f) { juntas = e; break; }
            }
            if (juntas == null) juntas = new EntradaMenu();
        }

        private void MoverCamera(Enquadramento qual)
        {
            if (cameraFundo == null) return;
            Vector3 posicao, alvo;
            switch (qual)
            {
                case Enquadramento.Personagem:
                {
                    // De frente para o personagem (na direção em que ele fica parado), corpo inteiro;
                    // o alvo fica à esquerda dele, para ele aparecer na parte livre da tela (à direita)
                    Vector3 basePos = vitrine != null ? vitrine.position : Vector3.zero;
                    Vector3 frente = Quaternion.Euler(0f, 200f, 0f) * Vector3.forward;
                    float distancia = Mathf.Lerp(3.6f, 1.5f, zoomVitrine);
                    posicao = basePos + frente * distancia + Vector3.up * Mathf.Lerp(1.05f, 1.5f, zoomVitrine);
                    Vector3 alvoBase = basePos + Vector3.up * Mathf.Lerp(0.92f, 1.5f, zoomVitrine);
                    Vector3 direita = Vector3.Cross(Vector3.up, (alvoBase - posicao).normalized);
                    alvo = alvoBase - direita * Mathf.Lerp(0.95f, 0.42f, zoomVitrine);
                    break;
                }
                case Enquadramento.Abertura:
                    float t = Time.time * 0.05f;
                    posicao = cameraAberturaPosicao + new Vector3(Mathf.Sin(t) * 1.5f, 0f, 0f);
                    alvo = cameraAberturaAlvo;
                    break;
                default:
                    posicao = cameraRuaPosicao;
                    alvo = cameraRuaAlvo;
                    break;
            }
            float k = 1f - Mathf.Exp(-Time.deltaTime * 4f);
            cameraFundo.transform.position = Vector3.Lerp(cameraFundo.transform.position, posicao, k);
            Quaternion rotacao = Quaternion.LookRotation(alvo - cameraFundo.transform.position, Vector3.up);
            cameraFundo.transform.rotation = Quaternion.Slerp(cameraFundo.transform.rotation, rotacao, k);
        }

        // ------------------------------------------------------------------ peças comuns das telas

        /// <summary>Barra de cima com o logo, o título da tela e o cartão do P1.</summary>
        public VisualElement BarraTopo(string titulo, VisualElement meio = null)
        {
            VisualElement barra = UI.Caixa("barra-topo");
            barra.Add(UI.Texto("BETS DE RUA", "titulo-logo"));
            if (meio != null) barra.Add(meio);
            else
            {
                Label t = UI.Texto(titulo, "titulo");
                t.style.flexGrow = 1;
                t.style.marginLeft = 40;
                t.style.fontSize = 40;
                barra.Add(t);
            }
            barra.Add(CartaoJogador(PersonagensSalvos.Pegar(0)));
            return barra;
        }

        public static VisualElement CartaoJogador(PerfilPersonagem p)
        {
            VisualElement cartao = UI.Caixa("cartao");
            cartao.Add(UI.Texto(p.NotaGeral.ToString(), "cartao-nota"));
            VisualElement nomes = new VisualElement();
            nomes.Add(UI.Texto(p.nome.ToUpperInvariant(), "cartao-nome"));
            nomes.Add(UI.Texto(NomeEstilo(p.estilo), "texto-pequeno"));
            cartao.Add(nomes);
            return cartao;
        }

        public static string NomeEstilo(EstiloJogo e) =>
            e == EstiloJogo.Rebatedor ? "Rebatedor" : e == EstiloJogo.Corredor ? "Corredor" : e == EstiloJogo.Lancador ? "Lançador"
            : e == EstiloJogo.Defensor ? "Defensor" : "Equilibrado";

        public VisualElement Rodape(params VisualElement[] dicas)
        {
            VisualElement r = UI.Caixa("barra-rodape");
            foreach (VisualElement d in dicas) r.Add(d);
            return r;
        }
    }
}
