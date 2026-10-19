using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace BetsDeRua.Editor
{
    /// <summary>
    /// Monta a cena do menu (Assets/Scenes/Menu.unity): um pedaço da rua de fundo no fim da tarde,
    /// o seu personagem na "vitrine" (em pé com o taco), a câmera que passeia entre as telas, o
    /// pós-processamento (brilho, cor, vinheta) e a interface (UI Toolkit). Menu "Bets > Montar cena Menu".
    /// Também cria os arquivos de interface usados nas duas cenas (painel e tema).
    /// </summary>
    public static class MontadorMenu
    {
        private const string CaminhoCena = "Assets/Scenes/Menu.unity";
        public const string CaminhoPainel = "Assets/UI/PainelUI.asset";
        public const string CaminhoEstilo = "Assets/UI/Menu.uss";
        private const string CaminhoTema = "Assets/UI/TemaPadrao.tss";
        private const string CaminhoPos = "Assets/Settings/PosProcessamento.asset";

        [MenuItem("Bets/Montar cena Menu")]
        public static void Montar()
        {
            Scene cena = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            ConfigFisica config = MontadorCena.CarregarOuCriarConfig<ConfigFisica>("Assets/Config/ConfigFisica.asset");
            ConfigPartida configPartida = MontadorCena.CarregarOuCriarConfig<ConfigPartida>("Assets/Config/ConfigPartida.asset");

            // ---------- Rua de fundo ----------
            GameObject campo = new GameObject("Rua");
            Material matChao = MontadorCena.CriarMaterial("Chao", new Color(0.22f, 0.22f, 0.23f));
            MontadorCena.Cubo("Chao", campo.transform, new Vector3(0f, -0.1f, 0f), new Vector3(config.comprimentoCampo, 0.2f, config.larguraCampo), matChao, true);
            ConstrutorRuaEditor.Montar(campo.transform, config);
            Material matGiz = MontadorCena.CriarMaterial("Giz", new Color(0.95f, 0.95f, 0.95f));
            MontadorCena.BaseDeGiz("BaseMenu", campo.transform, new Vector3(1.4f, 0f, 0.6f), config, matGiz);
            Material matGarrafa = MontadorCena.CriarMaterial("Garrafa", new Color(0.12f, 0.42f, 0.22f));
            Material matTampa = MontadorCena.CriarMaterial("TampaGarrafa", new Color(0.95f, 0.95f, 0.95f));
            Garrafa garrafa = MontadorCena.CriarGarrafa("Garrafa", campo.transform, new Vector3(1.4f, 0f, 0.6f), config, matGarrafa, matTampa);
            garrafa.enabled = false;

            // ---------- Vitrine: o personagem ----------
            GameObject vitrine = new GameObject("Vitrine");
            vitrine.transform.position = new Vector3(0f, 0f, 0f);
            vitrine.transform.rotation = Quaternion.Euler(0f, 200f, 0f);
            vitrine.AddComponent<PoseVitrine>();

            // ---------- Luz e câmera ----------
            Light sol = Object.FindFirstObjectByType<Light>();
            ConfigurarSol(sol);
            Camera camera = Camera.main;
            camera.transform.position = new Vector3(-3.2f, 1.45f, -3.6f);
            camera.transform.LookAt(new Vector3(-0.9f, 1.05f, 0f));
            camera.fieldOfView = 38f;
            ConfigurarCamera(camera);
            CriarVolume();

            // ---------- Interface ----------
            GameObject interfaceObj = new GameObject("Interface");
            UIDocument documento = interfaceObj.AddComponent<UIDocument>();
            documento.panelSettings = PainelUI();
            MenuPrincipal menu = interfaceObj.AddComponent<MenuPrincipal>();
            SerializedObject dados = new SerializedObject(menu);
            dados.FindProperty("documento").objectReferenceValue = documento;
            dados.FindProperty("estilo").objectReferenceValue = AssetDatabase.LoadAssetAtPath<StyleSheet>(CaminhoEstilo);
            dados.FindProperty("vitrine").objectReferenceValue = vitrine.transform;
            dados.FindProperty("cameraFundo").objectReferenceValue = camera;
            dados.FindProperty("materialTime").objectReferenceValue = MontadorCena.CriarMaterial("DuplaTaco", configPartida.corDuplaTaco);
            dados.FindProperty("configFisica").objectReferenceValue = config;
            dados.FindProperty("biblioteca").objectReferenceValue = AssetDatabase.LoadAssetAtPath<BibliotecaCorpos>("Assets/Config/BibliotecaCorpos.asset");
            dados.ApplyModifiedPropertiesWithoutUndo();
            CriarSistemaDeEventos();
            interfaceObj.AddComponent<AmbienteRua>();

            EditorSceneManager.SaveScene(cena, CaminhoCena);
            AtualizarBuild();
            AssetDatabase.SaveAssets();
            Debug.Log("[Bets] Cena Menu montada em " + CaminhoCena);
        }

        /// <summary>Menu primeiro, depois a partida.</summary>
        public static void AtualizarBuild()
        {
            var cenas = new System.Collections.Generic.List<EditorBuildSettingsScene>();
            if (System.IO.File.Exists(CaminhoCena)) cenas.Add(new EditorBuildSettingsScene(CaminhoCena, true));
            cenas.Add(new EditorBuildSettingsScene("Assets/Scenes/Prototipo.unity", true));
            EditorBuildSettings.scenes = cenas.ToArray();
        }

        /// <summary>Painel da interface (escala para 1920x1080, tema padrão). Cria se não existir.</summary>
        public static PanelSettings PainelUI()
        {
            PanelSettings painel = AssetDatabase.LoadAssetAtPath<PanelSettings>(CaminhoPainel);
            if (painel == null)
            {
                painel = ScriptableObject.CreateInstance<PanelSettings>();
                AssetDatabase.CreateAsset(painel, CaminhoPainel);
            }
            AssetDatabase.ImportAsset(CaminhoTema);
            painel.themeStyleSheet = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(CaminhoTema);
            painel.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            painel.referenceResolution = new Vector2Int(1920, 1080);
            painel.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            painel.match = 0.5f;
            EditorUtility.SetDirty(painel);
            return painel;
        }

        /// <summary>EventSystem com o módulo do Input System (mouse, teclado e controle na interface).</summary>
        public static void CriarSistemaDeEventos()
        {
            GameObject eventos = new GameObject("EventSystem");
            eventos.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventos.AddComponent<InputSystemUIInputModule>();
        }

        /// <summary>Sol de fim de tarde: baixo, quente, com sombra suave; céu e ambiente combinando.</summary>
        public static void ConfigurarSol(Light sol)
        {
            if (sol == null) return;
            sol.transform.rotation = Quaternion.Euler(32f, -38f, 0f);
            sol.color = new Color(1f, 0.86f, 0.68f);
            sol.intensity = 1.6f;
            sol.shadows = LightShadows.Soft;
            sol.shadowStrength = 0.85f;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.56f, 0.66f, 0.85f);
            RenderSettings.ambientEquatorColor = new Color(0.62f, 0.56f, 0.5f);
            RenderSettings.ambientGroundColor = new Color(0.25f, 0.22f, 0.2f);
            Material ceu = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materiais/Ceu.mat");
            if (ceu == null)
            {
                ceu = new Material(Shader.Find("Skybox/Procedural"));
                AssetDatabase.CreateAsset(ceu, "Assets/Materiais/Ceu.mat");
            }
            ceu.SetFloat("_SunSize", 0.035f);
            ceu.SetFloat("_AtmosphereThickness", 1.15f);
            ceu.SetColor("_SkyTint", new Color(0.52f, 0.6f, 0.78f));
            ceu.SetColor("_GroundColor", new Color(0.4f, 0.38f, 0.36f));
            ceu.SetFloat("_Exposure", 1.25f);
            EditorUtility.SetDirty(ceu);
            RenderSettings.skybox = ceu;
            RenderSettings.sun = sol;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.72f, 0.72f, 0.75f);
            RenderSettings.fogStartDistance = 45f;
            RenderSettings.fogEndDistance = 160f;
        }

        public static void ConfigurarCamera(Camera camera)
        {
            camera.clearFlags = CameraClearFlags.Skybox;
            UniversalAdditionalCameraData dadosCamera = camera.GetUniversalAdditionalCameraData();
            dadosCamera.renderPostProcessing = true;
            dadosCamera.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            dadosCamera.antialiasingQuality = AntialiasingQuality.High;
        }

        /// <summary>Volume global: tom de filme (ACES), um pouco mais de cor e contraste, brilho e vinheta.</summary>
        public static void CriarVolume()
        {
            VolumeProfile perfil = AssetDatabase.LoadAssetAtPath<VolumeProfile>(CaminhoPos);
            if (perfil == null)
            {
                perfil = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(perfil, CaminhoPos);
                Tonemapping tom = perfil.Add<Tonemapping>(true);
                tom.mode.Override(TonemappingMode.ACES);
                ColorAdjustments cor = perfil.Add<ColorAdjustments>(true);
                cor.postExposure.Override(0.35f);
                cor.contrast.Override(12f);
                cor.saturation.Override(14f);
                Bloom brilho = perfil.Add<Bloom>(true);
                brilho.intensity.Override(0.35f);
                brilho.threshold.Override(1.05f);
                Vignette vinheta = perfil.Add<Vignette>(true);
                vinheta.intensity.Override(0.22f);
                vinheta.smoothness.Override(0.45f);
                foreach (VolumeComponent c in perfil.components) AssetDatabase.AddObjectToAsset(c, perfil);
                EditorUtility.SetDirty(perfil);
            }
            GameObject objVolume = new GameObject("PosProcessamento");
            Volume volume = objVolume.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = perfil;
        }
    }
}
