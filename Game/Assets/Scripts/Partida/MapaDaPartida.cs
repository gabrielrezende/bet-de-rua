using UnityEngine;

namespace BetsDeRua
{
    /// <summary>
    /// Sorteia onde a partida acontece (rua, terrão, campinho, fazenda), liga só o cenário dele e
    /// passa o chão para a física da bola. Roda antes de todo mundo (a torcida já nasce no lugar certo).
    /// </summary>
    [DefaultExecutionOrder(-500)]
    public class MapaDaPartida : MonoBehaviour
    {
        [SerializeField] private ConfigMapas config;
        [Tooltip("Coisas só da rua de asfalto (calçadas, casas da frente, carros, postes).")]
        [SerializeField] private GameObject soDaRua;
        [SerializeField] private GameObject morro;
        [Tooltip("Cenários dos outros lugares (Mapa_Terrao, Mapa_Campinho...), desligados na cena.")]
        [SerializeField] private GameObject[] cenarios;

        /// <summary>Escolha feita fora da partida (-1 = sortear). O treino usa a rua.</summary>
        public static int Escolhido = -1;

        public static ConfigMapas.Mapa Atual { get; private set; }

        private void Awake()
        {
            if (config == null || config.mapas == null || config.mapas.Length == 0) { SuperficieAtual.Asfalto(); return; }
            int i = Escolhido >= 0 ? Escolhido : config.sortear ? Sortear() : config.mapaFixo;
            i = Mathf.Clamp(i, 0, config.mapas.Length - 1);
            Ligar(config.mapas[i]);
        }

        private void OnDestroy() => SuperficieAtual.Asfalto();

        private int Sortear()
        {
            float total = 0f;
            foreach (ConfigMapas.Mapa m in config.mapas) total += Mathf.Max(0f, m.peso);
            float sorte = Random.value * total;
            for (int i = 0; i < config.mapas.Length; i++)
            {
                sorte -= Mathf.Max(0f, config.mapas[i].peso);
                if (sorte <= 0f) return i;
            }
            return 0;
        }

        private void Ligar(ConfigMapas.Mapa mapa)
        {
            Atual = mapa;
            bool rua = mapa.nome == "Rua";
            if (soDaRua != null) soDaRua.SetActive(rua);
            if (morro != null) morro.SetActive(mapa.mostrarMorro);
            if (cenarios != null)
                foreach (GameObject c in cenarios)
                    if (c != null) c.SetActive(c.name == $"Mapa_{mapa.nome}");
            SuperficieAtual.Aplicar(mapa);
        }

        private void Start()
        {
            if (Atual != null) RegistroPartida.Evento($"Lugar da partida: {Atual.titulo}");
        }
    }
}
