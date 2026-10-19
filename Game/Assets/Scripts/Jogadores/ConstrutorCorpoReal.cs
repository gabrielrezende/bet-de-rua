using System.Collections.Generic;
using UnityEngine;

namespace BetsDeRua
{
    /// <summary>
    /// Monta o personagem com corpo de verdade (malha com esqueleto humano e animações de captura,
    /// BibliotecaCorpos) a partir da ficha: corpo masculino ou feminino, altura (escala), largura
    /// (corpo magro/médio/forte nos ossos do tronco), cabeça maior na criança, tom de pele, cabelo,
    /// barba, sobrancelhas, roupa (camisa do time, bermuda, tênis) e boné/chapéu na cabeça.
    /// O resultado fica no filho "Corpo" (o mesmo nome do boneco antigo), com um Animator.
    /// </summary>
    public static class ConstrutorCorpoReal
    {
        private static readonly Dictionary<string, Material> materiais = new Dictionary<string, Material>();
        private static Shader lit;

        public static Animator Montar(Transform raiz, PerfilPersonagem perfil, Material matTime, BibliotecaCorpos b)
        {
            Transform antigo = raiz.Find("Corpo");
            if (antigo != null)
            {
                antigo.name = "CorpoAntigo";
                antigo.gameObject.SetActive(false);
                if (Application.isPlaying) Object.Destroy(antigo.gameObject);
                else Object.DestroyImmediate(antigo.gameObject);
            }
            bool feminino = perfil != null && perfil.genero == Genero.Feminino;
            int g = feminino ? 1 : 0;
            GameObject modelo = feminino && b.corpoFeminino != null ? b.corpoFeminino : b.corpoMasculino;

            GameObject corpo = Object.Instantiate(modelo, raiz, false);
            corpo.name = "Corpo";
            corpo.transform.localPosition = Vector3.zero;
            corpo.transform.localRotation = Quaternion.identity;
            float altura = perfil != null ? perfil.altura : PerfilPersonagem.AlturaReferencia;
            float escala = altura / b.alturaBase;
            corpo.transform.localScale = Vector3.one * escala;

            Animator animador = corpo.GetComponent<Animator>();
            if (animador == null) animador = corpo.AddComponent<Animator>();
            animador.runtimeAnimatorController = b.controladorJogador;
            animador.applyRootMotion = false;
            animador.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            // ---- Pele, olhos, sobrancelhas originais
            Color pele = perfil != null ? perfil.corPele : new Color(0.72f, 0.53f, 0.38f);
            bool peleEscura = pele.grayscale < 0.5f;
            Texture2D texPele = feminino ? (peleEscura ? b.peleEscuraFeminina : b.peleClaraFeminina) : (peleEscura ? b.peleEscuraMasculina : b.peleClaraMasculina);
            Texture2D texNormal = feminino ? b.normalFeminina : b.normalMasculina;
            // Ajusta o tom: a textura já tem o tom base, a cor do material corrige para o tom escolhido
            Color referencia = peleEscura ? new Color(0.45f, 0.3f, 0.22f) : new Color(0.86f, 0.68f, 0.55f);
            Color tinta = new Color(Mathf.Clamp(pele.r / referencia.r, 0.4f, 1.6f), Mathf.Clamp(pele.g / referencia.g, 0.4f, 1.6f),
                                    Mathf.Clamp(pele.b / referencia.b, 0.4f, 1.6f));
            Material matPele = Mat($"pele{g}{peleEscura}{ColorUtility.ToHtmlStringRGB(tinta)}", Color.Lerp(Color.white, tinta, 0.85f), texPele, texNormal, 0.35f);
            SkinnedMeshRenderer principal = null;
            foreach (SkinnedMeshRenderer r in corpo.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (principal == null || r.sharedMesh.vertexCount > principal.sharedMesh.vertexCount) principal = r;
                r.updateWhenOffscreen = true;
            }
            Color corCabelo = perfil != null ? perfil.corCabelo : new Color(0.12f, 0.08f, 0.05f);
            Material matCabelo1 = Mat($"cabelo1{ColorUtility.ToHtmlStringRGB(corCabelo)}", Clarear(corCabelo), b.cabelo1, b.cabelo1Normal, 0.3f);
            Material matCabelo2 = Mat($"cabelo2{ColorUtility.ToHtmlStringRGB(corCabelo)}", Clarear(corCabelo), b.cabelo2, b.cabelo2Normal, 0.3f);
            foreach (SkinnedMeshRenderer r in corpo.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if (r == principal) r.sharedMaterial = matPele;
                else if (r.name.Contains("Eye") && !r.name.Contains("brow")) r.sharedMaterial = Mat("olhos", Color.white, b.olhos, null, 0.8f);
                else r.sharedMaterial = matCabelo1; // sobrancelhas do corpo
            }

            // ---- Roupa (camisa e boné na cor do time: peças com nome "Time_")
            Material tecidoTime = matTime != null ? matTime : Mat("time", Color.blue, null, null, 0.2f);
            Material camisa = MatTecido(tecidoTime, b);
            Color corBermuda = perfil != null ? perfil.corBermuda : new Color(0.16f, 0.18f, 0.24f);
            Color corCalcado = perfil != null ? perfil.corCalcado : Color.white;
            Material bermuda = Mat($"bermuda{ColorUtility.ToHtmlStringRGB(corBermuda)}", corBermuda, b.tecido, null, 0.15f);
            Material calcado = Mat($"calcado{ColorUtility.ToHtmlStringRGB(corCalcado)}", corCalcado, null, null, 0.45f);
            Material meia = Mat("meia", new Color(0.95f, 0.95f, 0.93f), b.tecido, null, 0.1f);

            Camisa tipoCamisa = perfil != null ? perfil.camisa : Camisa.Camiseta;
            Mesh malhaCamisa = tipoCamisa == Camisa.Regata ? b.regata[g] : tipoCamisa == Camisa.MangaLonga ? b.mangaLonga[g] : b.camiseta[g];
            Peca(corpo.transform, principal, "Time_Camisa", malhaCamisa, camisa);
            Bermuda tipoBermuda = perfil != null ? perfil.bermuda : Bermuda.Bermuda;
            Peca(corpo.transform, principal, "Bermuda", tipoBermuda == Bermuda.Calca ? b.calca[g] : tipoBermuda == Bermuda.Calcao ? b.calcao[g] : b.bermuda[g], bermuda);
            Calcado tipoCalcado = perfil != null ? perfil.calcado : Calcado.Tenis;
            if (tipoCalcado != Calcado.Chinelo)
            {
                Peca(corpo.transform, principal, "Meia", b.meia[g], meia);
                Peca(corpo.transform, principal, "Tenis", tipoCalcado == Calcado.TenisCanoAlto ? b.tenisCanoAlto[g] : b.tenis[g], calcado);
            }

            // ---- Cabelo, barba, sobrancelhas
            EstiloCabelo estilo = perfil != null ? perfil.cabelo : EstiloCabelo.Curto;
            GameObject cabelo = estilo == EstiloCabelo.Raspado || estilo == EstiloCabelo.Moicano ? (feminino ? b.cabeloRaspadoFeminino : b.cabeloRaspado)
                              : estilo == EstiloCabelo.Curto ? b.cabeloCurto
                              : estilo == EstiloCabelo.Longo ? b.cabeloLongo
                              : estilo == EstiloCabelo.Trancas ? b.cabeloCoque
                              : null;
            if (cabelo != null) Vestir(corpo.transform, principal, cabelo, estilo == EstiloCabelo.Longo || estilo == EstiloCabelo.Trancas ? matCabelo2 : matCabelo1);
            if (perfil != null && perfil.barba != Barba.Nada && b.barba != null) Vestir(corpo.transform, principal, b.barba, matCabelo1);

            Animator anim = animador;
            Transform cabeca = anim.isHuman ? anim.GetBoneTransform(HumanBodyBones.Head) : null;
            if (cabeca != null)
            {
                // Black power: volume redondo no osso da cabeça (o pacote não tem esse cabelo)
                if (estilo == EstiloCabelo.BlackPower)
                    Enfeitar(cabeca, "BlackPower", new Vector3(0f, 0.12f, -0.025f), new Vector3(0.24f, 0.2f, 0.23f), MalhasBoneco.Esfera(), matCabelo1, escala);
                if (estilo == EstiloCabelo.Moicano)
                    Enfeitar(cabeca, "Moicano", new Vector3(0f, 0.19f, 0f), new Vector3(0.045f, 0.12f, 0.22f), MalhasBoneco.Esfera(), matCabelo1, escala);
                Chapeu chapeu = perfil != null ? perfil.chapeu : Chapeu.Nada;
                if (chapeu == Chapeu.Bone || chapeu == Chapeu.BoneParaTras)
                {
                    float frente = chapeu == Chapeu.Bone ? 1f : -1f;
                    Enfeitar(cabeca, "Time_Bone", new Vector3(0f, 0.105f, 0.005f), new Vector3(0.105f, 0.075f, 0.118f), MalhasBoneco.Calota(0f, 90f), tecidoTime, escala);
                    Enfeitar(cabeca, "Time_Aba", new Vector3(0f, 0.11f, frente * 0.12f), new Vector3(0.085f, 0.008f, 0.07f), MalhasBoneco.Esfera(), tecidoTime, escala);
                }
                else if (chapeu == Chapeu.Bucket)
                {
                    Enfeitar(cabeca, "Time_Bone", new Vector3(0f, 0.11f, 0f), new Vector3(0.11f, 0.085f, 0.12f), MalhasBoneco.Calota(0f, 90f), tecidoTime, escala);
                    Enfeitar(cabeca, "Time_Aba", new Vector3(0f, 0.11f, 0f), Vector3.one, MalhasBoneco.Aba(0.1f, 0.17f, 0.008f), tecidoTime, escala);
                }
                else if (chapeu == Chapeu.Touca)
                    Enfeitar(cabeca, "Time_Bone", new Vector3(0f, 0.11f, -0.01f), new Vector3(0.112f, 0.11f, 0.122f), MalhasBoneco.Calota(-5f, 90f), tecidoTime, escala);
                if (perfil != null && perfil.acessorio == Acessorio.Oculos)
                    Enfeitar(cabeca, "Oculos", new Vector3(0f, 0.075f, 0.105f), new Vector3(0.085f, 0.02f, 0.012f), MalhasBoneco.Esfera(), Mat("oculos", new Color(0.04f, 0.04f, 0.05f), null, null, 0.9f), escala);
                // Criança: cabeça um pouco maior; adolescente, um pouco
                if (perfil != null && perfil.faixaEtaria != FaixaEtaria.Adulto)
                    cabeca.localScale = Vector3.one * (perfil.faixaEtaria == FaixaEtaria.Crianca ? 1.18f : 1.07f);
            }
            if (perfil != null && perfil.acessorio == Acessorio.Corrente && anim.isHuman)
            {
                Transform peito = anim.GetBoneTransform(HumanBodyBones.UpperChest) ?? anim.GetBoneTransform(HumanBodyBones.Chest);
                if (peito != null) Enfeitar(peito, "Corrente", new Vector3(0f, 0.12f, 0.06f), Vector3.one, MalhasBoneco.Anel(0.075f, 0.006f), Mat("ouro", new Color(0.95f, 0.75f, 0.25f), null, null, 0.9f), escala);
            }

            // ---- Corpo magro / forte: alarga ou afina o tronco, braços e coxas (escala dos ossos, a animação não mexe)
            float largura = perfil != null ? perfil.FatorLargura : 1f;
            if (anim.isHuman && Mathf.Abs(largura - 1f) > 0.01f)
            {
                foreach (HumanBodyBones osso in new[] { HumanBodyBones.Spine, HumanBodyBones.Chest })
                {
                    Transform t = anim.GetBoneTransform(osso);
                    if (t != null) t.localScale = new Vector3(Mathf.Lerp(1f, largura, 0.6f), 1f, Mathf.Lerp(1f, largura, 0.8f));
                }
            }
            return animador;
        }

