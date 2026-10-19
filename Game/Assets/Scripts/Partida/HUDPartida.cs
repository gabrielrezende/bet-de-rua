using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace BetsDeRua
{
    /// <summary>
    /// A interface da partida, no estilo das transmissões (FIFA, NBA 2K):
    /// - placar no canto (cor de cada time, pontos, quem está no taco, meta);
    /// - mensagem do lance grande no meio (PONTO! em amarelo, maior);
    /// - dicas do momento embaixo (só o que vale agora);
    /// - faixa de "TROCA DE PAPEL";
    /// - pausa (OPTIONS / Esc): continuar, reiniciar, como jogar, sair para o menu;
    /// - fim de partida: placar final, números da partida, jogar de novo ou menu;
    /// - no treino de arremesso: painel com arremessos, acertos, sequência e velocidade.
    /// </summary>
    public class HUDPartida : MonoBehaviour
    {
        /// <summary>True quando o HUD novo está na tela (o desenho antigo em OnGUI fica desligado).</summary>
        public static bool Ativo { get; private set; }
        /// <summary>True com a pausa ou o fim de partida na tela (as etiquetas do jogo somem).</summary>
        public static bool SobreposicaoAberta { get; private set; }

        [SerializeField] private UIDocument documento;
        [SerializeField] private StyleSheet estilo;
        [SerializeField] private ControladorLance lance;
        [SerializeField] private ConfigPartida configPartida;

        private VisualElement raiz;
        private Label pontos1, pontos2, nome1, nome2, info, mensagem, instrucoes;
        private VisualElement cor1, cor2, tacoNo1, tacoNo2;
        private VisualElement faixaTroca;
        private Label textoTroca;
        private VisualElement painelTreino;
        private Label treinoNumeros;
        private Label tituloTreino;
        private VisualElement pausa, fim;
        private readonly Navegacao navPausa = new Navegacao("botao-selecionado");
        private readonly Navegacao navFim = new Navegacao("botao-selecionado");
        private Label placarFinal, tituloFim;
        private VisualElement estatisticasFim;
        private int ultimaMensagem = -1;
        private float tempoMensagem;
        private bool pausado;
        private bool fimMostrado;
        private EstadoLance estadoAntes;

        private void OnEnable()
        {
            Ativo = true;
        }

        private void OnDisable()
        {
            Ativo = false;
            SobreposicaoAberta = false;
            if (pausado) Time.timeScale = 1f;
        }

        private void Start()
        {
            raiz = documento.rootVisualElement;
            raiz.styleSheets.Add(estilo);
            raiz.AddToClassList("raiz");
            raiz.pickingMode = PickingMode.Ignore;
            Montar();
        }

        private void Montar()
        {
            // ---- Placar
            VisualElement placar = UI.Caixa("placar");
            placar.Add(CaixaTime(out cor1, out nome1, out pontos1, out tacoNo1, "LADO 1"));
            placar.Add(CaixaTime(out cor2, out nome2, out pontos2, out tacoNo2, "LADO 2"));
            info = UI.Texto("", "placar-info");
            placar.Add(info);
            raiz.Add(placar);

            mensagem = UI.Texto("", "mensagem-lance");
            raiz.Add(mensagem);
            instrucoes = UI.Texto("", "instrucoes");
            raiz.Add(instrucoes);

            faixaTroca = UI.Caixa("faixa-troca");
            textoTroca = UI.Texto("TROCA DE PAPEL", "titulo");
            textoTroca.style.fontSize = 72;
            textoTroca.style.unityTextAlign = TextAnchor.MiddleCenter;
            faixaTroca.Add(textoTroca);
            faixaTroca.style.display = DisplayStyle.None;
            raiz.Add(faixaTroca);

            // ---- Treino
            painelTreino = UI.Caixa("painel");
            painelTreino.style.position = Position.Absolute;
            painelTreino.style.right = 36;
            painelTreino.style.top = 28;
            painelTreino.style.width = 420;
            tituloTreino = UI.Texto("TREINO DE ARREMESSO", "titulo");
            tituloTreino.style.fontSize = 32;
            painelTreino.Add(tituloTreino);
            treinoNumeros = UI.Texto("", "subtitulo");
            treinoNumeros.style.whiteSpace = WhiteSpace.Normal;
            treinoNumeros.style.fontSize = 28;
            painelTreino.Add(treinoNumeros);
            painelTreino.style.display = DisplayStyle.None;
            raiz.Add(painelTreino);

            MontarPausa();
            MontarFim();
        }

        private VisualElement CaixaTime(out VisualElement cor, out Label nome, out Label pontos, out VisualElement taco, string texto)
        {
            VisualElement time = UI.Caixa("placar-time");
            cor = UI.Caixa("placar-cor");
            time.Add(cor);
            nome = UI.Texto(texto, "placar-nome");
            time.Add(nome);
            taco = UI.Texto("TACO", "selo");
            taco.style.position = Position.Relative;
            taco.style.top = 0;
            taco.style.right = 0;
            taco.style.marginRight = 10;
            taco.style.fontSize = 18;
            time.Add(taco);
            pontos = UI.Texto("0", "placar-pontos");
            time.Add(pontos);
            return time;
        }

        private void MontarPausa()
        {
            pausa = UI.Caixa("sobreposicao");
            VisualElement caixa = UI.Caixa("menu-vertical");
            Label titulo = UI.Texto("PAUSA", "titulo-logo");
            titulo.style.fontSize = 110;
            titulo.style.marginBottom = 24;
            caixa.Add(titulo);
            BotaoMenu(caixa, navPausa, "CONTINUAR", Continuar);
            BotaoMenu(caixa, navPausa, "REINICIAR PARTIDA", () => { Continuar(); lance.ReiniciarPartida(); fimMostrado = false; });
            BotaoMenu(caixa, navPausa, "SAIR PARA O MENU", IrParaOMenu);
            Label dicas = UI.Texto("", "texto-pequeno");
            dicas.name = "dicasPausa";
            dicas.style.whiteSpace = WhiteSpace.Normal;
            dicas.style.marginTop = 24;
            caixa.Add(dicas);
            pausa.Add(caixa);
            pausa.style.display = DisplayStyle.None;
            raiz.Add(pausa);
        }

        private void MontarFim()
        {
            fim = UI.Caixa("sobreposicao");
            VisualElement caixa = new VisualElement();
            caixa.style.alignItems = Align.Center;
            tituloFim = UI.Texto("", "titulo-logo");
            tituloFim.style.fontSize = 96;
            tituloFim.style.unityTextAlign = TextAnchor.MiddleCenter;
            caixa.Add(tituloFim);
            placarFinal = UI.Texto("", "placar-final");
            caixa.Add(placarFinal);
            estatisticasFim = new VisualElement();
            estatisticasFim.style.marginBottom = 30;
            caixa.Add(estatisticasFim);
            VisualElement botoes = UI.Caixa("menu-vertical");
            BotaoMenu(botoes, navFim, "JOGAR DE NOVO", () => { fim.style.display = DisplayStyle.None; lance.ReiniciarPartida(); fimMostrado = false; });
            BotaoMenu(botoes, navFim, "MENU PRINCIPAL", IrParaOMenu);
            caixa.Add(botoes);
            fim.Add(caixa);
            fim.style.display = DisplayStyle.None;
            raiz.Add(fim);
        }

        private static void BotaoMenu(VisualElement pai, Navegacao nav, string texto, System.Action acao)
        {
            Label b = UI.Texto(texto, "botao-menu");
            pai.Add(b);
            int indice = nav.Quantidade;
            nav.Adicionar(b);
            nav.AoConfirmar += i => { if (i == indice) acao(); };
        }

        // ------------------------------------------------------------------ quadro a quadro

        private void Update()
        {
            if (lance == null || raiz == null) return;
            LerPausa();
            if (pausado) { SobreposicaoAberta = true; NavegarMenu(navPausa); return; }

            // Placar
            int noTaco = lance.LadoNoTaco;
            pontos1.text = lance.Pontos(0).ToString();
            pontos2.text = lance.Pontos(1).ToString();
            cor1.style.backgroundColor = configPartida.corDuplaTaco;
            cor2.style.backgroundColor = configPartida.corDuplaBola;
            nome1.text = NomeDoLado(0);
            nome2.text = NomeDoLado(1);
            tacoNo1.style.display = noTaco == 0 ? DisplayStyle.Flex : DisplayStyle.None;
            tacoNo2.style.display = noTaco == 1 ? DisplayStyle.Flex : DisplayStyle.None;
            string extra = lance.EmMatchPoint(0) || lance.EmMatchPoint(1) ? "MATCH POINT" : $"ATÉ {lance.Meta}";
            if (lance.Lanchas > 0) extra += $" · LANCHA {lance.Lanchas}";
            if (lance.FaltasCorpo > 0) extra += $" · CORPO {lance.FaltasCorpo}";
            info.text = extra;

            // Mensagem do lance (entra grande e assenta)
            if (lance.NumeroDaMensagem != ultimaMensagem)
            {
                ultimaMensagem = lance.NumeroDaMensagem;
                tempoMensagem = 0f;
                mensagem.text = lance.Mensagem;
                bool ponto = lance.Mensagem.Contains("PONTO") || lance.Mensagem.Contains("MATCH POINT") || lance.Mensagem.Contains("DERRUBOU")
                             || lance.Mensagem.Contains("QUEIMOU") || lance.Mensagem.Contains("FECHOU");
                mensagem.EnableInClassList("mensagem-ponto", ponto);
            }
            tempoMensagem += Time.unscaledDeltaTime;
            mensagem.style.display = lance.MensagemVisivel ? DisplayStyle.Flex : DisplayStyle.None;
            float entrada = Mathf.Clamp01(tempoMensagem / 0.18f);
            mensagem.style.scale = new Scale(Vector3.one * Mathf.Lerp(1.35f, 1f, entrada));
            mensagem.style.opacity = entrada;

            string dica = lance.TextoDasInstrucoes();
            instrucoes.text = dica;
            instrucoes.style.display = string.IsNullOrEmpty(dica) ? DisplayStyle.None : DisplayStyle.Flex;

            // Troca de papel
            bool troca = lance.Estado == EstadoLance.TrocaDePapel;
            faixaTroca.style.display = troca ? DisplayStyle.Flex : DisplayStyle.None;
            if (troca) textoTroca.text = $"TROCA DE PAPEL · {NomeDoLado(1 - noTaco)} VAI PARA O TACO";

            // Treino
            painelTreino.style.display = lance.Treino || lance.TreinoBatida ? DisplayStyle.Flex : DisplayStyle.None;
            if (lance.TreinoBatida)
            {
                ControladorLance.EstatisticasTreino e = lance.EstatisticasDoTreino;
                int aproveitamento = e.arremessos > 0 ? Mathf.RoundToInt(100f * e.rebatidas / e.arremessos) : 0;
                tituloTreino.text = "TREINO DE BATIDA";
                treinoNumeros.text = $"Arremessos: {e.arremessos}\nRebatidas: {e.rebatidas}  ({aproveitamento}%)\n" +
                                     $"Perfeitas: {e.perfeitas}   Erros: {e.erros}\nSeguidas: {e.sequencia}   (recorde {e.melhorSequencia})\n" +
                                     $"Mais forte: {e.maiorBatidaKmh:0} km/h   Queimadas: {e.queimadas}";
                cor1.parent.parent.style.display = DisplayStyle.None;
            }
            else if (lance.Treino)
            {
                ControladorLance.EstatisticasTreino e = lance.EstatisticasDoTreino;
                treinoNumeros.text = $"Arremessos: {e.arremessos}\nDerrubadas: {e.acertos}  ({e.Aproveitamento}%)\n" +
                                     $"Seguidas: {e.sequencia}   (recorde {e.melhorSequencia})\nMais rápido: {e.maiorKmh:0} km/h\n" +
                                     $"Último: {e.ultimoTipo}";
                cor1.parent.parent.style.display = DisplayStyle.None;
            }

            // Fim da partida
            if (lance.Estado == EstadoLance.FimDePartida && !fimMostrado && !configPartida.reiniciarSozinhoNoFim)
            {
                fimMostrado = true;
                MostrarFim();
            }
            SobreposicaoAberta = pausado || fim.resolvedStyle.display == DisplayStyle.Flex;
            if (fim.resolvedStyle.display == DisplayStyle.Flex) NavegarMenu(navFim);
            estadoAntes = lance.Estado;
        }

        private string NomeDoLado(int lado)
        {
            if (!SessaoDeJogo.Configurada || SessaoDeJogo.Pessoas.Count == 0) return lado == 0 ? "LADO 1" : "LADO 2";
            var nomes = new List<string>();
            foreach (PessoaNaPartida p in SessaoDeJogo.Pessoas)
                if (p.lado == lado) nomes.Add($"P{p.numero}");
            return nomes.Count == 0 ? "CPU" : string.Join(" + ", nomes);
        }

        private void MostrarFim()
        {
            int v = lance.Vencedor;
            tituloFim.text = v < 0 ? "FIM DE JOGO" : $"{NomeDoLado(v)} VENCEU!";
            placarFinal.text = $"{lance.Pontos(0)}  ×  {lance.Pontos(1)}";
            estatisticasFim.Clear();
            Estatistica("Duração", $"{Mathf.FloorToInt(lance.DuracaoDaPartida / 60f)} min {Mathf.FloorToInt(lance.DuracaoDaPartida % 60f):00} s");
            Estatistica("Trocas de papel", lance.TrocasNaPartida.ToString());
            fim.style.display = DisplayStyle.Flex;
            navFim.Selecionar(0);
        }

        private void Estatistica(string nome, string valor)
        {
            VisualElement linha = UI.Caixa("estatistica");
            linha.Add(UI.Texto(nome, "subtitulo"));
            linha.Add(UI.Texto(valor, "cartao-nome"));
            estatisticasFim.Add(linha);
        }

        private void LerPausa()
        {
            bool apertou = false;
            foreach (Gamepad g in Gamepad.all) if (g.startButton.wasPressedThisFrame) apertou = true;
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) apertou = true;
            if (!apertou || fim.resolvedStyle.display == DisplayStyle.Flex) return;
            if (pausado) Continuar();
            else Pausar();
        }

        private void Pausar()
        {
            pausado = true;
            Time.timeScale = 0f;
            pausa.style.display = DisplayStyle.Flex;
            navPausa.Selecionar(0);
            Label dicas = pausa.Q<Label>("dicasPausa");
            if (dicas != null) dicas.text = lance.TreinoBatida
                ? "Treino: leia a altura da bola. No ar, R2 (taco alto); baixa, L2 (taco baixo). O X amarelo mostra onde ela passa."
                : lance.Treino
                ? "Treino: derrube a garrafa do outro lado. Varie o tipo (R2, L2, R1) e use a curva depois de soltar."
                : "Regra rápida: cada cruzada no meio vale 1 ponto. Derrubar a garrafa com o taco fora do chão queima a dupla do taco.";
        }

        private void Continuar()
        {
            pausado = false;
            Time.timeScale = 1f;
            pausa.style.display = DisplayStyle.None;
        }

        private void IrParaOMenu()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SessaoDeJogo.CenaMenu);
        }

        private void NavegarMenu(Navegacao nav)
        {
            foreach (InputDevice d in ControlesMenu.Dispositivos())
            {
                Vector2Int dir = ControlesMenu.Direcao(d);
                if (dir.y != 0) nav.Mover(dir, true);
                if (ControlesMenu.Confirmar(d)) { nav.Confirmar(); return; }
                if (ControlesMenu.Voltar(d) && nav == navPausa) { Continuar(); return; }
            }
        }
    }
}
