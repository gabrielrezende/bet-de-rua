using UnityEngine;

namespace BetsDeRua
{
    /// <summary>
    /// Contas de trajetória da bola no ar, com gravidade e resistência do ar
    /// iguais às da Bola. Usadas para mirar: "com esta velocidade na horizontal,
    /// quanto preciso subir para chegar lá na altura certa?".
    /// </summary>
    public static class Trajetoria
    {
        private const float Passo = 0.005f;
        private const float SubidaMinima = -10f;
        private const float SubidaMaxima = 40f;
        private const int Tentativas = 25;

        /// <summary>
        /// Velocidade vertical inicial (m/s) para a bola, saindo com 'rapidezHorizontal' (m/s),
        /// chegar a 'distancia' metros com 'diferencaAltura' em relação à saída.
        /// Busca binária simulando o voo.
        /// </summary>
        public static float AcharSubida(ConfigFisica config, float distancia, float rapidezHorizontal, float diferencaAltura)
        {
            float minimo = SubidaMinima, maximo = SubidaMaxima;
            for (int i = 0; i < Tentativas; i++)
            {
                float meio = (minimo + maximo) * 0.5f;
                if (AlturaAoChegar(config, distancia, rapidezHorizontal, meio) < diferencaAltura) minimo = meio;
                else maximo = meio;
            }
            return (minimo + maximo) * 0.5f;
        }

        /// <summary>
        /// Velocidade vertical inicial para um arremesso quicado: a bola sai de 'alturaSaida',
        /// quica no chão e chega a 'distancia' metros na altura 'alturaChegada'.
        /// O quique acontece no máximo 'distanciaQuiqueMaxima' antes do alvo; mais perto se
        /// for preciso para a bola não passar por cima.
        /// </summary>
        public static float AcharSubidaQuicado(ConfigFisica config, float distancia, float rapidezHorizontal,
                                               float alturaSaida, float alturaChegada, float distanciaQuiqueMaxima)
        {
            float quiqueMaximo = Mathf.Clamp(distanciaQuiqueMaxima, 0f, distancia - 0.5f);
            if (AlturaComQuique(config, distancia, quiqueMaximo, rapidezHorizontal, alturaSaida) <= alturaChegada)
                return SubidaParaQuicarEm(config, distancia - quiqueMaximo, rapidezHorizontal, alturaSaida);

            // Quique perto do alvo = chega baixo; longe = chega alto. Procura o ponto certo no meio.
            float minimo = 0f, maximo = quiqueMaximo;
            for (int i = 0; i < Tentativas; i++)
            {
                float meio = (minimo + maximo) * 0.5f;
                if (AlturaComQuique(config, distancia, meio, rapidezHorizontal, alturaSaida) < alturaChegada) minimo = meio;
                else maximo = meio;
            }
            return SubidaParaQuicarEm(config, distancia - (minimo + maximo) * 0.5f, rapidezHorizontal, alturaSaida);
        }

        private static float SubidaParaQuicarEm(ConfigFisica config, float distanciaQuique, float vh, float alturaSaida)
        {
            return AcharSubida(config, distanciaQuique, vh, config.raioBola - alturaSaida);
        }

        /// <summary>Altura (absoluta) da bola no alvo, quicando 'antesDoAlvo' metros antes dele.</summary>
        private static float AlturaComQuique(ConfigFisica config, float distancia, float antesDoAlvo, float vh, float alturaSaida)
        {
            float vy = SubidaParaQuicarEm(config, distancia - antesDoAlvo, vh, alturaSaida);
            float x = 0f, y = alturaSaida;
            bool quicou = false;
            for (int i = 0; i < 8000 && x < distancia; i++)
            {
                float rapidez = Mathf.Sqrt(vh * vh + vy * vy);
                float freio = config.arrastoAr * rapidez * Passo;
                vh -= vh * freio;
                vy -= vy * freio + config.gravidade * Passo;
                x += vh * Passo;
                y += vy * Passo;
                if (y < config.raioBola && vy < 0f)
                {
                    if (quicou) return config.raioBola; // segundo quique antes do alvo: chega rasteira
                    // Mesmo quique da Bola: perde parte da velocidade vertical e um pouco da horizontal
                    y = config.raioBola;
                    vy = -vy * config.quiqueAsfalto;
                    vh *= config.atritoQuique;
                    quicou = true;
                }
                if (vh <= 0.01f) return config.raioBola;
            }
            return y;
        }

        /// <summary>Altura (relativa à saída) quando a bola anda 'distancia' metros na horizontal.</summary>
        private static float AlturaAoChegar(ConfigFisica config, float distancia, float vh, float vy)
        {
            float x = 0f, y = 0f;
            for (int i = 0; i < 4000 && x < distancia; i++)
            {
                float rapidez = Mathf.Sqrt(vh * vh + vy * vy);
                float freio = config.arrastoAr * rapidez * Passo;
                vh -= vh * freio;
                vy -= vy * freio + config.gravidade * Passo;
                x += vh * Passo;
                y += vy * Passo;
                if (vh <= 0.01f) return float.NegativeInfinity; // nem chega lá
            }
            return y;
        }
    }
}
