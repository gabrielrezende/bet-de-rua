using UnityEngine;

namespace BetsDeRua
{
    /// <summary>
    /// Liga as animações de captura de movimento (Animator do corpo de verdade) ao que acontece
    /// no jogo: andar/trotar/correr pela velocidade, postura de guarda do rebatedor na base,
    /// arremesso e chute, batida, pegar a bola, mergulho/carrinho, comemorar e lamentar.
    /// A cabeça olha a bola (IK do Animator). O rebatedor tem o corpo feito por código: na guarda
    /// agacha e inclina o tronco com a ponta do taco no chão; na batida o quadril e o tronco giram
    /// junto com o taco com os pés plantados; os braços seguram o taco com as duas mãos por
    /// cinemática inversa de dois ossos (IKDoisOssos), sem deslocar o ombro.
    /// A bola fica na mão de quem vai arremessar.
    /// </summary>
    [RequireComponent(typeof(Jogador))]
    public class AnimadorCorpo : MonoBehaviour
    {
        [SerializeField] private ConfigFisica configFisica;

        private Jogador jogador;
        private Arremessador arremessador;
        private Rebatedor rebatedor;
        private Taco taco;
        private Defensor defensor;
        private Bola bola;
        private Animator animador;
        private Transform maoDireita;
        private Vector3 posicaoBaseTaco;
        private float velocidadeSuave;
        private bool estavaGirando, estavaMergulhando, estavaBatendo;
        private float gestoAte;
        private bool gestoParado;  // o gesto de agora é parado no lugar (comemorar, lamentar): os pés não abrem

        private static readonly int pVelocidade = Animator.StringToHash("Velocidade");
        private static readonly int pGuarda = Animator.StringToHash("Guarda");
        private static readonly int pMergulhando = Animator.StringToHash("Mergulhando");
        private static readonly int pRapidezBatida = Animator.StringToHash("RapidezBatida");

        /// <summary>Em que ponto da animação o braço solta a bola (0 a 1): o gesto começa daí.</summary>
        private const float SolturaArremesso = 0.4f;

        private void Awake()
        {
            jogador = GetComponent<Jogador>();
            arremessador = GetComponent<Arremessador>();
            rebatedor = GetComponent<Rebatedor>();
            taco = GetComponentInChildren<Taco>(true);
            defensor = GetComponent<Defensor>();
            if (taco != null) posicaoBaseTaco = taco.transform.localPosition;
        }

        public void DefinirConfig(ConfigFisica config) => configFisica = config;

        /// <summary>Acha o Animator do corpo (depois que o corpo é montado ou remontado).</summary>
        public void Religar(Animator novo)
        {
            animador = novo;
            maoDireita = animador != null && animador.isHuman ? animador.GetBoneTransform(HumanBodyBones.RightHand) : null;
            if (taco != null) posicaoBaseTaco = taco.transform.localPosition;
            if (animador != null)
            {
                IKCorpo ik = animador.GetComponent<IKCorpo>();
                if (ik == null) ik = animador.gameObject.AddComponent<IKCorpo>();
                ik.dono = this;
            }
        }

        private void OnEnable()
        {
            if (arremessador != null) arremessador.Arremessou += AoArremessar;
            if (defensor != null) defensor.Pegou += AoPegar;
            ControladorLance.Comemoracao += AoComemorar;
        }

        private void OnDisable()
        {
            if (arremessador != null) arremessador.Arremessou -= AoArremessar;
            if (defensor != null) defensor.Pegou -= AoPegar;
            ControladorLance.Comemoracao -= AoComemorar;
        }

        private void Gesto(string estado, float inicio, float duracao)
        {
            if (animador == null) return;
            animador.CrossFadeInFixedTime(estado, 0.08f, 0, inicio);
            gestoParado = false;
            gestoAte = Time.time + duracao;
        }

        private void AoArremessar(Arremessador quem, Vector3 saida, Vector3 velocidade, TipoArremesso tipo, float kmh)
        {
            if (animador == null) return;
            animador.CrossFade("Arremesso", 0.05f, 0, SolturaArremesso);
            gestoParado = false;
            gestoAte = Time.time + 0.6f;
        }

        private void AoPegar(Defensor quem, bool noAr) => Gesto("Pegar", 0.15f, 0.5f);

