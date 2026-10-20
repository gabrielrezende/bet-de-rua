using UnityEngine;

namespace BetsDeRua.Editor
{
    /// <summary>
    /// Os lugares da segunda leva (pedido do Gabriel): favela (viela e escadão), avenida (canteiro
    /// central e comércio), rodovia (acostamento com guard-rail) e pontilhão (embaixo do viaduto).
    /// Mesma ideia dos outros: só cenário; o chão de cada um está no ConfigMapas e o limite do campo
    /// continua o mesmo (ConfigFisica), com paredes invisíveis na volta.
    /// </summary>
    public static partial class ConstrutorRuaEditor
    {
        // ------------------------------------------------------------------ favela

        /// <summary>
        /// Viela de cimentado com as casas coladas no fundo e nas pontas, subindo o morro em fileiras,
        /// um escadão de concreto no meio delas, gatos de fio pra todo lado, varal na laje.
        /// </summary>
        private static Transform MontarFavela(Transform pai, ConfigFisica config, System.Random r)
        {
            float L = config.comprimentoCampo, meia = config.larguraCampo * 0.5f;
            Material cimentado = Mat("Cimentado", new Color(1.25f, 1.22f, 1.16f), Foto("brushed_concrete", false), Foto("brushed_concrete", true), 3f, 0.06f);
            Transform t = NovoMapa(pai, "Mapa_Favela");

            // Chão de cimentado com remendos de terra e a valeta no pé das casas
            Juntar("ChaoCimentado", cimentado, new Vector3(0f, 0.002f, 0f), new Vector3(L + 80f, 0.004f, 2f * meia + 30f));
            for (int i = 0; i < 10; i++)
                Juntar("RemendoTerra", terra, new Vector3((float)(r.NextDouble() - 0.5) * L, 0.004f, (float)(r.NextDouble() - 0.5) * 2f * (meia - 1f)),
                       new Vector3(1.5f + (float)r.NextDouble() * 2.5f, 0.004f, 0.8f + (float)r.NextDouble() * 1.2f));
            Juntar("Valeta", grade, new Vector3(0f, 0.005f, meia + 0.9f), new Vector3(L + 80f, 0.004f, 0.3f));

            // Fileiras de casas subindo o morro (cada fileira mais alta que a da frente)
            float zFrente = meia + 1.8f;
            const int filas = 6;
            const float fundoFila = 5.5f, subidaFila = 2.2f, larguraEscadao = 2.6f;
            float xEscadao = L * 0.18f;
            for (int f = 0; f < filas; f++)
            {
                float z = zFrente + f * fundoFila, y = f * subidaFila;
                if (f > 0) Juntar("Encosta", terra, new Vector3(0f, y * 0.5f, z + fundoFila * 0.5f), new Vector3(L + 100f, y, fundoFila));
                float x0 = -L * 0.5f - 45f;
                while (x0 < L * 0.5f + 45f)
                {
                    float w = 3.5f + r.Next(3);
                    // Abre espaço para o escadão
                    if (x0 < xEscadao + larguraEscadao * 0.5f + 0.3f && x0 + w > xEscadao - larguraEscadao * 0.5f - 0.3f)
                    {
                        x0 = xEscadao + larguraEscadao * 0.5f + 0.3f;
                        continue;
                    }
                    // Viela: um vão estreito e escuro entre duas casas
                    if (f == 0 && r.NextDouble() < 0.12)
                    {
                        Juntar("Viela", janela, new Vector3(x0 + 0.7f, 2.5f, z + fundoFila * 0.5f), new Vector3(1.4f, 5f, 0.1f));
                        x0 += 1.4f;
                        continue;
                    }
                    float xc = x0 + w * 0.5f;
                    float h = (1 + r.Next(3)) * 2.9f - 0.2f;
                    Material m = r.NextDouble() < 0.55 ? tijolo : reboco[r.Next(reboco.Length)];
                    CasaVirada(new Vector3(xc, y, z), Vector3.back, w - 0.1f, h, fundoFila - 0.5f, m, r);
                    if (f == 0 && r.NextDouble() < 0.4)
                        Pichar(t, new Vector3(xc + (float)(r.NextDouble() - 0.5) * w * 0.3f, 1.3f, z - 0.03f), 1.5f, pichacao[r.Next(pichacao.Length)]);
                    if (f < 2 && r.NextDouble() < 0.25)
                        Varal(t, new Vector3(xc, y + h, z + 1.2f), w * 0.8f, r);
                    x0 += w;
                }
            }

            // Escadão de concreto subindo entre as casas, com corrimão de cano
            float comprimento = filas * fundoFila, subida = filas * subidaFila;
            int degraus = Mathf.CeilToInt(subida / 0.18f);
            float piso = comprimento / degraus, espelho = subida / degraus;
            for (int k = 0; k < degraus; k++)
            {
                float topo = (k + 1) * espelho;
                Juntar("Degrau", concreto, new Vector3(xEscadao, topo * 0.5f, zFrente + (k + 0.5f) * piso), new Vector3(larguraEscadao, topo, piso));
            }
            float inclinacao = Mathf.Atan2(subida, comprimento) * Mathf.Rad2Deg;
            float rampa = Mathf.Sqrt(comprimento * comprimento + subida * subida);
            foreach (float lado in new[] { -1f, 1f })
            {
                float x = xEscadao + lado * (larguraEscadao * 0.5f - 0.08f);
                GameObject corrimao = CaixaNoMundo("Corrimao", t, new Vector3(x, subida * 0.5f + 0.95f, zFrente + comprimento * 0.5f), new Vector3(0.05f, 0.05f, rampa), grade);
                corrimao.transform.rotation = Quaternion.Euler(-inclinacao, 0f, 0f);
                for (int k = 0; k < degraus; k += 8)
                    Juntar("PosteCorrimao", grade, new Vector3(x, (k + 1) * espelho + 0.475f, zFrente + (k + 0.5f) * piso), new Vector3(0.05f, 0.95f, 0.05f));
            }

            // Gatos de luz: postes na frente das casas e fios embolados entre eles e as casas
            var topos = new System.Collections.Generic.List<Vector3>();
            for (int i = 0; i < 5; i++)
                topos.Add(Poste(t, new Vector3(-L * 0.5f - 5f + i * (L + 10f) / 4f, 0f, zFrente - 0.35f)));
            for (int i = 0; i < topos.Count - 1; i++)
                for (int f = 0; f < 4; f++)
                    Fio(t, topos[i] + new Vector3(0f, -f * 0.3f, (f - 1.5f) * 0.25f), topos[i + 1] + new Vector3(0f, -f * 0.2f, (f - 1.5f) * 0.3f), 0.6f + (float)r.NextDouble() * 0.8f);
            for (int i = 0; i < 16; i++)
            {
                Vector3 a = topos[r.Next(topos.Count)] + Vector3.down * (float)r.NextDouble() * 0.8f;
                Vector3 b = new Vector3(a.x + (float)(r.NextDouble() - 0.5) * 14f, 3f + (float)r.NextDouble() * 3f, zFrente + 0.02f);
                Fio(t, a, b, 0.3f + (float)r.NextDouble() * 0.5f);
            }

            // Pontas: casas fechando a viela (a bola volta na parede)
            foreach (float lado in new[] { -1f, 1f })
            {
                float z0 = -meia - 1f;
                while (z0 < zFrente)
                {
                    float w = 3.5f + r.Next(3);
                    float h = z0 < 0f ? 2.7f : (1 + r.Next(2)) * 2.9f - 0.2f;
                    Material m = r.NextDouble() < 0.55 ? tijolo : reboco[r.Next(reboco.Length)];
                    CasaVirada(new Vector3(lado * (L * 0.5f + 0.6f), 0f, z0 + w * 0.5f), new Vector3(-lado, 0f, 0f), w - 0.1f, h, 6f, m, r);
                    z0 += w;
                }
            }

            // Lado da câmera: mureta baixa e casinhas térreas recuadas
            MuretaPerto(config, r);
            CasasLadoDeCa(config, r, -(meia + 9f));
            ColisoesDoCampo(t, config);
            FinalizarJuntas(t, "Favela_");
            return t;
        }

