"""Teaser v2: rampas de câmera lenta nos impactos, interpolação de movimento e transições.

    uv run --no-project --with pillow --with numpy python docs/marca/teaser/montar_v2.py CAMINHO_FFMPEG

Diferenças para a v1 (montar.py, de onde vêm os letreiros e cartões):
- Cada plano é feito de "pedaços" (vídeo, início, fim, velocidade). A rampa é uma escada de
  velocidades (1 → 0,5 → 0,2 → 0,4 → 0,7 → 1) que emenda o vídeo de aproximação (NNi.mp4,
  que termina no quadro do impacto) com o vídeo antigo (que começa nesse mesmo quadro).
- Pedaço lento ganha quadros intermediários (minterpolate) em vez de quadro repetido.
- No pedaço lento o som de rua sai; fica a trilha com o "tec!" exatamente no impacto.
- Transições: clarão branco no impacto, mergulho no preto, chicote (borrão lateral) e cortes no tempo da música.
"""

import json
import subprocess
import sys

import montar as m
from montar import BRUTOS, PASTA, TRAB, VIDEO_PADRAO, cartao, cartao_logo, letreiro, rodar

COR = "eq=contrast=1.05:saturation=1.05,colorbalance=rs=0.02:bs=-0.02"
BATIDA = 60 / 92  # um tempo da trilha (92 bpm)


def rampa(antes, a_ini, a_fim, depois, d_fim, d_ini=0.0):
    """Aproximação em velocidade normal, freia no impacto, volta ao normal.
    O impacto é o começo do terceiro pedaço (índice 2)."""
    pedacos = [
        (antes, a_ini, a_fim - 0.2, 1.0),
        (antes, a_fim - 0.2, a_fim, 0.5),
        (depois, d_ini, d_ini + 0.3, 0.2),  # o impacto: quase congelado
        (depois, d_ini + 0.3, d_ini + 0.5, 0.4),
        (depois, d_ini + 0.5, min(d_ini + 0.8, d_fim), 0.7),
    ]
    if d_fim > d_ini + 0.8:
        pedacos.append((depois, d_ini + 0.8, d_fim, 1.0))
    return pedacos


def plano(id, clip, ini, fim, **k):
    return dict(id=id, pedacos=[(clip, ini, fim, 1.0)], **k)


def lento(id, pedacos, **k):
    return dict(id=id, pedacos=pedacos, impacto=2, **k)