        private void AoComemorar(Dupla quemComemora, bool festa)
        {
            if (animador == null || jogador.Mergulhando) return;
            if (jogador.Dupla == quemComemora) { Gesto("Comemorar", 0f, 1.6f); gestoParado = true; }
            else if (festa) { Gesto("Lamentar", 0f, 1.2f); gestoParado = true; }
        }

        private void Update()
        {
            if (animador == null) return;
            if (bola == null) bola = FindFirstObjectByType<Bola>();
            Vector3 v = jogador.Velocidade;
            v.y = 0f;
            velocidadeSuave = Mathf.Lerp(velocidadeSuave, v.magnitude, 1f - Mathf.Exp(-Time.deltaTime * 10f));
            animador.SetFloat(pVelocidade, velocidadeSuave);
            // Na batida o corpo continua na base da guarda: o giro é feito por código (CorpoDoRebatedor)
            bool guarda = rebatedor != null && taco != null && velocidadeSuave < 0.6f && (jogador.DentroDoCirculo() || taco.Girando);
            // A captura de beisebol (estado Guarda) só entra no modo com captura: ela vira o corpo de lado e abre
            // as pernas, e no modo por código a guarda é montada em cima do "parado" natural
            animador.SetBool(pGuarda, guarda && configFisica != null && configFisica.usarCapturaNaBatida);

            bool mergulhando = jogador.Mergulhando;
            animador.SetBool(pMergulhando, mergulhando);
            if (mergulhando && !estavaMergulhando) animador.CrossFade("MergulhoInicio", 0.05f, 0, 0f);
            estavaMergulhando = mergulhando;

            if (taco != null)
            {
                // Batida com a captura de verdade: entra no fim do "carregar", acelerada até o contato
                bool captura = configFisica != null && configFisica.usarCapturaNaBatida;
                if (captura && taco.Girando && !estavaGirando)
                {
                    animador.SetFloat(pRapidezBatida, configFisica.rapidezBatidaCaptura);
                    animador.CrossFadeInFixedTime("Batida", 0.03f, 0, configFisica.inicioBatidaCaptura);
                    batidaRapidaAte = Time.time + configFisica.tempoBatidaRapida;
                    tocandoBatida = true;
                }
                if (Time.time > batidaRapidaAte) animador.SetFloat(pRapidezBatida, 1f);
                if (tocandoBatida && !taco.Girando && !animador.GetCurrentAnimatorStateInfo(0).IsName("Batida") && !animador.IsInTransition(0))
                    tocandoBatida = false;
                estavaGirando = taco.Girando;
            }

            // Enchendo o arco do arremesso: o braço vai para trás (a animação fica na preparação)
            if (arremessador != null && arremessador.ComBola && arremessador.Carregando && velocidadeSuave < 1.2f && Time.time > gestoAte)
                animador.Play("Arremesso", 0, Mathf.Lerp(0.08f, SolturaArremesso - 0.08f, arremessador.Carga));
        }

        private void LateUpdate()
        {
            if (animador == null) return;
            // Taco: o pivô (a pega) anda no arco da batida e sobe na cruzada (como no boneco antigo)
            if (taco != null)
            {
                Vector3 alvo = posicaoBaseTaco;
                if (taco.Girando)
                {
                    float t = Mathf.SmoothStep(0f, 1f, taco.FracaoGiro);
                    float arco = Mathf.Lerp(70f, -95f, t);
                    alvo = Quaternion.Euler(0f, arco, 0f) * new Vector3(0f, posicaoBaseTaco.y + 0.2f, 0.34f);
                    if (rebatedor != null && rebatedor.UltimoTipo == TipoBatida.Varrida) alvo.y -= 0.25f;
                }
                else if (taco.BatendoTacos) alvo = new Vector3(0.12f, posicaoBaseTaco.y + 0.8f, 0.2f);
                taco.transform.localPosition = Vector3.Lerp(taco.transform.localPosition, alvo,
                                                            taco.Girando || taco.BatendoTacos ? 1f : Mathf.Clamp01(Time.deltaTime * 12f));
            }
            VirarCorpoParaOndeVai();
            // Mãos presas no taco do jogo: soltam na batida gravada (as mãos da captura mandam) e voltam na guarda
            float alvoMaos = tocandoBatida ? 0f : 1f;
            pesoMaosNoTaco = Mathf.MoveTowards(pesoMaosNoTaco, alvoMaos,
                Time.deltaTime / Mathf.Max(0.02f, tocandoBatida ? 0.04f : (configFisica != null ? configFisica.tempoVoltarDaBatida : 0.4f)));
            CorpoDoRebatedor();
            PernasNaturais();
            ColarTacoNaMao();
            MaoDireitaNoCabo();

            // A bola na mão de quem vai arremessar (só o desenho)
            if (arremessador != null && arremessador.ComBola && bola != null && maoDireita != null && !jogador.Mergulhando)
                bola.transform.position = maoDireita.position + maoDireita.up * 0.06f + transform.forward * 0.02f;
        }