        /// <summary>Varal na laje: um fio com roupas coloridas penduradas.</summary>
        private static void Varal(Transform t, Vector3 laje, float largura, System.Random r)
        {
            Vector3 a = laje + new Vector3(-largura * 0.5f, 1.5f, 0f), b = laje + new Vector3(largura * 0.5f, 1.5f, 0f);
            Fio(t, a, b, 0.08f);
            Juntar("PosteVaral", grade, a + Vector3.down * 0.75f, new Vector3(0.04f, 1.5f, 0.04f));
            Juntar("PosteVaral", grade, b + Vector3.down * 0.75f, new Vector3(0.04f, 1.5f, 0.04f));
            int pecas = 2 + r.Next(3);
            for (int i = 0; i < pecas; i++)
            {
                float u = (i + 0.5f) / pecas;
                Vector3 p = Vector3.Lerp(a, b, u) + Vector3.down * (0.32f + 0.32f * 4f * u * (1f - u) * 0.25f);
                Juntar("Roupa", placaCores[r.Next(placaCores.Length)], p, new Vector3(0.45f, 0.55f, 0.02f));
            }
        }

        // ------------------------------------------------------------------ avenida

        /// <summary>
        /// Pista de asfalto novo com calçada do lado da câmera e o canteiro central no fundo (com
        /// meio-fio, grama e palmeiras); depois do canteiro, a outra pista e a fileira de lojas com
        /// prédio em cima, ponto de ônibus, faixa de pedestre e semáforo nas pontas.
        /// </summary>
        private static Transform MontarAvenida(Transform pai, ConfigFisica config, System.Random r, Material grama)
        {
            float L = config.comprimentoCampo, meia = config.larguraCampo * 0.5f;
            float meiaRua = config.larguraRua * 0.5f, hb = config.alturaCalcada, larguraCalcada = meia - meiaRua;
            Material faixaBranca = Mat("FaixaBranca", new Color(0.95f, 0.95f, 0.93f), null, null, 1f, 0.25f);
            Material laranja = Mat("Cone", new Color(0.95f, 0.42f, 0.1f), null, null, 1f, 0.3f);
            Material sinalVermelho = Mat("SinalVermelho", new Color(1f, 0.15f, 0.1f), null, null, 1f, 0f, false, true);
            Material sinalVerde = Mat("SinalVerde", new Color(0.2f, 0.9f, 0.4f), null, null, 1f, 0f, false, true);
            Transform t = NovoMapa(pai, "Mapa_Avenida");

            // Pista onde se joga: asfalto liso com a faixa branca tracejada separando as duas faixas
            Juntar("AsfaltoAvenida", asfalto, new Vector3(0f, 0.0008f, 0f), new Vector3(L + 100f, 0.001f, 2f * meiaRua + 0.2f));
            Tracejado(faixaBranca, 0f, -L * 0.5f - 40f, L * 0.5f + 40f, 3f, 5f, 0.12f);

            // Calçada (lado da câmera) e canteiro central (fundo): mesma altura e meio-fio da rua, com colisão
            foreach (float lado in new[] { -1f, 1f })
            {
                float zc = lado * (meiaRua + larguraCalcada * 0.5f);
                GameObject faixa = Caixa(lado < 0f ? "CalcadaAvenida" : "CanteiroCentral", t, new Vector3(0f, hb * 0.5f, zc),
                                         new Vector3(L + 100f, hb, larguraCalcada), lado < 0f ? calcada : concreto, false);
                BoxCollider col = faixa.AddComponent<BoxCollider>();
                col.size = new Vector3(L, hb, larguraCalcada);
                Juntar("MeioFioAvenida", meioFio, new Vector3(0f, hb * 0.5f + 0.003f, lado * (meiaRua + 0.07f)), new Vector3(L + 100f, hb + 0.006f, 0.14f));
            }
            Juntar("MeioFioCanteiro", meioFio, new Vector3(0f, hb * 0.5f + 0.003f, meia - 0.07f), new Vector3(L + 100f, hb + 0.006f, 0.14f));
            Juntar("GramaCanteiro", grama, new Vector3(0f, hb + 0.004f, meiaRua + larguraCalcada * 0.5f), new Vector3(L + 100f, 0.006f, larguraCalcada - 0.5f));
            for (float x = -L * 0.5f - 30f; x <= L * 0.5f + 30f; x += 11f)
                Palmeira(new Vector3(x + (float)(r.NextDouble() - 0.5) * 2f, hb, meiaRua + larguraCalcada * 0.5f), r);
            for (float x = -L * 0.5f - 20f; x <= L * 0.5f + 20f; x += 33f)
                Poste(t, new Vector3(x, hb, meiaRua + larguraCalcada * 0.5f + 0.4f));

            // A outra pista (mão contrária) com uns carros passando
            float zOutra = meia + 5.5f;
            Juntar("AsfaltoOutraPista", asfalto, new Vector3(0f, 0.0008f, zOutra), new Vector3(L + 100f, 0.001f, 11f));
            Tracejado(faixaBranca, zOutra, -L * 0.5f - 40f, L * 0.5f + 40f, 3f, 5f, 0.12f);
            for (int i = 0; i < 4; i++)
                Carro(t, new Vector3((float)(r.NextDouble() - 0.5) * (L + 30f), 0f, zOutra + (i % 2 == 0 ? -2.7f : 2.7f)), i % 2 == 0 ? 180f : 0f, carro[r.Next(carro.Length)]);

            // Calçada do comércio e a fileira de lojas, quase todas com prédio em cima
            float zLoja = meia + 11f + 3f;
            Juntar("CalcadaComercio", calcada, new Vector3(0f, hb * 0.5f, zLoja - 1.5f + 2f), new Vector3(L + 100f, hb, 7f));
            Juntar("MeioFioComercio", meioFio, new Vector3(0f, hb * 0.5f + 0.003f, zLoja - 3f + 0.07f), new Vector3(L + 100f, hb + 0.006f, 0.14f));
            string[] lojas = { "FARMÁCIA", "LOJA DE 1,99", "PADARIA", "ÓTICA", "LANCHONETE", "CELULARES", "CALÇADOS", "BAR DO ZÉ", "LOTÉRICA", "PET SHOP" };
            int loja = r.Next(lojas.Length);
            float x0 = -L * 0.5f - 45f;
            while (x0 < L * 0.5f + 45f)
            {
                float w = 6f + r.Next(5);
                float xc = x0 + w * 0.5f;
                Comercio(t, xc, zLoja, hb, w, reboco[r.Next(reboco.Length)], lojas[loja++ % lojas.Length], r);
                if (r.NextDouble() < 0.75)
                {
                    int andares = 1 + r.Next(4);
                    float ha = andares * 2.9f;
                    Material m = r.NextDouble() < 0.25 ? tijolo : reboco[r.Next(reboco.Length)];
                    Juntar("Predio", m, new Vector3(xc, hb + 4.4f + ha * 0.5f, zLoja + 0.4f + 4f), new Vector3(w - 0.1f, ha, 8f));
                    for (int a = 0; a < andares; a++)
                        for (float u = -0.3f; u <= 0.31f; u += 0.3f)
                            Juntar("JanelaPredio", janela, new Vector3(xc + u * w, hb + 4.4f + a * 2.9f + 1.5f, zLoja + 0.38f), new Vector3(1.2f, 1.1f, 0.05f));
                    if (r.NextDouble() < 0.5)
                        Juntar("CaixaDagua", caixaDagua, new Vector3(xc + w * 0.2f, hb + 4.4f + ha + 0.55f, zLoja + 4f), new Vector3(1.3f, 1.1f, 1.3f), "cilindro");
                }
                x0 += w;
            }

            // Ponto de ônibus na calçada do comércio
            Vector3 ponto = new Vector3(L * 0.2f, hb, zLoja - 1.6f);
            Juntar("CoberturaPonto", grade, ponto + new Vector3(0f, 2.5f, 0f), new Vector3(4f, 0.08f, 1.6f));
            foreach (float x in new[] { -1.8f, 1.8f })
                Juntar("PostePonto", grade, ponto + new Vector3(x, 1.25f, 0.6f), new Vector3(0.08f, 2.5f, 0.08f));
            Juntar("VidroPonto", vidroCarro, ponto + new Vector3(0f, 1.4f, 0.75f), new Vector3(3.6f, 1.8f, 0.04f));
            Juntar("BancoPonto", grade, ponto + new Vector3(0f, 0.45f, 0.5f), new Vector3(3f, 0.06f, 0.4f));
            Juntar("PlacaOnibus", placaCores[1], ponto + new Vector3(-2.3f, 2.6f, -0.3f), new Vector3(0.5f, 0.7f, 0.04f));
            Juntar("PostePlacaOnibus", grade, ponto + new Vector3(-2.3f, 1.3f, -0.3f), new Vector3(0.06f, 2.6f, 0.06f));

            // Pontas: faixa de pedestre atravessando as duas pistas, semáforo no canteiro e cones (o "fim do campo" da molecada)
            foreach (float lado in new[] { -1f, 1f })
            {
                float xf = lado * (L * 0.5f + 8f);
                foreach (float zIni in new[] { -meiaRua, zOutra - 5.5f })
                    for (float z = zIni + 0.6f; z < zIni + 2f * meiaRua - 0.4f; z += 1f)
                        Juntar("FaixaPedestre", faixaBranca, new Vector3(xf, 0.003f, z), new Vector3(3f, 0.002f, 0.5f));
                Vector3 s = new Vector3(lado * (L * 0.5f + 5f), hb, meiaRua + larguraCalcada * 0.5f);
                Juntar("PosteSemaforo", grade, s + Vector3.up * 2f, new Vector3(0.12f, 4f, 0.12f), "cilindro");
                Juntar("Semaforo", grade, s + new Vector3(0f, 4.4f, -0.1f), new Vector3(0.35f, 1f, 0.3f));
                Juntar("LuzVermelha", sinalVermelho, s + new Vector3(0f, 4.7f, -0.26f), new Vector3(0.2f, 0.2f, 0.04f));
                Juntar("LuzVerde", sinalVerde, s + new Vector3(0f, 4.1f, -0.26f), new Vector3(0.2f, 0.2f, 0.04f));
                for (float z = -meiaRua + 1.2f; z < meiaRua; z += 2.4f)
                    Cone(new Vector3(lado * (L * 0.5f + 0.4f), 0f, z), laranja, faixaBranca);
            }

            // Lado da câmera: grade baixa no fim da calçada e casinhas recuadas
            for (float x = -L * 0.5f - 25f; x <= L * 0.5f + 25f; x += 0.2f)
                Juntar("GradeCalcada", grade, new Vector3(x, hb + 0.45f, -meia + 0.1f), new Vector3(0.03f, 0.9f, 0.03f));
            Juntar("CorrimaoGrade", grade, new Vector3(0f, hb + 0.9f, -meia + 0.1f), new Vector3(L + 50f, 0.05f, 0.05f));
            Juntar("Quintal", calcada, new Vector3(0f, 0.01f, -(meia + 5.5f)), new Vector3(L + 100f, 0.02f, 11f));
            CasasLadoDeCa(config, r, -(meia + 11f));
            ColisoesDoCampo(t, config);
            FinalizarJuntas(t, "Avenida_");
            return t;
        }

