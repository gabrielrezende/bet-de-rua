using System.Collections.Generic;
using UnityEngine;

namespace BetsDeRua
{
    /// <summary>
    /// Os estados de um lance (CLAUDE.md):
    /// Arremesso → BolaViva → BolaMorta → TrocaDePapel → FimDePartida.
    /// FimDePartida entra na Etapa 6.
    /// </summary>
    public enum EstadoLance { Arremesso, BolaViva, BolaMorta, TrocaDePapel, FimDePartida }

    /// <summary>
    /// Máquina de estados do lance e as regras do bets:
    /// - Arremesso: a bola está na mão do arremessador; o rebatedor da garrafa alvo espera.
    /// - BolaViva: depois do arremesso. A defesa pega a bola (□), chuta na garrafa, passa;
    ///   o ataque corre e cruza (Corrida).
    /// - Queimada: a bola lançada pela defesa derruba a garrafa com o taco do dono fora do
    ///   chão → TrocaDePapel. Com o taco no chão, "não valeu" → BolaMorta.
    /// - Pegou no ar a bola rebatida → TrocaDePapel.
    /// - A defesa com a bola e os dois rebatedores seguros → BolaMorta → novo arremesso.
    /// Também guarda um placar simples por lado (o placar completo é a Etapa 6).
    /// </summary>
    public class ControladorLance : MonoBehaviour
    {
        [SerializeField] private ConfigFisica configFisica;
        [SerializeField] private ConfigPartida configPartida;
        [SerializeField] private ConfigHUD configHUD;
        [SerializeField] private Bola bola;
        [Tooltip("Quem arremessa no começo de cada lance.")]
        [SerializeField] private Arremessador arremessador;
        [Tooltip("Os dois defensores (arremessador e guardião).")]
        [SerializeField] private Defensor[] defensores;
        [SerializeField] private Garrafa garrafaA;
        [SerializeField] private Garrafa garrafaB;
        [Tooltip("Os dois rebatedores. Quem recebe o arremesso é o que está na base B.")]
        [SerializeField] private Rebatedor[] rebatedores;
        [SerializeField] private Corrida corrida;
        [SerializeField] private GerenciadorJogadores gerenciador;

        private float tempoNoEstado;
        private string mensagem = "";
        private float tempoMensagem = float.MaxValue;
        private string resumoArremesso = "";
        private string resumoBatida = "";
        private bool rebatida;
        private float tempoSeguro;
        private Arremessador portador;          // defensor com a bola na mão no campo (null = bola solta)
        private Rebatedor rebatedorDaVez;       // quem recebe o arremesso
        private readonly HashSet<Garrafa> garrafasAvaliadas = new HashSet<Garrafa>();
        private readonly int[] pontos = new int[2];
        private int ladoNoTaco;                 // 0 = Lado 1, 1 = Lado 2

        public EstadoLance Estado { get; private set; }
        public bool Rebatida => rebatida;
        public Arremessador Portador => portador;
        public Rebatedor RebatedorDaVez => rebatedorDaVez;
        public Garrafa GarrafaA => garrafaA;
        public Garrafa GarrafaB => garrafaB;

        private Garrafa[] Garrafas => new[] { garrafaA, garrafaB };

        private void Start()
        {
            foreach (Defensor defensor in defensores)
            {
                defensor.Pegou += AoPegar;
                if (defensor.ComBola != null) defensor.ComBola.Arremessou += AoArremessar;
            }
            foreach (Rebatedor rebatedor in rebatedores)
            {
                rebatedor.Rebateu += AoRebater;
                rebatedor.Errou += AoErrarBatida;
            }
            corrida.Cruzou += AoCruzar;
            corrida.PontosValidados += AoValidarPontos;
            ProximoLance();
        }

        private void OnDestroy()
        {
            foreach (Defensor defensor in defensores)
            {
                if (defensor == null) continue;
                defensor.Pegou -= AoPegar;
                if (defensor.ComBola != null) defensor.ComBola.Arremessou -= AoArremessar;
            }
            foreach (Rebatedor rebatedor in rebatedores)
            {
                if (rebatedor == null) continue;
                rebatedor.Rebateu -= AoRebater;
                rebatedor.Errou -= AoErrarBatida;
            }
            if (corrida != null)
            {
                corrida.Cruzou -= AoCruzar;
                corrida.PontosValidados -= AoValidarPontos;
            }
        }

