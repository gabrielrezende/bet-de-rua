using UnityEngine;

namespace BetsDeRua
{
    /// <summary>
    /// Anima o boneco de peças simples (protótipo cinza) por código, lendo o que o jogo já sabe:
    /// velocidade (andar, correr, sprint), arremesso (braço vai para trás enchendo o arco e
    /// chicoteia ao soltar), batida (tronco gira com o taco; agacha na varrida), pegar a bola
    /// (abaixa), mergulho (pose esticada) e cruzada (braço sobe com o taco).
    /// Os pivôs são criados pelo MontadorCena com estes nomes; as medidas ficam no ConfigHUD
    /// (seção do boneco). Quando houver modelo de verdade, este script vira um "tradutor" para o Animator.
    /// Convenção: os membros pendem para baixo do pivô; ângulo "para a frente" positivo.
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

        private Transform quadril, cintura, ombroE, ombroD, cotoveloE, cotoveloD, coxaE, coxaD, joelhoE, joelhoD;
        private float alturaQuadril;
        private float fase;
        private float tempoLancou = float.MaxValue;
        private float tempoPegou = float.MaxValue;

        private void Awake()
        {
            jogador = GetComponent<Jogador>();
            arremessador = GetComponent<Arremessador>();
            rebatedor = GetComponent<Rebatedor>();
            taco = GetComponentInChildren<Taco>();
            defensor = GetComponent<Defensor>();

            quadril = Achar("Quadril");
            cintura = Achar("Cintura");
            ombroE = Achar("OmbroE");
            ombroD = Achar("OmbroD");
            cotoveloE = Achar("CotoveloE");
            cotoveloD = Achar("CotoveloD");
            coxaE = Achar("CoxaE");
            coxaD = Achar("CoxaD");
            joelhoE = Achar("JoelhoE");
            joelhoD = Achar("JoelhoD");
            if (quadril != null) alturaQuadril = quadril.localPosition.y;
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
            foreach (Transform t in GetComponentsInChildren<Transform>(true))
                if (t.name == nome) return t;
            return null;
        }

        private void LateUpdate()
        {
            if (quadril == null) return;
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

            float pernaE = s * amplitudePerna, pernaD = -s * amplitudePerna;
            float joelhoEang = Mathf.Max(0f, -s) * amplitudePerna * 1.1f + 5f * fator;
            float joelhoDang = Mathf.Max(0f, s) * amplitudePerna * 1.1f + 5f * fator;
            float bracoE = -s * amplitudeBraco, bracoD = s * amplitudeBraco;
            float cotoveloEang = 15f + 40f * fator, cotoveloDang = 15f + 40f * fator;
            float inclinacao = configHUD.inclinacaoCorrendo * fator;
            float giroTronco = 0f;
            float agachar = 0f;
            float balanco = Mathf.Abs(Mathf.Sin(fase)) * 0.035f * fator;

            // ---- Rebatedor: braços no taco; gira o tronco na batida; agacha na varrida
            if (rebatedor != null && taco != null)
            {
                bool parado = rapidez < 0.5f;
                if (parado)
                {
                    bracoE = bracoD = 30f;
                    cotoveloEang = cotoveloDang = 25f;
                    agachar = 0.06f;
                }
                if (taco.Girando)
                {
                    float t = taco.FracaoGiro;
                    giroTronco = Mathf.Lerp(-70f, 70f, t);
                    bracoE = bracoD = 70f;
                    cotoveloEang = cotoveloDang = 10f;
                    if (rebatedor.UltimoTipo == TipoBatida.Varrida) agachar = 0.22f;
                }
                if (taco.BatendoTacos)
                {
                    bracoD = 160f;
                    cotoveloDang = 10f;
                }
            }

            // ---- Com a bola: braço do arremesso
            if (arremessador != null && arremessador.ComBola)
            {
                bracoD = 25f;
                cotoveloDang = 70f;
                if (arremessador.Carregando)
                {
                    // Enche o arco: o braço vai para trás e o tronco gira
                    float c = arremessador.Carga;
                    bracoD = Mathf.Lerp(20f, -110f, c);
                    cotoveloDang = Mathf.Lerp(60f, 90f, c);
                    giroTronco = Mathf.Lerp(0f, -35f, c);
                }
            }
            if (tempoLancou < configHUD.duracaoGestoLancar)
            {
                // Soltou: o braço chicoteia para a frente e para baixo
                float t = tempoLancou / configHUD.duracaoGestoLancar;
                bracoD = Mathf.Lerp(-100f, 70f, Mathf.SmoothStep(0f, 1f, t));
                cotoveloDang = Mathf.Lerp(80f, 10f, t);
                giroTronco = Mathf.Lerp(-30f, 25f, t);
                inclinacao = Mathf.Max(inclinacao, 15f * (1f - t));
            }

            // ---- Pegou a bola: abaixa e se curva
            if (tempoPegou < configHUD.duracaoGestoPegar)
            {
                float t = Mathf.Sin(tempoPegou / configHUD.duracaoGestoPegar * Mathf.PI);
                inclinacao = Mathf.Max(inclinacao, 55f * t);
                agachar = Mathf.Max(agachar, 0.18f * t);
                bracoD = Mathf.Max(bracoD, 80f * t);
                bracoE = Mathf.Max(bracoE, 60f * t);
            }

            // ---- Mergulho: pose esticada (o Jogador deita o corpo inteiro)
            if (jogador.Mergulhando)
            {
                bracoE = bracoD = 170f;
                cotoveloEang = cotoveloDang = 5f;
                pernaE = pernaD = -10f;
                joelhoEang = joelhoDang = 5f;
                inclinacao = 0f;
                agachar = 0f;
                balanco = 0f;
            }

            // ---- Aplica (para a frente = positivo)
            quadril.localPosition = new Vector3(0f, alturaQuadril - agachar + balanco, 0f);
            cintura.localRotation = Quaternion.Euler(inclinacao, giroTronco, 0f);
            ParaFrente(ombroE, bracoE);
            ParaFrente(ombroD, bracoD);
            DobrarParaFrente(cotoveloE, cotoveloEang);
            DobrarParaFrente(cotoveloD, cotoveloDang);
            float joelhoAgachado = agachar * 250f;
            ParaFrente(coxaE, pernaE + joelhoAgachado * 0.5f);
            ParaFrente(coxaD, pernaD + joelhoAgachado * 0.5f);
            DobrarParaTras(joelhoE, joelhoEang + joelhoAgachado);
            DobrarParaTras(joelhoD, joelhoDang + joelhoAgachado);
        }

        // Membro pendurado para baixo (-Y): girar -X leva a ponta para a frente (+Z)
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
