using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace BetsDeRua.Editor
{
    /// <summary>
    /// Menu Bets > Gravar partida: abre a cena Prototipo, liga uma partida no modo assistir
    /// (os 4 jogadores bots, com a meta e a dificuldade do menu Ajustes) e grava quadro a quadro
    /// com o tempo fixo de 60 quadros/s (GravadorFrameAFrame). Quando a partida acaba, sai do Play
    /// sozinho e mostra a pasta. As gravações ficam em Gravacoes/ na raiz do repositório
    /// (fora de Game/Assets e fora do Git).
    /// </summary>
    [InitializeOnLoad]
    public static class MenuGravarPartida
    {
        private const string chavePedido = "BetsDeRua.GravarPartida.Cada";
        private const string chaveJogar = "BetsDeRua.GravarPartida.Jogar";
        private const string cena = "Assets/Scenes/Prototipo.unity";
        private static bool configurouSessao;

        static MenuGravarPartida()
        {
            EditorApplication.playModeStateChanged += AoMudarPlay;
        }

        [MenuItem("Bets/Gravar partida/Assistir (bots), todos os quadros")]
        private static void GravarTodos() => Pedir(1, false);

        [MenuItem("Bets/Gravar partida/Assistir (bots), 1 a cada 4 quadros")]
        private static void GravarCada4() => Pedir(4, false);

        [MenuItem("Bets/Gravar partida/Jogando eu (controle ou teclado), todos os quadros")]
        private static void GravarJogando() => Pedir(1, true);

        [MenuItem("Bets/Gravar partida/Parar gravação")]
        private static void PararMenu()
        {
            if (GravadorFrameAFrame.Atual != null) GravadorFrameAFrame.Atual.Parar("parou pelo menu");
        }

        [MenuItem("Bets/Gravar partida/Parar gravação", true)]
        private static bool PodeParar() => GravadorFrameAFrame.Atual != null;

        /// <summary>Também pelo Claude Code: BetsDeRua.Editor.MenuGravarPartida.Pedir(1, false).</summary>
        public static void Pedir(int salvarCada, bool jogando)
        {
            if (EditorApplication.isPlaying)
            {
                // Já está rodando: grava a partida a partir de agora
                Comecar(salvarCada);
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(cena);
            SessionState.SetInt(chavePedido, salvarCada);
            SessionState.SetBool(chaveJogar, jogando);
            EditorApplication.isPlaying = true;
        }

        private static void AoMudarPlay(PlayModeStateChange mudanca)
        {
            if (mudanca == PlayModeStateChange.ExitingEditMode)
            {
                // Antes de a cena acordar: assistir = sessão do menu no modo Assistir (sem pessoas)
                if (SessionState.GetInt(chavePedido, 0) > 0 && !SessionState.GetBool(chaveJogar, false))
                {
                    SessaoDeJogo.Configurar(ModoDeJogo.Assistir, null);
                    configurouSessao = true;
                }
            }
            else if (mudanca == PlayModeStateChange.EnteredPlayMode)
            {
                int cada = SessionState.GetInt(chavePedido, 0);
                if (cada <= 0) return;
                SessionState.EraseInt(chavePedido);
                SessionState.EraseBool(chaveJogar);
                Comecar(cada);
            }
            else if (mudanca == PlayModeStateChange.ExitingPlayMode)
            {
                Time.captureDeltaTime = 0f;
                if (configurouSessao) SessaoDeJogo.Limpar();
                configurouSessao = false;
            }
        }

        private static void Comecar(int salvarCada)
        {
            GravadorFrameAFrame.Terminou -= AoTerminar;
            GravadorFrameAFrame.Terminou += AoTerminar;
            // O jogo precisa andar mesmo com o editor sem foco
            Application.runInBackground = true;
            GravadorFrameAFrame.Iniciar(new GravadorFrameAFrame.Opcoes { salvarCada = salvarCada });
            Debug.Log("[Gravação] Gravando quadro a quadro (60 quadros/s de jogo). Bets > Gravar partida > Parar gravação para parar antes.");
        }

        private static void AoTerminar(string pasta)
        {
            GravadorFrameAFrame.Terminou -= AoTerminar;
            Debug.Log($"[Gravação] Pronto: {pasta}");
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlaying) EditorApplication.isPlaying = false;
                if (!Application.isBatchMode) EditorUtility.RevealInFinder(System.IO.Path.Combine(pasta, "resumo.txt"));
            };
        }
    }
}
