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
    ///   chão → TrocaDePapel. Com o taco no chão, "queimou errado": a garrafa volta, o jogo segue e o ataque ganha ponto.
    /// - Pegou no ar a bola rebatida → TrocaDePapel (só com ConfigPartida.pegarNoArPerdeTaco; desligado no padrão).
    /// - Lancha: o arremesso bateu no corpo de um rebatedor; a bola segue. 3 lanchas → TrocaDePapel.
    /// - Sem bola morta (regra da rua): quem está com a bola volta para trás de uma casinha e,
    ///   com os dois rebatedores seguros, já arremessa dali (Arremesso de novo, sem parar o jogo).
    ///   Rodízio natural: quem pegou a bola arremessa da casinha mais perto.
    /// - BolaMorta só sobra como segurança (tempo esgotado).
    /// - Partida: chegou na meta = match point; mais uma cruzada validada fecha o jogo
    ///   (FimDePartida). Perdeu o taco em match point: os pontos caem pela metade.
    ///   Start (controle) ou Enter (teclado) reinicia a partida.
    /// </summary>
    public class ControladorLance : MonoBehaviour
    {
        [SerializeField] private ConfigFisica configFisica;
        [SerializeField] private ConfigPartida configPartida;
        [SerializeField] private ConfigHUD configHUD;
        [SerializeField] private Bola bola;
        [Tooltip("Quem arremessa no primeiro lance (depois, os defensores se revezam).")]
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
        private int vencedor = -1;              // lado que fechou o jogo (-1 = ninguém ainda)
        private Arremessador arremessadorDaVez; // quem arremessa neste lance
        private bool arremessoDeAParaB = true;  // de que casinha sai o arremesso neste lance
        private bool primeiroLance = true;
        private int lanchas;                    // lanchas da dupla que está no taco agora
        private float tempoSemBolaNoArremesso;

        public EstadoLance Estado { get; private set; }
        public bool Rebatida => rebatida;
        public Arremessador Portador => portador;
        public Rebatedor RebatedorDaVez => rebatedorDaVez;
        public Arremessador ArremessadorDaVez => arremessadorDaVez;
        public Garrafa GarrafaA => garrafaA;
        public Garrafa GarrafaB => garrafaB;
        /// <summary>Garrafa que recebe o arremesso neste lance.</summary>
        public Garrafa GarrafaAlvo => arremessoDeAParaB ? garrafaB : garrafaA;
        /// <summary>Garrafa da casinha de onde sai o arremesso neste lance.</summary>
        public Garrafa GarrafaDeOrigem => arremessoDeAParaB ? garrafaA : garrafaB;
        public bool EmMatchPoint(int lado) => pontos[lado] >= configPartida.metaPontos;

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
            bola.TocouNoCorpo += AoLancha;
            GuardarCoresDosTimes();
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
            if (bola != null) bola.TocouNoCorpo -= AoLancha;
        }

        // ------------------------------------------------------------------ ciclo

        private void Update()
        {
            tempoNoEstado += Time.deltaTime;
            tempoMensagem += Time.deltaTime;
            if (ApertouReiniciar()) { ReiniciarPartida(); return; }

            switch (Estado)
            {
                case EstadoLance.Arremesso:
                    // A bola acompanha a mão do arremessador
                    bola.Colocar(arremessadorDaVez.PosicaoMao);
                    LiberarDefensores(false);
                    // Segurança: o arremessador ficou sem a bola (arremesso ignorado) e o jogo travaria
                    tempoSemBolaNoArremesso = arremessadorDaVez.ComBola ? 0f : tempoSemBolaNoArremesso + Time.deltaTime;
                    if (tempoSemBolaNoArremesso > 1f)
                    {
                        Debug.LogWarning($"[Lance] {arremessadorDaVez.Jogador.Nome} estava sem bola no arremesso; devolvendo.");
                        tempoSemBolaNoArremesso = 0f;
                        arremessadorDaVez.ReceberBola(ModoComBola.Arremesso);
                    }
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
                        lanchas = 0;
                        PintarTimes();
                        ProximoLance();
                    }
                    break;

                case EstadoLance.FimDePartida:
                    // Fica parado mostrando o vencedor até alguém apertar Start / Enter
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
            ControlarCurva();

            VerificarGarrafas();
            if (Estado != EstadoLance.BolaViva) return;

            // Sem bola morta (regra da rua): o lance segue até quem está com a bola voltar para
            // trás de uma casinha com os dois rebatedores seguros; aí ele já arremessa dali.
            // Enquanto ele não chega, o ataque pode continuar cruzando.
            bool prontoParaArremessar = portador != null && AtrasDeUmaCasinha(portador.transform.position) && corrida.AmbosSeguros;
            tempoSeguro = prontoParaArremessar ? tempoSeguro + Time.deltaTime : 0f;
            if (tempoSeguro >= configPartida.tempoSeguroParaEncerrar) ArremessarDeNovoSemParar();
            else if (tempoNoEstado > configPartida.tempoMaximoBolaViva) EntrarBolaMorta("Tempo esgotado");
        }

        /// <summary>
        /// Curva do arremesso: depois de soltar, enquanto ninguém rebateu ou pegou, o analógico
        /// direito de quem arremessou empurra a bola de lado (para cima ou para baixo da tela,
        /// já que o arremesso corre de lado na câmera).
        /// </summary>
        private void ControlarCurva()
        {
            ControleHumano humano = arremessadorDaVez != null ? arremessadorDaVez.Jogador.Controlador : null;
            bool podeCurvar = !rebatida && portador == null && humano != null && humano.Conectado
                              && bola.UltimoLancador == arremessadorDaVez.Jogador;
            if (!podeCurvar) { bola.DefinirCurva(Vector3.zero, 0f); return; }

            Vector3 rumo = bola.Velocidade;
            rumo.y = 0f;
            Vector3 eixoLateral = Vector3.Cross(Vector3.up, rumo);
            Vector2 analogico = humano.MiraAnalogica;
            Vector3 empurrao = new Vector3(analogico.x, 0f, analogico.y);
            float intensidade = eixoLateral.sqrMagnitude > 0.0001f ? Vector3.Dot(empurrao, eixoLateral.normalized) : 0f;
            bola.DefinirCurva(eixoLateral, intensidade);
        }

        /// <summary>
        /// Quem está com a bola atrás de uma casinha vira o arremessador dali, sem parar o jogo
        /// e sem mudar ninguém de lugar. Arremessa na casinha do outro lado.
        /// </summary>
        private void ArremessarDeNovoSemParar()
        {
            Arremessador novo = portador;
            arremessoDeAParaB = CasinhaMaisPerto(novo.transform.position) == garrafaA;
            arremessadorDaVez = novo;
            portador = null;
            foreach (Garrafa garrafa in Garrafas) garrafa.Reiniciar();
            garrafasAvaliadas.Clear();
            corrida.Encerrar();
            EntrarArremesso();
            Mostrar($"{novo.Jogador.Nome} arremessa da casinha {(arremessoDeAParaB ? "A" : "B")}");
        }

        /// <summary>Garrafa da casinha mais perto de 'ponto' (pela distância no eixo das bases).</summary>
        public Garrafa CasinhaMaisPerto(Vector3 ponto)
        {
            return Mathf.Abs(ponto.x - garrafaA.transform.position.x) <= Mathf.Abs(ponto.x - garrafaB.transform.position.x)
                ? garrafaA : garrafaB;
        }

        /// <summary>
        /// True se 'ponto' está atrás da casinha mais perto (do lado de fora do campo), de onde
        /// se pode arremessar. Usa a mesma folga do arremesso para ir um pouco à frente da garrafa.
        /// </summary>
        public bool AtrasDeUmaCasinha(Vector3 ponto)
        {
            Vector3 garrafa = CasinhaMaisPerto(ponto).transform.position;
            float paraFora = -ZonaDoTaco.ParaOArremesso(garrafa);
            return (ponto.x - garrafa.x) * paraFora >= -configFisica.avancoMaximoComBola;
        }

        /// <summary>Onde um defensor com a bola vai para arremessar de novo: atrás da casinha mais perto.</summary>
        public Vector3 PontoParaArremessar(Vector3 de)
        {
            Vector3 garrafa = CasinhaMaisPerto(de).transform.position;
            float paraFora = -ZonaDoTaco.ParaOArremesso(garrafa);
            return new Vector3(garrafa.x + paraFora * configPartida.afastamentoArremessador, 0f, 0f);
        }

        private void MudarPara(EstadoLance novo)
        {
            if (novo != Estado) RegistroPartida.Evento($"estado: {Estado} -> {novo}");
            Estado = novo;
            tempoNoEstado = 0f;
            tempoSeguro = 0f;
        }

        /// <summary>Arruma o campo e começa um arremesso novo.</summary>
        private void ProximoLance()
        {
            // Rodízio (GDD): a cada lance a defesa arremessa da outra casinha;
            // quem estava guardando atrás da casinha alvo é quem arremessa agora
            if (primeiroLance)
            {
                primeiroLance = false;
                arremessadorDaVez = arremessador;
                arremessoDeAParaB = true;
            }
            else
            {
                arremessoDeAParaB = !arremessoDeAParaB;
                arremessadorDaVez = OutroDefensor(arremessadorDaVez.Jogador).GetComponent<Arremessador>();
            }

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

            // Recebe o arremesso o rebatedor que está na casinha alvo
            rebatedorDaVez = MaisPerto(rebatedores, GarrafaAlvo.transform.position);
            foreach (Rebatedor rebatedor in rebatedores)
            {
                if (rebatedor == rebatedorDaVez) rebatedor.PrepararParaArremesso();
                else rebatedor.Encerrar();
            }

            arremessadorDaVez.Alvo = GarrafaAlvo;
            arremessadorDaVez.GarrafaPropria = GarrafaDeOrigem;
            arremessadorDaVez.ReceberBola(ModoComBola.Arremesso);
            bola.Colocar(arremessadorDaVez.PosicaoMao);
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
            // Perdeu o taco em match point: os pontos caem pela metade (GDD)
            if (configPartida.caiMetadeAoPerderTacoNoMatchPoint && EmMatchPoint(ladoNoTaco))
            {
                pontos[ladoNoTaco] /= 2;
                motivo += $"  Perdeu no match point: caiu para {pontos[ladoNoTaco]}";
            }
            Mostrar($"{motivo}  —  troca de papel!");
        }

        private void EntrarFimDePartida(int lado)
        {
            vencedor = lado;
            MudarPara(EstadoLance.FimDePartida);
            EncerrarJogadas();
            foreach (Defensor defensor in defensores) defensor.ComBola?.TirarBola();
            portador = null;
            bola.Colocar(bola.Posicao);
            Mostrar($"LADO {lado + 1} FECHOU O JOGO!");
        }

        /// <summary>Start (qualquer controle) ou Enter (teclado).</summary>
        private static bool ApertouReiniciar()
        {
            foreach (UnityEngine.InputSystem.Gamepad controle in UnityEngine.InputSystem.Gamepad.all)
                if (controle.startButton.wasPressedThisFrame) return true;
            UnityEngine.InputSystem.Keyboard teclado = UnityEngine.InputSystem.Keyboard.current;
            return teclado != null && (teclado.enterKey.wasPressedThisFrame || teclado.numpadEnterKey.wasPressedThisFrame);
        }

        /// <summary>Zera o placar e começa de novo, com o Lado 1 no taco.</summary>
        private void ReiniciarPartida()
        {
            pontos[0] = pontos[1] = 0;
            vencedor = -1;
            // Quem joga volta para o lado em que começou (Lado 1 no taco)
            if (ladoNoTaco != 0) gerenciador.InverterDuplas();
            ladoNoTaco = 0;
            lanchas = 0;
            PintarTimes();
            primeiroLance = true;
            Mostrar("Partida nova!");
            ProximoLance();
        }

        private void EncerrarJogadas()
        {
            bola.CorposAtivos = false;
            corrida.Encerrar();
            foreach (Rebatedor rebatedor in rebatedores) rebatedor.Encerrar();
            LiberarDefensores(false);
        }

        // ------------------------------------------------------------------ cores dos times

        // Cor de cada time (Lado 1 e Lado 2): a do corpo e a do triângulo do jogador controlado.
        // A cor acompanha o time, não o boneco: na troca de papel, os bonecos trocam de cor.
        private readonly Material[] corCorpoDoLado = new Material[2];
        private readonly Material[] corMarcadorDoLado = new Material[2];

        /// <summary>No começo o Lado 1 está no taco: guarda as cores que os bonecos têm agora.</summary>
        private void GuardarCoresDosTimes()
        {
            Jogador rebatedor = rebatedores[0].Jogador;
            Jogador defensor = defensores[0].Jogador;
            corCorpoDoLado[0] = rebatedor.MaterialCorpo;
            corCorpoDoLado[1] = defensor.MaterialCorpo;
            corMarcadorDoLado[0] = rebatedor.GetComponent<MarcadorJogador>()?.MaterialTriangulo;
            corMarcadorDoLado[1] = defensor.GetComponent<MarcadorJogador>()?.MaterialTriangulo;
        }

        /// <summary>Pinta cada boneco com a cor do time que está naquele papel agora.</summary>
        private void PintarTimes()
        {
            foreach (Jogador jogador in gerenciador.Jogadores)
            {
                int lado = jogador.Dupla == Dupla.Taco ? ladoNoTaco : 1 - ladoNoTaco;
                if (corCorpoDoLado[lado] != null) jogador.PintarCorpo(corCorpoDoLado[lado]);
                MarcadorJogador marcador = jogador.GetComponent<MarcadorJogador>();
                if (marcador != null && corMarcadorDoLado[lado] != null) marcador.MaterialTriangulo = corMarcadorDoLado[lado];
            }
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
                    // Queimou errado: o taco do dono já estava no chão. A garrafa volta de pé,
                    // o jogo segue (sem bola morta) e o ataque ganha ponto (regra da rua)
                    garrafa.Reiniciar();
                    garrafasAvaliadas.Remove(garrafa);
                    int ganhou = configPartida.pontosQueimaErrada;
                    if (ganhou > 0 && !EmMatchPoint(ladoNoTaco))
                    {
                        // Para na meta: fechar o jogo continua exigindo uma cruzada
                        pontos[ladoNoTaco] = Mathf.Min(configPartida.metaPontos, pontos[ladoNoTaco] + ganhou);
                        Mostrar($"QUEIMOU ERRADO! Taco no chão: +{ganhou} para o Lado {ladoNoTaco + 1}");
                    }
                    else Mostrar("QUEIMOU ERRADO! Taco no chão");
                    continue;
                }
                EntrarTrocaDePapel("QUEIMOU!");
                return;
            }
        }

        /// <summary>
        /// Quanto tempo (s) o dono da garrafa leva para protegê-la: correr até a zona segura
        /// em sprint e o taco descer. Zero se já está protegida. Os bots usam para decidir se vale jogar.
        /// </summary>
        public float TempoParaProteger(Garrafa garrafa)
        {
            if (Protegida(garrafa)) return 0f;
            Rebatedor dono = MaisPerto(rebatedores, garrafa.transform.position);
            float falta = ZonaDoTaco.DistanciaAteZona(dono.transform.position, garrafa.transform.position, configFisica);
            return falta / Mathf.Max(0.1f, configFisica.velocidadeSprint) + configFisica.tempoMovimentoTaco;
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
            bool jogoRolando = configHUD.pintarGarrafaExposta
                               && (Estado == EstadoLance.Arremesso || Estado == EstadoLance.BolaViva);
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
            if (Estado == EstadoLance.Arremesso && origem == arremessadorDaVez && origem.Modo == ModoComBola.Arremesso)
            {
                MudarPara(EstadoLance.BolaViva);
                bola.Lancar(saida, velocidade, origem.Jogador);
                bola.CorposAtivos = true; // lancha: o arremesso pode bater no corpo de um rebatedor
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
                return;
            }
            Debug.LogWarning($"[Lance] Arremesso ignorado: {origem.Jogador.Nome} (modo {origem.Modo}) no estado {Estado}; " +
                             $"arremessa agora: {arremessadorDaVez?.Jogador.Nome}, portador: {portador?.Jogador.Nome}");
        }

        private void AoRebater(TipoBatida tipo, float kmh, float erroMs, bool perfeita)
        {
            rebatida = true;
            bola.CorposAtivos = false;
            rebatedorDaVez.Encerrar();
            corrida.Ativar();
            string qualidade = perfeita ? "PERFEITA!" : "Boa";
            resumoBatida = $"{qualidade} {NomeDaBatida(tipo)} · {kmh:0} km/h  ({TextoErro(erroMs)})";
            Mostrar(resumoBatida);
            // Troca automática (opcional): quem joga na defesa passa para quem chega antes na bola
            if (configPartida.trocaAutomaticaNaDefesa)
            {
                Interceptacao.Prever(bola);
                Defensor quemVai = Interceptacao.QuemChegaAntes(configFisica, defensores, out _, out _);
                if (quemVai != null) gerenciador.Ativar(quemVai.Jogador);
            }
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
            RegistroPartida.Evento($"{defensor.Jogador.Nome} pegou a bola{(noAr ? " no ar" : "")} (último a lançar: {lancador?.Nome ?? "-"})");
            bola.CorposAtivos = false;
            portador = defensor.ComBola;
            portador.Alvo = EscolherAlvo(portador.transform.position);
            portador.Parceiro = OutroDefensor(defensor.Jogador);
            portador.ReceberBola(ModoComBola.Campo);
            bola.Colocar(portador.PosicaoMao);
            // Pegou a bola: acabou a chance de rebater este arremesso
            rebatedorDaVez.Encerrar();
            if (configPartida.trocaAutomaticaNaDefesa) gerenciador.Ativar(defensor.Jogador);

            if (configPartida.pegarNoArPerdeTaco && noAr && lancador != null && lancador.Dupla == Dupla.Taco)
            {
                EntrarTrocaDePapel("PEGOU NO AR!");
                return;
            }
            // Passe do colega não precisa de aviso
            if (lancador == null || lancador.Dupla == Dupla.Taco) Mostrar($"{defensor.Jogador.Nome} dominou a bola");
        }

        /// <summary>
        /// Lancha (regra da rua): o arremesso bateu no corpo de um rebatedor, o da vez ou o parceiro.
        /// Conta contra a dupla do taco; a bola desvia e segue em jogo. Juntou o limite: perde os tacos.
        /// </summary>
        private void AoLancha(Jogador rebatedor)
        {
            if (Estado != EstadoLance.BolaViva || rebatida) return;
            rebatedorDaVez.Encerrar(); // a bola desviada não pode mais ser rebatida
            if (!configPartida.contarLancha)
            {
                // Padrão: bola no corpo não é falta; ela desvia e o jogo segue
                Mostrar($"Bola no corpo do {rebatedor.Nome}: segue o jogo");
                return;
            }
            if (!configPartida.lanchaNoRebatedorDaVez && rebatedor == rebatedorDaVez.Jogador)
            {
                // Quem ia rebater só não acertou e a bola pegou nele: não é falta, a bola segue
                Mostrar($"Bola no corpo do {rebatedor.Nome}: segue o jogo");
                return;
            }
            lanchas++;
            if (lanchas >= configPartida.lanchasParaPerderTaco)
            {
                EntrarTrocaDePapel($"{lanchas} LANCHAS!");
                return;
            }
            Mostrar($"LANCHA! Bola no corpo do {rebatedor.Nome}  ({lanchas} de {configPartida.lanchasParaPerderTaco})");
        }

        private void AoCruzar(int pendentes)
        {
            Mostrar($"CRUZOU!  +{pendentes} pendente (cada um até a base do outro lado)");
        }

        private void AoValidarPontos(int cruzadas)
        {
            int meta = configPartida.metaPontos;
            // Já estava em match point: esta cruzada validada fecha o jogo
            if (EmMatchPoint(ladoNoTaco))
            {
                EntrarFimDePartida(ladoNoTaco);
                return;
            }
            int ganhou = cruzadas * configPartida.pontosPorCruzada;
            // Para na meta: o resto não conta, precisa fechar com uma cruzada nova
            pontos[ladoNoTaco] = Mathf.Min(meta, pontos[ladoNoTaco] + ganhou);
            if (EmMatchPoint(ladoNoTaco)) Mostrar($"MATCH POINT! Lado {ladoNoTaco + 1} precisa de mais uma cruzada para fechar");
            else Mostrar(ganhou == 1 ? "PONTO!" : $"+{ganhou} PONTOS!");
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

            // Quem arremessa fica atrás da casinha de origem; o guardião atrás da casinha alvo
            Jogador guardiao = OutroDefensor(arremessadorDaVez.Jogador);
            arremessadorDaVez.Jogador.Teleportar(PosicaoDeGuarda(arremessadorDaVez.Jogador), RumoAoAlvo);
            if (guardiao != null) guardiao.Teleportar(PosicaoDeGuarda(guardiao), -RumoAoAlvo);
        }

        /// <summary>Direção (no chão) da casinha de origem para a casinha alvo.</summary>
        private Vector3 RumoAoAlvo => arremessoDeAParaB ? Vector3.right : Vector3.left;

        /// <summary>
        /// Onde cada defensor espera neste lance: quem arremessa, atrás da casinha de origem;
        /// o outro (guardião), atrás da casinha alvo. Os bots voltam para cá.
        /// </summary>
        public Vector3 PosicaoDeGuarda(Jogador defensor)
        {
            // Colega com a bola no campo: vai guardar atrás da casinha em que ele vai arremessar
            if (portador != null && portador.Jogador != defensor)
            {
                Garrafa origemPrevista = CasinhaMaisPerto(portador.transform.position);
                Garrafa alvoPrevisto = origemPrevista == garrafaA ? garrafaB : garrafaA;
                float paraFora = -ZonaDoTaco.ParaOArremesso(alvoPrevisto.transform.position);
                return new Vector3(alvoPrevisto.transform.position.x + paraFora * configPartida.distanciaGuardiao, 0f, 0f);
            }
            bool ehQuemArremessa = arremessadorDaVez != null && defensor == arremessadorDaVez.Jogador;
            Vector3 origem = new Vector3(GarrafaDeOrigem.transform.position.x, 0f, 0f);
            Vector3 alvo = new Vector3(GarrafaAlvo.transform.position.x, 0f, 0f);
            return ehQuemArremessa
                ? origem - RumoAoAlvo * configPartida.afastamentoArremessador
                : alvo + RumoAoAlvo * configPartida.distanciaGuardiao;
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
            RegistroPartida.Evento(texto);
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

        /// <summary>
        /// Placar compacto no canto superior esquerdo (REFERENCIAS_FIFA, seção 4):
        /// pontos dos dois lados, quem está no taco, de que casinha sai o arremesso,
        /// pontos pendentes e MATCH POINT piscando.
        /// </summary>
        private void DesenharPlacar()
        {
            const float velocidadePiscar = 3f;
            GUIStyle placar = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold };
            placar.normal.textColor = Color.white;
            GUIStyle detalhe = new GUIStyle(GUI.skin.label) { fontSize = 13 };
            detalhe.normal.textColor = new Color(0.85f, 0.85f, 0.85f);

            string Lado(int i) => $"LADO {i + 1}{(i == ladoNoTaco ? " (taco)" : "")}";
            GUI.Label(new Rect(16, 6, 600, 30), $"{Lado(0)}  {pontos[0]}  ×  {pontos[1]}  {Lado(1)}", placar);

            string casinhas = arremessoDeAParaB ? "A → B" : "B → A";
            string pendente = corrida.PontosPendentes > 0 ? $" · +{corrida.PontosPendentes} pendente" : "";
            string textoLanchas = lanchas > 0 ? $" · lanchas {lanchas}/{configPartida.lanchasParaPerderTaco}" : "";
            string linha = $"meta {configPartida.metaPontos} + cruzada final · arremesso {casinhas}{pendente}{textoLanchas}";
            bool piscando = Mathf.Repeat(Time.unscaledTime * velocidadePiscar, 1f) < 0.6f;
            if (EmMatchPoint(ladoNoTaco) && vencedor < 0 && piscando) linha += $"   MATCH POINT do Lado {ladoNoTaco + 1}!";
            GUI.Label(new Rect(16, 36, 900, 22), linha, detalhe);
        }

        /// <summary>Aviso grande na troca de papel: quem vai para o taco, quem vai para a bola, e em quanto tempo.</summary>
        private void DesenharTrocaDePapel()
        {
            GUIStyle grande = new GUIStyle(GUI.skin.label) { fontSize = 40, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            grande.normal.textColor = Color.white;
            GUIStyle menor = new GUIStyle(grande) { fontSize = 22, fontStyle = FontStyle.Normal };
            int novoNoTaco = 1 - ladoNoTaco;
            float falta = Mathf.Max(0f, configPartida.tempoTrocaDePapel - tempoNoEstado);
            float meio = Screen.height * 0.5f;
            GUI.Label(new Rect(0, meio - 70, Screen.width, 60), "TROCA DE PAPEL", grande);
            GUI.Label(new Rect(0, meio - 5, Screen.width, 30),
                      $"Lado {novoNoTaco + 1} vai para o TACO  ·  Lado {ladoNoTaco + 1} vai para a BOLA", menor);
            GUI.Label(new Rect(0, meio + 30, Screen.width, 30), $"cada time continua com a sua cor  ·  {falta:0} s", menor);
        }

        private void DesenharVencedor()
        {
            GUIStyle grande = new GUIStyle(GUI.skin.label)
            {
                fontSize = 48, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold
            };
            grande.normal.textColor = Color.white;
            GUIStyle menor = new GUIStyle(grande) { fontSize = 20, fontStyle = FontStyle.Normal };
            float meio = Screen.height * 0.5f;
            GUI.Label(new Rect(0, meio - 60, Screen.width, 60), $"LADO {vencedor + 1} VENCEU!", grande);
            GUI.Label(new Rect(0, meio + 5, Screen.width, 30),
                      $"{pontos[vencedor]} pontos + cruzada final  ·  placar {pontos[0]} × {pontos[1]}", menor);
            GUI.Label(new Rect(0, meio + 40, Screen.width, 30), "Start (controle) ou Enter (teclado): jogar de novo", menor);
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
                    ? "ARREMESSO: R2 / clique esq. reto · L2 / E quicado · R1 / clique dir. pingo (solte na faixa verde)\n" +
                      "   depois de soltar: analógico direito faz a bola curvar\n"
                    : "ARREMESSO: a CPU arremessa (Select / Tab troca de dupla)\n";
                if (tacoHumano)
                    instrucoes += "REBATEDOR: dentro do círculo o taco fica no chão sozinho (garrafa vermelha = fora, pode ser queimada)\n" +
                                  "   batida: R2 forte · R1 toque · L2 cavadinha · direção: analógico direito / mouse\n";
            }
            else if (Estado == EstadoLance.BolaViva)
            {
                if (rebatedorDaVez != null && rebatedorDaVez.Ativo && tacoHumano)
                    instrucoes += "BATIDA: R2 forte · R1 toque · L2 cavadinha — bata quando o anel fechar\n";
                if (tacoHumano && rebatida)
                    instrucoes += "CORRIDA: R1 / Shift sprint · no meio, X (A) / Espaço cruza · volte para o círculo · L1 / Q troca de jogador\n";
                if (bolaHumano)
                    instrucoes += portador != null && portador.Jogador.Controlador != null
                        ? "COM A BOLA: R2 chuta na garrafa · R1 passe · L2 lançamento · mira: analógico direito\n" +
                          "   volte para trás de uma casinha: com os rebatedores seguros, você arremessa dali\n"
                        : "DEFESA: □ (X) / R pega a bola · longe dela, mergulha / carrinho · L1 / Q troca de jogador\n";
            }
            else if (Estado == EstadoLance.BolaMorta) instrucoes += "Bola morta: novo arremesso";
            else if (Estado == EstadoLance.TrocaDePapel) instrucoes += "Troca de papel: quem defendia vai para o taco";
            instrucoes += "\nStart / Enter: reiniciar a partida";
            GUI.Label(new Rect(16, 62, 1000, 110), instrucoes, estilo);

            DesenharPlacar();
            if (Estado == EstadoLance.FimDePartida) DesenharVencedor();
            if (Estado == EstadoLance.TrocaDePapel) DesenharTrocaDePapel();

            // Mensagem do lance no alto, no meio, por alguns segundos
            if (tempoMensagem < configHUD.duracaoMensagem && !string.IsNullOrEmpty(mensagem))
            {
                GUIStyle grande = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 26, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold
                };
                grande.normal.textColor = Color.white;
                // Ponto marcado: bem maior e amarelo, para ninguém perder
                bool ponto = mensagem.Contains("PONTO") || mensagem.Contains("MATCH POINT");
                if (ponto)
                {
                    grande.fontSize = 48;
                    grande.normal.textColor = new Color(1f, 0.85f, 0.1f);
                }
                GUI.Label(new Rect(0, 115, Screen.width, ponto ? 70 : 40), mensagem, grande);
            }
        }
    }
}
