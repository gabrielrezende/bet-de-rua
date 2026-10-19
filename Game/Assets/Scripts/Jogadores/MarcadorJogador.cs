using UnityEngine;

namespace BetsDeRua
{
    /// <summary>
    /// Marcador do jogador controlado, como no FIFA: seta girando acima da cabeça com o nome
    /// de quem controla ("P1") em cima. Sem círculo no chão (parecia marca de onde a bola ia).
    /// Rebatedor: a seta fica verde quando ele está seguro e vermelha piscando na zona de perigo.
    /// Os outros: cor do time. Só aparece quando alguém controla o jogador; cresce na troca.
    /// </summary>
    public class MarcadorJogador : MonoBehaviour
    {
        [SerializeField] private ConfigHUD configHUD;
        [SerializeField] private ConfigFisica configFisica;
        [SerializeField] private Jogador jogador;
        [Tooltip("Material do triângulo (cor da dupla).")]
        [SerializeField] private Material materialTriangulo;
        [Tooltip("Material do círculo de giz (não usado: o círculo saiu).")]
        [SerializeField] private Material materialGiz;
        [Tooltip("Rebatedor seguro: cor da seta.")]
        [SerializeField] private Material materialSeguro;
        [Tooltip("Rebatedor na zona de perigo: cor da seta (pisca).")]
        [SerializeField] private Material materialPerigo;

        private Transform triangulo;
        private MeshRenderer desenhoTriangulo;
        private Taco taco;

        /// <summary>Material (cor do time) do triângulo em cima da cabeça.</summary>
        public Material MaterialTriangulo
        {
            get => materialTriangulo;
            set
            {
                materialTriangulo = value;
                if (triangulo != null) triangulo.GetComponent<MeshRenderer>().sharedMaterial = value;
            }
        }
        private Transform circulo;
        private bool estavaVisivel;
        private float tempoPiscando = float.MaxValue;
        private float raioCirculoMontado;
        private float espessuraCirculoMontada;

        private void Awake()
        {
            triangulo = CriarFilho("Triangulo", MontarTriangulo(), materialTriangulo);
            desenhoTriangulo = triangulo.GetComponent<MeshRenderer>();
            circulo = CriarFilho("CirculoGiz", null, materialGiz);
            circulo.gameObject.SetActive(false);
            taco = GetComponentInChildren<Taco>();
            Mostrar(false);
        }

        private void LateUpdate()
        {
            bool visivel = jogador.Controlador != null;
            if (visivel != estavaVisivel)
            {
                Mostrar(visivel);
                if (visivel) tempoPiscando = 0f;
                estavaVisivel = visivel;
            }
            if (!visivel) return;

            // Cor da seta: rebatedor = seguro (verde) ou perigo (vermelho piscando); os outros = cor do time
            Material cor = materialTriangulo;
            bool mostrarSeta = true;
            if (jogador.Papel == Papel.Rebatedor && materialSeguro != null && materialPerigo != null)
            {
                bool seguro = jogador.DentroDoCirculo() && taco != null && taco.NoChao;
                cor = seguro ? materialSeguro : materialPerigo;
                if (!seguro) mostrarSeta = Mathf.Repeat(Time.time * configHUD.piscadasPorSegundoPerigo, 1f) < 0.6f;
            }
            desenhoTriangulo.sharedMaterial = cor;
            triangulo.gameObject.SetActive(mostrarSeta);

            // Piscada: cresce e volta ao normal
            tempoPiscando += Time.deltaTime;
            float escala = 1f;
            if (tempoPiscando < configHUD.duracaoPiscar)
            {
                float t = tempoPiscando / configHUD.duracaoPiscar;
                escala = Mathf.Lerp(configHUD.escalaPiscar, 1f, t);
            }

            float alturaTopo = configFisica.alturaJogador + configHUD.alturaTrianguloAcimaCabeca;
            triangulo.localPosition = Vector3.up * alturaTopo;
            // Gira em torno do eixo vertical do mundo, sem herdar a rotação do corpo
            triangulo.rotation = Quaternion.Euler(0f, Time.time * configHUD.giroTriangulo, 0f);
            triangulo.localScale = Vector3.one * (configHUD.tamanhoTriangulo * escala);

        }

