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
        [SerializeField] private int quantidade = 28;

        private readonly List<Torcedor> torcedores = new List<Torcedor>();

        private void Start()
        {
            if (biblioteca == null || !biblioteca.Pronta) return;
            System.Random r = new System.Random(42);
            float meio = config.distanciaBases * 0.5f;
            float meiaLargura = config.larguraCampo * 0.5f;
            var lugares = new List<(Vector3 pos, float giro, string pose, bool crianca)>();
            // Em rodinhas de 2 a 4 pessoas (como na rua de verdade), nas duas calçadas, em duas fileiras:
            // a da frente colada no meio-fio (crianças agachadas ali), a de trás encostada no muro.
            int grupo = 0;
            while (lugares.Count < quantidade && grupo < quantidade)
            {
                grupo++;
                bool fundo = grupo % 3 != 0;
                float centro;
                int tentativas = 0;
                do
                {
                    centro = (float)(r.NextDouble() - 0.5) * (config.comprimentoCampo - 6f);
                    tentativas++;
                } while (Mathf.Abs(Mathf.Abs(centro) - meio) < 4.5f && tentativas < 30); // longe das bases
                int tamanho = 2 + r.Next(3);
                for (int k = 0; k < tamanho && lugares.Count < quantidade; k++)
                {
                    bool frente = k % 2 == 0;
                    bool crianca = frente && r.NextDouble() < 0.5;
                    float x = centro + (k - tamanho * 0.5f) * 0.75f + (float)(r.NextDouble() - 0.5) * 0.3f;
                    if (Mathf.Abs(Mathf.Abs(x) - meio) < 4f) continue;
                    // Na rua, a calçada do fundo (a fileira da frente colada no meio-fio); nos outros
                    // lugares, do lado de fora da cerca. Do lado da câmera, sempre atrás do muro baixo.
                    float meiaRua = config.larguraRua * 0.5f;
                    float z = fundo
                        ? (SuperficieAtual.TemCalcada ? (frente ? meiaRua + 0.45f : meiaLargura - 0.5f) : (frente ? meiaLargura + 0.45f : meiaLargura + 1.2f))
                        : (frente ? -meiaLargura - 0.45f : -meiaLargura - 1.2f);
                    float y = fundo && SuperficieAtual.TemCalcada ? config.alturaCalcada : 0f;
                    float giro = (fundo ? 180f : 0f) + (float)(r.NextDouble() - 0.5) * 50f;
                    string[] poses = crianca ? new[] { "Agachado", "Parado", "Torcendo" }
                                   : fundo ? new[] { "BracosCruzados", "Conversando", "Celular", "Parado" } : new[] { "BracosCruzados", "Parado", "Conversando" };
                    lugares.Add((new Vector3(x, y, z), giro, poses[r.Next(poses.Length)], crianca));
                }
            }
            int n = 0;
            foreach (var lugar in lugares)
            {
                GameObject g = new GameObject($"Torcedor{++n}");
                g.transform.SetParent(transform);
                g.transform.SetPositionAndRotation(lugar.pos, Quaternion.Euler(0f, lugar.giro, 0f));
                PerfilPersonagem p = Sortear(r, lugar.crianca);
                Material camisa = coresDeCamisa != null && coresDeCamisa.Length > 0 ? coresDeCamisa[r.Next(coresDeCamisa.Length)] : null;
                Animator a = ConstrutorCorpoReal.Montar(g.transform, p, camisa, biblioteca);
                a.runtimeAnimatorController = biblioteca.controladorTorcida;
                Torcedor t = a.gameObject.AddComponent<Torcedor>();
                t.Iniciar(lugar.pose, (float)r.NextDouble(), lugar.crianca);
                torcedores.Add(t);
            }
            ControladorLance.Comemoracao += AoComemorar;
            ControladorLance.Destaque += AoDestaque;
        }

        // A torcida abre caminho: quem está assistindo dá um passo para trás quando um jogador chega perto
        // (a bola pode ir para a calçada, e os defensores atravessavam as pessoas)


        private readonly List<Vector3> lugarDeCada = new List<Vector3>();
        private Jogador[] jogadores;

        private void Update()
        {
            if (torcedores.Count == 0) return;
            if (jogadores == null || jogadores.Length == 0) jogadores = FindObjectsByType<Jogador>(FindObjectsSortMode.None);
            if (lugarDeCada.Count != torcedores.Count)
            {
                lugarDeCada.Clear();
                foreach (Torcedor t in torcedores) lugarDeCada.Add(t != null ? t.transform.parent.position : Vector3.zero);
            }
            for (int i = 0; i < torcedores.Count; i++)
            {
                Torcedor t = torcedores[i];
                if (t == null) continue;
                Transform pessoa = t.transform.parent;
                Vector3 lugar = lugarDeCada[i];
                Vector3 desvio = Vector3.zero;
                foreach (Jogador j in jogadores)
                {
                    if (j == null) continue;
                    Vector3 d = lugar - j.transform.position;
                    d.y = 0f;
                    float dist = d.magnitude;
                    if (dist >= config.distanciaTorcidaAbrir) continue;
                    Vector3 rumo = dist > 0.01f ? d / dist : Vector3.right;
                    desvio += rumo * (config.passoTorcida * (1f - dist / config.distanciaTorcidaAbrir));
                }
                desvio = Vector3.ClampMagnitude(desvio, config.passoTorcida);
                // Não atravessa o muro do fundo: só anda ao longo da calçada e um pouco para trás
                Vector3 alvo = lugar + desvio;
                float limiteZ = config.larguraCampo * 0.5f - 0.3f;
                if (Mathf.Abs(lugar.z) < limiteZ) alvo.z = Mathf.Clamp(alvo.z, -limiteZ, limiteZ);
                pessoa.position = Vector3.MoveTowards(pessoa.position, alvo, Time.deltaTime * (desvio.sqrMagnitude > 0.01f ? 4f : 1.5f));
            }
        }

        private void OnDestroy()
        {
            ControladorLance.Comemoracao -= AoComemorar;
            ControladorLance.Destaque -= AoDestaque;
        }

        /// <summary>Lance bonito (garrafa caiu, batida perfeita): a rua inteira levanta.</summary>
        private void AoDestaque(Vector3 onde, bool garrafa)
        {
            foreach (Torcedor t in torcedores)
                if (t != null) t.Reagir(true);
        }

        private void AoComemorar(Dupla dupla, bool festa)
        {
            foreach (Torcedor t in torcedores)
                if (t != null) t.Reagir(festa || Random.value < 0.6f);
        }

        private static PerfilPersonagem Sortear(System.Random r, bool crianca)
        {
            PerfilPersonagem p = ScriptableObject.CreateInstance<PerfilPersonagem>();
            p.hideFlags = HideFlags.DontSave;
            p.genero = r.NextDouble() < 0.45 ? Genero.Feminino : Genero.Masculino;
            p.faixaEtaria = crianca ? FaixaEtaria.Crianca : r.NextDouble() < 0.12 ? FaixaEtaria.Crianca : r.NextDouble() < 0.3 ? FaixaEtaria.Adolescente : FaixaEtaria.Adulto;
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
}
