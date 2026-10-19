using UnityEngine;

namespace BetsDeRua
{
    /// <summary>
    /// Um passo da física própria da bola: gravidade, resistência do ar, rolar com atrito,
    /// quique no chão e nos muros. Usada pela Bola de verdade e pela previsão de trajetória
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
            bool noChao = posicao.y <= raio + 0.0001f && velocidade.y <= 0f;

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
                    posicao.y = raio;
                    parada = true;
                    return;
                }
                velocidade = horizontal * (novaRapidez / rapidezH);
            }

            posicao += velocidade * dt;
            ColidirComChao(config, ref posicao, ref velocidade);
            ColidirComMuros(config, ref posicao, ref velocidade);
        }

        private static void ColidirComChao(ConfigFisica config, ref Vector3 posicao, ref Vector3 velocidade)
        {
            float raio = config.raioBola;
            if (posicao.y >= raio) return;
            posicao.y = raio;
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
