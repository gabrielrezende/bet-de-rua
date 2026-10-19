using UnityEngine;

namespace BetsDeRua
{
    /// <summary>
    /// Os estados de um lance (CLAUDE.md):
    /// Arremesso → BolaViva → BolaMorta → TrocaDePapel → FimDePartida.
    /// Por enquanto (Etapa 3) o ciclo é Arremesso → BolaViva → BolaMorta → Arremesso;
    /// TrocaDePapel e FimDePartida entram nas Etapas 5 e 6.
    /// </summary>
    public enum EstadoLance { Arremesso, BolaViva, BolaMorta, TrocaDePapel, FimDePartida }

    /// <summary>
    /// Máquina de estados do lance. Cuida de quem está com a bola, de quando o lance
    /// acaba e de recomeçar, e mostra na tela as instruções e o resultado.
    /// </summary>
    public class ControladorLance : MonoBehaviour
    {
        [SerializeField] private ConfigFisica configFisica;
        [SerializeField] private ConfigPartida configPartida;
        [SerializeField] private ConfigHUD configHUD;
        [SerializeField] private Bola bola;
        [SerializeField] private Arremessador arremessador;
        [Tooltip("Garrafa que o arremessador mira.")]
        [SerializeField] private Garrafa garrafaAlvo;
        [Tooltip("Garrafa da base do arremessador: com a bola, ele não passa à frente dela.")]
        [SerializeField] private Garrafa garrafaDoArremessador;
        [SerializeField] private Garrafa[] todasGarrafas;

        private float tempoNoEstado;
        private string mensagem = "";
        private float tempoMensagem = float.MaxValue;
        private string resumoArremesso = "";

        public EstadoLance Estado { get; private set; }

        private void Start()
        {
            arremessador.Arremessou += AoArremessar;
            EntrarArremesso();
        }

        private void OnDestroy()
        {
            if (arremessador != null) arremessador.Arremessou -= AoArremessar;
        }

        private void Update()
        {
            tempoNoEstado += Time.deltaTime;
            tempoMensagem += Time.deltaTime;

            switch (Estado)
            {
                case EstadoLance.Arremesso:
                    // A bola acompanha a mão do arremessador
                    bola.Colocar(arremessador.PosicaoMao);
                    break;

                case EstadoLance.BolaViva:
                    if (bola.Parada || tempoNoEstado > configPartida.tempoMaximoBolaViva) EntrarBolaMorta();
                    break;

                case EstadoLance.BolaMorta:
                    if (tempoNoEstado > configPartida.tempoBolaMorta)
                    {
                        foreach (Garrafa garrafa in todasGarrafas) garrafa.Reiniciar();
                        EntrarArremesso();
                    }
                    break;
            }
        }

        private void MudarPara(EstadoLance novo)
        {
            Estado = novo;
            tempoNoEstado = 0f;
        }

        private void EntrarArremesso()
        {
            MudarPara(EstadoLance.Arremesso);
            arremessador.Alvo = garrafaAlvo;
            arremessador.GarrafaPropria = garrafaDoArremessador;
            arremessador.ComBola = true;
            bola.Colocar(arremessador.PosicaoMao);
        }

        private void AoArremessar(Vector3 saida, Vector3 velocidade, TipoArremesso tipo, float kmh)
        {
            if (Estado != EstadoLance.Arremesso) return;
            MudarPara(EstadoLance.BolaViva);
            bola.Lancar(saida, velocidade);
            resumoArremesso = $"{NomeDoTipo(tipo)} · {kmh:0} km/h";
            Mostrar(resumoArremesso);
        }

        private void EntrarBolaMorta()
        {
            MudarPara(EstadoLance.BolaMorta);
            string resultado;
            if (garrafaAlvo.Derrubada) resultado = "DERRUBOU!";
            else if (garrafaAlvo.UltimaBoladaKmh > 0f) resultado = $"Quase! Balançou a {garrafaAlvo.UltimaBoladaKmh:0} km/h";
            else resultado = "Errou a garrafa";
            Mostrar($"{resumoArremesso}  —  {resultado}");
        }

        private void Mostrar(string texto)
        {
            mensagem = texto;
            tempoMensagem = 0f;
        }

        private static string NomeDoTipo(TipoArremesso tipo)
        {
            switch (tipo)
            {
                case TipoArremesso.Quicado: return "Quicado";
                case TipoArremesso.Pingo: return "Pingo";
                default: return "Forte";
            }
        }

        private void OnGUI()
        {
            GUIStyle estilo = new GUIStyle(GUI.skin.label) { fontSize = 14 };
            estilo.normal.textColor = Color.white;

            string instrucoes;
            if (Estado == EstadoLance.Arremesso && arremessador.Jogador.Controlador == null)
                instrucoes = "ARREMESSO: ninguém está no arremessador.\nAperte Select (controle) ou Tab (teclado) para trocar de dupla.";
            else if (Estado == EstadoLance.Arremesso)
                instrucoes = "ARREMESSO — segure e solte na faixa verde do arco\n" +
                             "R2 (RT) / clique esq.: forte    R1 (RB) / clique dir.: quicado    L2 (LT) / E: pingo\n" +
                             "Andar: analógico esquerdo / WASD (não passa da sua garrafa)    Mirar: analógico direito / mouse";
            else
                instrucoes = Estado == EstadoLance.BolaViva ? "Bola viva..." : "Bola morta: voltando para o arremessador";
            GUI.Label(new Rect(16, 8, 800, 80), instrucoes, estilo);

            // Mensagem do lance no alto, no meio, por alguns segundos
            if (tempoMensagem < configHUD.duracaoMensagem && !string.IsNullOrEmpty(mensagem))
            {
                GUIStyle grande = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 26, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold
                };
                grande.normal.textColor = Color.white;
                GUI.Label(new Rect(0, 70, Screen.width, 40), mensagem, grande);
            }
        }
    }
}
