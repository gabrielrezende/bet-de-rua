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

            Material matChao = CriarMaterial("Chao", new Color(0.22f, 0.22f, 0.23f));
            Material matMuro = CriarMaterial("Muro", new Color(0.5f, 0.5f, 0.5f));
            Material matFaixa = CriarMaterial("FaixaMeio", new Color(0.3f, 0.3f, 0.31f));
            Material matGiz = CriarMaterial("Giz", new Color(0.95f, 0.95f, 0.95f));
            Material matGarrafa = CriarMaterial("Garrafa", new Color(0.8f, 0.82f, 0.85f));
            Material matBola = CriarMaterial("Bola", new Color(0.85f, 0.95f, 0.2f));

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

            // Bases: círculos de giz
            GameObject bases = new GameObject("Bases");
            bases.transform.SetParent(campo.transform);
            Circulo("BaseA", bases.transform, new Vector3(-meioBases, AlturaBase, 0f), config.diametroBase, matGiz);
            Circulo("BaseB", bases.transform, new Vector3(meioBases, AlturaBase, 0f), config.diametroBase, matGiz);

            // ---------- Garrafas ----------
            GameObject garrafas = new GameObject("Garrafas");
            Garrafa garrafaA = CriarGarrafa("GarrafaA", garrafas.transform, new Vector3(-meioBases, 0f, 0f), config, matGarrafa);
            Garrafa garrafaB = CriarGarrafa("GarrafaB", garrafas.transform, new Vector3(meioBases, 0f, 0f), config, matGarrafa);
            Garrafa[] todas = { garrafaA, garrafaB };

            // ---------- Bola ----------
            GameObject objBola = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            objBola.name = "Bola";
            Object.DestroyImmediate(objBola.GetComponent<Collider>()); // física própria, sem collider
            objBola.GetComponent<MeshRenderer>().sharedMaterial = matBola;
            float raioVisual = config.raioBola * config.escalaVisualBola;
            objBola.transform.position = LancadorDeTeste.PosicaoRepouso(config) + Vector3.up * (raioVisual - config.raioBola);
            Bola bola = objBola.AddComponent<Bola>();
            Ligar(bola, "config", config);
            LigarLista(bola, "garrafas", todas);

            // ---------- Câmera de transmissão ----------
            Camera cameraPrincipal = Camera.main;
            cameraPrincipal.name = "CameraTransmissao";
            CameraTransmissao transmissao = cameraPrincipal.gameObject.AddComponent<CameraTransmissao>();
            Ligar(transmissao, "configCamera", configCamera);
            Ligar(transmissao, "configFisica", config);
            Ligar(transmissao, "alvo", objBola.transform);

            // ---------- Lançador de teste ----------
            GameObject objLancador = new GameObject("LancadorDeTeste");
            LancadorDeTeste lancador = objLancador.AddComponent<LancadorDeTeste>();
            Ligar(lancador, "config", config);
            Ligar(lancador, "bola", bola);
            Ligar(lancador, "garrafaAlvo", garrafaB);
            LigarLista(lancador, "todasGarrafas", todas);

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

        private static Material CriarMaterial(string nome, Color cor)
        {
            CriarPasta("Assets", "Materiais");
            string caminho = $"{PastaMateriais}/{nome}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(caminho);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(material, caminho);
            }
            material.SetColor("_BaseColor", cor);
            material.SetFloat("_Smoothness", 0.1f);
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

        private static void Circulo(string nome, Transform pai, Vector3 posicao, float diametro, Material material)
        {
            // Cilindro achatado sem colisão = círculo pintado no chão
            GameObject circulo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            circulo.name = nome;
            circulo.transform.SetParent(pai);
            circulo.transform.localPosition = posicao;
            circulo.transform.localScale = new Vector3(diametro, EspessuraMarca * 0.5f, diametro);
            circulo.GetComponent<MeshRenderer>().sharedMaterial = material;
            Object.DestroyImmediate(circulo.GetComponent<Collider>());
        }

        private static Garrafa CriarGarrafa(string nome, Transform pai, Vector3 posicaoBase,
                                            ConfigFisica config, Material material)
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
    }
}