MONTAGEM = [
    dict(id="preto", tipo="preto", dur=1.2),
    plano("02", "02", 0.3, 3.7),
    plano("03", "03", 0.0, 4.0),
    plano("04", "04", 0.5, 3.7),
    plano("05", "05", 0.0, 2.6),
    # depois do toque o vídeo antigo é quase parado: segura só ~1 s e segue
    lento("06", [("06i", 0.9, 1.35, 1.0), ("06i", 1.35, 1.55, 0.5), ("06", 0.0, 0.2, 0.25), ("06", 0.2, 0.45, 0.5)],
          flash=True, vol=1.0),
    plano("07", "07", 0.6, 3.4, texto=("O que é isso?", "fala"), vol=0.6),
    plano("08", "08", 0.4, 3.8),
    dict(id="09", impacto=2, pedacos=[("09i", 0.9, 1.45, 1.0), ("09i", 1.45, 1.6, 0.5), ("09i", 1.6, 1.85, 0.25),
                  ("09i", 1.85, 2.1, 0.5), ("09i", 2.1, 2.9, 1.3), ("09", 0.0, 2.2, 1.6)], flash=True, texto=("QUEIMOU!", "grito"), texto_no_impacto=True),
    plano("10", "10", 0.3, 2.9, efeitos={"preto_out"}),
    dict(id="um_dia", tipo="cartao", texto="Um dia, faltou gente.", dur=2.4),
    plano("11", "11", 0.5, 3.5, efeitos={"preto_in"}, vol=0.9),
    plano("12", "12", 0.2, 2.8, texto=("Ei! Falta dois. Bora?", "grito"), vol=0.7),
    plano("13", "13", 0.5, 3.1, texto=("A gente não sabe jogar...", "fala"), vol=0.6),
    plano("14", "14", 0.0, 2.6, texto=("Ninguém nasce sabendo.", "fala"), vol=0.7),
    plano("15", "15", 0.0, 1.8),
    plano("16", "16", 0.3, 2.1, texto=("Taco no chão!", "fala")),
    plano("17", "17", 0.3, 3.3),
    # arremesso e bola vindo: cortes no tempo da música (2 tempos cada)
    plano("18", "18", 0.6, 0.6 + 2 * BATIDA, vol=0.8),
    plano("19", "19", 0.3, 0.3 + 2 * BATIDA, vol=0.8),
    lento("20", [("20i", 0.0, 0.8, 1.0), ("20i", 0.8, 1.0, 0.5)] + rampa("20i", 0, 0.2, "20", 0.8)[2:], flash=True, efeitos={"chicote_out"}),
    # a corrida freia no fim e corta seco (com clarão) para os tacos batendo: corte na ação
    dict(id="21", pedacos=[("21", 0.2, 2.3, 1.0), ("21", 2.3, 2.6, 0.5)],
         texto=("CORRE!", "grito"), efeitos={"chicote_in"}),
    dict(id="22", pedacos=rampa(None, 0, 0, "22", 2.6)[2:], impacto=0, flash=True, efeitos={"preto_out"}),
    plano("23", "23", 0.0, 2.4, efeitos={"preto_in"}, vol=0.8),
    plano("24", "24", 0.3, 2.5, texto=("Janta!", "grito"), vol=0.7),
    plano("23b", "23", 2.4, 4.1, texto=("Só mais uma!", "grito"), vol=0.8),
    plano("25", "25", 0.4, 4.0, efeitos={"preto_out"}, vol=0.7),
    dict(id="logo", tipo="logo", dur=4.5, flash_inicio=True),
    dict(id="voltou", tipo="cartao", texto="A rua voltou.", dur=2.6),
    dict(id="steam", tipo="steam", dur=3.4),
]


def dur_pedaco(p):
    return (p[2] - p[1]) / p[3]


def em_quadros(d):
    """Arredonda para um número inteiro de quadros (24 fps), para som e imagem não se separarem."""
    return round(d * 24) / 24


def render_pedaco(nome, p, vol):
    clip, ini, fim, vel = p
    saida = TRAB / f"{nome}.mp4"
    dur = dur_pedaco(p)
    entradas = ["-ss", f"{ini:.3f}", "-t", f"{fim - ini:.3f}", "-i", str(BRUTOS / f"{clip}.mp4")]
    if vel >= 0.95:
        filtro = [f"[0:v]setpts=(PTS-STARTPTS)/{vel},{VIDEO_PADRAO},{COR},setpts=N/24/TB[v]",
                  f"[0:a]atempo={vel},aresample=48000,aformat=channel_layouts=stereo,volume={vol},apad[a]"]
    else:
        fps_alvo = round(24 / vel)
        filtro = [f"[0:v]setpts=PTS-STARTPTS,{VIDEO_PADRAO},"
                  f"minterpolate=fps={fps_alvo}:mi_mode=mci:mc_mode=aobmc:me_mode=bidir:vsbmc=1,"
                  f"setpts=PTS/{vel},fps=24,{COR},setpts=N/24/TB[v]",
                  "anullsrc=r=48000:cl=stereo[a]"]
    rodar([*entradas, "-filter_complex", ";".join(filtro), "-map", "[v]", "-map", "[a]",
           "-t", f"{dur:.3f}", "-c:v", "libx264", "-preset", "medium", "-crf", "16", "-r", "24", "-pix_fmt", "yuv420p",
           "-c:a", "pcm_s16le", "-ar", "48000", "-ac", "2", str(saida)])
    return saida


