using UnityEngine;

namespace BetsDeRua
{
    /// <summary>
    /// Liga as animações de captura de movimento (Animator do corpo de verdade) ao que acontece
    /// no jogo: andar/trotar/correr pela velocidade, postura de guarda do rebatedor na base,
    /// arremesso e chute, batida, pegar a bola, mergulho/carrinho, comemorar e lamentar.
    /// Cinemática inversa (IK do Animator): as duas mãos no cabo do taco e a cabeça olhando a bola.
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
        private const float ContatoBatida = 0.12f;

        private void Awake()
        {
            jogador = GetComponent<Jogador>();
            arremessador = GetComponent<Arremessador>();
            rebatedor = GetComponent<Rebatedor>();
            taco = GetComponentInChildren<Taco>(true);
            defensor = GetComponent<Defensor>();
            if (taco != null) posicaoBaseTaco = taco.transform.localPosition;
        }

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
            bool guarda = rebatedor != null && taco != null && velocidadeSuave < 0.6f && jogador.DentroDoCirculo() && !taco.Girando;
            animador.SetBool(pGuarda, guarda);

            bool mergulhando = jogador.Mergulhando;
            animador.SetBool(pMergulhando, mergulhando);
            if (mergulhando && !estavaMergulhando) animador.CrossFade("MergulhoInicio", 0.05f, 0, 0f);
            estavaMergulhando = mergulhando;

            if (taco != null)
            {
                if (taco.Girando && !estavaGirando) animador.CrossFade("Batida", 0.04f, 0, ContatoBatida);
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
            // A bola na mão de quem vai arremessar (só o desenho)
            if (arremessador != null && arremessador.ComBola && bola != null && maoDireita != null && !jogador.Mergulhando)
                bola.transform.position = maoDireita.position + maoDireita.up * 0.06f + transform.forward * 0.02f;
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

            // Mãos no taco
            bool maosNoTaco = rebatedor != null && taco != null && !jogador.Mergulhando;
            float peso = maosNoTaco ? 1f : 0f;
            a.SetIKPositionWeight(AvatarIKGoal.RightHand, peso);
            a.SetIKPositionWeight(AvatarIKGoal.LeftHand, peso);
            a.SetIKRotationWeight(AvatarIKGoal.RightHand, peso * 0.6f);
            a.SetIKRotationWeight(AvatarIKGoal.LeftHand, peso * 0.6f);
            if (!maosNoTaco) return;
            Vector3 topo = taco.transform.position;
            Vector3 eixo = -taco.transform.up;
            Quaternion pega = Quaternion.LookRotation(transform.forward, -eixo);
            a.SetIKPosition(AvatarIKGoal.RightHand, topo + eixo * 0.04f);
            a.SetIKPosition(AvatarIKGoal.LeftHand, topo + eixo * 0.14f);
            a.SetIKRotation(AvatarIKGoal.RightHand, pega);
            a.SetIKRotation(AvatarIKGoal.LeftHand, pega);
            a.SetIKHintPositionWeight(AvatarIKHint.RightElbow, 0.5f);
            a.SetIKHintPositionWeight(AvatarIKHint.LeftElbow, 0.5f);
            a.SetIKHintPosition(AvatarIKHint.RightElbow, transform.position + transform.right * 0.5f + Vector3.up * 0.9f);
            a.SetIKHintPosition(AvatarIKHint.LeftElbow, transform.position - transform.right * 0.5f + Vector3.up * 0.9f);
        }
    }

    /// <summary>Fica no objeto do Animator (o Unity só chama OnAnimatorIK ali) e repassa para o AnimadorCorpo.</summary>
    public class IKCorpo : MonoBehaviour
    {
        public AnimadorCorpo dono;
        private Animator animador;

        private void Awake() => animador = GetComponent<Animator>();

        private void OnAnimatorIK(int camada)
        {
            if (dono != null && animador != null) dono.AplicarIK(animador);
        }
    }
}
