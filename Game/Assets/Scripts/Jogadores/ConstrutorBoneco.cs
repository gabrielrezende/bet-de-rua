using System.Collections.Generic;
using UnityEngine;

namespace BetsDeRua
{
    /// <summary>
    /// Monta o boneco a partir da ficha do personagem (PerfilPersonagem), com formas arredondadas
    /// geradas por código: corpo com proporção de gente (criança com cabeça maior), rosto (olhos,
    /// sobrancelhas, nariz, orelhas), cabelo, barba, boné/chapéu, camiseta/regata, bermuda,
    /// tênis/chinelo, acessórios e tatuagem. Roda no jogo (cada um joga com o seu personagem)
    /// e no criador de personagem (prévia).
    ///
    /// Esqueleto (nomes usados pelo AnimadorBoneco): Corpo > Quadril > Cintura > (Pescoco > Cabeca,
    /// OmbroE/D > CotoveloE/D > MaoE/D) e Quadril > CoxaE/D > JoelhoE/D > TornozeloE/D.
    /// Peças com nome "Time_..." (camisa, mangas, boné) recebem a cor do time.
    /// </summary>
    public static class ConstrutorBoneco
    {
        private static readonly Dictionary<Color, Material> materiais = new Dictionary<Color, Material>();
        private static Shader shaderLit;

        /// <summary>Material simples de uma cor (guardado: a mesma cor usa o mesmo material).</summary>
        public static Material Cor(Color cor, float brilho = 0.25f)
        {
            Color chave = new Color(cor.r, cor.g, cor.b, brilho);
            if (materiais.TryGetValue(chave, out Material m) && m != null) return m;
            if (shaderLit == null) shaderLit = Shader.Find("Universal Render Pipeline/Lit");
            m = new Material(shaderLit) { name = $"Boneco_{ColorUtility.ToHtmlStringRGB(cor)}" };
            m.SetColor("_BaseColor", cor);
            m.SetFloat("_Smoothness", brilho);
            materiais[chave] = m;
            return m;
        }

        /// <summary>Medidas do boneco montado (para o resto do jogo saber onde ficam as coisas).</summary>
        public struct Medidas
        {
            public float alturaQuadril;
            public float comprimentoBraco;    // ombro até cotovelo
            public float comprimentoAntebraco; // cotovelo até a mão
        }

