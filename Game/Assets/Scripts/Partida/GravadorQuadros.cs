using UnityEngine;

namespace BetsDeRua
{
    /// <summary>
    /// Ferramenta de teste: grava uma sequência de quadros (PNG) com o tempo do jogo fixo, para
    /// analisar movimento (mãos no taco, giro, corrida). Opcionalmente prende a câmera atrás/ao
    /// lado de um jogador ("câmera de estudo"). Usada pelo Claude Code via eval; não aparece no jogo.
    /// </summary>
    public class GravadorQuadros : MonoBehaviour
    {
        private string pasta;
        private int restantes;
        private int cada;
        private int contador;
        private int numero;
        private Transform alvo;
        private Vector3 deslocamento;
        private float alturaOlhar;

        /// <summary>Começa a gravar 'quadros' imagens, uma a cada 'cada' quadros, com o jogo a 'fps' fixos.</summary>
        public static GravadorQuadros Iniciar(string pasta, int quadros, int cada = 2, float fps = 30f)
        {
            GameObject g = new GameObject("GravadorQuadros");
            GravadorQuadros gq = g.AddComponent<GravadorQuadros>();
            gq.pasta = pasta;
            gq.restantes = quadros;
            gq.cada = Mathf.Max(1, cada);
            Time.captureDeltaTime = 1f / fps;
            System.IO.Directory.CreateDirectory(pasta);
            return gq;
        }

        /// <summary>Câmera de estudo: fica no 'deslocamento' (no espaço do jogador) olhando para ele.</summary>
        public void Seguir(Transform quem, Vector3 desloc, float olhar = 1.1f)
        {
            alvo = quem;
            deslocamento = desloc;
            alturaOlhar = olhar;
            CameraTransmissao ct = FindFirstObjectByType<CameraTransmissao>();
            if (ct != null) ct.enabled = false;
        }

        private void LateUpdate()
        {
            if (alvo != null && Camera.main != null)
            {
                Camera.main.transform.position = alvo.TransformPoint(deslocamento);
                Camera.main.transform.LookAt(alvo.position + Vector3.up * alturaOlhar);
                Camera.main.fieldOfView = 40f;
            }
            if (restantes <= 0) return;
            if (contador++ % cada != 0) return;
            ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(pasta, $"q{numero++:000}.png"));
            restantes--;
            if (restantes == 0)
            {
                Time.captureDeltaTime = 0f;
                CameraTransmissao ct = FindFirstObjectByType<CameraTransmissao>();
                if (ct != null) ct.enabled = true;
                Destroy(gameObject, 0.5f);
            }
        }
    }
}
