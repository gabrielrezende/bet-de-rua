using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace BetsDeRua
{
    /// <summary>
    /// Escolha de lado, como no FIFA: cada controle conectado aparece no meio (fora do jogo);
    /// esquerda/direita leva direto para o LADO 1 (começa no taco) ou o LADO 2 (começa na bola);
    /// para baixo volta para FORA. ✕ com ninguém nos lados leva quem apertou para o LADO 1.
    /// No máximo 2 por lado; vaga vazia é bot. Juntos no mesmo lado = parceiros contra a CPU;
    /// em lados diferentes = um contra o outro. △ muda a dificuldade dos bots, □ os pontos.
    /// ✕ (ou OPTIONS) começa.
    /// </summary>
    public class TelaLados : Tela
    {
        private class Ficha
        {
            public InputDevice dispositivo;
            public int numero;
            public int posicao; // -1 = Lado 1, 0 = fora, 1 = Lado 2
        }

        private readonly ModoDeJogo modo;
        private readonly List<Ficha> fichas = new List<Ficha>();
        private VisualElement colunaLado1, colunaFora, colunaLado2;
        private Label infoAjustes;
        private Label aviso;

        public static readonly Color[] CoresJogador =
        {
            new Color(0.25f, 0.55f, 1f), new Color(1f, 0.3f, 0.3f), new Color(0.3f, 0.85f, 0.4f), new Color(1f, 0.82f, 0.15f)
        };

        public TelaLados(ModoDeJogo modo) { this.modo = modo; }

        public override bool MostrarVitrine => false;

        protected override void Montar()
        {
            Raiz.Add(menu.BarraTopo(modo == ModoDeJogo.MultiplayerLocal ? "MULTIPLAYER LOCAL · ESCOLHA O LADO" : "PARTIDA RÁPIDA · ESCOLHA O LADO"));

            VisualElement colunas = UI.Caixa("colunas-lado");
            colunaLado1 = Coluna("LADO 1", "começa no TACO", new Color(0.95f, 0.5f, 0.12f));
            colunaFora = Coluna("FORA", "esse controle não joga", new Color(0.47f, 0.49f, 0.53f));
            colunaLado2 = Coluna("LADO 2", "começa na BOLA", new Color(0.15f, 0.4f, 0.95f));
            colunas.Add(colunaLado1);
            colunas.Add(colunaFora);
            colunas.Add(colunaLado2);
            Raiz.Add(colunas);

            infoAjustes = UI.Texto("", "subtitulo");
            infoAjustes.style.position = Position.Absolute;
            infoAjustes.style.bottom = 84;
            infoAjustes.style.left = 0;
            infoAjustes.style.right = 0;
            infoAjustes.style.unityTextAlign = TextAnchor.MiddleCenter;
            Raiz.Add(infoAjustes);
            aviso = UI.Texto("", "subtitulo");
            aviso.style.position = Position.Absolute;
            aviso.style.top = 116;
            aviso.style.left = 0;
            aviso.style.right = 0;
            aviso.style.unityTextAlign = TextAnchor.MiddleCenter;
            aviso.style.color = new Color(1f, 0.84f, 0.12f);
            Raiz.Add(aviso);

            Raiz.Add(menu.Rodape(UI.Dica(IconeBotao.Qual.Confirmar, "Começar"),
                                 UI.DicaTexto(() => "◀ ▶", "Mudar de lado"),
                                 UI.DicaTexto(() => "▼", "Ficar fora"),
                                 UI.Dica(IconeBotao.Qual.Triangulo, "Dificuldade"),
                                 UI.Dica(IconeBotao.Qual.Quadrado, "Pontos"),
                                 UI.Dica(IconeBotao.Qual.Voltar, "Voltar")));
        }

        private VisualElement Coluna(string titulo, string sub, Color cor)
        {
            VisualElement c = UI.Caixa("coluna-lado");
            c.style.borderTopColor = cor;
            Label t = UI.Texto(titulo, "titulo");
            t.style.unityTextAlign = TextAnchor.MiddleCenter;
            c.Add(t);
            c.Add(UI.Texto(sub, "subtitulo"));
            VisualElement lista = new VisualElement { name = "lista" };
            lista.style.width = Length.Percent(100);
            lista.style.alignItems = Align.Center;
            c.Add(lista);
            return c;
        }

        public override void AoAbrir()
        {
            fichas.Clear();
            AtualizarDispositivos();
            Desenhar();
        }

        private void AtualizarDispositivos()
        {
            var presentes = new List<InputDevice>(ControlesMenu.Dispositivos());
            fichas.RemoveAll(f => !presentes.Contains(f.dispositivo));
            // O P1 é quem apertou START na abertura; os outros entram na ordem em que aparecem
            InputDevice principal = SessaoDeJogo.DispositivoPrincipal;
            if (principal == null || !presentes.Contains(principal)) principal = presentes.Count > 0 ? presentes[0] : null;
            presentes.Remove(principal);
            if (principal != null) presentes.Insert(0, principal);
            foreach (InputDevice d in presentes)
            {
                if (fichas.Exists(f => f.dispositivo == d)) continue;
                // Teclado só aparece se não tiver controle (ou se ele for o P1)
                if (d is Keyboard && d != principal && Gamepad.all.Count > 0) continue;
                if (fichas.Count >= 4) break;
                fichas.Add(new Ficha { dispositivo = d, numero = ProximoNumero(), posicao = d == principal ? -1 : 0 });
            }
        }

        private int ProximoNumero()
        {
            for (int n = 1; ; n++)
                if (!fichas.Exists(f => f.numero == n)) return n;
        }

        private void Desenhar()
        {
            foreach (VisualElement col in new[] { colunaLado1, colunaFora, colunaLado2 }) col.Q("lista").Clear();
            foreach (Ficha f in fichas)
            {
                VisualElement col = f.posicao < 0 ? colunaLado1 : f.posicao > 0 ? colunaLado2 : colunaFora;
                col.Q("lista").Add(FichaVisual(f));
            }
            // Vagas de bot nos lados
            foreach (int lado in new[] { -1, 1 })
            {
                int pessoas = fichas.FindAll(f => f.posicao == lado).Count;
                VisualElement col = lado < 0 ? colunaLado1 : colunaLado2;
                for (int i = pessoas; i < 2; i++)
                {
                    VisualElement vazia = UI.Caixa("ficha-pessoa", "ficha-vazia");
                    Label n = UI.Texto("BOT", "ficha-numero");
                    n.style.backgroundColor = new Color(0.5f, 0.52f, 0.56f);
                    n.style.fontSize = 28;
                    vazia.Add(n);
                    vazia.Add(UI.Texto(NomeNivel(AjustesJogo.NivelBots), "subtitulo"));
                    col.Q("lista").Add(vazia);
                }
            }
            infoAjustes.text = $"Bots: {NomeNivel(AjustesJogo.NivelBots).ToUpperInvariant()}   ·   Partida até {AjustesJogo.MetaPontos} pontos (+ cruzada final)";
            bool lado1 = fichas.Exists(f => f.posicao < 0), lado2 = fichas.Exists(f => f.posicao > 0);
            aviso.text = !lado1 && !lado2 ? "Leve pelo menos um controle para um dos lados"
                : lado1 && lado2 ? "Um contra o outro: Lado 1 x Lado 2"
                : "Juntos contra a CPU";
        }

        private VisualElement FichaVisual(Ficha f)
        {
            VisualElement v = UI.Caixa("ficha-pessoa");
            Label n = UI.Texto($"P{f.numero}", "ficha-numero");
            n.style.backgroundColor = CoresJogador[(f.numero - 1) % 4];
            v.Add(n);
            VisualElement textos = new VisualElement();
            PerfilPersonagem p = PersonagensSalvos.Pegar(f.numero - 1);
            Label nome = UI.Texto(p.nome.ToUpperInvariant(), "cartao-nome");
            textos.Add(nome);
            textos.Add(UI.Texto($"{ControlesMenu.NomeDispositivo(f.dispositivo)} · nota {p.NotaGeral}", "texto-pequeno"));
            v.Add(textos);
            return v;
        }

        public static string NomeNivel(NivelDificuldade n) => n == NivelDificuldade.Facil ? "Fácil" : n == NivelDificuldade.Dificil ? "Difícil" : "Médio";

        public override void Atualizar(List<EntradaMenu> todas, EntradaMenu juntas)
        {
            int antes = fichas.Count;
            AtualizarDispositivos();
            bool mudou = fichas.Count != antes;
            foreach (EntradaMenu e in todas)
            {
                Ficha f = fichas.Find(x => x.dispositivo == e.dispositivo);
                if (f == null) continue;
                if (e.direcao.x != 0 || e.direcao.y < 0)
                {
                    // De um lado vai direto para o outro (sem parar no FORA); para baixo sai do lado
                    int nova = e.direcao.y < 0 && e.direcao.x == 0 ? 0 : e.direcao.x < 0 ? -1 : 1;
                    if (nova != 0 && fichas.FindAll(x => x.posicao == nova).Count >= 2) nova = f.posicao; // lado cheio
                    if (nova != f.posicao) { f.posicao = nova; mudou = true; Sons.Tocar(TipoSom.MenuMover, 0.6f); }
                }
                if (e.triangulo)
                {
                    AjustesJogo.NivelBots = (NivelDificuldade)(((int)AjustesJogo.NivelBots + 1) % 3);
                    mudou = true;
                }
                if (e.quadrado)
                {
                    AjustesJogo.MetaPontos = AjustesJogo.MetaPontos == 10 ? 5 : AjustesJogo.MetaPontos == 5 ? 3 : 10;
                    mudou = true;
                }
                if (e.voltar) { menu.Voltar(); return; }
                if (e.confirmar || e.start) { Comecar(f); return; }
            }
            if (mudou) Desenhar();
        }

        private void Comecar(Ficha quemApertou)
        {
            // Ninguém escolheu lado: quem apertou ✕ joga no LADO 1 (não fica parado sem resposta)
            if (!fichas.Exists(x => x.posicao != 0)) quemApertou.posicao = -1;
            var pessoas = new List<PessoaNaPartida>();
            foreach (Ficha f in fichas)
            {
                if (f.posicao == 0) continue;
                pessoas.Add(new PessoaNaPartida { dispositivo = f.dispositivo, numero = f.numero, lado = f.posicao < 0 ? 0 : 1, vaga = f.numero - 1 });
            }
            if (pessoas.Count == 0) return;
            menu.ComecarPartida(modo, pessoas);
        }
    }
}
