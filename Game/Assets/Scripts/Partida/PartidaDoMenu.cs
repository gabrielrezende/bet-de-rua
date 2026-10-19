using UnityEngine;

namespace BetsDeRua
{
    /// <summary>
    /// Leva para a partida o que foi escolhido no menu: pontos para vencer, dificuldade dos bots,
    /// modo assistir e os personagens de quem joga (cada pessoa com o seu, no time em que entrou).
    /// Roda antes de todo mundo. Com a cena aberta direto no editor (sem menu), não faz nada.
    /// No editor, devolve os números dos configs ao sair (para o Play não mudar os arquivos).
    /// </summary>
    [DefaultExecutionOrder(-300)]
    public class PartidaDoMenu : MonoBehaviour
    {
        [SerializeField] private ConfigPartida configPartida;
        [SerializeField] private GerenciadorJogadores gerenciador;

        private int metaAntes;
        private NivelDificuldade nivel1Antes, nivel2Antes;
        private bool assistirAntes;
        private bool aplicou;

        private void Awake()
        {
            if (!SessaoDeJogo.Configurada) return;
            aplicou = true;
            metaAntes = configPartida.metaPontos;
            nivel1Antes = configPartida.nivelLado1;
            nivel2Antes = configPartida.nivelLado2;
            assistirAntes = configPartida.soAssistir;

            configPartida.metaPontos = SessaoDeJogo.Modo == ModoDeJogo.TreinoArremesso ? 999 : AjustesJogo.MetaPontos;
            configPartida.nivelLado1 = AjustesJogo.NivelBots;
            configPartida.nivelLado2 = AjustesJogo.NivelBots;
            configPartida.soAssistir = SessaoDeJogo.Modo == ModoDeJogo.Assistir;
            AjustesJogo.Aplicar();

            // Personagens: as pessoas do Lado 1 começam no taco, as do Lado 2 na bola.
            // O primeiro jogador livre de cada dupla (na ordem do gerenciador) é quem a pessoa controla.
            int[] usados = new int[2];
            foreach (PessoaNaPartida pessoa in SessaoDeJogo.Pessoas)
            {
                Dupla dupla = pessoa.lado == 0 ? Dupla.Taco : Dupla.Bola;
                int achados = 0;
                foreach (Jogador jogador in gerenciador.Jogadores)
                {
                    if (jogador.Dupla != dupla) continue;
                    if (achados++ < usados[pessoa.lado]) continue;
                    jogador.DefinirPerfil(PersonagensSalvos.Pegar(pessoa.vaga).Clonar());
                    usados[pessoa.lado]++;
                    break;
                }
            }
        }

        private void OnDestroy()
        {
            if (!aplicou || !Application.isEditor) return;
            configPartida.metaPontos = metaAntes;
            configPartida.nivelLado1 = nivel1Antes;
            configPartida.nivelLado2 = nivel2Antes;
            configPartida.soAssistir = assistirAntes;
        }
    }
}