        /// <summary>
        /// O taco desenhado sai do meio da mão de cima (a esquerda: o destro segura a ponta do cabo
        /// com ela) e aponta para a ponta do taco do jogo (que
        /// cuida das regras: taco no chão, giro). Assim o taco nunca fica longe da mão.
        /// </summary>
        private void ColarTacoNaMao()
        {
            if (taco == null || animador == null || !animador.isHuman || jogador.Mergulhando) return;
            Transform desenho = taco.transform.Find("Taco");
            Transform mao = animador.GetBoneTransform(HumanBodyBones.LeftHand);
            Transform dedo = animador.GetBoneTransform(HumanBodyBones.LeftMiddleProximal);
            if (desenho == null || mao == null) return;
            Vector3 pega = dedo != null ? Vector3.Lerp(mao.position, dedo.position, 0.7f) : mao.position;
            float comprimento = configFisica != null ? configFisica.comprimentoTaco : 1f;
            Vector3 ponta = taco.transform.position - taco.transform.up * comprimento;
            // Na guarda, a ponta do taco desenhado encosta no chão à frente (as mãos ficam na cintura)
            // Depois da varrida, o taco desenhado sai de onde o golpe terminou (baixo, à frente) e volta para a
            // guarda; antes ele subia e ficava apontado para cima por cima do ombro, seguindo o ângulo da regra
            // ("o taco volta pelo ombro"), que continua valendo para saber quando ele está no chão
            if (rebatedor != null && taco.Girando) { pontaFimGiro = ponta; temFimGiro = true; }
            else if (temFimGiro && (!jogador.DentroDoCirculo() || pesoMaosGuarda >= 1f || taco.BatendoTacos)) temFimGiro = false;
            if (temFimGiro && !taco.Girando) ponta = Vector3.Lerp(pontaFimGiro, pontaGuarda, Mathf.SmoothStep(0f, 1f, pesoMaosGuarda));
            else if (pesoMaosGuarda > 0f) ponta = Vector3.Lerp(ponta, pontaGuarda, pesoMaosGuarda);
            // Correndo para a outra base: o taco vai levantado e inclinado para a frente (antes ia arrastando no chão)
            if (rebatedor != null && !taco.Girando && !taco.BatendoTacos && !jogador.DentroDoCirculo())
            {
                float pesoCorrendo = Mathf.InverseLerp(1.5f, 3.5f, velocidadeSuave);
                Vector3 frenteCorpo = -animador.transform.forward; frenteCorpo.y = 0f; frenteCorpo.Normalize();
                ponta = Vector3.Lerp(ponta, pega + (Vector3.up * 0.75f + frenteCorpo * 0.65f).normalized * comprimento, pesoCorrendo);
            }
            Vector3 eixo = ponta - pega;
            if (eixo.sqrMagnitude < 0.01f) return;
            Vector3 dir = eixo.normalized;
            // Na batida gravada o taco segue as mãos da captura: sai da mão de cima passando pela de baixo
            Transform outra = animador.GetBoneTransform(HumanBodyBones.RightHand);
            if (pesoMaosNoTaco < 1f && outra != null)
            {
                Vector3 pelasMaos = outra.position - mao.position;
                if (pelasMaos.sqrMagnitude > 0.0009f) dir = Vector3.Slerp(pelasMaos.normalized, dir, pesoMaosNoTaco).normalized;
            }
            eixo = dir;
            desenho.position = pega - dir * 0.05f; // o pomo do cabo aparece um pouco acima da mão
            Vector3 frente = Vector3.ProjectOnPlane(transform.forward, dir);
            if (frente.sqrMagnitude < 0.0001f) frente = Vector3.ProjectOnPlane(transform.right, dir);
            desenho.rotation = Quaternion.LookRotation(frente.normalized, -dir);
            caboAtual = pega;
            eixoAtual = eixo.normalized;
            temCabo = true;
        }

