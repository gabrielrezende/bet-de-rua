using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BetsDeRua
{
    /// <summary>
    /// Vibração do controle (como no FIFA): um tranco curto na batida, um toque leve ao pegar a
    /// bola e um tremor mais longo na queimada. Para a vibração sozinha depois do tempo.
    /// </summary>
    public class Vibracao : MonoBehaviour
    {
        private static Vibracao instancia;
        private readonly Dictionary<Gamepad, float> ate = new Dictionary<Gamepad, float>();

        public static void Pulsar(Gamepad controle, float grave, float agudo, float segundos)
        {
            if (controle == null || !Application.isPlaying) return;
            if (instancia == null)
            {
                GameObject g = new GameObject("Vibracao") { hideFlags = HideFlags.HideInHierarchy };
                DontDestroyOnLoad(g);
                instancia = g.AddComponent<Vibracao>();
            }
            controle.SetMotorSpeeds(Mathf.Clamp01(grave), Mathf.Clamp01(agudo));
            instancia.ate[controle] = Time.unscaledTime + segundos;
        }

        /// <summary>Vibra o controle de quem controla este jogador (se for uma pessoa com controle).</summary>
        public static void Pulsar(Jogador jogador, float grave, float agudo, float segundos)
        {
            if (jogador != null && jogador.Controlador != null) Pulsar(jogador.Controlador.Controle, grave, agudo, segundos);
        }

        /// <summary>Vibra todos os controles conectados (lances que todo mundo sente, como a queimada).</summary>
        public static void PulsarTodos(float grave, float agudo, float segundos)
        {
            foreach (Gamepad g in Gamepad.all) Pulsar(g, grave, agudo, segundos);
        }

        private void Update()
        {
            if (ate.Count == 0) return;
            var acabaram = new List<Gamepad>();
            foreach (var par in ate)
                if (Time.unscaledTime >= par.Value) acabaram.Add(par.Key);
            foreach (Gamepad g in acabaram)
            {
                if (g != null && g.added) g.SetMotorSpeeds(0f, 0f);
                ate.Remove(g);
            }
        }

        private void OnDisable()
        {
            foreach (Gamepad g in Gamepad.all) g.SetMotorSpeeds(0f, 0f);
        }
    }
}
