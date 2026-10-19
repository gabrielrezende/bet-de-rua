using UnityEngine;

namespace BetsDeRua
{
    /// <summary>
    /// Câmera de transmissão lateral e alta (estilo câmera alta do FC 26), na calçada
    /// do lado Z negativo, olhando a linha central. Nunca gira.
    /// Enquadra os 4 jogadores e a bola: desliza de lado e se afasta o quanto
    /// precisar (até ConfigCamera.afastamentoMaximo). Se nem assim couber todo mundo,
    /// os rebatedores têm prioridade (a cruzada no meio nunca sai da tela: a defesa
    /// precisa ver quantos pontos estão saindo) e a bola pode sair; aí aparece uma
    /// seta na borda com a distância até ela.
    /// </summary>
    [ExecuteAlways] // posiciona também fora do Play mode, para ver o enquadramento no editor
    [RequireComponent(typeof(Camera))]
    public class CameraTransmissao : MonoBehaviour
    {
        [SerializeField] private ConfigCamera configCamera;
        [Tooltip("Medidas do campo, para saber onde ficam os muros.")]
        [SerializeField] private ConfigFisica configFisica;
        [Tooltip("A bola: fica na tela sempre que der; fora dela, aparece a seta na borda.")]
        [SerializeField] private Transform alvo;
        [Tooltip("Os jogadores: ficam na tela sempre que der.")]
        [SerializeField] private Transform[] outrosAlvos;
        [Tooltip("Os rebatedores: nunca saem da tela (a cruzada no meio tem que ser vista).")]
        [SerializeField] private Transform[] prioritarios;

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
            // Se não coube todo mundo, os rebatedores mandam: a câmera desliza no máximo até
            // o ponto em que eles ainda ficam na tela (a bola pode sair; aí aparece a seta)
            if (prioritarios != null && prioritarios.Length > 0)
            {
                float minimoP = float.MaxValue, maximoP = float.MinValue;
                foreach (Transform t in prioritarios)
                {
                    if (t == null) continue;
                    minimoP = Mathf.Min(minimoP, t.position.x);
                    maximoP = Mathf.Max(maximoP, t.position.x);
                }
                if (minimoP <= maximoP)
                {
                    float folga = Mathf.Max(0f, meiaLarguraVisivel - margem);
                    float focoMinimo = maximoP - folga, focoMaximo = minimoP + folga;
                    foco = focoMinimo <= focoMaximo ? Mathf.Clamp(foco, focoMinimo, focoMaximo) : (minimoP + maximoP) * 0.5f;
                }
            }

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

        /// <summary>Bola fora da tela: seta na borda, apontando para ela, com a distância (GDD, regras de ouro da câmera).</summary>
        private void OnGUI()
        {
            if (Time.timeScale == 0f || HUDPartida.SobreposicaoAberta) return; // pausa ou fim de partida na frente
            if (!Application.isPlaying || alvo == null || cameraUnity == null || configCamera == null) return;
            Vector3 naTela = cameraUnity.WorldToViewportPoint(alvo.position);
            bool dentro = naTela.z > 0f && naTela.x >= 0f && naTela.x <= 1f && naTela.y >= 0f && naTela.y <= 1f;
            if (dentro) return;

            bool aDireita = naTela.z > 0f ? naTela.x > 1f : naTela.x < 0f;
            float centroTelaX = transform.position.x;
            float distancia = Mathf.Abs(alvo.position.x - centroTelaX) - MeiaLarguraVisivelNaFrente(afastamento);
            GUIStyle estilo = new GUIStyle(GUI.skin.label)
            {
                fontSize = 22, fontStyle = FontStyle.Bold,
                alignment = aDireita ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft
            };
            estilo.normal.textColor = new Color(1f, 0.85f, 0.1f);
            float y = Mathf.Clamp01(1f - naTela.y) * Screen.height;
            y = Mathf.Clamp(y, 60f, Screen.height - 60f);
            string texto = aDireita ? $"bola {Mathf.Max(1f, distancia):0} m ▶" : $"◀ bola {Mathf.Max(1f, distancia):0} m";
            const float margemBorda = 12f, larguraTexto = 220f;
            float x = aDireita ? Screen.width - larguraTexto - margemBorda : margemBorda;
            GUI.Label(new Rect(x, y - 15f, larguraTexto, 30f), texto, estilo);
        }

        private void Posicionar()
        {
            cameraUnity.fieldOfView = configCamera.campoVisao;
            cameraUnity.farClipPlane = configCamera.distanciaDesenho;
            Vector3 foco = new Vector3(focoX, 0f, 0f);
            // Afastar = recuar na mesma linha, sem mudar o ângulo
            transform.position = new Vector3(focoX, configCamera.altura * afastamento,
                                             -configCamera.distancia * afastamento);
            // Levanta um pouco o olhar: a rua desce na tela e aparecem mais casas e céu (menos chão perto da câmera)
            transform.rotation = Quaternion.LookRotation(foco - transform.position, Vector3.up) * Quaternion.Euler(-configCamera.inclinarParaCima, 0f, 0f);
        }
    }
}
