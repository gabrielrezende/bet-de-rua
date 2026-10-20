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

        private static readonly int pVelocidade = Animator.StringToHash("Velocidade");
        private static readonly int pGuarda = Animator.StringToHash("Guarda");
        private static readonly int pMergulhando = Animator.StringToHash("Mergulhando");

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
            gestoAte = Time.time + duracao;
        }

        private void AoArremessar(Arremessador quem, Vector3 saida, Vector3 velocidade, TipoArremesso tipo, float kmh)
        {
            if (animador == null) return;
            animador.CrossFade("Arremesso", 0.05f, 0, SolturaArremesso);
            gestoAte = Time.time + 0.6f;
        }

        private void AoPegar(Defensor quem, bool noAr) => Gesto("Pegar", 0.15f, 0.5f);

        private void AoComemorar(Dupla quemComemora, bool festa)
        {
            if (animador == null || jogador.Mergulhando) return;
            if (jogador.Dupla == quemComemora) Gesto("Comemorar", 0f, 1.6f);
            else if (festa) Gesto("Lamentar", 0f, 1.2f);
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
            animador.SetBool(pGuarda, guarda);

            bool mergulhando = jogador.Mergulhando;
            animador.SetBool(pMergulhando, mergulhando);
            if (mergulhando && !estavaMergulhando) animador.CrossFade("MergulhoInicio", 0.05f, 0, 0f);
            estavaMergulhando = mergulhando;

            if (taco != null) estavaGirando = taco.Girando;

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
            CorpoDoRebatedor();
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
            Vector3 eixo = ponta - pega;
            if (eixo.sqrMagnitude < 0.01f) return;
            Vector3 dir = eixo.normalized;
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

        private Vector3 caboAtual, eixoAtual;
        private bool temCabo;

        private float torcao;          // graus que o tronco está virado em relação à frente do jogador
        private float pesoGuarda;      // 0 = em pé normal, 1 = guarda completa
        private Vector3 frenteDoTronco;

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

            // Quanto de guarda e de torção agora
            bool naGuarda = jogador.DentroDoCirculo() && velocidadeSuave < 0.6f;
            pesoGuarda = Mathf.MoveTowards(pesoGuarda, naGuarda || taco.Girando ? 1f : 0f, Time.deltaTime * 4f);
            float alvoTorcao = naGuarda ? configFisica.carregarGuarda : 0f;
            if (taco.Girando) torcao = Mathf.Lerp(70f, -95f, Mathf.SmoothStep(0f, 1f, taco.FracaoGiro));
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
            quadril.position += Vector3.down * configFisica.agachamentoGuarda * pesoGuarda;
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
            }

            // Pernas de volta nos pés (joelhos dobram para a frente)
            IKDoisOssos.Resolver(perna[0], perna[1], perna[2], peE, peE + frente * 0.6f + Vector3.up * 0.5f, 1f);
            IKDoisOssos.Resolver(perna[3], perna[4], perna[5], peD, peD + frente * 0.6f + Vector3.up * 0.5f, 1f);
            if (perna[2] != null) perna[2].rotation = giroPeE;
            if (perna[5] != null) perna[5].rotation = giroPeD;

            // Mão esquerda na pega do taco (topo do cabo), cotovelo para baixo e para fora
            frenteDoTronco = Quaternion.AngleAxis(torcao * (configFisica.torcaoQuadril + configFisica.torcaoTronco), Vector3.up) * frente;
            Vector3 direitaTronco = Vector3.Cross(Vector3.up, frenteDoTronco);
            Transform braco = animador.GetBoneTransform(HumanBodyBones.LeftUpperArm);
            if (braco != null)
                IKDoisOssos.Resolver(braco, animador.GetBoneTransform(HumanBodyBones.LeftLowerArm), animador.GetBoneTransform(HumanBodyBones.LeftHand),
                                     taco.transform.position - taco.transform.up * 0.02f,
                                     braco.position + Vector3.down * 0.4f - direitaTronco * 0.35f - frenteDoTronco * 0.15f, 1f);
        }

        /// <summary>A mão direita segura o cabo logo abaixo da esquerda (no taco desenhado); dedos fechados no cabo.</summary>
        private void MaoDireitaNoCabo()
        {
            if (rebatedor == null || !temCabo || !animador.isHuman || jogador.Mergulhando) return;
            Transform braco = animador.GetBoneTransform(HumanBodyBones.RightUpperArm);
            if (braco == null) return;
            Vector3 direitaTronco = Vector3.Cross(Vector3.up, frenteDoTronco);
            IKDoisOssos.Resolver(braco, animador.GetBoneTransform(HumanBodyBones.RightLowerArm), animador.GetBoneTransform(HumanBodyBones.RightHand),
                                 caboAtual + eixoAtual * 0.11f,
                                 braco.position + Vector3.down * 0.4f + direitaTronco * 0.35f - frenteDoTronco * 0.15f, 1f);
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
                a.SetLookAtPosition(bola.transform.position);
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
