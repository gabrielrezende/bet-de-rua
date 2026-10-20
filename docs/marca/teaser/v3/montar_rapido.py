"""Teaser rápido no estilo 2K26: cortes de 1 a 1,5 s no tempo da música (120 bpm),
frases grandes de giz entre os lances e câmera lenta só nos impactos.
Usa os vídeos da v3 (brutos/NN.mp4), escolhendo só o melhor segundo de cada um.

    uv run --no-project --with pillow --with numpy python docs/marca/teaser/v3/montar_rapido.py CAMINHO_FFMPEG
"""

import json
import os
import random
import subprocess
import sys
from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter, ImageFont

AQUI = Path(__file__).parent
sys.path.insert(0, str(AQUI.parent))
import montar_v2 as v  # noqa: E402

v.BRUTOS = AQUI / "brutos"
v.TRAB = AQUI / "brutos" / "_rapido"
FONTE = AQUI.parent.parent / "fontes" / "PermanentMarker-Regular.ttf"
GIZ, LARANJA = (244, 241, 232), (255, 122, 26)
BPM = 120
T = 60 / BPM  # um tempo = 0,5 s


def letreiro_grande(texto, estilo, caminho):
    """Frase de impacto no centro da tela, estilo 2K26, com textura de giz e risco laranja."""
    L, A = 1920, 1080
    tam = 300 if max(len(x) for x in texto.split("|")) <= 8 else 240
    d0 = ImageDraw.Draw(Image.new("RGBA", (1, 1)))
    linhas = texto.split("|")
    while True:  # diminui até caber com folga na largura
        fonte = ImageFont.truetype(str(FONTE), tam)
        if max(d0.textbbox((0, 0), ln, font=fonte)[2] for ln in linhas) <= 1650 or tam <= 60:
            break
        tam -= 10
    caixas = [d0.textbbox((0, 0), ln, font=fonte) for ln in linhas]
    alt = sum(c[3] - c[1] for c in caixas) + 20 * (len(linhas) - 1)
    camada = Image.new("RGBA", (L, A), (0, 0, 0, 0))
    d = ImageDraw.Draw(camada)
    y = (A - alt) / 2 - 30
    for ln, c in zip(linhas, caixas):
        x = (L - (c[2] - c[0])) / 2 - c[0]
        d.text((x, y - c[1]), ln, font=fonte, fill=GIZ + (255,))
        y += c[3] - c[1] + 20
    x0 = (L - max(c[2] - c[0] for c in caixas)) / 2
    x1 = L - x0
    d.polygon([(x0, y + 8), (x1 - 10, y - 4), (x1 - 16, y + 18), (x0 - 4, y + 26)], fill=LARANJA + (255,))
    rnd = random.Random(len(texto))
    furos = Image.new("L", (L, A), 255)
    df = ImageDraw.Draw(furos)
    for _ in range(9000):
        px, py, r = rnd.uniform(0, L), rnd.uniform(0, A), rnd.uniform(0.6, 2.4)
        df.ellipse((px - r, py - r, px + r, py + r), fill=0)
    camada.putalpha(Image.composite(camada.getchannel("A"), Image.new("L", (L, A), 0), furos))
    camada = camada.rotate(-3, center=(L / 2, A / 2), resample=Image.BICUBIC)
    sombra = Image.new("RGBA", (L, A), (0, 0, 0, 0))
    sombra.putalpha(camada.getchannel("A").filter(ImageFilter.GaussianBlur(18)).point(lambda a: int(a * 0.75)))
    Image.alpha_composite(sombra, camada).save(caminho)


v.letreiro = letreiro_grande


def cartao_grande(texto, caminho, subtitulo=None):
    """Cartão final: fundo quase preto com a frase grande de giz."""
    png = Path(str(caminho) + ".txt.png")
    if texto.startswith("Lista de desejos"):  # o cartão da Steam vem da v2 com o texto corrido
        texto = "LISTA DE DESEJOS|NA STEAM"
    letreiro_grande(texto, "grande", png)
    fundo = Image.new("RGBA", (1920, 1080), (12, 12, 13, 255))
    img = Image.alpha_composite(fundo, Image.open(png))
    if subtitulo:
        f2 = ImageFont.truetype(str(FONTE), 80)
        d = ImageDraw.Draw(img)
        cx = d.textbbox((0, 0), subtitulo, font=f2)
        d.text(((1920 - cx[2]) / 2, 820), subtitulo, font=f2, fill=LARANJA)
    img.convert("RGB").save(caminho)


v.cartao = cartao_grande  # a v2 chama letreiro() para os textos; aqui viram frases grandes


def p(clip, ini, dur_final, vel=1.0):
    """Pedaço que dura exatamente dur_final segundos na montagem."""
    return (clip, ini, ini + dur_final * vel, vel)


def c(id, *pedacos, **k):
    return dict(id=id, pedacos=list(pedacos), vol=k.pop("vol", 0.9), **k)


