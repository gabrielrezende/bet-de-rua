using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace BetsDeRua
{
    /// <summary>
    /// Log da partida, para entender depois o que aconteceu, em ordem de tempo.
    /// Grava em Game/Logs/Partida.log (a pasta Logs não vai para o Git):
    /// - EVENTO: tudo que o jogo anuncia (arremesso, batida, pegou, lancha, queimou, troca...)
    ///   e as decisões dos bots;
    /// - FOTO: a cada ConfigPartida.intervaloFotoLog segundos, todos os jogadores (posição,
    ///   papel, quem controla, zona, taco) e a bola.
    /// Liga e desliga em ConfigPartida.gravarLog. Cada Play começa um arquivo novo.
    /// </summary>
    public class RegistroPartida : MonoBehaviour
    {
        [SerializeField] private ConfigPartida configPartida;
        [SerializeField] private ControladorLance lance;
        [SerializeField] private Bola bola;
        [SerializeField] private Jogador[] jogadores;

        private static RegistroPartida instancia;
        private StreamWriter arquivo;
        private float proximaFoto;

        private void Awake()
        {
            instancia = this;
            if (!configPartida.gravarLog) return;
            string pasta = Path.Combine(Application.dataPath, "..", "Logs");
            Directory.CreateDirectory(pasta);
            arquivo = new StreamWriter(Path.Combine(pasta, "Partida.log"), false, new UTF8Encoding(false)) { AutoFlush = true };
            arquivo.WriteLine($"# Partida começou em {System.DateTime.Now:dd/MM/yyyy HH:mm:ss}");
        }

        private void OnDestroy()
        {
            if (instancia == this) instancia = null;
            arquivo?.Dispose();
            arquivo = null;
        }

        /// <summary>Anota um evento (qualquer script pode chamar).</summary>
        public static void Evento(string texto)
        {
            if (instancia == null || instancia.arquivo == null) return;
            instancia.Escrever("EVENTO", texto);
        }

        private void Escrever(string tipo, string texto)
        {
            string estado = lance != null ? lance.Estado.ToString() : "-";
            arquivo.WriteLine($"{Time.time,8:0.00}s  {tipo,-6} [{estado}] {texto}");
        }

        private void Update()
        {
            if (arquivo == null || Time.time < proximaFoto) return;
            proximaFoto = Time.time + Mathf.Max(0.1f, configPartida.intervaloFotoLog);

            var foto = new StringBuilder();
            foreach (Jogador j in jogadores)
            {
                if (j == null) continue;
                Vector3 p = j.transform.position;
                string quem = j.Controlador != null ? $"P{j.Controlador.Numero}" : "bot";
                foto.Append($"{j.Nome}({quem}) ({p.x:0.0};{p.z:0.0}) v={j.Velocidade.magnitude:0.0}");
                if (j.Papel == Papel.Rebatedor)
                {
                    Taco taco = j.GetComponentInChildren<Taco>();
                    foto.Append(j.DentroDoCirculo() ? " zona" : " FORA");
                    foto.Append(taco != null && taco.NoChao ? " taco-chão" : " taco-alto");
                }
                else
                {
                    Arremessador a = j.GetComponent<Arremessador>();
                    if (a != null && a.ComBola) foto.Append($" COM-BOLA({a.Modo})");
                    if (j.Mergulhando) foto.Append(" mergulhando");
                }
                foto.Append(" | ");
            }
            if (bola != null)
            {
                Vector3 b = bola.Posicao;
                foto.Append($"bola ({b.x:0.0};{b.y:0.0};{b.z:0.0}) {bola.Velocidade.magnitude * 3.6f:0}km/h");
                foto.Append(bola.Parada ? " parada" : "");
            }
            Escrever("FOTO", foto.ToString());
        }
    }
}
