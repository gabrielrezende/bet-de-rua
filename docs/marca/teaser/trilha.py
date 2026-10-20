"""Trilha do teaser: violão de fim de tarde + percussão de rua, sintetizados (sem sample nenhum).

Violão: síntese Karplus-Strong (corda dedilhada). Percussão: surdo, caixinha e ganzá feitos de ruído.
As seções seguem a montagem (tempos em segundos). Gera trilha.wav em 48 kHz estéreo.

    uv run --no-project --with numpy python docs/marca/teaser/trilha.py SAIDA.wav DURACAO
"""

import sys
import wave

import numpy as np

TAXA = 48000
rng = np.random.default_rng(3)

# acordes (frequências) — Ré maior, Si menor, Sol maior, Lá (I vi IV V), clima de fim de tarde
NOTAS = {
    "D": [146.83, 220.00, 293.66, 369.99, 440.00],
    "Bm": [123.47, 185.00, 246.94, 293.66, 369.99],
    "G": [98.00, 146.83, 196.00, 246.94, 293.66],
    "A": [110.00, 164.81, 220.00, 277.18, 329.63],
    "Em": [82.41, 164.81, 196.00, 246.94, 329.63],
}
PROGRESSAO = ["D", "Bm", "G", "A"]


def corda(freq, dur, brilho=0.5, decaimento=0.996):
    """Karplus-Strong: um buffer de ruído que vai sendo filtrado = corda de nylon."""
    n = int(TAXA * dur)
    periodo = max(2, int(TAXA / freq))
    buf = rng.uniform(-1, 1, periodo)
    # suaviza o ataque (nylon é menos brilhante que aço)
    for _ in range(int((1 - brilho) * 4)):
        buf = 0.5 * (buf + np.roll(buf, 1))
    saida = np.empty(n)
    for i in range(n):
        saida[i] = buf[i % periodo]
        j = i % periodo
        buf[j] = decaimento * 0.5 * (buf[j] + buf[(j + 1) % periodo])
    return saida


def surdo(dur=0.6, f=58):
    t = np.arange(int(TAXA * dur)) / TAXA
    freq = f * (1 + 0.6 * np.exp(-t * 30))
    return np.sin(2 * np.pi * np.cumsum(freq) / TAXA) * np.exp(-t * 7)


def ganza(dur=0.09):
    t = np.arange(int(TAXA * dur)) / TAXA
    ruido = rng.uniform(-1, 1, len(t))
    ruido = ruido - np.convolve(ruido, np.ones(4) / 4, mode="same")  # só os agudos
    return ruido * np.exp(-t * 45) * 0.5


def caixa(dur=0.18):
    t = np.arange(int(TAXA * dur)) / TAXA
    corpo = np.sin(2 * np.pi * 190 * t) * np.exp(-t * 35)
    esteira = rng.uniform(-1, 1, len(t)) * np.exp(-t * 22)
    return 0.5 * corpo + 0.5 * esteira


def tec(dur=0.35):
    """O "tec!" de madeira: dois modos de vibração curtos, como tábua batendo em tábua."""
    t = np.arange(int(TAXA * dur)) / TAXA
    som = (np.sin(2 * np.pi * 820 * t) * np.exp(-t * 38) + 0.6 * np.sin(2 * np.pi * 1730 * t) * np.exp(-t * 55)
           + 0.3 * np.sin(2 * np.pi * 410 * t) * np.exp(-t * 25))
    estalo = rng.uniform(-1, 1, len(t)) * np.exp(-t * 400)
    return (som + 0.8 * estalo) * 0.9


def prato(dur=3.0):
    t = np.arange(int(TAXA * dur)) / TAXA
    ruido = rng.uniform(-1, 1, len(t))
    ruido = ruido - np.convolve(ruido, np.ones(3) / 3, mode="same")
    return ruido * np.exp(-t * 1.6) * 0.35


def por(trilha, som, t, ganho=1.0, pan=0.0):
    i = int(t * TAXA)
    if i >= trilha.shape[0]:
        return
    f = min(len(som), trilha.shape[0] - i)
    trilha[i : i + f, 0] += som[:f] * ganho * (1 - max(0, pan))
    trilha[i : i + f, 1] += som[:f] * ganho * (1 + min(0, pan))


