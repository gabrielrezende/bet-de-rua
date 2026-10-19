using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BetsDeRua
{
    /// <summary>
    /// Junta as pessoas (controles e teclado) com os 4 jogadores em campo.
    /// - Cada controle conectado vira uma pessoa, na ordem em que conecta (até 4).
    /// - O teclado entra como mais uma pessoa quando alguém aperta uma tecla de jogo.
    /// - Antes da partida, a tela de escolha: começar no TACO (rebater) ou na BOLA
    ///   (arremessar). Essa dupla é o "seu time": acompanha você na troca de papel.
    /// - Quem entra joga no seu time (o 2º controle é o seu parceiro, como chamar um amigo);
    ///   com o time cheio, os próximos vão para o outro lado (2x2).
    /// - Você controla um jogador; o parceiro joga sozinho (bot). L1 (LB) / Q troca qual
    ///   dos dois você controla, como no FIFA.
    /// - Select / Tab troca de dupla (só no protótipo, para testar sozinho).
    /// </summary>
    public class GerenciadorJogadores : MonoBehaviour
    {
        [SerializeField] private ConfigPartida configPartida;
        [Tooltip("Os 4 jogadores. O primeiro de cada dupla na lista é quem a pessoa pega ao entrar.")]
        [SerializeField] private Jogador[] jogadores;

        private readonly List<ControleHumano> humanos = new List<ControleHumano>();

        // Escolha do time antes da partida (o jogo fica parado enquanto isso)
        private bool escolhendoTime = true;
        private Dupla duplaEscolhida = Dupla.Taco;   // o que está marcado na tela
        private Dupla duplaDoTime = Dupla.Taco;      // onde o seu time está agora
        private bool analogicoSolto = true;          // para o analógico andar uma opção por vez

        public IReadOnlyList<Jogador> Jogadores => jogadores;

        private void Update()
        {
            // F2 liga e desliga o modo assistir (todos os jogadores viram bots)
            Keyboard teclado = Keyboard.current;
            if (teclado != null && teclado.f2Key.wasPressedThisFrame) configPartida.soAssistir = !configPartida.soAssistir;
            if (configPartida.soAssistir)
            {
                // Ninguém joga: solta todo mundo (os bots assumem) e não deixa ninguém entrar
                foreach (ControleHumano humano in humanos) Soltar(humano);
                humanos.Clear();
                if (escolhendoTime) Time.timeScale = 1f;
                return;
            }
            if (escolhendoTime)
            {
                AtualizarEscolha();
                return;
            }

            RemoverDesconectados();
            EntrarNovosControles();
            EntrarTeclado();

            foreach (ControleHumano humano in humanos)
            {
                if (humano.ApertouTrocarDupla) TrocarDupla(humano);
                // L1 troca para o parceiro, na defesa e no ataque (o taco no chão é automático)
                else if (humano.ApertouTrocarJogador) TrocarParceiro(humano);
                // Rebatedor correndo: □ se joga para a frente e desliza para alcançar a base com o taco
                else if (humano.ApertouPegar && humano.Jogador != null && humano.Jogador.Papel == Papel.Rebatedor)
                {
                    Rebatedor rebatedor = humano.Jogador.GetComponent<Rebatedor>();
                    if (rebatedor == null || !rebatedor.Ativo)
                    {
                        Vector3 rumo = humano.Jogador.Velocidade.sqrMagnitude > 0.5f ? humano.Jogador.Velocidade : humano.Jogador.transform.forward;
                        humano.Jogador.Mergulhar(rumo, false);
                        RegistroPartida.Evento($"{humano.Jogador.Nome} se joga para alcançar a base");
                    }
                }

                if (humano.Jogador != null)
                    humano.Jogador.DefinirEntrada(humano.Movimento, humano.Sprint, humano.Devagar);
            }
        }

        private void RemoverDesconectados()
        {
            for (int i = humanos.Count - 1; i >= 0; i--)
            {
                if (humanos[i].Conectado) continue;
                Soltar(humanos[i]);
                humanos.RemoveAt(i);
            }
        }

        private void EntrarNovosControles()
        {
            foreach (Gamepad controle in Gamepad.all)
            {
                if (humanos.Count >= configPartida.maximoJogadoresHumanos) return;
                if (humanos.Exists(h => h.Controle == controle)) continue;
                Entrar(new ControleHumano(ProximoNumero(), controle));
            }
        }

        private void EntrarTeclado()
        {
            Keyboard teclado = Keyboard.current;
            if (teclado == null || humanos.Exists(h => h.Teclado != null)) return;
            if (humanos.Count >= configPartida.maximoJogadoresHumanos) return;

            bool apertouTeclaDeJogo = teclado.wKey.wasPressedThisFrame || teclado.aKey.wasPressedThisFrame
                || teclado.sKey.wasPressedThisFrame || teclado.dKey.wasPressedThisFrame
                || teclado.qKey.wasPressedThisFrame || teclado.tabKey.wasPressedThisFrame;
            if (apertouTeclaDeJogo) Entrar(new ControleHumano(ProximoNumero(), teclado));
        }

        private int ProximoNumero()
        {
            for (int n = 1; ; n++)
                if (!humanos.Exists(h => h.Numero == n)) return n;
        }

        private void Entrar(ControleHumano humano)
        {
            humanos.Add(humano);
            // Entra no seu time; com o time cheio, vai para o outro lado
            Jogador livre = PrimeiroLivre(duplaDoTime) ?? PrimeiroLivre(Outra(duplaDoTime));
            Assumir(humano, livre);
        }

        // ------------------------------------------------------------------ escolha do time

        /// <summary>
        /// Tela de escolha: o jogo fica parado (timeScale 0) até alguém escolher.
        /// Esquerda/direita (analógico, direcional, A/D ou setas) muda; X (A) / Enter / Espaço confirma.
        /// </summary>
        private void AtualizarEscolha()
        {
            Time.timeScale = 0f;
            int mudar = 0;
            bool confirmar = false;
            bool algumAnalogicoInclinado = false;
            foreach (Gamepad controle in Gamepad.all)
            {
                if (controle.dpad.left.wasPressedThisFrame) mudar = -1;
                if (controle.dpad.right.wasPressedThisFrame) mudar = 1;
                float x = controle.leftStick.ReadValue().x;
                if (Mathf.Abs(x) > 0.6f)
                {
                    algumAnalogicoInclinado = true;
                    if (analogicoSolto) mudar = x < 0f ? -1 : 1;
                }
                if (controle.buttonSouth.wasPressedThisFrame || controle.startButton.wasPressedThisFrame) confirmar = true;
            }
            analogicoSolto = !algumAnalogicoInclinado;
            Keyboard teclado = Keyboard.current;
            if (teclado != null)
            {
                if (teclado.aKey.wasPressedThisFrame || teclado.leftArrowKey.wasPressedThisFrame) mudar = -1;
                if (teclado.dKey.wasPressedThisFrame || teclado.rightArrowKey.wasPressedThisFrame) mudar = 1;
                if (teclado.enterKey.wasPressedThisFrame || teclado.spaceKey.wasPressedThisFrame) confirmar = true;
            }
            if (mudar < 0) duplaEscolhida = Dupla.Taco;
            if (mudar > 0) duplaEscolhida = Dupla.Bola;
            if (confirmar) Escolher(duplaEscolhida);
        }

        /// <summary>Fecha a tela de escolha: o seu time começa nesta dupla e o jogo anda.</summary>
        public void Escolher(Dupla dupla)
        {
            escolhendoTime = false;
            duplaDoTime = dupla;
            Time.timeScale = 1f;
        }

        private void DesenharEscolha()
        {
            GUI.color = new Color(0f, 0f, 0f, 0.6f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;

            GUIStyle titulo = new GUIStyle(GUI.skin.label) { fontSize = 34, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            titulo.normal.textColor = Color.white;
            GUIStyle opcao = new GUIStyle(titulo) { fontSize = 28 };
            GUIStyle dica = new GUIStyle(titulo) { fontSize = 18, fontStyle = FontStyle.Normal };
            dica.normal.textColor = new Color(0.85f, 0.85f, 0.85f);

            float meio = Screen.height * 0.5f;
            GUI.Label(new Rect(0, meio - 130, Screen.width, 50), "Seu time começa com o quê?", titulo);

            bool taco = duplaEscolhida == Dupla.Taco;
            Color amarelo = new Color(1f, 0.85f, 0.1f);
            Color apagado = new Color(0.6f, 0.6f, 0.6f);
            opcao.normal.textColor = taco ? amarelo : apagado;
            GUI.Label(new Rect(Screen.width * 0.5f - 420, meio - 40, 400, 60), taco ? "> TACO (rebater) <" : "TACO (rebater)", opcao);
            opcao.normal.textColor = !taco ? amarelo : apagado;
            GUI.Label(new Rect(Screen.width * 0.5f + 20, meio - 40, 400, 60), !taco ? "> BOLA (arremessar) <" : "BOLA (arremessar)", opcao);

            GUI.Label(new Rect(0, meio + 50, Screen.width, 30),
                      "Você controla um jogador e o parceiro é bot (L1 troca qual você controla). " +
                      "Um 2º controle entra como seu parceiro.", dica);
            GUI.Label(new Rect(0, meio + 85, Screen.width, 30),
                      "Esquerda / direita (analógico, direcional ou A/D) escolhe  ·  X (A) / Enter confirma  ·  F2 só assistir", dica);
        }

        private void TrocarParceiro(ControleHumano humano)
        {
            if (humano.Jogador == null) return;
            Jogador parceiro = PrimeiroLivre(humano.Jogador.Dupla);
            if (parceiro != null) Assumir(humano, parceiro);
        }

        private void TrocarDupla(ControleHumano humano)
        {
            Dupla atual = humano.Jogador != null ? humano.Jogador.Dupla : Dupla.Bola;
            Jogador livre = PrimeiroLivre(Outra(atual));
            if (livre == null) return;
            Assumir(humano, livre);
            // Sozinho: o seu time passa a ser a outra dupla
            if (humanos.Count == 1) duplaDoTime = livre.Dupla;
        }

        /// <summary>
        /// Passa a pessoa que está jogando com a dupla de 'alvo' para ele (se ele estiver livre).
        /// Ex.: no começo do lance, quem joga com a dupla do taco vai para o rebatedor que recebe.
        /// </summary>
        public void Ativar(Jogador alvo)
        {
            if (alvo == null || alvo.Controlador != null) return;
            foreach (ControleHumano humano in humanos)
            {
                if (humano.Jogador != null && humano.Jogador.Dupla == alvo.Dupla)
                {
                    Assumir(humano, alvo);
                    return;
                }
            }
        }

        /// <summary>
        /// Troca de papel: quem estava no taco vai para a bola e vice-versa.
        /// Cada pessoa pega o primeiro jogador livre da outra dupla.
        /// </summary>
        public void InverterDuplas()
        {
            duplaDoTime = Outra(duplaDoTime);
            var duplaAntes = new Dictionary<ControleHumano, Dupla>();
            foreach (ControleHumano humano in humanos)
            {
                if (humano.Jogador == null) continue;
                duplaAntes[humano] = humano.Jogador.Dupla;
                Soltar(humano);
            }
            foreach (var par in duplaAntes) Assumir(par.Key, PrimeiroLivre(Outra(par.Value)));
        }

        /// <summary>True se alguma pessoa joga com esta dupla.</summary>
        public bool TemHumanoNaDupla(Dupla dupla)
        {
            foreach (ControleHumano humano in humanos)
                if (humano.Jogador != null && humano.Jogador.Dupla == dupla) return true;
            return false;
        }

        private Jogador PrimeiroLivre(Dupla dupla)
        {
            foreach (Jogador jogador in jogadores)
                if (jogador.Dupla == dupla && jogador.Controlador == null) return jogador;
            return null;
        }

        private static Dupla Outra(Dupla dupla) => dupla == Dupla.Taco ? Dupla.Bola : Dupla.Taco;

        private void Assumir(ControleHumano humano, Jogador jogador)
        {
            if (humano.Jogador != jogador)
                RegistroPartida.Evento($"P{humano.Numero}: {humano.Jogador?.Nome ?? "ninguém"} -> {jogador?.Nome ?? "ninguém"}");
            Soltar(humano);
            if (jogador == null) return;
            humano.Jogador = jogador;
            jogador.Controlador = humano;
        }

        private static void Soltar(ControleHumano humano)
        {
            if (humano.Jogador != null) humano.Jogador.Controlador = null;
            humano.Jogador = null;
        }

        private void OnGUI()
        {
            GUIStyle estilo = new GUIStyle(GUI.skin.label) { fontSize = 14, alignment = TextAnchor.UpperRight };
            estilo.normal.textColor = Color.white;

            if (escolhendoTime && !configPartida.soAssistir)
            {
                DesenharEscolha();
                return;
            }
            if (configPartida.soAssistir)
            {
                GUI.Label(new Rect(Screen.width - 716, Screen.height - 130, 700, 90),
                          "MODO ASSISTIR: todos são bots (F2 para voltar a jogar)", estilo);
                return;
            }
            string texto = "JOGADORES (L1/Q troca de jogador · Select ou Tab: trocar de dupla · F2: só assistir)\n";
            if (humanos.Count == 0) texto += "Conecte um controle ou aperte WASD para entrar\n";
            foreach (ControleHumano humano in humanos)
            {
                if (humano.Jogador == null)
                {
                    texto += $"P{humano.Numero} ({humano.NomeDispositivo}): sem jogador livre\n";
                    continue;
                }
                string dupla = humano.Jogador.Dupla == Dupla.Taco ? "Taco" : "Bola";
                int estamina = Mathf.RoundToInt(humano.Jogador.Estamina * 100f);
                texto += $"P{humano.Numero} ({humano.NomeDispositivo}): {dupla} · {humano.Jogador.Nome}" +
                         $" · {humano.Jogador.Velocidade.magnitude:0.0} m/s · sprint {estamina}%\n";
            }
            // Canto de baixo à direita, acima do placar, para não encostar nas instruções do alto
            const float altura = 90f;
            estilo.alignment = TextAnchor.LowerRight;
            GUI.Label(new Rect(Screen.width - 716, Screen.height - 40 - altura, 700, altura), texto, estilo);
        }
    }
}