        /// <summary>Palmeira de canteiro: tronco alto e fino, folhas abertas em cruz no topo.</summary>
        private static void Palmeira(Vector3 baseP, System.Random r)
        {
            float h = 6f + (float)r.NextDouble() * 2.5f;
            Juntar("TroncoPalmeira", tronco, baseP + Vector3.up * h * 0.5f, new Vector3(0.3f, h, 0.3f), "cilindro");
            Vector3 topo = baseP + Vector3.up * h;
            Juntar("CopaPalmeira", folhagem, topo, Vector3.one * 0.9f, "esfera");
            Juntar("FolhaPalmeira", folhagem, topo + new Vector3(1.3f, -0.35f, 0f), new Vector3(2.8f, 0.25f, 0.8f), "esfera");
            Juntar("FolhaPalmeira", folhagem, topo + new Vector3(-1.3f, -0.35f, 0f), new Vector3(2.8f, 0.25f, 0.8f), "esfera");
            Juntar("FolhaPalmeira", folhagem, topo + new Vector3(0f, -0.35f, 1.3f), new Vector3(0.8f, 0.25f, 2.8f), "esfera");
            Juntar("FolhaPalmeira", folhagem, topo + new Vector3(0f, -0.35f, -1.3f), new Vector3(0.8f, 0.25f, 2.8f), "esfera");
        }

