"""Gera o logo BETS DE RUA (direção A, "Giz no asfalto") em SVG e PNG.

Rodar da raiz do repositório:
    uv run --no-project --with fonttools --with resvg-py python docs/marca/gerar_logo.py

As letras saem como contorno (path), então o SVG não depende de fonte instalada.
Fonte base: Permanent Marker (Apache 2.0, ver docs/marca/fontes/LICENSE.txt).
Tudo é determinístico (semente fixa): rodar de novo gera os mesmos arquivos.
"""

import math
import random
from pathlib import Path

from fontTools.pens.boundsPen import BoundsPen
from fontTools.pens.svgPathPen import SVGPathPen
from fontTools.pens.transformPen import TransformPen
from fontTools.ttLib import TTFont

PASTA = Path(__file__).parent
SAIDA = PASTA / "logo"
FONTE = PASTA / "fontes" / "PermanentMarker-Regular.ttf"

# Paleta do GDD ("Marca e identidade visual")
ASFALTO = "#2B2D31"
GIZ = "#F4F1E8"
LARANJA = "#FF7A1A"
AMARELO = "#E8E337"

fonte = TTFont(FONTE)
glifos = fonte.getGlyphSet()
mapa = fonte.getBestCmap()
ALTURA_MAIUSCULA = fonte["OS/2"].sCapHeight  # 758 unidades


def texto_em_path(texto, x, y_base, altura, espaco=0.0):
    """Converte texto em um único 'd' de path. altura = altura da maiúscula em px."""
    escala = altura / ALTURA_MAIUSCULA
    caneta = SVGPathPen(glifos)
    cursor = x
    for letra in texto:
        nome = mapa[ord(letra)]
        # fonte tem y para cima; SVG tem y para baixo
        glifos[nome].draw(TransformPen(caneta, (escala, 0, 0, -escala, cursor, y_base)))
        cursor += fonte["hmtx"][nome][0] * escala + espaco
    return caneta.getCommands(), cursor - espaco


def largura_texto(texto, altura, espaco=0.0):
    escala = altura / ALTURA_MAIUSCULA
    limites = BoundsPen(glifos)
    cursor = 0
    for letra in texto:
        nome = mapa[ord(letra)]
        glifos[nome].draw(TransformPen(limites, (escala, 0, 0, escala, cursor, 0)))
        cursor += fonte["hmtx"][nome][0] * escala + espaco
    x0, _, x1, _ = limites.bounds
    return x0, x1


def taco_exclamacao(cx, topo, altura, angulo):
    """Taco de pé de cabeça (pá em cima, cabo embaixo) fazendo o traço do '!'.
    Devolve (path do taco, lista de faixas de fita isolante como retângulos)."""
    larg_topo = altura * 0.27   # ponta da pá, um pouco mais larga
    larg_pa = altura * 0.24     # pá afina devagar até o cabo
    larg_cabo = altura * 0.13
    larg_pomo = altura * 0.19   # o "pomo" na ponta do cabo, como taco de verdade
    fim_pa = topo + altura * 0.55
    ini_cabo = topo + altura * 0.70
    base = topo + altura
    r = larg_topo * 0.22
    rp = altura * 0.035
    d = (
        f"M{cx - larg_topo/2 + r:.1f},{topo:.1f} "
        f"L{cx + larg_topo/2 - r:.1f},{topo:.1f} "
        f"Q{cx + larg_topo/2:.1f},{topo:.1f} {cx + larg_topo/2:.1f},{topo + r:.1f} "
        f"L{cx + larg_pa/2:.1f},{fim_pa:.1f} "
        f"C{cx + larg_pa/2:.1f},{fim_pa + altura*0.08:.1f} {cx + larg_cabo/2:.1f},{ini_cabo - altura*0.06:.1f} {cx + larg_cabo/2:.1f},{ini_cabo:.1f} "
        f"L{cx + larg_cabo/2:.1f},{base - rp*2:.1f} "
        f"L{cx + larg_pomo/2:.1f},{base - rp:.1f} "
        f"Q{cx + larg_pomo/2:.1f},{base:.1f} {cx + larg_pomo/2 - rp:.1f},{base:.1f} "
        f"L{cx - larg_pomo/2 + rp:.1f},{base:.1f} "
        f"Q{cx - larg_pomo/2:.1f},{base:.1f} {cx - larg_pomo/2:.1f},{base - rp:.1f} "
        f"L{cx - larg_cabo/2:.1f},{base - rp*2:.1f} "
        f"L{cx - larg_cabo/2:.1f},{ini_cabo:.1f} "
        f"C{cx - larg_cabo/2:.1f},{ini_cabo - altura*0.06:.1f} {cx - larg_pa/2:.1f},{fim_pa + altura*0.08:.1f} {cx - larg_pa/2:.1f},{fim_pa:.1f} "
        f"L{cx - larg_topo/2:.1f},{topo + r:.1f} "
        f"Q{cx - larg_topo/2:.1f},{topo:.1f} {cx - larg_topo/2 + r:.1f},{topo:.1f} Z"
    )
    # fita isolante: três frisos finos no cabo
    faixas = []
    inicio = ini_cabo + altura * 0.05
    passo = altura * 0.055
    for i in range(3):
        y = inicio + i * passo
        faixas.append((cx - larg_cabo, y, larg_cabo * 2, altura * 0.016))
    giro = f"rotate({angulo} {cx:.1f} {topo + altura/2:.1f})"
    return d, faixas, giro


