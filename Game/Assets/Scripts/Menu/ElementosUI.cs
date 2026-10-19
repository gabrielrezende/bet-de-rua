using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace BetsDeRua
{
    /// <summary>
    /// Ícone de botão do controle desenhado à mão (✕ ○ □ △ do PlayStation, letras do Xbox,
    /// ou a tecla), para não depender da fonte ter esses símbolos.
    /// </summary>
    public class IconeBotao : VisualElement
    {
        public enum Qual { Confirmar, Voltar, Quadrado, Triangulo }
        private readonly Qual qual;
        private readonly Label letra;

        public IconeBotao(Qual qual)
        {
            this.qual = qual;
            AddToClassList("icone-botao");
            letra = new Label { pickingMode = PickingMode.Ignore };
            letra.style.position = Position.Absolute;
            letra.style.left = letra.style.right = letra.style.top = letra.style.bottom = 0;
            letra.style.unityTextAlign = TextAnchor.MiddleCenter;
            letra.style.fontSize = 18;
            letra.style.color = new Color(0.08f, 0.08f, 0.1f);
            Add(letra);
            generateVisualContent += Desenhar;
            schedule.Execute(Atualizar).Every(250);
            Atualizar();
        }

        private void Atualizar()
        {
            ControlesMenu.TipoIcone tipo = ControlesMenu.Icones;
            letra.text = tipo == ControlesMenu.TipoIcone.PlayStation ? "" :
                qual == Qual.Confirmar ? (tipo == ControlesMenu.TipoIcone.Xbox ? "A" : "↵") :
                qual == Qual.Voltar ? (tipo == ControlesMenu.TipoIcone.Xbox ? "B" : "Esc") :
                qual == Qual.Quadrado ? "X" : (tipo == ControlesMenu.TipoIcone.Xbox ? "Y" : "R");
            letra.style.fontSize = letra.text.Length > 1 ? 13 : 18;
            MarkDirtyRepaint();
        }

        private void Desenhar(MeshGenerationContext ctx)
        {
            Painter2D p = ctx.painter2D;
            Rect r = contentRect;
            Vector2 c = r.center;
            float raio = Mathf.Min(r.width, r.height) * 0.5f;
            // Fundo redondo
            p.fillColor = new Color(0.93f, 0.93f, 0.93f);
            p.BeginPath();
            p.Arc(c, raio, 0f, 360f);
            p.Fill();
            if (ControlesMenu.Icones != ControlesMenu.TipoIcone.PlayStation) return;

            float t = raio * 0.42f;
            p.lineWidth = raio * 0.16f;
            p.lineCap = LineCap.Round;
            switch (qual)
            {
                case Qual.Confirmar:
                    p.strokeColor = new Color(0.45f, 0.6f, 1f);
                    p.BeginPath(); p.MoveTo(c + new Vector2(-t, -t)); p.LineTo(c + new Vector2(t, t)); p.Stroke();
                    p.BeginPath(); p.MoveTo(c + new Vector2(t, -t)); p.LineTo(c + new Vector2(-t, t)); p.Stroke();
                    break;
                case Qual.Voltar:
                    p.strokeColor = new Color(1f, 0.35f, 0.4f);
                    p.BeginPath(); p.Arc(c, t * 1.05f, 0f, 360f); p.Stroke();
                    break;
                case Qual.Quadrado:
                    p.strokeColor = new Color(1f, 0.5f, 0.85f);
                    p.BeginPath();
                    p.MoveTo(c + new Vector2(-t, -t)); p.LineTo(c + new Vector2(t, -t)); p.LineTo(c + new Vector2(t, t)); p.LineTo(c + new Vector2(-t, t));
                    p.ClosePath(); p.Stroke();
                    break;
                case Qual.Triangulo:
                    p.strokeColor = new Color(0.3f, 0.85f, 0.7f);
                    p.BeginPath();
                    p.MoveTo(c + new Vector2(0f, -t * 1.1f)); p.LineTo(c + new Vector2(t * 1.1f, t * 0.8f)); p.LineTo(c + new Vector2(-t * 1.1f, t * 0.8f));
                    p.ClosePath(); p.Stroke();
                    break;
            }
        }
    }

    /// <summary>Atalhos para montar a interface em código (sem arquivos de layout).</summary>
    public static class UI
    {
        public static Label Texto(string texto, params string[] classes)
        {
            Label l = new Label(texto) { pickingMode = PickingMode.Ignore };
            foreach (string c in classes) l.AddToClassList(c);
            return l;
        }

        public static VisualElement Caixa(params string[] classes)
        {
            VisualElement v = new VisualElement();
            foreach (string c in classes) v.AddToClassList(c);
            return v;
        }

        /// <summary>Dica do rodapé: ícone + texto (ex.: ✕ Selecionar).</summary>
        public static VisualElement Dica(IconeBotao.Qual qual, string texto)
        {
            VisualElement d = Caixa("dica-botao");
            d.Add(new IconeBotao(qual));
            d.Add(Texto(texto));
            return d;
        }

        /// <summary>Dica do rodapé com o nome da tecla/botão escrito (ex.: L1/R1 Abas).</summary>
        public static VisualElement DicaTexto(Func<string> botao, string texto)
        {
            VisualElement d = Caixa("dica-botao");
            Label icone = Texto(botao(), "icone-texto");
            icone.schedule.Execute(() => icone.text = botao()).Every(250);
            d.Add(icone);
            d.Add(Texto(texto));
            return d;
        }

        /// <summary>Barra de atributo (nome, barra de 0 a 99, número).</summary>
        public static VisualElement Atributo(string nome, int valor, Color? cor = null)
        {
            VisualElement linha = Caixa("atributo");
            linha.Add(Texto(nome, "atributo-nome"));
            VisualElement fundo = Caixa("atributo-fundo");
            VisualElement barra = Caixa("atributo-barra");
            barra.style.width = Length.Percent(Mathf.Clamp(valor, 0, 99));
            Color c = cor ?? CorDaNota(valor);
            barra.style.backgroundColor = c;
            fundo.Add(barra);
            linha.Add(fundo);
            Label numero = Texto(valor.ToString(), "atributo-valor");
            numero.style.color = c;
            linha.Add(numero);
            return linha;
        }

        public static Color CorDaNota(int valor) =>
            valor >= 85 ? new Color(0.25f, 0.9f, 0.4f) : valor >= 72 ? new Color(1f, 0.84f, 0.12f) : new Color(1f, 0.55f, 0.2f);

        /// <summary>Textura com degradê diagonal e listras (fundo dos blocos, cara de muro pintado).</summary>
        public static Texture2D Degrade(Color a, Color b, bool listras = true, int largura = 256, int altura = 128)
        {
            Texture2D t = new Texture2D(largura, altura, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            Color[] px = new Color[largura * altura];
            for (int y = 0; y < altura; y++)
                for (int x = 0; x < largura; x++)
                {
                    float u = (x / (float)largura) * 0.7f + (y / (float)altura) * 0.3f;
                    Color c = Color.Lerp(a, b, u);
                    if (listras && ((x + y * 2) / 18) % 2 == 0) c = Color.Lerp(c, Color.white, 0.035f);
                    // escurece embaixo (o texto fica embaixo)
                    c = Color.Lerp(c, Color.black, Mathf.Clamp01(0.55f - y / (float)altura) * 0.6f);
                    px[y * largura + x] = c;
                }
            t.SetPixels(px);
            t.Apply();
            return t;
        }
    }

    /// <summary>
    /// Lista de itens selecionáveis numa tela, navegada pela posição na tela (como num console:
    /// direcional/analógico vai para o item mais perto naquela direção). O mouse também seleciona
    /// (passando por cima) e confirma (clicando).
    /// </summary>
    public class Navegacao
    {
        private readonly List<VisualElement> itens = new List<VisualElement>();
        private readonly string classeSelecionado;
        public int Indice { get; private set; }
        public event Action<int> AoMudar;
        public event Action<int> AoConfirmar;

        public Navegacao(string classeSelecionado) { this.classeSelecionado = classeSelecionado; }

        public VisualElement Atual => Indice >= 0 && Indice < itens.Count ? itens[Indice] : null;
        public int Quantidade => itens.Count;

        public void Adicionar(VisualElement item)
        {
            int indice = itens.Count;
            itens.Add(item);
            item.pickingMode = PickingMode.Position;
            item.RegisterCallback<PointerEnterEvent>(_ => Selecionar(indice));
            item.RegisterCallback<ClickEvent>(_ => { Selecionar(indice); AoConfirmar?.Invoke(indice); });
            if (itens.Count == 1) Selecionar(0);
        }

        public void Limpar()
        {
            itens.Clear();
            Indice = 0;
        }

        public void Selecionar(int indice)
        {
            if (itens.Count == 0) return;
            indice = Mathf.Clamp(indice, 0, itens.Count - 1);
            for (int i = 0; i < itens.Count; i++) itens[i].EnableInClassList(classeSelecionado, i == indice);
            bool mudou = indice != Indice;
            Indice = indice;
            if (mudou)
            {
                Sons.Tocar(TipoSom.MenuMover, 0.6f, 0.02f);
                AoMudar?.Invoke(indice);
            }
        }

        /// <summary>Anda para o item mais perto na direção (x: -1/1, y: 1 = para cima).</summary>
        public bool Mover(Vector2Int direcao, bool soEmLista = false)
        {
            if (itens.Count == 0 || direcao == Vector2Int.zero) return false;
            if (soEmLista)
            {
                int passo = direcao.y > 0 ? -1 : direcao.y < 0 ? 1 : 0;
                if (passo == 0) return false;
                Selecionar((Indice + passo + itens.Count) % itens.Count);
                return true;
            }
            Rect atual = itens[Indice].worldBound;
            Vector2 dir = new Vector2(direcao.x, -direcao.y); // tela: y para baixo
            int melhor = -1;
            float melhorNota = float.MaxValue;
            for (int i = 0; i < itens.Count; i++)
            {
                if (i == Indice) continue;
                Vector2 d = itens[i].worldBound.center - atual.center;
                float naDirecao = Vector2.Dot(d, dir);
                if (naDirecao <= 1f) continue;
                float deLado = Mathf.Abs(dir.x != 0f ? d.y : d.x);
                float nota = naDirecao + deLado * 2.2f;
                if (nota < melhorNota) { melhorNota = nota; melhor = i; }
            }
            if (melhor < 0) return false;
            Selecionar(melhor);
            return true;
        }

        public void Confirmar()
        {
            Sons.Tocar(TipoSom.MenuConfirmar, 0.7f, 0f);
            AoConfirmar?.Invoke(Indice);
        }
    }
}
