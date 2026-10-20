"""Monta o teaser "A rua nova" a partir dos planos gerados (brutos/NN.mp4).

    uv run --no-project --with pillow --with numpy python docs/marca/teaser/montar.py CAMINHO_FFMPEG

Etapas: 1) corta e padroniza cada plano (1920x1080, 24 fps, áudio 48 kHz), com câmera lenta
quando pedido; 2) desenha os letreiros de giz em PNG e põe por cima; 3) junta tudo;
4) gera a trilha (trilha.py) e mistura com o som de rua de cada plano.
Plano sem vídeo ainda vira o quadro parado com um zoom lento, para a montagem nunca travar.
"""

import json
import random
import subprocess
import sys
from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter, ImageFont

PASTA = Path(__file__).parent
BRUTOS = PASTA / "brutos"
QUADROS = PASTA / "quadros"
TRAB = PASTA / "brutos" / "_trabalho"
FONTE = PASTA.parent / "fontes" / "PermanentMarker-Regular.ttf"
LOGO = PASTA.parent / "logo" / "png" / "logo-transparente-2400.png"
FF = sys.argv[1] if len(sys.argv) > 1 else "ffmpeg"

GIZ = (244, 241, 232)
LARANJA = (255, 122, 26)
ASFALTO = (43, 45, 49)

# (plano, início no bruto, duração final, velocidade, letreiro, volume do som do plano)
# velocidade < 1 = câmera lenta. plano "preto"/"cartao:..." = tela de texto.
MONTAGEM = [
    ("preto", 0, 1.2, 1, None, 0),
    ("02", 0.3, 3.4, 1, None, 0.8),
    ("03", 0.0, 4.0, 1, None, 0.7),
    ("04", 0.5, 3.2, 1, None, 0.8),
    ("05", 0.0, 2.6, 1, None, 0.8),
    ("06", 0.0, 2.6, 0.6, None, 1.0),
    ("07", 0.6, 2.8, 1, ("O que é isso?", "fala"), 0.6),
    ("08", 0.4, 3.4, 1, None, 0.8),
    ("09", 0.0, 2.6, 0.75, ("QUEIMOU!", "grito"), 1.0),
    ("10", 0.3, 2.6, 1, None, 1.0),
    ("cartao:Um dia, faltou gente.", 0, 2.4, 1, None, 0),
    ("11", 0.5, 3.0, 1, None, 0.9),
    ("12", 0.2, 2.6, 1, ("Ei! Falta dois. Bora?", "grito"), 0.7),
    ("13", 0.5, 2.6, 1, ("A gente não sabe jogar...", "fala"), 0.6),
    ("14", 0.0, 2.6, 1, ("Ninguém nasce sabendo.", "fala"), 0.7),
    ("15", 0.0, 1.8, 1, None, 1.0),
    ("16", 0.3, 1.8, 1, ("Taco no chão!", "fala"), 1.0),
    ("17", 0.3, 3.0, 1, None, 1.0),
    ("18", 0.6, 1.9, 1, None, 0.8),
    ("19", 0.3, 2.0, 0.8, None, 0.8),
    ("20", 0.2, 3.0, 0.6, None, 1.0),
    ("21", 0.2, 2.4, 1, ("CORRE!", "grito"), 1.0),
    ("22", 0.0, 5.6, 0.7, None, 1.0),
    ("23", 0.0, 2.4, 1, None, 0.8),
    ("24", 0.3, 2.2, 1, ("Janta!", "grito"), 0.7),
    ("23", 2.4, 1.7, 1, ("Só mais uma!", "grito"), 0.8),
    ("25", 0.4, 3.6, 1, None, 0.7),
    ("logo", 0, 4.5, 1, None, 0),
    ("cartao:A rua voltou.", 0, 2.6, 1, None, 0),
    ("cartao_steam", 0, 3.4, 1, None, 0),
]

# momentos de "tec!" forte (prato + surdo na trilha) e seções da música, em segundos
def tempos():
    t, marcas = 0.0, {}
    for i, (plano, _, dur, *_r) in enumerate(MONTAGEM):
        marcas.setdefault(plano, []).append(t)
        t += dur
    return t, marcas


def rodar(args):
    r = subprocess.run([FF, "-hide_banner", "-loglevel", "error", "-y", *args], capture_output=True, text=True)
    if r.returncode:
        raise RuntimeError(r.stderr[-2000:])


