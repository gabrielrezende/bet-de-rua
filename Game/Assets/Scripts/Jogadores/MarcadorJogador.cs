using UnityEngine;

namespace BetsDeRua
{
    /// <summary>
    /// Marcador do jogador controlado (docs/REFERENCIAS_FIFA.md, seção 2):
    /// triângulo na cor da dupla girando acima da cabeça + círculo de giz no chão.
    /// Só aparece quando alguém controla o jogador; pisca quando a troca acontece.
    /// </summary>
    public class MarcadorJogador : MonoBehaviour
    {
        [SerializeField] private ConfigHUD configHUD;
        [SerializeField] private ConfigFisica configFisica;
        [SerializeField] private Jogador jogador;
        [Tooltip("Material do triângulo (cor da dupla).")]
        [SerializeField] private Material materialTriangulo;
        [Tooltip("Material do círculo de giz.")]
        [SerializeField] private Material materialGiz;

        private const int SegmentosCirculo = 48;

        private Transform triangulo;
        private Transform circulo;
        private bool estavaVisivel;
        private float tempoPiscando = float.MaxValue;
        private float raioCirculoMontado;
        private float espessuraCirculoMontada;

        private void Awake()
        {
            triangulo = CriarFilho("Triangulo", MontarTriangulo(), materialTriangulo);
            circulo = CriarFilho("CirculoGiz", null, materialGiz);
            MontarCirculoSePreciso();
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

            MontarCirculoSePreciso();

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

            // O círculo fica no chão e não gira com o corpo
            circulo.position = new Vector3(transform.position.x, 0.01f, transform.position.z);
            circulo.rotation = Quaternion.identity;
            circulo.localScale = Vector3.one * escala;
        }

        private void Mostrar(bool visivel)
        {
            triangulo.gameObject.SetActive(visivel);
            circulo.gameObject.SetActive(visivel);
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
            circulo.GetComponent<MeshFilter>().sharedMesh = MontarAnel(raioCirculoMontado, espessuraCirculoMontada);
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

        /// <summary>Anel achatado no chão (círculo de giz).</summary>
        private static Mesh MontarAnel(float raio, float espessura)
        {
            float raioDentro = Mathf.Max(0f, raio - espessura * 0.5f);
            float raioFora = raio + espessura * 0.5f;
            Vector3[] vertices = new Vector3[SegmentosCirculo * 2];
            int[] triangulos = new int[SegmentosCirculo * 6];
            for (int i = 0; i < SegmentosCirculo; i++)
            {
                float angulo = i * Mathf.PI * 2f / SegmentosCirculo;
                Vector3 direcao = new Vector3(Mathf.Cos(angulo), 0f, Mathf.Sin(angulo));
                vertices[i * 2] = direcao * raioDentro;
                vertices[i * 2 + 1] = direcao * raioFora;

                int proximo = (i + 1) % SegmentosCirculo;
                int t = i * 6;
                triangulos[t] = i * 2;
                triangulos[t + 1] = proximo * 2;
                triangulos[t + 2] = i * 2 + 1;
                triangulos[t + 3] = i * 2 + 1;
                triangulos[t + 4] = proximo * 2;
                triangulos[t + 5] = proximo * 2 + 1;
            }
            Mesh malha = new Mesh { name = "MarcadorCirculoGiz", vertices = vertices, triangles = triangulos };
            malha.RecalculateNormals();
            malha.RecalculateBounds();
            return malha;
        }
    }
}
