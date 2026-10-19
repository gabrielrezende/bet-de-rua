using System;
using System.IO;
using UnityEngine;

namespace BetsDeRua
{
    /// <summary>
    /// Os personagens das pessoas (4 vagas: P1 a P4), salvos num arquivo JSON no computador
    /// (pasta de dados do jogo). Cada pessoa monta o seu no criador e joga sempre com ele.
    /// </summary>
    public static class PersonagensSalvos
    {
        public const int Vagas = 4;
        private static PerfilPersonagem[] perfis;

        [Serializable]
        private class Arquivo
        {
            public string[] personagens = new string[Vagas];
        }

        private static string Caminho => Path.Combine(Application.persistentDataPath, "personagens.json");

        /// <summary>O personagem de uma vaga (0 = P1). Carrega do arquivo na primeira vez.</summary>
        public static PerfilPersonagem Pegar(int vaga)
        {
            Carregar();
            return perfis[Mathf.Clamp(vaga, 0, Vagas - 1)];
        }

        public static void Salvar()
        {
            if (perfis == null) return;
            Arquivo arquivo = new Arquivo();
            for (int i = 0; i < Vagas; i++) arquivo.personagens[i] = JsonUtility.ToJson(perfis[i]);
            try
            {
                File.WriteAllText(Caminho, JsonUtility.ToJson(arquivo, true));
            }
            catch (Exception erro)
            {
                Debug.LogWarning($"Não deu para salvar os personagens: {erro.Message}");
            }
        }

        private static void Carregar()
        {
            if (perfis != null) return;
            perfis = new PerfilPersonagem[Vagas];
            Arquivo arquivo = null;
            try
            {
                if (File.Exists(Caminho)) arquivo = JsonUtility.FromJson<Arquivo>(File.ReadAllText(Caminho));
            }
            catch (Exception erro)
            {
                Debug.LogWarning($"Arquivo de personagens com problema, usando os padrões: {erro.Message}");
            }
            for (int i = 0; i < Vagas; i++)
            {
                perfis[i] = Padrao(i);
                string json = arquivo != null && arquivo.personagens != null && i < arquivo.personagens.Length ? arquivo.personagens[i] : null;
                if (!string.IsNullOrEmpty(json))
                {
                    try { JsonUtility.FromJsonOverwrite(json, perfis[i]); }
                    catch { perfis[i] = Padrao(i); }
                }
                perfis[i].name = $"P{i + 1}";
            }
        }

        /// <summary>Personagem inicial de cada vaga (a pessoa muda tudo no criador).</summary>
        public static PerfilPersonagem Padrao(int vaga)
        {
            PerfilPersonagem p = ScriptableObject.CreateInstance<PerfilPersonagem>();
            p.hideFlags = HideFlags.DontSave;
            switch (vaga)
            {
                case 0:
                    p.nome = "Você"; p.cabelo = EstiloCabelo.Curto; p.chapeu = Chapeu.Bone; p.corPele = new Color(0.72f, 0.53f, 0.38f);
                    p.calcado = Calcado.Tenis; p.corCalcado = new Color(0.95f, 0.95f, 0.93f); p.estilo = EstiloJogo.Equilibrado;
                    break;
                case 1:
                    p.nome = "Parça"; p.cabelo = EstiloCabelo.BlackPower; p.chapeu = Chapeu.Nada; p.corPele = new Color(0.45f, 0.3f, 0.2f);
                    p.bermuda = Bermuda.Calcao; p.corBermuda = new Color(0.85f, 0.85f, 0.82f); p.calcado = Calcado.TenisCanoAlto;
                    p.corCalcado = new Color(0.9f, 0.2f, 0.2f); p.estilo = EstiloJogo.Corredor;
                    break;
                case 2:
                    p.nome = "Primo"; p.cabelo = EstiloCabelo.Raspado; p.chapeu = Chapeu.BoneParaTras; p.corPele = new Color(0.86f, 0.7f, 0.56f);
                    p.camisa = Camisa.Regata; p.calcado = Calcado.Chinelo; p.corCalcado = new Color(0.2f, 0.5f, 0.9f); p.estilo = EstiloJogo.Rebatedor;
                    p.faixaEtaria = FaixaEtaria.Adolescente; p.altura = 1.64f;
                    break;
                default:
                    p.nome = "Vizinha"; p.genero = Genero.Feminino; p.cabelo = EstiloCabelo.Longo; p.chapeu = Chapeu.Nada; p.corPele = new Color(0.62f, 0.43f, 0.3f);
                    p.calcado = Calcado.Tenis; p.corCalcado = new Color(1f, 0.8f, 0.2f); p.estilo = EstiloJogo.Lancador; p.altura = 1.68f;
                    p.tipoCorpo = TipoCorpo.Magro; p.acessorio = Acessorio.Munhequeira;
                    break;
            }
            return p;
        }
    }

    /// <summary>Ajustes do jogo (ficam salvos no computador entre uma vez e outra).</summary>
    public static class AjustesJogo
    {
        public static NivelDificuldade NivelBots
        {
            get => (NivelDificuldade)PlayerPrefs.GetInt("nivelBots", (int)NivelDificuldade.Medio);
            set { PlayerPrefs.SetInt("nivelBots", (int)value); PlayerPrefs.Save(); }
        }

        public static int MetaPontos
        {
            get => PlayerPrefs.GetInt("metaPontos", 10);
            set { PlayerPrefs.SetInt("metaPontos", value); PlayerPrefs.Save(); }
        }

        public static bool PreviaTrajetoria
        {
            get => PlayerPrefs.GetInt("previaTrajetoria", 1) == 1;
            set { PlayerPrefs.SetInt("previaTrajetoria", value ? 1 : 0); PlayerPrefs.Save(); }
        }

        public static bool TelaCheia
        {
            get => PlayerPrefs.GetInt("telaCheia", 1) == 1;
            set { PlayerPrefs.SetInt("telaCheia", value ? 1 : 0); PlayerPrefs.Save(); Aplicar(); }
        }

        /// <summary>0 = baixa, 1 = média, 2 = alta.</summary>
        public static int Qualidade
        {
            get => PlayerPrefs.GetInt("qualidade", 2);
            set { PlayerPrefs.SetInt("qualidade", value); PlayerPrefs.Save(); Aplicar(); }
        }

        public static float Volume
        {
            get => PlayerPrefs.GetFloat("volume", 0.8f);
            set { PlayerPrefs.SetFloat("volume", value); PlayerPrefs.Save(); Aplicar(); }
        }

        /// <summary>Aplica tela cheia, qualidade e volume (chamado ao abrir o jogo e ao mudar).</summary>
        public static void Aplicar()
        {
            if (!Application.isEditor)
            {
                // Tela cheia na resolução nativa do monitor (inclusive ultrawide, sem faixas pretas);
                // em janela, 1600x900 (ou menos, se o monitor for menor)
                int largura = Display.main.systemWidth, altura = Display.main.systemHeight;
                if (TelaCheia) Screen.SetResolution(largura, altura, FullScreenMode.FullScreenWindow);
                else Screen.SetResolution(Mathf.Min(1600, largura - 80), Mathf.Min(900, altura - 120), FullScreenMode.Windowed);
            }
            int niveis = QualitySettings.names.Length;
            if (niveis > 0) QualitySettings.SetQualityLevel(Mathf.Clamp(Mathf.RoundToInt(Qualidade / 2f * (niveis - 1)), 0, niveis - 1), true);
            AudioListener.volume = Volume;
        }
    }
}
