using UnityEngine;
using UnityEngine.InputSystem;

namespace BetsDeRua
{
    /// <summary>
    /// Ferramenta provisória da Etapa 1: lança a bola com botões para testar
    /// a câmera, o quique e a garrafa. Sai de cena quando o arremesso de verdade
    /// (Etapa 3) chegar.
    ///   A (✕) / Espaço  → lançamento forte em direção aleatória
    ///   B (○) / 1       → lançamento forte na garrafa B (deve derrubar)
    ///   X (□) / 2       → lançamento fraco na garrafa B (só balança)
    ///   Y (△) / R       → reinicia bola e garrafas
    /// </summary>
    public class LancadorDeTeste : MonoBehaviour
    {
        [SerializeField] private ConfigFisica config;
        [SerializeField] private Bola bola;
        [Tooltip("Garrafa que os lançamentos 1 e 2 tentam acertar.")]
        [SerializeField] private Garrafa garrafaAlvo;
        [SerializeField] private Garrafa[] todasGarrafas;

        private void Update()
        {
            Gamepad controle = Gamepad.current;
            Keyboard teclado = Keyboard.current;

            if (Apertou(controle?.buttonSouth, teclado?.spaceKey)) LancarAleatorio();
            if (Apertou(controle?.buttonEast, teclado?.digit1Key)) LancarNaGarrafa(config.velocidadeTesteForteKmh);
            if (Apertou(controle?.buttonWest, teclado?.digit2Key)) RolarNaGarrafa();
            if (Apertou(controle?.buttonNorth, teclado?.rKey)) Reiniciar();
        }

        private static bool Apertou(UnityEngine.InputSystem.Controls.ButtonControl botao,
                                    UnityEngine.InputSystem.Controls.KeyControl tecla)
        {
            return (botao != null && botao.wasPressedThisFrame) || (tecla != null && tecla.wasPressedThisFrame);
        }

        private Vector3 PontoDeSaidaBaseA()
        {
            return new Vector3(-config.distanciaBases * 0.5f, config.alturaSaidaTeste, 0f);
        }

        private void LancarAleatorio()
        {
            // Direção para o lado da base B, abrindo até 60° para os lados, subindo de 10° a 40°
            float anguloLado = Random.Range(-60f, 60f);
            float anguloSubida = Random.Range(10f, 40f);
            Vector3 direcao = Quaternion.Euler(0f, anguloLado, 0f) * Vector3.right;
            direcao = Vector3.RotateTowards(direcao, Vector3.up, anguloSubida * Mathf.Deg2Rad, 0f);
            bola.Lancar(PontoDeSaidaBaseA(), direcao * (config.velocidadeTesteForteKmh / 3.6f));
        }

        /// <summary>Bola fraca rolando no chão até a garrafa alvo (deve só balançar).</summary>
        private void RolarNaGarrafa()
        {
            Vector3 alvo = garrafaAlvo.transform.position;
            Vector3 saida = new Vector3(alvo.x - config.distanciaTesteFraco, config.raioBola, alvo.z);
            bola.Lancar(saida, Vector3.right * (config.velocidadeTesteFracoKmh / 3.6f));
        }

        /// <summary>Lança da base A na meia altura da garrafa alvo.</summary>
        private void LancarNaGarrafa(float velocidadeKmh)
        {
            Vector3 alvo = garrafaAlvo.transform.position + Vector3.up * (config.alturaGarrafa * 0.5f);
            Vector3 saida = PontoDeSaidaBaseA();

            Vector3 horizontal = alvo - saida;
            horizontal.y = 0f;
            float distancia = horizontal.magnitude;
            float rapidezHorizontal = velocidadeKmh / 3.6f;
            float subida = AcharSubida(distancia, rapidezHorizontal, alvo.y - saida.y);

            bola.Lancar(saida, horizontal.normalized * rapidezHorizontal + Vector3.up * subida);
        }

        /// <summary>
        /// Procura a velocidade vertical inicial que faz a bola chegar na altura certa
        /// depois de andar 'distancia' metros, já contando gravidade e resistência do ar.
        /// (Busca binária simulando a trajetória.)
        /// </summary>
        private float AcharSubida(float distancia, float rapidezHorizontal, float diferencaAltura)
        {
            float minimo = -5f, maximo = 30f;
            for (int i = 0; i < 25; i++)
            {
                float meio = (minimo + maximo) * 0.5f;
                if (AlturaAoChegar(distancia, rapidezHorizontal, meio) < diferencaAltura) minimo = meio;
                else maximo = meio;
            }
            return (minimo + maximo) * 0.5f;
        }

        private float AlturaAoChegar(float distancia, float vh, float vy)
        {
            const float passo = 0.005f;
            float x = 0f, y = 0f;
            for (int i = 0; i < 4000 && x < distancia; i++)
            {
                float rapidez = Mathf.Sqrt(vh * vh + vy * vy);
                float freio = config.arrastoAr * rapidez * passo;
                vh -= vh * freio;
                vy -= vy * freio + config.gravidade * passo;
                x += vh * passo;
                y += vy * passo;
                if (vh <= 0.01f) break;
            }
            return y;
        }

        /// <summary>Onde a bola fica parada antes do teste: logo à frente da base A, fora da garrafa.</summary>
        public static Vector3 PosicaoRepouso(ConfigFisica config)
        {
            return new Vector3(-config.distanciaBases * 0.5f + config.diametroBase, config.raioBola, 0f);
        }

        private void Reiniciar()
        {
            bola.Colocar(PosicaoRepouso(config));
            foreach (Garrafa garrafa in todasGarrafas) garrafa.Reiniciar();
        }

        private void OnGUI()
        {
            GUIStyle estilo = new GUIStyle(GUI.skin.label) { fontSize = 14 };
            estilo.normal.textColor = Color.white;
            string texto =
                "TESTE DA ETAPA 1\n" +
                "A (✕) / Espaço: lançamento aleatório\n" +
                "B (○) / 1: forte na garrafa B\n" +
                "X (□) / 2: fraco na garrafa B\n" +
                "Y (△) / R: reiniciar\n\n" +
                $"Bola agora: {bola.Velocidade.magnitude * 3.6f:0} km/h\n" +
                $"Garrafa B: {(garrafaAlvo.Derrubada ? "CAIU" : "em pé")}   " +
                $"última bolada: {garrafaAlvo.UltimaBoladaKmh:0} km/h (cai a partir de {config.velocidadeDerrubaKmh:0})";
            GUI.Label(new Rect(16, 8, 700, 200), texto, estilo);
        }
    }
}
