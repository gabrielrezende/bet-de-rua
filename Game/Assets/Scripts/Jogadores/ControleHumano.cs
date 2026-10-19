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

        /// <summary>
        /// True se o dispositivo ainda está conectado. Desconectado, todos os botões
        /// respondem "solto" (o gerenciador tira a pessoa do jogo no frame seguinte).
        /// </summary>
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
                if (!Conectado) return Vector2.zero;
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
        public bool Sprint => !Conectado ? default : Controle != null
            ? Controle.rightTrigger.isPressed
            : Teclado.leftShiftKey.isPressed || Teclado.rightShiftKey.isPressed;

        /// <summary>L2 (LT) ou Alt: andar devagar.</summary>
        public bool Devagar => !Conectado ? default : Controle != null
            ? Controle.leftTrigger.isPressed
            : Teclado.leftAltKey.isPressed || Teclado.rightAltKey.isPressed;

        /// <summary>○ (B) ou clique esquerdo, segurando: ação forte.</summary>
        public bool SegurandoForte => !Conectado ? default : Controle != null
            ? Controle.buttonEast.isPressed
            : Mouse.current != null && Mouse.current.leftButton.isPressed;

        /// <summary>X (A) ou Espaço, segurando: ação curta (quicado, toque, passe).</summary>
        public bool SegurandoCurta => !Conectado ? default : Controle != null ? Controle.buttonSouth.isPressed : Teclado.spaceKey.isPressed;

        /// <summary>△ (Y) ou E, segurando: por cima (pingo, cavadinha, lançamento).</summary>
        public bool SegurandoPorCima => !Conectado ? default : Controle != null ? Controle.buttonNorth.isPressed : Teclado.eKey.isPressed;

        /// <summary>Analógico direito: mira (comprimento de 0 a 1). No teclado a mira é o mouse.</summary>
        public Vector2 MiraAnalogica => !Conectado ? default : Controle != null ? Controle.rightStick.ReadValue() : Vector2.zero;

        /// <summary>True se a mira vem do mouse (teclado).</summary>
        public bool MiraPeloMouse => Controle == null;

        /// <summary>R2 (RT) ou clique esquerdo, segurando: arremesso forte.</summary>
        public bool SegurandoArremessoForte => !Conectado ? default : Controle != null
            ? Controle.rightTrigger.isPressed
            : Mouse.current != null && Mouse.current.leftButton.isPressed;

        /// <summary>R1 (RB) ou clique direito, segurando: arremesso quicado.</summary>
        public bool SegurandoArremessoQuicado => !Conectado ? default : Controle != null
            ? Controle.rightShoulder.isPressed
            : Mouse.current != null && Mouse.current.rightButton.isPressed;

        /// <summary>L2 (LT) ou E, segurando: arremesso pingo.</summary>
        public bool SegurandoArremessoPingo => !Conectado ? default : Controle != null ? Controle.leftTrigger.isPressed : Teclado.eKey.isPressed;

        /// <summary>Onde o mouse aponta no chão (y = 0). False se apontar para o céu.</summary>
        public bool PontoDoMouseNoChao(UnityEngine.Camera camera, out Vector3 ponto)
        {
            ponto = Vector3.zero;
            if (Mouse.current == null || camera == null) return false;
            Ray raio = camera.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (!new Plane(Vector3.up, Vector3.zero).Raycast(raio, out float distancia)) return false;
            ponto = raio.GetPoint(distancia);
            return true;
        }

        /// <summary>L1 (LB) ou Q: trocar para o parceiro da dupla.</summary>
        public bool ApertouTrocarJogador => !Conectado ? default : Controle != null
            ? Controle.leftShoulder.wasPressedThisFrame
            : Teclado.qKey.wasPressedThisFrame;

        /// <summary>
        /// Select/Share/View ou Tab: trocar de dupla.
        /// Só do protótipo, para testar sozinho os dois lados.
        /// </summary>
        public bool ApertouTrocarDupla => !Conectado ? default : Controle != null
            ? Controle.selectButton.wasPressedThisFrame
            : Teclado.tabKey.wasPressedThisFrame;
    }
}
