using UnityEngine;

namespace BetsDeRua
{
    /// <summary>
    /// Pose do personagem no fundo do menu: em pé, com a ponta do taco no chão à frente e as
    /// duas mãos no cabo (como o rebatedor esperando o arremesso), respirando e balançando o
    /// peso de um pé para o outro. Usa a mesma cinemática inversa do jogo para as mãos.
    /// </summary>
    public class PoseVitrine : MonoBehaviour
    {
        private Transform quadril, cintura, cabeca, ombroE, ombroD, cotoveloE, cotoveloD, maoE, maoD, coxaE, coxaD, joelhoE, joelhoD;
        private Transform taco;
        private float alturaQuadril;

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
            taco = transform.Find("MaoTaco");
            if (quadril != null) alturaQuadril = quadril.localPosition.y;
        }

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
            if (quadril == null) Religar();
            if (quadril == null) return;
            float t = Time.time;
            float resp = Mathf.Sin(t * 1.6f);
            float peso = Mathf.Sin(t * 0.45f);

            quadril.localPosition = new Vector3(peso * 0.02f, alturaQuadril - 0.035f + resp * 0.004f, 0f);
            quadril.localRotation = Quaternion.Euler(0f, 0f, peso * 2f);
            cintura.localRotation = Quaternion.Euler(9f + resp * 1.2f, 0f, -peso * 2.5f);
            if (cabeca != null) cabeca.localRotation = Quaternion.Euler(-6f + resp, Mathf.Sin(t * 0.3f) * 12f, 0f);
            Perna(coxaE, joelhoE, 6f - peso * 3f, 12f);
            Perna(coxaD, joelhoD, -4f + peso * 3f, 10f);

            // Taco: ponta no chão à frente, cabo na altura da cintura; as mãos seguram o cabo
            if (taco != null)
            {
                Vector3 ponta = transform.TransformPoint(new Vector3(0.05f, 0f, 0.62f));
                Vector3 cabo = transform.TransformPoint(new Vector3(0.04f, alturaQuadril * 0.98f, 0.22f));
                taco.position = cabo;
                taco.rotation = Quaternion.FromToRotation(Vector3.down, (ponta - cabo).normalized);
                Vector3 eixo = (ponta - cabo).normalized;
                Dobrar(ombroD, cotoveloD, maoD, cabo + eixo * 0.03f, Vector3.down + transform.right);
                Dobrar(ombroE, cotoveloE, maoE, cabo + eixo * 0.12f, Vector3.down - transform.right);
            }
        }

        private static void Perna(Transform coxa, Transform joelho, float frente, float dobra)
        {
            if (coxa != null) coxa.localRotation = Quaternion.Euler(-frente, 0f, 0f);
            if (joelho != null) joelho.localRotation = Quaternion.Euler(dobra, 0f, 0f);
        }

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
            Apontar(ombro, e - s);
            Apontar(cotovelo, s + dir * d - e);
        }

        private void Apontar(Transform pivo, Vector3 direcao)
        {
            Vector3 y = -direcao.normalized;
            Vector3 z = Vector3.ProjectOnPlane(transform.forward, y);
            if (z.sqrMagnitude < 0.0001f) z = Vector3.ProjectOnPlane(transform.up, y);
            pivo.rotation = Quaternion.LookRotation(z.normalized, y);
        }
    }
}

namespace BetsDeRua
{
    /// <summary>
    /// Vitrine com corpo de verdade: postura de guarda (animação), as duas mãos no cabo do taco
    /// apoiado no chão à frente e a cabeça olhando para a câmera.
    /// </summary>
    public class VitrineIK : UnityEngine.MonoBehaviour
    {
        public UnityEngine.Transform vitrine;
        public UnityEngine.Camera cameraFundo;
        private UnityEngine.Animator animador;

        private void Awake() => animador = GetComponent<UnityEngine.Animator>();

        private void LateUpdate()
        {
            UnityEngine.Transform taco = vitrine != null ? vitrine.Find("MaoTaco") : null;
            if (taco == null) return;
            UnityEngine.Vector3 ponta = vitrine.TransformPoint(new UnityEngine.Vector3(0.12f, 0f, 0.62f));
            UnityEngine.Vector3 cabo = vitrine.TransformPoint(new UnityEngine.Vector3(0.1f, 0.92f, 0.24f));
            taco.position = cabo;
            taco.rotation = UnityEngine.Quaternion.FromToRotation(UnityEngine.Vector3.down, (ponta - cabo).normalized);
        }

        private void OnAnimatorIK(int camada)
        {
            UnityEngine.Transform taco = vitrine != null ? vitrine.Find("MaoTaco") : null;
            if (animador == null || taco == null) return;
            UnityEngine.Vector3 eixo = -taco.up;
            foreach (var (meta, d) in new[] { (UnityEngine.AvatarIKGoal.RightHand, 0.04f), (UnityEngine.AvatarIKGoal.LeftHand, 0.14f) })
            {
                animador.SetIKPositionWeight(meta, 1f);
                animador.SetIKPosition(meta, taco.position + eixo * d);
            }
            if (cameraFundo != null)
            {
                animador.SetLookAtWeight(0.7f, 0.1f, 0.8f, 0f, 0.6f);
                animador.SetLookAtPosition(cameraFundo.transform.position);
            }
        }
    }
}