        // ------------------------------------------------------------------ rodovia

        /// <summary>
        /// Acostamento largo de rodovia: a pista com faixa dupla amarela e tachões passa do lado da
        /// câmera; no fundo, o guard-rail, o capim, a cerca de arame, eucaliptos e morros. Placa
        /// verde da BR, cones nas pontas, caminhão e carro parados no outro acostamento.
        /// </summary>
        private static Transform MontarRodovia(Transform pai, ConfigFisica config, System.Random r, Material madeira)
        {
            float L = config.comprimentoCampo, meia = config.larguraCampo * 0.5f;
            Material asfaltoBR = Mat("AsfaltoRodovia", new Color(0.7f, 0.7f, 0.72f), Foto("asphalt_02", false), Foto("asphalt_02", true), 3f, 0.15f);
            Material capim = Mat("CapimSeco", new Color(0.86f, 0.8f, 0.52f), Foto("leafy_grass", false), Foto("leafy_grass", true), 2.5f, 0.03f);
            Material metal = Mat("GuardRail", new Color(0.8f, 0.82f, 0.84f), null, null, 1f, 0.75f);
            Material faixaBranca = Mat("FaixaBranca", new Color(0.95f, 0.95f, 0.93f), null, null, 1f, 0.25f);
            Material amarela = Mat("FaixaAmarelaRodovia", new Color(0.98f, 0.78f, 0.1f), null, null, 1f, 0.3f);
            Material placaVerde = Mat("PlacaVerde", new Color(0.04f, 0.38f, 0.2f), null, null, 1f, 0.35f);
            Material laranja = Mat("Cone", new Color(0.95f, 0.42f, 0.1f), null, null, 1f, 0.3f);
            Material troncoEucalipto = Mat("TroncoEucalipto", new Color(0.78f, 0.74f, 0.66f), null, null, 1f, 0.05f);
            Transform t = NovoMapa(pai, "Mapa_Rodovia");

            // Capim em volta de tudo
            Juntar("Capim", capim, new Vector3(0f, 0.001f, 0f), new Vector3(400f, 0.002f, 400f));

            // Asfalto: o acostamento (o campo), as duas faixas da pista e o outro acostamento
            const float larguraFaixa = 3.6f;
            float zLonge = meia + 0.3f, zPerto = -meia - 2f * larguraFaixa - 2.5f;
            Juntar("AsfaltoRodovia", asfaltoBR, new Vector3(0f, 0.003f, (zLonge + zPerto) * 0.5f), new Vector3(L + 300f, 0.004f, zLonge - zPerto));
            Juntar("LinhaBordo", faixaBranca, new Vector3(0f, 0.0055f, -meia + 0.1f), new Vector3(L + 300f, 0.002f, 0.15f));
            Juntar("LinhaBordo", faixaBranca, new Vector3(0f, 0.0055f, -meia - 2f * larguraFaixa - 0.1f), new Vector3(L + 300f, 0.002f, 0.15f));
            float zMeio = -meia - larguraFaixa;
            foreach (float d in new[] { -0.12f, 0.12f })
                Juntar("FaixaDupla", amarela, new Vector3(0f, 0.0055f, zMeio + d), new Vector3(L + 300f, 0.002f, 0.1f));
            for (float x = -L * 0.5f - 100f; x <= L * 0.5f + 100f; x += 8f)
                Juntar("Tachao", amarela, new Vector3(x, 0.02f, zMeio), new Vector3(0.15f, 0.03f, 0.12f));

            // Parados no outro acostamento
            Carro(t, new Vector3(L * 0.5f + 14f, 0f, zPerto + 1.2f), 0f, carro[r.Next(carro.Length)]);
            Caminhao(t, new Vector3(-L * 0.5f - 18f, 0f, zPerto + 1.4f), 0f, placaCores[r.Next(placaCores.Length)]);

            // Guard-rail no fundo do acostamento (a bola bate e volta)
            Juntar("LaminaGuardRail", metal, new Vector3(0f, 0.6f, meia + 0.2f), new Vector3(L + 160f, 0.32f, 0.06f));
            for (float x = -L * 0.5f - 80f; x <= L * 0.5f + 80f; x += 4f)
                Juntar("PosteGuardRail", grade, new Vector3(x, 0.4f, meia + 0.3f), new Vector3(0.12f, 0.8f, 0.15f));

            // Placa verde da BR
            Vector3 placa = new Vector3(L * 0.28f, 0f, meia + 4f);
            foreach (float x in new[] { -1.8f, 1.8f })
                Juntar("PostePlaca", metal, placa + new Vector3(x, 2.5f, 0.05f), new Vector3(0.12f, 5f, 0.12f));
            CaixaNoMundo("PlacaVerde", t, placa + new Vector3(0f, 4.3f, 0f), new Vector3(4.6f, 1.6f, 0.08f), placaVerde);
            Letreiro(t, placa + new Vector3(0f, 4.6f, -0.06f), "BR-116", Color.white, 0.075f);
            Letreiro(t, placa + new Vector3(0f, 3.95f, -0.06f), "KM 42", Color.white, 0.06f);

            // Cerca de arame com mourão de madeira separando o pasto
            float zCerca = meia + 9f;
            for (float x = -L * 0.5f - 80f; x <= L * 0.5f + 80f; x += 3f)
                Juntar("Mourao", madeira, new Vector3(x, 0.65f, zCerca), new Vector3(0.12f, 1.3f, 0.12f), "cilindro");
            foreach (float y in new[] { 0.5f, 0.85f, 1.2f })
                Juntar("Arame", grade, new Vector3(0f, y, zCerca), new Vector3(L + 160f, 0.015f, 0.015f));

            // Eucaliptos em fileira e morros de capim no horizonte
            for (int i = 0; i < 40; i++)
            {
                Vector3 p = new Vector3(-L * 0.5f - 60f + i * (L + 120f) / 40f + (float)(r.NextDouble() - 0.5) * 2f, 0f, meia + 22f + (i % 3) * 4f);
                float h = 12f + (float)r.NextDouble() * 5f;
                Juntar("TroncoEucalipto", troncoEucalipto, p + Vector3.up * h * 0.5f, new Vector3(0.35f, h, 0.35f), "cilindro");
                // Copa comprida e cheia, descendo até perto do meio do tronco
                Juntar("CopaEucalipto", folhagem, p + Vector3.up * (h * 0.72f), new Vector3(4.5f, h * 0.6f, 4.5f), "esfera");
                Juntar("CopaEucalipto", folhagem, p + new Vector3(0.8f, h * 0.55f, 0.3f), new Vector3(3.2f, h * 0.35f, 3.2f), "esfera");
            }
            for (int i = 0; i < 5; i++)
                Juntar("MorroCapim", capim, new Vector3(-160f + i * 80f + (float)(r.NextDouble() - 0.5) * 30f, -4f, meia + 110f + (float)r.NextDouble() * 40f),
                       new Vector3(140f + (float)r.NextDouble() * 60f, 40f + (float)r.NextDouble() * 25f, 80f), "esfera");

            // Cones nas pontas do acostamento
            foreach (float lado in new[] { -1f, 1f })
                for (float z = -meia + 1f; z < meia; z += 2.5f)
                    Cone(new Vector3(lado * (L * 0.5f + 0.4f), 0f, z), laranja, faixaBranca);

            ColisoesDoCampo(t, config);
            FinalizarJuntas(t, "Rodovia_");
            return t;
        }