        /// <summary>
        /// Monta (ou remonta) o boneco dentro de 'raiz'. Apaga o boneco antigo ("Corpo") se houver.
        /// matTime: cor do time (camisa e boné). Retorna as medidas.
        /// </summary>
        public static Medidas Montar(Transform raiz, PerfilPersonagem perfil, Material matTime)
        {
            Transform antigo = raiz.Find("Corpo");
            if (antigo != null)
            {
                antigo.name = "CorpoAntigo";
                antigo.gameObject.SetActive(false);
                if (Application.isPlaying) Object.Destroy(antigo.gameObject);
                else Object.DestroyImmediate(antigo.gameObject);
            }

            float altura = perfil != null ? perfil.altura : PerfilPersonagem.AlturaReferencia;
            float s = altura / PerfilPersonagem.AlturaReferencia;
            float w = perfil != null ? perfil.FatorLargura : 1f;
            FaixaEtaria idade = perfil != null ? perfil.faixaEtaria : FaixaEtaria.Adulto;
            bool crianca = idade == FaixaEtaria.Crianca;
            bool adolescente = idade == FaixaEtaria.Adolescente;

            Material pele = Cor(perfil != null ? perfil.corPele : new Color(0.78f, 0.6f, 0.45f), 0.35f);
            Material cabeloMat = Cor(perfil != null ? perfil.corCabelo : new Color(0.12f, 0.08f, 0.05f), 0.15f);
            Material bermudaMat = Cor(perfil != null ? perfil.corBermuda : new Color(0.16f, 0.18f, 0.24f), 0.1f);
            Material calcadoMat = Cor(perfil != null ? perfil.corCalcado : Color.white, 0.3f);
            Material solaMat = Cor(new Color(0.93f, 0.92f, 0.88f), 0.2f);
            Material olhoMat = Cor(new Color(0.06f, 0.05f, 0.05f), 0.6f);
            Material brancoOlho = Cor(new Color(0.96f, 0.96f, 0.94f), 0.4f);
            Material bocaMat = Cor(new Color(0.45f, 0.2f, 0.18f), 0.2f);
            Material tatuagemMat = Cor(new Color(0.13f, 0.17f, 0.24f), 0.1f);
            Material douradoMat = Cor(new Color(0.95f, 0.75f, 0.25f), 0.85f);
            Material pretoMat = Cor(new Color(0.05f, 0.05f, 0.06f), 0.7f);

            // ---- Proporções (adulto de 1,75 m; a criança tem cabeça proporcionalmente maior e pernas mais curtas)
            float cabecaFator = crianca ? 1.2f : adolescente ? 1.07f : 1f;
            float alturaCabeca = 0.235f * s * cabecaFator;
            float pernaFator = crianca ? 0.95f : 1f;
            float coxa = 0.43f * s * pernaFator, canela = 0.41f * s * pernaFator, pe = 0.075f * s;
            float alturaQuadril = coxa + canela + pe;
            float pescoco = 0.06f * s;
            float tronco = altura - alturaQuadril - pescoco - alturaCabeca;
            float braco = 0.29f * s, antebraco = 0.25f * s;
            float ombros = (crianca ? 0.15f : 0.18f) * s * Mathf.Lerp(1f, w, 0.7f);
            float larguraQuadril = 0.095f * s * w;

            Transform corpo = Pivo("Corpo", raiz, Vector3.zero);
            Transform quadril = Pivo("Quadril", corpo, new Vector3(0f, alturaQuadril, 0f));
            Transform cintura = Pivo("Cintura", quadril, Vector3.zero);

            // ---- Tronco: camisa do time (mais largo no peito, cintura mais fina)
            float r = 0.13f * s * w * (crianca ? 0.9f : 1f);
            // Barriga um pouco maior no corpo forte; peito mais largo que a cintura
            float barriga = w > 1.1f ? 1.08f : 0.94f;
            var perfilTronco = new List<Vector2>
            {
                new Vector2(-0.02f * s, 0f), new Vector2(0f, r * 0.97f), new Vector2(tronco * 0.3f, r * barriga),
                new Vector2(tronco * 0.6f, r * 1.04f), new Vector2(tronco * 0.8f, r * 1.1f),
                new Vector2(tronco * 0.92f, r * 0.98f), new Vector2(tronco * 0.99f, r * 0.62f), new Vector2(tronco * 1.03f, r * 0.3f),
                new Vector2(tronco * 1.04f, 0f)
            };
            Peca("Time_Tronco", cintura, Vector3.zero, Vector3.one, MalhasBoneco.Torno($"tronco{s:0.##}{w:0.##}{(crianca ? 1 : 0)}", perfilTronco, 1.32f, 0.74f), matTime);

            // Bermuda/calção na cintura
            var perfilCintura = new List<Vector2>
            {
                new Vector2(-0.13f * s, 0f), new Vector2(-0.12f * s, r * 1.0f), new Vector2(0.03f * s, r * 0.95f), new Vector2(0.05f * s, 0f)
            };
            Peca("Bermuda", quadril, Vector3.zero, Vector3.one, MalhasBoneco.Torno($"cintura{s:0.##}{w:0.##}", perfilCintura, 1.2f, 0.7f), bermudaMat);

            // ---- Pescoço e cabeça
            Transform pivoPescoco = Pivo("Pescoco", cintura, new Vector3(0f, tronco, 0f));
            Peca("PescocoMalha", pivoPescoco, new Vector3(0f, pescoco * 1.2f, 0f), Vector3.one,
                 MalhasBoneco.Membro(pescoco * 1.6f, 0.05f * s * Mathf.Sqrt(w), 0.055f * s * Mathf.Sqrt(w)), pele);
            Transform cabeca = Pivo("Cabeca", pivoPescoco, new Vector3(0f, pescoco, 0f));
            MontarCabeca(cabeca, perfil, alturaCabeca, pele, cabeloMat, olhoMat, brancoOlho, bocaMat, matTime);

            // ---- Braços
            Camisa camisa = perfil != null ? perfil.camisa : Camisa.Camiseta;
            float grossBraco = 0.048f * s * Mathf.Lerp(1f, w, 0.8f);
            foreach (float lado in new[] { -1f, 1f })
            {
                string sufixo = lado < 0f ? "E" : "D";
                Transform ombro = Pivo($"Ombro{sufixo}", cintura, new Vector3(lado * ombros, tronco - 0.07f * s, 0f));
                // Ombro arredondado (manga do time, ou pele na regata)
                Peca($"{(camisa == Camisa.Regata ? "" : "Time_")}OmbroBola{sufixo}", ombro, new Vector3(-lado * 0.012f * s, -0.012f * s, 0f),
                     new Vector3(1.15f, 1f, 1.05f) * grossBraco * 1.12f, MalhasBoneco.Esfera(), camisa == Camisa.Regata ? pele : matTime);
                Peca($"Braco{sufixo}", ombro, new Vector3(0f, 0.01f, 0f), Vector3.one, MalhasBoneco.Membro(braco + 0.02f, grossBraco, grossBraco * 0.8f), pele);
                if (camisa != Camisa.Regata)
                {
                    float manga = camisa == Camisa.MangaLonga ? braco + 0.01f : braco * 0.55f;
                    Peca($"Time_Manga{sufixo}", ombro, new Vector3(0f, 0.01f, 0f), Vector3.one,
                         MalhasBoneco.Membro(manga, grossBraco * 1.16f, grossBraco * 1.1f), matTime);
                }
                Transform cotovelo = Pivo($"Cotovelo{sufixo}", ombro, new Vector3(0f, -braco, 0f));
                Peca($"Antebraco{sufixo}", cotovelo, new Vector3(0f, 0.015f, 0f), Vector3.one,
                     MalhasBoneco.Membro(antebraco + 0.015f, grossBraco * 0.85f, grossBraco * 0.62f), pele);
                if (camisa == Camisa.MangaLonga)
                    Peca($"Time_Punho{sufixo}", cotovelo, new Vector3(0f, 0.02f, 0f), Vector3.one,
                         MalhasBoneco.Membro(antebraco * 0.92f, grossBraco * 1.02f, grossBraco * 0.8f), matTime);
                if (perfil != null && perfil.tatuagem && lado > 0f && camisa != Camisa.MangaLonga)
                    Peca("Tatuagem", cotovelo, new Vector3(0f, -antebraco * 0.45f, 0f), new Vector3(1f, 2.2f, 1f),
                         MalhasBoneco.Anel(grossBraco * 0.74f, grossBraco * 0.06f), tatuagemMat);
                if (perfil != null && perfil.acessorio == Acessorio.Munhequeira)
                    Peca($"Time_Munhequeira{sufixo}", cotovelo, new Vector3(0f, -antebraco * 0.86f, 0f), new Vector3(1f, 3f, 1f),
                         MalhasBoneco.Anel(grossBraco * 0.66f, grossBraco * 0.12f), matTime);

                // Mão: palma e polegar
                Transform mao = Pivo($"Mao{sufixo}", cotovelo, new Vector3(0f, -antebraco, 0f));
                Peca($"MaoMalha{sufixo}", mao, new Vector3(0f, -0.045f * s, 0f), new Vector3(0.042f, 0.055f, 0.026f) * s * Mathf.Sqrt(w),
                     MalhasBoneco.Esfera(), pele);
                Peca($"Polegar{sufixo}", mao, new Vector3(-lado * 0.012f * s, -0.035f * s, 0.022f * s), new Vector3(0.014f, 0.03f, 0.014f) * s,
                     MalhasBoneco.Esfera(), pele);
            }

            // ---- Pernas
            Bermuda bermuda = perfil != null ? perfil.bermuda : Bermuda.Bermuda;
            Calcado calcado = perfil != null ? perfil.calcado : Calcado.Tenis;
            float grossCoxa = 0.068f * s * Mathf.Lerp(1f, w, 0.8f);
            foreach (float lado in new[] { -1f, 1f })
            {
                string sufixo = lado < 0f ? "E" : "D";
                Transform pivoCoxa = Pivo($"Coxa{sufixo}", quadril, new Vector3(lado * larguraQuadril, -0.02f * s, 0f));
                Peca($"CoxaMalha{sufixo}", pivoCoxa, new Vector3(0f, 0.02f, 0f), Vector3.one,
                     MalhasBoneco.Membro(coxa + 0.03f, grossCoxa, grossCoxa * 0.72f), pele);
                float perna = bermuda == Bermuda.Calca ? coxa + 0.02f : bermuda == Bermuda.Bermuda ? coxa * 0.92f : coxa * 0.5f;
                Peca($"PernaBermuda{sufixo}", pivoCoxa, new Vector3(0f, 0.03f, 0f), Vector3.one,
                     MalhasBoneco.Membro(perna, grossCoxa * 1.2f, grossCoxa * (bermuda == Bermuda.Calcao ? 1.1f : 0.98f)), bermudaMat);

                Transform joelho = Pivo($"Joelho{sufixo}", pivoCoxa, new Vector3(0f, -coxa, 0f));
                Peca($"Canela{sufixo}", joelho, new Vector3(0f, 0.02f, 0f), Vector3.one,
                     MalhasBoneco.Membro(canela + 0.02f, grossCoxa * 0.78f, grossCoxa * 0.5f), pele);
                if (bermuda == Bermuda.Calca)
                    Peca($"CanelaCalca{sufixo}", joelho, new Vector3(0f, 0.03f, 0f), Vector3.one,
                         MalhasBoneco.Membro(canela * 0.97f, grossCoxa * 0.9f, grossCoxa * 0.72f), bermudaMat);

                Transform tornozelo = Pivo($"Tornozelo{sufixo}", joelho, new Vector3(0f, -canela, 0f));
                MontarCalcado(tornozelo, calcado, s, pe, grossCoxa, pele, calcadoMat, solaMat);
            }

            // ---- Acessórios do pescoço
            if (perfil != null && perfil.acessorio == Acessorio.Corrente)
                Peca("Corrente", pivoPescoco, new Vector3(0f, -0.005f, 0.012f), new Vector3(1f, 1f, 1f),
                     MalhasBoneco.Anel(0.065f * s * w, 0.006f), douradoMat);
            if (perfil != null && perfil.acessorio == Acessorio.Oculos)
            {
                Transform olhos = cabeca.Find("Olhos");
                if (olhos != null)
                {
                    foreach (float lado in new[] { -1f, 1f })
                        Peca("Lente", olhos, new Vector3(lado * alturaCabeca * 0.17f, 0f, alturaCabeca * 0.03f),
                             new Vector3(alturaCabeca * 0.14f, alturaCabeca * 0.1f, alturaCabeca * 0.03f), MalhasBoneco.Esfera(), pretoMat);
                    Peca("Ponte", olhos, new Vector3(0f, 0.005f, alturaCabeca * 0.04f), new Vector3(alturaCabeca * 0.4f, 0.006f, 0.006f),
                         MalhasBoneco.Esfera(), pretoMat);
                }
            }

            return new Medidas { alturaQuadril = alturaQuadril, comprimentoBraco = braco, comprimentoAntebraco = antebraco };
        }

