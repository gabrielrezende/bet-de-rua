using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace BetsDeRua
{
    /// <summary>
    /// Online: GRUPO (você + 3 vagas para convidar e o código do grupo), AMIGOS (lista e convites)
    /// e ENTRAR COM CÓDIGO. As telas estão prontas; a conexão de verdade (lobby, convites, lista
    /// de amigos) entra com a Steam (Steamworks) quando o jogo tiver a página dele lá.
    /// </summary>
    public class TelaOnline : Tela
    {
        private readonly string[] abas = { "GRUPO", "AMIGOS", "ENTRAR COM CÓDIGO" };
        private readonly List<Label> rotulos = new List<Label>();
        private int aba;
        private VisualElement conteudo;
        private readonly Navegacao nav = new Navegacao("botao-selecionado");
        private readonly List<Action> acoes = new List<Action>();
        private Label status;
        private TextField campoCodigo;
        private static string codigoDoGrupo;

        public TelaOnline(int abaInicial) { aba = abaInicial; }

        protected override void Montar()
        {
            if (string.IsNullOrEmpty(codigoDoGrupo))
            {
                const string letras = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
                char[] c = new char[4];
                for (int i = 0; i < 4; i++) c[i] = letras[UnityEngine.Random.Range(0, letras.Length)];
                codigoDoGrupo = "RUA-" + new string(c);
            }
            VisualElement linhaAbas = UI.Caixa("abas");
            linhaAbas.Add(UI.DicaTexto(() => ControlesMenu.NomeAbaEsquerda, ""));
            for (int i = 0; i < abas.Length; i++)
            {
                int indice = i;
                Label r = UI.Texto(abas[i], "aba");
                r.pickingMode = PickingMode.Position;
                r.RegisterCallback<ClickEvent>(_ => MudarAba(indice));
                rotulos.Add(r);
                linhaAbas.Add(r);
            }
            linhaAbas.Add(UI.DicaTexto(() => ControlesMenu.NomeAbaDireita, ""));
            Raiz.Add(menu.BarraTopo("", linhaAbas));

            conteudo = new VisualElement();
            conteudo.style.paddingLeft = 56;
            conteudo.style.paddingTop = 40;
            conteudo.style.width = Length.Percent(62);
            Raiz.Add(conteudo);

            status = UI.Texto("", "subtitulo");
            status.style.position = Position.Absolute;
            status.style.left = 56;
            status.style.bottom = 86;
            status.style.color = new Color(1f, 0.84f, 0.12f);
            status.style.whiteSpace = WhiteSpace.Normal;
            status.style.maxWidth = 1100;
            Raiz.Add(status);

            Raiz.Add(menu.Rodape(UI.Dica(IconeBotao.Qual.Confirmar, "Selecionar"),
                                 UI.DicaTexto(() => ControlesMenu.NomeAbaEsquerda + " / " + ControlesMenu.NomeAbaDireita, "Abas"),
                                 UI.Dica(IconeBotao.Qual.Voltar, "Voltar")));
            nav.AoConfirmar += i => { if (i < acoes.Count) acoes[i]?.Invoke(); };
            MudarAba(aba);
        }

        private void MudarAba(int nova)
        {
            aba = (nova + abas.Length) % abas.Length;
            for (int i = 0; i < rotulos.Count; i++) rotulos[i].EnableInClassList("aba-ativa", i == aba);
            conteudo.Clear();
            nav.Limpar();
            acoes.Clear();
            status.text = "Online em preparação: o grupo, os convites e a lista de amigos usam a Steam e passam a funcionar na versão da loja.";
            campoCodigo = null;
            if (aba == 0) MontarGrupo();
            else if (aba == 1) MontarAmigos();
            else MontarCodigo();
        }

        private void MontarGrupo()
        {
            conteudo.Add(UI.Texto("SEU GRUPO", "titulo"));
            VisualElement vagas = new VisualElement();
            vagas.style.flexDirection = FlexDirection.Row;
            vagas.style.marginTop = 16;
            vagas.style.marginBottom = 24;
            for (int i = 0; i < 4; i++)
            {
                VisualElement v = UI.Caixa("painel");
                v.style.width = 240;
                v.style.height = 200;
                v.style.marginRight = 16;
                v.style.justifyContent = Justify.Center;
                v.style.alignItems = Align.Center;
                if (i == 0)
                {
                    PerfilPersonagem eu = PersonagensSalvos.Pegar(0);
                    v.style.borderTopWidth = 6;
                    v.style.borderTopColor = TelaLados.CoresJogador[0];
                    v.Add(UI.Texto(eu.NotaGeral.ToString(), "cartao-nota"));
                    v.Add(UI.Texto(eu.nome.ToUpperInvariant(), "cartao-nome"));
                    v.Add(UI.Texto("líder do grupo", "texto-pequeno"));
                }
                else
                {
                    Label mais = UI.Texto("+", "titulo");
                    mais.style.fontSize = 80;
                    mais.style.color = new Color(1f, 1f, 1f, 0.3f);
                    v.Add(mais);
                    v.Add(UI.Texto("CONVIDAR", "texto-pequeno"));
                }
                vagas.Add(v);
            }
            conteudo.Add(vagas);
            Label codigo = UI.Texto($"Código do grupo:  {codigoDoGrupo}", "subtitulo");
            codigo.style.fontSize = 34;
            codigo.style.marginBottom = 20;
            conteudo.Add(codigo);
            Botao("CONVIDAR AMIGO", () => MudarAba(1));
            Botao("BUSCAR PARTIDA (2x2)", () => status.text = "Sem conexão com o servidor: o matchmaking entra junto com a Steam.");
            Botao("JOGAR LOCAL COM O GRUPO", () => menu.Abrir(new TelaLados(ModoDeJogo.MultiplayerLocal)));
        }

        private void MontarAmigos()
        {
            conteudo.Add(UI.Texto("AMIGOS", "titulo"));
            VisualElement vazio = UI.Caixa("painel");
            vazio.style.marginTop = 16;
            vazio.style.marginBottom = 24;
            vazio.Add(UI.Texto("Nenhum amigo por aqui ainda.", "subtitulo"));
            Label t = UI.Texto("Com a Steam ligada, seus amigos aparecem nesta lista com o status (online, jogando, na rua) e dá para chamar para o grupo com um botão.", "texto-pequeno");
            t.style.whiteSpace = WhiteSpace.Normal;
            vazio.Add(t);
            conteudo.Add(vazio);
            conteudo.Add(UI.Texto("CONVITES RECEBIDOS", "titulo"));
            conteudo.Add(UI.Texto("Nenhum convite.", "subtitulo"));
            Botao("VOLTAR PARA O GRUPO", () => MudarAba(0));
        }

        private void MontarCodigo()
        {
            conteudo.Add(UI.Texto("ENTRAR COM CÓDIGO", "titulo"));
            conteudo.Add(UI.Texto("Digite o código que seu amigo passou (ex.: RUA-7K2P).", "subtitulo"));
            campoCodigo = new TextField { maxLength = 8, value = "RUA-" };
            campoCodigo.style.fontSize = 40;
            campoCodigo.style.width = 400;
            campoCodigo.style.marginTop = 16;
            campoCodigo.style.marginBottom = 24;
            conteudo.Add(campoCodigo);
            Botao("ENTRAR NO GRUPO", () => status.text = $"Não deu para entrar em {campoCodigo.value.ToUpperInvariant()}: o online ainda não está ligado nesta versão.");
            Botao("DIGITAR (TECLADO)", () => campoCodigo.Focus());
        }

        private void Botao(string texto, Action acao)
        {
            Label b = UI.Texto(texto, "botao-menu");
            b.style.width = 620;
            conteudo.Add(b);
            nav.Adicionar(b);
            acoes.Add(acao);
        }

        public override void Atualizar(List<EntradaMenu> todas, EntradaMenu juntas)
        {
            if (campoCodigo != null && campoCodigo.focusController?.focusedElement == campoCodigo)
            {
                if (juntas.voltar || (juntas.confirmar && juntas.dispositivo is UnityEngine.InputSystem.Gamepad)) campoCodigo.Blur();
                return;
            }
            if (juntas.abaEsquerda) MudarAba(aba - 1);
            else if (juntas.abaDireita) MudarAba(aba + 1);
            else if (juntas.direcao.y != 0) nav.Mover(juntas.direcao, true);
            else if (juntas.confirmar) nav.Confirmar();
            else if (juntas.voltar) menu.Voltar();
        }
    }

    /// <summary>Ajustes: dificuldade, pontos, ajudas, tela, qualidade e volume (salvos no computador).</summary>
    public class TelaAjustes : Tela
    {
        private class Item
        {
            public string nome;
            public Func<string> valor;
            public Action<int> mudar;
        }

        private readonly Navegacao nav = new Navegacao("linha-selecionada");
        private readonly List<Item> itens = new List<Item>();
        private readonly List<Label> valores = new List<Label>();

        protected override void Montar()
        {
            Raiz.Add(menu.BarraTopo("AJUSTES"));
            VisualElement painel = UI.Caixa("painel");
            painel.style.width = 900;
            painel.style.marginLeft = 56;
            painel.style.marginTop = 40;
            itens.Add(new Item { nome = "Dificuldade dos bots", valor = () => TelaLados.NomeNivel(AjustesJogo.NivelBots),
                                 mudar = d => AjustesJogo.NivelBots = (NivelDificuldade)(((int)AjustesJogo.NivelBots + d + 3) % 3) });
            itens.Add(new Item { nome = "Pontos para vencer", valor = () => AjustesJogo.MetaPontos.ToString(),
                                 mudar = d => AjustesJogo.MetaPontos = Ciclo(new[] { 3, 5, 7, 10, 15 }, AjustesJogo.MetaPontos, d) });
            itens.Add(new Item { nome = "Prévia do arremesso (linha)", valor = () => AjustesJogo.PreviaTrajetoria ? "Ligada" : "Desligada",
                                 mudar = d => AjustesJogo.PreviaTrajetoria = !AjustesJogo.PreviaTrajetoria });
            itens.Add(new Item { nome = "Tela cheia", valor = () => AjustesJogo.TelaCheia ? "Sim" : "Não",
                                 mudar = d => AjustesJogo.TelaCheia = !AjustesJogo.TelaCheia });
            itens.Add(new Item { nome = "Qualidade da imagem", valor = () => AjustesJogo.Qualidade == 0 ? "Baixa" : AjustesJogo.Qualidade == 1 ? "Média" : "Alta",
                                 mudar = d => AjustesJogo.Qualidade = Mathf.Clamp(AjustesJogo.Qualidade + d, 0, 2) });
            itens.Add(new Item { nome = "Volume", valor = () => $"{Mathf.RoundToInt(AjustesJogo.Volume * 100)}%",
                                 mudar = d => AjustesJogo.Volume = Mathf.Clamp01(Mathf.Round((AjustesJogo.Volume + d * 0.1f) * 10f) / 10f) });
            foreach (Item it in itens)
            {
                VisualElement linha = UI.Caixa("linha-opcao");
                linha.Add(UI.Texto(it.nome, "opcao-nome"));
                VisualElement direita = new VisualElement { pickingMode = PickingMode.Ignore };
                direita.style.flexDirection = FlexDirection.Row;
                Item este = it;
                Label menos = UI.Texto("◀", "seta");
                menos.pickingMode = PickingMode.Position;
                menos.RegisterCallback<ClickEvent>(e => { este.mudar(-1); Atualizar(); e.StopPropagation(); });
                Label valor = UI.Texto(it.valor(), "opcao-valor");
                valores.Add(valor);
                Label mais = UI.Texto("▶", "seta");
                mais.pickingMode = PickingMode.Position;
                mais.RegisterCallback<ClickEvent>(e => { este.mudar(1); Atualizar(); e.StopPropagation(); });
                direita.Add(menos);
                direita.Add(valor);
                direita.Add(mais);
                linha.Add(direita);
                painel.Add(linha);
                nav.Adicionar(linha);
            }
            Raiz.Add(painel);
            Raiz.Add(menu.Rodape(UI.DicaTexto(() => "◀ ▶", "Mudar"), UI.Dica(IconeBotao.Qual.Voltar, "Voltar")));
        }

        private static int Ciclo(int[] lista, int atual, int d)
        {
            int i = Array.IndexOf(lista, atual);
            if (i < 0) i = 0;
            return lista[(i + d + lista.Length) % lista.Length];
        }

        private void Atualizar()
        {
            for (int i = 0; i < itens.Count; i++) valores[i].text = itens[i].valor();
        }

        public override void Atualizar(List<EntradaMenu> todas, EntradaMenu juntas)
        {
            if (juntas.direcao.y != 0) nav.Mover(juntas.direcao, true);
            else if (juntas.direcao.x != 0) { itens[nav.Indice].mudar(juntas.direcao.x); Atualizar(); }
            else if (juntas.confirmar) { itens[nav.Indice].mudar(1); Atualizar(); }
            else if (juntas.voltar) menu.Voltar();
        }
    }

    /// <summary>Como jogar: as regras da rua e os botões de cada momento do jogo.</summary>
    public class TelaComoJogar : Tela
    {
        public override bool MostrarVitrine => false;

        protected override void Montar()
        {
            Raiz.Add(menu.BarraTopo("COMO JOGAR"));
            VisualElement colunas = new VisualElement();
            colunas.style.flexDirection = FlexDirection.Row;
            colunas.style.paddingLeft = 56;
            colunas.style.paddingRight = 56;
            colunas.style.paddingTop = 30;

            VisualElement regras = UI.Caixa("painel");
            regras.style.width = Length.Percent(40);
            regras.style.marginRight = 20;
            regras.Add(UI.Texto("A REGRA DA RUA", "titulo"));
            foreach (string linha in new[]
            {
                "Duas duplas: uma no TACO, outra na BOLA. Cada base tem uma garrafa (a casinha).",
                "Quem está na bola arremessa na garrafa do outro lado. Derrubou = QUEIMOU: troca de papel.",
                "Quem está no taco rebate e corre: cada vez que os dois se cruzam no meio e chegam na outra base, 1 ponto.",
                "Taco no chão dentro do giz protege a garrafa. Derrubar garrafa protegida é QUEIMOU ERRADO: ponto para o taco.",
                "Proteger a garrafa com o corpo é falta (3 = perde o taco).",
                "Quem fecha os pontos e faz a cruzada final, ganha.",
            })
            {
                Label l = UI.Texto("•  " + linha, "texto-pequeno");
                l.style.whiteSpace = WhiteSpace.Normal;
                l.style.fontSize = 24;
                l.style.marginTop = 10;
                l.style.color = new Color(0.9f, 0.92f, 0.95f);
                regras.Add(l);
            }
            colunas.Add(regras);

            VisualElement controles = UI.Caixa("painel");
            controles.style.flexGrow = 1;
            controles.Add(UI.Texto("CONTROLES", "titulo"));
            string[,] tabela =
            {
                { "Andar / correr", "Analógico esquerdo · R1 sprint", "WASD · Shift" },
                { "Trocar de jogador", "L1", "Q" },
                { "Arremesso reto / rasteira", "R2 (+ analógico dir. p/ baixo)", "Botão esq. do mouse" },
                { "Arremesso quicado / pingo", "L2 / R1", "E / botão dir." },
                { "Curva e bate-pronto", "Analógico direito · □ (bola no ar)", "Mouse · F" },
                { "Rebater bola alta / baixa", "R2 / L2 (segure para força)", "Botão esq. / E" },
                { "Cavadinha", "R1 + R2", "Botão dir. + esq." },
                { "Pegar a bola / mergulhar", "R2 ou L2 / □", "R / F" },
                { "Chutar na garrafa / passe", "R2 / L2 (com a bola)", "Botão esq. / E" },
                { "Escolher a garrafa", "Analógico direito ◀ ▶", "Mouse" },
                { "Pausa", "OPTIONS", "Esc" },
            };
            for (int i = 0; i < tabela.GetLength(0); i++)
            {
                VisualElement linha = UI.Caixa("estatistica");
                linha.style.width = Length.Percent(100);
                Label a = UI.Texto(tabela[i, 0], "texto-pequeno");
                a.style.width = Length.Percent(34);
                a.style.color = Color.white;
                Label b = UI.Texto(tabela[i, 1], "texto-pequeno");
                b.style.width = Length.Percent(38);
                b.style.color = new Color(1f, 0.84f, 0.12f);
                Label c = UI.Texto(tabela[i, 2], "texto-pequeno");
                c.style.width = Length.Percent(28);
                linha.Add(a);
                linha.Add(b);
                linha.Add(c);
                controles.Add(linha);
            }
            colunas.Add(controles);
            Raiz.Add(colunas);
            Raiz.Add(menu.Rodape(UI.Dica(IconeBotao.Qual.Voltar, "Voltar")));
        }

        public override void Atualizar(List<EntradaMenu> todas, EntradaMenu juntas)
        {
            if (juntas.voltar || juntas.confirmar) menu.Voltar();
        }
    }
}
