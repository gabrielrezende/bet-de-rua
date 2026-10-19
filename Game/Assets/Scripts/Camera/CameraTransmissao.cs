using UnityEngine;

namespace BetsDeRua
{
    /// <summary>
    /// Câmera de transmissão lateral e alta (estilo câmera alta do FC 26), na calçada
    /// do lado Z negativo, olhando a linha central. Nunca gira.
    /// Enquadra sempre os 4 jogadores e a bola: desliza de lado e se afasta o quanto
    /// precisar (até ConfigCamera.afastamentoMaximo). Se nem assim couber todo mundo,
    /// a bola tem prioridade e a câmera vai atrás dela.
    /// </summary>
    [ExecuteAlways] // posiciona também fora do Play mode, para ver o enquadramento no editor
    [RequireComponent(typeof(Camera))]
    public class CameraTransmissao : MonoBehaviour
    {
        [SerializeField] private ConfigCamera configCamera;
        [Tooltip("Medidas do campo, para saber onde ficam os muros.")]
        [SerializeField] private ConfigFisica configFisica;
        [Tooltip("A bola: sempre fica na tela.")]
        [SerializeField] private Transform alvo;
        [Tooltip("Os jogadores: ficam na tela sempre que der.")]
        [SerializeField] private Transform[] outrosAlvos;

        private Camera cameraUnity;
        private float focoX;
        private float velocidadeFoco;
        private float afastamento = 1f;   // 1 = o mais perto possível
        private float velocidadeAfastamento;

        private void Awake()
        {
            cameraUnity = GetComponent<Camera>();
            if (configCamera == null || configFisica == null) return;
            CalcularEnquadramento(out focoX, out afastamento);
            Posicionar();
        }

        private void LateUpdate()
        {
            if (configCamera == null || configFisica == null) return;
            CalcularEnquadramento(out float focoDesejado, out float afastamentoDesejado);
            if (Application.isPlaying)
            {
                afastamento = Mathf.SmoothDamp(afastamento, afastamentoDesejado, ref velocidadeAfastamento,
                                               configCamera.suavizacaoZoom);
                focoX = Mathf.SmoothDamp(focoX, focoDesejado, ref velocidadeFoco, configCamera.suavizacao);
            }
            else
            {
                afastamento = afastamentoDesejado;
                focoX = focoDesejado;
            }
            Posicionar();
        }

        /// <summary>Decide onde focar e quanto se afastar para caber a bola e os jogadores.</summary>
        private void CalcularEnquadramento(out float foco, out float afastamentoNecessario)
        {
            float bolaX = alvo != null ? alvo.position.x : 0f;
            float minimo = bolaX, maximo = bolaX;
            if (outrosAlvos != null)
            {
                foreach (Transform t in outrosAlvos)
                {
                    if (t == null) continue;
                    minimo = Mathf.Min(minimo, t.position.x);
                    maximo = Mathf.Max(maximo, t.position.x);
                }
            }
            float margem = configCamera.margemEnquadramento;
            float meiaLarguraNecessaria = (maximo - minimo) * 0.5f + margem;

            // Quanto afastar para essa meia largura caber na beira do campo mais perto da câmera
            afastamentoNecessario = Mathf.Clamp(AfastamentoParaMeiaLargura(meiaLarguraNecessaria),
                                                1f, Mathf.Max(1f, configCamera.afastamentoMaximo));
            float meiaLarguraVisivel = MeiaLarguraVisivelNaFrente(afastamentoNecessario);

            foco = (minimo + maximo) * 0.5f;
            // Se não coube todo mundo, a bola manda: garante que ela fica na tela
            float folgaBola = Mathf.Max(0f, meiaLarguraVisivel - margem);
            foco = Mathf.Clamp(foco, bolaX - folgaBola, bolaX + folgaBola);

            // Não mostra além dos muros do fundo
            float limite = Mathf.Max(0f, configFisica.comprimentoCampo * 0.5f + configCamera.margemAlemDoMuro
                                         - meiaLarguraVisivel);
            foco = Mathf.Clamp(foco, -limite, limite);
        }

        private float InclinacaoRad => Mathf.Atan2(configCamera.altura, configCamera.distancia);
        private float DistanciaAteCentro => Mathf.Sqrt(configCamera.altura * configCamera.altura
                                                       + configCamera.distancia * configCamera.distancia);
        private float TangenteHorizontal => Mathf.Tan(configCamera.campoVisao * 0.5f * Mathf.Deg2Rad) * cameraUnity.aspect;

        /// <summary>
        /// Quantos metros para cada lado a câmera mostra na beira do campo mais perto dela
        /// (a parte mais "apertada" da imagem, por causa da perspectiva).
        /// </summary>
        private float MeiaLarguraVisivelNaFrente(float fatorAfastamento)
        {
            float profundidadeFrente = DistanciaAteCentro * fatorAfastamento
                                     - configFisica.larguraCampo * 0.5f * Mathf.Cos(InclinacaoRad);
            return profundidadeFrente * TangenteHorizontal;
        }

        /// <summary>O inverso da conta acima: quanto afastar para mostrar 'meiaLargura'.</summary>
        private float AfastamentoParaMeiaLargura(float meiaLargura)
        {
            float profundidade = meiaLargura / TangenteHorizontal
                               + configFisica.larguraCampo * 0.5f * Mathf.Cos(InclinacaoRad);
            return profundidade / DistanciaAteCentro;
        }

        private void Posicionar()
        {
            cameraUnity.fieldOfView = configCamera.campoVisao;
            cameraUnity.farClipPlane = configCamera.distanciaDesenho;
            Vector3 foco = new Vector3(focoX, 0f, 0f);
            // Afastar = recuar na mesma linha, sem mudar o ângulo
            transform.position = new Vector3(focoX, configCamera.altura * afastamento,
                                             -configCamera.distancia * afastamento);
            transform.rotation = Quaternion.LookRotation(foco - transform.position, Vector3.up);
        }
    }
}
