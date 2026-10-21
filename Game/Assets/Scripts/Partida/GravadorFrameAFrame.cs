using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace BetsDeRua
{
    /// <summary>
    /// Ferramenta de teste (não aparece no jogo): grava a partida quadro a quadro, com o tempo do
    /// jogo fixo (por padrão 60 quadros por segundo de jogo, por mais devagar que o PC desenhe).
    /// Numa pasta fora de Game/Assets, salva:
    /// - quadros/q000001.jpg ... : a tela da transmissão (com placar e mensagens), em sequência;
    /// - perto/q000001.jpg ... : no mesmo quadro, uma câmera de perto (corpo inteiro) no jogador mais perto
    ///   da bola, para avaliar postura e animação;
    /// - quadros.csv : uma linha por quadro do jogo (salvo ou não), com o tempo do jogo, o tempo real
    ///   que o PC levou para desenhar, o custo da gravação, o estado do lance, o placar e a bola;
    /// - lances.txt : o que aconteceu em cada lance (eventos do jogo e dos bots), com o quadro de cada um;
    /// - resumo.txt : quadros por segundo, travadas e contagens da partida.
    /// Ligada pelo menu Bets > Gravar partida (MenuGravarPartida) ou, no jogo instalado, pela linha
    /// de comando: BetsDeRua.exe -gravarPartida "pasta" [-gravarCada 1] [-soMedir] [-gravarSegundos 600].
    /// </summary>
    public class GravadorFrameAFrame : MonoBehaviour
    {
        /// <summary>Opções de uma gravação.</summary>
        public class Opcoes
        {
            public string pasta;
            /// <summary>Quadros do jogo por segundo (tempo fixo). 0 = tempo real (para medir desempenho).</summary>
            public float quadrosPorSegundo = 60f;
            /// <summary>Salva uma imagem a cada N quadros (1 = todos). O quadros.csv registra todos.</summary>
            public int salvarCada = 1;
            /// <summary>Largura das imagens salvas (a altura segue a tela).</summary>
            public int largura = 960;
            public int qualidadeJpg = 80;
            /// <summary>True = não salva imagens (só o registro e o desempenho).</summary>
            public bool soMedir;
            /// <summary>Para sozinho depois deste tempo de jogo (segundos).</summary>
            public float segundosMaximos = 1800f;
            /// <summary>Para sozinho quando a partida acaba (alguém fecha o jogo).</summary>
            public bool pararNoFimDaPartida = true;
            /// <summary>Grava também uma câmera de perto (perto/), de corpo inteiro, no jogador mais perto da bola:
            /// para avaliar a postura e a animação, que na transmissão ficam pequenas demais.</summary>
            public bool cameraDePerto = true;
            public int larguraPerto = 720;
        }

        /// <summary>A gravação terminou (o menu do editor usa para sair do Play).</summary>
        public static event System.Action<string> Terminou;
        public static GravadorFrameAFrame Atual { get; private set; }

        /// <summary>Trava: um quadro que levou mais que isto (tempo real, sem contar a gravação).</summary>
        private const float limiteTravadaMs = 50f;
        /// <summary>Imagens esperando para virar arquivo; acima disso, o jogo espera (não estoura a memória).</summary>
        private const int maximoNaFila = 90;

        private Opcoes opcoes;
        private string pastaQuadros;
        private string pastaPerto;

        // Câmera de perto: segue o jogador mais perto da bola (troca com uma folga, para não pular toda hora)
        private Camera perto;
        private RenderTexture rtPerto;
        private Texture2D leituraPerto;
        private Jogador[] jogadores;
        private Jogador foco;
        private float tempoCandidato;
        private Vector3 posicaoPerto;
        private const float distanciaPerto = 4.2f;
        private const float alturaPerto = 1.3f;
        private const float folgaTrocaFoco = 1.0f;   // outro precisa estar 1 m mais perto da bola...
        private const float tempoTrocaFoco = 0.35f;  // ...por este tempo, para a câmera trocar
        private StreamWriter csv;
        private readonly StringBuilder lances = new StringBuilder();
        private ControladorLance lance;
        private Bola bola;
        private EstadoLance estadoAnterior;
        private bool estadoConhecido;
        private int numeroDoLance;
        private int quadro;
        private int salvos;
        private float inicioTempoJogo;
        private bool encerrando;
        private float encerrarEm = -1f;
        private string motivoFim = "";
        private bool saindo;   // o jogo está fechando (ou o editor saindo do Play): não captura mais nada

        // Desempenho (tempo real)
        private readonly Stopwatch relogio = new Stopwatch();
        private double ultimoTempoReal;
        private readonly System.Collections.Generic.List<float> msJogo = new System.Collections.Generic.List<float>();
        private int travadas;
        private float piorMs;
        private int piorQuadro;

        // Arquivos escritos em segundo plano (para a gravação pesar menos no jogo)
        private readonly ConcurrentQueue<(byte[] dados, int w, int h, string caminho)> fila =
            new ConcurrentQueue<(byte[], int, int, string)>();
        private Thread escritor;
        private volatile bool escritorLigado;
        private RenderTexture reduzida;
        private Texture2D leitura;

        // ------------------------------------------------------------------ começar e parar

        public static GravadorFrameAFrame Iniciar(Opcoes opcoes)
        {
            if (Atual != null) Atual.Parar("nova gravação");
            GameObject g = new GameObject("GravadorFrameAFrame");
            DontDestroyOnLoad(g);
            GravadorFrameAFrame gravador = g.AddComponent<GravadorFrameAFrame>();
            gravador.Comecar(opcoes);
            return gravador;
        }

        /// <summary>Pasta padrão: Gravacoes/AAAA-MM-DD_HH-MM-SS na raiz do repositório (fora de Game/Assets).</summary>
        public static string PastaPadrao()
        {
            string raiz = Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));
            if (!Application.isEditor) raiz = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            return Path.Combine(raiz, "Gravacoes", System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"));
        }

        private void Comecar(Opcoes o)
        {
            Atual = this;
            opcoes = o;
            if (string.IsNullOrEmpty(opcoes.pasta)) opcoes.pasta = PastaPadrao();
            pastaQuadros = Path.Combine(opcoes.pasta, "quadros");
            Directory.CreateDirectory(pastaQuadros);
            pastaPerto = Path.Combine(opcoes.pasta, "perto");
            if (opcoes.cameraDePerto && !opcoes.soMedir) Directory.CreateDirectory(pastaPerto);
            csv = new StreamWriter(Path.Combine(opcoes.pasta, "quadros.csv"), false, new UTF8Encoding(false));
            csv.WriteLine("quadro;tempo_jogo_s;ms_real;ms_gravacao;salvo;lance;estado;placar_lado1;placar_lado2;lado_no_taco;bola_x;bola_y;bola_z;bola_kmh;foco_perto;mensagem");
            lances.AppendLine($"# Gravação frame a frame — {System.DateTime.Now:dd/MM/yyyy HH:mm:ss}");
            lances.AppendLine($"# {(opcoes.quadrosPorSegundo > 0f ? $"tempo fixo de {opcoes.quadrosPorSegundo:0} quadros/s de jogo" : "tempo real")}, " +
                              $"imagem a cada {opcoes.salvarCada} quadro(s){(opcoes.soMedir ? " (só medindo, sem imagens)" : "")}");
            lances.AppendLine($"# PC: {SystemInfo.processorType.Trim()} | {SystemInfo.graphicsDeviceName} | {SystemInfo.systemMemorySize} MB | tela {Screen.width}x{Screen.height}");
            lances.AppendLine("# Formato: tempo de jogo | quadro | texto");
            lances.AppendLine();

            Time.captureDeltaTime = opcoes.quadrosPorSegundo > 0f ? 1f / opcoes.quadrosPorSegundo : 0f;
            inicioTempoJogo = Time.time;
            RegistroPartida.AoEvento += AoEvento;
            Application.quitting += AoSair;

            escritorLigado = true;
            escritor = new Thread(Escrever) { IsBackground = true, Name = "GravadorFrameAFrame" };
            escritor.Start();
            relogio.Start();
            ultimoTempoReal = relogio.Elapsed.TotalMilliseconds;
        }

        /// <summary>Para a gravação e fecha os arquivos.</summary>
        public void Parar(string motivo)
        {
            if (encerrando) return;
            encerrando = true;
            motivoFim = motivo;
            RegistroPartida.AoEvento -= AoEvento;
            Application.quitting -= AoSair;
            Time.captureDeltaTime = 0f;
            Anotar($"FIM DA GRAVAÇÃO ({motivo})");

            // Espera as imagens da fila virarem arquivo
            escritorLigado = false;
            if (escritor != null && escritor.IsAlive) escritor.Join(60000);

            csv?.Dispose();
            csv = null;
            File.WriteAllText(Path.Combine(opcoes.pasta, "lances.txt"), lances.ToString(), new UTF8Encoding(false));
            File.WriteAllText(Path.Combine(opcoes.pasta, "resumo.txt"), Resumo(), new UTF8Encoding(false));
            if (reduzida != null) reduzida.Release();
            if (rtPerto != null) rtPerto.Release();
            if (Atual == this) Atual = null;
            string pasta = opcoes.pasta;
            Destroy(gameObject);
            Terminou?.Invoke(pasta);
        }

        private void AoSair() => saindo = true;

        private void OnDestroy()
        {
            // Saiu do Play no meio: fecha tudo do mesmo jeito
            if (!encerrando && opcoes != null) Parar("saiu do Play");
        }

        // ------------------------------------------------------------------ cada quadro

        private void LateUpdate()
        {
            if (encerrando) return;
            if (lance == null) lance = FindAnyObjectByType<ControladorLance>();
            if (bola == null) bola = FindAnyObjectByType<Bola>();
            AcompanharLance();
            StartCoroutine(FimDoQuadro());
        }

        private System.Collections.IEnumerator FimDoQuadro()
        {
            yield return new WaitForEndOfFrame();
            // Saindo do Play: este quadro já não existe
            if (encerrando || saindo || this == null) yield break;

            // Tempo real deste quadro: do fim da gravação do quadro anterior até agora
            // (o relógio volta a contar depois de gravar, então o custo da gravação fica de fora)
            double agora = relogio.Elapsed.TotalMilliseconds;
            float msReal = (float)(agora - ultimoTempoReal);
            if (quadro > 0) Medir(msReal);

            bool salvar = !opcoes.soMedir && quadro % Mathf.Max(1, opcoes.salvarCada) == 0;
            double antesGravar = relogio.Elapsed.TotalMilliseconds;
            string arquivo = "";
            if (salvar)
            {
                arquivo = $"q{quadro:000000}.jpg";
                Capturar(Path.Combine(pastaQuadros, arquivo));
                if (opcoes.cameraDePerto) CapturarPerto(Path.Combine(pastaPerto, arquivo));
                salvos++;
            }
            float msGravacao = (float)(relogio.Elapsed.TotalMilliseconds - antesGravar);
            EscreverLinha(msReal, msGravacao, arquivo);
            ultimoTempoReal = relogio.Elapsed.TotalMilliseconds;
            quadro++;

            float tempoJogo = Time.time - inicioTempoJogo;
            if (encerrarEm >= 0f && Time.time >= encerrarEm) Parar(motivoFim);
            else if (tempoJogo >= opcoes.segundosMaximos) Parar("tempo máximo");
        }

        private void Medir(float ms)
        {
            msJogo.Add(ms);
            if (ms > limiteTravadaMs)
            {
                travadas++;
                if (travadas <= 200) Anotar($"travada: {ms:0} ms");
            }
            if (ms > piorMs) { piorMs = ms; piorQuadro = quadro; }
        }

        private void AcompanharLance()
        {
            if (lance == null) return;
            EstadoLance estado = lance.Estado;
            if (estadoConhecido && estado == estadoAnterior) return;
            if (estado == EstadoLance.Arremesso)
            {
                numeroDoLance++;
                lances.AppendLine();
                lances.AppendLine($"=== LANCE {numeroDoLance} — placar {lance.Pontos(0)} x {lance.Pontos(1)}, " +
                                  $"Lado {lance.LadoNoTaco + 1} no taco, " +
                                  $"arremessa {(lance.ArremessadorDaVez != null ? lance.ArremessadorDaVez.Jogador.Nome : "?")}, " +
                                  $"rebate {(lance.RebatedorDaVez != null ? lance.RebatedorDaVez.Jogador.Nome : "?")} ===");
            }
            Anotar($"estado: {(estadoConhecido ? estadoAnterior.ToString() : "-")} -> {estado}");
            if (estado == EstadoLance.FimDePartida && opcoes.pararNoFimDaPartida && encerrarEm < 0f)
            {
                Anotar($"FIM DA PARTIDA: venceu o Lado {lance.Vencedor + 1}, {lance.Pontos(0)} x {lance.Pontos(1)}");
                motivoFim = "fim da partida";
                encerrarEm = Time.time + 2f; // mais 2 s para gravar a comemoração
            }
            estadoAnterior = estado;
            estadoConhecido = true;
        }

        private void AoEvento(string texto)
        {
            if (texto.StartsWith("estado:")) return; // o gravador já anota a troca de estado
            Anotar(texto);
        }

        private void Anotar(string texto)
        {
            float t = Time.time - inicioTempoJogo;
            int min = (int)(t / 60f);
            lances.AppendLine($"{min:00}:{t - min * 60f:00.00} | q{quadro:000000} | {texto}");
        }

        private void EscreverLinha(float msReal, float msGravacao, string arquivo)
        {
            if (csv == null) return;
            var c = System.Globalization.CultureInfo.InvariantCulture;
            string estado = lance != null ? lance.Estado.ToString() : "-";
            string p1 = lance != null ? lance.Pontos(0).ToString() : "";
            string p2 = lance != null ? lance.Pontos(1).ToString() : "";
            string taco = lance != null ? (lance.LadoNoTaco + 1).ToString() : "";
            Vector3 b = bola != null ? bola.Posicao : Vector3.zero;
            float kmh = bola != null ? bola.Velocidade.magnitude * 3.6f : 0f;
            string msg = lance != null && lance.MensagemVisivel ? lance.Mensagem.Replace(';', ',').Replace('\n', ' ') : "";
            csv.WriteLine(string.Join(";",
                quadro.ToString(), (Time.time - inicioTempoJogo).ToString("0.000", c), msReal.ToString("0.0", c),
                msGravacao.ToString("0.0", c), arquivo, numeroDoLance.ToString(), estado, p1, p2, taco,
                b.x.ToString("0.00", c), b.y.ToString("0.00", c), b.z.ToString("0.00", c), kmh.ToString("0", c), foco != null ? foco.Nome : "", msg));
        }

        // ------------------------------------------------------------------ câmera de perto

        private void EscolherFoco()
        {
            if (jogadores == null || jogadores.Length == 0) jogadores = FindObjectsByType<Jogador>(FindObjectsSortMode.None);
            if (bola == null || jogadores.Length == 0) return;
            Jogador maisPerto = null;
            float melhor = float.MaxValue;
            foreach (Jogador j in jogadores)
            {
                if (j == null) continue;
                float d = DistanciaNoChao(j.transform.position, bola.Posicao);
                if (d < melhor) { melhor = d; maisPerto = j; }
            }
            if (foco == null) { foco = maisPerto; return; }
            float doFoco = DistanciaNoChao(foco.transform.position, bola.Posicao);
            tempoCandidato = maisPerto != foco && melhor + folgaTrocaFoco < doFoco ? tempoCandidato + Time.deltaTime : 0f;
            if (tempoCandidato >= tempoTrocaFoco) { foco = maisPerto; tempoCandidato = 0f; }
        }

        private static float DistanciaNoChao(Vector3 a, Vector3 b)
        {
            a.y = 0f; b.y = 0f;
            return Vector3.Distance(a, b);
        }

        /// <summary>Corpo inteiro do jogador em foco, de 3/4 pela frente, do lado da transmissão.</summary>
        private void CapturarPerto(string caminho)
        {
            EscolherFoco();
            if (foco == null || Camera.main == null) return;
            if (perto == null)
            {
                GameObject c = new GameObject("CameraDePerto");
                c.transform.SetParent(transform);
                perto = c.AddComponent<Camera>();
                perto.CopyFrom(Camera.main);
                perto.enabled = false;
                perto.fieldOfView = 38f;
                int h = opcoes.larguraPerto * 9 / 16;
                rtPerto = new RenderTexture(opcoes.larguraPerto, h, 24, RenderTextureFormat.ARGB32);
                leituraPerto = new Texture2D(opcoes.larguraPerto, h, TextureFormat.RGB24, false);
            }
            // Fica entre a frente do jogador e o lado da transmissão (de onde vem a câmera principal)
            Vector3 frente = foco.transform.forward; frente.y = 0f;
            Vector3 lado = Camera.main.transform.position - foco.transform.position; lado.y = 0f;
            Vector3 rumo = (frente.normalized + lado.normalized * 1.2f);
            if (rumo.sqrMagnitude < 0.01f) rumo = lado;
            Vector3 alvo = foco.transform.position + rumo.normalized * distanciaPerto + Vector3.up * alturaPerto;
            // Suaviza (a troca de foco vira um corte seco, como na TV)
            bool corte = (posicaoPerto - alvo).sqrMagnitude > 9f;
            posicaoPerto = corte ? alvo : Vector3.Lerp(posicaoPerto, alvo, 0.15f);
            perto.transform.position = posicaoPerto;
            perto.transform.LookAt(foco.transform.position + Vector3.up * 0.9f);
            perto.targetTexture = rtPerto;
            perto.Render();
            perto.targetTexture = null;
            RenderTexture antes = RenderTexture.active;
            RenderTexture.active = rtPerto;
            leituraPerto.ReadPixels(new Rect(0, 0, rtPerto.width, rtPerto.height), 0, 0, false);
            RenderTexture.active = antes;
            byte[] dados = leituraPerto.GetRawTextureData<byte>().ToArray();
            while (fila.Count >= maximoNaFila) Thread.Sleep(2);
            fila.Enqueue((dados, rtPerto.width, rtPerto.height, caminho));
        }

        // ------------------------------------------------------------------ imagens

        private void Capturar(string caminho)
        {
            Texture2D tela = ScreenCapture.CaptureScreenshotAsTexture();
            if (tela == null) return;
            int w = Mathf.Min(opcoes.largura, tela.width);
            int h = Mathf.RoundToInt(tela.height * (w / (float)tela.width));
            if (reduzida == null || reduzida.width != w || reduzida.height != h)
            {
                if (reduzida != null) reduzida.Release();
                reduzida = new RenderTexture(w, h, 0, RenderTextureFormat.ARGB32);
                leitura = new Texture2D(w, h, TextureFormat.RGB24, false);
            }
            Graphics.Blit(tela, reduzida);
            Destroy(tela);
            RenderTexture antes = RenderTexture.active;
            RenderTexture.active = reduzida;
            leitura.ReadPixels(new Rect(0, 0, w, h), 0, 0, false);
            RenderTexture.active = antes;
            byte[] dados = leitura.GetRawTextureData<byte>().ToArray();

            // Fila cheia (o disco não está dando conta): espera um pouco em vez de estourar a memória
            while (fila.Count >= maximoNaFila) Thread.Sleep(2);
            fila.Enqueue((dados, w, h, caminho));
        }

        private void Escrever()
        {
            while (escritorLigado || !fila.IsEmpty)
            {
                if (!fila.TryDequeue(out var item)) { Thread.Sleep(2); continue; }
                try
                {
                    byte[] jpg = ImageConversion.EncodeArrayToJPG(item.dados, GraphicsFormat.R8G8B8_SRGB,
                        (uint)item.w, (uint)item.h, 0, opcoes.qualidadeJpg);
                    File.WriteAllBytes(item.caminho, jpg);
                }
                catch (System.Exception e) { UnityEngine.Debug.LogWarning($"[Gravação] {item.caminho}: {e.Message}"); }
            }
        }

        // ------------------------------------------------------------------ resumo

        private string Resumo()
        {
            var r = new StringBuilder();
            float tempoJogo = Time.time - inicioTempoJogo;
            r.AppendLine("Resumo da gravação frame a frame");
            r.AppendLine($"Pasta: {opcoes.pasta}");
            r.AppendLine($"Motivo do fim: {motivoFim}");
            r.AppendLine($"Tempo de jogo gravado: {tempoJogo:0.0} s ({quadro} quadros, {salvos} imagens)");
            r.AppendLine($"Tempo real gasto: {relogio.Elapsed.TotalSeconds:0} s");
            r.AppendLine($"Lances: {numeroDoLance}");
            if (lance != null) r.AppendLine($"Placar: {lance.Pontos(0)} x {lance.Pontos(1)}, trocas de papel: {lance.TrocasNaPartida}");
            r.AppendLine();
            r.AppendLine("Desempenho (tempo real de cada quadro, sem o custo da gravação):");
            if (msJogo.Count > 0)
            {
                var ordenado = new System.Collections.Generic.List<float>(msJogo);
                ordenado.Sort();
                double soma = 0;
                foreach (float m in ordenado) soma += m;
                float media = (float)(soma / ordenado.Count);
                float p50 = ordenado[ordenado.Count / 2];
                float p95 = ordenado[Mathf.Min(ordenado.Count - 1, (int)(ordenado.Count * 0.95f))];
                float p99 = ordenado[Mathf.Min(ordenado.Count - 1, (int)(ordenado.Count * 0.99f))];
                r.AppendLine($"  média {media:0.0} ms ({1000f / media:0} quadros/s)");
                r.AppendLine($"  mediana {p50:0.0} ms ({1000f / p50:0} quadros/s) | 95% {p95:0.0} ms | 99% {p99:0.0} ms");
                r.AppendLine($"  pior quadro {piorMs:0} ms (quadro {piorQuadro})");
                r.AppendLine($"  travadas (> {limiteTravadaMs:0} ms): {travadas} ({100f * travadas / ordenado.Count:0.00}% dos quadros)");
            }
            r.AppendLine($"Tela: {Screen.width}x{Screen.height} | {SystemInfo.graphicsDeviceName} | {(Application.isEditor ? "editor" : "jogo instalado")}");
            return r.ToString();
        }

        // ------------------------------------------------------------------ jogo instalado (linha de comando)

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void PelaLinhaDeComando()
        {
            if (Application.isEditor) return;
            string[] args = System.Environment.GetCommandLineArgs();
            int i = System.Array.IndexOf(args, "-gravarPartida");
            if (i < 0) return;
            var o = new Opcoes { pasta = i + 1 < args.Length && !args[i + 1].StartsWith("-") ? args[i + 1] : null };
            int cada = System.Array.IndexOf(args, "-gravarCada");
            if (cada >= 0 && cada + 1 < args.Length && int.TryParse(args[cada + 1], out int n)) o.salvarCada = n;
            int seg = System.Array.IndexOf(args, "-gravarSegundos");
            if (seg >= 0 && seg + 1 < args.Length && float.TryParse(args[seg + 1], System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out float s)) o.segundosMaximos = s;
            if (System.Array.IndexOf(args, "-soMedir") >= 0) { o.soMedir = true; o.quadrosPorSegundo = 0f; }
            // Vai direto para uma partida no modo assistir (os 4 bots), como pelo menu
            SessaoDeJogo.Configurar(ModoDeJogo.Assistir, null);
            UnityEngine.SceneManagement.SceneManager.LoadScene(SessaoDeJogo.CenaPartida);
            Iniciar(o);
            Terminou += _ => Application.Quit();
        }
    }
}