        /// <summary>Caminhão baú parado (olhando para +X quando giro = 0).</summary>
        private static void Caminhao(Transform t, Vector3 baseC, float giro, Material pintura)
        {
            GameObject raiz = new GameObject("Caminhao");
            raiz.transform.SetParent(t);
            raiz.transform.position = baseC;
            raiz.transform.rotation = Quaternion.Euler(0f, giro, 0f);
            Transform c = raiz.transform;
            CaixaNoMundo("Chassi", c, Vector3.zero, Vector3.zero, pneu, new Vector3(0f, 0.75f, 0f), new Vector3(9f, 0.35f, 2.2f));
            CaixaNoMundo("Cabine", c, Vector3.zero, Vector3.zero, pintura, new Vector3(3.5f, 1.85f, 0f), new Vector3(2f, 2.1f, 2.4f));
            CaixaNoMundo("ParaBrisa", c, Vector3.zero, Vector3.zero, vidroCarro, new Vector3(4.51f, 2.2f, 0f), new Vector3(0.04f, 0.9f, 2.1f));
            CaixaNoMundo("Bau", c, Vector3.zero, Vector3.zero, placaFundo, new Vector3(-1f, 2.3f, 0f), new Vector3(6.6f, 2.8f, 2.5f));
            foreach (float x in new[] { -3.2f, -2f, 3.3f })
                foreach (float z in new[] { -1f, 1f })
                {
                    GameObject roda = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    roda.name = "Roda";
                    Object.DestroyImmediate(roda.GetComponent<Collider>());
                    roda.transform.SetParent(c, false);
                    roda.transform.localPosition = new Vector3(x, 0.5f, z);
                    roda.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                    roda.transform.localScale = new Vector3(1f, 0.15f, 1f);
                    roda.GetComponent<MeshRenderer>().sharedMaterial = pneu;
                }
        }