        private static void MontarCabeca(Transform cabeca, PerfilPersonagem perfil, float h, Material pele, Material cabeloMat,
                                         Material olhoMat, Material brancoOlho, Material bocaMat, Material matTime)
        {
            float rx = h * 0.42f, ry = h * 0.5f, rz = h * 0.47f;
            Vector3 centro = new Vector3(0f, ry, 0f);
            Peca("CabecaMalha", cabeca, centro, new Vector3(rx, ry, rz), MalhasBoneco.Esfera(), pele);
            // Queixo/mandíbula: um pouco mais estreito, para o rosto não ficar uma bola
            Peca("Queixo", cabeca, centro + new Vector3(0f, -ry * 0.42f, rz * 0.18f), new Vector3(rx * 0.72f, ry * 0.5f, rz * 0.72f),
                 MalhasBoneco.Esfera(), pele);

            // Olhos (branco + pupila), sobrancelhas, nariz, orelhas, boca
            Transform olhos = Pivo("Olhos", cabeca, centro + new Vector3(0f, ry * 0.08f, rz * 0.86f));
            foreach (float lado in new[] { -1f, 1f })
            {
                Peca("OlhoBranco", olhos, new Vector3(lado * rx * 0.38f, 0f, 0f), new Vector3(h * 0.07f, h * 0.05f, h * 0.035f),
                     MalhasBoneco.Esfera(), brancoOlho);
                Peca("Pupila", olhos, new Vector3(lado * rx * 0.38f, 0f, h * 0.028f), new Vector3(h * 0.034f, h * 0.038f, h * 0.018f),
                     MalhasBoneco.Esfera(), olhoMat);
                Peca("Sobrancelha", olhos, new Vector3(lado * rx * 0.38f, h * 0.075f, h * 0.012f), new Vector3(h * 0.08f, h * 0.014f, h * 0.02f),
                     MalhasBoneco.Esfera(), cabeloMat);
                Peca("Orelha", cabeca, centro + new Vector3(lado * rx * 0.97f, 0f, -rz * 0.05f), new Vector3(h * 0.04f, h * 0.09f, h * 0.06f),
                     MalhasBoneco.Esfera(), pele);
            }
            Peca("Nariz", cabeca, centro + new Vector3(0f, -ry * 0.12f, rz * 0.95f), new Vector3(h * 0.045f, h * 0.07f, h * 0.06f),
                 MalhasBoneco.Esfera(), pele);
            Peca("Boca", cabeca, centro + new Vector3(0f, -ry * 0.45f, rz * 0.8f), new Vector3(h * 0.1f, h * 0.018f, h * 0.03f),
                 MalhasBoneco.Esfera(), bocaMat);

            // Barba
            Barba barba = perfil != null ? perfil.barba : Barba.Nada;
            if (barba == Barba.Bigode || barba == Barba.Cavanhaque)
                Peca("Bigode", cabeca, centro + new Vector3(0f, -ry * 0.32f, rz * 0.86f), new Vector3(h * 0.13f, h * 0.025f, h * 0.04f),
                     MalhasBoneco.Esfera(), cabeloMat);
            if (barba == Barba.Cavanhaque)
                Peca("Cavanhaque", cabeca, centro + new Vector3(0f, -ry * 0.72f, rz * 0.62f), new Vector3(h * 0.07f, h * 0.08f, h * 0.06f),
                     MalhasBoneco.Esfera(), cabeloMat);
            if (barba == Barba.Cheia)
            {
                // Barba rente: cobre o queixo e as bochechas por baixo, sem tapar a boca
                Peca("BarbaCheia", cabeca, centro + new Vector3(0f, -ry * 0.42f, rz * 0.2f), new Vector3(rx * 0.76f, ry * 0.52f, rz * 0.78f),
                     MalhasBoneco.Calota(-90f, -5f), cabeloMat);
                Peca("Bigode", cabeca, centro + new Vector3(0f, -ry * 0.32f, rz * 0.86f), new Vector3(h * 0.13f, h * 0.025f, h * 0.04f),
                     MalhasBoneco.Esfera(), cabeloMat);
            }

            // Cabelo
            EstiloCabelo cabelo = perfil != null ? perfil.cabelo : EstiloCabelo.Curto;
            Chapeu chapeu = perfil != null ? perfil.chapeu : Chapeu.Nada;
            Vector3 escalaCabelo = new Vector3(rx, ry, rz);
            switch (cabelo)
            {
                case EstiloCabelo.Raspado:
                    Peca("Cabelo", cabeca, centro, escalaCabelo * 1.015f, MalhasBoneco.Calota(15f, 90f), cabeloMat);
                    Peca("CabeloNuca", cabeca, centro, escalaCabelo * 1.012f, MalhasBoneco.Calota(-20f, 90f), cabeloMat, -55f);
                    break;
                case EstiloCabelo.Curto:
                    Peca("Cabelo", cabeca, centro + new Vector3(0f, ry * 0.04f, 0f), escalaCabelo * 1.07f, MalhasBoneco.Calota(18f, 90f), cabeloMat);
                    Peca("CabeloNuca", cabeca, centro, escalaCabelo * 1.05f, MalhasBoneco.Calota(-15f, 90f), cabeloMat, -55f);
                    break;
                case EstiloCabelo.BlackPower:
                    // Black power: uma "nuvem" redonda e volumosa, mais alta que larga, deixando a testa livre
                    Peca("Cabelo", cabeca, centro + new Vector3(0f, ry * 0.42f, -rz * 0.18f), new Vector3(rx * 1.5f, ry * 1.25f, rz * 1.4f),
                         MalhasBoneco.Esfera(), cabeloMat);
                    Peca("CabeloLadoE", cabeca, centro + new Vector3(-rx * 0.75f, ry * 0.05f, -rz * 0.2f), new Vector3(rx * 0.6f, ry * 0.75f, rz * 0.8f),
                         MalhasBoneco.Esfera(), cabeloMat);
                    Peca("CabeloLadoD", cabeca, centro + new Vector3(rx * 0.75f, ry * 0.05f, -rz * 0.2f), new Vector3(rx * 0.6f, ry * 0.75f, rz * 0.8f),
                         MalhasBoneco.Esfera(), cabeloMat);
                    break;
                case EstiloCabelo.Moicano:
                    Peca("Cabelo", cabeca, centro + new Vector3(0f, ry * 0.75f, -rz * 0.05f), new Vector3(rx * 0.22f, ry * 0.55f, rz * 1.0f),
                         MalhasBoneco.Esfera(), cabeloMat);
                    Peca("CabeloLados", cabeca, centro, escalaCabelo * 1.01f, MalhasBoneco.Calota(15f, 90f), Cor(Color.Lerp(perfil != null ? perfil.corPele : Color.gray, perfil != null ? perfil.corCabelo : Color.black, 0.4f), 0.2f));
                    break;
                case EstiloCabelo.Longo:
                    Peca("Cabelo", cabeca, centro + new Vector3(0f, ry * 0.04f, 0f), escalaCabelo * 1.08f, MalhasBoneco.Calota(15f, 90f), cabeloMat);
                    Peca("CabeloNuca", cabeca, centro, escalaCabelo * 1.07f, MalhasBoneco.Calota(-15f, 90f), cabeloMat, -55f);
                    Peca("CabeloCostas", cabeca, centro + new Vector3(0f, -ry * 0.75f, -rz * 0.55f), new Vector3(rx * 0.95f, ry * 0.9f, rz * 0.35f),
                         MalhasBoneco.Esfera(), cabeloMat);
                    break;
                case EstiloCabelo.Trancas:
                    Peca("Cabelo", cabeca, centro + new Vector3(0f, ry * 0.03f, 0f), escalaCabelo * 1.06f, MalhasBoneco.Calota(15f, 90f), cabeloMat);
                    Peca("CabeloNuca", cabeca, centro, escalaCabelo * 1.05f, MalhasBoneco.Calota(-15f, 90f), cabeloMat, -55f);
                    for (int i = 0; i < 6; i++)
                    {
                        float a = Mathf.Lerp(-70f, 70f, i / 5f) * Mathf.Deg2Rad;
                        Vector3 base_ = centro + new Vector3(Mathf.Sin(a) * rx * 0.9f, -ry * 0.25f, -Mathf.Cos(a) * rz * 0.9f);
                        Peca("Tranca", cabeca, base_, Vector3.one, MalhasBoneco.Membro(h * 0.55f, h * 0.035f, h * 0.025f), cabeloMat);
                    }
                    break;
            }

            // Boné / chapéu (cor do time: "Time_")
            switch (chapeu)
            {
                case Chapeu.Bone:
                case Chapeu.BoneParaTras:
                {
                    float frente = chapeu == Chapeu.Bone ? 1f : -1f;
                    // Boné: copa justa na cabeça (acima das orelhas) e aba comprida e fina
                    Peca("Time_Bone", cabeca, centro + new Vector3(0f, ry * 0.22f, -rz * 0.02f), new Vector3(rx * 1.07f, ry * 0.86f, rz * 1.07f),
                         MalhasBoneco.Calota(0f, 90f), matTime);
                    Peca("Time_Aba", cabeca, centro + new Vector3(0f, ry * 0.26f, frente * rz * 1.12f),
                         new Vector3(rx * 0.82f, h * 0.014f, rz * 0.58f), MalhasBoneco.Calota(-90f, 90f), matTime, frente * -8f);
                    break;
                }
                case Chapeu.Bucket:
                    Peca("Time_Bone", cabeca, centro + new Vector3(0f, ry * 0.15f, 0f), new Vector3(rx, ry * 1.05f, rz) * 1.12f,
                         MalhasBoneco.Calota(0f, 90f), matTime);
                    Peca("Time_Aba", cabeca, centro + new Vector3(0f, ry * 0.12f, 0f), new Vector3(1f, 1f, 1f),
                         MalhasBoneco.Aba(rx * 0.95f, rx * 1.65f, h * 0.025f), matTime);
                    break;
                case Chapeu.Touca:
                    Peca("Time_Bone", cabeca, centro + new Vector3(0f, ry * 0.18f, -rz * 0.04f), new Vector3(rx * 1.1f, ry * 1.25f, rz * 1.1f),
                         MalhasBoneco.Calota(0f, 90f), matTime);
                    Peca("Time_Barra", cabeca, centro + new Vector3(0f, ry * 0.2f, -rz * 0.04f), new Vector3(1.02f, 2.2f, 1.08f),
                         MalhasBoneco.Anel(rx * 1.07f, h * 0.03f), matTime);
                    break;
            }
        }