        private Quaternion giroCorpo = Quaternion.identity;

        /// <summary>
        /// Correndo, o corpo vira para onde está indo (a animação de corrida é só para a frente);
        /// parado ou devagar, olha para onde o jogador olha (o alvo, a bola).
        /// </summary>
        private void VirarCorpoParaOndeVai()
        {
            Transform corpo = animador.transform;
            Vector3 v = jogador.Velocidade;
            v.y = 0f;
            Quaternion meiaVolta = Quaternion.Euler(0f, 180f, 0f); // o modelo olha para -Z
            Quaternion alvo = transform.rotation * meiaVolta;
            bool rebatedorNaBase = rebatedor != null && jogador.DentroDoCirculo();
            if (v.magnitude > 1.6f && !jogador.Mergulhando && !rebatedorNaBase)
                alvo = Quaternion.LookRotation(v.normalized, Vector3.up) * meiaVolta;
            if (giroCorpo == Quaternion.identity) giroCorpo = alvo;
            giroCorpo = Quaternion.Slerp(giroCorpo, alvo, 1f - Mathf.Exp(-Time.deltaTime * 12f));
            corpo.rotation = giroCorpo;
        }

        private Vector3 pontaFimGiro;   // onde a ponta do taco desenhado estava no fim da varrida
        private bool temFimGiro;
        private Vector3 caboAtual, eixoAtual;
        private bool temCabo;

        private float torcao;          // graus que o tronco está virado em relação à frente do jogador
        private float pesoMaosNoTaco = 1f; // 1 = mãos presas no taco do jogo; 0 = mãos da captura
        private bool tocandoBatida;
        private float batidaRapidaAte;
        private float pesoGuarda;      // 0 = em pé normal, 1 = guarda completa
        private Vector3 frenteDoTronco;
        // Guarda do bets por cima da captura de beisebol: corpo centrado, pega na frente do peito
        private float pesoGuardaGravada;
        private float pesoMaosGuarda;  // 1 = mãos na pega da guarda do bets e ponta do taco no chão à frente
        private Vector3 direitaPeito = Vector3.right, frentePeito = Vector3.forward;
        private Vector3 pontaGuarda;

