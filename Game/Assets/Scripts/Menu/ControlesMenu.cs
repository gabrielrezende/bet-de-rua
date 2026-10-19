using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.DualShock;

namespace BetsDeRua
{
    /// <summary>
    /// Leitura dos controles nos menus, por dispositivo (cada controle, ou o teclado):
    /// direção com repetição (segurar anda de opção em opção), confirmar, voltar, abas (L1/R1),
    /// botões extras e analógico direito. Também sabe qual foi o último dispositivo usado, para
    /// os ícones da tela mostrarem ✕○□△ (PlayStation), A B X Y (Xbox) ou as teclas.
    /// </summary>
    public static class ControlesMenu
    {
        private const float EsperaRepetir = 0.38f;
        private const float IntervaloRepetir = 0.11f;
        private const float LimiteAnalogico = 0.55f;

        private class EstadoDirecao
        {
            public Vector2Int ultima;
            public float proxima;
        }

        private static readonly Dictionary<InputDevice, EstadoDirecao> estados = new Dictionary<InputDevice, EstadoDirecao>();

        public enum TipoIcone { PlayStation, Xbox, Teclado }

        /// <summary>Que ícones mostrar (do último dispositivo que apertou algo).</summary>
        public static TipoIcone Icones { get; private set; } = TipoIcone.PlayStation;

        /// <summary>Todos os dispositivos que podem mexer no menu (controles e teclado).</summary>
        public static IEnumerable<InputDevice> Dispositivos()
        {
            foreach (Gamepad g in Gamepad.all) yield return g;
            if (Keyboard.current != null) yield return Keyboard.current;
        }

        /// <summary>Direção apertada neste quadro (com repetição ao segurar). (0,0) = nada.</summary>
        public static Vector2Int Direcao(InputDevice d)
        {
            Vector2Int agora = DirecaoSegurada(d);
            if (!estados.TryGetValue(d, out EstadoDirecao e)) estados[d] = e = new EstadoDirecao();
            Vector2Int resultado = Vector2Int.zero;
            if (agora != Vector2Int.zero)
            {
                if (agora != e.ultima) { resultado = agora; e.proxima = Time.unscaledTime + EsperaRepetir; }
                else if (Time.unscaledTime >= e.proxima) { resultado = agora; e.proxima = Time.unscaledTime + IntervaloRepetir; }
            }
            e.ultima = agora;
            if (resultado != Vector2Int.zero) Marcar(d);
            return resultado;
        }

        private static Vector2Int DirecaoSegurada(InputDevice d)
        {
            if (d is Gamepad g)
            {
                Vector2 v = g.dpad.ReadValue() + g.leftStick.ReadValue();
                if (Mathf.Abs(v.x) < LimiteAnalogico) v.x = 0f;
                if (Mathf.Abs(v.y) < LimiteAnalogico) v.y = 0f;
                if (Mathf.Abs(v.x) >= Mathf.Abs(v.y)) v.y = 0f; else v.x = 0f;
                return new Vector2Int((int)Mathf.Sign(v.x) * (v.x != 0f ? 1 : 0), (int)Mathf.Sign(v.y) * (v.y != 0f ? 1 : 0));
            }
            if (d is Keyboard k)
            {
                int x = (k.rightArrowKey.isPressed || k.dKey.isPressed ? 1 : 0) - (k.leftArrowKey.isPressed || k.aKey.isPressed ? 1 : 0);
                int y = (k.upArrowKey.isPressed || k.wKey.isPressed ? 1 : 0) - (k.downArrowKey.isPressed || k.sKey.isPressed ? 1 : 0);
                if (x != 0) y = 0;
                return new Vector2Int(x, y);
            }
            return Vector2Int.zero;
        }

