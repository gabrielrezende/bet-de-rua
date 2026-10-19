using System.IO;
using UnityEditor;
using UnityEngine;

namespace BetsDeRua.Editor
{
    /// <summary>
    /// Ferramenta de teste: monta bonecos num "estúdio" longe do campo e salva uma foto em PNG,
    /// sem precisar dar Play. Usada para avaliar o visual do personagem e do taco.
    /// </summary>
    public static class FotoBoneco
    {
        /// <summary>
        /// Fotografa os 4 personagens dos bots lado a lado (de frente, levemente de lado).
        /// 'pose' = 0 parado, 1 segurando o taco.
        /// </summary>
        public static string Foto(string caminhoPng, float giroCamera = 20f, float distancia = 4.2f, float alturaCamera = 1.1f,
                                  int largura = 1600, int altura = 900, bool comTaco = true)
        {
            Vector3 origem = new Vector3(600f, 0f, 600f);
            GameObject estudio = new GameObject("EstudioFoto");
            estudio.transform.position = origem;
            try
            {
                Material time1 = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materiais/DuplaTaco.mat");
                Material time2 = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materiais/DuplaBola.mat");
                ConfigFisica config = AssetDatabase.LoadAssetAtPath<ConfigFisica>("Assets/Config/ConfigFisica.asset");
                for (int i = 1; i <= 4; i++)
                {
                    PerfilPersonagem perfil = AssetDatabase.LoadAssetAtPath<PerfilPersonagem>($"Assets/Config/Personagens/Personagem{i}.asset");
                    GameObject boneco = new GameObject($"Boneco{i}");
                    boneco.transform.SetParent(estudio.transform, false);
                    boneco.transform.localPosition = new Vector3((i - 2.5f) * 1.0f, 0f, 0f);
                    ConstrutorBoneco.Montar(boneco.transform, perfil, i <= 2 ? time1 : time2);
                    if (comTaco && i <= 2)
                    {
                        Transform pivo = new GameObject("MaoTaco").transform;
                        pivo.SetParent(boneco.transform, false);
                        float escala = perfil.altura / PerfilPersonagem.AlturaReferencia;
                        pivo.localPosition = new Vector3(config.raioJogador * 0.9f, config.alturaMaoTaco * escala, config.raioJogador * 0.6f);
                        float cosseno = Mathf.Clamp(pivo.localPosition.y / config.comprimentoTaco, -1f, 1f);
                        pivo.localRotation = Quaternion.Euler(-Mathf.Acos(cosseno) * Mathf.Rad2Deg, 0f, 0f);
                        ConstrutorBoneco.MontarTaco(pivo, config.comprimentoTaco, perfil.corFitaTaco);
                    }
                }
                // Chão e luzes
                GameObject chao = GameObject.CreatePrimitive(PrimitiveType.Plane);
                chao.transform.SetParent(estudio.transform, false);
                chao.transform.localScale = Vector3.one * 2f;
                chao.GetComponent<MeshRenderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materiais/Chao.mat");

                Camera camera = new GameObject("CameraFoto").AddComponent<Camera>();
                camera.transform.SetParent(estudio.transform, false);
                Quaternion giro = Quaternion.Euler(0f, 180f + giroCamera, 0f);
                camera.transform.localPosition = giro * new Vector3(0f, 0f, -distancia) + Vector3.up * alturaCamera;
                camera.transform.LookAt(origem + Vector3.up * 0.9f);
                camera.fieldOfView = 40f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(0.55f, 0.62f, 0.7f);

                RenderTexture rt = new RenderTexture(largura, altura, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
                camera.targetTexture = rt;
                camera.Render();
                RenderTexture.active = rt;
                Texture2D tex = new Texture2D(largura, altura, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, largura, altura), 0, 0);
                tex.Apply();
                RenderTexture.active = null;
                File.WriteAllBytes(caminhoPng, tex.EncodeToPNG());
                camera.targetTexture = null;
                Object.DestroyImmediate(rt);
                Object.DestroyImmediate(tex);
                return caminhoPng;
            }
            finally
            {
                Object.DestroyImmediate(estudio);
            }
        }
    }
}