def letreiro(texto, estilo, caminho):
    """Letra de giz com falhas, levemente torta, sublinhado laranja nos gritos."""
    L, A = 1920, 1080
    img = Image.new("RGBA", (L, A), (0, 0, 0, 0))
    tamanho = 150 if estilo == "grito" else 108
    fonte = ImageFont.truetype(str(FONTE), tamanho)
    camada = Image.new("RGBA", (L, A), (0, 0, 0, 0))
    d = ImageDraw.Draw(camada)
    x, y = 110, A - 300 if estilo == "fala" else A - 360
    caixa = d.textbbox((x, y), texto, font=fonte)
    # sombra suave para ler em cima de céu claro
    sombra = Image.new("RGBA", (L, A), (0, 0, 0, 0))
    ImageDraw.Draw(sombra).text((x + 4, y + 6), texto, font=fonte, fill=(0, 0, 0, 170))
    sombra = sombra.filter(ImageFilter.GaussianBlur(10))
    d.text((x, y), texto, font=fonte, fill=GIZ + (255,))
    if estilo == "grito":
        y0 = caixa[3] + 14
        pts = [(caixa[0] + 6, y0 + 4), (caixa[2] - 10, y0 - 6), (caixa[2] - 14, y0 + 8), (caixa[0] + 2, y0 + 16)]
        d.polygon(pts, fill=LARANJA + (255,))
    # falhas de giz
    rnd = random.Random(hash(texto) & 0xFFFF)
    furos = Image.new("L", (L, A), 255)
    df = ImageDraw.Draw(furos)
    for _ in range(int((caixa[2] - caixa[0]) * (caixa[3] - caixa[1] + 40) / 70)):
        px, py = rnd.uniform(caixa[0], caixa[2]), rnd.uniform(caixa[1], caixa[3] + 30)
        r = rnd.uniform(0.6, 2.2)
        df.ellipse((px - r, py - r, px + r, py + r), fill=0)
    alfa = Image.composite(camada.getchannel("A"), Image.new("L", (L, A), 0), furos)
    camada.putalpha(alfa)
    camada = camada.rotate(rnd.uniform(-2.5, 1.0), center=(x, y), resample=Image.BICUBIC)
    img = Image.alpha_composite(img, sombra)
    img = Image.alpha_composite(img, camada)
    img.save(caminho)


def cartao(texto, caminho, subtitulo=None):
    L, A = 1920, 1080
    img = Image.new("RGB", (L, A), (12, 12, 13))
    d = ImageDraw.Draw(img)
    fonte = ImageFont.truetype(str(FONTE), 130)
    caixa = d.textbbox((0, 0), texto, font=fonte)
    d.text(((L - caixa[2]) / 2, (A - caixa[3]) / 2 - 30), texto, font=fonte, fill=GIZ)
    if subtitulo:
        f2 = ImageFont.truetype(str(FONTE), 72)
        c2 = d.textbbox((0, 0), subtitulo, font=f2)
        d.text(((L - c2[2]) / 2, (A + caixa[3]) / 2 + 40), subtitulo, font=f2, fill=LARANJA)
    img.save(caminho)


