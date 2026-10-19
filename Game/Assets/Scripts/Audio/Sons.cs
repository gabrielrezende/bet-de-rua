using System.Collections.Generic;
using UnityEngine;

namespace BetsDeRua
{
    public enum TipoSom
    {
        Batida, Quique, Parede, Garrafa, Balanco, Tec, Arremesso, GiroTaco, Pegou,
        Ponto, Queimou, Vitoria, MenuMover, MenuConfirmar, MenuVoltar, Start, Passaro
    }

    /// <summary>
    /// Os sons do jogo, sintetizados por código na primeira vez que tocam (sem arquivos de áudio):
    /// estalo de madeira do taco na bola, quique da bola de borracha no asfalto, a garrafa PET
    /// caindo e rolando, o "TEC!" dos tacos na cruzada, o vento do arremesso, a vinheta do ponto,
    /// a da queimada, a da vitória, os cliques do menu e os passarinhos do fundo.
    /// Toca com Sons.Tocar(tipo, volume); o volume geral vem dos Ajustes.
    /// </summary>
    public static class Sons
    {
        private const int Taxa = 44100;
        private static readonly Dictionary<TipoSom, AudioClip> clipes = new Dictionary<TipoSom, AudioClip>();
        private static AudioSource[] fontes;
        private static int proxima;
        private static readonly Dictionary<TipoSom, float> ultimoToque = new Dictionary<TipoSom, float>();
        private static System.Random sorteio = new System.Random(5);

        public static void Tocar(TipoSom tipo, float volume = 1f, float variacaoTom = 0.06f)
        {
            if (!Application.isPlaying || volume <= 0.01f) return;
            // Não empilha o mesmo som no mesmo instante (ex.: vários quiques no mesmo quadro)
            if (ultimoToque.TryGetValue(tipo, out float ultimo) && Time.unscaledTime - ultimo < 0.04f) return;
            ultimoToque[tipo] = Time.unscaledTime;
            Garantir();
            AudioSource fonte = fontes[proxima];
            proxima = (proxima + 1) % fontes.Length;
            fonte.clip = Clipe(tipo);
            fonte.volume = Mathf.Clamp01(volume);
            fonte.pitch = 1f + ((float)sorteio.NextDouble() * 2f - 1f) * variacaoTom;
            fonte.Play();
        }

        private static void Garantir()
        {
            if (fontes != null && fontes[0] != null) return;
            GameObject g = new GameObject("Sons") { hideFlags = HideFlags.HideInHierarchy };
            Object.DontDestroyOnLoad(g);
            fontes = new AudioSource[14];
            for (int i = 0; i < fontes.Length; i++)
            {
                fontes[i] = g.AddComponent<AudioSource>();
                fontes[i].playOnAwake = false;
                fontes[i].spatialBlend = 0f;
                fontes[i].ignoreListenerPause = true;
            }
        }

        public static AudioClip Clipe(TipoSom tipo)
        {
            if (clipes.TryGetValue(tipo, out AudioClip c) && c != null) return c;
            float[] dados = Sintetizar(tipo);
            c = AudioClip.Create(tipo.ToString(), dados.Length, 1, Taxa, false);
            c.SetData(dados, 0);
            clipes[tipo] = c;
            return c;
        }

        // ------------------------------------------------------------------ síntese

