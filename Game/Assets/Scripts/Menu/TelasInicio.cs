using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace BetsDeRua
{
    /// <summary>Abertura: logo grande, "APERTE START" piscando. Quem apertar vira o P1.</summary>
    public class TelaAbertura : Tela
    {
        private Label aperte;
        private float tempo;

        public override MenuPrincipal.Enquadramento Camera => MenuPrincipal.Enquadramento.Abertura;

        protected override void Montar()
        {
            VisualElement caixa = UI.Caixa("abertura");
            // Logo em cima, "aperte start" embaixo: o personagem fica livre no meio da tela
            VisualElement topo = new VisualElement { pickingMode = PickingMode.Ignore };
            topo.style.alignItems = Align.Center;
            Label logo = UI.Texto("BETS DE RUA", "titulo-logo", "titulo-logo-grande");
            topo.Add(logo);
            Label sub = UI.Texto("TACO · BETE · TACOBOL — O JOGO DE RUA", "subtitulo");
            sub.style.fontSize = 30;
            sub.style.letterSpacing = 6;
            sub.style.color = Color.white;
            topo.Add(sub);
            caixa.Add(topo);
            aperte = UI.Texto("APERTE START", "aperte-start");
            caixa.Add(aperte);
            Label versao = UI.Texto($"versão {Application.version} · protótipo para testes", "texto-pequeno");
            versao.style.position = Position.Absolute;
            versao.style.right = 40;
            versao.style.bottom = 24;
            caixa.Add(versao);
            Raiz.Add(caixa);
            caixa.RegisterCallback<ClickEvent>(_ => Entrar(Keyboard.current));
        }

        public override void Atualizar(List<EntradaMenu> todas, EntradaMenu juntas)
        {
            tempo += Time.deltaTime;
            aperte.style.opacity = 0.35f + 0.65f * Mathf.Abs(Mathf.Sin(tempo * 2.2f));
            foreach (EntradaMenu e in todas)
                if (e.start || e.confirmar || (e.qualquer && e.dispositivo is Keyboard)) { Entrar(e.dispositivo); return; }
        }

        private void Entrar(InputDevice quem)
        {
            SessaoDeJogo.DispositivoPrincipal = quem;
            Sons.Tocar(TipoSom.Start, 0.8f, 0f);
            menu.Trocar(new TelaInicio());
        }
    }

    /// <summary>
    /// Início, como o FIFA: abas no topo (L1/R1) e blocos grandes. JOGAR, TREINO, MEU JOGADOR,
    /// ONLINE e AJUSTES. O seu personagem aparece no fundo.
    /// </summary>
    public class TelaInicio : Tela
    {
        private class Bloco
        {
            public string titulo, texto, selo;
            public bool grande, desligado;
            public Color corA, corB;
            public Action acao;
        }

        private static int abaGuardada;
        private readonly string[] abas = { "JOGAR", "TREINO", "MEU JOGADOR", "ONLINE", "AJUSTES" };
        private readonly List<Label> rotulosAbas = new List<Label>();
        private VisualElement grade;
        private readonly Navegacao nav = new Navegacao("bloco-selecionado");
        private List<Bloco> blocosDaAba;
        private int aba;

        protected override void Montar()
        {
            VisualElement linhaAbas = UI.Caixa("abas");
            linhaAbas.Add(UI.DicaTexto(() => ControlesMenu.NomeAbaEsquerda, ""));
            for (int i = 0; i < abas.Length; i++)
            {
                int indice = i;
                Label rotulo = UI.Texto(abas[i], "aba");
                rotulo.pickingMode = PickingMode.Position;
                rotulo.RegisterCallback<ClickEvent>(_ => MudarAba(indice));
                rotulosAbas.Add(rotulo);
                linhaAbas.Add(rotulo);
            }
            linhaAbas.Add(UI.DicaTexto(() => ControlesMenu.NomeAbaDireita, ""));
            Raiz.Add(menu.BarraTopo("", linhaAbas));

            grade = UI.Caixa("grade");
            Raiz.Add(grade);
            Raiz.Add(menu.Rodape(UI.Dica(IconeBotao.Qual.Confirmar, "Selecionar"),
                                 UI.DicaTexto(() => ControlesMenu.NomeAbaEsquerda + " / " + ControlesMenu.NomeAbaDireita, "Abas"),
                                 UI.DicaTexto(() => ControlesMenu.NomeAnalogicoDireito, "Girar o personagem")));
            nav.AoConfirmar += i => { Bloco b = blocosDaAba[i]; if (!b.desligado) b.acao?.Invoke(); };
            MudarAba(abaGuardada);
        }

        public override void AoAbrir()
        {
            menu.MostrarPersonagem(PersonagensSalvos.Pegar(0));
            // Atualiza o cartão (o personagem pode ter mudado no criador)
            Raiz.Clear();
            rotulosAbas.Clear();
            nav.Limpar();
            int abaAntes = aba;
            Montar();
            MudarAba(abaAntes);
        }

        private void MudarAba(int nova)
        {
            aba = (nova + abas.Length) % abas.Length;
            abaGuardada = aba;
            for (int i = 0; i < rotulosAbas.Count; i++) rotulosAbas[i].EnableInClassList("aba-ativa", i == aba);
            grade.Clear();
            nav.Limpar();
            blocosDaAba = Blocos(aba);
            foreach (Bloco b in blocosDaAba)
            {
                VisualElement v = UI.Caixa("bloco");
                if (b.grande) v.AddToClassList("bloco-grande");
                if (b.desligado) v.AddToClassList("bloco-desligado");
                v.style.backgroundImage = UI.Degrade(b.corA, b.corB);
                if (!string.IsNullOrEmpty(b.selo)) v.Add(UI.Texto(b.selo, "selo"));
                v.Add(UI.Texto(b.titulo, "bloco-titulo"));
                v.Add(UI.Texto(b.texto, "bloco-texto"));
                grade.Add(v);
                nav.Adicionar(v);
            }
        }

        private List<Bloco> Blocos(int qual)
        {
            Color laranja = new Color(0.95f, 0.45f, 0.1f), vermelho = new Color(0.75f, 0.12f, 0.18f);
            Color azul = new Color(0.12f, 0.35f, 0.85f), roxo = new Color(0.4f, 0.15f, 0.6f);
            Color verde = new Color(0.1f, 0.55f, 0.35f), petroleo = new Color(0.05f, 0.3f, 0.38f);
            Color cinza = new Color(0.22f, 0.24f, 0.3f), grafite = new Color(0.1f, 0.11f, 0.14f);
            switch (qual)
            {
                case 0:
                    return new List<Bloco>
                    {
                        new Bloco { titulo = "PARTIDA RÁPIDA", texto = "Você e um parceiro bot contra a dupla da CPU. Escolha seu lado e jogue.",
                                    grande = true, corA = laranja, corB = vermelho, selo = "JOGUE AGORA",
                                    acao = () => menu.Abrir(new TelaLados(ModoDeJogo.PartidaRapida)) },
                        new Bloco { titulo = "MULTIPLAYER LOCAL", texto = "De 2 a 4 controles no mesmo sofá: dupla contra dupla ou juntos contra a CPU.",
                                    corA = azul, corB = roxo, acao = () => menu.Abrir(new TelaLados(ModoDeJogo.MultiplayerLocal)) },
                        new Bloco { titulo = "ASSISTIR", texto = "Os quatro são bots. Bom para ver as jogadas e aprender as regras.",
                                    corA = cinza, corB = grafite, acao = () => menu.ComecarPartida(ModoDeJogo.Assistir, new List<PessoaNaPartida>()) },
                    };
                case 1:
                    return new List<Bloco>
                    {
                        new Bloco { titulo = "TREINO DE ARREMESSO", texto = "Derrube a garrafa: rasteira, quicado, pingo, curva e bate-pronto. Placar de acertos e velocidade.",
                                    grande = true, corA = verde, corB = petroleo, selo = "NOVO",
                                    acao = () => menu.ComecarPartida(ModoDeJogo.TreinoArremesso, new List<PessoaNaPartida>
                                    { new PessoaNaPartida { dispositivo = SessaoDeJogo.DispositivoPrincipal ?? Keyboard.current, numero = 1, lado = 1, vaga = 0 } }) },
                        new Bloco { titulo = "COMO JOGAR", texto = "Regras da rua e todos os botões, no controle e no teclado.",
                                    corA = cinza, corB = grafite, acao = () => menu.Abrir(new TelaComoJogar()) },
                        new Bloco { titulo = "TREINO DE BATIDA", texto = "A CPU arremessa sem parar: treine o tempo e a altura da batida (R2 no ar, L2 baixa).",
                                    corA = azul, corB = roxo, selo = "NOVO",
                                    acao = () => menu.ComecarPartida(ModoDeJogo.TreinoBatida, new List<PessoaNaPartida>
                                    { new PessoaNaPartida { dispositivo = SessaoDeJogo.DispositivoPrincipal ?? Keyboard.current, numero = 1, lado = 0, vaga = 0 } }) },
                    };
                case 2:
                    PerfilPersonagem eu = PersonagensSalvos.Pegar(0);
                    var lista = new List<Bloco>
                    {
                        new Bloco { titulo = "MEU PERSONAGEM", texto = $"{eu.nome} · {MenuPrincipal.NomeEstilo(eu.estilo)} · nota {eu.NotaGeral}. Corpo, cabelo, roupa, tênis e estilo de jogo.",
                                    grande = true, corA = new Color(0.95f, 0.75f, 0.1f), corB = laranja, selo = "CRIADOR",
                                    acao = () => menu.Abrir(new TelaPersonagem(0)) },
                    };
                    for (int i = 1; i < PersonagensSalvos.Vagas; i++)
                    {
                        int vaga = i;
                        PerfilPersonagem p = PersonagensSalvos.Pegar(i);
                        lista.Add(new Bloco { titulo = $"P{i + 1}: {p.nome.ToUpperInvariant()}", texto = "O personagem de quem joga no controle " + (i + 1) + ".",
                                              corA = i == 1 ? azul : i == 2 ? roxo : petroleo, corB = grafite,
                                              acao = () => menu.Abrir(new TelaPersonagem(vaga)) });
                    }
                    return lista;
                case 3:
                    return new List<Bloco>
                    {
                        new Bloco { titulo = "JOGAR ONLINE", texto = "Monte um grupo, chame um amigo e joguem juntos contra outras duplas.",
                                    grande = true, corA = roxo, corB = azul, selo = "STEAM",
                                    acao = () => menu.Abrir(new TelaOnline(0)) },
                        new Bloco { titulo = "AMIGOS", texto = "Sua lista de amigos e os convites recebidos.",
                                    corA = cinza, corB = grafite, acao = () => menu.Abrir(new TelaOnline(1)) },
                        new Bloco { titulo = "ENTRAR COM CÓDIGO", texto = "Um amigo te passou o código do grupo? Entre por aqui.",
                                    corA = cinza, corB = grafite, acao = () => menu.Abrir(new TelaOnline(2)) },
                    };
                default:
                    return new List<Bloco>
                    {
                        new Bloco { titulo = "AJUSTES", texto = "Dificuldade dos bots, pontos da partida, ajudas, imagem e som.",
                                    grande = true, corA = cinza, corB = grafite, acao = () => menu.Abrir(new TelaAjustes()) },
                        new Bloco { titulo = "SAIR DO JOGO", texto = "Fecha o Bets de Rua.", corA = vermelho, corB = grafite, acao = menu.Sair },
                    };
            }
        }

        public override void Atualizar(List<EntradaMenu> todas, EntradaMenu juntas)
        {
            if (juntas.abaEsquerda) MudarAba(aba - 1);
            else if (juntas.abaDireita) MudarAba(aba + 1);
            else if (juntas.direcao != Vector2Int.zero) nav.Mover(juntas.direcao);
            else if (juntas.confirmar) nav.Confirmar();
            else if (juntas.voltar) menu.Trocar(new TelaAbertura());
        }
    }
}
