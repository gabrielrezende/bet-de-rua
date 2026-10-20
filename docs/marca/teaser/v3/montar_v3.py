"""Teaser v3: monta os planos de v3/brutos com cortes secos, câmera lenta só nos momentos do jogo,
sem nenhum texto até o fecho, e som = ambiente de rua (dos próprios vídeos) + trilha.

    uv run --no-project --with pillow --with numpy python docs/marca/teaser/v3/montar_v3.py CAMINHO_FFMPEG

Reaproveita as funções da v2 (pedaços com velocidade, interpolação, contagem exata de quadros).
"""

import json
import subprocess
import sys
from pathlib import Path

AQUI = Path(__file__).parent
sys.path.insert(0, str(AQUI.parent))
import montar_v2 as v  # noqa: E402

v.BRUTOS = AQUI / "brutos"
v.TRAB = AQUI / "brutos" / "_trabalho"


def p(clip, ini, fim, vel=1.0):
    return (clip, ini, fim, vel)


def plano(id, *pedacos, impacto=None, **k):
    d = dict(id=id, pedacos=list(pedacos), **k)
    if impacto is not None:
        d["impacto"] = impacto
    return d


# (clip, início, fim, velocidade). impacto = índice do pedaço em que acontece o "tec!".
MONTAGEM = [
    dict(id="preto", tipo="preto", dur=1.0),
    # Ato 1 — a chegada
    plano("01", p("01", 0.8, 3.8), vol=0.7),
    plano("02", p("02", 0.0, 2.9), vol=0.7),
    plano("03", p("03", 3.4, 4.9), vol=0.8),
    plano("04", p("04", 1.7, 4.8), vol=0.7),
    plano("05", p("05", 2.3, 4.9), vol=0.6, efeitos={"preto_out"}),
    # Ato 2 — a rua cheia
    plano("06", p("06", 1.0, 4.6), vol=0.8, efeitos={"preto_in"}),
    plano("07", p("07", 0.6, 1.4), p("07", 1.4, 2.4, 0.4), p("07", 2.4, 3.4), impacto=1, vol=1.0),
    plano("08", p("08", 0.0, 3.0), vol=1.0, efeitos={"preto_out"}),
    # Ato 3 — o convite
    plano("09", p("09", 1.5, 4.5), vol=0.8, efeitos={"preto_in"}),
    plano("10", p("10", 1.0, 4.0), vol=0.7),
    plano("11", p("11", 1.2, 4.5), vol=0.6),
    plano("12", p("12", 0.3, 3.0), vol=0.6),
    # Ato 4 — aprendendo
    plano("13", p("13", 1.0, 3.2), vol=1.0),
    plano("14", p("14", 0.5, 2.5), p("14", 3.3, 4.8), vol=0.7),
    plano("15", p("15", 0.0, 2.7), vol=1.0, efeitos={"preto_out"}),
    # Ato 5 — o lance (cortes rápidos, como as câmeras do jogo)
    plano("16", p("16", 0.8, 2.8), vol=0.8, efeitos={"preto_in"}),
    plano("17", p("17", 2.6, 4.9), vol=0.8),
    plano("18", p("18", 0.8, 1.7), p("18", 1.7, 2.6, 0.3), p("18", 2.6, 4.0), impacto=1, vol=1.0),
    plano("19", p("19", 0.8, 3.6), vol=1.0),
    plano("20", p("20", 0.6, 1.8), p("20", 1.8, 2.6, 0.3), p("20", 2.6, 4.8), impacto=1, vol=1.0),
    plano("21", p("21", 1.5, 3.8), vol=1.0),
    plano("22", p("22", 0.5, 3.3), vol=1.0, efeitos={"preto_out"}),
    # Ato 6 — só mais uma
    plano("23", p("23", 1.0, 4.2), vol=0.8, efeitos={"preto_in"}),
    plano("24", p("24", 0.8, 3.4), vol=0.6),
    plano("25", p("25", 1.5, 4.8), vol=0.6, efeitos={"preto_out"}),
    dict(id="logo", tipo="logo", dur=3.8, flash_inicio=True),
    dict(id="voltou", tipo="cartao", texto="A rua voltou.", dur=2.6),
    dict(id="steam", tipo="steam", dur=3.4),
]

# Os tempos de impacto dentro de cada clipe (07, 18, 20) são acertados na revisão dos vídeos.


def main():
    v.TRAB.mkdir(parents=True, exist_ok=True)
    segs, inicio, t, impactos, flashes = [], {}, 0.0, {}, []
    for i, item in enumerate(MONTAGEM):
        arq, dur, imp = v.segmento(i, item)
        segs.append(arq)
        inicio[item["id"]] = t
        if "impacto" in item:
            impactos[item["id"]] = t + sum(v.dur_pedaco(x) for x in item["pedacos"][: item["impacto"]])
        if imp is not None and item.get("flash_inicio"):
            flashes.append(t)
        t += dur
        print(f"{item['id']:>6} {dur:5.2f}s  até {t:6.2f}s", flush=True)
    total = t

    lista = v.TRAB / "v3_lista.txt"
    lista.write_text("".join(f"file '{s.as_posix()}'\n" for s in segs), encoding="utf-8")
    juntado = v.TRAB / "v3_juntado.mov"
    v.rodar(["-f", "concat", "-safe", "0", "-i", str(lista), "-c", "copy", str(juntado)])

    s = inicio
    secoes = [
        [s["01"], s["06"], 0],     # chegada: violão solto
        [s["06"], s["09"], 1],     # rua cheia: dedilhado
        [s["09"], s["13"], 0],     # convite: volta ao solto
        [s["13"], s["16"], 1],     # aprendendo
        [s["16"], s["20"], 2],     # o lance: entra a percussão
        [s["20"], s["23"], 3],     # cruzada e festa: batida cheia
        [s["23"], s["logo"], 1],   # noite e sofá
        [s["logo"] + 0.4, total, 0],
    ]
    tecs = [0.3, impactos["20"], s["logo"]]
    pratos = [impactos["20"]]
    trilha = v.TRAB / "v3_trilha.wav"
    subprocess.run([sys.executable, str(AQUI.parent / "trilha.py"), str(trilha), str(total),
                    json.dumps(secoes), json.dumps(pratos), json.dumps(tecs)], check=True)

    termos = "+".join(f"gte(t,{tf:.3f})*max(0,1-(t-{tf:.3f})/0.35)" for tf in flashes) or "0"
    final = AQUI / "teaser_a_rua_nova_v3.mp4"
    v.rodar(["-i", str(juntado), "-i", str(trilha), "-filter_complex",
             f"[0:v]eq=eval=frame:brightness='0.75*({termos})',format=yuv420p[v];"
             "[1:a]volume=0.5[m];[0:a][m]amix=inputs=2:duration=first:normalize=0,"
             "alimiter=limit=0.95,loudnorm=I=-15:TP=-1.5:LRA=11,aresample=48000[a]",
             "-map", "[v]", "-map", "[a]", "-c:v", "libx264", "-preset", "slow", "-crf", "20",
             "-movflags", "+faststart", "-c:a", "aac", "-b:a", "192k", "-ar", "48000", str(final)])
    print("pronto:", final, f"{total:.1f}s", "impactos:", {k: round(x, 2) for k, x in impactos.items()})


if __name__ == "__main__":
    main()
