using System;
using UnityEngine;

namespace BetsDeRua
{
    /// <summary>
    /// A corrida da dupla do taco depois da batida (GDD, passos 6 a 8):
    /// - Cruzada: os dois dentro da faixa do meio e a até distanciaCruzada um do outro;
    ///   aparece um anel no chão e X (A) / Espaço bate os tacos = +1 ponto pendente.
    ///   Sem ninguém na dupla, os bots cruzam sozinhos.
    /// - Regra da rua: depois de cruzar, cada um tem que chegar na base do OUTRO lado e
    ///   encostar o taco (zona segura). Os dois chegaram: o ponto vale, e só aí pode cruzar
    ///   de novo. Dá para ir e voltar quantas vezes quiser enquanto a bola não volta.
    /// </summary>
    public class Corrida : MonoBehaviour
    {
        [SerializeField] private ConfigPartida configPartida;
        [SerializeField] private ConfigFisica configFisica;
        [SerializeField] private ConfigHUD configHUD;
        [SerializeField] private Jogador rebatedorA;
        [SerializeField] private Jogador rebatedorB;
        [SerializeField] private Material materialAnel;

        /// <summary>Avisa quando os tacos se cruzam (pontos pendentes agora).</summary>
        public event Action<int> Cruzou;
        /// <summary>Avisa quando pontos pendentes viram pontos de verdade (quantos).</summary>
        public event Action<int> PontosValidados;

        private const float AlturaAnel = 0.02f;
        private MeshFilter anel;
        private Mesh malhaAnel;
        private Taco tacoA, tacoB;
        // Lado (-1 = base A, +1 = base B) da última base em que cada um encostou o taco
        private float ultimaBaseA, ultimaBaseB;
        // Depois de uma cruzada: cada um precisa encostar o taco na base do outro lado
        private bool esperandoChegarNaBase;
        private bool chegouA, chegouB;
        private float baseDeSaidaA, baseDeSaidaB;

        /// <summary>True depois da batida, enquanto a bola está viva.</summary>
        public bool Ativa { get; private set; }
        public int PontosPendentes { get; private set; }
        /// <summary>True se os dois estão no ponto de cruzar agora.</summary>
        public bool PodeCruzar { get; private set; }
        /// <summary>True depois de uma cruzada, até os dois encostarem o taco na base do outro lado.</summary>
        public bool EsperandoChegarNaBase => esperandoChegarNaBase;

        private void Awake()
        {
            tacoA = rebatedorA.GetComponentInChildren<Taco>();
            tacoB = rebatedorB.GetComponentInChildren<Taco>();
            GameObject filho = new GameObject("AnelCruzada");
            filho.transform.SetParent(transform, false);
            MeshRenderer desenho = filho.AddComponent<MeshRenderer>();
            desenho.sharedMaterial = materialAnel;
            desenho.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            anel = filho.AddComponent<MeshFilter>();
            filho.SetActive(false);
        }

        /// <summary>Começa a valer a corrida (a bola foi rebatida).</summary>
        public void Ativar()
        {
            Ativa = true;
            esperandoChegarNaBase = false;
            ultimaBaseA = LadoDaBase(rebatedorA.transform.position);
            ultimaBaseB = LadoDaBase(rebatedorB.transform.position);
        }

        /// <summary>Fim do lance: pontos pendentes que não viraram ponto são perdidos.</summary>
        public void Encerrar()
        {
            Ativa = false;
            PontosPendentes = 0;
            PodeCruzar = false;
            esperandoChegarNaBase = false;
            anel.gameObject.SetActive(false);
        }

        private static float LadoDaBase(Vector3 posicao) => posicao.x < 0f ? -1f : 1f;

        /// <summary>True se o rebatedor está seguro: dentro de um círculo com o taco no chão.</summary>
        public bool Seguro(Jogador rebatedor)
        {
            Taco taco = rebatedor == rebatedorA ? tacoA : tacoB;
            return rebatedor.DentroDoCirculo() && taco != null && taco.NoChao;
        }

        public bool AmbosSeguros => Seguro(rebatedorA) && Seguro(rebatedorB);

