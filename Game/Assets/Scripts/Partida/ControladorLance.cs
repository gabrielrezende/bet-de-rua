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
        [Tooltip("Rebatedor que recebe o arremesso (o da garrafa alvo).")]
        [SerializeField] private Rebatedor rebatedor;
        [SerializeField] private GerenciadorJogadores gerenciador;

        private float tempoNoEstado;
        private string mensagem = "";
        private float tempoMensagem = float.MaxValue;
        private string resumoArremesso = "";
        private string resumoBatida = "";
        private bool rebatida;

        public EstadoLance Estado { get; private set; }

        private void Start()
        {
            arremessador.Arremessou += AoArremessar;
            rebatedor.Rebateu += AoRebater;
            rebatedor.Errou += AoErrarBatida;
            EntrarArremesso();
        }

        private void OnDestroy()
        {
            if (arremessador != null) arremessador.Arremessou -= AoArremessar;
            if (rebatedor != null)
            {
                rebatedor.Rebateu -= AoRebater;
                rebatedor.Errou -= AoErrarBatida;
            }
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
            rebatida = false;
            resumoBatida = "";
            rebatedor.PrepararParaArremesso();
            // Quem joga com a dupla do taco passa para o rebatedor que vai receber (GDD)
            gerenciador.Ativar(rebatedor.Jogador);
        }

        private void AoArremessar(Vector3 saida, Vector3 velocidade, TipoArremesso tipo, float kmh)
        {
            if (Estado != EstadoLance.Arremesso) return;
            MudarPara(EstadoLance.BolaViva);
            bola.Lancar(saida, velocidade);
            rebatedor.ArremessoSaiu(velocidade);
            resumoArremesso = $"{NomeDoTipo(tipo)} · {kmh:0} km/h";
            Mostrar(resumoArremesso);
        }

        private void AoRebater(TipoBatida tipo, float kmh, float erroMs, bool perfeita)
        {
            rebatida = true;
            rebatedor.Encerrar();
            string qualidade = perfeita ? "PERFEITA!" : "Boa";
            resumoBatida = $"{qualidade} {NomeDaBatida(tipo)} · {kmh:0} km/h  ({TextoErro(erroMs)})";
            Mostrar(resumoBatida);
        }

        private void AoErrarBatida(string motivo, float erroMs)
        {
            // Errou no tempo: o próprio número já diz se adiantou ou atrasou
            bool erroDeTempo = motivo == "Atrasou" || motivo == "Adiantou";
            if (float.IsNaN(erroMs)) resumoBatida = $"Errou: {motivo.ToLower()}";
            else if (erroDeTempo) resumoBatida = $"Errou: {TextoErro(erroMs)}";
            else resumoBatida = $"Errou: {motivo.ToLower()} ({TextoErro(erroMs)})";
            Mostrar(resumoBatida);
        }

        /// <summary>"atrasou 40 ms" / "adiantou 25 ms" / "no tempo".</summary>
        private static string TextoErro(float erroMs)
        {
            float absoluto = Mathf.Abs(erroMs);
            if (absoluto < 1f) return "no tempo, 0 ms";
            return erroMs > 0f ? $"atrasou {absoluto:0} ms" : $"adiantou {absoluto:0} ms";
        }

        private static string NomeDaBatida(TipoBatida tipo)
        {
            switch (tipo)
            {
                case TipoBatida.Toque: return "Toque";
                case TipoBatida.Cavadinha: return "Cavadinha";
                default: return "Forte";
            }
        }

        private void EntrarBolaMorta()
        {
            MudarPara(EstadoLance.BolaMorta);
            rebatedor.Encerrar();
            string resultado;
            if (rebatida) resultado = resumoBatida;
            else if (garrafaAlvo.Derrubada) resultado = "DERRUBOU!";
            else if (garrafaAlvo.UltimaBoladaKmh > 0f) resultado = $"Quase! Balançou a {garrafaAlvo.UltimaBoladaKmh:0} km/h";
            else resultado = "Errou a garrafa";
            if (!rebatida && resumoBatida != "") resultado += $"   ·   {resumoBatida}";
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

            string instrucoes = "";
            bool arremessadorHumano = arremessador.Jogador.Controlador != null;
            bool rebatedorHumano = rebatedor.Jogador.Controlador != null;

            if (Estado == EstadoLance.Arremesso)
            {
                if (arremessadorHumano)
                    instrucoes += "ARREMESSO — segure e solte na faixa verde: R2 / clique esq. forte · R1 / clique dir. quicado · L2 / E pingo\n" +
                                  "   andar: analógico esquerdo / WASD (não passa da sua garrafa) · mirar: analógico direito / mouse\n";
                else
                    instrucoes += configPartida.arremessadorAutomatico
                        ? "ARREMESSO: a CPU arremessa sozinha (Select / Tab troca de dupla)\n"
                        : "ARREMESSO: ninguém no arremessador (Select / Tab troca de dupla)\n";
            }
            if ((Estado == EstadoLance.Arremesso || Estado == EstadoLance.BolaViva) && rebatedor.Ativo)
            {
                instrucoes += rebatedorHumano
                    ? "BATIDA — R2 / clique esq. forte (segura e solta) · R1 / clique dir. toque · L2 / E cavadinha\n" +
                      "   direção: analógico direito / mouse · bata quando o anel amarelo fechar na bola\n"
                    : configPartida.rebatedorAutomatico ? "BATIDA: a CPU rebate\n" : "";
            }
            if (Estado == EstadoLance.BolaViva && !rebatedor.Ativo) instrucoes += "Bola viva...";
            if (Estado == EstadoLance.BolaMorta) instrucoes += "Bola morta: voltando para o arremessador";
            GUI.Label(new Rect(16, 8, 900, 100), instrucoes, estilo);

            // Mensagem do lance no alto, no meio, por alguns segundos
            if (tempoMensagem < configHUD.duracaoMensagem && !string.IsNullOrEmpty(mensagem))
            {
                GUIStyle grande = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 26, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold
                };
                grande.normal.textColor = Color.white;
                GUI.Label(new Rect(0, 115, Screen.width, 40), mensagem, grande);
            }
        }
    }
}