        private static float[] Sintetizar(TipoSom tipo)
        {
            System.Random r = new System.Random((int)tipo * 7 + 3);
            switch (tipo)
            {
                case TipoSom.Batida: return Madeira(r, 0.25f, 920f, 1.9f, 0.05f, 0.9f, true);
                case TipoSom.Tec:
                {
                    float[] a = Madeira(r, 0.3f, 1350f, 2.3f, 0.035f, 0.8f, false);
                    float[] tudo = new float[(int)(Taxa * 0.32f)];
                    Somar(tudo, a, 0);
                    Somar(tudo, a, (int)(Taxa * 0.11f), 0.8f);
                    return tudo;
                }
                case TipoSom.Quique: return Baque(r, 0.12f, 190f, 90f, 0.045f, 0.5f);
                case TipoSom.Parede: return Baque(r, 0.18f, 140f, 70f, 0.07f, 0.7f);
                case TipoSom.Balanco: return Madeira(r, 0.15f, 700f, 1.6f, 0.03f, 0.35f, false);
                case TipoSom.Pegou: return Baque(r, 0.12f, 160f, 110f, 0.05f, 0.35f);
                case TipoSom.Garrafa:
                {
                    // Garrafa PET: "poc" oco e uns quiques de plástico rolando
                    float[] tudo = new float[(int)(Taxa * 0.7f)];
                    Somar(tudo, Plastico(r, 0.2f, 520f, 0.08f, 0.9f), 0);
                    float t = 0.16f, v = 0.55f;
                    for (int i = 0; i < 5; i++)
                    {
                        Somar(tudo, Plastico(r, 0.12f, 600f + i * 40f, 0.04f, v), (int)(Taxa * t));
                        t += 0.09f - i * 0.012f;
                        v *= 0.7f;
                    }
                    return tudo;
                }
                case TipoSom.Arremesso: return Vento(r, 0.28f, 0.35f);
                case TipoSom.GiroTaco: return Vento(r, 0.2f, 0.45f);
                case TipoSom.Ponto: return Vinheta(r, new[] { 523f, 659f, 784f, 1047f }, 0.09f, 0.22f, 0.75f);
                case TipoSom.Queimou: return Vinheta(r, new[] { 392f, 330f, 262f }, 0.14f, 0.35f, 0.4f);
                case TipoSom.Vitoria: return Vinheta(r, new[] { 523f, 659f, 784f, 1047f, 784f, 1047f, 1319f }, 0.11f, 0.5f, 1f);
                case TipoSom.MenuMover: return Bip(0.03f, 1900f, 0.25f);
                case TipoSom.MenuConfirmar:
                {
                    float[] tudo = new float[(int)(Taxa * 0.14f)];
                    Somar(tudo, Bip(0.05f, 880f, 0.35f), 0);
                    Somar(tudo, Bip(0.07f, 1320f, 0.35f), (int)(Taxa * 0.05f));
                    return tudo;
                }
                case TipoSom.MenuVoltar:
                {
                    float[] tudo = new float[(int)(Taxa * 0.14f)];
                    Somar(tudo, Bip(0.05f, 1100f, 0.3f), 0);
                    Somar(tudo, Bip(0.07f, 740f, 0.3f), (int)(Taxa * 0.05f));
                    return tudo;
                }
                case TipoSom.Start: return Vinheta(r, new[] { 392f, 523f, 784f }, 0.08f, 0.15f, 0.6f);
                case TipoSom.Passaro: return Passaro(r);
            }
            return new float[1];
        }

        /// <summary>Madeira batendo: estalo curto + ressonância (taco na bola, tacos em X).</summary>
        private static float[] Madeira(System.Random r, float duracao, float freq, float harmonico, float decaimento, float volume, bool grave)
        {
            int n = (int)(Taxa * duracao);
            float[] s = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Taxa;
                float env = Mathf.Exp(-t / decaimento);
                float ruido = ((float)r.NextDouble() * 2f - 1f) * Mathf.Exp(-t / 0.004f);
                float tom = Mathf.Sin(2f * Mathf.PI * freq * t) * 0.6f + Mathf.Sin(2f * Mathf.PI * freq * harmonico * t) * 0.3f;
                float baixo = grave ? Mathf.Sin(2f * Mathf.PI * 170f * t) * Mathf.Exp(-t / 0.07f) * 0.6f : 0f;
                s[i] = (ruido * 0.8f + tom * env + baixo) * volume * 0.6f;
            }
            return s;
        }

