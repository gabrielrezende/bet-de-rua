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
            // Pessoa realista (MakeHuman) quando houver uma que combine; senão, o corpo de peças
            BibliotecaCorpos.Pessoa pessoa = b.EscolherPessoa(perfil);
            if (pessoa != null) return MontarPessoa(raiz, perfil, matTime, b, pessoa);

            bool feminino = perfil != null && perfil.genero == Genero.Feminino;
            int g = feminino ? 1 : 0;
            GameObject modelo = feminino && b.corpoFeminino != null ? b.corpoFeminino : b.corpoMasculino;

            GameObject corpo = Object.Instantiate(modelo, raiz, false);
            corpo.name = "Corpo";
            corpo.transform.localPosition = Vector3.zero;
            // O modelo do pacote olha para -Z; o jogador olha para +Z: meia volta
            corpo.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
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
            // Com boné/chapéu, o cabelo curto fica escondido (raspado aparece só nas laterais)
            Chapeu naCabeca = perfil != null ? perfil.chapeu : Chapeu.Nada;
            if (naCabeca != Chapeu.Nada && (estilo == EstiloCabelo.Curto || estilo == EstiloCabelo.BlackPower || estilo == EstiloCabelo.Moicano))
                estilo = EstiloCabelo.Raspado;
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
                // Black power: uma base em cúpula coberta de cachinhos (o pacote não tem esse cabelo).
                // Antes era uma bola só, que parecia um capacete preto e tampava o rosto.
                if (estilo == EstiloCabelo.BlackPower)
                {
                    Vector3 centro = new Vector3(0f, 0.15f, -0.04f);
                    Vector3 raio = new Vector3(0.122f, 0.126f, 0.132f);
                    Enfeitar(cabeca, "BlackPower", centro, raio * 0.97f, MalhasBoneco.Calota(-5f, 90f), matCabelo1, escala);
                    int n = 0;
                    for (int anel = 0; anel < 7; anel++)
                    {
                        float lat = Mathf.Lerp(-14f, 82f, anel / 6f) * Mathf.Deg2Rad;
                        int voltas = Mathf.Max(6, Mathf.RoundToInt(26 * Mathf.Cos(lat)));
                        for (int k = 0; k < voltas; k++)
                        {
                            float lon = (k + (anel % 2) * 0.5f) / voltas * Mathf.PI * 2f;
                            Vector3 d = new Vector3(Mathf.Cos(lat) * Mathf.Sin(lon), Mathf.Sin(lat), Mathf.Cos(lat) * Mathf.Cos(lon));
                            // Deixa a testa e o rosto livres (a frente é +z)
                            if (d.z > 0.3f && d.y < 0.62f) continue;
                            float t = 0.026f + 0.008f * Mathf.Abs(Mathf.Sin(n * 2.3f));
                            Enfeitar(cabeca, "Cacho", centro + Vector3.Scale(d, raio), Vector3.one * t, MalhasBoneco.Esfera(), matCabelo1, escala);
                            n++;
                        }
                    }
                }
                if (estilo == EstiloCabelo.Moicano)
                    Enfeitar(cabeca, "Moicano", new Vector3(0f, 0.2f, 0f), new Vector3(0.04f, 0.07f, 0.13f), MalhasBoneco.Esfera(), matCabelo1, escala);
                ChapeuEOculos(cabeca, perfil, tecidoTime, escala);
                // Criança: cabeça um pouco maior; adolescente, um pouco
                if (perfil != null && perfil.faixaEtaria != FaixaEtaria.Adulto)
                    cabeca.localScale = Vector3.one * (perfil.faixaEtaria == FaixaEtaria.Crianca ? 1.18f : 1.07f);
            }
            Corrente(anim, perfil, escala);

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

        /// <summary>
        /// Pessoa realista do MakeHuman: o modelo já vem vestido (pele, olhos, cabelo, camiseta,
        /// bermuda, tênis). A camiseta vira a camisa do time ("Time_Camisa"), a bermuda e o tênis
        /// pegam a cor da ficha, e o boné/óculos/corrente vão por cima, como no corpo de peças.
        /// </summary>
        private static Animator MontarPessoa(Transform raiz, PerfilPersonagem perfil, Material matTime, BibliotecaCorpos b, BibliotecaCorpos.Pessoa pessoa)
        {
            GameObject corpo = Object.Instantiate(pessoa.modelo, raiz, false);
            corpo.name = "Corpo";
            corpo.transform.localPosition = Vector3.zero;
            corpo.transform.localRotation = Quaternion.Euler(0f, b.giroPessoas, 0f);
            float altura = perfil != null ? perfil.altura : PerfilPersonagem.AlturaReferencia;
            float escala = altura / Mathf.Max(0.5f, pessoa.alturaModelo);
            corpo.transform.localScale = Vector3.one * escala;

            Animator animador = corpo.GetComponent<Animator>();
            if (animador == null) animador = corpo.AddComponent<Animator>();
            animador.runtimeAnimatorController = b.controladorJogador;
            animador.applyRootMotion = false;
            animador.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            Material tecidoTime = matTime != null ? matTime : Mat("time", Color.blue, null, null, 0.2f);
            Color corBermuda = perfil != null ? perfil.corBermuda : new Color(0.16f, 0.18f, 0.24f);
            Color corCalcado = perfil != null ? perfil.corCalcado : Color.white;
            bool comChapeu = perfil != null && perfil.chapeu != Chapeu.Nada;
            foreach (SkinnedMeshRenderer r in corpo.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                r.updateWhenOffscreen = true;
                // As peças vêm com nome em português do gerador (Ferramentas/Blender/gerar_pessoas.py)
                string n = r.name.ToLowerInvariant();
                if (n.StartsWith("camisa"))
                {
                    r.name = "Time_Camisa";
                    r.sharedMaterial = MatTecidoPessoa(r.sharedMaterial, tecidoTime);
                }
                else if (n.StartsWith("bermuda"))
                    r.sharedMaterial = Tingir(r.sharedMaterial, Color.Lerp(Color.white, corBermuda * 1.6f, 0.45f), "bermuda");
                else if (n.StartsWith("tenis"))
                    r.sharedMaterial = Tingir(r.sharedMaterial, Color.Lerp(Color.white, corCalcado, 0.3f), "tenis");
                else if (comChapeu && n.StartsWith("cabelo") && r.sharedMaterial != null && !CabeloPreso(r.sharedMaterial.name))
                    r.gameObject.SetActive(false); // cabelo solto vazaria pelo boné; rabo de cavalo e trança aparecem por trás
            }

            Transform cabeca = animador.isHuman ? animador.GetBoneTransform(HumanBodyBones.Head) : null;
            // Boné e óculos medidos pelos olhos da pessoa (onde fica a testa, para onde ela olha, o tamanho da cabeça)
            if (cabeca != null) ChapeuPelosOlhos(animador, cabeca, perfil, tecidoTime);
            Corrente(animador, perfil, escala);
            return animador;
        }

        private static bool CabeloPreso(string material)
        {
            string m = material.ToLowerInvariant();
            return m.Contains("ponytail") || m.Contains("braid");
        }

        /// <summary>Camisa do time por cima da camiseta do MakeHuman: mantém a textura do tecido e pinta com a cor do time.</summary>
        private static Material MatTecidoPessoa(Material original, Material time)
        {
            Color c = time.HasProperty("_BaseColor") ? time.GetColor("_BaseColor") : time.color;
            return Tingir(original, c, "camisaPessoa");
        }

        /// <summary>Cópia do material com a cor multiplicada (a textura fica, só muda a cor).</summary>
        private static Material Tingir(Material original, Color cor, string tipo)
        {
            string chave = $"{tipo}{(original != null ? original.name : "")}{ColorUtility.ToHtmlStringRGB(cor)}";
            if (materiais.TryGetValue(chave, out Material m) && m != null) return m;
            if (lit == null) lit = Shader.Find("Universal Render Pipeline/Lit");
            m = original != null ? new Material(original) : new Material(lit);
            m.name = "Pessoa_" + chave;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", cor);
            else m.color = cor;
            materiais[chave] = m;
            return m;
        }

        /// <summary>Boné, chapéu, touca e óculos no osso da cabeça (peças feitas por código).</summary>
        private static void ChapeuEOculos(Transform cabeca, PerfilPersonagem perfil, Material tecidoTime, float escala)
        {
                Chapeu chapeu = perfil != null ? perfil.chapeu : Chapeu.Nada;
                if (chapeu == Chapeu.Bone || chapeu == Chapeu.BoneParaTras)
                {
                    float frente = chapeu == Chapeu.Bone ? 1f : -1f;
                    Enfeitar(cabeca, "Time_Bone", new Vector3(0f, 0.145f, 0.01f), new Vector3(0.112f, 0.11f, 0.124f), MalhasBoneco.Calota(-8f, 90f), tecidoTime, escala);
                    Enfeitar(cabeca, "Time_Aba", new Vector3(0f, 0.14f, frente * 0.155f), new Vector3(0.088f, 0.01f, 0.085f), MalhasBoneco.Esfera(), tecidoTime, escala);
                }
                else if (chapeu == Chapeu.Bucket)
                {
                    Enfeitar(cabeca, "Time_Bone", new Vector3(0f, 0.13f, 0f), new Vector3(0.112f, 0.1f, 0.122f), MalhasBoneco.Calota(0f, 90f), tecidoTime, escala);
                    Enfeitar(cabeca, "Time_Aba", new Vector3(0f, 0.13f, 0f), Vector3.one, MalhasBoneco.Aba(0.1f, 0.17f, 0.008f), tecidoTime, escala);
                }
                else if (chapeu == Chapeu.Touca)
                    Enfeitar(cabeca, "Time_Bone", new Vector3(0f, 0.125f, -0.01f), new Vector3(0.112f, 0.115f, 0.124f), MalhasBoneco.Calota(-5f, 90f), tecidoTime, escala);
                if (perfil != null && perfil.acessorio == Acessorio.Oculos)
                    Enfeitar(cabeca, "Oculos", new Vector3(0f, 0.085f, 0.1f), new Vector3(0.085f, 0.02f, 0.012f), MalhasBoneco.Esfera(), Mat("oculos", new Color(0.04f, 0.04f, 0.05f), null, null, 0.9f), escala);
        }

        /// <summary>
        /// Boné, chapéu, touca e óculos nas pessoas do MakeHuman, posicionados pelos olhos: a frente é para
        /// onde os olhos apontam, a altura sai da linha dos olhos e o tamanho da distância entre eles
        /// (criança tem cabeça menor). Fica preso no osso da cabeça (acompanha a animação).
        /// </summary>
        private static void ChapeuPelosOlhos(Animator a, Transform cabeca, PerfilPersonagem perfil, Material tecidoTime)
        {
            Transform oe = a.GetBoneTransform(HumanBodyBones.LeftEye), od = a.GetBoneTransform(HumanBodyBones.RightEye);
            if (oe == null || od == null) return;
            Vector3 olhos = (oe.position + od.position) * 0.5f;
            Vector3 frente = olhos - cabeca.position;
            frente.y = 0f;
            if (frente.sqrMagnitude < 1e-6f) return;
            frente.Normalize();
            Quaternion giro = Quaternion.LookRotation(frente, Vector3.up);
            float k = Vector3.Distance(oe.position, od.position) / 0.06f; // 6 cm entre os olhos = adulto
            Vector3 centro = cabeca.position + Vector3.up * 0.065f * k + frente * 0.012f * k;

            void Peca(string nome, Vector3 deslocamento, Vector3 tamanho, Mesh malha, Material mat)
            {
                GameObject g = new GameObject(nome);
                g.transform.SetPositionAndRotation(centro + giro * (deslocamento * k), giro);
                g.transform.localScale = tamanho * k;
                g.transform.SetParent(cabeca, true);
                g.AddComponent<MeshFilter>().sharedMesh = malha;
                g.AddComponent<MeshRenderer>().sharedMaterial = mat;
            }

            Chapeu chapeu = perfil != null ? perfil.chapeu : Chapeu.Nada;
            if (chapeu == Chapeu.Bone || chapeu == Chapeu.BoneParaTras)
            {
                float lado = chapeu == Chapeu.Bone ? 1f : -1f;
                Peca("Time_Bone", Vector3.zero, new Vector3(0.096f, 0.098f, 0.106f), MalhasBoneco.Calota(-6f, 90f), tecidoTime);
                Peca("Time_Aba", new Vector3(0f, 0.004f, lado * 0.118f), new Vector3(0.082f, 0.008f, 0.07f), MalhasBoneco.Esfera(), tecidoTime);
            }
            else if (chapeu == Chapeu.Bucket)
            {
                Peca("Time_Bone", new Vector3(0f, 0.01f, 0f), new Vector3(0.098f, 0.092f, 0.104f), MalhasBoneco.Calota(0f, 90f), tecidoTime);
                Peca("Time_Aba", new Vector3(0f, 0.01f, 0f), Vector3.one, MalhasBoneco.Aba(0.095f, 0.15f, 0.007f), tecidoTime);
            }
            else if (chapeu == Chapeu.Touca)
                Peca("Time_Bone", new Vector3(0f, -0.005f, -0.005f), new Vector3(0.098f, 0.104f, 0.108f), MalhasBoneco.Calota(-8f, 90f), tecidoTime);
            if (perfil != null && perfil.acessorio == Acessorio.Oculos)
            {
                GameObject g = new GameObject("Oculos");
                g.transform.SetPositionAndRotation(olhos + frente * 0.022f * k, giro);
                g.transform.localScale = new Vector3(0.075f, 0.018f, 0.01f) * k;
                g.transform.SetParent(cabeca, true);
                g.AddComponent<MeshFilter>().sharedMesh = MalhasBoneco.Esfera();
                g.AddComponent<MeshRenderer>().sharedMaterial = Mat("oculos", new Color(0.04f, 0.04f, 0.05f), null, null, 0.9f);
            }
        }

        /// <summary>Corrente de ouro no peito.</summary>
        private static void Corrente(Animator anim, PerfilPersonagem perfil, float escala)
        {
            if (perfil != null && perfil.acessorio == Acessorio.Corrente && anim.isHuman)
            {
                Transform peito = anim.GetBoneTransform(HumanBodyBones.UpperChest) ?? anim.GetBoneTransform(HumanBodyBones.Chest);
                if (peito != null) Enfeitar(peito, "Corrente", new Vector3(0f, 0.12f, 0.06f), Vector3.one, MalhasBoneco.Anel(0.075f, 0.006f), Mat("ouro", new Color(0.95f, 0.75f, 0.25f), null, null, 0.9f), escala);
            }
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
                // Camiseta do MakeHuman: só troca a cor (a textura do tecido e as costuras ficam)
                if (r is SkinnedMeshRenderer && r.sharedMaterial != null && r.sharedMaterial.name.StartsWith("Pessoa_"))
                    r.sharedMaterial = MatTecidoPessoa(r.sharedMaterial, time);
                else r.sharedMaterial = r is SkinnedMeshRenderer ? camisa : time;
            }
        }
    }
}
