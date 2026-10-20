using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

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

        // Câmeras de perto (arremesso, rebatedor, close) misturadas com a transmissão
        private ControladorLance lance;
        private Vector3 posicaoTransmissao;
        private Quaternion rotacaoTransmissao;
        private float pesoPerto;           // 0 = transmissão, 1 = câmera de perto
        private Vector3 posicaoPerto;
        private Quaternion rotacaoPerto;
        private float campoPerto;
        private Jogador emClose;
        private float closeAte;
        private bool inscrito;
        private int modoAtual;              // 0 = transmissão, 1 = arremesso/rebatedor, 2 = close
        private Vector3 posicaoPertoSuave;
        // Câmera lenta nos lances grandes
        private Vector3 pontoDestaque;
        private bool destaqueQueimada;
        private float destaqueAte;
        private bool lento;
        private Quaternion rotacaoPertoSuave;
        // Fundo desfocado nos closes e na câmera lenta (lente de cinema, como no teaser do FC)
        private Volume volumeFoco;
        private DepthOfField desfoqueFundo;
        private Vector3 pontoFoco;

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
            if (Application.isPlaying) MisturarCameras();
        }

        private void OnDisable()
        {
            if (inscrito)
            {
                ControladorLance.Comemoracao -= AoComemorar;
                ControladorLance.Destaque -= AoDestaque;
            }
            inscrito = false;
            if (lento) { Time.timeScale = 1f; lento = false; }
        }

        private void AoDestaque(Vector3 ponto, bool queimada)
        {
            if (!configCamera.usarClose) return;
            pontoDestaque = ponto;
            destaqueQueimada = queimada;
            // Mesma duração na batida perfeita: 0,7 s era um "pisca" de câmera lenta (corte, corte)
            float duracao = configCamera.duracaoCameraLenta;
            destaqueAte = Time.unscaledTime + duracao;
            // A comemoração vem depois do lance em câmera lenta
            closeAte = Mathf.Max(closeAte, Time.time) + duracao;
        }

        /// <summary>Close em quem comemora (o humano dessa dupla, ou quem está mais perto da bola).</summary>
        private void AoComemorar(Dupla dupla, bool festa)
        {
            if (!configCamera.usarClose || outrosAlvos == null) return;
            Jogador melhor = null;
            float menor = float.MaxValue;
            foreach (Transform t in outrosAlvos)
            {
                Jogador j = t != null ? t.GetComponent<Jogador>() : null;
                if (j == null || j.Dupla != dupla) continue;
                float d = (j.Controlador != null ? -100f : 0f) + (alvo != null ? Vector3.Distance(j.transform.position, alvo.position) : 0f);
                if (d < menor) { menor = d; melhor = j; }
            }
            if (melhor == null) return;
            emClose = melhor;
            closeAte = Time.time + configCamera.duracaoClose;
        }

        /// <summary>
        /// Decide qual câmera vale agora e mistura com a transmissão:
        /// close (comemoração) > arremesso (antes e logo depois do arremesso) > transmissão.
        /// </summary>
        private void MisturarCameras()
        {
            if (lance == null) lance = FindFirstObjectByType<ControladorLance>();
            if (!inscrito) { ControladorLance.Comemoracao += AoComemorar; ControladorLance.Destaque += AoDestaque; inscrito = true; }

            // Câmera lenta (sem brigar com a pausa)
            bool destaque = Time.unscaledTime < destaqueAte;
            if (destaque && !HUDPartida.SobreposicaoAberta) { Time.timeScale = configCamera.escalaCameraLenta; lento = true; }
            else if (lento) { if (!HUDPartida.SobreposicaoAberta) Time.timeScale = 1f; lento = false; }
            posicaoTransmissao = transform.position;
            rotacaoTransmissao = transform.rotation;

            bool perto = false;
            int modo = 0;
            if (destaque)
            {
                // Baixinho, de lado: a garrafa caindo (queimada) ou o rebatedor no momento da pancada
                Vector3 alvoCam = pontoDestaque + Vector3.up * (destaqueQueimada ? 0.25f : 1.0f);
                Vector3 lado = destaqueQueimada ? new Vector3(0.6f, 0f, -1f).normalized : new Vector3(-0.8f, 0f, -1f).normalized;
                float dist = destaqueQueimada ? 1.9f : 3.2f;
                posicaoPerto = alvoCam + lado * dist + Vector3.up * (destaqueQueimada ? 0.25f : 0.3f);
                rotacaoPerto = Quaternion.LookRotation(alvoCam - posicaoPerto, Vector3.up);
                campoPerto = configCamera.campoVisaoClose;
                pontoFoco = alvoCam;
                perto = true;
                modo = 3;
            }
            // Close só com a jogada parada: com a bola viva (a cruzada vale ponto e o lance segue) a
            // transmissão não pode largar a bola para mostrar quem comemora
            else if (emClose != null && Time.time < closeAte && (lance == null || lance.Estado != EstadoLance.BolaViva))
            {
                // De frente para o jogador, na altura do peito, um pouco de lado
                Transform j = emClose.transform;
                Vector3 frente = j.forward;
                Vector3 olho = j.position + Vector3.up * (emClose.Altura * 0.82f);
                posicaoPerto = olho + (frente * configCamera.distanciaClose + j.right * 0.8f) + Vector3.up * 0.15f;
                rotacaoPerto = Quaternion.LookRotation(olho - posicaoPerto, Vector3.up);
                campoPerto = configCamera.campoVisaoClose;
                pontoFoco = olho;
                perto = true;
                modo = 2;
            }
            else if (configCamera.usarCameraDoArremesso && lance != null && lance.ArremessadorDaVez != null && lance.RebatedorDaVez != null
                     && (lance.Estado == EstadoLance.Arremesso
                         || (lance.Estado == EstadoLance.BolaViva && !lance.Rebatida && lance.Portador == null && ArremessoAindaChegando())))
            {
                Vector3 arremessa = lance.ArremessadorDaVez.transform.position;
                Vector3 garrafa = lance.GarrafaAlvo.transform.position;
                Vector3 rumo = garrafa - arremessa;
                rumo.y = 0f;
                rumo.Normalize();
                Vector3 lado = Vector3.Cross(Vector3.up, rumo);
                bool pessoaNoTaco = lance.RebatedorDaVez.Jogador.Controlador != null && lance.ArremessadorDaVez.Jogador.Controlador == null;
                if (pessoaNoTaco)
                {
                    // Atrás da casinha do rebatedor, olhando quem arremessa (lê a bola vindo)
                    Vector3 rebatedor = lance.RebatedorDaVez.transform.position;
                    posicaoPerto = garrafa + rumo * configCamera.distanciaRebatedor + Vector3.up * configCamera.alturaRebatedor
                                   + (rebatedor - garrafa).normalized * -0.6f;
                    Vector3 mira = Vector3.Lerp(garrafa, arremessa, 0.55f) + Vector3.up * 0.9f;
                    rotacaoPerto = Quaternion.LookRotation(mira - posicaoPerto, Vector3.up);
                }
                else
                {
                    // Atrás de quem arremessa, por cima do ombro, olhando a garrafa do outro lado
                    posicaoPerto = arremessa - rumo * configCamera.distanciaArremesso + Vector3.up * configCamera.alturaArremesso + lado * configCamera.ladoArremesso;
                    Vector3 mira = Vector3.Lerp(arremessa, garrafa, 0.7f) + Vector3.up * 0.4f;
                    rotacaoPerto = Quaternion.LookRotation(mira - posicaoPerto, Vector3.up);
                }
                campoPerto = configCamera.campoVisaoPerto;
                perto = true;
                modo = 1;
            }

            // Troca de câmera = corte seco (como na TV); dentro da mesma câmera, movimento suave
            if (modo != modoAtual)
            {
                modoAtual = modo;
                pesoPerto = perto ? 1f : 0f;
                posicaoPertoSuave = posicaoPerto;
                rotacaoPertoSuave = rotacaoPerto;
            }
            if (!perto) { AjustarDesfoque(false); return; }
            float k = 1f - Mathf.Exp(-Time.deltaTime * configCamera.rapidezTroca);
            posicaoPertoSuave = Vector3.Lerp(posicaoPertoSuave, posicaoPerto, k);
            rotacaoPertoSuave = Quaternion.Slerp(rotacaoPertoSuave, rotacaoPerto, k);
            transform.position = posicaoPertoSuave;
            transform.rotation = rotacaoPertoSuave;
            cameraUnity.fieldOfView = campoPerto;
            // Foco medido da câmera já no lugar (senão foca no fundo e desfoca quem importa)
            AjustarDesfoque(modo >= 2);
        }

        private Bola bolaDoLance;

        /// <summary>
        /// A câmera do arremesso fica até a bola chegar na casinha (ou ser rebatida): abrir antes, por tempo
        /// fixo, fazia três cortes em um segundo quando a bola derrubava a garrafa (arremesso, transmissão,
        /// câmera lenta). Passou da garrafa ou parou, abre para a transmissão.
        /// </summary>
        private bool ArremessoAindaChegando()
        {
            if (lance.TempoNoEstado < configCamera.tempoAteAbrir) return true;
            if (lance.TempoNoEstado > configCamera.tempoMaximoCameraArremesso) return false;
            if (bolaDoLance == null) bolaDoLance = FindFirstObjectByType<Bola>();
            if (bolaDoLance == null || bolaDoLance.Parada || lance.GarrafaAlvo == null || lance.ArremessadorDaVez == null) return false;
            Vector3 rumo = lance.GarrafaAlvo.transform.position - lance.ArremessadorDaVez.transform.position;
            rumo.y = 0f;
            return Vector3.Dot(bolaDoLance.transform.position - lance.GarrafaAlvo.transform.position, rumo.normalized) < configCamera.passarDaGarrafaParaAbrir;
        }

        /// <summary>Liga o fundo desfocado (foco em quem/no que importa) só nas câmeras de perto.</summary>
        private void AjustarDesfoque(bool ligar)
        {
            if (!configCamera.desfocarFundoNoClose) ligar = false;
            if (volumeFoco == null)
            {
                if (!ligar) return;
                GameObject g = new GameObject("FocoDeCinema");
                volumeFoco = g.AddComponent<Volume>();
                volumeFoco.isGlobal = true;
                volumeFoco.priority = 10f;
                VolumeProfile perfil = ScriptableObject.CreateInstance<VolumeProfile>();
                desfoqueFundo = perfil.Add<DepthOfField>(true);
                desfoqueFundo.mode.Override(DepthOfFieldMode.Bokeh);
                volumeFoco.profile = perfil;
            }
            volumeFoco.weight = ligar ? 1f : 0f;
            if (!ligar) return;
            desfoqueFundo.focalLength.Override(configCamera.distanciaFocalCinema);
            desfoqueFundo.aperture.Override(configCamera.aberturaCinema);
            desfoqueFundo.focusDistance.Override(Mathf.Max(0.3f, Vector3.Distance(transform.position, pontoFoco)));
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
            if (!Application.isPlaying || alvo == null || cameraUnity == null || configCamera == null || pesoPerto > 0.3f) return;
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