        private static Color Clarear(Color c) => Color.Lerp(c, Color.white, 0.08f) * 1.6f;

        /// <summary>Roupa: uma malha que usa o esqueleto do corpo (anda junto com a animação).</summary>
        private static void Peca(Transform corpo, SkinnedMeshRenderer molde, string nome, Mesh malha, Material material)
        {
            if (malha == null) return;
            GameObject g = new GameObject(nome);
            g.transform.SetParent(molde.transform.parent, false);
            g.transform.localPosition = molde.transform.localPosition;
            g.transform.localRotation = molde.transform.localRotation;
            g.transform.localScale = molde.transform.localScale;
            SkinnedMeshRenderer r = g.AddComponent<SkinnedMeshRenderer>();
            r.sharedMesh = malha;
            r.bones = molde.bones;
            r.rootBone = molde.rootBone;
            r.sharedMaterial = material;
            r.updateWhenOffscreen = true;
        }

        /// <summary>Cabelo/barba (mesmo esqueleto): copia a malha e liga nos ossos do corpo.</summary>
        private static void Vestir(Transform corpo, SkinnedMeshRenderer molde, GameObject modelo, Material material)
        {
            foreach (SkinnedMeshRenderer origem in modelo.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                GameObject g = new GameObject(origem.name);
                g.transform.SetParent(molde.transform.parent, false);
                g.transform.localPosition = molde.transform.localPosition;
                g.transform.localRotation = molde.transform.localRotation;
                g.transform.localScale = molde.transform.localScale;
                SkinnedMeshRenderer r = g.AddComponent<SkinnedMeshRenderer>();
                r.sharedMesh = origem.sharedMesh;
                // Mesma ordem de ossos nos arquivos do pacote; por segurança, liga pelo nome
                var porNome = new Dictionary<string, Transform>();
                foreach (Transform t in molde.bones) if (t != null) porNome[t.name] = t;
                Transform[] ossos = new Transform[origem.bones.Length];
                for (int i = 0; i < ossos.Length; i++)
                    ossos[i] = origem.bones[i] != null && porNome.TryGetValue(origem.bones[i].name, out Transform t) ? t : molde.rootBone;
                r.bones = ossos;
                r.rootBone = molde.rootBone;
                r.sharedMaterial = material;
                r.updateWhenOffscreen = true;
            }
        }

