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
}