        private static void MontarCalcado(Transform tornozelo, Calcado calcado, float s, float pe, float grossCoxa,
                                          Material pele, Material calcadoMat, Material solaMat)
        {
            float comprimento = 0.26f * s, largura = 0.1f * s;
            Vector3 frente = new Vector3(0f, 0f, comprimento * 0.28f);
            if (calcado == Calcado.Chinelo)
            {
                Peca("Pe", tornozelo, new Vector3(0f, -pe * 0.55f, comprimento * 0.25f), new Vector3(largura * 0.42f, pe * 0.45f, comprimento * 0.46f),
                     MalhasBoneco.Esfera(), pele);
                Peca("Sola", tornozelo, new Vector3(0f, -pe + 0.008f, comprimento * 0.25f), new Vector3(largura * 0.55f, 0.01f, comprimento * 0.55f),
                     MalhasBoneco.Esfera(), calcadoMat);
                Peca("Tira", tornozelo, new Vector3(0f, -pe * 0.5f, comprimento * 0.45f), new Vector3(largura * 0.46f, pe * 0.3f, 0.018f),
                     MalhasBoneco.Esfera(), calcadoMat);
                return;
            }
            Peca("Tenis", tornozelo, new Vector3(0f, -pe * 0.42f, 0f) + frente, new Vector3(largura * 0.5f, pe * 0.62f, comprimento * 0.52f),
                 MalhasBoneco.Esfera(), calcadoMat);
            Peca("Sola", tornozelo, new Vector3(0f, -pe + 0.012f, 0f) + frente, new Vector3(largura * 0.54f, 0.018f, comprimento * 0.55f),
                 MalhasBoneco.Esfera(), solaMat);
            if (calcado == Calcado.TenisCanoAlto)
                Peca("Cano", tornozelo, new Vector3(0f, pe * 0.9f, 0f), Vector3.one,
                     MalhasBoneco.Membro(pe * 1.6f, grossCoxa * 0.6f, grossCoxa * 0.62f), calcadoMat);
            else
                Peca("Meia", tornozelo, new Vector3(0f, pe * 0.45f, 0f), Vector3.one,
                     MalhasBoneco.Membro(pe * 0.8f, grossCoxa * 0.52f, grossCoxa * 0.52f), solaMat);
        }

