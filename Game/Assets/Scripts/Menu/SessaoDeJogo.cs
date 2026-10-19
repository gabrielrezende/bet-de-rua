using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BetsDeRua
{
    public enum ModoDeJogo { PartidaRapida, MultiplayerLocal, TreinoArremesso, Assistir, TreinoBatida }

    /// <summary>Uma pessoa que vai jogar: o controle dela, o lado em que começa e o personagem dela.</summary>
    public class PessoaNaPartida
    {
        public InputDevice dispositivo;   // Gamepad ou Keyboard
        public int numero;                // P1, P2, P3, P4
        public int lado;                  // 0 = Lado 1 (começa no taco), 1 = Lado 2 (começa na bola)
        public int vaga;                  // qual personagem salvo (0 a 3) ela usa

        public bool Teclado => dispositivo is Keyboard;
    }

    /// <summary>
    /// O que foi escolhido no menu e vale para a partida (passa de uma cena para a outra).
    /// Se a cena da partida for aberta direto no editor (sem menu), Configurada fica false e a
    /// partida funciona do jeito antigo (tela de escolha do time, controles entram sozinhos).
    /// </summary>
    public static class SessaoDeJogo
    {
        public static bool Configurada { get; private set; }
        public static ModoDeJogo Modo { get; private set; } = ModoDeJogo.PartidaRapida;
        public static readonly List<PessoaNaPartida> Pessoas = new List<PessoaNaPartida>();

        /// <summary>O controle de quem apertou START na abertura (é o P1, dono do menu).</summary>
        public static InputDevice DispositivoPrincipal { get; set; }

        public const string CenaMenu = "Menu";
        public const string CenaPartida = "Prototipo";

        public static void Configurar(ModoDeJogo modo, IEnumerable<PessoaNaPartida> pessoas)
        {
            Modo = modo;
            Pessoas.Clear();
            if (pessoas != null) Pessoas.AddRange(pessoas);
            Configurada = true;
        }

        public static void Limpar()
        {
            Configurada = false;
            Pessoas.Clear();
        }

        /// <summary>Tem alguém (pessoa) jogando neste lado?</summary>
        public static bool TemPessoaNoLado(int lado)
        {
            foreach (PessoaNaPartida p in Pessoas)
                if (p.lado == lado) return true;
            return false;
        }
    }
}
