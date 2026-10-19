using UnityEngine;

namespace BetsDeRua
{
    /// <summary>
    /// Anima o boneco (montado pelo ConstrutorBoneco) por código, lendo o que o jogo já sabe:
    /// velocidade (andar, correr, sprint), arremesso (braço vai para trás enchendo o arco e
    /// chicoteia ao soltar), batida (tronco gira com o taco; agacha na varrida), pegar a bola
    /// (abaixa), mergulho (pose esticada) e cruzada (tacos em X).
    /// As DUAS MÃOS SEGURAM O TACO de verdade: depois da pose, cada braço é dobrado
    /// (cinemática inversa de dois ossos: ombro-cotovelo-mão) até a mão chegar no cabo.
    /// A cabeça acompanha a bola e a bola fica na mão de quem vai arremessar.
    /// As medidas ficam no ConfigHUD (seção do boneco).
    /// </summary>
    [RequireComponent(typeof(Jogador))]
    public class AnimadorBoneco : MonoBehaviour
    {
        [SerializeField] private ConfigFisica configFisica;
        [SerializeField] private ConfigHUD configHUD;

        private Jogador jogador;
        private Arremessador arremessador;
        private Rebatedor rebatedor;
        private Taco taco;
        private Defensor defensor;
        private Bola bola;

        private Transform quadril, cintura, cabeca, ombroE, ombroD, cotoveloE, cotoveloD, maoE, maoD,
                          coxaE, coxaD, joelhoE, joelhoD, tornozeloE, tornozeloD;
        private float alturaQuadril;
        private Vector3 posicaoBaseTaco;
        private float fase;
        private float tempoLancou = float.MaxValue;
        private float tempoPegou = float.MaxValue;
        private Vector3 olharSuave;
        private float faseRespiracao;

        private void Awake()
        {
            jogador = GetComponent<Jogador>();
            arremessador = GetComponent<Arremessador>();
            rebatedor = GetComponent<Rebatedor>();
            taco = GetComponentInChildren<Taco>(true);
            defensor = GetComponent<Defensor>();
            faseRespiracao = Random.Range(0f, 6.3f);
            if (taco != null) posicaoBaseTaco = taco.transform.localPosition;
            Religar();
        }

        /// <summary>Acha os pivôs de novo (depois que o boneco é remontado com outro personagem).</summary>
        public void Religar()
        {
            quadril = Achar("Quadril");
            cintura = Achar("Cintura");
            cabeca = Achar("Cabeca");
            ombroE = Achar("OmbroE");
            ombroD = Achar("OmbroD");
            cotoveloE = Achar("CotoveloE");
            cotoveloD = Achar("CotoveloD");
            maoE = Achar("MaoE");
            maoD = Achar("MaoD");
            coxaE = Achar("CoxaE");
            coxaD = Achar("CoxaD");
            joelhoE = Achar("JoelhoE");
            joelhoD = Achar("JoelhoD");
            tornozeloE = Achar("TornozeloE");
            tornozeloD = Achar("TornozeloD");
            if (quadril != null) alturaQuadril = quadril.localPosition.y;
            if (taco != null) posicaoBaseTaco = taco.transform.localPosition;
        }

        private void OnEnable()
        {
            if (arremessador != null) arremessador.Arremessou += AoArremessar;
            if (defensor != null) defensor.Pegou += AoPegar;
        }

        private void OnDisable()
        {
            if (arremessador != null) arremessador.Arremessou -= AoArremessar;
            if (defensor != null) defensor.Pegou -= AoPegar;
        }

        private void AoArremessar(Arremessador quem, Vector3 saida, Vector3 velocidade, TipoArremesso tipo, float kmh) => tempoLancou = 0f;
        private void AoPegar(Defensor quem, bool noAr) => tempoPegou = 0f;

        private Transform Achar(string nome)
        {
            Transform corpo = transform.Find("Corpo");
            if (corpo == null) return null;
            foreach (Transform t in corpo.GetComponentsInChildren<Transform>(true))
                if (t.name == nome) return t;
            return null;
        }