        // ------------------------------------------------------------------ pontilhão

        /// <summary>
        /// Embaixo do viaduto: o tabuleiro de concreto passa por cima do fundo do campo (dá sombra),
        /// apoiado em duas fileiras de pilares pichados; no fundo, a parede do encontro coberta de
        /// grafite. Mureta de concreto, pneus velhos, um sofá largado e mato do lado da câmera.
        /// </summary>
        private static Transform MontarPontilhao(Transform pai, ConfigFisica config, System.Random r)
        {
            float L = config.comprimentoCampo, meia = config.larguraCampo * 0.5f;
            Material concretoViaduto = Mat("ConcretoViaduto", new Color(0.72f, 0.71f, 0.68f), Foto("brushed_concrete", false), Foto("brushed_concrete", true), 2.5f, 0.05f);
            Material chao = Mat("ChaoPontilhao", new Color(1.1f, 1.06f, 1f), Foto("brushed_concrete", false), Foto("brushed_concrete", true), 3f, 0.04f);
            Material sofa = Mat("SofaVelho", new Color(0.42f, 0.3f, 0.22f), null, null, 1f, 0.05f);
            Transform t = NovoMapa(pai, "Mapa_Pontilhao");

            // Chão de concreto empoeirado, com terra acumulada no pé dos pilares
            Juntar("ChaoPontilhao", chao, new Vector3(0f, 0.002f, 0f), new Vector3(L + 80f, 0.004f, 2f * meia + 40f));
            Juntar("TerraPilares", terra, new Vector3(0f, 0.004f, meia + 4f), new Vector3(L + 80f, 0.004f, 6f));

            // Tabuleiro do viaduto: passa por cima do fundo do campo, com vigas e mureta New Jersey em cima
            const float yViga = 6.8f, espessura = 1.3f;
            float zIni = meia - 3f, zFim = meia + 14f;
            Juntar("Tabuleiro", concretoViaduto, new Vector3(0f, yViga + espessura * 0.5f, (zIni + zFim) * 0.5f), new Vector3(L + 200f, espessura, zFim - zIni));
            foreach (float z in new[] { zIni + 0.2f, zFim - 0.2f })
                Juntar("BarreiraViaduto", concretoViaduto, new Vector3(0f, yViga + espessura + 0.45f, z), new Vector3(L + 200f, 0.9f, 0.4f));
            foreach (float z in new[] { zIni + 1.2f, (zIni + zFim) * 0.5f, zFim - 1.2f })
                Juntar("Longarina", concretoViaduto, new Vector3(0f, yViga - 0.45f, z), new Vector3(L + 200f, 0.9f, 0.6f));

            // Pilares em duas fileiras, travessa em cima; os da frente pichados
            float zPilarFrente = meia + 2.5f, zPilarFundo = meia + 11f;
            for (float x = -L * 0.5f - 40f; x <= L * 0.5f + 40f; x += 16f)
            {
                foreach (float z in new[] { zPilarFrente, zPilarFundo })
                    Juntar("Pilar", concretoViaduto, new Vector3(x, (yViga - 1.8f) * 0.5f, z), new Vector3(1.1f, yViga - 1.8f, 1.1f));
                Juntar("Travessa", concretoViaduto, new Vector3(x, yViga - 1.35f, (zPilarFrente + zPilarFundo) * 0.5f), new Vector3(1.4f, 0.9f, zPilarFundo - zPilarFrente + 2f));
                if (r.NextDouble() < 0.8)
                    Pichar(t, new Vector3(x, 1.4f + (float)r.NextDouble() * 0.8f, zPilarFrente - 0.57f), 1.05f, pichacao[r.Next(pichacao.Length)]);
            }

            // Encontro do viaduto: a parede do fundo, tomada de grafite
            Juntar("Encontro", concretoViaduto, new Vector3(0f, yViga * 0.5f, zFim - 0.5f), new Vector3(L + 200f, yViga, 1f));
            for (float x = -L * 0.5f - 20f; x <= L * 0.5f + 20f; x += 4.5f + (float)r.NextDouble() * 3f)
                Pichar(t, new Vector3(x, 1.3f + (float)r.NextDouble() * 2.5f, zFim - 1.02f), 2.5f + (float)r.NextDouble() * 2f, pichacao[r.Next(pichacao.Length)]);

            // Luminárias penduradas embaixo do tabuleiro
            for (float x = -L * 0.5f; x <= L * 0.5f; x += 14f)
                Juntar("LuminariaViaduto", grade, new Vector3(x, yViga - 0.95f, zIni + 2f), new Vector3(0.6f, 0.12f, 0.3f));

            // Mureta baixa no fundo e nas pontas (a bola volta nela)
            Juntar("MuretaFundo", concretoViaduto, new Vector3(0f, 0.25f, meia + 0.15f), new Vector3(L + 0.6f, 0.5f, 0.3f));
            foreach (float lado in new[] { -1f, 1f })
            {
                Juntar("MuretaPonta", concretoViaduto, new Vector3(lado * (L * 0.5f + 0.15f), 0.25f, 0f), new Vector3(0.3f, 0.5f, 2f * meia + 0.6f));
                // Pneus velhos empilhados perto das pontas
                Vector3 p = new Vector3(lado * (L * 0.5f + 2f), 0f, meia - 2f);
                for (int i = 0; i < 4; i++)
                    Juntar("Pneu", pneu, p + new Vector3((float)(r.NextDouble() - 0.5) * 0.3f, 0.12f + i * 0.24f, (float)(r.NextDouble() - 0.5) * 0.3f), new Vector3(0.75f, 0.24f, 0.75f), "cilindro");
            }

            // Sofá largado atrás da mureta, virado para o campo (a arquibancada)
            Vector3 s = new Vector3(-L * 0.3f, 0f, meia + 1.6f);
            Juntar("SofaAssento", sofa, s + new Vector3(0f, 0.25f, 0f), new Vector3(2f, 0.5f, 0.85f));
            Juntar("SofaEncosto", sofa, s + new Vector3(0f, 0.65f, 0.35f), new Vector3(2f, 0.8f, 0.2f));
            foreach (float x in new[] { -1f, 1f })
                Juntar("SofaBraco", sofa, s + new Vector3(x, 0.45f, 0f), new Vector3(0.2f, 0.5f, 0.85f));

            // Lado da câmera: mureta baixa com mato e casinhas recuadas
            MuretaPerto(config, r);
            for (int i = 0; i < 18; i++)
                Juntar("Arbusto", folhagem, new Vector3(-L * 0.5f - 10f + i * (L + 20f) / 17f, 0.5f, -meia - 1.3f), new Vector3(1.6f, 1.1f, 1.2f) * (0.8f + (float)r.NextDouble() * 0.5f), "esfera");
            CasasLadoDeCa(config, r, -(meia + 11f));
            ColisoesDoCampo(t, config);
            FinalizarJuntas(t, "Pontilhao_");
            return t;
        }