UM, UMEMEIO, DOIS = 2 * T, 3 * T, 4 * T
MONTAGEM = [
    dict(id="preto", tipo="preto", dur=DOIS),
    # abertura: o olhar, o bairro, a rua
    c("01", p("01", 1.0, UMEMEIO)),
    c("02", p("02", 0.2, UM)),
    c("03", p("03", 3.5, UM)),
    c("13", p("13", 1.6, UM), texto=("TACO|NO CHÃO", "grande")),
    c("14", p("14", 3.4, UM)),
    c("16", p("16", 0.8, UM)),
    c("17", p("17", 3.8, UM)),
    # 1º impacto: a batida em câmera lenta
    c("18", p("18", 1.6, UM, 0.4), p("18", 2.0, UM)),
    c("19", p("19", 1.2, UM), texto=("CORRE...", "grande")),
    # 2º impacto: a cruzada
    c("20", p("20", 1.2, UM), p("20", 2.2, DOIS, 0.3), flash_impacto=True),
    c("titulo1", p("21", 2.6, UMEMEIO), texto=("...E CRUZA!", "grande")),
    c("22", p("22", 1.2, UMEMEIO)),
    # virada: o outro lado também joga — a queimada
    c("07", p("07", 1.0, UM), p("07", 1.4, UMEMEIO, 0.4)),
    c("08", p("08", 3.2, UMEMEIO), texto=("QUEIMOU!", "grande")),
    c("12", p("12", 3.0, UM)),
    c("11", p("11", 3.5, UM)),
    c("10", p("10", 2.2, UM)),
    c("23", p("23", 1.6, UMEMEIO), texto=("SÓ MAIS|UMA!", "grande")),
    c("25", p("25", 3.2, UMEMEIO)),
    dict(id="logo", tipo="logo", dur=3 * UM, flash_inicio=True),
    dict(id="voltou", tipo="cartao", texto="A RUA|VOLTOU.", dur=DOIS),
    dict(id="steam", tipo="steam", dur=3 * UM),
]


def main():
    v.TRAB.mkdir(parents=True, exist_ok=True)
    segs, t, flashes, marcas = [], 0.0, [], {}
    for i, item in enumerate(MONTAGEM):
        arq, dur, _ = v.segmento(i, item)
        segs.append(arq)
        marcas[item["id"]] = t
        if item.get("flash_inicio"):
            flashes.append(t)
        if item.get("flash_impacto"):
            flashes.append(t + v.dur_pedaco(item["pedacos"][0]))
        t += dur
        print(f"{item['id']:>8} {dur:5.2f}s até {t:6.2f}s", flush=True)
    total = t
    lista = v.TRAB / "lista.txt"
    lista.write_text("".join(f"file '{s.as_posix()}'\n" for s in segs), encoding="utf-8")
    juntado = v.TRAB / "juntado.mov"
    # junta decodificando cada pedaço (filtro concat): o concat por cópia perdia quadros
    entradas, rotulos = [], ""
    for k, sg in enumerate(segs):
        entradas += ["-i", str(sg)]
        rotulos += f"[{k}:v][{k}:a]"
    v.rodar([*entradas, "-filter_complex", f"{rotulos}concat=n={len(segs)}:v=1:a=1[v][a]",
             "-map", "[v]", "-map", "[a]", "-c:v", "libx264", "-preset", "medium", "-crf", "14",
             "-pix_fmt", "yuv420p", "-c:a", "pcm_s16le", str(juntado)])

    m = marcas
    secoes = [[0.0, m["18"], 2], [m["18"], m["logo"], 3], [m["logo"] + 0.5, total, 0]]
    tecs = [0.0, m["18"] + 0.75, *flashes]
    trilha = v.TRAB / "trilha.wav"
    amb = dict(os.environ, TRILHA_BPM=str(BPM))
    subprocess.run([sys.executable, str(AQUI.parent / "trilha.py"), str(trilha), str(total),
                    json.dumps(secoes), json.dumps(flashes), json.dumps(tecs)], check=True, env=amb)
    termos = "+".join(f"gte(t,{f:.3f})*max(0,1-(t-{f:.3f})/0.3)" for f in flashes) or "0"
    final = AQUI / "teaser_rapido.mp4"
    v.rodar(["-i", str(juntado), "-i", str(trilha), "-filter_complex",
             f"[0:v]eq=eval=frame:brightness='0.8*({termos})',format=yuv420p[v];"
             "[1:a]volume=0.9[mus];[0:a]volume=0.45[amb];[amb][mus]amix=inputs=2:duration=first:normalize=0,"
             "alimiter=limit=0.95,loudnorm=I=-14:TP=-1.5:LRA=9,aresample=48000[a]",
             "-map", "[v]", "-map", "[a]", "-c:v", "libx264", "-preset", "slow", "-crf", "20",
             "-movflags", "+faststart", "-c:a", "aac", "-b:a", "192k", "-ar", "48000", str(final)])
    print("pronto:", final, f"{total:.1f}s", "clarões:", [round(f, 2) for f in flashes])


if __name__ == "__main__":
    main()
