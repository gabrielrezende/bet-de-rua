using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace BetsDeRua.Editor
{
    /// <summary>
    /// Prepara os personagens de verdade (Quaternius, CC0) para o jogo:
    /// 1) gera as roupas a partir do próprio corpo (recorta a região de cada osso e "infla" um
    ///    pouco: camiseta, regata, manga longa, bermuda, calção, calça, meia, tênis, cano alto);
    /// 2) gera a textura de tecido;
    /// 3) monta os controladores de animação (jogador e torcida) com as animações de captura;
    /// 4) junta tudo na BibliotecaCorpos (Assets/Config/BibliotecaCorpos.asset).
    /// Menu "Bets > Preparar corpos de verdade".
    /// </summary>
    public static class PreparadorCorpos
    {
        private const string PastaGerados = "Assets/Personagens/Gerados";
        private const string CaminhoBiblioteca = "Assets/Config/BibliotecaCorpos.asset";

        [MenuItem("Bets/Preparar corpos de verdade")]
        public static string Preparar()
        {
            if (!AssetDatabase.IsValidFolder(PastaGerados)) AssetDatabase.CreateFolder("Assets/Personagens", "Gerados");
            BibliotecaCorpos b = AssetDatabase.LoadAssetAtPath<BibliotecaCorpos>(CaminhoBiblioteca);
            if (b == null)
            {
                b = ScriptableObject.CreateInstance<BibliotecaCorpos>();
                AssetDatabase.CreateAsset(b, CaminhoBiblioteca);
            }

            b.corpoMasculino = Carregar<GameObject>("Base/Superhero_Male_FullBody.fbx");
            b.corpoFeminino = Carregar<GameObject>("Base/Superhero_Female_FullBody.fbx");
            b.cabeloRaspado = Carregar<GameObject>("Cabelos/Hair_Buzzed.fbx");
            b.cabeloRaspadoFeminino = Carregar<GameObject>("Cabelos/Hair_BuzzedFemale.fbx");
            b.cabeloCurto = Carregar<GameObject>("Cabelos/Hair_SimpleParted.fbx");
            b.cabeloLongo = Carregar<GameObject>("Cabelos/Hair_Long.fbx");
            b.cabeloCoque = Carregar<GameObject>("Cabelos/Hair_Buns.fbx");
            b.barba = Carregar<GameObject>("Cabelos/Hair_Beard.fbx");
            b.sobrancelhasMasculinas = Carregar<GameObject>("Cabelos/Eyebrows_Regular.fbx");
            b.sobrancelhasFemininas = Carregar<GameObject>("Cabelos/Eyebrows_Female.fbx");

            b.peleClaraMasculina = Carregar<Texture2D>("Base/Texturas/T_Superhero_Male_Ligh.png");
            b.peleEscuraMasculina = Carregar<Texture2D>("Base/Texturas/T_Superhero_Male_Dark.png");
            b.normalMasculina = Carregar<Texture2D>("Base/Texturas/T_Superhero_Male_Normal.png");
            b.peleClaraFeminina = Carregar<Texture2D>("Base/Texturas/T_Superhero_Female_Light_BaseColor.png");
            b.peleEscuraFeminina = Carregar<Texture2D>("Base/Texturas/T_Superhero_Female_Dark_BaseColor.png");
            b.normalFeminina = Carregar<Texture2D>("Base/Texturas/T_Superhero_Female_Normal.png");
            b.olhos = Carregar<Texture2D>("Base/Texturas/T_Eye_Brown.png");
            b.cabelo1 = Carregar<Texture2D>("Base/Texturas/T_Hair_1_BaseColor.png");
            b.cabelo1Normal = Carregar<Texture2D>("Base/Texturas/T_Hair_1_Normal.png");
            b.cabelo2 = Carregar<Texture2D>("Base/Texturas/T_Hair_2_BaseColor.png");
            b.cabelo2Normal = Carregar<Texture2D>("Base/Texturas/T_Hair_2_Normal.png");
            b.tecido = TexturaTecido();

            string relatorio = "";
            for (int g = 0; g < 2; g++)
            {
                GameObject corpo = g == 0 ? b.corpoMasculino : b.corpoFeminino;
                SkinnedMeshRenderer pele = CorpoPrincipal(corpo);
                string s = g == 0 ? "M" : "F";
                Recortador r = new Recortador(pele);
                b.camiseta[g] = r.Gerar($"Camiseta_{s}", 0.012f, Recortador.Camiseta);
                b.regata[g] = r.Gerar($"Regata_{s}", 0.011f, Recortador.Regata);
                b.mangaLonga[g] = r.Gerar($"MangaLonga_{s}", 0.012f, Recortador.MangaLonga);
                b.bermuda[g] = r.Gerar($"Bermuda_{s}", 0.017f, Recortador.Bermuda);
                b.calcao[g] = r.Gerar($"Calcao_{s}", 0.016f, Recortador.Calcao);
                b.calca[g] = r.Gerar($"Calca_{s}", 0.014f, Recortador.Calca);
                b.meia[g] = r.Gerar($"Meia_{s}", 0.006f, Recortador.Meia);
                b.tenis[g] = r.Gerar($"Tenis_{s}", 0.02f, Recortador.Tenis);
                b.tenisCanoAlto[g] = r.Gerar($"TenisCanoAlto_{s}", 0.02f, Recortador.TenisCanoAlto);
                relatorio += $"{s}: camiseta {b.camiseta[g].triangles.Length / 3} tri, bermuda {b.bermuda[g].triangles.Length / 3}, tenis {b.tenis[g].triangles.Length / 3}; ";
            }

            b.controladorJogador = ControladorJogador();
            b.controladorTorcida = ControladorTorcida();
            EditorUtility.SetDirty(b);
            AssetDatabase.SaveAssets();
            return relatorio;
        }

        private static T Carregar<T>(string relativo) where T : Object => AssetDatabase.LoadAssetAtPath<T>($"Assets/Personagens/{relativo}");

        /// <summary>A malha do corpo (a maior: a pele), ignorando olhos e sobrancelhas.</summary>
        public static SkinnedMeshRenderer CorpoPrincipal(GameObject modelo)
        {
            SkinnedMeshRenderer maior = null;
            foreach (SkinnedMeshRenderer r in modelo.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                if (maior == null || r.sharedMesh.vertexCount > maior.sharedMesh.vertexCount) maior = r;
            return maior;
        }

        // ------------------------------------------------------------------ roupas

        /// <summary>Recorta pedaços do corpo pelos ossos (com a posição ao longo do osso) e infla.</summary>
        private class Recortador
        {
            public delegate float Regra(Recortador r, string osso, Vector3 v);

            private readonly Mesh malha;
            private readonly string[] ossos;
            private readonly Dictionary<string, Vector3> posicao = new Dictionary<string, Vector3>();
            private readonly Vector3 cima;
            private readonly float alturaQuadril;

            public Recortador(SkinnedMeshRenderer r)
            {
                malha = r.sharedMesh;
                ossos = new string[r.bones.Length];
                for (int i = 0; i < r.bones.Length; i++)
                {
                    ossos[i] = r.bones[i].name;
                    posicao[ossos[i]] = malha.bindposes[i].inverse.MultiplyPoint3x4(Vector3.zero);
                }
                cima = (posicao["Head"] - posicao["pelvis"]).normalized;
                alturaQuadril = Vector3.Dot(posicao["pelvis"], cima);
            }

            private float Altura(Vector3 v) => Vector3.Dot(v, cima) - alturaQuadril;

            /// <summary>0 no começo do osso, 1 no começo do próximo (ex.: ombro 0, cotovelo 1).</summary>
            private float AoLongo(string de, string ate, Vector3 v)
            {
                Vector3 a = posicao[de], b = posicao[ate];
                Vector3 ab = b - a;
                return Vector3.Dot(v - a, ab) / Mathf.Max(0.0001f, ab.sqrMagnitude);
            }

            private static string Lado(string osso) => osso.EndsWith("_l") ? "_l" : "_r";

            public static float Camiseta(Recortador r, string o, Vector3 v)
            {
                if (o.StartsWith("spine") || o.StartsWith("clavicle")) return 1f;
                if (o == "pelvis") return r.Altura(v) > -0.06f ? 1f : 0f;
                if (o.StartsWith("upperarm")) return r.AoLongo("upperarm" + Lado(o), "lowerarm" + Lado(o), v) < 0.5f ? 1f : 0f;
                if (o == "neck_01") return r.Altura(v) < Vector3.Dot(r.posicao["neck_01"], r.cima) - r.alturaQuadril + 0.01f ? 0.6f : 0f;
                return 0f;
            }

            public static float Regata(Recortador r, string o, Vector3 v)
            {
                if (o.StartsWith("spine")) return 1f;
                if (o.StartsWith("clavicle")) return 0.7f;
                if (o == "pelvis") return r.Altura(v) > -0.06f ? 1f : 0f;
                return 0f;
            }

            public static float MangaLonga(Recortador r, string o, Vector3 v)
            {
                float c = Camiseta(r, o, v);
                if (c > 0f) return c;
                if (o.StartsWith("upperarm")) return 1f;
                if (o.StartsWith("lowerarm")) return r.AoLongo("lowerarm" + Lado(o), "hand" + Lado(o), v) < 0.92f ? 1f : 0f;
                return 0f;
            }

            private static float Perna(Recortador r, string o, Vector3 v, float ateCoxa, float ateCanela)
            {
                if (o == "pelvis") return r.Altura(v) < 0.07f ? 1f : 0f;
                if (o.StartsWith("thigh")) return r.AoLongo("thigh" + Lado(o), "calf" + Lado(o), v) < ateCoxa ? 1f : 0f;
                if (o.StartsWith("calf") && ateCanela > 0f) return r.AoLongo("calf" + Lado(o), "foot" + Lado(o), v) < ateCanela ? 1f : 0f;
                return 0f;
            }

            public static float Bermuda(Recortador r, string o, Vector3 v) => Perna(r, o, v, 0.88f, 0f);
            public static float Calcao(Recortador r, string o, Vector3 v) => Perna(r, o, v, 0.45f, 0f);
            public static float Calca(Recortador r, string o, Vector3 v) => Perna(r, o, v, 2f, 0.93f);

            private static float Pe(Recortador r, string o, Vector3 v, float desdeCanela)
            {
                if (o.StartsWith("foot") || o.StartsWith("ball")) return 1f;
                if (o.StartsWith("calf")) return r.AoLongo("calf" + Lado(o), "foot" + Lado(o), v) > desdeCanela ? 1f : 0f;
                return 0f;
            }

            public static float Tenis(Recortador r, string o, Vector3 v) => Pe(r, o, v, 0.9f);
            public static float TenisCanoAlto(Recortador r, string o, Vector3 v) => Pe(r, o, v, 0.74f);
            public static float Meia(Recortador r, string o, Vector3 v) => o.StartsWith("calf") && r.AoLongo("calf" + Lado(o), "foot" + Lado(o), v) > 0.72f ? 1f : 0f;

            public Mesh Gerar(string nome, float espessura, Regra regra)
            {
                Vector3[] v = malha.vertices;
                Vector3[] n = malha.normals;
                BoneWeight[] w = malha.boneWeights;
                bool[] dentro = new bool[v.Length];
                for (int i = 0; i < v.Length; i++)
                {
                    float soma = 0f;
                    soma += w[i].weight0 * regra(this, ossos[w[i].boneIndex0], v[i]);
                    soma += w[i].weight1 * regra(this, ossos[w[i].boneIndex1], v[i]);
                    soma += w[i].weight2 * regra(this, ossos[w[i].boneIndex2], v[i]);
                    soma += w[i].weight3 * regra(this, ossos[w[i].boneIndex3], v[i]);
                    dentro[i] = soma >= 0.5f;
                }
                int[] tri = malha.triangles;
                var novos = new List<int>();
                for (int t = 0; t < tri.Length; t += 3)
                    if (dentro[tri[t]] && dentro[tri[t + 1]] && dentro[tri[t + 2]])
                    {
                        novos.Add(tri[t]); novos.Add(tri[t + 1]); novos.Add(tri[t + 2]);
                    }
                Vector3[] inflado = new Vector3[v.Length];
                for (int i = 0; i < v.Length; i++) inflado[i] = v[i] + n[i] * espessura;

                Mesh m = new Mesh { name = nome };
                m.vertices = inflado;
                m.normals = n;
                m.uv = malha.uv;
                m.boneWeights = w;
                m.bindposes = malha.bindposes;
                m.SetTriangles(novos, 0);
                m.RecalculateTangents();
                m.RecalculateBounds();
                string caminho = $"{PastaGerados}/{nome}.asset";
                Mesh existente = AssetDatabase.LoadAssetAtPath<Mesh>(caminho);
                if (existente != null)
                {
                    EditorUtility.CopySerialized(m, existente);
                    return existente;
                }
                AssetDatabase.CreateAsset(m, caminho);
                return m;
            }
        }

        /// <summary>Tecido de malha (camiseta): trama fina clara, a cor vem do material.</summary>
        private static Texture2D TexturaTecido()
        {
            const int n = 256;
            Texture2D t = new Texture2D(n, n, TextureFormat.RGBA32, false);
            System.Random r = new System.Random(9);
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float trama = 0.9f + 0.06f * Mathf.Sin(x * 1.6f) * Mathf.Sin(y * 1.6f) + (float)(r.NextDouble() - 0.5) * 0.05f;
                    t.SetPixel(x, y, new Color(trama, trama, trama, 1f));
                }
            t.Apply();
            string caminho = $"{PastaGerados}/Tecido.png";
            File.WriteAllBytes(caminho, t.EncodeToPNG());
            Object.DestroyImmediate(t);
            AssetDatabase.ImportAsset(caminho);
            return AssetDatabase.LoadAssetAtPath<Texture2D>(caminho);
        }

        // ------------------------------------------------------------------ animação

        private static AnimationClip Clipe(string nome)
        {
            foreach (string arquivo in new[] { "Assets/Personagens/Animacoes/UAL1_Standard.fbx", "Assets/Personagens/Animacoes/UAL2_Standard.fbx" })
                foreach (Object o in AssetDatabase.LoadAllAssetsAtPath(arquivo))
                    if (o is AnimationClip c && (c.name == nome || c.name.EndsWith("|" + nome))) return c;
            Debug.LogWarning($"[Bets] animação não encontrada: {nome}");
            return null;
        }

        private static AnimatorController ControladorJogador()
        {
            string caminho = $"{PastaGerados}/ControladorJogador.controller";
            AssetDatabase.DeleteAsset(caminho);
            AnimatorController c = AnimatorController.CreateAnimatorControllerAtPath(caminho);
            c.AddParameter("Velocidade", AnimatorControllerParameterType.Float);
            c.AddParameter("Guarda", AnimatorControllerParameterType.Bool);
            c.AddParameter("Mergulhando", AnimatorControllerParameterType.Bool);
            AnimatorStateMachine sm = c.layers[0].stateMachine;

            AnimatorState locomocao = c.CreateBlendTreeInController("Locomocao", out BlendTree arvore, 0);
            arvore.blendType = BlendTreeType.Simple1D;
            arvore.blendParameter = "Velocidade";
            arvore.useAutomaticThresholds = false;
            arvore.AddChild(Clipe("Idle_Loop"), 0f);
            arvore.AddChild(Clipe("Walk_Loop"), 1.4f);
            arvore.AddChild(Clipe("Jog_Fwd_Loop"), 3.6f);
            arvore.AddChild(Clipe("Sprint_Loop"), 6.8f);
            sm.defaultState = locomocao;

            AnimatorState guarda = sm.AddState("Guarda");
            guarda.motion = Clipe("Sword_Idle");
            var ida = locomocao.AddTransition(guarda);
            ida.AddCondition(AnimatorConditionMode.If, 0f, "Guarda");
            ida.hasExitTime = false;
            ida.duration = 0.2f;
            var volta = guarda.AddTransition(locomocao);
            volta.AddCondition(AnimatorConditionMode.IfNot, 0f, "Guarda");
            volta.hasExitTime = false;
            volta.duration = 0.2f;

            // Gestos tocados pelo jogo (CrossFade com o nome do estado); voltam sozinhos para a locomoção
            Gesto(sm, locomocao, "Arremesso", Clipe("OverhandThrow"), 0.92f);
            Gesto(sm, locomocao, "Batida", Clipe("Sword_Regular_A"), 0.9f);
            Gesto(sm, locomocao, "Pegar", Clipe("PickUp_Table"), 0.85f);
            Gesto(sm, locomocao, "Comemorar", Clipe("Yes"), 0.95f);
            Gesto(sm, locomocao, "Lamentar", Clipe("Idle_No_Loop"), 0.9f);
            Gesto(sm, locomocao, "Chute", Clipe("OverhandThrow"), 0.92f);

            // Mergulho: entra deslizando, segura enquanto estiver no chão, levanta
            AnimatorState entra = sm.AddState("MergulhoInicio");
            entra.motion = Clipe("Slide_Start");
            AnimatorState chao = sm.AddState("MergulhoChao");
            chao.motion = Clipe("Slide_Loop");
            AnimatorState sai = sm.AddState("MergulhoFim");
            sai.motion = Clipe("Slide_Exit");
            var t1 = entra.AddTransition(chao);
            t1.hasExitTime = true; t1.exitTime = 0.95f; t1.duration = 0.05f;
            var t2 = chao.AddTransition(sai);
            t2.AddCondition(AnimatorConditionMode.IfNot, 0f, "Mergulhando");
            t2.hasExitTime = false; t2.duration = 0.1f;
            var t3 = sai.AddTransition(locomocao);
            t3.hasExitTime = true; t3.exitTime = 0.9f; t3.duration = 0.15f;

            // IK: mãos no taco e olhar para a bola
            AnimatorControllerLayer[] camadas = c.layers;
            camadas[0].iKPass = true;
            c.layers = camadas;
            EditorUtility.SetDirty(c);
            return c;
        }

        private static void Gesto(AnimatorStateMachine sm, AnimatorState volta, string nome, AnimationClip clipe, float saida)
        {
            AnimatorState e = sm.AddState(nome);
            e.motion = clipe;
            var t = e.AddTransition(volta);
            t.hasExitTime = true;
            t.exitTime = saida;
            t.duration = 0.2f;
        }

        private static AnimatorController ControladorTorcida()
        {
            string caminho = $"{PastaGerados}/ControladorTorcida.controller";
            AssetDatabase.DeleteAsset(caminho);
            AnimatorController c = AnimatorController.CreateAnimatorControllerAtPath(caminho);
            AnimatorStateMachine sm = c.layers[0].stateMachine;
            AnimatorState parado = null;
            foreach (var par in new[] { ("Parado", "Idle_Loop"), ("BracosCruzados", "Idle_FoldArms_Loop"), ("Conversando", "Idle_Talking_Loop"),
                                        ("Celular", "Idle_TalkingPhone_Loop"), ("Sentado", "Sitting_Idle_Loop"), ("SentadoConversando", "Sitting_Talking_Loop"),
                                        ("Torcendo", "Dance_Loop"), ("Agachado", "Crouch_Idle_Loop") })
            {
                AnimatorState s = sm.AddState(par.Item1);
                s.motion = Clipe(par.Item2);
                if (parado == null) parado = s;
            }
            AnimatorState sim = sm.AddState("Sim");
            sim.motion = Clipe("Yes");
            AnimatorState nao = sm.AddState("Nao");
            nao.motion = Clipe("Idle_No_Loop");
            sm.defaultState = parado;
            AnimatorControllerLayer[] camadas = c.layers;
            camadas[0].iKPass = true;
            c.layers = camadas;
            EditorUtility.SetDirty(c);
            return c;
        }
    }
}