def risco_laranja(x0, x1, y, espessura, curva):
    """Traço de pincel sublinhando: grosso no meio, afinando nas pontas."""
    pontos_cima, pontos_baixo = [], []
    n = 24
    for i in range(n + 1):
        t = i / n
        x = x0 + (x1 - x0) * t
        meio = y - curva * math.sin(math.pi * t) + (t - 0.5) * espessura * 0.9
        # começa cheio (onde o pincel encosta) e afina até a ponta
        meia = espessura / 2 * max(0.04, (1 - t) ** 0.45 * min(1, t * 10) ** 0.5)
        pontos_cima.append((x, meio - meia))
        pontos_baixo.append((x, meio + meia))
    pts = pontos_cima + pontos_baixo[::-1]
    return "M" + " L".join(f"{px:.1f},{py:.1f}" for px, py in pts) + " Z"


def textura_giz(x0, y0, x1, y1, semente, densidade=1.0):
    """Falhas de giz: pontinhos e riscos que viram 'buracos' na máscara."""
    rnd = random.Random(semente)
    area = (x1 - x0) * (y1 - y0)
    partes = []
    for _ in range(int(area / 95 * densidade)):
        x, y = rnd.uniform(x0, x1), rnd.uniform(y0, y1)
        raio = rnd.uniform(0.5, 1.9)
        partes.append(f'<circle cx="{x:.1f}" cy="{y:.1f}" r="{raio:.2f}"/>')
    for _ in range(int(area / 3000 * densidade)):
        x, y = rnd.uniform(x0, x1), rnd.uniform(y0, y1)
        comp = rnd.uniform(5, 18)
        ang = rnd.uniform(-20, 20)
        partes.append(
            f'<rect x="{x:.1f}" y="{y:.1f}" width="{comp:.1f}" height="{rnd.uniform(0.7, 1.4):.2f}" '
            f'rx="0.6" transform="rotate({ang:.1f} {x:.1f} {y:.1f})"/>'
        )
    return "\n      ".join(partes)


def montar_logo(cor_giz, cor_fundo, com_textura, contorno_bola=None, mono=None):
    """Logo principal 1200x800. mono = uma cor só para tudo (carimbo, bordado)."""
    L, A = 1200, 800
    alt_bets = 235
    espaco_bets = -6
    x0_b, x1_b = largura_texto("BETS", alt_bets, espaco_bets)
    larg_taco = alt_bets * 0.30
    folga = 28
    total = (x1_b - x0_b) + folga + larg_taco
    inicio = (L - total) / 2 - x0_b
    y_base_bets = 425

    d_bets, _ = texto_em_path("BETS", inicio, y_base_bets, alt_bets, espaco_bets)
    fim_bets = inicio + x1_b

    cx_taco = fim_bets + folga + larg_taco / 2
    topo_taco = y_base_bets - alt_bets - 18
    alt_taco = alt_bets * 0.9
    d_taco, faixas, giro = taco_exclamacao(cx_taco, topo_taco, alt_taco, 7)
    cy_bola = y_base_bets - alt_bets * 0.02
    r_bola = alt_bets * 0.115

    x_ini_risco = inicio + x0_b + 4
    x_fim_risco = cx_taco - r_bola * 1.4
    d_risco = risco_laranja(x_ini_risco, x_fim_risco, y_base_bets + 38, 36, 9)

    alt_rua = 84
    espaco_rua = 14
    x0_r, x1_r = largura_texto("DE RUA", alt_rua, espaco_rua)
    meio_bets = (x_ini_risco + x_fim_risco) / 2
    inicio_rua = meio_bets - (x1_r - x0_r) / 2 - x0_r
    d_rua, _ = texto_em_path("DE RUA", inicio_rua, y_base_bets + 168, alt_rua, espaco_rua)

    c_giz = mono or cor_giz
    c_lar = mono or LARANJA
    c_ama = mono or AMARELO

    fita = "\n      ".join(
        f'<rect x="{x:.1f}" y="{y:.1f}" width="{w:.1f}" height="{h:.1f}" transform="{giro}"/>'
        for x, y, w, h in faixas
    )
    furos = textura_giz(0, 0, L, A, semente=7, densidade=0.45) if com_textura else ""
    furos_cor = textura_giz(0, 0, L, A, semente=11, densidade=0.25) if com_textura else ""
    fundo = f'<rect width="{L}" height="{A}" fill="{cor_fundo}"/>' if cor_fundo else ""
    borda_bola = (
        f' stroke="{contorno_bola}" stroke-width="6"' if contorno_bola and not mono else ""
    )

    return f"""<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 {L} {A}" width="{L}" height="{A}">
  <title>BETS DE RUA</title>
  <defs>
    <mask id="giz" maskUnits="userSpaceOnUse" x="0" y="0" width="{L}" height="{A}">
      <rect width="{L}" height="{A}" fill="#fff"/>
      <g fill="#000">
      {fita}
      {furos}
      </g>
    </mask>
    <mask id="giz-cor" maskUnits="userSpaceOnUse" x="0" y="0" width="{L}" height="{A}">
      <rect width="{L}" height="{A}" fill="#fff"/>
      <g fill="#000">
      {furos_cor}
      </g>
    </mask>
  </defs>
  {fundo}
  <path d="{d_risco}" fill="{c_lar}" mask="url(#giz-cor)"/>
  <g fill="{c_giz}" mask="url(#giz)">
    <path d="{d_bets}"/>
    <path d="{d_taco}" transform="{giro}"/>
    <path d="{d_rua}"/>
  </g>
  <circle cx="{cx_taco + alt_taco*0.07:.1f}" cy="{cy_bola:.1f}" r="{r_bola:.1f}" fill="{c_ama}"{borda_bola} mask="url(#giz-cor)"/>
</svg>
"""


