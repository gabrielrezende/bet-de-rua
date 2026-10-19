using UnityEngine;

namespace BetsDeRua
{
    /// <summary>
    /// A base de cada casinha, como riscada na rua. Visto de cima:
    /// - a casinha da garrafa: um quadradinho de giz com a garrafa no meio;
    /// - a área do taco: um retângulo de giz maior, encostado na frente da casinha
    ///   (do lado de onde vem o arremesso), atravessando a rua; a ponta do taco encosta nele;
    /// - a zona segura (invisível): os dois desenhos mais uma folga em volta. Com o rebatedor
    ///   nela, o taco fica no chão sozinho e a garrafa está protegida.
    /// Convenção do campo: as bases ficam no eixo X; a rua atravessa no eixo Z.
    /// </summary>
    public static class ZonaDoTaco
    {
        /// <summary>Para que lado (no eixo X, +1 ou -1) vem o arremesso, olhando da garrafa: o meio do campo.</summary>
        public static float ParaOArremesso(Vector3 posicaoGarrafa)
        {
            return posicaoGarrafa.x > 0f ? -1f : 1f;
        }

        /// <summary>Centro (no chão) do retângulo da área do taco da garrafa em 'posicaoGarrafa'.</summary>
        public static Vector3 CentroDaArea(Vector3 posicaoGarrafa, ConfigFisica config)
        {
            float distancia = config.ladoCasinhaGarrafa * 0.5f + config.profundidadeAreaTaco * 0.5f;
            return new Vector3(posicaoGarrafa.x + ParaOArremesso(posicaoGarrafa) * distancia, 0f, posicaoGarrafa.z);
        }

        /// <summary>True se 'ponto' (no chão) está na zona segura da garrafa em 'posicaoGarrafa'.</summary>
        public static bool NaZonaSegura(Vector3 ponto, Vector3 posicaoGarrafa, ConfigFisica config)
        {
            // Caixa que cobre a casinha e a área do taco, mais a folga
            float margem = config.margemZonaSegura;
            float paraOArremesso = ParaOArremesso(posicaoGarrafa);
            float atras = -config.ladoCasinhaGarrafa * 0.5f - margem;
            float frente = config.ladoCasinhaGarrafa * 0.5f + config.profundidadeAreaTaco + margem;
            float naDirecaoDoArremesso = (ponto.x - posicaoGarrafa.x) * paraOArremesso;
            float meiaLargura = Mathf.Max(config.larguraAreaTaco, config.ladoCasinhaGarrafa) * 0.5f + margem;
            return naDirecaoDoArremesso >= atras && naDirecaoDoArremesso <= frente
                && Mathf.Abs(ponto.z - posicaoGarrafa.z) <= meiaLargura;
        }

        /// <summary>Posição (no chão) da garrafa da base mais perto de 'ponto'.</summary>
        public static Vector3 GarrafaMaisPerto(Vector3 ponto, ConfigFisica config)
        {
            float meio = config.distanciaBases * 0.5f;
            return new Vector3(ponto.x < 0f ? -meio : meio, 0f, 0f);
        }

        /// <summary>True se 'ponto' está na zona segura de alguma das duas bases.</summary>
        public static bool NaZonaSeguraDeAlguma(Vector3 ponto, ConfigFisica config)
        {
            float meio = config.distanciaBases * 0.5f;
            return NaZonaSegura(ponto, new Vector3(-meio, 0f, 0f), config)
                || NaZonaSegura(ponto, new Vector3(meio, 0f, 0f), config);
        }
    }
}
