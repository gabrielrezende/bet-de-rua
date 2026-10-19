using System.Collections.Generic;
using UnityEngine;

namespace BetsDeRua
{
    /// <summary>
    /// Quem chega antes na bola? Prevê a trajetória e, para cada defensor, acha o primeiro
    /// momento em que ele (correndo em sprint) alcança a bola numa altura que dá para pegar.
    /// Usado para escolher quem vai na bola (bots e troca automática de quem você controla):
    /// não é quem está mais perto da bola agora, e sim quem chega primeiro onde ela vai estar.
    /// </summary>
    public static class Interceptacao
    {
        // Previsão mais longa e mais espaçada que a da batida: aqui só importa o caminho da bola
        private const float DuracaoPrevisao = 5f;
        private const float PassoPrevisao = 0.05f;
        // Tempo para reagir e acelerar antes de chegar no sprint (s)
        private const float TempoParaArrancar = 0.25f;

        private static readonly List<Vector3> previsao = new List<Vector3>();

        /// <summary>Atualiza a previsão da bola (chamar uma vez antes de várias consultas).</summary>
        public static void Prever(Bola bola)
        {
            bola.PreverTrajetoria(DuracaoPrevisao, PassoPrevisao, previsao);
        }

        /// <summary>
        /// Em quanto tempo (s) este jogador alcança a bola, e onde. Usa a última previsão.
        /// </summary>
        public static float TempoParaAlcancar(ConfigFisica config, Vector3 posicaoJogador, out Vector3 ponto)
        {
            ponto = previsao.Count > 0 ? previsao[previsao.Count - 1] : posicaoJogador;
            for (int i = 0; i < previsao.Count; i++)
            {
                float t = i * PassoPrevisao;
                Vector3 p = previsao[i];
                if (p.y > config.alturaMaximaPegar) continue;
                float distancia = new Vector2(p.x - posicaoJogador.x, p.z - posicaoJogador.z).magnitude - config.raioPegar;
                float alcanceCorrendo = config.velocidadeSprint * Mathf.Max(0f, t - TempoParaArrancar);
                if (distancia <= alcanceCorrendo)
                {
                    ponto = p;
                    return t;
                }
            }
            // Não alcança enquanto a bola anda: vai até onde ela para
            float distanciaFinal = new Vector2(ponto.x - posicaoJogador.x, ponto.z - posicaoJogador.z).magnitude;
            return previsao.Count * PassoPrevisao + distanciaFinal / Mathf.Max(0.1f, config.velocidadeSprint);
        }

        /// <summary>O defensor que chega antes na bola (e onde). Chama Prever antes.</summary>
        public static Defensor QuemChegaAntes(ConfigFisica config, Defensor[] defensores, out Vector3 ponto, out float tempo)
        {
            Defensor melhor = null;
            ponto = Vector3.zero;
            tempo = float.MaxValue;
            foreach (Defensor defensor in defensores)
            {
                float t = TempoParaAlcancar(config, defensor.transform.position, out Vector3 p);
                if (t < tempo) { melhor = defensor; tempo = t; ponto = p; }
            }
            return melhor;
        }
    }
}