def montar_icone(com_textura):
    """Ícone quadrado: taco e bola dentro do círculo de giz. Pensado para ler em 32 px."""
    L = 1024
    c = L / 2
    d_taco, faixas, giro = taco_exclamacao(c - 30, 205, 560, -16)
    fita = "\n      ".join(
        f'<rect x="{x:.1f}" y="{y:.1f}" width="{w:.1f}" height="{h * 1.3:.1f}" transform="{giro}"/>'
        for x, y, w, h in faixas
    )
    furos = textura_giz(0, 0, L, L, semente=5, densidade=0.35) if com_textura else ""
    return f"""<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 {L} {L}" width="{L}" height="{L}">
  <title>BETS DE RUA</title>
  <defs>
    <mask id="giz" maskUnits="userSpaceOnUse" x="0" y="0" width="{L}" height="{L}">
      <rect width="{L}" height="{L}" fill="#fff"/>
      <g fill="#000">
      {fita}
      {furos}
      </g>
    </mask>
  </defs>
  <rect width="{L}" height="{L}" rx="{L*0.22:.0f}" fill="{ASFALTO}"/>
  <g mask="url(#giz)">
    <circle cx="{c}" cy="{c}" r="{L*0.385:.0f}" fill="none" stroke="{GIZ}" stroke-width="{L*0.055:.0f}"/>
    <path d="{d_taco}" fill="{GIZ}" transform="{giro}"/>
  </g>
  <circle cx="{c + 200:.0f}" cy="{c + 200:.0f}" r="{L*0.085:.0f}" fill="{AMARELO}"/>
</svg>
"""


def salvar(nome, svg, tamanhos):
    import resvg_py

    (SAIDA / f"{nome}.svg").write_text(svg, encoding="utf-8")
    for largura in tamanhos:
        png = resvg_py.svg_to_bytes(svg_string=svg, width=largura)
        (SAIDA / "png" / f"{nome}-{largura}.png").write_bytes(bytes(png))


if __name__ == "__main__":
    (SAIDA / "png").mkdir(parents=True, exist_ok=True)
    grandes = [600, 1200, 2400]
    salvar("logo-principal", montar_logo(GIZ, ASFALTO, True), grandes)
    salvar("logo-principal-limpo", montar_logo(GIZ, ASFALTO, False), grandes)
    salvar("logo-transparente", montar_logo(GIZ, None, True), grandes)
    salvar("logo-invertido", montar_logo(ASFALTO, GIZ, False, contorno_bola=ASFALTO), grandes)
    salvar("logo-mono-asfalto", montar_logo(None, None, False, mono=ASFALTO), grandes)
    salvar("logo-mono-giz", montar_logo(None, None, False, mono=GIZ), grandes)
    salvar("icone", montar_icone(True), [1024, 512, 256])
    salvar("icone-limpo", montar_icone(False), [256, 64, 48, 32, 16])
    print("Logo gerado em", SAIDA)