        /// <summary>
        /// O corpo do rebatedor por cima da animação: guarda (agachado, tronco para a frente),
        /// torção do quadril e do tronco acompanhando o arco do taco (pés plantados), pernas de
        /// volta nos pés e a mão esquerda na pega do taco (destro: esquerda em cima, direita embaixo).
        /// </summary>
        private void CorpoDoRebatedor()
        {
            if (rebatedor == null || taco == null || configFisica == null || !animador.isHuman || jogador.Mergulhando) return;
            Transform quadril = animador.GetBoneTransform(HumanBodyBones.Hips);
            if (quadril == null) return;

            // Quanto de guarda e de torção agora (com a captura de verdade, o corpo é o da gravação)
            bool captura = configFisica.usarCapturaNaBatida;
            bool guardaGravada = captura && !taco.Girando && jogador.DentroDoCirculo() && velocidadeSuave < 0.6f;
            pesoGuardaGravada = Mathf.MoveTowards(pesoGuardaGravada, guardaGravada ? 1f : 0f, Time.deltaTime * 4f);
            if (pesoGuardaGravada > 0f)
            {
                // A gravação de beisebol anda para o lado: traz o corpo inteiro (pés juntos) para cima do jogador
                Vector3 desloc = transform.position - quadril.position;
                desloc.y = 0f;
                quadril.position += desloc * configFisica.centralizarCorpoGuarda * pesoGuardaGravada;
            }
            bool naGuarda = jogador.DentroDoCirculo() && velocidadeSuave < 0.6f && !captura;
            pesoGuarda = Mathf.MoveTowards(pesoGuarda, naGuarda || (taco.Girando && !captura) ? 1f : 0f, Time.deltaTime * 4f);
            // Mãos na cintura e ponta do taco no chão à frente, na medida do corpo (como nos vídeos do bets):
            // solta na hora quando o taco gira e volta suave depois da batida
            bool maosNaGuarda = jogador.DentroDoCirculo() && velocidadeSuave < 0.6f && !taco.Girando && !taco.BatendoTacos;
            if (captura) pesoMaosGuarda = pesoGuardaGravada;
            else if (taco.Girando) pesoMaosGuarda = 0f;
            else pesoMaosGuarda = Mathf.MoveTowards(pesoMaosGuarda, maosNaGuarda ? 1f : 0f,
                                                    Time.deltaTime / Mathf.Max(0.05f, configFisica.tempoVoltarDaBatida));
            float alvoTorcao = naGuarda ? configFisica.carregarGuarda : 0f;
            if (taco.Girando && !captura) torcao = Mathf.Lerp(70f, -95f, Mathf.SmoothStep(0f, 1f, taco.FracaoGiro));
            else torcao = Mathf.Lerp(torcao, alvoTorcao, 1f - Mathf.Exp(-Time.deltaTime * 3f / Mathf.Max(0.05f, configFisica.tempoVoltarDaBatida)));

            // Onde os pés estão na animação (ficam plantados)
            Transform[] perna =
            {
                animador.GetBoneTransform(HumanBodyBones.LeftUpperLeg), animador.GetBoneTransform(HumanBodyBones.LeftLowerLeg), animador.GetBoneTransform(HumanBodyBones.LeftFoot),
                animador.GetBoneTransform(HumanBodyBones.RightUpperLeg), animador.GetBoneTransform(HumanBodyBones.RightLowerLeg), animador.GetBoneTransform(HumanBodyBones.RightFoot)
            };
            Vector3 peE = perna[2] != null ? perna[2].position : Vector3.zero, peD = perna[5] != null ? perna[5].position : Vector3.zero;
            Quaternion giroPeE = perna[2] != null ? perna[2].rotation : Quaternion.identity, giroPeD = perna[5] != null ? perna[5].rotation : Quaternion.identity;

            Vector3 frente = transform.forward;
            // Quadril: desce na guarda e gira um pouco; o tronco gira o resto e inclina para a frente
            quadril.position += Vector3.down * (configFisica.agachamentoGuarda * pesoGuarda + configFisica.agachamentoGuardaBets * pesoGuardaGravada);
            // Como o golfista se preparando: o quadril vai um pouco para trás (o tronco inclina para a frente por cima)
            quadril.position -= frente * (configFisica.recuarQuadrilGuarda * pesoGuarda);
            IKDoisOssos.Torcer(quadril, torcao * configFisica.torcaoQuadril, Vector3.up);
            Transform[] coluna = { animador.GetBoneTransform(HumanBodyBones.Spine), animador.GetBoneTransform(HumanBodyBones.Chest), animador.GetBoneTransform(HumanBodyBones.UpperChest) };
            int ossos = 0;
            foreach (Transform o in coluna) if (o != null) ossos++;
            Vector3 direitaTorcida = Quaternion.AngleAxis(torcao * configFisica.torcaoQuadril, Vector3.up) * transform.right;
            foreach (Transform o in coluna)
            {
                if (o == null) continue;
                IKDoisOssos.Torcer(o, torcao * configFisica.torcaoTronco / ossos, Vector3.up);
                IKDoisOssos.Torcer(o, configFisica.inclinacaoGuarda * pesoGuarda / ossos, direitaTorcida);
                // Guarda do bets (como o golfista): o tronco desce para a frente em volta do eixo dos ombros
                if (pesoGuardaGravada > 0f) IKDoisOssos.Torcer(o, configFisica.inclinacaoGuardaBets * pesoGuardaGravada / ossos, direitaPeito);
            }

            // Pés na largura dos ombros (a animação de base abre demais as pernas) e joelhos na linha dos pés
            float pesoPes = Mathf.Max(pesoGuarda, pesoGuardaGravada);
            AproximarPes(quadril.position, ref peE, ref peD, pesoPes);
            // Pernas de volta nos pés (joelhos dobram para a frente, um pouco para fora: nunca para dentro)
            Vector3 ladoPes = peD - peE; ladoPes.y = 0f; ladoPes = ladoPes.sqrMagnitude > 1e-4f ? ladoPes.normalized : transform.right;
            IKDoisOssos.Resolver(perna[0], perna[1], perna[2], peE, peE + frente * 0.6f + Vector3.up * 0.5f - ladoPes * 0.3f, 1f);
            IKDoisOssos.Resolver(perna[3], perna[4], perna[5], peD, peD + frente * 0.6f + Vector3.up * 0.5f + ladoPes * 0.3f, 1f);
            if (perna[2] != null) perna[2].rotation = giroPeE;
            if (perna[5] != null) perna[5].rotation = giroPeD;

            // Mão esquerda na pega do taco (topo do cabo), cotovelo para baixo e para fora
            frenteDoTronco = Quaternion.AngleAxis(torcao * (configFisica.torcaoQuadril + configFisica.torcaoTronco), Vector3.up) * frente;
            Vector3 direitaTronco = Vector3.Cross(Vector3.up, frenteDoTronco);
            Transform braco = animador.GetBoneTransform(HumanBodyBones.LeftUpperArm);
            Transform bracoD = animador.GetBoneTransform(HumanBodyBones.RightUpperArm);
            Vector3 alvoPega = taco.transform.position - taco.transform.up * 0.02f;
            Vector3 dicaCotovelo = braco != null ? braco.position + Vector3.down * 0.4f - direitaTronco * 0.35f - frenteDoTronco * 0.15f : Vector3.zero;
            if (braco != null && bracoD != null)
            {
                // A frente e a direita de verdade do peito (pelos ombros), para as mãos não cruzarem
                Vector3 d = bracoD.position - braco.position;
                d.y = 0f;
                if (d.sqrMagnitude > 1e-4f)
                {
                    direitaPeito = d.normalized;
                    frentePeito = Vector3.Cross(direitaPeito, Vector3.up);
                }
                if (pesoMaosGuarda > 0f)
                {
                    // Bets: as duas mãos no cabo na altura da cintura, a ponta do taco no chão à frente
                    Vector3 meioOmbros = (braco.position + bracoD.position) * 0.5f;
                    // Tudo na medida do corpo (criança tem braço mais curto): 1,45 m = ombro de um adulto
                    float medida = Mathf.Clamp((meioOmbros.y - transform.position.y) / 1.45f, 0.6f, 1.2f);
                    Vector3 pegaGuarda = meioOmbros + (frentePeito * configFisica.pegaGuardaFrente
                                         + Vector3.down * configFisica.pegaGuardaAbaixo + direitaPeito * configFisica.pegaGuardaLado) * medida;
                    pontaGuarda = transform.position
                                  + (frentePeito * configFisica.pontaGuardaFrente + direitaPeito * configFisica.pontaGuardaLado) * medida;
                    // As duas mãos têm que alcançar: desce a pega só até onde os dois braços chegam
                    // (a de baixo fica 'distanciaEntreMaos' mais embaixo no cabo)
                    float bracoE = Comprimento(HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftHand) * 0.97f;
                    float bracoDir = Comprimento(HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand) * 0.97f;
                    float frenteE = Vector3.Dot(pegaGuarda - braco.position, frentePeito), ladoE = Vector3.Dot(pegaGuarda - braco.position, direitaPeito);
                    float frenteD = Vector3.Dot(pegaGuarda - bracoD.position, frentePeito), ladoD = Vector3.Dot(pegaGuarda - bracoD.position, direitaPeito);
                    float descerE = Mathf.Sqrt(Mathf.Max(0.0025f, bracoE * bracoE - frenteE * frenteE - ladoE * ladoE));
                    float descerD = Mathf.Sqrt(Mathf.Max(0.0025f, bracoDir * bracoDir - frenteD * frenteD - ladoD * ladoD)) - configFisica.distanciaEntreMaos;
                    float descer = Mathf.Min(meioOmbros.y - pegaGuarda.y, descerE, descerD);
                    pegaGuarda.y = Mathf.Min(braco.position.y, bracoD.position.y) - descer;
                    alvoPega = Vector3.Lerp(alvoPega, pegaGuarda, pesoMaosGuarda);
                    dicaCotovelo = Vector3.Lerp(dicaCotovelo, braco.position + Vector3.down * 0.45f - direitaPeito * 0.3f, pesoMaosGuarda);
                }
            }
            if (braco != null)
                IKDoisOssos.Resolver(braco, animador.GetBoneTransform(HumanBodyBones.LeftLowerArm), animador.GetBoneTransform(HumanBodyBones.LeftHand),
                                     alvoPega, dicaCotovelo, pesoMaosNoTaco);
        }