def segmento(i, item):
    """Devolve (arquivo, duração, tempo do impacto dentro do segmento ou None)."""
    saida = TRAB / f"v2_seg_{i:02d}.mp4"
    tipo = item.get("tipo", "video")
    if tipo != "video":
        dur = em_quadros(item["dur"])
        if tipo == "preto":
            entradas = ["-f", "lavfi", "-i", f"color=c=black:s=1920x1080:r=24:d={dur}"]
            filtro = f"[0:v]{VIDEO_PADRAO}[v]"
        else:
            png = TRAB / f"v2_cartao_{i:02d}.png"
            if tipo == "logo":
                cartao_logo(png)
            elif tipo == "steam":
                cartao("Lista de desejos na Steam", png, "Em breve no PC")
            else:
                cartao(item["texto"], png)
            entradas = ["-loop", "1", "-t", str(dur), "-i", str(png)]
            entra = "" if tipo == "logo" else "fade=in:st=0:d=0.5,"  # o logo entra seco, com o clarão
            filtro = (f"[0:v]{VIDEO_PADRAO},zoompan=z='1+0.0006*on':d=1:s=1920x1080:fps=24,"
                      f"{entra}fade=out:st={dur - 0.5}:d=0.5[v]")
        filtro = filtro.replace("[v]", f",setpts=N/24/TB,tpad=stop_mode=clone:stop=30,trim=end_frame={round(dur * 24)}[v]")
        rodar([*entradas, "-f", "lavfi", "-t", str(dur), "-i", "anullsrc=r=48000:cl=stereo",
               "-filter_complex", filtro, "-map", "[v]", "-map", "1:a",
               "-fps_mode", "passthrough",
               "-c:v", "libx264", "-preset", "medium", "-crf", "16", "-pix_fmt", "yuv420p",
               "-c:a", "pcm_s16le", "-ar", "48000", "-ac", "2", str(saida)])
        return saida, dur, (0.0 if item.get("flash_inicio") else None)

    vol = item.get("vol", 0.8)
    pedacos = [render_pedaco(f"v2_p_{i:02d}_{k}", p, vol) for k, p in enumerate(item["pedacos"])]
    dur = em_quadros(sum(dur_pedaco(p) for p in item["pedacos"]))
    impacto = None
    if "impacto" in item:
        impacto = sum(dur_pedaco(p) for p in item["pedacos"][: item["impacto"]])
    lista = TRAB / f"v2_lista_{i:02d}.txt"
    lista.write_text("".join(f"file '{p.as_posix()}'\n" for p in pedacos), encoding="utf-8")
    cru = TRAB / f"v2_cru_{i:02d}.mov"
    rodar(["-f", "concat", "-safe", "0", "-i", str(lista), "-c", "copy", str(cru)])

    efeitos = item.get("efeitos", set())
    filtro, entradas, ultimo = ["[0:v]setpts=N/24/TB[base]"], ["-i", str(cru)], "base"
    if "texto" in item:
        png = TRAB / f"v2_texto_{i:02d}.png"
        letreiro(*item["texto"], png)
        entradas += ["-loop", "1", "-i", str(png)]
        em = impacto + 0.1 if item.get("texto_no_impacto") else 0.15
        filtro.append(f"[1:v]format=rgba,fade=in:st={em:.2f}:d=0.25:alpha=1[t]")
        filtro.append(f"[{ultimo}][t]overlay=0:0:shortest=1[vt]")
        ultimo = "vt"
    cadeia, afx = [], []
    if "preto_in" in efeitos:
        cadeia.append("fade=in:st=0:d=0.45")
        afx.append("afade=in:d=0.45")
    if "preto_out" in efeitos:
        cadeia.append(f"fade=out:st={dur - 0.45:.3f}:d=0.45")
        afx.append(f"afade=out:st={dur - 0.45:.3f}:d=0.45")
    # chicote: borrão lateral forte nos últimos/primeiros 4 quadros, como câmera virando rápido
    if "chicote_out" in efeitos:
        cadeia.append(f"gblur=sigma=45:sigmaV=0.5:enable='gte(t,{dur - 0.17:.3f})'")
    if "chicote_in" in efeitos:
        cadeia.append("gblur=sigma=45:sigmaV=0.5:enable='lte(t,0.17)'")
    filtro.append(f"[{ultimo}]{','.join(['setpts=N/24/TB', 'tpad=stop_mode=clone:stop=30', f'trim=end_frame={round(dur * 24)}', *cadeia])}[v]")
    filtro.append(f"[0:a]{','.join(['apad', f'atrim=duration={dur:.4f}', *afx])}[a]")
    rodar([*entradas, "-filter_complex", ";".join(filtro), "-map", "[v]", "-map", "[a]",
           "-fps_mode", "passthrough",
           "-c:v", "libx264", "-preset", "medium", "-crf", "16", "-pix_fmt", "yuv420p",
           "-c:a", "pcm_s16le", "-ar", "48000", "-ac", "2", str(saida)])
    return saida, dur, (impacto if item.get("flash") else None)


