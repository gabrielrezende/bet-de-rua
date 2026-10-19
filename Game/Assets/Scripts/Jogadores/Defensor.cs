using System;
using UnityEngine;

namespace BetsDeRua
{
    /// <summary>
    /// Pegar a bola na defesa (GDD, "Dinâmica do lance", passo 4):
    /// - □ (X do Xbox) / R perto da bola (até raioPegar) = pega.
    ///   Antes dela quicar = "pegou no ar" (o ataque perde o taco).
    /// - □ com a bola um pouco mais longe (até alcanceMergulho) = mergulho, ou carrinho
    ///   se a bola estiver no chão: um bote rápido que pega se alcançar; errou, fica caído.
    /// - Passe do colega chegando perto é pego sozinho.
    /// Quem decide se valeu é o ControladorLance (ouvindo o evento Pegou).
    /// </summary>
    [RequireComponent(typeof(Jogador))]
    public class Defensor : MonoBehaviour
    {
        [SerializeField] private ConfigFisica config;
        [SerializeField] private ConfigHUD configHUD;
        [SerializeField] private Bola bola;
        [Tooltip("Círculo de alcance normal (bola longe).")]
        [SerializeField] private Material materialAlcance;
        [Tooltip("Círculo de alcance aceso (bola ao alcance: aperte □).")]
        [SerializeField] private Material materialAoAlcance;

        /// <summary>Avisa (quem pegou, pegou no ar?).</summary>
        public event Action<Defensor, bool> Pegou;

        private Jogador jogador;
        private Arremessador comBola;
        private bool procurandoNoBote;

        // Círculo no chão mostrando até onde dá para pegar a bola (só para quem uma pessoa controla)
        private const float AlturaCirculo = 0.018f;
        private MeshFilter circulo;
        private MeshRenderer desenhoCirculo;
        private Mesh malhaCirculo;
        private bool circuloAceso;

        /// <summary>O ControladorLance diz se a bola está solta no campo (dá para pegar).</summary>
        public bool BolaLivre { get; set; }
        public Jogador Jogador => jogador;
        public Arremessador ComBola => comBola;

        private void Awake()
        {
            jogador = GetComponent<Jogador>();
            comBola = GetComponent<Arremessador>();

            GameObject filho = new GameObject("CirculoPegar");
            filho.transform.SetParent(transform, false);
            desenhoCirculo = filho.AddComponent<MeshRenderer>();
            desenhoCirculo.sharedMaterial = materialAlcance;
            desenhoCirculo.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            circulo = filho.AddComponent<MeshFilter>();
            filho.SetActive(false);
        }

        private void LateUpdate()
        {
            // Círculo de alcance: aparece para quem você controla com a bola solta;
            // acende (amarelo e mais grosso) quando a bola está ao alcance do □
            bool mostrar = BolaLivre && jogador.Controlador != null && !jogador.Mergulhando;
            circulo.gameObject.SetActive(mostrar);
            if (!mostrar) return;
            bool aoAlcance = AoAlcance(config.raioPegar);
            if (malhaCirculo == null || aoAlcance != circuloAceso)
            {
                circuloAceso = aoAlcance;
                float espessura = configHUD.espessuraRaioAlcance * (aoAlcance ? 3f : 1f);
                malhaCirculo = MalhasProcedurais.AnelNoChao(config.raioPegar, espessura, malhaCirculo);
                circulo.sharedMesh = malhaCirculo;
                desenhoCirculo.sharedMaterial = aoAlcance ? materialAoAlcance : materialAlcance;
            }
            circulo.transform.SetPositionAndRotation(new Vector3(transform.position.x, AlturaCirculo, transform.position.z),
                                                     Quaternion.identity);
        }

        private void Update()
        {
            if (!BolaLivre) { procurandoNoBote = false; return; }

            ControleHumano humano = jogador.Controlador;
            if (humano != null && humano.Conectado && humano.ApertouPegar) TentarPegar();

            // No bote do mergulho: pega se alcançar
            if (procurandoNoBote)
            {
                if (!jogador.NoBoteDoMergulho) procurandoNoBote = false;
                else if (AoAlcance(config.raioPegarNoMergulho)) Pegar();
            }

            // Passe do colega chegando: pega sozinho
            Jogador lancador = bola.UltimoLancador;
            if (lancador != null && lancador != jogador && lancador.Dupla == jogador.Dupla
                && AoAlcance(config.raioReceberPasse))
                Pegar();
        }

        /// <summary>
        /// Tenta pegar (é o que o □ faz; os bots também usam). Perto: pega.
        /// Um pouco longe: mergulha na direção da bola. Longe demais: nada.
        /// </summary>
        public void TentarPegar()
        {
            if (!BolaLivre || jogador.Mergulhando) return;
            if (AoAlcance(config.raioPegar)) { Pegar(); return; }

            Vector3 rumo = bola.Posicao - transform.position;
            rumo.y = 0f;
            if (rumo.magnitude <= config.alcanceMergulho && bola.Posicao.y <= config.alturaMaximaPegar)
            {
                jogador.Mergulhar(rumo);
                procurandoNoBote = true;
            }
        }

        /// <summary>True se a bola está a até 'raio' metros (no chão) e numa altura que dá para pegar.</summary>
        public bool AoAlcance(float raio)
        {
            Vector3 rumo = bola.Posicao - transform.position;
            rumo.y = 0f;
            return rumo.magnitude <= raio && bola.Posicao.y <= config.alturaMaximaPegar;
        }

        private void Pegar()
        {
            procurandoNoBote = false;
            bool noAr = !bola.QuicouDesdeLancamento;
            Pegou?.Invoke(this, noAr);
        }
    }
}
