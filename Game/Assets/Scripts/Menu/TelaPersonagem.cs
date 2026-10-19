using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace BetsDeRua
{
    /// <summary>
    /// Criador de personagem (como o Pro Clubs do FIFA / MyPlayer do NBA 2K): categorias à
    /// esquerda (L1/R1), opções no meio (◀ ▶ muda), o boneco 3D no fundo (analógico direito
    /// gira, L2/R2 aproxima) e as barras do estilo de jogo à direita. △ sorteia um visual.
    /// Salva sozinho ao sair.
    /// </summary>
    public class TelaPersonagem : Tela
    {
        private class Opcao
        {
            public string nome;
            public Func<string> valor;
            public Action<int> mudar;
            public Func<Color?> cor;
            public Action confirmar;
        }

        private readonly int vaga;
        private PerfilPersonagem perfil;
        private readonly string[] categorias = { "IDENTIDADE", "CORPO", "CABEÇA", "ROUPA", "EXTRAS", "ESTILO DE JOGO" };
        private int categoria;
        private readonly List<Label> rotulosCategorias = new List<Label>();
        private VisualElement painelOpcoes, painelAtributos;
        private readonly Navegacao nav = new Navegacao("linha-selecionada");
        private List<Opcao> opcoes;
        private TextField campoNome;

        private static readonly string[] Nomes =
        {
            "Você", "Gabi", "Tico", "Bruno", "Lia", "Dudu", "Nina", "Rafa", "Biel", "Juju", "Caio", "Léo", "Bia", "Magrão",
            "Baixinho", "Fumaça", "Foguete", "Canhota", "Zica", "Tampinha", "Mano", "Parça"
        };

        public static readonly Color[] Peles =
        {
            new Color(0.96f, 0.82f, 0.7f), new Color(0.9f, 0.74f, 0.6f), new Color(0.86f, 0.7f, 0.56f), new Color(0.78f, 0.6f, 0.45f),
            new Color(0.72f, 0.53f, 0.38f), new Color(0.62f, 0.43f, 0.3f), new Color(0.52f, 0.35f, 0.24f), new Color(0.45f, 0.3f, 0.2f),
            new Color(0.36f, 0.24f, 0.16f), new Color(0.26f, 0.17f, 0.12f)
        };

        public static readonly Color[] CoresCabelo =
        {
            new Color(0.06f, 0.05f, 0.04f), new Color(0.12f, 0.08f, 0.05f), new Color(0.28f, 0.17f, 0.08f), new Color(0.45f, 0.28f, 0.12f),
            new Color(0.75f, 0.55f, 0.28f), new Color(0.92f, 0.8f, 0.5f), new Color(0.55f, 0.15f, 0.08f), new Color(0.8f, 0.8f, 0.82f),
            new Color(0.2f, 0.35f, 0.9f), new Color(0.85f, 0.2f, 0.55f)
        };

        public static readonly Color[] CoresRoupa =
        {
            new Color(0.95f, 0.95f, 0.93f), new Color(0.08f, 0.08f, 0.1f), new Color(0.16f, 0.18f, 0.24f), new Color(0.3f, 0.35f, 0.2f),
            new Color(0.6f, 0.15f, 0.4f), new Color(0.9f, 0.2f, 0.2f), new Color(0.2f, 0.5f, 0.9f), new Color(1f, 0.8f, 0.2f),
            new Color(0.2f, 0.75f, 0.45f), new Color(0.55f, 0.55f, 0.58f), new Color(0.85f, 0.85f, 0.8f), new Color(0.95f, 0.5f, 0.15f)
        };

        public TelaPersonagem(int vaga) { this.vaga = vaga; }

        public override MenuPrincipal.Enquadramento Camera => MenuPrincipal.Enquadramento.Personagem;

        protected override void Montar()
        {
            perfil = PersonagensSalvos.Pegar(vaga);
            Raiz.Add(menu.BarraTopo(vaga == 0 ? "MEU PERSONAGEM" : $"PERSONAGEM DO P{vaga + 1}"));

            VisualElement corpo = new VisualElement();
            corpo.style.flexDirection = FlexDirection.Row;
            corpo.style.flexGrow = 1;
            corpo.style.paddingLeft = 56;
            corpo.style.paddingTop = 36;
            corpo.style.paddingRight = 56;
            corpo.style.paddingBottom = 90;

            VisualElement listaCategorias = UI.Caixa("painel");
            listaCategorias.style.width = 340;
            listaCategorias.style.alignSelf = Align.FlexStart;
            listaCategorias.Add(UI.DicaTexto(() => ControlesMenu.NomeAbaEsquerda + " / " + ControlesMenu.NomeAbaDireita, "categoria"));
            for (int i = 0; i < categorias.Length; i++)
            {
                int indice = i;
                Label l = UI.Texto(categorias[i], "categoria");
                l.pickingMode = PickingMode.Position;
                l.RegisterCallback<ClickEvent>(_ => MudarCategoria(indice));
                rotulosCategorias.Add(l);
                listaCategorias.Add(l);
            }
            corpo.Add(listaCategorias);

            VisualElement meio = new VisualElement { pickingMode = PickingMode.Ignore };
            meio.style.width = 640;
            meio.style.marginLeft = 20;
            painelOpcoes = UI.Caixa("painel");
            meio.Add(painelOpcoes);
            painelAtributos = UI.Caixa("painel");
            painelAtributos.style.marginTop = 16;
            meio.Add(painelAtributos);
            corpo.Add(meio);
            Raiz.Add(corpo);

            Raiz.Add(menu.Rodape(UI.DicaTexto(() => "◀ ▶", "Mudar"),
                                 UI.DicaTexto(() => ControlesMenu.NomeAnalogicoDireito, "Girar"),
                                 UI.DicaTexto(() => ControlesMenu.NomeZoom, "Aproximar"),
                                 UI.Dica(IconeBotao.Qual.Triangulo, "Sortear visual"),
                                 UI.Dica(IconeBotao.Qual.Voltar, "Salvar e voltar")));
            nav.AoConfirmar += i => opcoes[i].confirmar?.Invoke();
            MudarCategoria(0);
            AtualizarAtributos();
        }

        public override void AoAbrir()
        {
            menu.GiroVitrine = 0f;
            menu.MostrarPersonagem(perfil);
        }

        public override void AoFechar()
        {
            PersonagensSalvos.Salvar();
            menu.ZoomVitrine = 0f;
        }

        private void MudarCategoria(int nova)
        {
            categoria = (nova + categorias.Length) % categorias.Length;
            for (int i = 0; i < rotulosCategorias.Count; i++) rotulosCategorias[i].EnableInClassList("categoria-ativa", i == categoria);
            opcoes = Opcoes(categoria);
            painelOpcoes.Clear();
            nav.Limpar();
            Label titulo = UI.Texto(categorias[categoria], "titulo");
            titulo.style.fontSize = 40;
            titulo.style.marginBottom = 12;
            painelOpcoes.Add(titulo);
            foreach (Opcao o in opcoes)
            {
                VisualElement linha = UI.Caixa("linha-opcao");
                linha.Add(UI.Texto(o.nome, "opcao-nome"));
                VisualElement direita = new VisualElement { pickingMode = PickingMode.Ignore };
                direita.style.flexDirection = FlexDirection.Row;
                direita.style.alignItems = Align.Center;
                Label menos = UI.Texto("◀", "seta");
                menos.pickingMode = PickingMode.Position;
                Opcao esta = o;
                menos.RegisterCallback<ClickEvent>(e => { esta.mudar(-1); Mudou(); e.StopPropagation(); });
                Label valor = UI.Texto(o.valor(), "opcao-valor");
                valor.name = "valor";
                Label mais = UI.Texto("▶", "seta");
                mais.pickingMode = PickingMode.Position;
                mais.RegisterCallback<ClickEvent>(e => { esta.mudar(1); Mudou(); e.StopPropagation(); });
                direita.Add(menos);
                direita.Add(valor);
                direita.Add(mais);
                if (o.cor != null)
                {
                    VisualElement amostra = UI.Caixa("amostra-cor");
                    amostra.name = "amostra";
                    amostra.style.backgroundColor = o.cor() ?? Color.clear;
                    direita.Add(amostra);
                }
                linha.Add(direita);
                painelOpcoes.Add(linha);
                nav.Adicionar(linha);
            }
            if (categoria == 0)
            {
                campoNome = new TextField("Digitar nome (teclado)") { maxLength = 14, value = perfil.nome };
                campoNome.style.marginTop = 16;
                campoNome.style.fontSize = 24;
                campoNome.RegisterValueChangedCallback(e => { perfil.nome = string.IsNullOrWhiteSpace(e.newValue) ? "Jogador" : e.newValue.Trim(); AtualizarValores(); AtualizarAtributos(); });
                painelOpcoes.Add(campoNome);
            }
            else campoNome = null;
        }

        private List<Opcao> Opcoes(int qual)
        {
            PerfilPersonagem p = perfil;
            switch (qual)
            {
                case 0:
                    return new List<Opcao>
                    {
                        new Opcao { nome = "Apelido", valor = () => p.nome, mudar = d => p.nome = Proximo(Nomes, p.nome, d),
                                    confirmar = () => campoNome?.Focus() },
                        new Opcao { nome = "Idade", valor = () => p.faixaEtaria == FaixaEtaria.Crianca ? "Criança" : p.faixaEtaria == FaixaEtaria.Adolescente ? "Adolescente" : "Adulto",
                                    mudar = d => { p.faixaEtaria = Ciclo(p.faixaEtaria, d); p.altura = PerfilPersonagem.AlturaPadrao(p.faixaEtaria); } },
                        new Opcao { nome = "Altura", valor = () => $"{p.altura:0.00} m".Replace('.', ','),
                                    mudar = d => { Vector2 faixa = PerfilPersonagem.FaixaAltura(p.faixaEtaria); p.altura = Mathf.Clamp(Mathf.Round((p.altura + d * 0.02f) * 100f) / 100f, faixa.x, faixa.y); } },
                    };
                case 1:
                    return new List<Opcao>
                    {
                        new Opcao { nome = "Tipo de corpo", valor = () => p.tipoCorpo == TipoCorpo.Magro ? "Magro" : p.tipoCorpo == TipoCorpo.Forte ? "Forte" : "Médio",
                                    mudar = d => p.tipoCorpo = Ciclo(p.tipoCorpo, d) },
                        new Opcao { nome = "Tom de pele", valor = () => $"{IndiceCor(Peles, p.corPele) + 1} de {Peles.Length}",
                                    mudar = d => p.corPele = ProximaCor(Peles, p.corPele, d), cor = () => p.corPele },
                    };
                case 2:
                    return new List<Opcao>
                    {
                        new Opcao { nome = "Cabelo", valor = () => NomeCabelo(p.cabelo), mudar = d => p.cabelo = Ciclo(p.cabelo, d) },
                        new Opcao { nome = "Cor do cabelo", valor = () => $"{IndiceCor(CoresCabelo, p.corCabelo) + 1} de {CoresCabelo.Length}",
                                    mudar = d => p.corCabelo = ProximaCor(CoresCabelo, p.corCabelo, d), cor = () => p.corCabelo },
                        new Opcao { nome = "Barba", valor = () => p.barba == Barba.Bigode ? "Bigode" : p.barba == Barba.Cavanhaque ? "Cavanhaque" : p.barba == Barba.Cheia ? "Cheia" : "Sem barba",
                                    mudar = d => p.barba = Ciclo(p.barba, d) },
                        new Opcao { nome = "Boné / chapéu", valor = () => NomeChapeu(p.chapeu), mudar = d => p.chapeu = Ciclo(p.chapeu, d) },
                    };
                case 3:
                    return new List<Opcao>
                    {
                        new Opcao { nome = "Camisa (cor do time)", valor = () => p.camisa == Camisa.Regata ? "Regata" : p.camisa == Camisa.MangaLonga ? "Manga longa" : "Camiseta",
                                    mudar = d => p.camisa = Ciclo(p.camisa, d) },
                        new Opcao { nome = "Bermuda", valor = () => p.bermuda == Bermuda.Calcao ? "Calção" : p.bermuda == Bermuda.Calca ? "Calça" : "Bermuda",
                                    mudar = d => p.bermuda = Ciclo(p.bermuda, d) },
                        new Opcao { nome = "Cor da bermuda", valor = () => $"{IndiceCor(CoresRoupa, p.corBermuda) + 1} de {CoresRoupa.Length}",
                                    mudar = d => p.corBermuda = ProximaCor(CoresRoupa, p.corBermuda, d), cor = () => p.corBermuda },
                        new Opcao { nome = "Calçado", valor = () => p.calcado == Calcado.Chinelo ? "Chinelo" : p.calcado == Calcado.TenisCanoAlto ? "Tênis cano alto" : "Tênis",
                                    mudar = d => p.calcado = Ciclo(p.calcado, d) },
                        new Opcao { nome = "Cor do calçado", valor = () => $"{IndiceCor(CoresRoupa, p.corCalcado) + 1} de {CoresRoupa.Length}",
                                    mudar = d => p.corCalcado = ProximaCor(CoresRoupa, p.corCalcado, d), cor = () => p.corCalcado },
                    };
                case 4:
                    return new List<Opcao>
                    {
                        new Opcao { nome = "Acessório", valor = () => p.acessorio == Acessorio.Oculos ? "Óculos escuros" : p.acessorio == Acessorio.Corrente ? "Corrente" : p.acessorio == Acessorio.Munhequeira ? "Munhequeira" : "Nenhum",
                                    mudar = d => p.acessorio = Ciclo(p.acessorio, d) },
                        new Opcao { nome = "Tatuagem no braço", valor = () => p.tatuagem ? "Sim" : "Não", mudar = d => p.tatuagem = !p.tatuagem },
                        new Opcao { nome = "Fita do taco", valor = () => $"{IndiceCor(CoresRoupa, p.corFitaTaco) + 1} de {CoresRoupa.Length}",
                                    mudar = d => p.corFitaTaco = ProximaCor(CoresRoupa, p.corFitaTaco, d), cor = () => p.corFitaTaco },
                    };
                default:
                    return new List<Opcao>
                    {
                        new Opcao { nome = "Estilo de jogo", valor = () => MenuPrincipal.NomeEstilo(p.estilo), mudar = d => p.estilo = Ciclo(p.estilo, d) },
                    };
            }
        }

        private static string NomeCabelo(EstiloCabelo c) =>
            c == EstiloCabelo.Careca ? "Careca" : c == EstiloCabelo.Raspado ? "Raspado" : c == EstiloCabelo.BlackPower ? "Black power"
            : c == EstiloCabelo.Moicano ? "Moicano" : c == EstiloCabelo.Longo ? "Longo" : c == EstiloCabelo.Trancas ? "Tranças" : "Curto";

        private static string NomeChapeu(Chapeu c) =>
            c == Chapeu.Bone ? "Boné" : c == Chapeu.BoneParaTras ? "Boné para trás" : c == Chapeu.Bucket ? "Bucket" : c == Chapeu.Touca ? "Touca" : "Nada";

        private static T Ciclo<T>(T atual, int d) where T : Enum
        {
            Array valores = Enum.GetValues(typeof(T));
            int i = Array.IndexOf(valores, atual);
            return (T)valores.GetValue((i + d + valores.Length) % valores.Length);
        }

        private static string Proximo(string[] lista, string atual, int d)
        {
            int i = Array.IndexOf(lista, atual);
            if (i < 0) i = 0;
            return lista[(i + d + lista.Length) % lista.Length];
        }

        private static int IndiceCor(Color[] lista, Color cor)
        {
            int melhor = 0;
            float menor = float.MaxValue;
            for (int i = 0; i < lista.Length; i++)
            {
                float d = Mathf.Abs(lista[i].r - cor.r) + Mathf.Abs(lista[i].g - cor.g) + Mathf.Abs(lista[i].b - cor.b);
                if (d < menor) { menor = d; melhor = i; }
            }
            return melhor;
        }

        private static Color ProximaCor(Color[] lista, Color atual, int d) => lista[(IndiceCor(lista, atual) + d + lista.Length) % lista.Length];

        private void Mudou()
        {
            AtualizarValores();
            AtualizarAtributos();
            menu.MostrarPersonagem(perfil);
        }

        private void AtualizarValores()
        {
            int i = 0;
            foreach (VisualElement linha in painelOpcoes.Children())
            {
                if (!linha.ClassListContains("linha-opcao")) continue;
                if (i >= opcoes.Count) break;
                Label valor = linha.Q<Label>("valor");
                if (valor != null) valor.text = opcoes[i].valor();
                VisualElement amostra = linha.Q("amostra");
                if (amostra != null && opcoes[i].cor != null) amostra.style.backgroundColor = opcoes[i].cor() ?? Color.clear;
                i++;
            }
            if (campoNome != null && campoNome.value != perfil.nome && campoNome.focusController?.focusedElement != campoNome)
                campoNome.SetValueWithoutNotify(perfil.nome);
        }

        private void AtualizarAtributos()
        {
            painelAtributos.Clear();
            VisualElement cabecalho = new VisualElement();
            cabecalho.style.flexDirection = FlexDirection.Row;
            cabecalho.style.alignItems = Align.Center;
            cabecalho.style.marginBottom = 16;
            Label nota = UI.Texto(perfil.NotaGeral.ToString(), "cartao-nota");
            nota.style.fontSize = 64;
            cabecalho.Add(nota);
            VisualElement nomes = new VisualElement();
            nomes.Add(UI.Texto(perfil.nome.ToUpperInvariant(), "titulo"));
            nomes.Add(UI.Texto(MenuPrincipal.NomeEstilo(perfil.estilo).ToUpperInvariant(), "subtitulo"));
            cabecalho.Add(nomes);
            painelAtributos.Add(cabecalho);
            int[] barras = perfil.Barras;
            for (int i = 0; i < barras.Length; i++) painelAtributos.Add(UI.Atributo(PerfilPersonagem.NomesBarras[i], barras[i]));
            Label explica = UI.Texto(DescricaoEstilo(perfil.estilo), "texto-pequeno");
            explica.style.whiteSpace = WhiteSpace.Normal;
            explica.style.marginTop = 12;
            painelAtributos.Add(explica);
            Label justo = UI.Texto("Todos os estilos somam os mesmos pontos. O visual não muda os atributos.", "texto-pequeno");
            justo.style.whiteSpace = WhiteSpace.Normal;
            justo.style.marginTop = 10;
            justo.style.color = new Color(0.6f, 0.63f, 0.7f);
            painelAtributos.Add(justo);
        }

        private static string DescricaoEstilo(EstiloJogo e) =>
            e == EstiloJogo.Rebatedor ? "Bate forte e alcança longe com o taco. Corre um pouco menos."
            : e == EstiloJogo.Corredor ? "Rápido entre as bases e com fôlego de sobra. Bate mais fraco."
            : e == EstiloJogo.Lancador ? "Arremesso e chute certeiros e fortes. Ideal para queimar."
            : e == EstiloJogo.Defensor ? "Chega antes na bola e alcança mais no peixinho."
            : "Faz de tudo um pouco. Bom para começar.";

        public override void Atualizar(List<EntradaMenu> todas, EntradaMenu juntas)
        {
            // Digitando o nome: o teclado é do campo de texto
            if (campoNome != null && campoNome.focusController?.focusedElement == campoNome)
            {
                if (juntas.voltar || (juntas.confirmar && juntas.dispositivo is UnityEngine.InputSystem.Gamepad)) campoNome.Blur();
                return;
            }
            if (juntas.abaEsquerda) MudarCategoria(categoria - 1);
            else if (juntas.abaDireita) MudarCategoria(categoria + 1);
            else if (juntas.direcao.y != 0) nav.Mover(juntas.direcao, true);
            else if (juntas.direcao.x != 0 && opcoes.Count > 0) { opcoes[nav.Indice].mudar(juntas.direcao.x); Mudou(); }
            else if (juntas.confirmar) nav.Confirmar();
            else if (juntas.triangulo) { Sortear(); Mudou(); }
            else if (juntas.voltar) menu.Voltar();
        }

        private void Sortear()
        {
            PerfilPersonagem p = perfil;
            p.corPele = Peles[UnityEngine.Random.Range(0, Peles.Length)];
            p.cabelo = (EstiloCabelo)UnityEngine.Random.Range(0, Enum.GetValues(typeof(EstiloCabelo)).Length);
            p.corCabelo = CoresCabelo[UnityEngine.Random.Range(0, 6)];
            p.barba = UnityEngine.Random.value < 0.6f ? Barba.Nada : (Barba)UnityEngine.Random.Range(1, 4);
            p.chapeu = (Chapeu)UnityEngine.Random.Range(0, Enum.GetValues(typeof(Chapeu)).Length);
            p.camisa = (Camisa)UnityEngine.Random.Range(0, 3);
            p.bermuda = (Bermuda)UnityEngine.Random.Range(0, 3);
            p.corBermuda = CoresRoupa[UnityEngine.Random.Range(0, CoresRoupa.Length)];
            p.calcado = (Calcado)UnityEngine.Random.Range(0, 3);
            p.corCalcado = CoresRoupa[UnityEngine.Random.Range(0, CoresRoupa.Length)];
            p.tipoCorpo = (TipoCorpo)UnityEngine.Random.Range(0, 3);
            p.acessorio = UnityEngine.Random.value < 0.5f ? Acessorio.Nada : (Acessorio)UnityEngine.Random.Range(1, 4);
            p.tatuagem = UnityEngine.Random.value < 0.3f;
            if (p.faixaEtaria != FaixaEtaria.Adulto) p.barba = Barba.Nada;
        }
    }
}