        /// <summary>
        /// Traz os pés para a largura natural (ConfigFisica.larguraPes × altura do quadril), mantendo
        /// a posição de frente e de trás de cada um. A animação de "parado" da biblioteca abre as pernas
        /// demais para o corpo do MakeHuman e o boneco ficava de pernas abertas, como um manequim.
        /// </summary>
        private void AproximarPes(Vector3 quadril, ref Vector3 peE, ref Vector3 peD, float peso)
        {
            if (configFisica == null || peso <= 0f) return;
            Vector3 lado = peD - peE; lado.y = 0f;
            if (lado.sqrMagnitude < 1e-4f) return;
            lado.Normalize();
            float alturaQuadril = Mathf.Max(0.4f, quadril.y - Mathf.Min(peE.y, peD.y));
            float meia = configFisica.larguraPes * alturaQuadril * 0.5f;
            Vector3 centro = new Vector3(quadril.x, 0f, quadril.z);
            float lE = Vector3.Dot(new Vector3(peE.x, 0f, peE.z) - centro, lado);
            float lD = Vector3.Dot(new Vector3(peD.x, 0f, peD.z) - centro, lado);
            // Só aproxima (pé mais aberto que a largura natural); nunca abre nem cruza
            if (lE < -meia) peE += lado * ((-meia - lE) * peso);
            if (lD > meia) peD -= lado * ((lD - meia) * peso);
        }