        // ------------------------------------------------------------------ ciclo

        private void Update()
        {
            tempoNoEstado += Time.deltaTime;
            tempoMensagem += Time.deltaTime;

            switch (Estado)
            {
                case EstadoLance.Arremesso:
                    // A bola acompanha a mão do arremessador
                    bola.Colocar(arremessador.PosicaoMao);
                    LiberarDefensores(false);
                    break;

                case EstadoLance.BolaViva:
                    AtualizarBolaViva();
                    break;

                case EstadoLance.BolaMorta:
                    if (tempoNoEstado > configPartida.tempoBolaMorta) ProximoLance();
                    break;

                case EstadoLance.TrocaDePapel:
                    if (tempoNoEstado > configPartida.tempoTrocaDePapel)
                    {
                        gerenciador.InverterDuplas();
                        ladoNoTaco = 1 - ladoNoTaco;
                        ProximoLance();
                    }
                    break;
            }
            PintarGarrafas();
        }

        private void AtualizarBolaViva()
        {
            if (portador != null)
            {
                bola.Colocar(portador.PosicaoMao);
                portador.Alvo = EscolherAlvo(portador.transform.position);
                portador.Parceiro = OutroDefensor(portador.Jogador);
            }
            LiberarDefensores(portador == null);

            VerificarGarrafas();
            if (Estado != EstadoLance.BolaViva) return;

            // Defesa com a bola e os dois rebatedores seguros por um tempinho: fim do lance
            tempoSeguro = portador != null && corrida.AmbosSeguros ? tempoSeguro + Time.deltaTime : 0f;
            if (tempoSeguro >= configPartida.tempoSeguroParaEncerrar) EntrarBolaMorta(null);
            else if (tempoNoEstado > configPartida.tempoMaximoBolaViva) EntrarBolaMorta("Tempo esgotado");
        }

        private void MudarPara(EstadoLance novo)
        {
            Estado = novo;
            tempoNoEstado = 0f;
            tempoSeguro = 0f;
        }

        /// <summary>Arruma o campo e começa um arremesso novo.</summary>
        private void ProximoLance()
        {
            foreach (Garrafa garrafa in Garrafas) garrafa.Reiniciar();
            garrafasAvaliadas.Clear();
            foreach (Defensor defensor in defensores) defensor.ComBola?.TirarBola();
            portador = null;
            corrida.Encerrar();
            ArrumarPosicoes();
            EntrarArremesso();
        }

        private void EntrarArremesso()
        {
            MudarPara(EstadoLance.Arremesso);
            rebatida = false;
            resumoBatida = "";

            // Recebe o arremesso o rebatedor que está na base B
            rebatedorDaVez = MaisPerto(rebatedores, garrafaB.transform.position);
            foreach (Rebatedor rebatedor in rebatedores)
            {
                if (rebatedor == rebatedorDaVez) rebatedor.PrepararParaArremesso();
                else rebatedor.Encerrar();
            }

            arremessador.Alvo = garrafaB;
            arremessador.GarrafaPropria = garrafaA;
            arremessador.ReceberBola(ModoComBola.Arremesso);
            bola.Colocar(arremessador.PosicaoMao);
            // Quem joga com a dupla do taco passa para o rebatedor que vai receber (GDD)
            gerenciador.Ativar(rebatedorDaVez.Jogador);
        }

        private void EntrarBolaMorta(string motivo)
        {
            MudarPara(EstadoLance.BolaMorta);
            EncerrarJogadas();
            if (motivo != null) Mostrar(motivo);
        }

        private void EntrarTrocaDePapel(string motivo)
        {
            MudarPara(EstadoLance.TrocaDePapel);
            EncerrarJogadas();
            bola.Colocar(bola.Posicao); // a bola para onde está
            Mostrar($"{motivo}  —  troca de papel!");
        }

