"""Desenha um BVH como boneco de palito (frente e lado) em vários quadros. Uso: palito.py arq.bvh saida.png q1,q2,..."""
import sys
import numpy as np
from scipy.spatial.transform import Rotation as R
import matplotlib
matplotlib.use('Agg')
import matplotlib.pyplot as plt

arq, saida, qs = sys.argv[1], sys.argv[2], [int(x) for x in sys.argv[3].split(',')]
texto = open(arq).read()
hier, mov = texto.split('MOTION')
juntas = []  # (nome, pai, offset, canais)
pilha = []
for l in hier.splitlines():
    p = l.split()
    if not p:
        continue
    if p[0] in ('ROOT', 'JOINT'):
        juntas.append([p[1], pilha[-1] if pilha else None, None, []])
        atual = len(juntas) - 1
    elif p[0] == 'End':
        juntas.append(['fim', pilha[-1], None, []])
        atual = len(juntas) - 1
    elif p[0] == '{':
        pilha.append(atual)
    elif p[0] == '}':
        pilha.pop()
    elif p[0] == 'OFFSET':
        juntas[atual][2] = np.array([float(x) for x in p[1:4]])
    elif p[0] == 'CHANNELS':
        juntas[atual][3] = p[2:]
linhas = [l for l in mov.splitlines()[3:] if l.strip()]
dados = np.array([[float(x) for x in l.split()] for l in linhas])


def pose(q):
    v = dados[q]
    k = 0
    pos, rot = {}, {}
    for i, (nome, pai, off, ch) in enumerate(juntas):
        vals = dict(zip(ch, v[k:k + len(ch)]))
        k += len(ch)
        local = R.from_euler('ZXY', [vals.get('Zrotation', 0), vals.get('Xrotation', 0), vals.get('Yrotation', 0)], degrees=True)
        if pai is None:
            pos[i] = np.array([vals['Xposition'], vals['Yposition'], vals['Zposition']])
            rot[i] = local
        else:
            pos[i] = pos[pai] + rot[pai].apply(off)
            rot[i] = rot[pai] * local
    return pos


fig, eixos = plt.subplots(2, len(qs), figsize=(2.2 * len(qs), 5))
for c, q in enumerate(qs):
    p = pose(q)
    centro = p[0]
    for linha, (a, b, titulo) in enumerate([(0, 1, 'frente'), (2, 1, 'lado')]):
        ax = eixos[linha][c]
        for i, j in enumerate(juntas):
            if j[1] is None:
                continue
            nome = j[0] if j[0] != 'fim' else juntas[j[1]][0]
            cor = 'r' if nome.startswith('r') and nome != 'root' else 'b' if nome.startswith('l') and nome != 'lowerback' else 'k'
            ax.plot([p[j[1]][a] - centro[a], p[i][a] - centro[a]], [p[j[1]][b], p[i][b]], cor, lw=2)
        ax.set_xlim(-1, 1)
        ax.set_ylim(-0.1, 1.9)
        ax.set_aspect('equal')
        ax.set_xticks([])
        ax.set_yticks([])
        ax.set_title(f'{titulo} q{q}', fontsize=7)
plt.tight_layout()
plt.savefig(saida, dpi=80)
print('ok')
