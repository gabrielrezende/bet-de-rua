using System;
using UnityEngine;

namespace BetsDeRua
{
    /// <summary>
    /// A corrida da dupla do taco depois da batida (GDD, passos 6 a 8):
    /// - Cruzada: os dois dentro da faixa do meio e a até distanciaCruzada um do outro;
    ///   aparece um anel no chão e X (A) / Espaço bate os tacos = +1 ponto pendente.
    ///   Sem ninguém na dupla, os bots cruzam sozinhos.
    /// - O ponto só vale quando os dois estão seguros: dentro de um círculo, com o taco no chão.
    /// </summary>
    public class Corrida : MonoBehaviour
    {
        [SerializeField] private ConfigPartida configPartida;
        [SerializeField] private ConfigFisica configFisica;
        [SerializeField] private ConfigHUD configHUD;
        [SerializeField] private Jogador rebatedorA;
        [SerializeField] private Jogador rebatedorB;
        [SerializeField] private Material materialAnel;

        /// <summary>Avisa quando os tacos se cruzam (pontos pendentes agora).</summary>
        public event Action<int> Cruzou;
        /// <summary>Avisa quando pontos pendentes viram pontos de verdade (quantos).</summary>
        public event Action<int> PontosValidados;

        private const float AlturaAnel = 0.02f;
        private MeshFilter anel;
        private Mesh malhaAnel;
        private Taco tacoA, tacoB;
        private bool cruzouNestaPassagem;   // evita contar duas vezes a mesma passagem pelo meio

        /// <summary>True depois da batida, enquanto a bola está viva.</summary>
        public bool Ativa { get; private set; }
        public int PontosPendentes { get; private set; }
        /// <summary>True se os dois estão no ponto de cruzar agora.</summary>
        public bool PodeCruzar { get; private set; }

        private void Awake()
        {
            tacoA = rebatedorA.GetComponentInChildren<Taco>();
            tacoB = rebatedorB.GetComponentInChildren<Taco>();
            GameObject filho = new GameObject("AnelCruzada");
            filho.transform.SetParent(transform, false);
            MeshRenderer desenho = filho.AddComponent<MeshRenderer>();
            desenho.sharedMaterial = materialAnel;
            desenho.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            anel = filho.AddComponent<MeshFilter>();
            filho.SetActive(false);
        }

        /// <summary>Começa a valer a corrida (a bola foi rebatida).</summary>
        public void Ativar()
        {
            Ativa = true;
            cruzouNestaPassagem = false;
        }

        /// <summary>Fim do lance: pontos pendentes que não viraram ponto são perdidos.</summary>
        public void Encerrar()
        {
            Ativa = false;
            PontosPendentes = 0;
            PodeCruzar = false;
            anel.gameObject.SetActive(false);
        }

        /// <summary>True se o rebatedor está seguro: dentro de um círculo com o taco no chão.</summary>
        public bool Seguro(Jogador rebatedor)
        {
            Taco taco = rebatedor == rebatedorA ? tacoA : tacoB;
            return rebatedor.DentroDoCirculo() && taco != null && taco.NoChao;
        }

        public bool AmbosSeguros => Seguro(rebatedorA) && Seguro(rebatedorB);

        private void Update()
        {
            if (!Ativa) return;

            Vector3 a = rebatedorA.transform.position;
            Vector3 b = rebatedorB.transform.position;
            float meiaFaixa = configFisica.larguraFaixaMeio * 0.5f;
            bool aNaFaixa = Mathf.Abs(a.x) <= meiaFaixa;
            bool bNaFaixa = Mathf.Abs(b.x) <= meiaFaixa;
            float distancia = new Vector2(a.x - b.x, a.z - b.z).magnitude;

            // Os dois saíram da faixa: a próxima passagem pode cruzar de novo ("mais uma")
            if (!aNaFaixa && !bNaFaixa) cruzouNestaPassagem = false;

            PodeCruzar = aNaFaixa && bNaFaixa && distancia <= configPartida.distanciaCruzada && !cruzouNestaPassagem;
            MostrarAnel(a, b);

            if (PodeCruzar && AlguemApertouCruzar()) Cruzar();

            // O ponto vale quando os dois param seguros nos círculos
            if (PontosPendentes > 0 && AmbosSeguros)
            {
                int validados = PontosPendentes;
                PontosPendentes = 0;
                PontosValidados?.Invoke(validados);
            }
        }

        private bool AlguemApertouCruzar()
        {
            bool temHumano = false;
            foreach (Jogador rebatedor in new[] { rebatedorA, rebatedorB })
            {
                ControleHumano humano = rebatedor.Controlador;
                if (humano == null || !humano.Conectado) continue;
                temHumano = true;
                if (humano.ApertouCruzar) return true;
            }
            // Sem ninguém na dupla do taco: os bots cruzam sozinhos
            return !temHumano;
        }

        private void Cruzar()
        {
            cruzouNestaPassagem = true;
            PontosPendentes++;
            Cruzou?.Invoke(PontosPendentes);
        }

        private void MostrarAnel(Vector3 a, Vector3 b)
        {
            bool perto = Mathf.Abs(a.x) <= configFisica.larguraFaixaMeio && Mathf.Abs(b.x) <= configFisica.larguraFaixaMeio
                         && !cruzouNestaPassagem;
            anel.gameObject.SetActive(perto);
            if (!perto) return;
            // Anel em volta dos dois; fica cheio (mais grosso) quando dá para cruzar
            float raio = configPartida.distanciaCruzada * 0.5f + configFisica.raioJogador;
            float espessura = PodeCruzar ? configHUD.espessuraAnelTempo * 2.5f : configHUD.espessuraAnelTempo;
            malhaAnel = MalhasProcedurais.AnelNoChao(raio, espessura, malhaAnel);
            anel.sharedMesh = malhaAnel;
            Vector3 meio = (a + b) * 0.5f;
            anel.transform.SetPositionAndRotation(new Vector3(meio.x, AlturaAnel, meio.z), Quaternion.identity);
        }
    }
}