        // ------------------------------------------------------------------ peças comuns

        /// <summary>Mureta baixa do lado da câmera (na altura do muro da rua, não tampa o jogo).</summary>
        private static void MuretaPerto(ConfigFisica config, System.Random r)
        {
            float L = config.comprimentoCampo, z = -config.larguraCampo * 0.5f - 0.1f, h = config.alturaMuroPerto;
            float x0 = -L * 0.5f - 22f;
            while (x0 < L * 0.5f + 22f)
            {
                float w = 4f + r.Next(6);
                Material m = r.NextDouble() < 0.4 ? tijolo : reboco[r.Next(reboco.Length)];
                Juntar("MuretaPerto", m, new Vector3(x0 + w * 0.5f, h * 0.5f, z), new Vector3(w, h, 0.2f));
                Juntar("CapaMureta", concreto, new Vector3(x0 + w * 0.5f, h + 0.03f, z), new Vector3(w + 0.02f, 0.06f, 0.26f));
                x0 += w;
            }
        }

        /// <summary>Casinhas térreas do lado da câmera, de frente para o campo (abaixo da linha da transmissão).</summary>
        private static void CasasLadoDeCa(ConfigFisica config, System.Random r, float zFrente)
        {
            float L = config.comprimentoCampo;
            float x0 = -L * 0.5f - 22f;
            while (x0 < L * 0.5f + 22f)
            {
                float w = 6f + r.Next(4);
                Material m = r.NextDouble() < 0.35 ? tijolo : reboco[r.Next(reboco.Length)];
                CasaVirada(new Vector3(x0 + w * 0.5f, 0f, zFrente), Vector3.forward, w - 0.5f, 2.7f, 8f, m, r);
                x0 += w;
            }
        }