def cartao_logo(caminho):
    L, A = 1920, 1080
    img = Image.new("RGB", (L, A), ASFALTO)
    logo = Image.open(LOGO).convert("RGBA")
    logo = logo.resize((1500, 1000))
    img.paste(logo, ((L - 1500) // 2, (A - 1000) // 2), logo)
    img.save(caminho)


VIDEO_PADRAO = "scale=1920:1080:force_original_aspect_ratio=increase,crop=1920:1080,setsar=1,fps=24,format=yuv420p"


def segmento(i, item):
    plano, ini, dur, vel, texto, vol = item
    saida = TRAB / f"seg_{i:02d}.mp4"
    entradas, filtro = [], []
    if plano == "preto":
        entradas = ["-f", "lavfi", "-i", f"color=c=black:s=1920x1080:r=24:d={dur}"]
        filtro.append(f"[0:v]{VIDEO_PADRAO}[v]")
    elif plano.startswith("cartao") or plano == "logo":
        png = TRAB / f"cartao_{i:02d}.png"
        if plano == "logo":
            cartao_logo(png)
        elif plano == "cartao_steam":
            cartao("Lista de desejos na Steam", png, "Em breve no PC")
        else:
            cartao(plano.split(":", 1)[1], png)
        entradas = ["-loop", "1", "-t", str(dur), "-i", str(png)]
        filtro.append(
            f"[0:v]{VIDEO_PADRAO},zoompan=z='1+0.0006*on':d=1:s=1920x1080:fps=24,"
            f"fade=in:st=0:d=0.5,fade=out:st={dur - 0.5}:d=0.5[v]"
        )
    else:
        video = BRUTOS / f"{plano}.mp4"
        if video.exists():
            entradas = ["-ss", str(ini), "-i", str(video)]
            fator = 1 / vel
            filtro.append(f"[0:v]setpts={fator}*(PTS-STARTPTS),{VIDEO_PADRAO},trim=duration={dur}[v0]")
            # leve unificação de cor: contraste e um tom quente
            filtro.append("[v0]eq=contrast=1.05:saturation=1.05,colorbalance=rs=0.02:bs=-0.02[v1]")
        else:
            entradas = ["-loop", "1", "-t", str(dur), "-i", str(QUADROS / f"{plano}.png")]
            filtro.append(f"[0:v]{VIDEO_PADRAO},zoompan=z='1+0.001*on':d=1:s=1920x1080:fps=24,trim=duration={dur}[v1]")
        ultimo = "v1"
        if texto:
            png = TRAB / f"texto_{i:02d}.png"
            letreiro(texto[0], texto[1], png)
            entradas += ["-loop", "1", "-i", str(png)]
            filtro.append(f"[1:v]format=rgba,fade=in:st=0.15:d=0.25:alpha=1[t]")
            filtro.append(f"[{ultimo}][t]overlay=0:0:shortest=1,trim=duration={dur}[v]")
        else:
            filtro.append(f"[{ultimo}]null[v]")
    # áudio do plano (som de rua gerado junto) ou silêncio
    tem_audio = plano not in ("preto", "logo") and not plano.startswith("cartao") and (BRUTOS / f"{plano}.mp4").exists()
    if tem_audio:
        tempo_audio = "atempo=0.5,atempo=" + str(round(vel / 0.5, 4)) if vel < 0.5 else f"atempo={vel}"
        filtro.append(
            f"[0:a]{tempo_audio},aresample=48000,aformat=channel_layouts=stereo,volume={vol},"
            f"atrim=duration={dur},afade=in:d=0.08,afade=out:st={dur - 0.15}:d=0.15,apad=whole_dur={dur}[a]"
        )
        mapa_a = ["-map", "[a]"]
    else:
        entradas += ["-f", "lavfi", "-t", str(dur), "-i", "anullsrc=r=48000:cl=stereo"]
        mapa_a = ["-map", f"{len([e for e in entradas if e == '-i']) - 1}:a"]
    rodar(
        [*entradas, "-filter_complex", ";".join(filtro), "-map", "[v]", *mapa_a,
         "-t", str(dur), "-c:v", "libx264", "-preset", "medium", "-crf", "18", "-r", "24",
         "-c:a", "pcm_s16le", "-ar", "48000", "-ac", "2", str(saida)]
    )
    return saida


def main():
    TRAB.mkdir(parents=True, exist_ok=True)
    segs = [segmento(i, item) for i, item in enumerate(MONTAGEM)]
    lista = TRAB / "lista.txt"
    lista.write_text("".join(f"file '{s.as_posix()}'\n" for s in segs), encoding="utf-8")
    juntado = TRAB / "juntado.mov"
    rodar(["-f", "concat", "-safe", "0", "-i", str(lista), "-c", "copy", str(juntado)])

    total, marcas = tempos()
    t = lambda p, k=0: marcas[p][k]
    secoes = [
        [t("02"), t("08"), 0],
        [t("08"), t("cartao:Um dia, faltou gente."), 2],
        [t("12"), t("17"), 1],
        [t("17"), t("21"), 2],
        [t("21"), t("23"), 3],
        [t("23"), t("logo"), 1],
        [t("logo") + 0.4, total, 0],
    ]
    pratos = [t("22") + 0.9]
    tecs = [0.35, t("22") + 0.9, t("logo") + 0.1]
    trilha = TRAB / "trilha.wav"
    subprocess.run(
        [sys.executable, str(PASTA / "trilha.py"), str(trilha), str(total), json.dumps(secoes), json.dumps(pratos), json.dumps(tecs)],
        check=True,
    )
    final = PASTA / "teaser_a_rua_nova.mp4"
    rodar(
        ["-i", str(juntado), "-i", str(trilha), "-filter_complex",
         "[1:a]volume=0.55[m];[0:a][m]amix=inputs=2:duration=first:normalize=0,"
         "alimiter=limit=0.95,loudnorm=I=-15:TP=-1.5:LRA=11[a]",
         "-map", "0:v", "-map", "[a]", "-c:v", "libx264", "-preset", "slow", "-crf", "20",
         "-pix_fmt", "yuv420p", "-movflags", "+faststart", "-c:a", "aac", "-b:a", "192k", "-ar", "48000", str(final)]
    )
    print("pronto:", final, f"{total:.1f}s")


if __name__ == "__main__":
    main()
