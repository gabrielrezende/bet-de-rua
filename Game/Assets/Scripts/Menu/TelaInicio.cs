using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace BetsDeRua
{
    /// <summary>
    /// Início no estilo do FC 26: lista vertical de modos à esquerda (o escolhido maior), no centro
    /// o monograma do modo, a frase, os destaques e o botão "Entrar"; ao fundo, uma tomada de
    /// cinema diferente do seu personagem para cada modo, com o fundo desfocado.
    /// </summary>
    public class TelaInicio : Tela
    {
        private class Item
        {
            public string nome, monograma, frase, destaques, botao;
            public Action acao;
            // Tomada da câmera em volta do personagem: ângulo (graus, 0 = de frente), distância, altura da câmera, altura do olhar
            public float angulo, distancia, altura, olhar;
            public bool guarda;
        }

        private static int indiceGuardado;
        private readonly List<Item> itens = new List<Item>();
        private readonly List<Label> rotulos = new List<Label>();
        private VisualElement lista;
        private Label monograma, frase, destaques, botao;
        private VisualElement centro;
        private int indice;
        private float trocouEm;

        public override MenuPrincipal.Enquadramento Camera => MenuPrincipal.Enquadramento.Tomada;

        protected override void Montar()
        {
            CriarItens();
            VisualElement topo = UI.Caixa("fc-topo");
            topo.Add(UI.Texto("BETS DE RUA", "titulo-logo"));
            VisualElement espaco = new VisualElement { pickingMode = PickingMode.Ignore };
            espaco.style.flexGrow = 1;
            topo.Add(espaco);
            topo.Add(MenuPrincipal.CartaoJogador(PersonagensSalvos.Pegar(0)));
            Raiz.Add(topo);

            lista = UI.Caixa("fc-lista");
            for (int i = 0; i < itens.Count; i++)
            {
                int k = i;
                Label l = UI.Texto(itens[i].nome, "fc-item");
                l.pickingMode = PickingMode.Position;
                l.RegisterCallback<PointerEnterEvent>(_ => Selecionar(k));
                l.RegisterCallback<ClickEvent>(_ => { Selecionar(k); Confirmar(); });
                rotulos.Add(l);
                lista.Add(l);
            }
            Raiz.Add(lista);

            centro = UI.Caixa("fc-centro");
            monograma = UI.Texto("", "fc-monograma");
            centro.Add(monograma);
            frase = UI.Texto("", "fc-frase");
            centro.Add(frase);
            destaques = UI.Texto("", "fc-destaques");
            centro.Add(destaques);
            VisualElement pilula = UI.Caixa("fc-pilula");
            pilula.Add(new IconeBotao(IconeBotao.Qual.Confirmar));
            botao = UI.Texto("", "fc-pilula-texto");
            pilula.Add(botao);
            pilula.pickingMode = PickingMode.Position;
            pilula.RegisterCallback<ClickEvent>(_ => Confirmar());
            centro.Add(pilula);
            Raiz.Add(centro);

            Raiz.Add(menu.Rodape(UI.Dica(IconeBotao.Qual.Confirmar, "Selecionar"),
                                 UI.DicaTexto(() => "▲ ▼", "Navegar"),
                                 UI.DicaTexto(() => ControlesMenu.NomeAnalogicoDireito, "Girar o personagem")));
            Selecionar(indiceGuardado, false);
        }

        private void CriarItens()
        {
            itens.Clear();
            PessoaNaPartida Eu(int lado) => new PessoaNaPartida { dispositivo = SessaoDeJogo.DispositivoPrincipal ?? Keyboard.current, numero = 1, lado = lado, vaga = 0 };
            itens.Add(new Item { nome = "PARTIDA RÁPIDA", monograma = "PR", frase = "Escolha seu lado e jogue agora",
                                 destaques = "Você + parceiro bot  ·  Contra a dupla da CPU  ·  Até 10 pontos", botao = "Entrar na Partida Rápida",
                                 acao = () => menu.Abrir(new TelaLados(ModoDeJogo.PartidaRapida)), angulo = 25f, distancia = 3.4f, altura = 1.2f, olhar = 1.05f, guarda = true });
            itens.Add(new Item { nome = "MULTIPLAYER LOCAL", monograma = "ML", frase = "Junta a galera no sofá",
                                 destaques = "De 2 a 4 controles  ·  Dupla contra dupla  ·  Ou todos contra a CPU", botao = "Entrar no Multiplayer Local",
                                 acao = () => menu.Abrir(new TelaLados(ModoDeJogo.MultiplayerLocal)), angulo = -35f, distancia = 4.2f, altura = 0.6f, olhar = 1.2f, guarda = true });
            itens.Add(new Item { nome = "TREINO DE ARREMESSO", monograma = "TA", frase = "Mira na casinha",
                                 destaques = "Reto, quicado, pingo, rasteira  ·  Curva e bate-pronto  ·  Placar de acertos", botao = "Entrar no Treino de Arremesso",
                                 acao = () => menu.ComecarPartida(ModoDeJogo.TreinoArremesso, new List<PessoaNaPartida> { Eu(1) }), angulo = 70f, distancia = 2.8f, altura = 1.5f, olhar = 1.35f });
            itens.Add(new Item { nome = "TREINO DE BATIDA", monograma = "TB", frase = "Leia a bola e mande longe",
                                 destaques = "A CPU arremessa sem parar  ·  Taco alto e taco baixo  ·  Perfeitas e sequência", botao = "Entrar no Treino de Batida",
                                 acao = () => menu.ComecarPartida(ModoDeJogo.TreinoBatida, new List<PessoaNaPartida> { Eu(0) }), angulo = -10f, distancia = 2.2f, altura = 1.55f, olhar = 1.5f, guarda = true });
            itens.Add(new Item { nome = "MEU PERSONAGEM", monograma = "MP", frase = "Monte quem vai pra rua",
                                 destaques = "Corpo, cabelo, roupa, tênis  ·  Estilo de jogo com barras  ·  Salvo no seu PC", botao = "Editar Meu Personagem",
                                 acao = () => menu.Abrir(new TelaPersonagem(0)), angulo = 5f, distancia = 1.6f, altura = 1.65f, olhar = 1.62f });
            itens.Add(new Item { nome = "PERSONAGENS DA GALERA", monograma = "P2", frase = "Cada amigo com o seu",
                                 destaques = "Personagens do P2, P3 e P4  ·  Entram quando o controle deles joga", botao = "Editar o P2",
                                 acao = () => menu.Abrir(new TelaPersonagem(1)), angulo = -60f, distancia = 3.2f, altura = 1.4f, olhar = 1.1f });
            itens.Add(new Item { nome = "ONLINE", monograma = "ON", frase = "Chame seu parça pra rua",
                                 destaques = "Grupo com código  ·  Lista de amigos  ·  Convites (chega com a Steam)", botao = "Entrar no Online",
                                 acao = () => menu.Abrir(new TelaOnline(0)), angulo = 140f, distancia = 3.8f, altura = 1.8f, olhar = 1.3f });
            itens.Add(new Item { nome = "ASSISTIR", monograma = "AS", frase = "Os bots jogam, você aprende",
                                 destaques = "Quatro bots  ·  Bom para ver as regras na prática", botao = "Assistir uma partida",
                                 acao = () => menu.ComecarPartida(ModoDeJogo.Assistir, new List<PessoaNaPartida>()), angulo = 180f, distancia = 5.5f, altura = 2.4f, olhar = 1f });
            itens.Add(new Item { nome = "COMO JOGAR", monograma = "CJ", frase = "A regra da rua e os botões",
                                 destaques = "Queimar, cruzar, taco no chão  ·  Controle e teclado", botao = "Ver Como Jogar",
                                 acao = () => menu.Abrir(new TelaComoJogar()), angulo = 40f, distancia = 2.4f, altura = 0.5f, olhar = 1.4f, guarda = true });
            itens.Add(new Item { nome = "AJUSTES", monograma = "AJ", frase = "Do seu jeito",
                                 destaques = "Dificuldade  ·  Pontos  ·  Ajudas  ·  Imagem e som", botao = "Abrir os Ajustes",
                                 acao = () => menu.Abrir(new TelaAjustes()), angulo = -100f, distancia = 3f, altura = 1.3f, olhar = 1.1f });
            itens.Add(new Item { nome = "SAIR DO JOGO", monograma = "X", frase = "Até a próxima rodada",
                                 destaques = "Fecha o Bets de Rua", botao = "Sair", acao = menu.Sair, angulo = 0f, distancia = 6f, altura = 2f, olhar = 1f });
        }

        public override void AoAbrir()
        {
            menu.MostrarPersonagem(PersonagensSalvos.Pegar(0));
            Selecionar(indice, false);
        }

        private void Selecionar(int novo, bool som = true)
        {
            novo = Mathf.Clamp(novo, 0, itens.Count - 1);
            if (som && novo != indice) Sons.Tocar(TipoSom.MenuMover, 0.6f, 0.02f);
            indice = novo;
            indiceGuardado = indice;
            for (int i = 0; i < rotulos.Count; i++)
            {
                rotulos[i].EnableInClassList("fc-item-ativo", i == indice);
                rotulos[i].style.opacity = Mathf.Lerp(1f, 0.35f, Mathf.Clamp01(Mathf.Abs(i - indice) / 5f));
            }
            Item it = itens[indice];
            monograma.text = it.monograma;
            frase.text = it.frase;
            destaques.text = it.destaques;
            botao.text = it.botao;
            menu.DefinirTomada(it.angulo, it.distancia, it.altura, it.olhar, it.guarda);
            trocouEm = Time.time;
        }

        private void Confirmar()
        {
            Sons.Tocar(TipoSom.MenuConfirmar, 0.7f, 0f);
            itens[indice].acao?.Invoke();
        }

        public override void Atualizar(List<EntradaMenu> todas, EntradaMenu juntas)
        {
            float t = Mathf.Clamp01((Time.time - trocouEm) / 0.3f);
            centro.style.opacity = t;
            centro.style.translate = new Translate(0f, (1f - t) * 18f);
            if (juntas.direcao.y != 0) Selecionar(indice - juntas.direcao.y);
            else if (juntas.confirmar) Confirmar();
            else if (juntas.voltar) menu.Trocar(new TelaAbertura());
        }
    }
}