        private void Update()
        {
            if (!Ativa) return;

            Vector3 a = rebatedorA.transform.position;
            Vector3 b = rebatedorB.transform.position;
            float meiaFaixa = configFisica.larguraFaixaMeio * 0.5f;
            bool aNaFaixa = Mathf.Abs(a.x) <= meiaFaixa;
            bool bNaFaixa = Mathf.Abs(b.x) <= meiaFaixa;
            float distancia = new Vector2(a.x - b.x, a.z - b.z).magnitude;

            // Lembra a última base em que cada um encostou o taco
            if (Seguro(rebatedorA)) ultimaBaseA = LadoDaBase(a);
            if (Seguro(rebatedorB)) ultimaBaseB = LadoDaBase(b);

            // Depois da cruzada: cada um tem que encostar o taco na base do outro lado
            if (esperandoChegarNaBase)
            {
                if (Seguro(rebatedorA) && LadoDaBase(a) != baseDeSaidaA) chegouA = true;
                if (Seguro(rebatedorB) && LadoDaBase(b) != baseDeSaidaB) chegouB = true;
                if (chegouA && chegouB)
                {
                    // Os dois chegaram: o ponto vale, e já pode voltar para cruzar de novo
                    esperandoChegarNaBase = false;
                    int validados = PontosPendentes;
                    PontosPendentes = 0;
                    PontosValidados?.Invoke(validados);
                }
            }

            PodeCruzar = aNaFaixa && bNaFaixa && distancia <= configPartida.distanciaCruzada && !esperandoChegarNaBase;
            MostrarAnel(a, b);

            if (PodeCruzar && AlguemApertouCruzar()) Cruzar();
        }

        private bool AlguemApertouCruzar()
        {
            // Cruzada automática: os dois se encontraram no meio, já vale (sem botão)
            if (configPartida.cruzadaAutomatica) return true;
            bool temHumano = false;
            foreach (Jogador rebatedor in new[] { rebatedorA, rebatedorB })
            {
                ControleHumano humano = rebatedor.Controlador;
                if (humano == null || !humano.Conectado) continue;
                temHumano = true;
                if (humano.ApertouCruzar) return true;
            }
            // Sem ninguém na dupla do taco: os bots cruzam sozinhos
            return !temHumano;
        }

        private void Cruzar()
        {
            // Os dois batem os tacos em X, no alto, um na direção do outro; "TEC!" no ponto do encontro
            Vector3 a = rebatedorA.transform.position, b = rebatedorB.transform.position;
            tacoA?.BaterNoOutroTaco(b - a);
            tacoB?.BaterNoOutroTaco(a - b);
            pontoDoTec = (a + b) * 0.5f + Vector3.up * (configFisica.alturaJogador + 0.3f);
            tempoDoTec = 0f;

            PontosPendentes++;
            // Agora cada um tem que chegar na base do outro lado (saiu da última em que encostou)
            esperandoChegarNaBase = true;
            chegouA = chegouB = false;
            baseDeSaidaA = ultimaBaseA;
            baseDeSaidaB = ultimaBaseB;
            Cruzou?.Invoke(PontosPendentes);
        }

        private Vector3 pontoDoTec;
        private float tempoDoTec = float.MaxValue;
        private const float DuracaoTec = 0.7f;

        /// <summary>"TEC!" amarelo no ponto onde os tacos se cruzaram, subindo e sumindo.</summary>
        private void OnGUI()
        {
            if (Time.timeScale == 0f || HUDPartida.SobreposicaoAberta) return; // pausa ou fim de partida na frente
            if (tempoDoTec >= DuracaoTec) return;
            tempoDoTec += Time.deltaTime;
            Camera camera = Camera.main;
            if (camera == null) return;
            Vector3 tela = camera.WorldToScreenPoint(pontoDoTec);
            if (tela.z <= 0f) return;
            float t = tempoDoTec / DuracaoTec;
            GUIStyle estilo = new GUIStyle(GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(Mathf.Lerp(42f, 30f, t)), fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter
            };
            estilo.normal.textColor = new Color(1f, 0.85f, 0.1f, 1f - t);
            float y = Screen.height - tela.y - 40f * t;
            GUI.Label(new Rect(tela.x - 100f, y - 30f, 200f, 60f), "TEC!", estilo);
        }

        private void MostrarAnel(Vector3 a, Vector3 b)
        {
            bool perto = Mathf.Abs(a.x) <= configFisica.larguraFaixaMeio && Mathf.Abs(b.x) <= configFisica.larguraFaixaMeio
                         && !esperandoChegarNaBase;
            anel.gameObject.SetActive(perto);
            if (!perto) return;
            // Anel em volta dos dois; fica cheio (mais grosso) quando dá para cruzar
            float raio = configPartida.distanciaCruzada * 0.5f + configFisica.raioJogador;
            float espessura = PodeCruzar ? configHUD.espessuraAnelTempo * 2.5f : configHUD.espessuraAnelTempo;
            malhaAnel = MalhasProcedurais.AnelNoChao(raio, espessura, malhaAnel);
            anel.sharedMesh = malhaAnel;
            Vector3 meio = (a + b) * 0.5f;
            anel.transform.SetPositionAndRotation(new Vector3(meio.x, AlturaAnel, meio.z), Quaternion.identity);
        }
    }
}