        public static bool Confirmar(InputDevice d) => Apertou(d, g => g.buttonSouth, k => k.enterKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame || k.numpadEnterKey.wasPressedThisFrame);
        public static bool Voltar(InputDevice d) => Apertou(d, g => g.buttonEast, k => k.escapeKey.wasPressedThisFrame || k.backspaceKey.wasPressedThisFrame);
        public static bool Start(InputDevice d) => Apertou(d, g => g.startButton, k => k.enterKey.wasPressedThisFrame || k.escapeKey.wasPressedThisFrame);
        public static bool AbaEsquerda(InputDevice d) => Apertou(d, g => g.leftShoulder, k => k.qKey.wasPressedThisFrame);
        public static bool AbaDireita(InputDevice d) => Apertou(d, g => g.rightShoulder, k => k.eKey.wasPressedThisFrame);
        public static bool Quadrado(InputDevice d) => Apertou(d, g => g.buttonWest, k => k.xKey.wasPressedThisFrame);
        public static bool Triangulo(InputDevice d) => Apertou(d, g => g.buttonNorth, k => k.rKey.wasPressedThisFrame);
        public static bool QualquerBotao(InputDevice d) => Apertou(d, g => g.buttonSouth, k => k.anyKey.wasPressedThisFrame)
                                                           || (d is Gamepad gp && (gp.startButton.wasPressedThisFrame || gp.buttonEast.wasPressedThisFrame));

        /// <summary>Analógico direito (girar a prévia); no teclado, as teclas J/L e I/K.</summary>
        public static Vector2 AnalogicoDireito(InputDevice d)
        {
            if (d is Gamepad g) return g.rightStick.ReadValue();
            if (d is Keyboard k)
                return new Vector2((k.lKey.isPressed ? 1 : 0) - (k.jKey.isPressed ? 1 : 0), (k.iKey.isPressed ? 1 : 0) - (k.kKey.isPressed ? 1 : 0));
            return Vector2.zero;
        }

        /// <summary>Gatilhos (zoom da prévia): R2 aproxima, L2 afasta. No teclado, + e -.</summary>
        public static float Zoom(InputDevice d)
        {
            if (d is Gamepad g) return g.rightTrigger.ReadValue() - g.leftTrigger.ReadValue();
            if (d is Keyboard k) return (k.equalsKey.isPressed || k.numpadPlusKey.isPressed ? 1 : 0) - (k.minusKey.isPressed || k.numpadMinusKey.isPressed ? 1 : 0);
            return 0f;
        }

        private static bool Apertou(InputDevice d, System.Func<Gamepad, UnityEngine.InputSystem.Controls.ButtonControl> botao, System.Func<Keyboard, bool> tecla)
        {
            bool apertou = d is Gamepad g ? botao(g).wasPressedThisFrame : d is Keyboard k && tecla(k);
            if (apertou) Marcar(d);
            return apertou;
        }

        private static void Marcar(InputDevice d)
        {
            if (d is Keyboard) Icones = TipoIcone.Teclado;
            else if (d is DualShockGamepad) Icones = TipoIcone.PlayStation;
            else if (d is Gamepad) Icones = d.description.manufacturer != null && d.description.manufacturer.Contains("Sony") ? TipoIcone.PlayStation : TipoIcone.Xbox;
        }

        // ------------------------------------------------------------------ nomes dos botões na tela

        public static string NomeConfirmar => Icones == TipoIcone.PlayStation ? "✕" : Icones == TipoIcone.Xbox ? "A" : "Enter";
        public static string NomeVoltar => Icones == TipoIcone.PlayStation ? "○" : Icones == TipoIcone.Xbox ? "B" : "Esc";
        public static string NomeQuadrado => Icones == TipoIcone.PlayStation ? "□" : Icones == TipoIcone.Xbox ? "X" : "X";
        public static string NomeTriangulo => Icones == TipoIcone.PlayStation ? "△" : Icones == TipoIcone.Xbox ? "Y" : "R";
        public static string NomeAbaEsquerda => Icones == TipoIcone.PlayStation ? "L1" : Icones == TipoIcone.Xbox ? "LB" : "Q";
        public static string NomeAbaDireita => Icones == TipoIcone.PlayStation ? "R1" : Icones == TipoIcone.Xbox ? "RB" : "E";
        public static string NomeStart => Icones == TipoIcone.PlayStation ? "OPTIONS" : Icones == TipoIcone.Xbox ? "MENU" : "Enter";
        public static string NomeAnalogicoDireito => Icones == TipoIcone.Teclado ? "J / L" : "R3 ↔";
        public static string NomeZoom => Icones == TipoIcone.PlayStation ? "L2 / R2" : Icones == TipoIcone.Xbox ? "LT / RT" : "+ / -";

        /// <summary>Nome curto do dispositivo, para a tela de escolha de lado.</summary>
        public static string NomeDispositivo(InputDevice d)
        {
            if (d is Keyboard) return "Teclado";
            if (d is DualShockGamepad || (d.description.manufacturer ?? "").Contains("Sony")) return "PlayStation";
            return "Controle";
        }
    }
}
