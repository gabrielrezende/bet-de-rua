using UnityEngine;

namespace BetsDeRua
{
    /// <summary>
    /// Mira no chão (docs/REFERENCIAS_FIFA.md, seções 3 e 9.2): círculo amarelo em volta
    /// da garrafa alvo e linha pontilhada curta saindo de quem arremessa, na direção mirada.
    /// Só aparece enquanto alguém mira.
    /// </summary>
    public class MiraNoChao : MonoBehaviour
    {
        [SerializeField] private ConfigHUD configHUD;
        [SerializeField] private Material materialMira;

        // Alturas pequenas para as marcas não "piscarem" com o chão
        private const float AlturaAnel = 0.012f;
        private const float AlturaLinha = 0.014f;

        private MeshFilter anel;
        private MeshFilter linha;
        private Mesh malhaAnel;
        private Mesh malhaLinha;
        private float raioMontado;
        private float espessuraMontada;

        private void Awake()
        {
            anel = CriarFilho("CirculoAlvo");
            linha = CriarFilho("LinhaPontilhada");
            Esconder();
        }

        private MeshFilter CriarFilho(string nome)
        {
            GameObject filho = new GameObject(nome);
            filho.transform.SetParent(transform, false);
            MeshRenderer desenho = filho.AddComponent<MeshRenderer>();
            desenho.sharedMaterial = materialMira;
            desenho.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return filho.AddComponent<MeshFilter>();
        }

        /// <summary>Mostra a mira: linha de 'origem' até 'pontoMira' e círculo em volta de 'centroAlvo'.</summary>
        public void Mostrar(Vector3 origem, Vector3 pontoMira, Vector3 centroAlvo)
        {
            anel.gameObject.SetActive(true);
            linha.gameObject.SetActive(true);

            if (!Mathf.Approximately(raioMontado, configHUD.raioCirculoAlvo)
                || !Mathf.Approximately(espessuraMontada, configHUD.espessuraCirculoAlvo))
            {
                raioMontado = configHUD.raioCirculoAlvo;
                espessuraMontada = configHUD.espessuraCirculoAlvo;
                malhaAnel = MalhasProcedurais.AnelNoChao(raioMontado, espessuraMontada, malhaAnel);
                anel.sharedMesh = malhaAnel;
            }
            anel.transform.SetPositionAndRotation(new Vector3(centroAlvo.x, AlturaAnel, centroAlvo.z), Quaternion.identity);

            // Linha curta: só os primeiros metros, para mostrar a direção sem entregar o ponto exato
            Vector3 fimLinha = pontoMira;
            Vector3 trecho = new Vector3(pontoMira.x - origem.x, 0f, pontoMira.z - origem.z);
            if (configHUD.comprimentoMaximoLinha > 0f && trecho.magnitude > configHUD.comprimentoMaximoLinha)
                fimLinha = origem + trecho.normalized * configHUD.comprimentoMaximoLinha;

            // A linha é montada direto em coordenadas do mundo
            linha.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            malhaLinha = MalhasProcedurais.LinhaTracejada(origem, fimLinha, configHUD.comprimentoTraco,
                                                          configHUD.espacoTraco, configHUD.larguraTraco,
                                                          AlturaLinha, malhaLinha);
            linha.sharedMesh = malhaLinha;
        }

        public void Esconder()
        {
            if (anel != null) anel.gameObject.SetActive(false);
            if (linha != null) linha.gameObject.SetActive(false);
        }
    }
}