        private void EncerrarJogadas()
        {
            corrida.Encerrar();
            foreach (Rebatedor rebatedor in rebatedores) rebatedor.Encerrar();
            LiberarDefensores(false);
        }

        // ------------------------------------------------------------------ regras

        /// <summary>
        /// A garrafa está protegida quando o dono dela (o rebatedor mais perto) está dentro
        /// do círculo dessa base com o taco no chão.
        /// </summary>
        public bool Protegida(Garrafa garrafa)
        {
            Rebatedor dono = MaisPerto(rebatedores, garrafa.transform.position);
            Taco taco = dono.GetComponentInChildren<Taco>();
            return dono.Jogador.DentroDoCirculoDe(garrafa.transform.position) && taco != null && taco.NoChao;
        }

        private void VerificarGarrafas()
        {
            foreach (Garrafa garrafa in Garrafas)
            {
                if (!garrafa.Derrubada || garrafasAvaliadas.Contains(garrafa)) continue;
                garrafasAvaliadas.Add(garrafa);

                Jogador lancador = bola.UltimoLancador;
                if (lancador == null || lancador.Dupla != Dupla.Bola)
                {
                    // Derrubada pela batida do próprio ataque: não conta, a garrafa volta
                    garrafa.Reiniciar();
                    garrafasAvaliadas.Remove(garrafa);
                    continue;
                }
                if (Protegida(garrafa))
                {
                    EntrarBolaMorta("Não valeu: taco no chão!");
                    return;
                }
                EntrarTrocaDePapel("QUEIMOU!");
                return;
            }
        }

        /// <summary>A garrafa exposta mais perto (se nenhuma estiver exposta, a mais perto).</summary>
        public Garrafa EscolherAlvo(Vector3 de)
        {
            foreach (bool soExpostas in new[] { true, false })
            {
                Garrafa melhor = null;
                float melhorDistancia = float.MaxValue;
                foreach (Garrafa garrafa in Garrafas)
                {
                    if (garrafa.Derrubada || (soExpostas && Protegida(garrafa))) continue;
                    float d = Vector3.Distance(de, garrafa.transform.position);
                    if (d < melhorDistancia) { melhor = garrafa; melhorDistancia = d; }
                }
                if (melhor != null) return melhor;
            }
            return garrafaB;
        }

        /// <summary>True se alguma garrafa em pé está exposta agora.</summary>
        public bool AlgumaExposta()
        {
            foreach (Garrafa garrafa in Garrafas)
                if (!garrafa.Derrubada && !Protegida(garrafa)) return true;
            return false;
        }

        private void PintarGarrafas()
        {
            bool jogoRolando = Estado == EstadoLance.Arremesso || Estado == EstadoLance.BolaViva;
            foreach (Garrafa garrafa in Garrafas)
                garrafa.MarcarExposta(jogoRolando && !garrafa.Derrubada && !Protegida(garrafa), configHUD.corGarrafaExposta);
        }

        private void LiberarDefensores(bool bolaLivre)
        {
            foreach (Defensor defensor in defensores) defensor.BolaLivre = bolaLivre && Estado == EstadoLance.BolaViva;
        }

        // ------------------------------------------------------------------ eventos

        private void AoArremessar(Arremessador origem, Vector3 saida, Vector3 velocidade, TipoArremesso tipo, float kmh)
        {
            if (Estado == EstadoLance.Arremesso && origem == arremessador && origem.Modo == ModoComBola.Arremesso)
            {
                MudarPara(EstadoLance.BolaViva);
                bola.Lancar(saida, velocidade, origem.Jogador);
                rebatedorDaVez.ArremessoSaiu(velocidade);
                resumoArremesso = $"{NomeDoTipo(tipo)} · {kmh:0} km/h";
                Mostrar(resumoArremesso);
                return;
            }
            if (Estado == EstadoLance.BolaViva && origem == portador)
            {
                portador = null;
                bola.Lancar(saida, velocidade, origem.Jogador);
                Mostrar(tipo == TipoArremesso.Forte ? $"Chute na garrafa · {kmh:0} km/h" : NomeDoTipo(tipo));
            }
        }

