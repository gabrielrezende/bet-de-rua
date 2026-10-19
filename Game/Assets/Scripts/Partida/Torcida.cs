using System.Collections.Generic;
using UnityEngine;

namespace BetsDeRua
{
    /// <summary>
    /// A vizinhança assistindo (como a torcida do FIFA Street / a arquibancada do MLB): pessoas de
    /// verdade, sorteadas, nas calçadas e na frente do bar — em pé de braços cruzados, conversando,
    /// no celular, sentadas, agachadas. Olham a bola e comemoram (ou reclamam) os lances.
    /// Ficam longe das bases e não atrapalham a bola (só cenário).
    /// </summary>
    public class Torcida : MonoBehaviour
    {
        [SerializeField] private BibliotecaCorpos biblioteca;
        [SerializeField] private ConfigFisica config;
        [SerializeField] private Material[] coresDeCamisa;
        [Tooltip("Quantas pessoas assistindo.")]
        [SerializeField] private int quantidade = 12;

        private readonly List<Torcedor> torcedores = new List<Torcedor>();

        private void Start()
        {
            if (biblioteca == null || !biblioteca.Pronta) return;
            System.Random r = new System.Random(42);
            float meio = config.distanciaBases * 0.5f;
            float meiaLargura = config.larguraCampo * 0.5f;
            var lugares = new List<(Vector3 pos, float giro, string pose)>();
            for (int i = 0; i < quantidade; i++)
            {
                bool fundo = i % 3 != 0;
                float x;
                int tentativas = 0;
                do
                {
                    x = (float)(r.NextDouble() - 0.5) * (config.comprimentoCampo - 6f);
                    tentativas++;
                } while (Mathf.Abs(Mathf.Abs(x) - meio) < 4f && tentativas < 20); // longe das bases
                Vector3 pos = fundo ? new Vector3(x, config.alturaCalcada, meiaLargura - 0.55f) : new Vector3(x, 0f, -meiaLargura - 0.9f);
                float giro = fundo ? 180f + (float)(r.NextDouble() - 0.5) * 50f : (float)(r.NextDouble() - 0.5) * 50f;
                string[] poses = fundo ? new[] { "BracosCruzados", "Conversando", "Celular", "Parado", "Agachado" } : new[] { "BracosCruzados", "Parado", "Conversando" };
                lugares.Add((pos, giro, poses[r.Next(poses.Length)]));
            }
            int n = 0;
            foreach (var lugar in lugares)
            {
                GameObject g = new GameObject($"Torcedor{++n}");
                g.transform.SetParent(transform);
                g.transform.SetPositionAndRotation(lugar.pos, Quaternion.Euler(0f, lugar.giro, 0f));
                PerfilPersonagem p = Sortear(r);
                Material camisa = coresDeCamisa != null && coresDeCamisa.Length > 0 ? coresDeCamisa[r.Next(coresDeCamisa.Length)] : null;
                Animator a = ConstrutorCorpoReal.Montar(g.transform, p, camisa, biblioteca);
                a.runtimeAnimatorController = biblioteca.controladorTorcida;
                Torcedor t = a.gameObject.AddComponent<Torcedor>();
                t.Iniciar(lugar.pose, (float)r.NextDouble());
                torcedores.Add(t);
            }
            ControladorLance.Comemoracao += AoComemorar;
        }

        private void OnDestroy() => ControladorLance.Comemoracao -= AoComemorar;

        private void AoComemorar(Dupla dupla, bool festa)
        {
            foreach (Torcedor t in torcedores)
                if (t != null) t.Reagir(festa || Random.value < 0.6f);
        }

        private static PerfilPersonagem Sortear(System.Random r)
        {
            PerfilPersonagem p = ScriptableObject.CreateInstance<PerfilPersonagem>();
            p.hideFlags = HideFlags.DontSave;
            p.genero = r.NextDouble() < 0.45 ? Genero.Feminino : Genero.Masculino;
            p.faixaEtaria = r.NextDouble() < 0.25 ? FaixaEtaria.Crianca : r.NextDouble() < 0.3 ? FaixaEtaria.Adolescente : FaixaEtaria.Adulto;
            Vector2 faixa = PerfilPersonagem.FaixaAltura(p.faixaEtaria);
            p.altura = Mathf.Lerp(faixa.x, faixa.y, (float)r.NextDouble());
            p.tipoCorpo = (TipoCorpo)r.Next(3);
            p.corPele = Color.Lerp(new Color(0.95f, 0.8f, 0.68f), new Color(0.3f, 0.2f, 0.14f), (float)r.NextDouble());
            p.cabelo = (EstiloCabelo)r.Next(System.Enum.GetValues(typeof(EstiloCabelo)).Length);
            p.corCabelo = Color.Lerp(new Color(0.05f, 0.04f, 0.03f), new Color(0.5f, 0.32f, 0.15f), (float)r.NextDouble() * 0.7f);
            p.barba = p.genero == Genero.Masculino && p.faixaEtaria == FaixaEtaria.Adulto && r.NextDouble() < 0.4 ? Barba.Cheia : Barba.Nada;
            p.chapeu = r.NextDouble() < 0.35 ? (Chapeu)(1 + r.Next(4)) : Chapeu.Nada;
            p.camisa = (Camisa)r.Next(3);
            p.bermuda = (Bermuda)r.Next(3);
            p.corBermuda = new Color((float)r.NextDouble() * 0.6f, (float)r.NextDouble() * 0.6f, (float)r.NextDouble() * 0.7f);
            p.calcado = (Calcado)r.Next(3);
            p.corCalcado = r.NextDouble() < 0.5 ? Color.white : new Color((float)r.NextDouble(), (float)r.NextDouble(), (float)r.NextDouble());
            return p;
        }
    }

    /// <summary>Uma pessoa da torcida: fica na pose dela, olha a bola e reage aos lances.</summary>
    public class Torcedor : MonoBehaviour
    {
        private Animator animador;
        private string pose;
        private float voltarEm;
        private Transform bola;

        public void Iniciar(string pose, float fase)
        {
            animador = GetComponent<Animator>();
            this.pose = pose;
            animador.Play(pose, 0, fase);
        }

        public void Reagir(bool festa)
        {
            if (animador == null) return;
            animador.CrossFade(festa ? (Random.value < 0.5f ? "Torcendo" : "Sim") : "Nao", 0.2f, 0, 0f);
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
