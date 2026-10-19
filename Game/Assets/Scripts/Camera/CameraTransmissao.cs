using UnityEngine;

namespace BetsDeRua
{
    /// <summary>
    /// Câmera de transmissão ("Transmissão alta", como a câmera alta do FC 26):
    /// na calçada de um lado da rua (Z negativo), alta, olhando a linha central.
    /// Desliza de lado seguindo a bola, nunca gira, e para de deslizar quando
    /// chegaria a mostrar além dos muros do fundo.
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

        private Camera cameraUnity;
        private float focoX;
        private float velocidadeFoco;

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
                focoX = Mathf.SmoothDamp(focoX, AlvoX(), ref velocidadeFoco, configCamera.suavizacao);
            else
                focoX = AlvoX();
            Posicionar();
        }

        private float AlvoX()
        {
            float x = alvo != null ? alvo.position.x : 0f;
            float limite = Mathf.Max(0f, configFisica.comprimentoCampo * 0.5f + configCamera.margemAlemDoMuro
                                         - MeiaLarguraVisivelNaFrente());
            return Mathf.Clamp(x, -limite, limite);
        }

        /// <summary>
        /// Quantos metros para cada lado a câmera mostra na beira do campo mais perto dela
        /// (a parte mais "apertada" da imagem, por causa da perspectiva).
        /// </summary>
        private float MeiaLarguraVisivelNaFrente()
        {
            float inclinacao = Mathf.Atan2(configCamera.altura, configCamera.distancia);
            float distanciaAteCentro = Mathf.Sqrt(configCamera.altura * configCamera.altura
                                                  + configCamera.distancia * configCamera.distancia);
            float profundidadeFrente = distanciaAteCentro - configFisica.larguraCampo * 0.5f * Mathf.Cos(inclinacao);
            float tangenteHorizontal = Mathf.Tan(configCamera.campoVisao * 0.5f * Mathf.Deg2Rad) * cameraUnity.aspect;
            return profundidadeFrente * tangenteHorizontal;
        }

        private void Posicionar()
        {
            cameraUnity.fieldOfView = configCamera.campoVisao;
            cameraUnity.farClipPlane = configCamera.distanciaDesenho;
            Vector3 foco = new Vector3(focoX, 0f, 0f);
            transform.position = new Vector3(focoX, configCamera.altura, -configCamera.distancia);
            transform.rotation = Quaternion.LookRotation(foco - transform.position, Vector3.up);
        }
    }
}