def main():
    TRAB.mkdir(parents=True, exist_ok=True)
    segs, inicio, t = [], {}, 0.0
    impactos, flashes = {}, []
    for i, item in enumerate(MONTAGEM):
        arq, dur, imp = segmento(i, item)
        segs.append(arq)
        inicio[item["id"]] = t
        if "impacto" in item:
            impactos[item["id"]] = t + sum(dur_pedaco(p) for p in item["pedacos"][: item["impacto"]])
        if imp is not None:
            flashes.append(t + imp)
        t += dur
        print(f"{item['id']:>7}  {dur:5.2f}s  termina em {t:6.2f}s", flush=True)
    total = t

    lista = TRAB / "v2_lista.txt"
    lista.write_text("".join(f"file '{s.as_posix()}'\n" for s in segs), encoding="utf-8")
    juntado = TRAB / "v2_juntado.mov"
    rodar(["-f", "concat", "-safe", "0", "-i", str(lista), "-c", "copy", str(juntado)])
    # os pedaços lentos saem com outra base de tempo; padroniza tudo em 24 fps constantes,
    # senão o ffmpeg perde quadros e corta o som na mixagem final
    padrao = TRAB / "v2_juntado_24.mov"
    rodar(["-i", str(juntado), "-vf", "setpts=N/24/TB", "-af", "aresample=async=1,asetpts=N/SR/TB",
           "-c:v", "libx264", "-preset", "medium", "-crf", "14", "-pix_fmt", "yuv420p", "-c:a", "pcm_s16le", str(padrao)])
    juntado = padrao

    s = inicio
    secoes = [
        [s["02"], s["08"], 0],
        [s["08"], s["um_dia"], 2],
        [s["12"], s["17"], 1],
        [s["17"], s["21"], 2],
        [s["21"], s["23"], 3],
        [s["23"], s["logo"], 1],
        [s["logo"] + 0.4, total, 0],
    ]
    tecs = [0.35, *impactos.values(), s["logo"]]
    pratos = [impactos["20"], impactos["22"]]
    trilha = TRAB / "v2_trilha.wav"
    subprocess.run([sys.executable, str(PASTA / "trilha.py"), str(trilha), str(total),
                    json.dumps(secoes), json.dumps(pratos), json.dumps(tecs)], check=True)

    # clarões brancos sobre o vídeo já juntado
    entradas = ["-i", str(juntado), "-i", str(trilha)]
    # cada clarão: brilho sobe na hora e cai em 0,35 s
    termos = "+".join(f"gte(t,{tf:.3f})*max(0,1-(t-{tf:.3f})/0.35)" for tf in flashes) or "0"
    filtro = [f"[0:v]eq=eval=frame:brightness='0.75*({termos})':saturation='1-0.6*({termos})',format=yuv420p[v]"]
    filtro.append("[1:a]volume=0.55[m];[0:a][m]amix=inputs=2:duration=first:normalize=0,"
                  "alimiter=limit=0.95,loudnorm=I=-15:TP=-1.5:LRA=11,aresample=48000[a]")
    final = PASTA / "teaser_a_rua_nova_v2.mp4"
    rodar([*entradas, "-filter_complex", ";".join(filtro), "-map", "[v]", "-map", "[a]",
           "-c:v", "libx264", "-preset", "slow", "-crf", "20", "-movflags", "+faststart",
           "-c:a", "aac", "-b:a", "192k", "-ar", "48000", str(final)])
    print("pronto:", final, f"{total:.1f}s")
    print("impactos:", {k: round(v, 2) for k, v in impactos.items()}, "clarões:", [round(f, 2) for f in flashes])


if __name__ == "__main__":
    main()