        private static void Enfeitar(Transform osso, string nome, Vector3 local, Vector3 tamanho, Mesh malha, Material material, float escalaCorpo)
        {
            GameObject g = new GameObject(nome);
            g.transform.SetParent(osso, false);
            // Os ossos da cabeça podem ter eixos girados: posiciona no espaço do corpo
            Transform corpo = osso.root;
            g.transform.position = osso.position + osso.root.rotation * (local * escalaCorpo);
            g.transform.rotation = osso.root.rotation;
            g.transform.localScale = Vector3.Scale(tamanho, InversoEscala(osso.lossyScale)) * escalaCorpo;
            g.AddComponent<MeshFilter>().sharedMesh = malha;
            g.AddComponent<MeshRenderer>().sharedMaterial = material;
        }

        private static Vector3 InversoEscala(Vector3 s) => new Vector3(1f / Mathf.Max(0.0001f, s.x), 1f / Mathf.Max(0.0001f, s.y), 1f / Mathf.Max(0.0001f, s.z));

        private static Material MatTecido(Material time, BibliotecaCorpos b)
        {
            Color c = time.HasProperty("_BaseColor") ? time.GetColor("_BaseColor") : time.color;
            return Mat($"camisa{ColorUtility.ToHtmlStringRGB(c)}", c, b.tecido, null, 0.2f);
        }

        public static Material Mat(string chave, Color cor, Texture2D tex, Texture2D normal, float brilho)
        {
            if (materiais.TryGetValue(chave, out Material m) && m != null) return m;
            if (lit == null) lit = Shader.Find("Universal Render Pipeline/Lit");
            m = new Material(lit) { name = "Corpo_" + chave };
            m.SetColor("_BaseColor", cor);
            if (tex != null) m.SetTexture("_BaseMap", tex);
            if (normal != null)
            {
                m.SetTexture("_BumpMap", normal);
                m.EnableKeyword("_NORMALMAP");
            }
            m.SetFloat("_Smoothness", brilho);
            materiais[chave] = m;
            return m;
        }

        /// <summary>Repinta a camisa e o boné com a cor do time (troca de papel).</summary>
        public static void PintarTime(Transform raiz, Material time, BibliotecaCorpos b)
        {
            Material camisa = MatTecido(time, b);
            foreach (Renderer r in raiz.GetComponentsInChildren<Renderer>(true))
            {
                if (!r.name.StartsWith("Time_")) continue;
                r.sharedMaterial = r is SkinnedMeshRenderer ? camisa : time;
            }
        }
    }
}
