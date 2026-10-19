using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BetsDeRua.Editor
{
    /// <summary>
    /// Monta a cena "Prototipo" do zero a partir dos números do ConfigFisica.
    /// Menu: Bets > Montar cena Prototipo. Rodar de novo recria a cena
    /// (útil depois de mudar medidas do campo no ConfigFisica).
    /// </summary>
    public static class MontadorCena
    {
        private const string CaminhoCena = "Assets/Scenes/Prototipo.unity";
        private const string CaminhoConfig = "Assets/Config/ConfigFisica.asset";
        private const string CaminhoConfigCamera = "Assets/Config/ConfigCamera.asset";
        private const string CaminhoConfigPartida = "Assets/Config/ConfigPartida.asset";
        private const string CaminhoConfigHUD = "Assets/Config/ConfigHUD.asset";
        private const string PastaMateriais = "Assets/Materiais";

        // Detalhes só de visual (não afetam o jogo): espessura do chão e
        // pequenas alturas para as marcas de giz não "piscarem" com o chão.
        private const float EspessuraChao = 0.2f;
        private const float AlturaFaixa = 0.002f;
        private const float AlturaGiz = 0.004f;
        private const float AlturaBase = 0.006f;
        private const float EspessuraMarca = 0.002f;

        [MenuItem("Bets/Montar cena Prototipo")]
        public static void Montar()
        {
            // A cena nova vem primeiro: criar a cena descarrega objetos da memória,
            // então config e materiais são carregados depois dela.
            Scene cena = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            ConfigFisica config = CarregarOuCriarConfig<ConfigFisica>(CaminhoConfig);
            ConfigCamera configCamera = CarregarOuCriarConfig<ConfigCamera>(CaminhoConfigCamera);
            ConfigPartida configPartida = CarregarOuCriarConfig<ConfigPartida>(CaminhoConfigPartida);
            ConfigHUD configHUD = CarregarOuCriarConfig<ConfigHUD>(CaminhoConfigHUD);

            Material matChao = CriarMaterial("Chao", new Color(0.22f, 0.22f, 0.23f));
            Material matMuro = CriarMaterial("Muro", new Color(0.5f, 0.5f, 0.5f));
            Material matFaixa = CriarMaterial("FaixaMeio", new Color(0.3f, 0.3f, 0.31f));
            Material matGiz = CriarMaterial("Giz", new Color(0.95f, 0.95f, 0.95f));
            // Garrafa escura, verde de PET, para contrastar com o giz e o asfalto
            Material matGarrafa = CriarMaterial("Garrafa", new Color(0.12f, 0.42f, 0.22f));
            Material matTampa = CriarMaterial("TampaGarrafa", new Color(0.95f, 0.95f, 0.95f));
            Material matBola = CriarMaterial("Bola", new Color(0.85f, 0.95f, 0.2f));
            Material matDuplaTaco = CriarMaterial("DuplaTaco", configPartida.corDuplaTaco);
            Material matDuplaBola = CriarMaterial("DuplaBola", configPartida.corDuplaBola);
            Material matTaco = CriarMaterial("Taco", new Color(0.95f, 0.85f, 0.6f));
            Material matRosto = CriarMaterial("Rosto", new Color(0.12f, 0.12f, 0.12f));
            Material matMarcadorTaco = CriarMaterial("MarcadorDuplaTaco", configPartida.corDuplaTaco, true);
            Material matMarcadorBola = CriarMaterial("MarcadorDuplaBola", configPartida.corDuplaBola, true);
            Material matMarcadorGiz = CriarMaterial("MarcadorGiz", new Color(0.95f, 0.95f, 0.95f), true);
            Material matMira = CriarMaterial("MiraAmarela", new Color(1f, 0.85f, 0.1f), true);
            Material matArcoFundo = CriarMaterial("ArcoFundo", new Color(0.12f, 0.12f, 0.12f), true);
            Material matArcoFaixa = CriarMaterial("ArcoFaixaVerde", new Color(0.2f, 0.9f, 0.3f), true);
            Material matArcoPreenchimento = CriarMaterial("ArcoPreenchimento", Color.white, true);
            Material matSombra = CriarMaterial("SombraBola", new Color(0.06f, 0.06f, 0.06f), true);

            float comprimento = config.comprimentoCampo;
            float largura = config.larguraCampo;
            float meioBases = config.distanciaBases * 0.5f;

            // ---------- Campo ----------
            GameObject campo = new GameObject("Campo");

            Cubo("Chao", campo.transform, new Vector3(0f, -EspessuraChao * 0.5f, 0f),
                 new Vector3(comprimento, EspessuraChao, largura), matChao, true);

            // Muros baixos por fora do retângulo jogável
            GameObject muros = new GameObject("Muros");
            muros.transform.SetParent(campo.transform);
            float e = config.espessuraMuro;
            float h = config.alturaMuro;
            Cubo("MuroFundoA", muros.transform, new Vector3(-(comprimento + e) * 0.5f, h * 0.5f, 0f),
                 new Vector3(e, h, largura + 2f * e), matMuro, true);
            Cubo("MuroFundoB", muros.transform, new Vector3((comprimento + e) * 0.5f, h * 0.5f, 0f),
                 new Vector3(e, h, largura + 2f * e), matMuro, true);
            Cubo("MuroFrente", muros.transform, new Vector3(0f, h * 0.5f, -(largura + e) * 0.5f),
                 new Vector3(comprimento, h, e), matMuro, true);
            Cubo("MuroTras", muros.transform, new Vector3(0f, h * 0.5f, (largura + e) * 0.5f),
                 new Vector3(comprimento, h, e), matMuro, true);

            // Faixa do meio (onde a cruzada vale), com linhas de giz nas bordas
            GameObject faixa = new GameObject("FaixaMeio");
            faixa.transform.SetParent(campo.transform);
            float larguraFaixa = config.larguraFaixaMeio;
            Cubo("Area", faixa.transform, new Vector3(0f, AlturaFaixa, 0f),
                 new Vector3(larguraFaixa, EspessuraMarca, largura), matFaixa, false);
            Cubo("LinhaA", faixa.transform, new Vector3(-larguraFaixa * 0.5f, AlturaGiz, 0f),
                 new Vector3(config.espessuraLinhaGiz, EspessuraMarca, largura), matGiz, false);
            Cubo("LinhaB", faixa.transform, new Vector3(larguraFaixa * 0.5f, AlturaGiz, 0f),
                 new Vector3(config.espessuraLinhaGiz, EspessuraMarca, largura), matGiz, false);

            // Bases riscadas de giz: casinha da garrafa + área do taco na frente (a zona segura em volta é invisível)
            GameObject bases = new GameObject("Bases");
            bases.transform.SetParent(campo.transform);
            BaseDeGiz("BaseA", bases.transform, new Vector3(-meioBases, 0f, 0f), config, matGiz);
            BaseDeGiz("BaseB", bases.transform, new Vector3(meioBases, 0f, 0f), config, matGiz);

            // ---------- Garrafas ----------
            GameObject garrafas = new GameObject("Garrafas");
            Garrafa garrafaA = CriarGarrafa("GarrafaA", garrafas.transform, new Vector3(-meioBases, 0f, 0f), config, matGarrafa, matTampa);
            Garrafa garrafaB = CriarGarrafa("GarrafaB", garrafas.transform, new Vector3(meioBases, 0f, 0f), config, matGarrafa, matTampa);
            Garrafa[] todas = { garrafaA, garrafaB };

            // ---------- Bola ----------
            GameObject objBola = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            objBola.name = "Bola";
            Object.DestroyImmediate(objBola.GetComponent<Collider>()); // física própria, sem collider
            objBola.GetComponent<MeshRenderer>().sharedMaterial = matBola;
            float raioVisual = config.raioBola * config.escalaVisualBola;
            // Provisório: no Play, o ControladorLance põe a bola na mão do arremessador
            objBola.transform.position = new Vector3(-meioBases, raioVisual, 0f);
            Bola bola = objBola.AddComponent<Bola>();
            Ligar(bola, "config", config);
            LigarLista(bola, "garrafas", todas);

            // ---------- Jogadores ----------
            // Dupla do taco: um rebatedor em cada base, ao lado da garrafa (lado do fundo,
            // para não esconder a garrafa da câmera). Dupla da bola: arremessador atrás da
            // garrafa A, guardião atrás da garrafa B.
            GameObject raizJogadores = new GameObject("Jogadores");
            Material matZonaSegura = CriarMaterial("SinalZonaSegura", configHUD.corZonaSegura, true);
            Material matZonaPerigo = CriarMaterial("SinalZonaPerigo", configHUD.corZonaPerigo, true);
            var montagem = new MontagemJogador(config, configHUD, matRosto, matTaco, matMarcadorGiz, matZonaSegura, matZonaPerigo);
            float lado = configPartida.afastamentoRebatedor;
            Jogador rebatedorB = montagem.Criar("RebatedorB", "Rebatedor B", Dupla.Taco, Papel.Rebatedor,
                new Vector3(meioBases, 0f, lado), Vector3.left, matDuplaTaco, matMarcadorTaco, raizJogadores.transform);
            Jogador rebatedorA = montagem.Criar("RebatedorA", "Rebatedor A", Dupla.Taco, Papel.Rebatedor,
                new Vector3(-meioBases, 0f, lado), Vector3.right, matDuplaTaco, matMarcadorTaco, raizJogadores.transform);
            Jogador arremessador = montagem.Criar("Arremessador", "Arremessador", Dupla.Bola, Papel.Arremessador,
                new Vector3(-meioBases - configPartida.afastamentoArremessador, 0f, 0f), Vector3.right,
                matDuplaBola, matMarcadorBola, raizJogadores.transform);
            Jogador guardiao = montagem.Criar("Guardiao", "Guardião", Dupla.Bola, Papel.Guardiao,
                new Vector3(meioBases + configPartida.distanciaGuardiao, 0f, 0f), Vector3.left,
                matDuplaBola, matMarcadorBola, raizJogadores.transform);
            // Ordem importa: o primeiro de cada dupla é quem a pessoa pega ao entrar
            Jogador[] todosJogadores = { rebatedorB, rebatedorA, arremessador, guardiao };

            GerenciadorJogadores gerenciador = raizJogadores.AddComponent<GerenciadorJogadores>();
            Ligar(gerenciador, "configPartida", configPartida);
            LigarLista(gerenciador, "jogadores", todosJogadores);
            // Lancha: a bola do arremesso pode bater no corpo dos rebatedores
            LigarLista(bola, "corpos", new Object[] { rebatedorA, rebatedorB });

            // ---------- Câmera de transmissão ----------
            Camera cameraPrincipal = Camera.main;
            cameraPrincipal.name = "CameraTransmissao";
            CameraTransmissao transmissao = cameraPrincipal.gameObject.AddComponent<CameraTransmissao>();
            Ligar(transmissao, "configCamera", configCamera);
            Ligar(transmissao, "configFisica", config);
            Ligar(transmissao, "alvo", objBola.transform);
            LigarLista(transmissao, "outrosAlvos", System.Array.ConvertAll(todosJogadores, j => (Object)j.transform));
            LigarLista(transmissao, "prioritarios", new Object[] { rebatedorA.transform, rebatedorB.transform });

            // ---------- Arremesso: mira no chão e arco de força ----------
            GameObject objMira = new GameObject("MiraNoChao");
            MiraNoChao mira = objMira.AddComponent<MiraNoChao>();
            Ligar(mira, "configHUD", configHUD);
            Ligar(mira, "materialMira", matMira);

            // Prévia da trajetória inteira (protótipo: liga e desliga no ConfigHUD)
            GameObject objPrevia = new GameObject("PreviaTrajetoria");
            PreviaTrajetoria previa = objPrevia.AddComponent<PreviaTrajetoria>();
            Ligar(previa, "configFisica", config);
            Ligar(previa, "configHUD", configHUD);
            Ligar(previa, "material", matMira);

            GameObject objArco = new GameObject("ArcoForca");
            ArcoForca arco = objArco.AddComponent<ArcoForca>();
            Ligar(arco, "configHUD", configHUD);
            Ligar(arco, "configFisica", config);
            Ligar(arco, "materialFundo", matArcoFundo);
            Ligar(arco, "materialFaixa", matArcoFaixa);
            Ligar(arco, "materialPreenchimento", matArcoPreenchimento);

            // Os dois defensores podem ficar com a bola (arremessar, chutar, passar) e pegar a bola
            Arremessador arremessadorComp = null;
            var listaDefensores = new System.Collections.Generic.List<Defensor>();
            foreach (Jogador defensorJogador in new[] { arremessador, guardiao })
            {
                Arremessador comBola = defensorJogador.gameObject.AddComponent<Arremessador>();
                Ligar(comBola, "config", config);
                Ligar(comBola, "configPartida", configPartida);
                Ligar(comBola, "mira", mira);
                Ligar(comBola, "arco", arco);
                Ligar(comBola, "previa", previa);
                if (defensorJogador == arremessador) arremessadorComp = comBola;

                Defensor defensor = defensorJogador.gameObject.AddComponent<Defensor>();
                Ligar(defensor, "config", config);
                Ligar(defensor, "configHUD", configHUD);
                Ligar(defensor, "bola", bola);
                Ligar(defensor, "materialAlcance", matMarcadorGiz);
                Ligar(defensor, "materialAoAlcance", matMira);
                listaDefensores.Add(defensor);
            }

            // ---------- Batida: ajudas, sombra da bola, arco do rebatedor ----------
            GameObject objAjudas = new GameObject("AjudasBatida");
            AjudasBatida ajudas = objAjudas.AddComponent<AjudasBatida>();
            Ligar(ajudas, "configHUD", configHUD);
            Ligar(ajudas, "configFisica", config);
            Ligar(ajudas, "materialGiz", matMarcadorGiz);
            Ligar(ajudas, "materialAnel", matMira);

            GameObject objSombra = new GameObject("SombraBola");
            objSombra.AddComponent<MeshFilter>();
            MeshRenderer desenhoSombra = objSombra.AddComponent<MeshRenderer>();
            desenhoSombra.sharedMaterial = matSombra;
            desenhoSombra.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            SombraBola sombra = objSombra.AddComponent<SombraBola>();
            Ligar(sombra, "configFisica", config);
            Ligar(sombra, "configHUD", configHUD);
            Ligar(sombra, "bola", bola);

            GameObject objArcoBatida = new GameObject("ArcoForcaRebatedor");
            ArcoForca arcoBatida = objArcoBatida.AddComponent<ArcoForca>();
            Ligar(arcoBatida, "configHUD", configHUD);
            Ligar(arcoBatida, "configFisica", config);
            Ligar(arcoBatida, "materialFundo", matArcoFundo);
            Ligar(arcoBatida, "materialFaixa", matArcoFaixa);
            Ligar(arcoBatida, "materialPreenchimento", matArcoPreenchimento);

            var listaRebatedores = new System.Collections.Generic.List<Rebatedor>();
            foreach (Jogador rebatedorJogador in new[] { rebatedorA, rebatedorB })
            {
                Rebatedor comp = rebatedorJogador.gameObject.AddComponent<Rebatedor>();
                Ligar(comp, "config", config);
                Ligar(comp, "configPartida", configPartida);
                Ligar(comp, "taco", rebatedorJogador.GetComponentInChildren<Taco>());
                Ligar(comp, "bola", bola);
                Ligar(comp, "ajudas", ajudas);
                Ligar(comp, "arco", arcoBatida);
                listaRebatedores.Add(comp);
            }

            // ---------- Lance (máquina de estados) ----------
            GameObject objLance = new GameObject("ControladorLance");
            ControladorLance lance = objLance.AddComponent<ControladorLance>();
            Ligar(lance, "configFisica", config);
            Ligar(lance, "configPartida", configPartida);
            Ligar(lance, "configHUD", configHUD);
            Ligar(lance, "bola", bola);
            Ligar(lance, "arremessador", arremessadorComp);
            LigarLista(lance, "defensores", listaDefensores.ToArray());
            Ligar(lance, "garrafaA", garrafaA);
            Ligar(lance, "garrafaB", garrafaB);
            LigarLista(lance, "rebatedores", listaRebatedores.ToArray());
            Ligar(lance, "gerenciador", gerenciador);

            // ---------- Corrida (cruzada e pontos) ----------
            Corrida corrida = objLance.AddComponent<Corrida>();
            Ligar(corrida, "configPartida", configPartida);
            Ligar(corrida, "configFisica", config);
            Ligar(corrida, "configHUD", configHUD);
            Ligar(corrida, "rebatedorA", rebatedorA);
            Ligar(corrida, "rebatedorB", rebatedorB);
            Ligar(corrida, "materialAnel", matMira);
            Ligar(lance, "corrida", corrida);

            // ---------- Bots de campo ----------
            CerebroBots bots = objLance.AddComponent<CerebroBots>();
            Ligar(bots, "configPartida", configPartida);
            Ligar(bots, "configFisica", config);
            Ligar(bots, "lance", lance);
            Ligar(bots, "corrida", corrida);
            Ligar(bots, "bola", bola);
            LigarLista(bots, "rebatedores", new Object[] { rebatedorA, rebatedorB });
            LigarLista(bots, "defensores", listaDefensores.ToArray());

            // Salva e põe a cena como a primeira do build
            EditorSceneManager.SaveScene(cena, CaminhoCena);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(CaminhoCena, true) };
            AssetDatabase.SaveAssets();
            Debug.Log("[Bets] Cena Prototipo montada em " + CaminhoCena);
        }

        private static T CarregarOuCriarConfig<T>(string caminho) where T : ScriptableObject
        {
            T config = AssetDatabase.LoadAssetAtPath<T>(caminho);
            if (config != null) return config;
            CriarPasta("Assets", "Config");
            config = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(config, caminho);
            return config;
        }

        /// <summary>
        /// Cria (ou atualiza) um material. semLuz = cor chapada, sem sombra,
        /// visível dos dois lados (bom para marcadores).
        /// </summary>
        private static Material CriarMaterial(string nome, Color cor, bool semLuz = false)
        {
            CriarPasta("Assets", "Materiais");
            string caminho = $"{PastaMateriais}/{nome}.mat";
            Shader shader = Shader.Find(semLuz ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit");
            Material material = AssetDatabase.LoadAssetAtPath<Material>(caminho);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, caminho);
            }
            material.shader = shader;
            material.SetColor("_BaseColor", cor);
            if (semLuz) material.SetFloat("_Cull", 0f); // desenha os dois lados
            else material.SetFloat("_Smoothness", 0.1f);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void CriarPasta(string pai, string nome)
        {
            if (!AssetDatabase.IsValidFolder($"{pai}/{nome}")) AssetDatabase.CreateFolder(pai, nome);
        }

        private static GameObject Cubo(string nome, Transform pai, Vector3 posicao, Vector3 tamanho,
                                       Material material, bool comColisao)
        {
            GameObject cubo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cubo.name = nome;
            cubo.transform.SetParent(pai);
            cubo.transform.localPosition = posicao;
            cubo.transform.localScale = tamanho;
            cubo.GetComponent<MeshRenderer>().sharedMaterial = material;
            if (!comColisao) Object.DestroyImmediate(cubo.GetComponent<Collider>());
            return cubo;
        }

        /// <summary>
        /// Base riscada no asfalto, vista de cima: a casinha (quadradinho com a garrafa no meio)
        /// e, encostado na frente dela, o retângulo da área do taco atravessando a rua.
        /// Só contorno, para não cobrir a garrafa nem o jogador.
        /// </summary>
        private static void BaseDeGiz(string nome, Transform pai, Vector3 posicaoGarrafa, ConfigFisica config,
                                      Material material)
        {
            GameObject baseDeGiz = new GameObject(nome);
            baseDeGiz.transform.SetParent(pai);
            baseDeGiz.transform.localPosition = posicaoGarrafa + Vector3.up * AlturaBase;

            // Contas em "para o arremesso" (eixo X local virado para o meio do campo)
            float sentido = ZonaDoTaco.ParaOArremesso(posicaoGarrafa);
            float e = config.espessuraLinhaGiz;
            float lado = config.ladoCasinhaGarrafa;
            float largura = config.larguraAreaTaco;
            float profundidade = config.profundidadeAreaTaco;
            float frenteCasinha = lado * 0.5f;
            float frenteArea = frenteCasinha + profundidade;

            // Casinha: fundo e lados (a frente é a linha de trás da área do taco)
            LinhaDeGiz("CasinhaFundo", baseDeGiz.transform, new Vector3(-frenteCasinha * sentido, 0f, 0f), e, lado + e, material);
            LinhaDeGiz("CasinhaLadoA", baseDeGiz.transform, new Vector3(0f, 0f, -lado * 0.5f), lado, e, material);
            LinhaDeGiz("CasinhaLadoB", baseDeGiz.transform, new Vector3(0f, 0f, lado * 0.5f), lado, e, material);

            // Área do taco: retângulo inteiro
            float meioArea = (frenteCasinha + frenteArea) * 0.5f * sentido;
            LinhaDeGiz("AreaTras", baseDeGiz.transform, new Vector3(frenteCasinha * sentido, 0f, 0f), e, largura + e, material);
            LinhaDeGiz("AreaFrente", baseDeGiz.transform, new Vector3(frenteArea * sentido, 0f, 0f), e, largura + e, material);
            LinhaDeGiz("AreaLadoA", baseDeGiz.transform, new Vector3(meioArea, 0f, -largura * 0.5f), profundidade, e, material);
            LinhaDeGiz("AreaLadoB", baseDeGiz.transform, new Vector3(meioArea, 0f, largura * 0.5f), profundidade, e, material);
        }

        /// <summary>Uma linha de giz no chão: 'tamanhoX' × 'tamanhoZ' (m), sem colisão.</summary>
        private static void LinhaDeGiz(string nome, Transform pai, Vector3 posicao, float tamanhoX, float tamanhoZ,
                                       Material material)
        {
            Cubo(nome, pai, posicao, new Vector3(tamanhoX, EspessuraMarca, tamanhoZ), material, false);
        }

        private static Garrafa CriarGarrafa(string nome, Transform pai, Vector3 posicaoBase,
                                            ConfigFisica config, Material material, Material materialTampa)
        {
            // Objeto pai com pivô na base (para balançar a partir do chão)
            GameObject raiz = new GameObject(nome);
            raiz.transform.SetParent(pai);
            raiz.transform.position = posicaoBase;

            // Corpo visível: o cilindro do Unity tem 2 m de altura e 1 m de diâmetro na escala 1
            GameObject corpoVisual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            corpoVisual.name = "Corpo";
            corpoVisual.transform.SetParent(raiz.transform);
            corpoVisual.transform.localPosition = new Vector3(0f, config.alturaGarrafa * 0.5f, 0f);
            corpoVisual.transform.localScale = new Vector3(config.diametroGarrafa, config.alturaGarrafa * 0.5f, config.diametroGarrafa);
            corpoVisual.GetComponent<MeshRenderer>().sharedMaterial = material;

            // Tampa clara em cima, para ler como garrafa (só visual, sem colisão).
            // Vem depois do corpo: a cor de "exposta" pinta o primeiro desenho (o corpo).
            GameObject tampa = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            tampa.name = "Tampa";
            tampa.transform.SetParent(raiz.transform);
            float alturaTampa = config.alturaGarrafa * 0.08f;
            tampa.transform.localPosition = new Vector3(0f, config.alturaGarrafa + alturaTampa * 0.5f, 0f);
            tampa.transform.localScale = new Vector3(config.diametroGarrafa * 0.4f, alturaTampa * 0.5f, config.diametroGarrafa * 0.4f);
            tampa.GetComponent<MeshRenderer>().sharedMaterial = materialTampa;
            Object.DestroyImmediate(tampa.GetComponent<Collider>());

            Rigidbody corpo = raiz.AddComponent<Rigidbody>();
            corpo.mass = config.massaGarrafa;
            corpo.isKinematic = true;
            corpo.interpolation = RigidbodyInterpolation.Interpolate;
            corpo.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

            Garrafa garrafa = raiz.AddComponent<Garrafa>();
            Ligar(garrafa, "config", config);
            return garrafa;
        }

        /// <summary>Preenche um campo [SerializeField] privado de um componente.</summary>
        private static void Ligar(Object componente, string campo, Object valor)
        {
            SerializedObject objeto = new SerializedObject(componente);
            objeto.FindProperty(campo).objectReferenceValue = valor;
            objeto.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void LigarLista(Object componente, string campo, Object[] valores)
        {
            SerializedObject objeto = new SerializedObject(componente);
            SerializedProperty lista = objeto.FindProperty(campo);
            lista.arraySize = valores.Length;
            for (int i = 0; i < valores.Length; i++)
                lista.GetArrayElementAtIndex(i).objectReferenceValue = valores[i];
            objeto.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>Monta as cápsulas dos jogadores, com taco e marcador.</summary>
        private class MontagemJogador
        {
            private readonly ConfigFisica config;
            private readonly ConfigHUD configHUD;
            private readonly Material matRosto;
            private readonly Material matTaco;
            private readonly Material matGiz;
            private readonly Material matZonaSegura;
            private readonly Material matZonaPerigo;

            public MontagemJogador(ConfigFisica config, ConfigHUD configHUD,
                                   Material matRosto, Material matTaco, Material matGiz,
                                   Material matZonaSegura, Material matZonaPerigo)
            {
                this.config = config;
                this.configHUD = configHUD;
                this.matRosto = matRosto;
                this.matTaco = matTaco;
                this.matGiz = matGiz;
                this.matZonaSegura = matZonaSegura;
                this.matZonaPerigo = matZonaPerigo;
            }

            public Jogador Criar(string nomeObjeto, string nome, Dupla dupla, Papel papel, Vector3 posicao,
                                 Vector3 olhandoPara, Material matCorpo, Material matMarcador, Transform pai)
            {
                float altura = config.alturaJogador;
                float raio = config.raioJogador;

                // Raiz com o pivô nos pés
                GameObject raiz = new GameObject(nomeObjeto);
                raiz.transform.SetParent(pai);
                raiz.transform.SetPositionAndRotation(posicao, Quaternion.LookRotation(olhandoPara, Vector3.up));

                CharacterController controlador = raiz.AddComponent<CharacterController>();
                controlador.height = altura;
                controlador.radius = raio;
                controlador.center = new Vector3(0f, altura * 0.5f, 0f);
                controlador.stepOffset = 0.2f;
                controlador.skinWidth = 0.02f;
                controlador.minMoveDistance = 0f; // senão, com FPS alto, passos pequenos são ignorados

                // Corpo: a cápsula do Unity tem 2 m de altura e 1 m de diâmetro na escala 1
                GameObject corpo = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                corpo.name = "Corpo";
                Object.DestroyImmediate(corpo.GetComponent<Collider>());
                corpo.transform.SetParent(raiz.transform, false);
                corpo.transform.localPosition = new Vector3(0f, altura * 0.5f, 0f);
                corpo.transform.localScale = new Vector3(raio * 2f, altura * 0.5f, raio * 2f);
                corpo.GetComponent<MeshRenderer>().sharedMaterial = matCorpo;

                // "Rosto": faixa escura na frente, para ver para onde o jogador olha
                GameObject rosto = GameObject.CreatePrimitive(PrimitiveType.Cube);
                rosto.name = "Rosto";
                Object.DestroyImmediate(rosto.GetComponent<Collider>());
                rosto.transform.SetParent(raiz.transform, false);
                rosto.transform.localPosition = new Vector3(0f, altura * 0.85f, raio * 0.85f);
                rosto.transform.localScale = new Vector3(raio * 1.2f, altura * 0.08f, raio * 0.5f);
                rosto.GetComponent<MeshRenderer>().sharedMaterial = matRosto;

                Jogador jogador = raiz.AddComponent<Jogador>();
                SerializedObject dados = new SerializedObject(jogador);
                dados.FindProperty("config").objectReferenceValue = config;
                dados.FindProperty("dupla").enumValueIndex = (int)dupla;
                dados.FindProperty("papel").enumValueIndex = (int)papel;
                dados.FindProperty("nome").stringValue = nome;
                dados.ApplyModifiedPropertiesWithoutUndo();

                if (papel == Papel.Rebatedor)
                {
                    CriarTaco(raiz.transform, jogador);
                    // Sinal em cima da cabeça: verde = seguro, vermelho = zona de perigo
                    SinalZona sinal = raiz.AddComponent<SinalZona>();
                    Ligar(sinal, "configHUD", configHUD);
                    Ligar(sinal, "configFisica", config);
                    Ligar(sinal, "jogador", jogador);
                    Ligar(sinal, "taco", raiz.GetComponentInChildren<Taco>());
                    Ligar(sinal, "materialSeguro", matZonaSegura);
                    Ligar(sinal, "materialPerigo", matZonaPerigo);
                }

                MarcadorJogador marcador = raiz.AddComponent<MarcadorJogador>();
                Ligar(marcador, "configHUD", configHUD);
                Ligar(marcador, "configFisica", config);
                Ligar(marcador, "jogador", jogador);
                Ligar(marcador, "materialTriangulo", matMarcador);
                Ligar(marcador, "materialGiz", matGiz);
                return jogador;
            }

            private void CriarTaco(Transform dono, Jogador jogador)
            {
                // Pivô na mão, do lado direito e um pouco à frente do corpo
                GameObject mao = new GameObject("MaoTaco");
                mao.transform.SetParent(dono, false);
                mao.transform.localPosition = new Vector3(config.raioJogador * 0.9f, config.alturaMaoTaco,
                                                          config.raioJogador * 0.6f);

                GameObject taco = GameObject.CreatePrimitive(PrimitiveType.Cube);
                taco.name = "Taco";
                Object.DestroyImmediate(taco.GetComponent<Collider>());
                taco.transform.SetParent(mao.transform, false);
                taco.transform.localPosition = new Vector3(0f, -config.comprimentoTaco * 0.5f, 0f);
                taco.transform.localScale = new Vector3(config.espessuraTaco, config.comprimentoTaco, config.espessuraTaco);
                taco.GetComponent<MeshRenderer>().sharedMaterial = matTaco;

                Taco componente = mao.AddComponent<Taco>();
                Ligar(componente, "config", config);
                Ligar(componente, "dono", jogador);
            }
        }
    }
}