        /// <summary>Nome de quem controla ("P1") em cima da seta.</summary>
        private void OnGUI()
        {
            if (!estavaVisivel || jogador.Controlador == null) return;
            Camera camera = Camera.main;
            if (camera == null) return;
            float alturaNome = configFisica.alturaJogador + configHUD.alturaTrianguloAcimaCabeca + configHUD.tamanhoTriangulo * 0.6f;
            Vector3 tela = camera.WorldToScreenPoint(transform.position + Vector3.up * alturaNome);
            if (tela.z <= 0f) return;
            GUIStyle estilo = new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold, alignment = TextAnchor.LowerCenter };
            estilo.normal.textColor = Color.white;
            string nome = $"P{jogador.Controlador.Numero}";
            Rect area = new Rect(tela.x - 50f, Screen.height - tela.y - 28f, 100f, 26f);
            // Sombra para ler em cima de qualquer fundo
            GUIStyle sombra = new GUIStyle(estilo);
            sombra.normal.textColor = new Color(0f, 0f, 0f, 0.8f);
            GUI.Label(new Rect(area.x + 1.5f, area.y + 1.5f, area.width, area.height), nome, sombra);
            GUI.Label(area, nome, estilo);
        }

        private void Mostrar(bool visivel)
        {
            triangulo.gameObject.SetActive(visivel);
        }

        private Transform CriarFilho(string nome, Mesh malha, Material material)
        {
            GameObject filho = new GameObject(nome);
            filho.transform.SetParent(transform, false);
            filho.AddComponent<MeshFilter>().sharedMesh = malha;
            MeshRenderer desenho = filho.AddComponent<MeshRenderer>();
            desenho.sharedMaterial = material;
            desenho.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return filho.transform;
        }

        /// <summary>Remonta o círculo se raio ou espessura mudaram no ConfigHUD.</summary>
        private void MontarCirculoSePreciso()
        {
            if (Mathf.Approximately(raioCirculoMontado, configHUD.raioCirculoGiz)
                && Mathf.Approximately(espessuraCirculoMontada, configHUD.espessuraCirculoGiz)) return;
            raioCirculoMontado = configHUD.raioCirculoGiz;
            espessuraCirculoMontada = configHUD.espessuraCirculoGiz;
            circulo.GetComponent<MeshFilter>().sharedMesh = MalhasProcedurais.AnelNoChao(raioCirculoMontado, espessuraCirculoMontada);
        }

        /// <summary>
        /// Pirâmide de base triangular de cabeça para baixo (ponta para o jogador),
        /// com 1 unidade de tamanho; a escala vem do ConfigHUD.
        /// </summary>
        private static Mesh MontarTriangulo()
        {
            const float raio = 0.5f;
            const float altura = 0.8f;
            Vector3 ponta = new Vector3(0f, -altura, 0f);
            Vector3[] topo = new Vector3[3];
            for (int i = 0; i < 3; i++)
            {
                float angulo = (90f + i * 120f) * Mathf.Deg2Rad;
                topo[i] = new Vector3(Mathf.Cos(angulo) * raio, 0f, Mathf.Sin(angulo) * raio);
            }

            // Faces: a tampa de cima e os 3 lados (vértices repetidos para cada face)
            Vector3[] vertices =
            {
                topo[0], topo[2], topo[1],
                topo[0], topo[1], ponta,
                topo[1], topo[2], ponta,
                topo[2], topo[0], ponta,
            };
            int[] triangulos = new int[vertices.Length];
            for (int i = 0; i < triangulos.Length; i++) triangulos[i] = i;

            Mesh malha = new Mesh { name = "MarcadorTriangulo", vertices = vertices, triangles = triangulos };
            malha.RecalculateNormals();
            malha.RecalculateBounds();
            return malha;
        }
    }
}
