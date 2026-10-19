using UnityEngine;
using UnityEngine.InputSystem;

namespace BetsDeRua
{
    /// <summary>
    /// Uma pessoa jogando: um controle (gamepad) ou o teclado.
    /// Traduz os botões do dispositivo para as intenções do jogo,
    /// assim o resto do código não precisa saber se é PlayStation, Xbox ou teclado.
    /// </summary>
    public class ControleHumano
    {
        /// <summary>Número da pessoa na tela: 1 a 4.</summary>
        public int Numero { get; }
        public Gamepad Controle { get; }
        public Keyboard Teclado { get; }
        /// <summary>Jogador que esta pessoa controla agora (null = nenhum livre).</summary>
        public Jogador Jogador { get; set; }

        public ControleHumano(int numero, Gamepad controle)
        {
            Numero = numero;
            Controle = controle;
        }

        public ControleHumano(int numero, Keyboard teclado)
        {
            Numero = numero;
            Teclado = teclado;
        }

        /// <summary>True se o dispositivo ainda está conectado.</summary>
        public bool Conectado => Controle != null ? Controle.added : Teclado != null && Teclado.added;

        public string NomeDispositivo
        {
            get
            {
                if (Controle == null) return "Teclado";
                string nome = Controle.displayName;
                return string.IsNullOrEmpty(nome) ? "Controle" : nome;
            }
        }

        /// <summary>Analógico esquerdo ou WASD (comprimento de 0 a 1).</summary>
        public Vector2 Movimento
        {
            get
            {
                if (Controle != null) return Controle.leftStick.ReadValue();
                Vector2 v = Vector2.zero;
                if (Teclado.aKey.isPressed) v.x -= 1f;
                if (Teclado.dKey.isPressed) v.x += 1f;
                if (Teclado.sKey.isPressed) v.y -= 1f;
                if (Teclado.wKey.isPressed) v.y += 1f;
                return Vector2.ClampMagnitude(v, 1f);
            }
        }

        /// <summary>R2 (RT) ou Shift: sprint.</summary>
        public bool Sprint => Controle != null
            ? Controle.rightTrigger.isPressed
            : Teclado.leftShiftKey.isPressed || Teclado.rightShiftKey.isPressed;

        /// <summary>L2 (LT) ou Alt: andar devagar.</summary>
        public bool Devagar => Controle != null
            ? Controle.leftTrigger.isPressed
            : Teclado.leftAltKey.isPressed || Teclado.rightAltKey.isPressed;

        /// <summary>L1 (LB) ou Q: trocar para o parceiro da dupla.</summary>
        public bool ApertouTrocarJogador => Controle != null
            ? Controle.leftShoulder.wasPressedThisFrame
            : Teclado.qKey.wasPressedThisFrame;

        /// <summary>
        /// Select/Share/View ou Tab: trocar de dupla.
        /// Só do protótipo, para testar sozinho os dois lados.
        /// </summary>
        public bool ApertouTrocarDupla => Controle != null
            ? Controle.selectButton.wasPressedThisFrame
            : Teclado.tabKey.wasPressedThisFrame;
    }
}
