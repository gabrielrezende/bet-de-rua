using System.Collections;
using UnityEngine;

namespace BetsDeRua
{
    /// <summary>
    /// A casinha: uma garrafa em pé na base.
    /// Fica parada (Rigidbody cinemático) até levar uma bolada acima do limite;
    /// aí vira física de verdade e cai. Bolada fraca só faz ela balançar ("quase!").
    /// O objeto tem o pivô na base, para balançar a partir do chão.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class Garrafa : MonoBehaviour
    {
        [SerializeField] private ConfigFisica config;

        private Rigidbody corpo;
        private Vector3 posicaoInicial;
        private Quaternion rotacaoInicial;
        private Coroutine balancoAtual;

        /// <summary>True depois que a garrafa foi derrubada (até o próximo Reiniciar).</summary>
        public bool Derrubada { get; private set; }

        /// <summary>Velocidade da última bolada que a garrafa levou (km/h).</summary>
        public float UltimaBoladaKmh { get; private set; }

        public float Raio => config.diametroGarrafa * 0.5f;
        public float Altura => config.alturaGarrafa;

        private void Awake()
        {
            corpo = GetComponent<Rigidbody>();
            posicaoInicial = transform.position;
            rotacaoInicial = transform.rotation;
            corpo.mass = config.massaGarrafa;
            corpo.isKinematic = true;
        }

        /// <summary>
        /// Chamado pela bola quando encosta na garrafa.
        /// velocidadeBola em m/s, pontoContato em coordenadas do mundo.
        /// </summary>
        public void ReceberBolada(Vector3 velocidadeBola, Vector3 pontoContato)
        {
            if (Derrubada) return;

            float velocidadeKmh = velocidadeBola.magnitude * 3.6f;
            UltimaBoladaKmh = velocidadeKmh;
            if (velocidadeKmh >= config.velocidadeDerrubaKmh)
                Derrubar(velocidadeBola, pontoContato);
            else
                Balancar(velocidadeBola);
        }

        private void Derrubar(Vector3 velocidadeBola, Vector3 pontoContato)
        {
            if (balancoAtual != null) StopCoroutine(balancoAtual);
            Derrubada = true;
            corpo.mass = config.massaGarrafa;
            corpo.isKinematic = false;
            // Empurrão = quantidade de movimento da bola (massa × velocidade)
            Vector3 impulso = velocidadeBola * config.massaBola * config.forcaDerrubada;
            corpo.AddForceAtPosition(impulso, pontoContato, ForceMode.Impulse);
        }

        private void Balancar(Vector3 velocidadeBola)
        {
            if (balancoAtual != null) StopCoroutine(balancoAtual);
            // Inclina para o lado em que a bola empurrou
            Vector3 direcao = new Vector3(velocidadeBola.x, 0f, velocidadeBola.z).normalized;
            Vector3 eixo = Vector3.Cross(Vector3.up, direcao);
            if (eixo.sqrMagnitude < 0.0001f) eixo = Vector3.forward;
            balancoAtual = StartCoroutine(RotinaBalanco(eixo));
        }

        private IEnumerator RotinaBalanco(Vector3 eixo)
        {
            float tempo = 0f;
            float duracao = Mathf.Max(0.05f, config.duracaoBalanco);
            while (tempo < duracao)
            {
                tempo += Time.deltaTime;
                float t = tempo / duracao;
                // Oscila 3 vezes, perdendo força até parar
                float angulo = config.anguloBalanco * (1f - t) * Mathf.Sin(t * Mathf.PI * 6f);
                transform.rotation = Quaternion.AngleAxis(angulo, eixo) * rotacaoInicial;
                yield return null;
            }
            transform.rotation = rotacaoInicial;
            balancoAtual = null;
        }

        /// <summary>Coloca a garrafa de pé de novo na base.</summary>
        public void Reiniciar()
        {
            if (balancoAtual != null) StopCoroutine(balancoAtual);
            balancoAtual = null;
            if (!corpo.isKinematic)
            {
                corpo.linearVelocity = Vector3.zero;
                corpo.angularVelocity = Vector3.zero;
            }
            corpo.isKinematic = true;
            transform.SetPositionAndRotation(posicaoInicial, rotacaoInicial);
            Derrubada = false;
            UltimaBoladaKmh = 0f;
        }
    }
}
