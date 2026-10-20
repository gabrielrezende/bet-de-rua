using UnityEngine;

namespace BetsDeRua
{
    /// <summary>Fica no objeto do Animator (o Unity só chama OnAnimatorIK ali) e repassa para o AnimadorCorpo.</summary>
    public class IKCorpo : MonoBehaviour
    {
        public AnimadorCorpo dono;
        public int chamadas;
        private Animator animador;

        private void Awake() => animador = GetComponent<Animator>();

        private void OnAnimatorIK(int camada)
        {
            chamadas++;
            if (dono != null && animador != null) dono.AplicarIK(animador);
        }
    }
}