        /// <summary>Faixa tracejada no chão ao longo do X.</summary>
        private static void Tracejado(Material material, float z, float xIni, float xFim, float traco, float espaco, float largura)
        {
            for (float x = xIni; x + traco <= xFim; x += traco + espaco)
                Juntar("Tracejado", material, new Vector3(x + traco * 0.5f, 0.0025f, z), new Vector3(traco, 0.002f, largura));
        }

        /// <summary>Cone de trânsito laranja com faixa branca.</summary>
        private static void Cone(Vector3 p, Material laranja, Material branco)
        {
            Juntar("BaseCone", laranja, p + Vector3.up * 0.02f, new Vector3(0.4f, 0.04f, 0.4f));
            Juntar("Cone", laranja, p + Vector3.up * 0.3f, new Vector3(0.2f, 0.52f, 0.2f), "cilindro");
            Juntar("FaixaCone", branco, p + Vector3.up * 0.36f, new Vector3(0.215f, 0.08f, 0.215f), "cilindro");
        }

        /// <summary>Texto pintado (placa), virado para a câmera (-Z).</summary>
        private static void Letreiro(Transform t, Vector3 posicao, string texto, Color cor, float tamanho)
        {
            GameObject g = new GameObject("Letreiro");
            g.transform.SetParent(t);
            g.transform.position = posicao;
            TextMesh tm = g.AddComponent<TextMesh>();
            tm.text = texto;
            tm.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            tm.fontSize = 64;
            tm.characterSize = tamanho;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.fontStyle = FontStyle.Bold;
            tm.color = cor;
            g.GetComponent<MeshRenderer>().sharedMaterial = tm.font.material;
        }
    }
}
