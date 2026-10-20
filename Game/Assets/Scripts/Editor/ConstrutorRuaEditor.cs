using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace BetsDeRua.Editor
{
    /// <summary>
    /// Monta a rua de bairro (partida e fundo do menu), com cara de rua brasileira de verdade:
    /// asfalto gasto com remendos, calçada de placas e meio-fio pintado, casas coloridas de
    /// reboco, sobrado de tijolo baiano sem reboco, comércio com portão de enrolar e placa,
    /// muros com pichação, lajes com caixa d'água azul, telhado de telha, grades nas janelas,
    /// postes com fios, árvores, carros parados nas pontas, amarelinha de giz e um morro com
    /// casinhas ao fundo. A física continua a mesma (ConfigFisica): calçada, fachadas e muros do
    /// fim da rua têm colisão; o resto é só cenário. Tudo é sorteado com semente fixa (sempre igual).
    /// </summary>
    public static partial class ConstrutorRuaEditor
    {
        private const string PastaMalhas = "Assets/Malhas";
        private const string PastaMateriais = "Assets/Materiais/Rua";
        private static readonly Dictionary<string, Mesh> malhas = new Dictionary<string, Mesh>();
        private static readonly Dictionary<string, List<CombineInstance>> juntar = new Dictionary<string, List<CombineInstance>>();
        private static readonly Dictionary<string, Material> materiaisJuntar = new Dictionary<string, Material>();

        private static Material asfalto, calcada, meioFio, tijolo, telha, janela, grade, porta, caixaDagua, concreto,
                                fio, folhagem, tronco, terra, giz, placaFundo, vidroCarro, pneu, faixaAmarela;
        private static Material[] reboco, portao, pichacao, carro, placaCores;

        public static void Montar(Transform pai, ConfigFisica config)
        {
            malhas.Clear();
            juntar.Clear();
            materiaisJuntar.Clear();
            CriarMateriais();
            System.Random r = new System.Random(1977);

            float L = config.comprimentoCampo;
            float meiaLargura = config.larguraCampo * 0.5f;
            float meiaRua = config.larguraRua * 0.5f;
            float larguraCalcada = meiaLargura - meiaRua;
            float hCalcada = config.alturaCalcada;

            GameObject rua = new GameObject("Rua");
            rua.transform.SetParent(pai);
            // Tudo o que é só da rua de asfalto fica num grupo (os outros lugares desligam ele)
            GameObject soRua = new GameObject("SoDaRua");
            soRua.transform.SetParent(rua.transform);
            Transform t = soRua.transform;
            raizAvulsos = t;

            // ---------- Chão: asfalto da rua (um pouco além das pontas) e terra em volta ----------
            Caixa("Asfalto", t, new Vector3(0f, 0.0008f, 0f), new Vector3(L + 40f, 0.001f, config.larguraRua + 0.2f), asfalto, false);
            Caixa("TerraEmVolta", rua.transform, new Vector3(0f, -0.02f, 0f), new Vector3(400f, 0.02f, 400f), terra, false);

            // Faixa amarela tracejada no meio da rua
            const float traco = 3f, espaco = 2f;
            for (float x = -L * 0.5f - 18f; x + traco <= L * 0.5f + 18f; x += traco + espaco)
                Juntar("FaixaAmarela", faixaAmarela, new Vector3(x + traco * 0.5f, 0.0025f, 0f), new Vector3(traco, 0.002f, 0.12f));

            // Bueiros e remendos escuros no asfalto
            for (int i = 0; i < 6; i++)
            {
                float x = -L * 0.5f + (i + 0.5f) * L / 6f + (float)(r.NextDouble() - 0.5) * 4f;
                float z = (r.NextDouble() < 0.5 ? -1f : 1f) * (meiaRua - 0.45f);
                Juntar("Bueiro", grade, new Vector3(x, 0.003f, z), new Vector3(0.9f, 0.004f, 0.45f));
            }

            // ---------- Calçadas e meio-fio (com colisão: os jogadores sobem) ----------
            foreach (float lado in new[] { -1f, 1f })
            {
                string nome = lado < 0f ? "Perto" : "Fundo";
                float zCalcada = lado * (meiaRua + larguraCalcada * 0.5f);
                GameObject c = Caixa($"Calcada{nome}", t, new Vector3(0f, hCalcada * 0.5f, zCalcada),
                                     new Vector3(L + 40f, hCalcada, larguraCalcada), calcada, false);
                // Colisão só dentro do campo (o jogo tem muro nas pontas)
                BoxCollider col = c.AddComponent<BoxCollider>();
                col.size = new Vector3(L, hCalcada, larguraCalcada);
                Caixa($"MeioFio{nome}", t, new Vector3(0f, hCalcada * 0.5f + 0.003f, lado * (meiaRua + 0.07f)),
                      new Vector3(L + 40f, hCalcada + 0.006f, 0.14f), meioFio, false);
            }

            // ---------- Fundo: as casas ----------
            MontarFachadas(t, config, r, 1f);
            // ---------- Perto da câmera: muro baixo (não pode tampar o campo) ----------
            MontarMuroPerto(t, config, r);

            // Colisões invisíveis das fachadas (os jogadores não atravessam)
            ColisaoInvisivel("ColisaoFundo", t, new Vector3(0f, 2f, meiaLargura + config.espessuraMuro * 0.5f), new Vector3(L, 4f, config.espessuraMuro));
            ColisaoInvisivel("ColisaoPerto", t, new Vector3(0f, 1f, -meiaLargura - config.espessuraMuro * 0.5f), new Vector3(L, 2f, config.espessuraMuro));

            // ---------- Fim da rua: muro de tijolo com portão (a bola volta), carros parados atrás ----------
            float hFim = config.alturaMuro, eFim = config.espessuraMuro;
            foreach (float lado in new[] { -1f, 1f })
            {
                GameObject muro = Caixa(lado < 0 ? "MuroFimA" : "MuroFimB", t, new Vector3(lado * (L + eFim) * 0.5f, hFim * 0.5f, 0f),
                                        new Vector3(eFim, hFim, config.larguraCampo + 2f * eFim), tijolo, false);
                muro.AddComponent<BoxCollider>();
                Carro(t, new Vector3(lado * (L * 0.5f + 4.5f), 0f, -meiaRua + 1.3f), lado < 0 ? 0f : 180f, carro[r.Next(carro.Length)]);
                Carro(t, new Vector3(lado * (L * 0.5f + 11f), 0f, meiaRua - 1.3f), lado < 0 ? 180f : 0f, carro[r.Next(carro.Length)]);
            }

            // ---------- Postes com fios (calçada do fundo) e árvores nas pontas ----------
            var topos = new List<Vector3>();
            for (float x = -L * 0.5f - 6f; x <= L * 0.5f + 6f; x += 23f)
                topos.Add(Poste(t, new Vector3(x, hCalcada, meiaLargura - 0.45f)));
            for (int i = 0; i < topos.Count - 1; i++)
                for (int f = 0; f < 3; f++)
                    Fio(t, topos[i] + new Vector3(0f, -f * 0.25f, (f - 1) * 0.35f), topos[i + 1] + new Vector3(0f, -f * 0.25f, (f - 1) * 0.35f), 0.7f + f * 0.15f);
            foreach (float x in new[] { -L * 0.5f - 3f, L * 0.5f + 3f, -L * 0.5f + 4f, L * 0.5f - 4f })
                Arvore(t, new Vector3(x, hCalcada, meiaLargura - 1.1f), r);

            // ---------- Amarelinha de giz na calçada do fundo ----------
            Amarelinha(t, new Vector3(-L * 0.25f, hCalcada + 0.004f, meiaRua + larguraCalcada * 0.5f));

            // ---------- Casas fechando as pontas da rua e do lado de cá (para as câmeras de perto) ----------
            MontarPontasELadoDeCa(t, config, r);

            FinalizarJuntas(t, "");

            // ---------- Morro com casinhas ao fundo (some na fazenda) ----------
            GameObject morro = new GameObject("Morro");
            morro.transform.SetParent(rua.transform);
            MontarMorro(morro.transform, config, r);
            FinalizarJuntas(morro.transform, "Morro_");

            // ---------- Os outros lugares: terrão, campinho, fazenda, favela, avenida, rodovia, pontilhão (desligados; a partida sorteia) ----------
            MontarMapas(rua.transform, config, r);

            foreach (Transform filho in rua.GetComponentsInChildren<Transform>(true))
                GameObjectUtility.SetStaticEditorFlags(filho.gameObject, StaticEditorFlags.BatchingStatic | StaticEditorFlags.ContributeGI);
        }

        // ------------------------------------------------------------------ casas

        private static void MontarFachadas(Transform pai, ConfigFisica config, System.Random r, float lado)
        {
            float L = config.comprimentoCampo;
            float z0 = config.larguraCampo * 0.5f; // frente das casas (encosta na calçada)
            float hBase = config.alturaCalcada;
            GameObject raiz = new GameObject("Casas");
            raiz.transform.SetParent(pai);
            Transform t = raiz.transform;
            string[] placas = { "BAR DO ZÉ", "MERCADINHO", "AÇAÍ", "SALÃO DA RÔ", "OFICINA", "PADARIA", "LAN HOUSE", "CHAVEIRO" };
            int placa = r.Next(placas.Length);
            float x0 = -L * 0.5f - 22f;
            int i = 0;
            while (x0 < L * 0.5f + 22f)
            {
                float w = 5f + r.Next(5);
                float xc = x0 + w * 0.5f;
                int tipo = r.Next(10);
                Material corParede = reboco[r.Next(reboco.Length)];
                if (tipo <= 3) CasaTerrea(t, xc, z0, hBase, w, corParede, r);
                else if (tipo <= 5) Sobrado(t, xc, z0, hBase, w, corParede, r);
                else if (tipo <= 7) MuroComCasaAtras(t, xc, z0, hBase, w, corParede, r);
                else Comercio(t, xc, z0, hBase, w, corParede, placas[placa++ % placas.Length], r);
                // Pichação em algumas paredes
                if (r.NextDouble() < 0.4)
                    Pichar(t, new Vector3(xc + (float)(r.NextDouble() - 0.5) * w * 0.5f, hBase + 0.9f + (float)r.NextDouble() * 0.6f, z0 - 0.015f),
                           1.6f + (float)r.NextDouble() * 1.2f, pichacao[r.Next(pichacao.Length)]);
                x0 += w;
                i++;
            }
        }

        private static void CasaTerrea(Transform t, float xc, float z0, float hb, float w, Material parede, System.Random r)
        {
            float h = 3.1f + (float)r.NextDouble() * 0.4f;
            float fundo = 7f;
            Juntar("Parede", parede, new Vector3(xc, hb + h * 0.5f, z0 + fundo * 0.5f), new Vector3(w, h, fundo));
            Barrado(xc, z0, hb, w);
            bool telhado = r.NextDouble() < 0.5;
            if (telhado) Telhado(t, xc, z0, hb + h, w, fundo);
            else
            {
                Juntar("Platibanda", parede, new Vector3(xc, hb + h + 0.25f, z0 + 0.1f), new Vector3(w, 0.5f, 0.2f));
                CaixaDagua(new Vector3(xc + w * 0.25f, hb + h, z0 + 3f), r);
            }
            Porta(xc - w * 0.3f, z0, hb);
            Janela(xc + w * 0.15f, z0, hb + 1.1f, 1.3f, 1.1f, true);
            if (w >= 7f) PortaoGaragem(xc + w * 0.35f - 1.2f, z0, hb, 2.6f, 2.3f, r);
        }

        private static void Sobrado(Transform t, float xc, float z0, float hb, float w, Material parede, System.Random r)
        {
            float h1 = 3f, h2 = 2.9f, fundo = 8f;
            // Térreo rebocado e pintado; andar de cima no tijolo (obra que nunca termina)
            Juntar("Parede", parede, new Vector3(xc, hb + h1 * 0.5f, z0 + fundo * 0.5f), new Vector3(w, h1, fundo));
            Barrado(xc, z0, hb, w);
            bool rebocado = r.NextDouble() < 0.4;
            Juntar(rebocado ? "Parede2" : "Tijolo", rebocado ? parede : tijolo, new Vector3(xc, hb + h1 + h2 * 0.5f, z0 + fundo * 0.5f + 0.1f), new Vector3(w - 0.2f, h2, fundo - 0.2f));
            Juntar("Laje", concreto, new Vector3(xc, hb + h1 + 0.06f, z0 - 0.35f), new Vector3(w, 0.12f, 0.9f));
            // Grade da sacada
            for (float x = xc - w * 0.5f + 0.15f; x <= xc + w * 0.5f; x += 0.18f)
                Juntar("GradeSacada", grade, new Vector3(x, hb + h1 + 0.55f, z0 - 0.75f), new Vector3(0.025f, 0.9f, 0.025f));
            Juntar("CorrimaoSacada", grade, new Vector3(xc, hb + h1 + 1.0f, z0 - 0.75f), new Vector3(w, 0.05f, 0.05f));
            // Vergalhões para cima (a laje do próximo andar)
            for (int k = 0; k < 4; k++)
                Juntar("Vergalhao", grade, new Vector3(xc - w * 0.4f + k * w * 0.27f, hb + h1 + h2 + 0.4f, z0 + 0.3f), new Vector3(0.025f, 0.8f, 0.025f));
            CaixaDagua(new Vector3(xc - w * 0.2f, hb + h1 + h2, z0 + 4f), r);
            Porta(xc - w * 0.25f, z0, hb);
            PortaoGaragem(xc + w * 0.18f, z0, hb, Mathf.Min(3f, w * 0.45f), 2.4f, r);
            Janela(xc - w * 0.2f, z0 + 0.1f, hb + h1 + 0.9f, 1.2f, 1f, false);
            Janela(xc + w * 0.22f, z0 + 0.1f, hb + h1 + 0.9f, 1.2f, 1f, false);
        }

        private static void MuroComCasaAtras(Transform t, float xc, float z0, float hb, float w, Material parede, System.Random r)
        {
            float hMuro = 2.3f;
            Juntar("Muro", r.NextDouble() < 0.35 ? tijolo : parede, new Vector3(xc, hb + hMuro * 0.5f, z0 + 0.1f), new Vector3(w, hMuro, 0.2f));
            Barrado(xc, z0, hb, w);
            // Portão de grade (dá para ver o quintal)
            float lp = Mathf.Min(2.6f, w * 0.4f);
            float xp = xc - w * 0.2f;
            Juntar("VaoPortao", janela, new Vector3(xp, hb + 1.05f, z0 + 0.05f), new Vector3(lp, 2.1f, 0.12f));
            for (float x = xp - lp * 0.5f + 0.06f; x < xp + lp * 0.5f; x += 0.12f)
                Juntar("GradePortao", portao[r.Next(portao.Length)], new Vector3(x, hb + 1.05f, z0 - 0.02f), new Vector3(0.03f, 2.1f, 0.03f));
            // Casa recuada com telhado aparecendo
            float h = 3.3f;
            Juntar("Parede", reboco[r.Next(reboco.Length)], new Vector3(xc, hb + h * 0.5f, z0 + 3f + 3f), new Vector3(w - 0.4f, h, 6f));
            Telhado(t, xc, z0 + 3f, hb + h, w - 0.4f, 6f);
        }

        private static void Comercio(Transform t, float xc, float z0, float hb, float w, Material parede, string nome, System.Random r)
        {
            float h = 3.6f, fundo = 8f;
            Juntar("Parede", parede, new Vector3(xc, hb + h * 0.5f, z0 + fundo * 0.5f), new Vector3(w, h, fundo));
            Juntar("Platibanda", parede, new Vector3(xc, hb + h + 0.4f, z0 + 0.1f), new Vector3(w, 0.8f, 0.2f));
            // Portão de enrolar meio aberto, com o escuro da loja embaixo
            float lp = w * 0.7f;
            Juntar("Loja", janela, new Vector3(xc, hb + 1.1f, z0 + 0.04f), new Vector3(lp, 2.2f, 0.1f));
            CaixaNoMundo("PortaoEnrolar", t, new Vector3(xc, hb + 2.45f, z0 - 0.04f), new Vector3(lp, 1.3f, 0.06f), portao[r.Next(portao.Length)]);
            Juntar("Toldo", placaCores[r.Next(placaCores.Length)], new Vector3(xc, hb + 3.05f, z0 - 0.5f), new Vector3(lp + 0.4f, 0.08f, 1f));
            // Placa com o nome
            Material corPlaca = placaCores[r.Next(placaCores.Length)];
            CaixaNoMundo("Placa", t, new Vector3(xc, hb + 3.55f, z0 - 0.08f), new Vector3(w * 0.8f, 0.75f, 0.06f), corPlaca);
            GameObject texto = new GameObject("TextoPlaca");
            texto.transform.SetParent(t);
            texto.transform.position = new Vector3(xc, hb + 3.55f, z0 - 0.12f);
            TextMesh tm = texto.AddComponent<TextMesh>();
            tm.text = nome;
            tm.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            tm.fontSize = 64;
            tm.characterSize = 0.055f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.fontStyle = FontStyle.Bold;
            tm.color = corPlaca.GetColor("_BaseColor").grayscale > 0.55f ? new Color(0.1f, 0.1f, 0.12f) : Color.white;
            texto.GetComponent<MeshRenderer>().sharedMaterial = tm.font.material;
            // Mesa e cadeiras de plástico na calçada (bar)
            if (nome.StartsWith("BAR"))
            {
                Material plastico = placaCores[0];
                Juntar("Mesa", plastico, new Vector3(xc + w * 0.3f, hb + 0.72f, z0 - 1.2f), new Vector3(0.7f, 0.04f, 0.7f));
                Juntar("PeMesa", plastico, new Vector3(xc + w * 0.3f, hb + 0.36f, z0 - 1.2f), new Vector3(0.06f, 0.72f, 0.06f));
                Juntar("Cadeira", plastico, new Vector3(xc + w * 0.3f + 0.6f, hb + 0.45f, z0 - 1.2f), new Vector3(0.45f, 0.06f, 0.45f));
                Juntar("Encosto", plastico, new Vector3(xc + w * 0.3f + 0.82f, hb + 0.75f, z0 - 1.2f), new Vector3(0.05f, 0.6f, 0.45f));
            }
        }

        private static void Barrado(float xc, float z0, float hb, float w)
        {
            // Barra mais escura embaixo da parede (pintura de rodapé, comum nas casas)
            Juntar("Barrado", concreto, new Vector3(xc, hb + 0.2f, z0 - 0.012f), new Vector3(w, 0.4f, 0.02f));
        }

        private static void Porta(float x, float z0, float hb)
        {
            Juntar("Porta", porta, new Vector3(x, hb + 1.05f, z0 - 0.03f), new Vector3(0.95f, 2.1f, 0.06f));
            Juntar("Macaneta", grade, new Vector3(x + 0.35f, hb + 1.0f, z0 - 0.07f), new Vector3(0.05f, 0.05f, 0.04f));
        }

        private static void Janela(float x, float z0, float y, float largura, float altura, bool comGrade)
        {
            Juntar("Janela", janela, new Vector3(x, y + altura * 0.5f, z0 - 0.02f), new Vector3(largura, altura, 0.05f));
            Juntar("Peitoril", concreto, new Vector3(x, y - 0.04f, z0 - 0.08f), new Vector3(largura + 0.2f, 0.08f, 0.16f));
            if (!comGrade) return;
            for (float xx = x - largura * 0.5f + 0.1f; xx < x + largura * 0.5f; xx += 0.16f)
                Juntar("Grade", grade, new Vector3(xx, y + altura * 0.5f, z0 - 0.07f), new Vector3(0.025f, altura, 0.025f));
            Juntar("GradeH", grade, new Vector3(x, y + altura * 0.5f, z0 - 0.07f), new Vector3(largura, 0.025f, 0.025f));
        }

        private static void PortaoGaragem(float x, float z0, float hb, float largura, float altura, System.Random r)
        {
            CaixaNoMundo("PortaoGaragem", null, new Vector3(x, hb + altura * 0.5f, z0 - 0.03f), new Vector3(largura, altura, 0.05f), portao[r.Next(portao.Length)]);
        }

        private static void Telhado(Transform t, float xc, float z0, float y, float w, float fundo)
        {
            // Duas águas: caindo para a frente e para trás (telha cerâmica), com beiral
            float meio = fundo * 0.5f;
            float inclinacao = 22f;
            float comprimento = meio / Mathf.Cos(inclinacao * Mathf.Deg2Rad) + 0.5f;
            float subida = Mathf.Tan(inclinacao * Mathf.Deg2Rad) * meio;
            foreach (float lado in new[] { -1f, 1f })
            {
                GameObject agua = CaixaNoMundo("Telhado", t, new Vector3(xc, y + subida * 0.5f, z0 + meio + lado * meio * 0.5f),
                                               new Vector3(w + 0.4f, 0.08f, comprimento), telha);
                agua.transform.rotation = Quaternion.Euler(lado * inclinacao, 0f, 0f);
            }
        }

        private static void CaixaDagua(Vector3 baseCentro, System.Random r)
        {
            // A caixa d'água azul em cima da laje, marca registrada do bairro
            Juntar("CaixaDagua", caixaDagua, baseCentro + new Vector3(0f, 0.55f, 0f), new Vector3(1.3f, 1.1f, 1.3f), "cilindro");
            Juntar("TampaCaixa", caixaDagua, baseCentro + new Vector3(0f, 1.15f, 0f), new Vector3(1.36f, 0.1f, 1.36f), "cilindro");
        }

        private static void Pichar(Transform t, Vector3 centro, float tamanho, Material material) => Pichar(t, centro, tamanho, material, Quaternion.identity);

        private static void Pichar(Transform t, Vector3 centro, float tamanho, Material material, Quaternion giro)
        {
            GameObject q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            q.name = "Pichacao";
            Object.DestroyImmediate(q.GetComponent<Collider>());
            q.transform.SetParent(t);
            q.transform.position = centro;
            q.transform.rotation = giro; // sem giro, olha para -Z (para a rua)
            q.transform.localScale = new Vector3(tamanho, tamanho * 0.55f, 1f);
            q.GetComponent<MeshRenderer>().sharedMaterial = material;
            q.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        // ------------------------------------------------------------------ perto da câmera

        private static void MontarMuroPerto(Transform t, ConfigFisica config, System.Random r)
        {
            float L = config.comprimentoCampo;
            float z = -config.larguraCampo * 0.5f;
            float h = config.alturaMuroPerto;
            float hb = config.alturaCalcada;
            float x0 = -L * 0.5f - 22f;
            while (x0 < L * 0.5f + 22f)
            {
                float w = 4f + r.Next(6);
                float xc = x0 + w * 0.5f;
                Material m = r.NextDouble() < 0.3 ? tijolo : reboco[r.Next(reboco.Length)];
                Juntar("MuroPerto", m, new Vector3(xc, hb + h * 0.5f, z - 0.1f), new Vector3(w, h, 0.2f));
                Juntar("CapaMuro", concreto, new Vector3(xc, hb + h + 0.03f, z - 0.1f), new Vector3(w + 0.02f, 0.06f, 0.26f));
                // Matinho atrás do muro (só aparece por cima)
                if (r.NextDouble() < 0.5)
                    Juntar("Arbusto", folhagem, new Vector3(xc + (float)(r.NextDouble() - 0.5) * w * 0.6f, hb + h * 0.7f, z - 1.2f),
                           new Vector3(1.6f, 1.2f, 1.2f) * (0.8f + (float)r.NextDouble() * 0.5f), "esfera");
                x0 += w;
            }
        }

        // ------------------------------------------------------------------ objetos

        private static Vector3 Poste(Transform t, Vector3 baseP)
        {
            const float altura = 8.5f;
            Juntar("Poste", concreto, baseP + new Vector3(0f, altura * 0.5f, 0f), new Vector3(0.24f, altura, 0.24f));
            Juntar("Cruzeta", concreto, baseP + new Vector3(0f, altura - 0.4f, 0f), new Vector3(0.12f, 0.12f, 1.4f));
            // Braço da luminária para a rua
            Juntar("Braco", concreto, baseP + new Vector3(0f, altura - 1.4f, -0.9f), new Vector3(0.07f, 0.07f, 1.8f));
            Juntar("Luminaria", grade, baseP + new Vector3(0f, altura - 1.48f, -1.8f), new Vector3(0.28f, 0.12f, 0.55f));
            Juntar("Transformador", concreto, baseP + new Vector3(0f, altura - 2.4f, 0.25f), new Vector3(0.5f, 0.7f, 0.4f), "cilindro");
            return baseP + new Vector3(0f, altura - 0.35f, 0f);
        }

        private static void Fio(Transform t, Vector3 a, Vector3 b, float barriga)
        {
            GameObject g = new GameObject("Fio");
            g.transform.SetParent(t);
            LineRenderer lr = g.AddComponent<LineRenderer>();
            const int pontos = 14;
            lr.positionCount = pontos;
            for (int i = 0; i < pontos; i++)
            {
                float u = i / (float)(pontos - 1);
                lr.SetPosition(i, Vector3.Lerp(a, b, u) + Vector3.down * barriga * 4f * u * (1f - u));
            }
            lr.widthMultiplier = 0.025f;
            lr.sharedMaterial = fio;
            lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        private static void Arvore(Transform t, Vector3 baseA, System.Random r)
        {
            float h = 2.6f + (float)r.NextDouble() * 1.2f;
            Juntar("Tronco", tronco, baseA + new Vector3(0f, h * 0.5f, 0f), new Vector3(0.22f, h, 0.22f), "cilindro");
            for (int i = 0; i < 4; i++)
            {
                Vector3 o = new Vector3((float)(r.NextDouble() - 0.5) * 1.4f, h + 0.3f + (float)r.NextDouble() * 0.9f, (float)(r.NextDouble() - 0.5) * 1.2f);
                Juntar("Copa", folhagem, baseA + o, Vector3.one * (1.5f + (float)r.NextDouble() * 1.0f), "esfera");
            }
            Juntar("Canteiro", concreto, baseA + new Vector3(0f, 0.04f, 0f), new Vector3(0.9f, 0.08f, 0.9f));
        }

        private static void Carro(Transform t, Vector3 baseC, float giro, Material pintura)
        {
            GameObject raiz = new GameObject("Carro");
            raiz.transform.SetParent(t);
            raiz.transform.position = baseC;
            raiz.transform.rotation = Quaternion.Euler(0f, giro, 0f);
            Transform c = raiz.transform;
            CaixaNoMundo("Lataria", c, Vector3.zero, Vector3.zero, pintura, new Vector3(0f, 0.62f, 0f), new Vector3(4f, 0.6f, 1.7f));
            CaixaNoMundo("Cabine", c, Vector3.zero, Vector3.zero, vidroCarro, new Vector3(-0.2f, 1.12f, 0f), new Vector3(2.1f, 0.5f, 1.5f));
            CaixaNoMundo("Teto", c, Vector3.zero, Vector3.zero, pintura, new Vector3(-0.2f, 1.39f, 0f), new Vector3(1.9f, 0.05f, 1.42f));
            foreach (float x in new[] { -1.3f, 1.3f })
                foreach (float z in new[] { -0.82f, 0.82f })
                {
                    GameObject roda = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    roda.name = "Roda";
                    Object.DestroyImmediate(roda.GetComponent<Collider>());
                    roda.transform.SetParent(c, false);
                    roda.transform.localPosition = new Vector3(x, 0.32f, z);
                    roda.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                    roda.transform.localScale = new Vector3(0.64f, 0.12f, 0.64f);
                    roda.GetComponent<MeshRenderer>().sharedMaterial = pneu;
                }
        }

        private static void Amarelinha(Transform t, Vector3 inicio)
        {
            // Casas da amarelinha (1, 2, 3-4, 5, 6-7, 8, céu), riscadas com giz
            float c = 0.5f, e = 0.035f;
            int[] largura = { 1, 1, 2, 1, 2, 1 };
            float x = inicio.x;
            foreach (int n in largura)
            {
                float w = c * n;
                Linha(t, new Vector3(x, inicio.y, inicio.z - w * 0.5f), new Vector3(x + c, inicio.y, inicio.z - w * 0.5f), e);
                Linha(t, new Vector3(x, inicio.y, inicio.z + w * 0.5f), new Vector3(x + c, inicio.y, inicio.z + w * 0.5f), e);
                Linha(t, new Vector3(x, inicio.y, inicio.z - w * 0.5f), new Vector3(x, inicio.y, inicio.z + w * 0.5f), e);
                if (n == 2) Linha(t, new Vector3(x, inicio.y, inicio.z), new Vector3(x + c, inicio.y, inicio.z), e);
                x += c;
            }
            Linha(t, new Vector3(x, inicio.y, inicio.z - c * 0.5f), new Vector3(x, inicio.y, inicio.z + c * 0.5f), e);
            // Céu: meia-lua
            for (int i = 0; i < 8; i++)
            {
                float a0 = Mathf.Lerp(-90f, 90f, i / 8f) * Mathf.Deg2Rad, a1 = Mathf.Lerp(-90f, 90f, (i + 1) / 8f) * Mathf.Deg2Rad;
                Linha(t, new Vector3(x + Mathf.Cos(a0) * c * 0.6f, inicio.y, inicio.z + Mathf.Sin(a0) * c * 0.5f),
                      new Vector3(x + Mathf.Cos(a1) * c * 0.6f, inicio.y, inicio.z + Mathf.Sin(a1) * c * 0.5f), e);
            }
        }

        private static void Linha(Transform t, Vector3 a, Vector3 b, float espessura)
        {
            Vector3 meio = (a + b) * 0.5f;
            Vector3 d = b - a;
            GameObject g = CaixaNoMundo("Giz", t, meio, new Vector3(d.magnitude + espessura, 0.002f, espessura), giz);
            g.transform.rotation = Quaternion.FromToRotation(Vector3.right, d.normalized);
        }

        /// <summary>
        /// Casa simples virada para qualquer lado (n = para onde a fachada olha): parede, porta,
        /// janelas com grade por andar, laje com caixa d'água ou telhado plano.
        /// </summary>
        private static void CasaVirada(Vector3 frente, Vector3 n, float largura, float altura, float fundo, Material parede, System.Random r)
        {
            Vector3 u = Vector3.Cross(Vector3.up, n);
            Vector3 au = new Vector3(Mathf.Abs(u.x), 0f, Mathf.Abs(u.z)), an = new Vector3(Mathf.Abs(n.x), 0f, Mathf.Abs(n.z));
            Juntar("CasaVirada", parede, frente - n * fundo * 0.5f + Vector3.up * altura * 0.5f, au * largura + Vector3.up * altura + an * fundo);
            Juntar("Barrado", concreto, frente + n * 0.012f + Vector3.up * 0.2f, au * largura + Vector3.up * 0.4f + an * 0.02f);
            Juntar("Porta", r.NextDouble() < 0.5 ? porta : portao[r.Next(portao.Length)], frente + n * 0.03f + u * (-largura * 0.28f) + Vector3.up * 1.05f,
                   au * (r.NextDouble() < 0.4 ? 2.4f : 0.95f) + Vector3.up * 2.1f + an * 0.06f);
            int andares = Mathf.Max(1, Mathf.FloorToInt(altura / 2.9f));
            for (int a = 0; a < andares; a++)
            {
                float y = a * 2.9f + 1.3f;
                foreach (float lado in a == 0 ? new[] { 0.22f } : new[] { -0.22f, 0.22f })
                {
                    Juntar("Janela", janela, frente + n * 0.02f + u * (largura * lado) + Vector3.up * (y + 0.5f), au * 1.2f + Vector3.up * 1f + an * 0.05f);
                    Juntar("Peitoril", concreto, frente + n * 0.08f + u * (largura * lado) + Vector3.up * (y - 0.04f), au * 1.4f + Vector3.up * 0.08f + an * 0.16f);
                }
            }
            if (r.NextDouble() < 0.6)
                Juntar("CaixaDagua", caixaDagua, frente - n * fundo * 0.6f + u * (largura * 0.2f) + Vector3.up * (altura + 0.55f), new Vector3(1.3f, 1.1f, 1.3f), "cilindro");
        }

        private static void MontarPontasELadoDeCa(Transform t, ConfigFisica config, System.Random r)
        {
            float L = config.comprimentoCampo;
            float meiaLargura = config.larguraCampo * 0.5f;
            float hb = 0f;
            // Pontas: uma fileira de casas atravessada (a rua termina numa rua de frente, em T)
            foreach (float lado in new[] { -1f, 1f })
            {
                float x = lado * (L * 0.5f + 22f);
                Vector3 n = new Vector3(-lado, 0f, 0f);
                // Calçada da rua de frente
                Juntar("CalcadaPonta", calcada, new Vector3(x - lado * 1.5f, 0.07f, 0f), new Vector3(3f, 0.14f, 120f));
                float z0 = -60f;
                while (z0 < 60f)
                {
                    float w = 5f + r.Next(5);
                    float h = r.NextDouble() < 0.5 ? 3.2f + (float)r.NextDouble() * 0.6f : 5.9f + (float)r.NextDouble() * 2.6f;
                    Material m = r.NextDouble() < 0.3 ? tijolo : reboco[r.Next(reboco.Length)];
                    CasaVirada(new Vector3(x, hb, z0 + w * 0.5f), n, w, h, 9f, m, r);
                    if (r.NextDouble() < 0.35)
                        Pichar(t, new Vector3(x - lado * 0.02f, 1.4f, z0 + w * 0.5f + 1f), 1.8f, pichacao[r.Next(pichacao.Length)], Quaternion.Euler(0f, lado * 90f, 0f));
                    z0 += w;
                }
                // Poste na esquina
                Poste(t, new Vector3(x - lado * 1.2f, 0.14f, meiaLargura + 1f));
            }
            // Lado de cá (atrás do muro baixo): casas térreas recuadas, abaixo da linha da câmera de transmissão
            float zFrente = -(meiaLargura + 11f);
            float x0 = -L * 0.5f - 22f;
            while (x0 < L * 0.5f + 22f)
            {
                float w = 6f + r.Next(4);
                Material m = r.NextDouble() < 0.35 ? tijolo : reboco[r.Next(reboco.Length)];
                CasaVirada(new Vector3(x0 + w * 0.5f, hb, zFrente), Vector3.forward, w - 0.5f, 2.7f, 8f, m, r);
                x0 += w;
            }
            // Quintal entre o muro e as casas: um pouco de calçada e mato
            Juntar("Quintal", calcada, new Vector3(0f, 0.01f, -(meiaLargura + 5.5f)), new Vector3(L + 44f, 0.02f, 10.6f));
        }

        private static void MontarMorro(Transform pai, ConfigFisica config, System.Random r)
        {
            // Um morro de mato atrás das casas, coberto de casinhas de tijolo aparente e reboco,
            // com janela, laje e caixa d'água (como as comunidades de verdade). Fica mais longe e mais
            // baixo para não tomar a tela da transmissão; cada casinha fica assentada na encosta.
            float zCentro = config.larguraCampo * 0.5f + 88f;
            Vector3 raio = new Vector3(170f, 22f, 52f);
            Material mato = Mat("MatoMorro", new Color(0.55f, 0.62f, 0.42f), Foto("leafy_grass", false), Foto("leafy_grass", true), 9f, 0.03f);
            Juntar("Morro", mato, new Vector3(0f, 0f, zCentro), raio * 2f, "esfera");
            for (int i = 0; i < 520; i++)
            {
                float x = (float)(r.NextDouble() - 0.5) * 2f * raio.x * 0.82f;
                float z = zCentro - raio.z * (0.1f + (float)r.NextDouble() * 0.85f);
                float dentro = 1f - (x * x) / (raio.x * raio.x) - ((z - zCentro) * (z - zCentro)) / (raio.z * raio.z);
                if (dentro <= 0.04f) continue;
                // Mais casas embaixo e no meio; no alto, mato com poucas casas
                float chao = raio.y * Mathf.Sqrt(dentro);
                if (chao > raio.y * 0.8f && r.NextDouble() < 0.6) continue;
                Vector3 tam = new Vector3(3f + (float)r.NextDouble() * 3f, 2.7f + (float)r.Next(2) * 2.8f, 3f + (float)r.NextDouble() * 2.5f);
                Material m = r.NextDouble() < 0.6 ? tijolo : reboco[r.Next(reboco.Length)];
                Vector3 centro = new Vector3(x, chao + tam.y * 0.5f - 1.4f, z);
                Juntar("CasaMorro", m, centro, tam + new Vector3(0f, 1.4f, 0f));
                // Laje (concreto) por cima e janelas na frente (virada para a rua)
                Juntar("LajeMorro", concreto, centro + Vector3.up * (tam.y * 0.5f + 0.06f), new Vector3(tam.x + 0.2f, 0.12f, tam.z + 0.2f));
                float frente = centro.z - tam.z * 0.5f - 0.03f;
                int andares = tam.y > 4f ? 2 : 1;
                for (int a = 0; a < andares; a++)
                {
                    float y = chao - 1.4f + 1.4f + a * 2.8f + 1.5f;
                    Juntar("JanelaMorro", janela, new Vector3(x - tam.x * 0.22f, y, frente), new Vector3(0.9f, 0.8f, 0.06f));
                    if (r.NextDouble() < 0.6) Juntar("JanelaMorro", janela, new Vector3(x + tam.x * 0.22f, y, frente), new Vector3(0.9f, 0.8f, 0.06f));
                }
                if (r.NextDouble() < 0.45)
                    Juntar("CaixaMorro", caixaDagua, centro + Vector3.up * (tam.y * 0.5f + 0.55f) + new Vector3(tam.x * 0.25f, 0f, 0f), new Vector3(1f, 0.85f, 1f), "cilindro");
            }
            // Árvores e moitas no meio das casas (o verde da encosta)
            for (int i = 0; i < 140; i++)
            {
                float x = (float)(r.NextDouble() - 0.5) * 2f * raio.x * 0.9f;
                float z = zCentro - raio.z * (0.05f + (float)r.NextDouble() * 0.95f);
                float dentro = 1f - (x * x) / (raio.x * raio.x) - ((z - zCentro) * (z - zCentro)) / (raio.z * raio.z);
                if (dentro <= 0.02f) continue;
                float chao = raio.y * Mathf.Sqrt(dentro);
                Juntar("CopaMorro", folhagem, new Vector3(x, chao + 1.2f, z), Vector3.one * (2.5f + (float)r.NextDouble() * 3f), "esfera");
            }
        }

        // ------------------------------------------------------------------ malhas e materiais

        /// <summary>Caixa com UV em metros (a textura não estica: repete no tamanho certo).</summary>
        private static Mesh MalhaCaixa(Vector3 tamanho)
        {
            string chave = $"Caixa_{tamanho.x:0.###}_{tamanho.y:0.###}_{tamanho.z:0.###}";
            if (malhas.TryGetValue(chave, out Mesh m)) return m;
            m = new Mesh { name = chave };
            Vector3 h = tamanho * 0.5f;
            var v = new List<Vector3>();
            var uv = new List<Vector2>();
            var n = new List<Vector3>();
            var tri = new List<int>();
            void Face(Vector3 normal, Vector3 eixoU, Vector3 eixoV, float tu, float tv)
            {
                int b = v.Count;
                Vector3 c = Vector3.Scale(normal, h);
                Vector3 du = eixoU * tu * 0.5f, dv = eixoV * tv * 0.5f;
                v.Add(c - du - dv); v.Add(c + du - dv); v.Add(c + du + dv); v.Add(c - du + dv);
                uv.Add(new Vector2(0, 0)); uv.Add(new Vector2(tu, 0)); uv.Add(new Vector2(tu, tv)); uv.Add(new Vector2(0, tv));
                for (int i = 0; i < 4; i++) n.Add(normal);
                tri.Add(b); tri.Add(b + 2); tri.Add(b + 1); tri.Add(b); tri.Add(b + 3); tri.Add(b + 2);
            }
            Face(Vector3.forward, Vector3.left, Vector3.up, tamanho.x, tamanho.y);
            Face(Vector3.back, Vector3.right, Vector3.up, tamanho.x, tamanho.y);
            Face(Vector3.right, Vector3.forward, Vector3.up, tamanho.z, tamanho.y);
            Face(Vector3.left, Vector3.back, Vector3.up, tamanho.z, tamanho.y);
            Face(Vector3.up, Vector3.right, Vector3.forward, tamanho.x, tamanho.z);
            Face(Vector3.down, Vector3.right, Vector3.back, tamanho.x, tamanho.z);
            m.SetVertices(v);
            m.SetUVs(0, uv);
            m.SetNormals(n);
            m.SetTriangles(tri, 0);
            m.RecalculateTangents();
            m.RecalculateBounds();
            malhas[chave] = m;
            return m;
        }

        private static GameObject Caixa(string nome, Transform pai, Vector3 posicao, Vector3 tamanho, Material material, bool colisao)
        {
            GameObject g = CaixaNoMundo(nome, pai, posicao, tamanho, material);
            if (colisao) g.AddComponent<BoxCollider>();
            return g;
        }

        /// <summary>Objeto avulso com malha de caixa (para coisas que giram, têm colisão ou texto).</summary>
        private static GameObject CaixaNoMundo(string nome, Transform pai, Vector3 posicao, Vector3 tamanho, Material material,
                                               Vector3? local = null, Vector3? tamanhoLocal = null)
        {
            GameObject g = new GameObject(nome);
            if (pai != null) g.transform.SetParent(pai, false);
            else g.transform.SetParent(raizAvulsos, false);
            if (local.HasValue) g.transform.localPosition = local.Value;
            else g.transform.position = posicao;
            Mesh malha = MalhaCaixa(tamanhoLocal ?? tamanho);
            g.AddComponent<MeshFilter>().sharedMesh = SalvarMalha(malha);
            g.AddComponent<MeshRenderer>().sharedMaterial = material;
            return g;
        }

        private static Transform raizAvulsos;

        private static void ColisaoInvisivel(string nome, Transform pai, Vector3 posicao, Vector3 tamanho)
        {
            GameObject g = new GameObject(nome);
            g.transform.SetParent(pai);
            g.transform.position = posicao;
            BoxCollider c = g.AddComponent<BoxCollider>();
            c.size = tamanho;
        }

        /// <summary>Guarda uma peça para juntar com as outras do mesmo material numa malha só (desenha muito mais rápido).</summary>
        private static void Juntar(string nome, Material material, Vector3 centro, Vector3 tamanho, string forma = "caixa")
        {
            Mesh m;
            Matrix4x4 matriz;
            if (forma == "caixa")
            {
                m = MalhaCaixa(tamanho);
                matriz = Matrix4x4.Translate(centro);
            }
            else
            {
                m = Primitiva(forma == "esfera" ? PrimitiveType.Sphere : PrimitiveType.Cylinder);
                Vector3 escala = forma == "cilindro" ? new Vector3(tamanho.x, tamanho.y * 0.5f, tamanho.z) : tamanho;
                matriz = Matrix4x4.TRS(centro, Quaternion.identity, escala);
            }
            string chave = material.name;
            if (!juntar.TryGetValue(chave, out var lista)) juntar[chave] = lista = new List<CombineInstance>();
            materiaisJuntar[chave] = material;
            lista.Add(new CombineInstance { mesh = m, transform = matriz });
        }

        private static Mesh Primitiva(PrimitiveType tipo)
        {
            GameObject g = GameObject.CreatePrimitive(tipo);
            Mesh m = g.GetComponent<MeshFilter>().sharedMesh;
            Object.DestroyImmediate(g);
            return m;
        }

        private static void FinalizarJuntas(Transform pai, string prefixo)
        {
            int k = 0;
            foreach (var par in juntar)
            {
                // Uma malha por material (separando se passar do limite de vértices)
                var lista = par.Value;
                int inicio = 0;
                while (inicio < lista.Count)
                {
                    var parte = new List<CombineInstance>();
                    int vertices = 0;
                    while (inicio < lista.Count && vertices + lista[inicio].mesh.vertexCount < 60000)
                    {
                        vertices += lista[inicio].mesh.vertexCount;
                        parte.Add(lista[inicio++]);
                    }
                    Mesh m = new Mesh { name = $"Junta_{prefixo}{par.Key}_{k++}" };
                    m.CombineMeshes(parte.ToArray(), true, true);
                    m.RecalculateBounds();
                    GameObject g = new GameObject($"Junta_{par.Key}");
                    g.transform.SetParent(pai);
                    g.AddComponent<MeshFilter>().sharedMesh = SalvarMalha(m);
                    g.AddComponent<MeshRenderer>().sharedMaterial = materiaisJuntar[par.Key];
                }
            }
            juntar.Clear();
        }

        // ------------------------------------------------------------------ outros lugares

        private static void MontarMapas(Transform pai, ConfigFisica config, System.Random r)
        {
            float L = config.comprimentoCampo;
            float meia = config.larguraCampo * 0.5f;
            Material grama = Mat("Grama", new Color(0.82f, 0.9f, 0.75f), Foto("leafy_grass", false), Foto("leafy_grass", true), 3f, 0.05f);
            Material gramaFazenda = Mat("GramaFazenda", new Color(0.7f, 0.85f, 0.6f), Foto("leafy_grass", false), Foto("leafy_grass", true), 2.2f, 0.04f);
            Material bloco = Mat("Bloco", new Color(0.66f, 0.67f, 0.68f), Foto("brushed_concrete", false), Foto("brushed_concrete", true), 1.2f, 0.05f);
            Material madeira = Mat("MadeiraCerca", new Color(0.55f, 0.42f, 0.3f), null, null, 1f, 0.1f);
            Material alambrado = Mat("Alambrado", new Color(0.62f, 0.64f, 0.66f), GeradorTexturas.Alambrado(), null, 1f / 5f, 0.4f, true);
            Material ferro = Mat("PosteAlambrado", new Color(0.45f, 0.47f, 0.5f), null, null, 1f, 0.5f);
            Material vacaBranca = Mat("VacaBranca", new Color(0.93f, 0.91f, 0.86f), null, null, 1f, 0.15f);
            Material vacaPreta = Mat("VacaPreta", new Color(0.08f, 0.07f, 0.07f), null, null, 1f, 0.15f);
            Material vacaCaramelo = Mat("VacaCaramelo", new Color(0.55f, 0.33f, 0.18f), null, null, 1f, 0.15f);
            Material focinho = Mat("VacaFocinho", new Color(0.85f, 0.6f, 0.58f), null, null, 1f, 0.2f);
            Material celeiro = Mat("Celeiro", new Color(0.62f, 0.14f, 0.1f), Foto("beige_wall_001", false), Foto("beige_wall_001", true), 3f, 0.08f);

            // ---------------- Terrão da vila: chão de terra batida, muro de bloco pichado, casas atrás ----------------
            Transform terrao = NovoMapa(pai, "Mapa_Terrao");
            Juntar("ChaoTerrao", terra, new Vector3(0f, 0.002f, 0f), new Vector3(L + 60f, 0.004f, 2f * meia + 40f));
            MuroDeBloco(terrao, config, bloco, r);
            CasasAtras(config, r, meia + 6.5f);
            foreach (float x in new[] { -L * 0.5f - 4f, L * 0.5f + 4f, -L * 0.25f, L * 0.3f })
                Arvore(terrao, new Vector3(x, 0f, meia + 3f), r);
            ColisoesDoCampo(terrao, config);
            FinalizarJuntas(terrao, "Terrao_");

            // ---------------- Campinho de grama: alambrado, terra gasta nas bases, casas ao longe ----------------
            Transform campinho = NovoMapa(pai, "Mapa_Campinho");
            Juntar("ChaoGrama", grama, new Vector3(0f, 0.002f, 0f), new Vector3(L + 80f, 0.004f, 2f * meia + 60f));
            foreach (float lado in new[] { -1f, 1f })
                Juntar("TerraGasta", terra, new Vector3(lado * config.distanciaBases * 0.5f, 0.004f, 0f), new Vector3(4.5f, 0.004f, 3.2f));
            Juntar("TerraGastaMeio", terra, new Vector3(0f, 0.004f, 0f), new Vector3(config.distanciaBases * 0.8f, 0.004f, 1.1f));
            Cercar(campinho, config, 1.6f, 3.2f, 3f, (a, b, h) =>
            {
                PainelAlambrado(campinho, a, b, h, alambrado);
                Juntar("PosteAlambrado", ferro, a + Vector3.up * h * 0.5f, new Vector3(0.07f, h, 0.07f), "cilindro");
            });
            CasasAtras(config, r, meia + 14f);
            for (int i = 0; i < 6; i++)
                Arvore(campinho, new Vector3(-L * 0.5f + i * L / 5f + (float)(r.NextDouble() - 0.5) * 4f, 0f, meia + 5f + (float)r.NextDouble() * 3f), r);
            ColisoesDoCampo(campinho, config);
            FinalizarJuntas(campinho, "Campinho_");

            // ---------------- Fazenda: pasto, cerca de madeira, vacas, celeiro e morros verdes ----------------
            Transform fazenda = NovoMapa(pai, "Mapa_Fazenda");
            Juntar("ChaoPasto", gramaFazenda, new Vector3(0f, 0.002f, 0f), new Vector3(400f, 0.004f, 400f));
            Cercar(fazenda, config, 1.1f, 1.3f, 2.5f, (a, b, h) =>
            {
                Juntar("Mourao", madeira, a + Vector3.up * (h * 0.5f + 0.05f), new Vector3(0.14f, h + 0.1f, 0.14f), "cilindro");
                foreach (float f in new[] { 0.35f, 0.65f, 0.92f })
                {
                    Vector3 c = (a + b) * 0.5f + Vector3.up * h * f;
                    Vector3 d = b - a;
                    Juntar("Ripa", madeira, c, new Vector3(Mathf.Abs(d.x) + 0.08f, 0.09f, Mathf.Abs(d.z) + 0.08f) + new Vector3(Mathf.Abs(d.x) < 0.01f ? 0.04f : 0f, 0f, Mathf.Abs(d.z) < 0.01f ? 0.04f : 0f));
                }
            });
            for (int i = 0; i < 9; i++)
            {
                float x = (float)(r.NextDouble() - 0.5) * (L + 30f);
                float z = meia + 5f + (float)r.NextDouble() * 22f;
                Material pelo = r.NextDouble() < 0.4 ? vacaCaramelo : vacaBranca;
                Vaca(new Vector3(x, 0f, z), r.NextDouble() < 0.5 ? 1f : -1f, pelo, vacaPreta, focinho, r, pelo == vacaBranca);
            }
            // Celeiro vermelho ao fundo, com telhado de duas águas
            Vector3 cel = new Vector3(-L * 0.3f, 0f, meia + 34f);
            Juntar("Celeiro", celeiro, cel + Vector3.up * 3f, new Vector3(14f, 6f, 10f));
            Juntar("PortaCeleiro", madeira, cel + new Vector3(0f, 2.1f, -5.02f), new Vector3(4f, 4.2f, 0.06f));
            foreach (float lado in new[] { -1f, 1f })
            {
                GameObject agua = CaixaNoMundo("TelhadoCeleiro", fazenda, cel + new Vector3(0f, 7.4f, lado * 2.6f), new Vector3(14.6f, 0.2f, 6.6f), telha);
                agua.transform.rotation = Quaternion.Euler(lado * 32f, 0f, 0f);
            }
            for (int i = 0; i < 10; i++)
                Arvore(fazenda, new Vector3((float)(r.NextDouble() - 0.5) * 160f, 0f, meia + 18f + (float)r.NextDouble() * 40f), r);
            // Morros verdes no horizonte (no lugar do morro de casinhas)
            for (int i = 0; i < 5; i++)
                Juntar("MorroVerde", gramaFazenda, new Vector3(-160f + i * 80f + (float)(r.NextDouble() - 0.5) * 30f, -4f, meia + 110f + (float)r.NextDouble() * 40f),
                       new Vector3(140f + (float)r.NextDouble() * 60f, 40f + (float)r.NextDouble() * 25f, 80f), "esfera");
            ColisoesDoCampo(fazenda, config);
            FinalizarJuntas(fazenda, "Fazenda_");

            // ---------------- Favela, avenida, rodovia e pontilhão (ConstrutorRuaEditorLugares.cs) ----------------
            Transform favela = MontarFavela(pai, config, r);
            Transform avenida = MontarAvenida(pai, config, r, grama);
            Transform rodovia = MontarRodovia(pai, config, r, madeira);
            Transform pontilhao = MontarPontilhao(pai, config, r);

            terrao.gameObject.SetActive(false);
            campinho.gameObject.SetActive(false);
            fazenda.gameObject.SetActive(false);
            favela.gameObject.SetActive(false);
            avenida.gameObject.SetActive(false);
            rodovia.gameObject.SetActive(false);
            pontilhao.gameObject.SetActive(false);
        }

        private static Transform NovoMapa(Transform pai, string nome)
        {
            GameObject g = new GameObject(nome);
            g.transform.SetParent(pai);
            raizAvulsos = g.transform;
            return g.transform;
        }

        /// <summary>
        /// Percorre a volta do campo em pedaços de 'passo' metros e chama 'peca(a, b, altura)' em cada um.
        /// O lado da câmera usa 'alturaPerto' (baixo, para não tampar o jogo); o resto, 'alturaFundo'.
        /// </summary>
        private static void Cercar(Transform t, ConfigFisica config, float alturaPerto, float alturaFundo, float passo,
                                   System.Action<Vector3, Vector3, float> peca)
        {
            float L = config.comprimentoCampo * 0.5f + 0.15f, meia = config.larguraCampo * 0.5f + 0.15f;
            void Lado(Vector3 de, Vector3 ate, float h)
            {
                int n = Mathf.Max(1, Mathf.RoundToInt(Vector3.Distance(de, ate) / passo));
                for (int i = 0; i < n; i++) peca(Vector3.Lerp(de, ate, i / (float)n), Vector3.Lerp(de, ate, (i + 1) / (float)n), h);
            }
            Lado(new Vector3(-L, 0f, meia), new Vector3(L, 0f, meia), alturaFundo);
            Lado(new Vector3(L, 0f, -meia), new Vector3(L, 0f, meia), alturaFundo);
            Lado(new Vector3(-L, 0f, -meia), new Vector3(-L, 0f, meia), alturaFundo);
            Lado(new Vector3(-L, 0f, -meia), new Vector3(L, 0f, -meia), alturaPerto);
        }

        private static void PainelAlambrado(Transform t, Vector3 a, Vector3 b, float h, Material tela)
        {
            GameObject q = GameObject.CreatePrimitive(PrimitiveType.Quad);
            q.name = "Alambrado";
            Object.DestroyImmediate(q.GetComponent<Collider>());
            q.transform.SetParent(t);
            q.transform.position = (a + b) * 0.5f + Vector3.up * h * 0.5f;
            q.transform.rotation = Quaternion.LookRotation(Vector3.Cross(b - a, Vector3.up).normalized);
            float w = Vector3.Distance(a, b);
            q.transform.localScale = new Vector3(w, h, 1f);
            MeshRenderer mr = q.GetComponent<MeshRenderer>();
            mr.sharedMaterial = tela;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        }

        /// <summary>Muro de bloco em volta do terrão: alto no fundo e nas pontas, baixo do lado da câmera; com pichação.</summary>
        private static void MuroDeBloco(Transform t, ConfigFisica config, Material bloco, System.Random r)
        {
            float L = config.comprimentoCampo, meia = config.larguraCampo * 0.5f;
            // Fundo na altura do peito: a vizinhança assiste encostada no muro
            const float h = 2.4f, e = 0.2f, hFundo = 1.25f;
            Juntar("MuroBlocoFundo", bloco, new Vector3(0f, hFundo * 0.5f, meia + e * 0.5f), new Vector3(L + 2f * e, hFundo, e));
            Juntar("CapaMuroBloco", concreto, new Vector3(0f, hFundo + 0.03f, meia + e * 0.5f), new Vector3(L + 2f * e + 0.04f, 0.06f, e + 0.06f));
            Juntar("MuroBlocoPerto", bloco, new Vector3(0f, config.alturaMuroPerto * 0.5f, -meia - e * 0.5f), new Vector3(L + 2f * e, config.alturaMuroPerto, e));
            foreach (float lado in new[] { -1f, 1f })
                Juntar("MuroBlocoPonta", bloco, new Vector3(lado * (L + e) * 0.5f, h * 0.5f, 0f), new Vector3(e, h, 2f * meia + 2f * e));
            for (int i = 0; i < 5; i++)
                Pichar(t, new Vector3(-L * 0.4f + i * L * 0.2f + (float)(r.NextDouble() - 0.5) * 4f, 0.62f, meia - 0.01f), 1.8f, pichacao[r.Next(pichacao.Length)]);
        }

        /// <summary>Uma fileira de casas de bairro atrás do campo, de frente para ele.</summary>
        private static void CasasAtras(ConfigFisica config, System.Random r, float zFrente)
        {
            float L = config.comprimentoCampo;
            float x0 = -L * 0.5f - 25f;
            while (x0 < L * 0.5f + 25f)
            {
                float w = 6f + r.Next(4);
                float h = r.NextDouble() < 0.5 ? 3f + (float)r.NextDouble() : 5.8f + (float)r.NextDouble() * 2.4f;
                Material m = r.NextDouble() < 0.35 ? tijolo : reboco[r.Next(reboco.Length)];
                CasaVirada(new Vector3(x0 + w * 0.5f, 0f, zFrente), Vector3.back, w - 0.4f, h, 8f, m, r);
                x0 += w;
            }
        }

        /// <summary>Paredes invisíveis na volta do campo (os jogadores não saem; a bola já volta pela física).</summary>
        private static void ColisoesDoCampo(Transform t, ConfigFisica config)
        {
            float L = config.comprimentoCampo, meia = config.larguraCampo * 0.5f, e = config.espessuraMuro;
            ColisaoInvisivel("ColisaoFundo", t, new Vector3(0f, 2f, meia + e * 0.5f), new Vector3(L, 4f, e));
            ColisaoInvisivel("ColisaoPerto", t, new Vector3(0f, 2f, -meia - e * 0.5f), new Vector3(L, 4f, e));
            foreach (float lado in new[] { -1f, 1f })
                ColisaoInvisivel("ColisaoPonta", t, new Vector3(lado * (L + e) * 0.5f, 2f, 0f), new Vector3(e, 4f, 2f * meia));
        }

        /// <summary>Vaca pastando (olhando para +X ou -X): corpo, cabeça, focinho, pernas, rabo e manchas.</summary>
        private static void Vaca(Vector3 p, float dir, Material pelo, Material mancha, Material focinho, System.Random r, bool malhada)
        {
            float s = 0.9f + (float)r.NextDouble() * 0.2f;
            Juntar("VacaCorpo", pelo, p + new Vector3(0f, 1.05f, 0f) * s, new Vector3(1.9f, 0.95f, 0.85f) * s, "esfera");
            bool pastando = r.NextDouble() < 0.6;
            Vector3 cabeca = p + new Vector3(dir * 1.05f, pastando ? 0.55f : 1.3f, 0f) * s;
            Juntar("VacaCabeca", pelo, cabeca, new Vector3(0.55f, 0.48f, 0.42f) * s, "esfera");
            Juntar("VacaPescoco", pelo, p + new Vector3(dir * 0.8f, pastando ? 0.8f : 1.2f, 0f) * s, new Vector3(0.5f, 0.55f, 0.45f) * s, "esfera");
            Juntar("VacaFocinho", focinho, cabeca + new Vector3(dir * 0.24f, -0.07f, 0f) * s, new Vector3(0.24f, 0.22f, 0.3f) * s, "esfera");
            foreach (float z in new[] { -0.17f, 0.17f })
            {
                Juntar("VacaOrelha", pelo, cabeca + new Vector3(-dir * 0.05f, 0.12f, z * 1.6f) * s, new Vector3(0.1f, 0.08f, 0.22f) * s, "esfera");
                foreach (float x in new[] { -0.62f, 0.62f })
                    Juntar("VacaPerna", pelo, p + new Vector3(x, 0.4f, z * 1.4f) * s, new Vector3(0.15f, 0.8f, 0.15f) * s, "cilindro");
            }
            Juntar("VacaRabo", pelo, p + new Vector3(-dir * 0.98f, 0.85f, 0f) * s, new Vector3(0.05f, 0.7f, 0.05f) * s, "cilindro");
            if (!malhada) return;
            for (int i = 0; i < 4; i++)
            {
                Vector3 o = new Vector3((float)(r.NextDouble() - 0.5) * 1.3f, 1.05f + (float)(r.NextDouble() - 0.2) * 0.35f, (r.NextDouble() < 0.5 ? -1f : 1f) * 0.3f);
                Juntar("VacaMancha", mancha, p + o * s, new Vector3(0.5f, 0.45f, 0.3f) * s * (0.7f + (float)r.NextDouble() * 0.5f), "esfera");
            }
        }

        private static Mesh SalvarMalha(Mesh m)
        {
            if (AssetDatabase.Contains(m)) return m;
            if (!AssetDatabase.IsValidFolder(PastaMalhas)) AssetDatabase.CreateFolder("Assets", "Malhas");
            string caminho = $"{PastaMalhas}/{m.name}.asset";
            Mesh existente = AssetDatabase.LoadAssetAtPath<Mesh>(caminho);
            if (existente != null)
            {
                existente.Clear();
                EditorUtility.CopySerialized(m, existente);
                return existente;
            }
            AssetDatabase.CreateAsset(m, caminho);
            return m;
        }

        private static Material Mat(string nome, Color cor, Texture2D textura = null, Texture2D relevo = null, float metrosPorTextura = 1f,
                                    float brilho = 0.12f, bool recorte = false, bool semLuz = false)
        {
            if (!AssetDatabase.IsValidFolder("Assets/Materiais")) AssetDatabase.CreateFolder("Assets", "Materiais");
            if (!AssetDatabase.IsValidFolder(PastaMateriais)) AssetDatabase.CreateFolder("Assets/Materiais", "Rua");
            string caminho = $"{PastaMateriais}/{nome}.mat";
            Shader shader = Shader.Find(semLuz ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit");
            Material m = AssetDatabase.LoadAssetAtPath<Material>(caminho);
            if (m == null)
            {
                m = new Material(shader);
                AssetDatabase.CreateAsset(m, caminho);
            }
            m.shader = shader;
            m.name = nome;
            m.SetColor("_BaseColor", cor);
            m.SetTexture("_BaseMap", textura);
            m.SetTextureScale("_BaseMap", Vector2.one / Mathf.Max(0.01f, metrosPorTextura));
            if (!semLuz)
            {
                m.SetFloat("_Smoothness", brilho);
                if (relevo != null)
                {
                    m.SetTexture("_BumpMap", relevo);
                    m.SetFloat("_BumpScale", 1f);
                    m.EnableKeyword("_NORMALMAP");
                }
            }
            if (recorte)
            {
                m.SetFloat("_AlphaClip", 1f);
                m.SetFloat("_Cutoff", 0.45f);
                m.EnableKeyword("_ALPHATEST_ON");
                m.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
                m.SetFloat("_Cull", 0f);
            }
            m.enableInstancing = true;
            EditorUtility.SetDirty(m);
            return m;
        }

        /// <summary>
        /// Textura de foto (Poly Haven, CC0) em Assets/Texturas/Fotos/{nome}: cor e relevo (normal map).
        /// Se não achar, devolve null e o material fica com a textura desenhada por código.
        /// </summary>
        private static Texture2D Foto(string nome, bool relevo)
        {
            string caminho = $"Assets/Texturas/Fotos/{nome}/{nome}_{(relevo ? "normal" : "cor")}.jpg";
            if (AssetImporter.GetAtPath(caminho) is TextureImporter imp)
            {
                bool mudou = false;
                if (relevo && imp.textureType != TextureImporterType.NormalMap) { imp.textureType = TextureImporterType.NormalMap; mudou = true; }
                if (imp.wrapMode != TextureWrapMode.Repeat) { imp.wrapMode = TextureWrapMode.Repeat; mudou = true; }
                if (imp.anisoLevel < 8) { imp.anisoLevel = 8; mudou = true; }
                if (mudou) imp.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(caminho);
        }

        private static Texture2D FotoOu(string nome, bool relevo, Texture2D seNaoTiver) => Foto(nome, relevo) ?? seNaoTiver;

        private static void CriarMateriais()
        {
            GeradorTexturas.Asfalto(out Texture2D tAsfalto, out Texture2D nAsfalto);
            GeradorTexturas.Calcada(out Texture2D tCalcada, out Texture2D nCalcada);
            GeradorTexturas.Reboco(out Texture2D tReboco, out Texture2D nReboco);
            GeradorTexturas.Tijolo(out Texture2D tTijolo, out Texture2D nTijolo);
            GeradorTexturas.PortaoEnrolar(out Texture2D tPortao, out Texture2D nPortao);
            GeradorTexturas.Telha(out Texture2D tTelha);
            Texture2D tGiz = GeradorTexturas.Giz();
            Texture2D tFolha = GeradorTexturas.Folhagem();
            Texture2D tTerra = GeradorTexturas.Terra();

            // Chão e paredes com foto de verdade (Poly Haven, CC0) quando disponível
            bool fotos = Foto("asphalt_02", false) != null;
            asfalto = fotos ? Mat("Asfalto", new Color(0.9f, 0.9f, 0.92f), Foto("asphalt_02", false), Foto("asphalt_02", true), 4f, 0.2f)
                            : Mat("Asfalto", Color.white, tAsfalto, nAsfalto, 7f, 0.18f);
            calcada = Mat("Calcada", new Color(0.95f, 0.93f, 0.9f), FotoOu("brushed_concrete", false, tCalcada), FotoOu("brushed_concrete", true, nCalcada), 2.4f, 0.1f);
            meioFio = Mat("MeioFio", new Color(0.95f, 0.95f, 0.92f), tCalcada, nCalcada, 1f, 0.1f);
            tijolo = Mat("Tijolo", Color.white, FotoOu("brick_wall_02", false, tTijolo), FotoOu("brick_wall_02", true, nTijolo), 2.5f, 0.08f);
            telha = Mat("Telha", Color.white, tTelha, null, 2f, 0.15f);
            janela = Mat("Janela", new Color(0.09f, 0.11f, 0.14f), null, null, 1f, 0.85f);
            grade = Mat("Grade", new Color(0.12f, 0.12f, 0.13f), null, null, 1f, 0.5f);
            porta = Mat("PortaMadeira", new Color(0.45f, 0.28f, 0.16f), null, null, 1f, 0.3f);
            caixaDagua = Mat("CaixaDagua", new Color(0.12f, 0.34f, 0.78f), null, null, 1f, 0.45f);
            concreto = Mat("Concreto", new Color(0.62f, 0.62f, 0.6f), tCalcada, null, 2f, 0.08f);
            fio = Mat("Fio", new Color(0.05f, 0.05f, 0.06f), null, null, 1f, 0f, false, true);
            folhagem = Mat("Folhagem", Color.white, tFolha, null, 1.5f, 0.05f);
            tronco = Mat("Tronco", new Color(0.36f, 0.26f, 0.18f), null, null, 1f, 0.05f);
            terra = Mat("Terra", Color.white, FotoOu("baseball_playground", false, tTerra), Foto("baseball_playground", true), 4f, 0.03f);
            giz = Mat("GizRua", Color.white, tGiz, null, 0.5f, 0.05f, true);
            faixaAmarela = Mat("FaixaAmarela", new Color(0.95f, 0.78f, 0.15f), tGiz, null, 0.6f, 0.2f, true);
            vidroCarro = Mat("VidroCarro", new Color(0.08f, 0.12f, 0.16f), null, null, 1f, 0.92f);
            pneu = Mat("Pneu", new Color(0.06f, 0.06f, 0.07f), null, null, 1f, 0.2f);
            placaFundo = Mat("PlacaFundo", new Color(0.95f, 0.95f, 0.92f));

            Color[] cores =
            {
                new Color(0.95f, 0.86f, 0.62f), new Color(0.62f, 0.85f, 0.78f), new Color(0.95f, 0.66f, 0.62f), new Color(0.7f, 0.78f, 0.95f),
                new Color(0.98f, 0.95f, 0.88f), new Color(0.9f, 0.7f, 0.45f), new Color(0.98f, 0.82f, 0.25f), new Color(0.55f, 0.8f, 0.45f),
                new Color(0.95f, 0.55f, 0.75f), new Color(0.85f, 0.85f, 0.82f)
            };
            reboco = new Material[cores.Length];
            // Reboco: a foto é bege; clareia a cor para a tinta da casa não ficar escura
            Texture2D fReboco = Foto("beige_wall_001", false), nfReboco = Foto("beige_wall_001", true);
            for (int i = 0; i < cores.Length; i++)
                reboco[i] = fReboco != null ? Mat($"Reboco{i + 1}", Color.Lerp(cores[i], Color.white, 0.15f) * 1.12f, fReboco, nfReboco, 3f, 0.08f)
                                            : Mat($"Reboco{i + 1}", cores[i], tReboco, nReboco, 3f, 0.08f);
            Color[] coresPortao = { new Color(0.75f, 0.75f, 0.76f), new Color(0.3f, 0.55f, 0.35f), new Color(0.25f, 0.4f, 0.7f), new Color(0.6f, 0.2f, 0.18f) };
            portao = new Material[coresPortao.Length];
            for (int i = 0; i < coresPortao.Length; i++) portao[i] = Mat($"Portao{i + 1}", coresPortao[i], tPortao, nPortao, 1f, 0.35f);
            pichacao = new Material[5];
            for (int i = 0; i < pichacao.Length; i++) pichacao[i] = Mat($"Pichacao{i + 1}", Color.white, GeradorTexturas.Pichacao(i), null, 1f, 0.2f, true);
            Color[] coresCarro = { new Color(0.85f, 0.85f, 0.83f), new Color(0.7f, 0.1f, 0.1f), new Color(0.15f, 0.2f, 0.35f), new Color(0.2f, 0.2f, 0.2f), new Color(0.9f, 0.75f, 0.2f) };
            carro = new Material[coresCarro.Length];
            for (int i = 0; i < coresCarro.Length; i++) carro[i] = Mat($"Carro{i + 1}", coresCarro[i], null, null, 1f, 0.7f);
            Color[] coresPlaca = { new Color(0.95f, 0.2f, 0.2f), new Color(0.15f, 0.45f, 0.85f), new Color(0.98f, 0.82f, 0.15f), new Color(0.2f, 0.6f, 0.3f), new Color(0.98f, 0.98f, 0.95f) };
            placaCores = new Material[coresPlaca.Length];
            for (int i = 0; i < coresPlaca.Length; i++) placaCores[i] = Mat($"Placa{i + 1}", coresPlaca[i], null, null, 1f, 0.3f);

            // As marcas de giz das bases (material "Giz" da cena) ganham a textura de giz falhado
            Material gizBases = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materiais/Giz.mat");
            if (gizBases != null)
            {
                gizBases.SetTexture("_BaseMap", tGiz);
                gizBases.SetFloat("_AlphaClip", 1f);
                gizBases.SetFloat("_Cutoff", 0.4f);
                gizBases.EnableKeyword("_ALPHATEST_ON");
                gizBases.renderQueue = (int)UnityEngine.Rendering.RenderQueue.AlphaTest;
                EditorUtility.SetDirty(gizBases);
            }
            AssetDatabase.SaveAssets();
        }
    }
}