        private void LateUpdate()
        {
            if (quadril == null) return;
            if (bola == null) bola = FindFirstObjectByType<Bola>();
            float dt = Time.deltaTime;
            tempoLancou += dt;
            tempoPegou += dt;

            // ---- Passada: frequência e amplitude crescem com a velocidade (andar -> correr -> sprint)
            Vector3 v = jogador.Velocidade;
            v.y = 0f;
            float rapidez = v.magnitude;
            float fator = Mathf.Clamp01(rapidez / Mathf.Max(0.1f, configFisica.velocidadeSprint));
            fase += dt * Mathf.PI * 2f * (configHUD.passadasPorSegundoBase + rapidez * configHUD.passadasPorMetro);
            float s = Mathf.Sin(fase);
            float amplitudePerna = configHUD.amplitudePernaMaxima * Mathf.Clamp01(fator * 1.4f);
            float amplitudeBraco = amplitudePerna * 0.8f;
            // Parado: respiração leve (o boneco nunca fica uma estátua)
            float respiracao = Mathf.Sin(Time.time * 1.7f + faseRespiracao) * (1f - Mathf.Clamp01(fator * 3f));

            float pernaE = s * amplitudePerna, pernaD = -s * amplitudePerna;
            float joelhoEang = Mathf.Max(0f, -s) * amplitudePerna * 1.3f + 6f * fator + 3f;
            float joelhoDang = Mathf.Max(0f, s) * amplitudePerna * 1.3f + 6f * fator + 3f;
            float peE = -Mathf.Max(0f, -s) * amplitudePerna * 0.4f, peD = -Mathf.Max(0f, s) * amplitudePerna * 0.4f;
            float bracoE = -s * amplitudeBraco + 4f, bracoD = s * amplitudeBraco + 4f;
            float cotoveloEang = 12f + 70f * fator, cotoveloDang = 12f + 70f * fator;
            float aberturaBraco = 6f + 2f * respiracao;
            float inclinacao = configHUD.inclinacaoCorrendo * fator + 1.5f * respiracao;
            float giroTronco = -s * 8f * fator;
            float agachar = 0f;
            float balanco = Mathf.Abs(Mathf.Cos(fase)) * 0.04f * fator + 0.004f * respiracao;
            bool maosNoTaco = false;
            Vector3 posicaoTaco = posicaoBaseTaco;

            // ---- Rebatedor: as mãos vão no taco; gira o tronco na batida; agacha na varrida
            if (rebatedor != null && taco != null)
            {
                maosNoTaco = true;
                bool parado = rapidez < 0.5f;
                if (parado)
                {
                    agachar = 0.07f;
                    inclinacao = Mathf.Max(inclinacao, 14f);
                }
                if (taco.Girando)
                {
                    // O taco descreve um arco na frente do corpo (de trás para a frente) e o tronco gira junto
                    float t = taco.FracaoGiro;
                    giroTronco = Mathf.Lerp(-55f, 65f, Mathf.SmoothStep(0f, 1f, t));
                    float arco = Mathf.Lerp(70f, -95f, Mathf.SmoothStep(0f, 1f, t));
                    posicaoTaco = Quaternion.Euler(0f, arco, 0f) * new Vector3(0f, posicaoBaseTaco.y + 0.15f, 0.32f);
                    inclinacao = 18f;
                    if (rebatedor.UltimoTipo == TipoBatida.Varrida) { agachar = 0.24f; posicaoTaco.y -= 0.2f; }
                }
                else if (taco.BatendoTacos)
                {
                    posicaoTaco = new Vector3(0.12f, posicaoBaseTaco.y + 0.75f, 0.2f);
                }
            }

            // ---- Com a bola: braço do arremesso
            bool comBola = arremessador != null && arremessador.ComBola;
            if (comBola)
            {
                bracoD = 30f;
                cotoveloDang = 85f;
                if (arremessador.Carregando)
                {
                    // Enche o arco: o braço vai para trás e o tronco gira e inclina para trás
                    float c = arremessador.Carga;
                    bracoD = Mathf.Lerp(20f, -120f, c);
                    cotoveloDang = Mathf.Lerp(70f, 95f, c);
                    giroTronco = Mathf.Lerp(0f, -40f, c);
                    bracoE = Mathf.Lerp(10f, 70f, c); // braço de mira para a frente
                    inclinacao = Mathf.Lerp(inclinacao, -6f, c);
                    pernaE = Mathf.Lerp(pernaE, 22f, c); // perna da frente adianta
                    pernaD = Mathf.Lerp(pernaD, -12f, c);
                }
            }
            if (tempoLancou < configHUD.duracaoGestoLancar)
            {
                // Soltou: o braço chicoteia para a frente e para baixo, o tronco acompanha
                float t = tempoLancou / configHUD.duracaoGestoLancar;
                bracoD = Mathf.Lerp(-110f, 75f, Mathf.SmoothStep(0f, 1f, t));
                cotoveloDang = Mathf.Lerp(90f, 10f, t);
                giroTronco = Mathf.Lerp(-35f, 30f, t);
                inclinacao = Mathf.Max(inclinacao, 22f * Mathf.Sin(t * Mathf.PI));
                bracoE = Mathf.Lerp(60f, -20f, t);
            }

            // ---- Pegou a bola: abaixa e se curva
            if (tempoPegou < configHUD.duracaoGestoPegar)
            {
                float t = Mathf.Sin(tempoPegou / configHUD.duracaoGestoPegar * Mathf.PI);
                inclinacao = Mathf.Max(inclinacao, 55f * t);
                agachar = Mathf.Max(agachar, 0.2f * t);
                bracoD = Mathf.Max(bracoD, 80f * t);
                bracoE = Mathf.Max(bracoE, 60f * t);
            }

            // ---- Mergulho: pose esticada (o Jogador deita o corpo inteiro)
            if (jogador.Mergulhando)
            {
                bracoE = bracoD = 170f;
                cotoveloEang = cotoveloDang = 5f;
                pernaE = pernaD = -10f;
                joelhoEang = joelhoDang = 8f;
                inclinacao = 0f;
                agachar = 0f;
                balanco = 0f;
                giroTronco = 0f;
            }

            // ---- Aplica (para a frente = positivo)
            quadril.localPosition = new Vector3(0f, alturaQuadril - agachar + balanco, 0f);
            quadril.localRotation = Quaternion.Euler(0f, -giroTronco * 0.3f, 0f);
            cintura.localRotation = Quaternion.Euler(inclinacao, giroTronco * 0.7f, 0f);
            Membro(ombroE, bracoE, -aberturaBraco);
            Membro(ombroD, bracoD, aberturaBraco);
            DobrarParaFrente(cotoveloE, cotoveloEang);
            DobrarParaFrente(cotoveloD, cotoveloDang);
            // Agachado: coxa vai para a frente, joelho dobra e o pé compensa (fica no chão)
            float joelhoAgachado = agachar * 260f;
            ParaFrente(coxaE, pernaE + joelhoAgachado * 0.5f);
            ParaFrente(coxaD, pernaD + joelhoAgachado * 0.5f);
            DobrarParaTras(joelhoE, joelhoEang + joelhoAgachado);
            DobrarParaTras(joelhoD, joelhoDang + joelhoAgachado);
            // Pé: desfaz a inclinação da coxa e da canela (fica paralelo ao chão) e dobra um pouco ao sair do chão
            ParaFrente(tornozeloE, -pernaE + joelhoEang + joelhoAgachado * 0.5f + peE);
            ParaFrente(tornozeloD, -pernaD + joelhoDang + joelhoAgachado * 0.5f + peD);

            // ---- Cabeça: compensa a inclinação e olha para a bola (dentro de um limite)
            if (cabeca != null)
            {
                cabeca.localRotation = Quaternion.Euler(-inclinacao * 0.6f, 0f, 0f);
                if (bola != null && !jogador.Mergulhando)
                {
                    Vector3 paraBola = bola.transform.position - cabeca.position;
                    olharSuave = Vector3.Slerp(olharSuave == Vector3.zero ? paraBola : olharSuave, paraBola, dt * 6f);
                    Vector3 local = cabeca.parent.InverseTransformDirection(olharSuave);
                    float yaw = Mathf.Clamp(Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg, -70f, 70f);
                    float pitch = Mathf.Clamp(-Mathf.Atan2(local.y, new Vector2(local.x, local.z).magnitude) * Mathf.Rad2Deg, -30f, 35f);
                    cabeca.localRotation = Quaternion.Euler(pitch, yaw, 0f);
                }
            }

            // ---- Taco: o pivô (pega) anda no arco da batida; as duas mãos seguram o cabo
            if (taco != null)
            {
                taco.transform.localPosition = Vector3.Lerp(taco.transform.localPosition, posicaoTaco,
                                                            taco.Girando || taco.BatendoTacos ? 1f : Mathf.Clamp01(dt * 12f));
                if (maosNoTaco && !jogador.Mergulhando)
                {
                    Vector3 topo = taco.transform.position;
                    Vector3 eixo = -taco.transform.up; // o taco pende para baixo do pivô
                    Vector3 lado = transform.right;
                    Dobrar(ombroD, cotoveloD, maoD, topo + eixo * 0.03f, Vector3.down + lado * 0.8f - transform.forward * 0.2f);
                    Dobrar(ombroE, cotoveloE, maoE, topo + eixo * 0.12f, Vector3.down - lado * 0.8f - transform.forward * 0.2f);
                }
            }

            // ---- Bola na mão de quem vai arremessar (só o desenho: a física solta do ponto de sempre)
            if (comBola && bola != null && maoD != null && !jogador.Mergulhando)
                bola.transform.position = maoD.position + maoD.up * -0.06f;
        }