        /// <summary>
        /// Quem não é rebatedor de guarda, parado: pés na largura natural, joelhos para a frente.
        /// </summary>
        private void PernasNaturais()
        {
            if (!animador.isHuman || jogador.Mergulhando || configFisica == null) return;
            bool rebatedorNaGuarda = rebatedor != null && (pesoGuarda > 0.01f || pesoGuardaGravada > 0.01f);
            // Arremesso e pegada dão passada (os pés abrem de propósito); comemorar e lamentar são parados
            bool gesto = (Time.time < gestoAte && !gestoParado) || (arremessador != null && arremessador.Carregando);
            pesoPernasParado = Mathf.MoveTowards(pesoPernasParado, velocidadeSuave < 0.4f && !rebatedorNaGuarda && !gesto ? 1f : 0f, Time.deltaTime * (gesto ? 12f : 3f));
            if (pesoPernasParado <= 0f) return;
            Transform quadril = animador.GetBoneTransform(HumanBodyBones.Hips);
            Transform cE = animador.GetBoneTransform(HumanBodyBones.LeftUpperLeg), jE = animador.GetBoneTransform(HumanBodyBones.LeftLowerLeg), pE = animador.GetBoneTransform(HumanBodyBones.LeftFoot);
            Transform cD = animador.GetBoneTransform(HumanBodyBones.RightUpperLeg), jD = animador.GetBoneTransform(HumanBodyBones.RightLowerLeg), pD = animador.GetBoneTransform(HumanBodyBones.RightFoot);
            if (quadril == null || pE == null || pD == null) return;
            Vector3 peE = pE.position, peD = pD.position;
            Quaternion gE = pE.rotation, gD = pD.rotation;
            AproximarPes(quadril.position, ref peE, ref peD, pesoPernasParado);
            Vector3 frenteCorpo = -animador.transform.forward; frenteCorpo.y = 0f; frenteCorpo.Normalize(); // o modelo olha para -Z
            Vector3 lado = peD - peE; lado.y = 0f; lado = lado.sqrMagnitude > 1e-4f ? lado.normalized : animador.transform.right;
            IKDoisOssos.Resolver(cE, jE, pE, peE, peE + frenteCorpo * 0.6f + Vector3.up * 0.5f - lado * 0.1f, 1f);
            IKDoisOssos.Resolver(cD, jD, pD, peD, peD + frenteCorpo * 0.6f + Vector3.up * 0.5f + lado * 0.1f, 1f);
            pE.rotation = gE;
            pD.rotation = gD;
        }

        private float pesoPernasParado;