        private void AoRebater(TipoBatida tipo, float kmh, float erroMs, bool perfeita)
        {
            rebatida = true;
            rebatedorDaVez.Encerrar();
            corrida.Ativar();
            string qualidade = perfeita ? "PERFEITA!" : "Boa";
            resumoBatida = $"{qualidade} {NomeDaBatida(tipo)} · {kmh:0} km/h  ({TextoErro(erroMs)})";
            Mostrar(resumoBatida);
            // Na defesa, quem joga passa para o defensor mais perto da bola (GDD)
            gerenciador.Ativar(MaisPerto(defensores, bola.Posicao).Jogador);
        }

        private void AoErrarBatida(string motivo, float erroMs)
        {
            bool erroDeTempo = motivo == "Atrasou" || motivo == "Adiantou";
            if (float.IsNaN(erroMs)) resumoBatida = $"Errou: {motivo.ToLower()}";
            else if (erroDeTempo) resumoBatida = $"Errou: {TextoErro(erroMs)}";
            else resumoBatida = $"Errou: {motivo.ToLower()} ({TextoErro(erroMs)})";
            Mostrar(resumoBatida);
        }

        private void AoPegar(Defensor defensor, bool noAr)
        {
            if (Estado != EstadoLance.BolaViva || portador != null || defensor.ComBola == null) return;

            Jogador lancador = bola.UltimoLancador;
            portador = defensor.ComBola;
            portador.Alvo = EscolherAlvo(portador.transform.position);
            portador.Parceiro = OutroDefensor(defensor.Jogador);
            portador.ReceberBola(ModoComBola.Campo);
            bola.Colocar(portador.PosicaoMao);
            // Pegou a bola: acabou a chance de rebater este arremesso
            rebatedorDaVez.Encerrar();
            gerenciador.Ativar(defensor.Jogador);

            if (noAr && lancador != null && lancador.Dupla == Dupla.Taco)
            {
                EntrarTrocaDePapel("PEGOU NO AR!");
                return;
            }
            // Passe do colega não precisa de aviso
            if (lancador == null || lancador.Dupla == Dupla.Taco) Mostrar($"{defensor.Jogador.Nome} dominou a bola");
        }

        private void AoCruzar(int pendentes)
        {
            Mostrar($"CRUZOU!  +{pendentes} pendente (pare no círculo com o taco no chão)");
        }

        private void AoValidarPontos(int quantos)
        {
            pontos[ladoNoTaco] += quantos;
            Mostrar(quantos == 1 ? "PONTO!" : $"+{quantos} PONTOS!");
        }

        // ------------------------------------------------------------------ campo

        /// <summary>Põe cada um no seu lugar para o próximo arremesso.</summary>
        private void ArrumarPosicoes()
        {
            float meio = configFisica.distanciaBases * 0.5f;
            Vector3 baseA = new Vector3(-meio, 0f, 0f);
            Vector3 baseB = new Vector3(meio, 0f, 0f);
            Vector3 lado = Vector3.forward * configPartida.afastamentoRebatedor;

            // Cada rebatedor volta para a base em que está (depois de cruzar, eles trocaram de lado)
            Jogador r0 = rebatedores[0].Jogador, r1 = rebatedores[1].Jogador;
            float jeitoAtual = Distancia(r0, baseA) + Distancia(r1, baseB);
            float jeitoTrocado = Distancia(r0, baseB) + Distancia(r1, baseA);
            Jogador naA = jeitoAtual <= jeitoTrocado ? r0 : r1;
            Jogador naB = naA == r0 ? r1 : r0;
            naA.Teleportar(baseA + lado, Vector3.right);
            naB.Teleportar(baseB + lado, Vector3.left);

            // Arremessador atrás da garrafa A, guardião atrás da garrafa B
            Jogador guardiao = OutroDefensor(arremessador.Jogador);
            arremessador.Jogador.Teleportar(baseA + Vector3.left * configPartida.afastamentoArremessador, Vector3.right);
            if (guardiao != null)
                guardiao.Teleportar(baseB + Vector3.right * configPartida.distanciaGuardiao, Vector3.left);
        }

