using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BetsDeRua
{
    /// <summary>
    /// Junta as pessoas (controles e teclado) com os 4 jogadores em campo.
    /// - Cada controle conectado vira uma pessoa, na ordem em que conecta (até 4).
    /// - O teclado entra como mais uma pessoa quando alguém aperta uma tecla de jogo.
    /// - 1ª pessoa fica com a dupla do taco, 2ª com a dupla da bola, 3ª e 4ª
    ///   pegam o parceiro que sobrar (2x2).
    /// - L1 (LB) / Q troca para o parceiro da dupla, se ele estiver livre.
    /// - Select / Tab troca de dupla (só no protótipo, para testar sozinho).
    /// </summary>
    public class GerenciadorJogadores : MonoBehaviour
    {
        [SerializeField] private ConfigPartida configPartida;
        [Tooltip("Os 4 jogadores. O primeiro de cada dupla na lista é quem a pessoa pega ao entrar.")]
        [SerializeField] private Jogador[] jogadores;

        private readonly List<ControleHumano> humanos = new List<ControleHumano>();

        public IReadOnlyList<Jogador> Jogadores => jogadores;

        private void Update()
        {
            RemoverDesconectados();
            EntrarNovosControles();
            EntrarTeclado();

            foreach (ControleHumano humano in humanos)
            {
                if (humano.ApertouTrocarDupla) TrocarDupla(humano);
                else if (humano.ApertouTrocarJogador) TrocarParceiro(humano);

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
            // Ímpares (1ª, 3ª) começam na dupla do taco; pares (2ª, 4ª) na da bola
            Dupla preferida = humano.Numero % 2 == 1 ? Dupla.Taco : Dupla.Bola;
            Jogador livre = PrimeiroLivre(preferida) ?? PrimeiroLivre(Outra(preferida));
            Assumir(humano, livre);
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
            if (livre != null) Assumir(humano, livre);
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

        private Jogador PrimeiroLivre(Dupla dupla)
        {
            foreach (Jogador jogador in jogadores)
                if (jogador.Dupla == dupla && jogador.Controlador == null) return jogador;
            return null;
        }

        private static Dupla Outra(Dupla dupla) => dupla == Dupla.Taco ? Dupla.Bola : Dupla.Taco;

        private void Assumir(ControleHumano humano, Jogador jogador)
        {
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

            string texto = "JOGADORES (L1/LB ou Q: parceiro · Select ou Tab: trocar de dupla)\n";
            if (humanos.Count == 0) texto += "Conecte um controle ou aperte WASD para entrar\n";
            foreach (ControleHumano humano in humanos)
            {
                if (humano.Jogador == null)
                {
                    texto += $"P{humano.Numero} ({humano.NomeDispositivo}): sem jogador livre\n";
                    continue;
                }
                string dupla = humano.Jogador.Dupla == Dupla.Taco ? "Taco (azul)" : "Bola (laranja)";
                int estamina = Mathf.RoundToInt(humano.Jogador.Estamina * 100f);
                texto += $"P{humano.Numero} ({humano.NomeDispositivo}): {dupla} · {humano.Jogador.Nome}" +
                         $" · {humano.Jogador.Velocidade.magnitude:0.0} m/s · sprint {estamina}%\n";
            }
            GUI.Label(new Rect(Screen.width - 716, 8, 700, 200), texto, estilo);
        }
    }
}
