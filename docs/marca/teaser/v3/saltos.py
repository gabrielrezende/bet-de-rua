"""Mede saltos quadro a quadro em cada clipe (corte escondido, objeto que aparece/some, fundo que troca).

    uv run --no-project --with numpy python docs/marca/teaser/v3/saltos.py CAMINHO_FFMPEG

Para cada clipe: diferença média entre quadros seguidos (imagem reduzida em cinza) e a
diferença do fundo (bordas do quadro). Um salto é um quadro com diferença muito maior que a
mediana do próprio clipe. Imprime os tempos suspeitos.
"""

import subprocess
import sys
from pathlib import Path

import numpy as np

FF = sys.argv[1]
BRUTOS = Path(__file__).parent / "brutos"
L, A = 160, 90


def quadros(arq):
    r = subprocess.run([FF, "-loglevel", "error", "-i", str(arq), "-vf", f"scale={L}:{A},format=gray",
                        "-f", "rawvideo", "-"], capture_output=True)
    return np.frombuffer(r.stdout, np.uint8).reshape(-1, A, L).astype(np.float32)


for arq in sorted(BRUTOS.glob("[0-9][0-9].mp4")):
    q = quadros(arq)
    d = np.abs(np.diff(q, axis=0)).mean(axis=(1, 2))
    borda = np.concatenate([q[:, :10, :].reshape(len(q), -1), q[:, :, :12].reshape(len(q), -1),
                            q[:, :, -12:].reshape(len(q), -1)], axis=1)
    db = np.abs(np.diff(borda, axis=0)).mean(axis=1)
    med, medb = np.median(d) + 0.3, np.median(db) + 0.3
    suspeitos = [i for i in range(len(d)) if d[i] > 3.5 * med or db[i] > 4 * medb]
    tempos = ", ".join(f"{(i + 1) / 24:.2f}s({d[i] / med:.0f}x)" for i in suspeitos)
    print(f"{arq.stem}: {len(q)} quadros, mediana {med:.1f} | saltos: {tempos or 'nenhum'}")