        private static float Distancia(Jogador jogador, Vector3 ponto)
        {
            Vector3 d = jogador.transform.position - ponto;
            d.y = 0f;
            return d.magnitude;
        }

        private Jogador OutroDefensor(Jogador jogador)
        {
            foreach (Defensor defensor in defensores)
                if (defensor.Jogador != jogador) return defensor.Jogador;
            return null;
        }

        private static T MaisPerto<T>(T[] lista, Vector3 ponto) where T : Component
        {
            T melhor = null;
            float melhorDistancia = float.MaxValue;
            foreach (T item in lista)
            {
                Vector3 d = item.transform.position - ponto;
                d.y = 0f;
                if (d.sqrMagnitude < melhorDistancia) { melhor = item; melhorDistancia = d.sqrMagnitude; }
            }
            return melhor;
        }

        // ------------------------------------------------------------------ textos e tela

        private void Mostrar(string texto)
        {
            mensagem = texto;
            tempoMensagem = 0f;
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

        private static string NomeDoTipo(TipoArremesso tipo)
        {
            switch (tipo)
            {
                case TipoArremesso.Quicado: return "Quicado";
                case TipoArremesso.Pingo: return "Pingo";
                case TipoArremesso.Passe: return "Passe";
                case TipoArremesso.Lancamento: return "Lançamento";
                default: return "Forte";
            }
        }

        private void OnGUI()
        {
            GUIStyle estilo = new GUIStyle(GUI.skin.label) { fontSize = 14 };
            estilo.normal.textColor = Color.white;

            string instrucoes = "";
            bool tacoHumano = gerenciador.TemHumanoNaDupla(Dupla.Taco);
            bool bolaHumano = gerenciador.TemHumanoNaDupla(Dupla.Bola);

            if (Estado == EstadoLance.Arremesso)
            {
                instrucoes += bolaHumano
                    ? "ARREMESSO: R2 / clique esq. forte · R1 / clique dir. quicado · L2 / E pingo (solte na faixa verde)\n"
                    : "ARREMESSO: a CPU arremessa (Select / Tab troca de dupla)\n";
                if (tacoHumano)
                    instrucoes += "REBATEDOR: segure L1 / Q para o taco ficar no chão (garrafa vermelha = pode ser queimada)\n" +
                                  "   batida: R2 forte · R1 toque · L2 cavadinha · direção: analógico direito / mouse\n";
            }
            else if (Estado == EstadoLance.BolaViva)
            {
                if (rebatedorDaVez != null && rebatedorDaVez.Ativo && tacoHumano)
                    instrucoes += "BATIDA: R2 forte · R1 toque · L2 cavadinha — bata quando o anel fechar\n";
                if (tacoHumano && rebatida)
                    instrucoes += "CORRIDA: corra (R2 sprint) · no meio, X (A) / Espaço cruza · pare no círculo segurando L1 / Q\n";
                if (bolaHumano)
                    instrucoes += portador != null && portador.Jogador.Controlador != null
                        ? "COM A BOLA: R2 chuta na garrafa · R1 passe · L2 lançamento · mira: analógico direito\n"
                        : "DEFESA: □ (X) / R pega a bola · longe dela, mergulha / carrinho · L1 / Q troca de jogador\n";
            }
            else if (Estado == EstadoLance.BolaMorta) instrucoes += "Bola morta: novo arremesso";
            else if (Estado == EstadoLance.TrocaDePapel) instrucoes += "Troca de papel: quem defendia vai para o taco";
            GUI.Label(new Rect(16, 8, 1000, 100), instrucoes, estilo);

            // Placar simples (o completo é a Etapa 6)
            GUIStyle placar = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18, alignment = TextAnchor.UpperCenter, fontStyle = FontStyle.Bold
            };
            placar.normal.textColor = Color.white;
            string pendente = corrida.PontosPendentes > 0 ? $"   (+{corrida.PontosPendentes} pendente)" : "";
            GUI.Label(new Rect(0, Screen.height - 34, Screen.width, 30),
                      $"LADO 1: {pontos[0]}   ×   LADO 2: {pontos[1]}      no taco: Lado {ladoNoTaco + 1}{pendente}", placar);

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