def reverb(sinal, tempo=1.8, mistura=0.25):
    n = int(TAXA * tempo)
    ir = rng.standard_normal(n) * np.exp(-np.arange(n) / TAXA * 6.9 / tempo)
    ir /= np.sqrt(np.sum(ir**2))
    tam = 1 << int(np.ceil(np.log2(len(sinal) + n)))
    saida = np.empty_like(sinal)
    for c in range(sinal.shape[1]):
        molhado = np.fft.irfft(np.fft.rfft(sinal[:, c], tam) * np.fft.rfft(ir, tam), tam)[: len(sinal)]
        saida[:, c] = (1 - mistura) * sinal[:, c] + mistura * molhado
    return saida


def gerar(duracao, secoes):
    """secoes: lista de (inicio, fim, intensidade 0..3).
    0 = notas soltas, 1 = dedilhado, 2 = dedilhado + ganzá + surdo, 3 = batida cheia."""
    trilha = np.zeros((int(TAXA * duracao), 2))
    bpm = 92
    tempo_batida = 60 / bpm
    cache = {}

    def nota(freq, dur, brilho):
        chave = (round(freq, 2), dur, brilho)
        if chave not in cache:
            cache[chave] = corda(freq, dur, brilho)
        return cache[chave]

    for inicio, fim, nivel in secoes:
        t = inicio
        compasso = int(inicio / (tempo_batida * 4))
        while t < fim:
            acorde = NOTAS[PROGRESSAO[compasso % 4]]
            for tempo in range(8):  # colcheias
                tt = t + tempo * tempo_batida / 2
                if tt >= fim:
                    break
                if nivel == 0:
                    if tempo in (0, 5):
                        por(trilha, nota(acorde[[0, 3][tempo == 5]], 2.5, 0.4), tt, 0.35, -0.2)
                elif nivel in (1, 2):
                    ordem = [0, 2, 3, 4, 1, 3, 2, 4]
                    por(trilha, nota(acorde[ordem[tempo]], 2.0, 0.5), tt, 0.28 if tempo else 0.4, 0.25 - 0.06 * tempo)
                else:
                    # batida de violão: acorde rasgado nos tempos fortes e contratempos
                    if tempo in (0, 3, 4, 6):
                        for k, f in enumerate(acorde):
                            por(trilha, nota(f, 1.2, 0.7), tt + k * 0.012, 0.16, -0.3 + 0.15 * k)
                if nivel >= 2:
                    por(trilha, ganza(), tt, 0.25 if tempo % 2 else 0.4, 0.5)
                    if tempo in (0, 4):
                        por(trilha, surdo(), tt, 0.75)
                    if tempo == 7:
                        por(trilha, surdo(0.4, 70), tt, 0.4)
                if nivel >= 3 and tempo in (2, 6):
                    por(trilha, caixa(), tt, 0.35, -0.3)
            t += tempo_batida * 4
            compasso += 1
    return trilha


def salvar(caminho, trilha):
    trilha = reverb(trilha)
    trilha = trilha / (np.max(np.abs(trilha)) + 1e-9) * 0.85
    dados = (trilha * 32767).astype(np.int16)
    with wave.open(caminho, "wb") as w:
        w.setnchannels(2)
        w.setsampwidth(2)
        w.setframerate(TAXA)
        w.writeframes(dados.tobytes())


if __name__ == "__main__":
    import json

    caminho = sys.argv[1]
    duracao = float(sys.argv[2])
    secoes = json.loads(sys.argv[3])
    pratos = json.loads(sys.argv[4]) if len(sys.argv) > 4 else []
    trilha = gerar(duracao, secoes)
    for t in pratos:
        por(trilha, prato(), t, 1.0)
        por(trilha, surdo(1.2, 50), t, 1.2)
    tecs = json.loads(sys.argv[5]) if len(sys.argv) > 5 else []
    for t in tecs:
        por(trilha, tec(), t, 1.6)
    salvar(caminho, trilha)
    print("ok", caminho)
