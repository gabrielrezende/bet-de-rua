using UnityEngine;

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
