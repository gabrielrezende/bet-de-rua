using UnityEngine;

namespace BetsDeRua
{
    /// <summary>
    /// Os lugares onde se joga bets (GDD: rua, terrão, campinho, fazenda...). Cada um tem o seu chão:
    /// a terra segura a bola e quica torto, a grama freia e quica baixo, o asfalto deixa correr.
    /// Os números multiplicam os do ConfigFisica (1 = igual ao asfalto).
    /// </summary>
    [CreateAssetMenu(fileName = "ConfigMapas", menuName = "Bets/Config Mapas")]
    public class ConfigMapas : ScriptableObject
    {
        [System.Serializable]
        public class Mapa
        {
            [Tooltip("Nome do objeto do cenário na cena (Mapa_<nome>). 'Rua' usa a rua de asfalto.")]
            public string nome = "Rua";
            [Tooltip("Como aparece na tela no começo da partida.")]
            public string titulo = "RUA DO BAR DO ZÉ";
            [Tooltip("Chance de sair no sorteio (peso relativo).")]
            public float peso = 1f;
            [Tooltip("Tem calçada com meio-fio (a bola pega na quina e sobe)?")]
            public bool temCalcada = true;
            [Tooltip("Mostra o morro com casinhas ao fundo?")]
            public bool mostrarMorro = true;
            [Tooltip("Quique no chão (x quiqueAsfalto).")]
            public float quique = 1f;
            [Tooltip("Atrito no quique (x atritoQuique; menor = freia mais no quique).")]
            public float atritoQuique = 1f;
            [Tooltip("Atrito rolando (x atritoRolar; maior = a bola para antes).")]
            public float atritoRolar = 1f;
            [Tooltip("Quique no muro/cerca (x quiqueMuro).")]
            public float quiqueMuro = 1f;
            [Tooltip("Quique torto: desvio máximo para o lado a cada quique (graus). Terra irregular.")]
            public float desvioQuique = 0f;
        }

        [Tooltip("Sorteia o lugar a cada partida. Desligado: usa sempre o 'mapaFixo'.")]
        public bool sortear = true;
        [Tooltip("Lugar usado quando não sorteia (índice na lista).")]
        public int mapaFixo = 0;

        public Mapa[] mapas =
        {
            new Mapa { nome = "Rua", titulo = "RUA DO BAR DO ZÉ", peso = 1.4f },
            new Mapa { nome = "Terrao", titulo = "TERRÃO DA VILA", temCalcada = false, quique = 0.8f, atritoQuique = 0.88f, atritoRolar = 1.7f, quiqueMuro = 0.9f, desvioQuique = 9f },
            new Mapa { nome = "Campinho", titulo = "CAMPINHO DO BAIRRO", temCalcada = false, quique = 0.68f, atritoQuique = 0.84f, atritoRolar = 2.3f, quiqueMuro = 0.6f, desvioQuique = 3f },
            new Mapa { nome = "Fazenda", titulo = "FAZENDA DO TIO", temCalcada = false, mostrarMorro = false, quique = 0.58f, atritoQuique = 0.8f, atritoRolar = 2.9f, quiqueMuro = 0.75f, desvioQuique = 5f },
        };
    }

    /// <summary>O chão do lugar sorteado, lido pela física da bola a cada passo.</summary>
    public static class SuperficieAtual
    {
        public static float Quique = 1f;
        public static float AtritoQuique = 1f;
        public static float AtritoRolar = 1f;
        public static float QuiqueMuro = 1f;
        public static float DesvioQuique = 0f;
        public static bool TemCalcada = true;
        public static string Titulo = "";

        public static void Aplicar(ConfigMapas.Mapa m)
        {
            Quique = m.quique;
            AtritoQuique = m.atritoQuique;
            AtritoRolar = m.atritoRolar;
            QuiqueMuro = m.quiqueMuro;
            DesvioQuique = m.desvioQuique;
            TemCalcada = m.temCalcada;
            Titulo = m.titulo;
        }

        /// <summary>Volta ao asfalto (menu e cenas sem mapa).</summary>
        public static void Asfalto()
        {
            Quique = AtritoQuique = AtritoRolar = QuiqueMuro = 1f;
            DesvioQuique = 0f;
            TemCalcada = true;
            Titulo = "";
        }
    }
}
