using UnityEngine;

namespace BetsDeRua
{
    /// <summary>
    /// Câmera de transmissão ("Transmissão alta", como a câmera alta do FC 26):
    /// na calçada de um lado da rua (Z negativo), alta, olhando a linha central.
    /// Desliza de lado seguindo a bola, nunca gira, e para de deslizar quando
    /// chegaria a mostrar além dos muros do fundo. Quando a bola e os jogadores
    /// se espalham mais do que cabe na tela, ela abre um pouco (zoom dinâmico).
    /// </summary>
    [ExecuteAlways] // posiciona também fora do Play mode, para ver o enquadramento no editor
    [RequireComponent(typeof(Camera))]
    public class CameraTransmissao : MonoBehaviour
    {
        [SerializeField] private ConfigCamera configCamera;
        [Tooltip("Medidas do campo, para saber onde ficam os muros.")]
        [SerializeField] private ConfigFisica configFisica;
        [Tooltip("O que a câmera segue (a bola). Sem alvo, fica no meio do campo.")]
        [SerializeField] private Transform alvo;
        [Tooltip("Jogadores: a câmera abre um pouco quando eles se espalham.")]
        [SerializeField] private Transform[] outrosAlvos;

        private Camera cameraUnity;
        private float focoX;
        private float velocidadeFoco;
        private float abertura;          // 0 = normal; aberturaMaxima = toda aberta
        private float velocidadeAbertura;

        private void Awake()
        {
            cameraUnity = GetComponent<Camera>();
            if (configCamera == null || configFisica == null) return;
            focoX = AlvoX();
            Posicionar();
        }

        private void LateUpdate()
        {
            if (configCamera == null || configFisica == null) return;
            // Desliza o ponto de foco em direção à bola, sem trancos (no editor, vai direto)
            if (Application.isPlaying)
            {
                abertura = Mathf.SmoothDamp(abertura, AberturaDesejada(), ref velocidadeAbertura, configCamera.suavizacaoZoom);
                focoX = Mathf.SmoothDamp(focoX, AlvoX(), ref velocidadeFoco, configCamera.suavizacao);
            }
            else
            {
                abertura = 0f;
                focoX = AlvoX();
            }
            Posicionar();
        }

        private float AlvoX()
        {
            float x = alvo != null ? alvo.position.x : 0f;
            float limite = Mathf.Max(0f, configFisica.comprimentoCampo * 0.5f + configCamera.margemAlemDoMuro
                                         - MeiaLarguraVisivelNaFrente(abertura));
            return Mathf.Clamp(x, -limite, limite);
        }

        /// <summary>
        /// Abre quando a distância entre o primeiro e o último (bola e jogadores, no comprimento)
        /// passa de 90% do que cabe na tela; abre tudo quando passa de 120%.
        /// </summary>
        private float AberturaDesejada()
        {
            if (alvo == null || outrosAlvos == null || outrosAlvos.Length == 0) return 0f;
            float minimo = alvo.position.x, maximo = alvo.position.x;
            foreach (Transform t in outrosAlvos)
            {
                if (t == null) continue;
                minimo = Mathf.Min(minimo, t.position.x);
                maximo = Mathf.Max(maximo, t.position.x);
            }
            float larguraVisivelNormal = 2f * MeiaLarguraVisivelNaFrente(0f);
            float espalhamento = (maximo - minimo) / larguraVisivelNormal;
            return Mathf.InverseLerp(0.9f, 1.2f, espalhamento) * configCamera.aberturaMaxima;
        }

        /// <summary>
        /// Quantos metros para cada lado a câmera mostra na beira do campo mais perto dela
        /// (a parte mais "apertada" da imagem, por causa da perspectiva).
        /// </summary>
        private float MeiaLarguraVisivelNaFrente(float comAbertura)
        {
            float inclinacao = Mathf.Atan2(configCamera.altura, configCamera.distancia);
            float distanciaAteCentro = Mathf.Sqrt(configCamera.altura * configCamera.altura
                                                  + configCamera.distancia * configCamera.distancia)
                                       * (1f + comAbertura);
            float profundidadeFrente = distanciaAteCentro - configFisica.larguraCampo * 0.5f * Mathf.Cos(inclinacao);
            float tangenteHorizontal = Mathf.Tan(configCamera.campoVisao * 0.5f * Mathf.Deg2Rad) * cameraUnity.aspect;
            return profundidadeFrente * tangenteHorizontal;
        }

        private void Posicionar()
        {
            cameraUnity.fieldOfView = configCamera.campoVisao;
            cameraUnity.farClipPlane = configCamera.distanciaDesenho;
            Vector3 foco = new Vector3(focoX, 0f, 0f);
            // Abrir = afastar na mesma linha, sem mudar o ângulo
            float escala = 1f + abertura;
            transform.position = new Vector3(focoX, configCamera.altura * escala, -configCamera.distancia * escala);
            transform.rotation = Quaternion.LookRotation(foco - transform.position, Vector3.up);
        }
    }
}
