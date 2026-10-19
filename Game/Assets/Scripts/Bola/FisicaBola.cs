using UnityEngine;

namespace BetsDeRua
{
    /// <summary>
    /// Um passo da física própria da bola: gravidade, resistência do ar, rolar com atrito,
    /// quique no chão (asfalto e calçada), no meio-fio e nas fachadas. Usada pela Bola de verdade e pela previsão de trajetória
    /// (para o rebatedor saber quando e onde a bola vai passar). As garrafas ficam de fora:
    /// quem cuida delas é a Bola.
    /// </summary>
    public static class FisicaBola
    {
        /// <summary>Avança a bola 'dt' segundos. 'parada' vira true quando ela para de rolar.</summary>
        public static void Passo(ConfigFisica config, ref Vector3 posicao, ref Vector3 velocidade, ref bool parada, float dt)
        {
            if (parada) return;
            float raio = config.raioBola;
            float chao = AlturaDoChao(config, posicao.z);
            bool noChao = posicao.y <= chao + raio + 0.0001f && velocidade.y <= 0f;

            // Gravidade (só no ar) e resistência do ar
            if (!noChao) velocidade.y -= config.gravidade * dt;
            float rapidez = velocidade.magnitude;
            velocidade -= velocidade * (config.arrastoAr * rapidez * dt);

            // Rolando: atrito do chão freia a bola
            if (noChao)
            {
                velocidade.y = 0f;
                Vector3 horizontal = new Vector3(velocidade.x, 0f, velocidade.z);
                float rapidezH = horizontal.magnitude;
                float novaRapidez = rapidezH - config.atritoRolar * dt;
                if (novaRapidez <= config.velocidadeParada)
                {
                    velocidade = Vector3.zero;
                    posicao.y = chao + raio;
                    parada = true;
                    return;
                }
                velocidade = horizontal * (novaRapidez / rapidezH);
            }

            posicao += velocidade * dt;
            ColidirComMeioFio(config, ref posicao, ref velocidade);
            ColidirComChao(config, ref posicao, ref velocidade);
            ColidirComMuros(config, ref posicao, ref velocidade);
        }

        /// <summary>Altura do chão (m) nesta posição: 0 no asfalto, a altura da calçada fora da rua.</summary>
        public static float AlturaDoChao(ConfigFisica config, float z)
        {
            return Mathf.Abs(z) > config.larguraRua * 0.5f ? config.alturaCalcada : 0f;
        }

        /// <summary>
        /// Meio-fio: a bola indo da rua para a calçada abaixo da altura dela bate e volta; na altura
        /// da quina, pula para cima (sobe e perde força).
        /// </summary>
        private static void ColidirComMeioFio(ConfigFisica config, ref Vector3 posicao, ref Vector3 velocidade)
        {
            float raio = config.raioBola;
            float meiaRua = config.larguraRua * 0.5f;
            float topo = config.alturaCalcada;
            if (Mathf.Abs(posicao.z) + raio <= meiaRua || posicao.z * velocidade.z <= 0f) return;
            if (Mathf.Abs(posicao.z) > meiaRua + raio) return;       // já está em cima da calçada
            if (posicao.y - raio >= topo) return;                    // passa por cima
            if (posicao.y < topo - raio * 0.5f)
            {
                // Bate na face do meio-fio e volta para a rua
                posicao.z = Mathf.Sign(posicao.z) * (meiaRua - raio);
                velocidade.z = -velocidade.z * config.quiqueMeioFio;
                velocidade.x *= config.quiqueMeioFio + (1f - config.quiqueMeioFio) * 0.5f;
            }
            else
            {
                // Pega na quina: pula para cima da calçada, perdendo força para o lado
                velocidade.y = Mathf.Max(velocidade.y, Mathf.Abs(velocidade.z) * 0.6f);
                velocidade.z *= config.quiqueMeioFio;
                posicao.y = topo + raio;
            }
        }

        private static void ColidirComChao(ConfigFisica config, ref Vector3 posicao, ref Vector3 velocidade)
        {
            float raio = config.raioBola;
            float chao = AlturaDoChao(config, posicao.z);
            if (posicao.y >= chao + raio) return;
            posicao.y = chao + raio;
            if (velocidade.y >= 0f) return;

            if (-velocidade.y > config.velocidadeMinimaQuique)
            {
                // Quique: perde parte da velocidade vertical e um pouco da horizontal
                velocidade.y = -velocidade.y * config.quiqueAsfalto;
                velocidade.x *= config.atritoQuique;
                velocidade.z *= config.atritoQuique;
            }
            else
            {
                // Quique fraco demais: começa a rolar
                velocidade.y = 0f;
            }
        }

        private static void ColidirComMuros(ConfigFisica config, ref Vector3 posicao, ref Vector3 velocidade)
        {
            // Por enquanto a bola sempre volta no limite do campo.
            // ("Bola no mato", por cima do muro, fica para depois.)
            float raio = config.raioBola;
            float limiteX = config.comprimentoCampo * 0.5f - raio;
            float limiteZ = config.larguraCampo * 0.5f - raio;

            if (Mathf.Abs(posicao.x) > limiteX && posicao.x * velocidade.x > 0f)
            {
                posicao.x = Mathf.Sign(posicao.x) * limiteX;
                velocidade.x = -velocidade.x;
                velocidade *= config.quiqueMuro;
            }
            if (Mathf.Abs(posicao.z) > limiteZ && posicao.z * velocidade.z > 0f)
            {
                posicao.z = Mathf.Sign(posicao.z) * limiteZ;
                velocidade.z = -velocidade.z;
                velocidade *= config.quiqueMuro;
            }
        }
    }
}
