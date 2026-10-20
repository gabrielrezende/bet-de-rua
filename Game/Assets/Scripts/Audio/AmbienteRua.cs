using System.Collections.Generic;
using UnityEngine;

namespace BetsDeRua
{
    /// <summary>Clima de rua: barulho de fundo em loop e passarinhos de vez em quando.</summary>
    public class AmbienteRua : MonoBehaviour
    {
        [SerializeField, Range(0f, 1f)] private float volumeFundo = 0.18f;
        private float proximoPassaro;

        private void Start()
        {
            AudioSource fundo = gameObject.AddComponent<AudioSource>();
            fundo.clip = Sons.FundoDaRua();
            fundo.loop = true;
            fundo.volume = volumeFundo;
            fundo.spatialBlend = 0f;
            fundo.Play();
            proximoPassaro = Time.time + Random.Range(2f, 5f);
        }

        private void Update()
        {
            if (Time.time < proximoPassaro) return;
            proximoPassaro = Time.time + Random.Range(4f, 11f);
            Sons.Tocar(TipoSom.Passaro, Random.Range(0.12f, 0.3f), 0.15f);
        }
    }
}
