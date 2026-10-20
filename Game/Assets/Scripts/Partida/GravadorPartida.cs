using System.IO;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace BetsDeRua
{
    /// <summary>
    /// Ferramenta de teste (não aparece no jogo): grava a partida inteira para revisar quadro a quadro.
    /// - A cada 'intervalo' segundos de jogo, um quadro da tela (câmera da transmissão + placar e mensagens).
    /// - A cada 'intervaloAngulos' segundos, um quadro de outro ângulo (câmera extra, sem placar), girando
    ///   entre: rente ao chão ao lado do rebatedor, atrás de quem arremessa, de cima (drone) e seguindo a bola.
    /// - Um registro (cortes.txt) de todo salto de câmera (corte), com o modo antes e depois, e da bola sumida da tela.
    /// O nome do arquivo tem o tempo, o estado do lance e o modo da câmera, para achar o momento depois.
    /// Ligado pelo Claude Code via eval: GravadorPartida.Iniciar(pasta, 1f, 3f, 960, segundosMaximos).
    /// </summary>
    public class GravadorPartida : MonoBehaviour
    {
        private string pasta;
        private float intervalo, intervaloAngulos, ate;
        private int largura;
        private float proximo, proximoAngulo;
        private int angulo;
        private Camera extra;
        private RenderTexture rtExtra;
        private ControladorLance lance;
        private CameraTransmissao transmissao;
        private FieldInfo campoModo;
        private Vector3 posAnterior;
        private int modoAnterior = -1;
        private StringBuilder cortes = new StringBuilder();
        private int quadros;

        public static GravadorPartida Iniciar(string pasta, float intervalo, float intervaloAngulos, int largura, float segundosMaximos)
        {
            GameObject g = new GameObject("GravadorPartida");
            DontDestroyOnLoad(g);
            GravadorPartida gp = g.AddComponent<GravadorPartida>();
            gp.pasta = pasta;
            gp.intervalo = intervalo;
            gp.intervaloAngulos = intervaloAngulos;
            gp.largura = largura;
            gp.ate = Time.time + segundosMaximos;
            Directory.CreateDirectory(Path.Combine(pasta, "transmissao"));
            Directory.CreateDirectory(Path.Combine(pasta, "angulos"));
            gp.proximo = Time.time;
            gp.proximoAngulo = Time.time + 0.5f;
            return gp;
        }

        public int Quadros => quadros;

        private void Achar()
        {
            if (lance == null) lance = FindFirstObjectByType<ControladorLance>();
            if (transmissao == null)
            {
                transmissao = FindFirstObjectByType<CameraTransmissao>();
                campoModo = typeof(CameraTransmissao).GetField("modoAtual", BindingFlags.NonPublic | BindingFlags.Instance);
            }
            if (extra == null && Camera.main != null)
            {
                GameObject c = new GameObject("CameraEstudo");
                extra = c.AddComponent<Camera>();
                extra.CopyFrom(Camera.main);
                extra.enabled = false;
                extra.fieldOfView = 45f;
                rtExtra = new RenderTexture(largura, largura * 9 / 16, 24);
            }
        }

        private int Modo => transmissao != null && campoModo != null ? (int)campoModo.GetValue(transmissao) : -1;

        private string Rotulo()
        {
            string estado = lance != null ? lance.Estado.ToString() : "?";
            return $"{Time.time:0000.0}_{estado}_cam{Modo}";
        }

        private void LateUpdate()
        {
            Achar();
            Camera cam = Camera.main;
            if (cam == null || lance == null) return;

            // Cortes de câmera: salto grande de posição num quadro só, ou troca de modo
            Vector3 p = cam.transform.position;
            int modo = Modo;
            float salto = Vector3.Distance(p, posAnterior);
            if (modoAnterior >= 0 && (modo != modoAnterior || salto > 2.5f))
                cortes.AppendLine($"{Time.time:0000.00}\tcorte\tmodo {modoAnterior}->{modo}\tsalto {salto:0.0} m\testado {lance.Estado}");
            // Bola fora da tela com o lance vivo (a transmissão perdeu a jogada)
            Bola bola = NiveisBotsBola();
            if (bola != null && lance.Estado == EstadoLance.BolaViva && Time.frameCount % 15 == 0)
            {
                Vector3 v = cam.WorldToViewportPoint(bola.transform.position);
                if (v.z < 0f || v.x < -0.02f || v.x > 1.02f || v.y < -0.02f || v.y > 1.02f)
                    cortes.AppendLine($"{Time.time:0000.00}\tbola_fora_da_tela\tmodo {modo}\tviewport {v.x:0.00},{v.y:0.00}");
            }
            posAnterior = p;
            modoAnterior = modo;

            if (Time.time > ate) { Encerrar(); return; }
            if (Time.time >= proximo)
            {
                proximo += intervalo;
                StartCoroutine(GravarTela(Rotulo()));
            }
            if (Time.time >= proximoAngulo)
            {
                proximoAngulo += intervaloAngulos;
                GravarAngulo(Rotulo());
            }
        }

        private Bola bolaCache;
        private Bola NiveisBotsBola()
        {
            if (bolaCache == null) bolaCache = FindFirstObjectByType<Bola>();
            return bolaCache;
        }

        private System.Collections.IEnumerator GravarTela(string rotulo)
        {
            yield return new WaitForEndOfFrame();
            Texture2D t = ScreenCapture.CaptureScreenshotAsTexture();
            Salvar(t, Path.Combine(pasta, "transmissao", rotulo + ".jpg"));
        }

        private void GravarAngulo(string rotulo)
        {
            Bola bola = NiveisBotsBola();
            Transform reb = lance.RebatedorDaVez != null ? lance.RebatedorDaVez.transform : null;
            Transform arr = lance.ArremessadorDaVez != null ? lance.ArremessadorDaVez.transform : null;
            if (reb == null || arr == null || bola == null) return;
            string nome;
            switch (angulo++ % 4)
            {
                case 0: // rente ao chão, ao lado do rebatedor (como a câmera de beisebol de lado)
                    extra.transform.position = reb.position + reb.right * 3.2f + Vector3.up * 0.7f;
                    extra.transform.LookAt(reb.position + Vector3.up * 0.9f);
                    nome = "lado_rebatedor"; break;
                case 1: // atrás de quem arremessa (MLB)
                    Vector3 rumo = (reb.position - arr.position); rumo.y = 0f; rumo.Normalize();
                    extra.transform.position = arr.position - rumo * 3.5f + Vector3.up * 1.8f;
                    extra.transform.LookAt(reb.position + Vector3.up * 0.8f);
                    nome = "atras_arremesso"; break;
                case 2: // drone de cima, campo inteiro
                    extra.transform.position = new Vector3(bola.transform.position.x * 0.5f, 26f, -8f);
                    extra.transform.LookAt(new Vector3(bola.transform.position.x * 0.5f, 0f, 1f));
                    nome = "drone"; break;
                default: // seguindo a bola, de perto
                    extra.transform.position = bola.transform.position + new Vector3(-2.5f, 1.4f, -3.5f);
                    extra.transform.LookAt(bola.transform.position);
                    nome = "bola"; break;
            }
            extra.targetTexture = rtExtra;
            extra.Render();
            extra.targetTexture = null;
            RenderTexture antes = RenderTexture.active;
            RenderTexture.active = rtExtra;
            Texture2D t = new Texture2D(rtExtra.width, rtExtra.height, TextureFormat.RGB24, false);
            t.ReadPixels(new Rect(0, 0, rtExtra.width, rtExtra.height), 0, 0);
            t.Apply();
            RenderTexture.active = antes;
            Salvar(t, Path.Combine(pasta, "angulos", rotulo + "_" + nome + ".jpg"), false);
        }

        private void Salvar(Texture2D t, string caminho, bool reduzir = true)
        {
            Texture2D final = t;
            if (reduzir && t.width > largura)
            {
                int altura = t.height * largura / t.width;
                RenderTexture rt = RenderTexture.GetTemporary(largura, altura);
                Graphics.Blit(t, rt);
                RenderTexture antes = RenderTexture.active;
                RenderTexture.active = rt;
                final = new Texture2D(largura, altura, TextureFormat.RGB24, false);
                final.ReadPixels(new Rect(0, 0, largura, altura), 0, 0);
                final.Apply();
                RenderTexture.active = antes;
                RenderTexture.ReleaseTemporary(rt);
                Destroy(t);
            }
            File.WriteAllBytes(caminho, final.EncodeToJPG(82));
            Destroy(final);
            quadros++;
        }

        private void Encerrar()
        {
            File.WriteAllText(Path.Combine(pasta, "cortes.txt"), cortes.ToString());
            if (rtExtra != null) rtExtra.Release();
            Destroy(gameObject);
        }

        private void OnApplicationQuit()
        {
            if (pasta != null) File.WriteAllText(Path.Combine(pasta, "cortes.txt"), cortes.ToString());
        }

        private void OnDestroy()
        {
            if (pasta != null && cortes.Length > 0) File.WriteAllText(Path.Combine(pasta, "cortes.txt"), cortes.ToString());
        }
    }
}
