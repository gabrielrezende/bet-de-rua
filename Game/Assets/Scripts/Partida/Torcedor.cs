using UnityEngine;

namespace BetsDeRua
{
    /// <summary>Uma pessoa da torcida: fica na pose dela, olha a bola e reage aos lances.</summary>
    public class Torcedor : MonoBehaviour
    {
        private Animator animador;
        private string pose;
        private float voltarEm;
        private Transform bola;

        private bool crianca;

        public void Iniciar(string pose, float fase, bool crianca = false)
        {
            this.crianca = crianca;
            animador = GetComponent<Animator>();
            this.pose = pose;
            animador.Play(pose, 0, fase);
        }

        public void Reagir(bool festa)
        {
            if (animador == null) return;
            // Criança comemora pulando quase sempre; adulto às vezes só balança a cabeça
            string reacao = festa ? (crianca || Random.value < 0.5f ? "Torcendo" : "Sim") : "Nao";
            animador.CrossFade(reacao, Random.Range(0.1f, 0.3f), 0, 0f);
            voltarEm = Time.time + Random.Range(1.8f, 3f);
        }

        private void Update()
        {
            if (voltarEm > 0f && Time.time > voltarEm)
            {
                voltarEm = 0f;
                animador.CrossFade(pose, 0.3f, 0, Random.value);
            }
        }

        private void OnAnimatorIK(int camada)
        {
            if (bola == null)
            {
                Bola b = FindFirstObjectByType<Bola>();
                if (b != null) bola = b.transform;
            }
            if (bola == null || animador == null) return;
            animador.SetLookAtWeight(0.8f, 0.1f, 0.8f, 0f, 0.6f);
            animador.SetLookAtPosition(bola.position);
        }
    }
}
