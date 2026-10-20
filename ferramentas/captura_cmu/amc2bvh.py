"""Converte ASF/AMC (CMU mocap) para BVH. Uso: python amc2bvh.py arquivo.asf arquivo.amc saida.bvh [quadroIni quadroFim]
Rotação de cada osso: L = C * M * C^-1 (C = eixo do osso no ASF, M = ângulos do AMC na ordem dos dof),
expressa no referencial do mundo da pose de repouso (como o BVH espera)."""
import sys
import numpy as np
from scipy.spatial.transform import Rotation as R

ESCALA = (1.0 / 0.45) * 0.0254  # unidades do ASF -> polegadas -> metros
# Pose de repouso em "T" como o Unity espera: pernas retas para baixo (na CMU ficam abertas 20 graus).
# A rotação de cada osso é compensada (L' = Q_pai * L * Q^-1), então o movimento fica igual.
NOVA_DIRECAO = {'lfemur': (0, -1, 0), 'ltibia': (0, -1, 0), 'rfemur': (0, -1, 0), 'rtibia': (0, -1, 0)}


def correcao(o):
    if o['nome'] not in NOVA_DIRECAO:
        return R.identity()
    a = o['dir'] / np.linalg.norm(o['dir'])
    b = np.array(NOVA_DIRECAO[o['nome']], dtype=float)
    eixo = np.cross(a, b)
    if np.linalg.norm(eixo) < 1e-9:
        return R.identity()
    ang = np.arccos(np.clip(np.dot(a, b), -1, 1))
    return R.from_rotvec(eixo / np.linalg.norm(eixo) * ang)


def ler_asf(caminho):
    ossos = {}
    hierarquia = {}
    linhas = open(caminho).read().splitlines()
    i = 0
    raiz_ordem = None
    raiz_eixo = 'XYZ'
    while i < len(linhas):
        l = linhas[i].strip()
        if l.startswith(':root'):
            i += 1
            while not linhas[i].strip().startswith(':'):
                p = linhas[i].split()
                if p[0] == 'order':
                    raiz_ordem = p[1:]
                if p[0] == 'axis':
                    raiz_eixo = p[1]
                i += 1
            continue
        if l == 'begin':
            o = {'dof': []}
            i += 1
            while linhas[i].strip() != 'end':
                p = linhas[i].split()
                if p[0] == 'name':
                    o['nome'] = p[1]
                elif p[0] == 'direction':
                    o['dir'] = np.array([float(x) for x in p[1:4]])
                elif p[0] == 'length':
                    o['comp'] = float(p[1]) * ESCALA
                elif p[0] == 'axis':
                    o['eixo'] = R.from_euler('xyz', [float(x) for x in p[1:4]], degrees=True)
                elif p[0] == 'dof':
                    o['dof'] = p[1:]
                i += 1
            ossos[o['nome']] = o
        if l.startswith(':hierarchy'):
            i += 1
            while not linhas[i].strip().startswith('end'):
                p = linhas[i].split()
                if p and p[0] != 'begin':
                    hierarquia[p[0]] = p[1:]
                i += 1
        i += 1
    return ossos, hierarquia, raiz_ordem, raiz_eixo


def ler_amc(caminho):
    quadros = []
    atual = None
    for l in open(caminho).read().splitlines():
        l = l.strip()
        if not l or l.startswith('#') or l.startswith(':'):
            continue
        if l.split()[0].isdigit() and len(l.split()) == 1:
            atual = {}
            quadros.append(atual)
            continue
        p = l.split()
        atual[p[0]] = [float(x) for x in p[1:]]
    return quadros


def main():
    asf, amc, saida = sys.argv[1:4]
    ini = int(sys.argv[4]) if len(sys.argv) > 4 else 0
    fim = int(sys.argv[5]) if len(sys.argv) > 5 else None
    ossos, hier, raiz_ordem, raiz_eixo = ler_asf(asf)
    for o in ossos.values():
        o['Q'] = correcao(o)
    pai = {f: p for p, fs in hier.items() for f in fs}
    quadros = ler_amc(amc)[ini:fim]
    ordem = []  # ordem de escrita (profundidade)

    def escrever(nome, offset, nivel, out):
        tab = '\t' * nivel
        if nome == 'root':
            out.append('HIERARCHY')
            out.append('ROOT root')
        else:
            out.append(f'{tab}JOINT {nome}')
        out.append(tab + '{')
        out.append(f'{tab}\tOFFSET {offset[0]:.6f} {offset[1]:.6f} {offset[2]:.6f}')
        if nome == 'root':
            out.append(f'{tab}\tCHANNELS 6 Xposition Yposition Zposition Zrotation Xrotation Yrotation')
        else:
            out.append(f'{tab}\tCHANNELS 3 Zrotation Xrotation Yrotation')
        ordem.append(nome)
        filhos = hier.get(nome, [])
        ponta = np.zeros(3) if nome == 'root' else ossos[nome]['Q'].apply(ossos[nome]['dir'] * ossos[nome]['comp'])
        if filhos:
            for f in filhos:
                escrever(f, ponta, nivel + 1, out)
        else:
            out.append(f'{tab}\tEnd Site')
            out.append(tab + '\t{')
            out.append(f'{tab}\t\tOFFSET {ponta[0]:.6f} {ponta[1]:.6f} {ponta[2]:.6f}')
            out.append(tab + '\t}')
        out.append(tab + '}')

    out = []
    escrever('root', np.zeros(3), 0, out)
    out.append('MOTION')
    out.append(f'Frames: {len(quadros)}')
    out.append(f'Frame Time: {1/120:.8f}')
    eixo_raiz = R.from_euler('xyz', [0, 0, 0], degrees=True)
    for q in quadros:
        valores = []
        for nome in ordem:
            if nome == 'root':
                v = q['root']
                pos = np.array(v[0:3]) * ESCALA
                # ordem do AMC da raiz: TX TY TZ RX RY RZ -> rotação aplicada na ordem dos dof
                rot = R.from_euler('xyz', [v[3], v[4], v[5]], degrees=True)
                rot = eixo_raiz * rot * eixo_raiz.inv()
                zxy = rot.as_euler('ZXY', degrees=True)
                valores += list(pos) + list(zxy)
                continue
            o = ossos[nome]
            M = R.identity()
            if o['dof'] and nome in q:
                angs = q[nome]
                # ângulos aplicados na ordem em que os dof aparecem (rx ry rz -> eixos intrínsecos)
                eixos = ''.join(d[1].lower() for d in o['dof'])  # extrínseco: matriz Rz*Ry*Rx (convenção CMU)
                M = R.from_euler(eixos, angs, degrees=True)
            C = o['eixo']
            L = C * M * C.inv()
            Qpai = ossos[pai[nome]]['Q'] if pai[nome] != 'root' else R.identity()
            L = Qpai * L * o['Q'].inv()
            valores += list(L.as_euler('ZXY', degrees=True))
        out.append(' '.join(f'{x:.5f}' for x in valores))
    open(saida, 'w').write('\n'.join(out) + '\n')
    print('ok', len(quadros), 'quadros', len(ordem), 'juntas')


main()