        /// <summary>
        /// Taco de bets: pá de madeira achatada (como a do críquete) com o cabo enrolado em fita.
        /// O pivô fica na mão (topo do cabo) e o taco pende para baixo (-Y).
        /// </summary>
        public static void MontarTaco(Transform pivo, float comprimento, Color corFita)
        {
            Transform antigo = pivo.Find("Taco");
            if (antigo != null)
            {
                antigo.gameObject.SetActive(false);
                if (Application.isPlaying) Object.Destroy(antigo.gameObject);
                else Object.DestroyImmediate(antigo.gameObject);
            }
            Transform taco = Pivo("Taco", pivo, Vector3.zero);
            float cabo = comprimento * 0.32f;
            Material madeira = Cor(new Color(0.78f, 0.6f, 0.38f), 0.35f);
            Material madeiraEscura = Cor(new Color(0.55f, 0.38f, 0.22f), 0.3f);
            Material fita = Cor(corFita, 0.15f);

            // Cabo redondo com fita
            Peca("Cabo", taco, new Vector3(0f, 0.03f, 0f), Vector3.one, MalhasBoneco.Membro(cabo + 0.03f, 0.016f, 0.017f), fita);
            Peca("Pomo", taco, new Vector3(0f, 0.03f, 0f), new Vector3(0.022f, 0.012f, 0.022f), MalhasBoneco.Esfera(), madeiraEscura);
            // Pá: oval achatada, larga e fina, com a ponta arredondada
            float pa = comprimento - cabo;
            var perfilPa = new List<Vector2>
            {
                new Vector2(-comprimento, 0f), new Vector2(-comprimento + 0.012f, 0.6f), new Vector2(-comprimento + 0.04f, 0.92f),
                new Vector2(-comprimento + pa * 0.5f, 1f), new Vector2(-cabo - 0.05f, 0.95f), new Vector2(-cabo + 0.005f, 0.42f),
                new Vector2(-cabo + 0.03f, 0f)
            };
            Peca("Pa", taco, Vector3.zero, Vector3.one, MalhasBoneco.Torno($"pa{comprimento:0.###}", perfilPa, 0.038f, 0.012f, 16), madeira);
            // Friso no meio da pá (dá leitura de madeira trabalhada)
            Peca("Friso", taco, new Vector3(0f, -cabo - pa * 0.48f, 0f), new Vector3(0.006f, pa * 0.42f, 0.0135f), MalhasBoneco.Esfera(), madeiraEscura);
        }

        private static Transform Pivo(string nome, Transform pai, Vector3 posicao)
        {
            Transform pivo = new GameObject(nome).transform;
            pivo.SetParent(pai, false);
            pivo.localPosition = posicao;
            return pivo;
        }

        private static void Peca(string nome, Transform pai, Vector3 posicao, Vector3 escala, Mesh malha, Material material,
                                 float inclinarX = 0f)
        {
            GameObject peca = new GameObject(nome);
            peca.transform.SetParent(pai, false);
            peca.transform.localPosition = posicao;
            peca.transform.localRotation = Quaternion.Euler(inclinarX, 0f, 0f);
            peca.transform.localScale = escala;
            peca.AddComponent<MeshFilter>().sharedMesh = malha;
            peca.AddComponent<MeshRenderer>().sharedMaterial = material;
        }
    }
}
