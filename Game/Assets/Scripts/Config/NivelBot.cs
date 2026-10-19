using UnityEngine;

namespace BetsDeRua
{
    /// <summary>Os níveis dos bots. Cada time (Lado 1 e Lado 2) tem o seu.</summary>
    public enum NivelDificuldade { Facil, Medio, Dificil }

    /// <summary>
    /// O "talento" de um bot: quanto ele erra e quanto arrisca. Não muda as regras, só a habilidade.
    /// Fica no ConfigPartida (niveisBots), um por nível, ajustável com o jogo rodando.
    /// </summary>
    [System.Serializable]
    public class NivelBot
    {
        public string nome = "Médio";
        [Tooltip("Batida: erro de tempo, para mais ou para menos (ms). A janela boa é ±janelaBoaMs; passou dela, erra.")]
        public float erroBatidaMs = 230f;
        [Tooltip("Arremesso: até quanto (m) a mira sai para o lado da garrafa, ao acaso.")]
        public float desvioArremesso = 0.8f;
        [Tooltip("Chute na garrafa (no campo): até quanto (m) a mira sai para o lado, ao acaso. Com ~0,23 m a bola ainda acerta.")]
        public float desvioChute = 0.35f;
        [Tooltip("Tempo de reação (s) do defensor antes de pegar, passar ou chutar.")]
        public float reacao = 0.25f;
        [Tooltip("Corrida: só sai se a defesa demorar este tanto (s) a mais para queimar do que eles para chegar. " +
                 "Negativo = arrisca; positivo = cauteloso.")]
        public float margemRiscoCorrida = -0.5f;
        [Tooltip("Batida: chance (0 a 1) de ler certo a altura da bola (taco alto ou baixo).")]
        [Range(0f, 1f)] public float acertoAlturaBatida = 0.8f;

        public NivelBot() { }

        public NivelBot(string nome, float erroBatidaMs, float desvioArremesso, float desvioChute, float reacao,
                        float margemRiscoCorrida, float acertoAlturaBatida)
        {
            this.acertoAlturaBatida = acertoAlturaBatida;
            this.nome = nome;
            this.erroBatidaMs = erroBatidaMs;
            this.desvioArremesso = desvioArremesso;
            this.desvioChute = desvioChute;
            this.reacao = reacao;
            this.margemRiscoCorrida = margemRiscoCorrida;
        }
    }

    /// <summary>Acha o nível do time de um jogador (o time muda de dupla na troca de papel).</summary>
    public static class NiveisBots
    {
        /// <summary>Quem controla o lance (para saber o lado de cada jogador). O ControladorLance se registra aqui.</summary>
        public static ControladorLance Lance { get; set; }

        public static NivelBot De(ConfigPartida config, Jogador jogador)
        {
            int lado = Lance != null && jogador != null ? Lance.LadoDe(jogador) : 0;
            NivelDificuldade nivel = lado == 0 ? config.nivelLado1 : config.nivelLado2;
            int indice = Mathf.Clamp((int)nivel, 0, config.niveisBots.Length - 1);
            return config.niveisBots.Length > 0 ? config.niveisBots[indice] : new NivelBot();
        }
    }
}