        /// <summary>Baque de borracha: tom grave que cai rápido + um clique.</summary>
        private static float[] Baque(System.Random r, float duracao, float freqInicio, float freqFim, float decaimento, float volume)
        {
            int n = (int)(Taxa * duracao);
            float[] s = new float[n];
            float fase = 0f;
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Taxa;
                float f = Mathf.Lerp(freqInicio, freqFim, t / duracao);
                fase += 2f * Mathf.PI * f / Taxa;
                float env = Mathf.Exp(-t / decaimento);
                float clique = ((float)r.NextDouble() * 2f - 1f) * Mathf.Exp(-t / 0.002f) * 0.4f;
                s[i] = (Mathf.Sin(fase) * env + clique) * volume;
            }
            return s;
        }

        private static float[] Plastico(System.Random r, float duracao, float freq, float decaimento, float volume)
        {
            int n = (int)(Taxa * duracao);
            float[] s = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Taxa;
                float env = Mathf.Exp(-t / decaimento);
                float tom = Mathf.Sin(2f * Mathf.PI * freq * t) * 0.5f + Mathf.Sin(2f * Mathf.PI * freq * 2.7f * t) * 0.25f
                          + Mathf.Sin(2f * Mathf.PI * freq * 4.1f * t) * 0.12f;
                float ruido = ((float)r.NextDouble() * 2f - 1f) * Mathf.Exp(-t / 0.006f) * 0.5f;
                s[i] = (tom * env + ruido) * volume * 0.6f;
            }
            return s;
        }

        /// <summary>Vento (arremesso, giro do taco): ruído filtrado que sobe e desce.</summary>
        private static float[] Vento(System.Random r, float duracao, float volume)
        {
            int n = (int)(Taxa * duracao);
            float[] s = new float[n];
            float filtrado = 0f;
            for (int i = 0; i < n; i++)
            {
                float u = i / (float)n;
                float env = Mathf.Sin(u * Mathf.PI) * Mathf.Sin(u * Mathf.PI);
                float corte = Mathf.Lerp(0.05f, 0.35f, env);
                filtrado += (((float)r.NextDouble() * 2f - 1f) - filtrado) * corte;
                s[i] = filtrado * env * volume;
            }
            return s;
        }

        /// <summary>Vinheta de notas (estilo jogo de esporte): cada nota com timbre de metal leve, a última segura.</summary>
        private static float[] Vinheta(System.Random r, float[] notas, float duracaoNota, float sustentarUltima, float volume)
        {
            int n = (int)(Taxa * (duracaoNota * notas.Length + sustentarUltima + 0.25f));
            float[] s = new float[n];
            for (int k = 0; k < notas.Length; k++)
            {
                bool ultima = k == notas.Length - 1;
                int inicio = (int)(Taxa * duracaoNota * k);
                float dur = ultima ? duracaoNota + sustentarUltima : duracaoNota * 1.4f;
                int tam = (int)(Taxa * dur);
                for (int i = 0; i < tam && inicio + i < n; i++)
                {
                    float t = i / (float)Taxa;
                    float ataque = Mathf.Clamp01(t / 0.008f);
                    float env = ataque * Mathf.Exp(-t / (ultima ? dur * 0.6f : 0.08f));
                    float f = notas[k];
                    float v = Mathf.Sin(2f * Mathf.PI * f * t) * 0.55f + Mathf.Sin(2f * Mathf.PI * f * 2f * t) * 0.22f
                            + Mathf.Sin(2f * Mathf.PI * f * 3f * t) * 0.12f;
                    s[inicio + i] += v * env * volume * 0.45f;
                }
            }
            return s;
        }

        private static float[] Bip(float duracao, float freq, float volume)
        {
            int n = (int)(Taxa * duracao);
            float[] s = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)Taxa;
                float env = Mathf.Clamp01(t / 0.003f) * Mathf.Exp(-t / (duracao * 0.4f));
                s[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * env * volume;
            }
            return s;
        }

        /// <summary>Passarinho: três piados rápidos subindo de tom.</summary>
        private static float[] Passaro(System.Random r)
        {
            int n = (int)(Taxa * 0.5f);
            float[] s = new float[n];
            float fase = 0f;
            for (int p = 0; p < 3; p++)
            {
                int inicio = (int)(Taxa * (0.04f + p * 0.13f));
                int tam = (int)(Taxa * 0.07f);
                for (int i = 0; i < tam && inicio + i < n; i++)
                {
                    float u = i / (float)tam;
                    float f = Mathf.Lerp(3200f, 4800f, u) + Mathf.Sin(u * 40f) * 300f;
                    fase += 2f * Mathf.PI * f / Taxa;
                    s[inicio + i] += Mathf.Sin(fase) * Mathf.Sin(u * Mathf.PI) * 0.25f;
                }
            }
            return s;
        }

        private static void Somar(float[] destino, float[] fonte, int inicio, float volume = 1f)
        {
            for (int i = 0; i < fonte.Length && inicio + i < destino.Length; i++) destino[inicio + i] += fonte[i] * volume;
        }

        /// <summary>Barulho de fundo da rua (trânsito longe), para tocar em loop. 6 s, emenda sem estalo.</summary>
        public static AudioClip FundoDaRua()
        {
            const float duracao = 6f;
            int n = (int)(Taxa * duracao);
            float[] s = new float[n];
            System.Random r = new System.Random(77);
            float a = 0f, b = 0f;
            for (int i = 0; i < n; i++)
            {
                float branco = (float)r.NextDouble() * 2f - 1f;
                a += (branco - a) * 0.02f;   // grave (motor longe)
                b += (a - b) * 0.05f;
                float onda = 0.7f + 0.3f * Mathf.Sin(i / (float)n * Mathf.PI * 2f * 2f);
                s[i] = b * 2.2f * onda;
            }
            // Emenda: mistura o fim com o começo
            int mistura = Taxa / 2;
            for (int i = 0; i < mistura; i++)
            {
                float u = i / (float)mistura;
                s[n - mistura + i] = Mathf.Lerp(s[n - mistura + i], s[i], u);
            }
            AudioClip c = AudioClip.Create("FundoRua", n, 1, Taxa, false);
            c.SetData(s, 0);
            return c;
        }
    }

    /// <summary>Clima de rua: barulho de fundo em loop e passarinhos de vez em quando.</summary>
    public class AmbienteRua : MonoBehaviour
    {
        [SerializeField, Range(0f, 1f)] private float volumeFundo = 0.18f;
        private float proximoPassaro;

        private void Start()
        {
            AudioSource fundo = gameObject.AddComponent<AudioSource>();
            fundo.clip = Sons.FundoDaRua();
            fundo.loop = true;
            fundo.volume = volumeFundo;
            fundo.spatialBlend = 0f;
            fundo.Play();
            proximoPassaro = Time.time + Random.Range(2f, 5f);
        }

        private void Update()
        {
            if (Time.time < proximoPassaro) return;
            proximoPassaro = Time.time + Random.Range(4f, 11f);
            Sons.Tocar(TipoSom.Passaro, Random.Range(0.12f, 0.3f), 0.15f);
        }
    }
}