        /// <summary>Comprimento do braço (ombro-cotovelo-mão) nesta pessoa.</summary>
        private float Comprimento(HumanBodyBones a, HumanBodyBones b, HumanBodyBones c)
        {
            Transform ta = animador.GetBoneTransform(a), tb = animador.GetBoneTransform(b), tc = animador.GetBoneTransform(c);
            if (ta == null || tb == null || tc == null) return 0.5f;
            return Vector3.Distance(ta.position, tb.position) + Vector3.Distance(tb.position, tc.position);
        }

        /// <summary>A mão direita segura o cabo logo abaixo da esquerda (no taco desenhado); dedos fechados no cabo.</summary>
        private void MaoDireitaNoCabo()
        {
            if (rebatedor == null || !temCabo || !animador.isHuman || jogador.Mergulhando) return;
            Transform braco = animador.GetBoneTransform(HumanBodyBones.RightUpperArm);
            if (braco == null) return;
            Vector3 direitaTronco = Vector3.Cross(Vector3.up, frenteDoTronco);
            IKDoisOssos.Resolver(braco, animador.GetBoneTransform(HumanBodyBones.RightLowerArm), animador.GetBoneTransform(HumanBodyBones.RightHand),
                                 caboAtual + eixoAtual * (configFisica != null ? configFisica.distanciaEntreMaos : 0.11f),
                                 Vector3.Lerp(braco.position + Vector3.down * 0.4f + direitaTronco * 0.35f - frenteDoTronco * 0.15f,
                                              braco.position + Vector3.down * 0.45f + direitaPeito * 0.3f, pesoMaosGuarda), pesoMaosNoTaco);
            // A direção dos dedos fica de través ao taco (a mão fecha em volta do cabo)
            AjeitarMao(HumanBodyBones.LeftHand, HumanBodyBones.LeftMiddleProximal);
            AjeitarMao(HumanBodyBones.RightHand, HumanBodyBones.RightMiddleProximal);
        }

        private void AjeitarMao(HumanBodyBones mao, HumanBodyBones dedo)
        {
            Transform m = animador.GetBoneTransform(mao), d = animador.GetBoneTransform(dedo);
            if (m == null || d == null) return;
            Vector3 atual = d.position - m.position;
            Vector3 desejado = Vector3.ProjectOnPlane(atual, eixoAtual);
            if (desejado.sqrMagnitude > 1e-5f) m.rotation = Quaternion.FromToRotation(atual, desejado) * m.rotation;
        }

        /// <summary>Chamado pelo IKCorpo (no objeto do Animator) a cada quadro de animação.</summary>
        public void AplicarIK(Animator a)
        {
            // Olhar a bola (mais forte parado, mais fraco correndo)
            if (bola != null && !jogador.Mergulhando)
            {
                a.SetLookAtWeight(Mathf.Lerp(0.85f, 0.35f, Mathf.Clamp01(velocidadeSuave / 6f)), 0.15f, 0.9f, 0f, 0.6f);
                // Bola no chão, perto dos pés: olha para ela sem dobrar o pescoço até o peito
                // (o olhar desce no máximo uns 35° abaixo da linha dos olhos)
                Vector3 olhar = bola.transform.position;
                Transform cabeca = a.GetBoneTransform(HumanBodyBones.Head);
                if (cabeca != null)
                {
                    Vector3 rumo = olhar - cabeca.position;
                    float noChao = new Vector2(rumo.x, rumo.z).magnitude;
                    float descidaMaxima = Mathf.Max(0.3f, noChao) * Mathf.Tan(35f * Mathf.Deg2Rad);
                    if (-rumo.y > descidaMaxima) olhar.y = cabeca.position.y - descidaMaxima;
                }
                a.SetLookAtPosition(olhar);
            }
            else a.SetLookAtWeight(0f);

            // As mãos no taco são resolvidas depois da animação (CorpoDoRebatedor), não aqui
            a.SetIKPositionWeight(AvatarIKGoal.RightHand, 0f);
            a.SetIKPositionWeight(AvatarIKGoal.LeftHand, 0f);
            a.SetIKRotationWeight(AvatarIKGoal.RightHand, 0f);
            a.SetIKRotationWeight(AvatarIKGoal.LeftHand, 0f);
        }
    }
}