        /// <summary>
        /// Cinemática inversa de dois ossos: dobra ombro e cotovelo para a mão chegar em 'alvo'.
        /// 'dica' diz para onde o cotovelo aponta (para baixo e para fora, como gente segurando um taco).
        /// </summary>
        private void Dobrar(Transform ombro, Transform cotovelo, Transform mao, Vector3 alvo, Vector3 dica)
        {
            if (ombro == null || cotovelo == null || mao == null) return;
            float l1 = cotovelo.localPosition.magnitude, l2 = mao.localPosition.magnitude;
            Vector3 s = ombro.position;
            Vector3 paraAlvo = alvo - s;
            float d = Mathf.Clamp(paraAlvo.magnitude, Mathf.Abs(l1 - l2) + 0.01f, l1 + l2 - 0.001f);
            Vector3 dir = paraAlvo.normalized;
            float a = (l1 * l1 - l2 * l2 + d * d) / (2f * d);
            float h = Mathf.Sqrt(Mathf.Max(0f, l1 * l1 - a * a));
            Vector3 polo = Vector3.ProjectOnPlane(dica, dir).normalized;
            Vector3 e = s + dir * a + polo * h;
            Vector3 m = s + dir * d;
            Apontar(ombro, e - s);
            Apontar(cotovelo, m - e);
        }

        // Faz o -Y do pivô (o membro pendurado) apontar para 'direcao', com a frente do corpo como referência
        private void Apontar(Transform pivo, Vector3 direcao)
        {
            Vector3 y = -direcao.normalized;
            Vector3 z = Vector3.ProjectOnPlane(transform.forward, y);
            if (z.sqrMagnitude < 0.0001f) z = Vector3.ProjectOnPlane(transform.up, y);
            pivo.rotation = Quaternion.LookRotation(z.normalized, y);
        }

        // Membro pendurado para baixo (-Y): girar -X leva a ponta para a frente (+Z); 'abrir' afasta do corpo
        private static void Membro(Transform pivo, float graus, float abrir)
        {
            if (pivo != null) pivo.localRotation = Quaternion.Euler(-graus, 0f, abrir);
        }

        private static void ParaFrente(Transform pivo, float graus)
        {
            if (pivo != null) pivo.localRotation = Quaternion.Euler(-graus, 0f, 0f);
        }

        private static void DobrarParaFrente(Transform pivo, float graus) => ParaFrente(pivo, graus);

        private static void DobrarParaTras(Transform pivo, float graus)
        {
            if (pivo != null) pivo.localRotation = Quaternion.Euler(graus, 0f, 0f);
        }
    }
}
