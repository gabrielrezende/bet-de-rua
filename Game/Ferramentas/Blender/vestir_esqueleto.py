"""
Põe um esqueleto bom (o do MakeHuman, com dedos, no padrão "mixamo_unity" que o Unity entende como
Humanoid) num corpo 3D gerado por IA (Tripo, Meshy...), que vem sem esqueleto ou com um esqueleto ruim.

Como: gera no MakeHuman (MPFB) um corpo da mesma altura e proporção, com o esqueleto; encaixa o corpo da
IA em cima dele; copia os "pesos" (quanto cada osso puxa cada pedaço da pele) do corpo do MakeHuman para o
da IA, pelo ponto mais perto; joga fora o corpo do MakeHuman e exporta só o corpo da IA com o esqueleto.

Rodar:
  blender --background --python vestir_esqueleto.py -- <modelo_ia.glb> <saida.fbx> <genero 0..1> <idade 0..1> [<pasta_testes>]
Se der a pasta de testes, salva fotos do corpo dobrado (braço, perna, tronco) para conferir a deformação.
"""
import math
import os
import sys

import bpy
import mathutils

from bl_ext.blender_org.mpfb.services.humanservice import HumanService
from bl_ext.blender_org.mpfb.services.exportservice import ExportService
from bl_ext.blender_org.mpfb.services.targetservice import TargetService


def caixa(objs):
    mn = mathutils.Vector((1e9, 1e9, 1e9))
    mx = -mn
    for o in objs:
        for c in o.bound_box:
            w = o.matrix_world @ mathutils.Vector(c)
            mn = mathutils.Vector(map(min, mn, w))
            mx = mathutils.Vector(map(max, mx, w))
    return mn, mx


def main():
    a = sys.argv[sys.argv.index("--") + 1:]
    glb, saida, genero, idade = a[0], a[1], float(a[2]), float(a[3])
    testes = a[4] if len(a) > 4 else None

    bpy.ops.wm.read_homefile(use_empty=True)
    for o in list(bpy.data.objects):
        bpy.data.objects.remove(o)

    # 1) Corpo da IA: só a malha (tira esqueleto e objetos extras que vierem junto)
    bpy.ops.import_scene.gltf(filepath=glb)
    malhas = [o for o in bpy.context.scene.objects if o.type == 'MESH' and len(o.data.vertices) > 2000]
    for o in list(bpy.context.scene.objects):
        if o not in malhas:
            bpy.data.objects.remove(o)
    ia = malhas[0]
    if len(malhas) > 1:
        bpy.context.view_layer.objects.active = ia
        for o in malhas:
            o.select_set(True)
        bpy.ops.object.join()
    ia.name = "Corpo"
    ia.parent = None
    for m in list(ia.modifiers):
        ia.modifiers.remove(m)
    ia.vertex_groups.clear()
    bpy.context.view_layer.objects.active = ia
    ia.select_set(True)
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    mn_ia, mx_ia = caixa([ia])

    # 2) Corpo do MakeHuman com o esqueleto, da mesma altura
    macro = {"gender": genero, "age": idade, "muscle": 0.55, "weight": 0.45, "proportions": 0.6,
             "height": 0.6, "cupsize": 0.5, "firmness": 0.5,
             "race": {"african": 0.33, "caucasian": 0.33, "asian": 0.33}}
    mh = HumanService.create_human(mask_helpers=True, detailed_helpers=True, extra_vertex_groups=True,
                                   feet_on_ground=True, scale=0.1, macro_detail_dict=macro)
    rig = HumanService.add_builtin_rig(mh, "mixamo_unity", import_weights=True)
    TargetService.bake_targets(mh)
    ExportService.bake_modifiers_remove_helpers(mh, bake_masks=True, bake_subdiv=False, remove_helpers=True)
    mn_mh, mx_mh = caixa([mh])

    # 3) Encaixa o corpo da IA no do MakeHuman: mesma altura, pés no mesmo lugar
    k = (mx_mh.z - mn_mh.z) / max(0.01, (mx_ia.z - mn_ia.z))
    centro_ia = mathutils.Vector(((mn_ia.x + mx_ia.x) / 2, (mn_ia.y + mx_ia.y) / 2, mn_ia.z))
    centro_mh = mathutils.Vector(((mn_mh.x + mx_mh.x) / 2, (mn_mh.y + mx_mh.y) / 2, mn_mh.z))
    ia.scale = (k, k, k)
    ia.location = centro_mh - centro_ia * k
    bpy.context.view_layer.objects.active = ia
    bpy.ops.object.select_all(action='DESELECT')
    ia.select_set(True)
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    print("ESCALA_IA", k, "ALTURA_MH", mx_mh.z - mn_mh.z)

    # 3b) Virado para o mesmo lado? Testa 0° e 180° e fica com o que encaixa melhor no corpo do MakeHuman
    #     (virado ao contrário, a perna esquerda da IA pegaria os pesos da perna direita)
    from mathutils.bvhtree import BVHTree
    deps = bpy.context.evaluated_depsgraph_get()
    arvore = BVHTree.FromObject(mh, deps)

    def erro():
        total = 0.0
        vs = ia.data.vertices
        passo = max(1, len(vs) // 3000)
        for i in range(0, len(vs), passo):
            r = arvore.find_nearest(ia.matrix_world @ vs[i].co)
            total += r[3] if r[0] is not None else 1.0
        return total

    e0 = erro()
    ia.rotation_euler = (0, 0, math.pi)
    ia.location = (centro_mh.x * 2, centro_mh.y * 2, 0)
    bpy.context.view_layer.update()
    e180 = erro()
    print("ENCAIXE 0:", round(e0, 2), "180:", round(e180, 2))
    if e180 < e0:
        bpy.ops.object.select_all(action='DESELECT')
        ia.select_set(True)
        bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    else:
        ia.rotation_euler = (0, 0, 0)
        ia.location = (0, 0, 0)
        bpy.context.view_layer.update()

    # 4) Pesos: o Blender calcula pelo próprio formato do corpo da IA ("calor" saindo de cada osso).
    #    Copiar do corpo do MakeHuman pelo ponto mais perto misturava a coxa com a bermuda e virava lençol.
    #    Ossos de enfeite (seio, pálpebra, olho, mandíbula, raiz) não puxam a pele.
    # Costura os pedaços soltos (modelo de IA vem em "retalhos"; sem isso o cálculo de pesos falha neles)
    bpy.ops.object.select_all(action='DESELECT')
    ia.select_set(True)
    bpy.context.view_layer.objects.active = ia
    bpy.ops.object.mode_set(mode='EDIT')
    bpy.ops.mesh.select_all(action='SELECT')
    bpy.ops.mesh.remove_doubles(threshold=0.002)
    bpy.ops.object.mode_set(mode='OBJECT')
    # Pesos do corpo do MakeHuman guardados por ponto, para preencher onde o cálculo falhar
    from mathutils.kdtree import KDTree
    pesos_mh = []
    nomes_osso = set(b.name for b in rig.data.bones)
    grupos_mh = {vg.index: vg.name for vg in mh.vertex_groups if vg.name in nomes_osso}
    kd = KDTree(len(mh.data.vertices))
    for v in mh.data.vertices:
        kd.insert(mh.matrix_world @ v.co, v.index)
        pesos_mh.append([(grupos_mh[g.group], g.weight) for g in v.groups if g.group in grupos_mh and g.weight > 0.01])
    kd.balance()
    for o in list(rig.children):
        bpy.data.objects.remove(o)
    enfeites = ("Breast", "Orbicularis", "Eye", "Jaw", "Root", "Buttock")
    for b in rig.data.bones:
        b.use_deform = not any(e in b.name for e in enfeites)
    bpy.ops.object.select_all(action='DESELECT')
    ia.select_set(True)
    rig.select_set(True)
    bpy.context.view_layer.objects.active = rig
    bpy.ops.object.parent_set(type='ARMATURE_AUTO')
    print("GRUPOS", len(ia.vertex_groups))
    # Onde o calor não chegou (pedaço solto), copia o peso do ponto mais perto do corpo do MakeHuman
    sem = 0
    for v in ia.data.vertices:
        if sum(g.weight for g in v.groups) > 0.05:
            continue
        sem += 1
        _, idx, _ = kd.find(ia.matrix_world @ v.co)
        for nome, w in pesos_mh[idx]:
            vg = ia.vertex_groups.get(nome) or ia.vertex_groups.new(name=nome)
            vg.add([v.index], w, 'REPLACE')
    print("SEM_PESO_PREENCHIDOS", sem, "de", len(ia.data.vertices))
    bpy.context.view_layer.objects.active = ia
    bpy.ops.object.vertex_group_limit_total(limit=4)
    bpy.ops.object.vertex_group_normalize_all(lock_active=False)
    rig.name = os.path.splitext(os.path.basename(saida))[0]

    if testes:
        fotos_de_teste(rig, ia, testes)

    bpy.ops.object.select_all(action='DESELECT')
    rig.select_set(True)
    ia.select_set(True)
    bpy.context.view_layer.objects.active = rig
    # Texturas para fora, para o FBX levar
    pasta = os.path.join(os.path.dirname(saida), "texturas")
    os.makedirs(pasta, exist_ok=True)
    for im in bpy.data.images:
        if im.packed_file:
            im.filepath_raw = os.path.join(pasta, f"{rig.name}_{im.name.replace(':', '_')}.png")
            im.file_format = 'PNG'
            im.save()
    bpy.ops.export_scene.fbx(filepath=saida, use_selection=True, object_types={'ARMATURE', 'MESH'},
                             add_leaf_bones=False, bake_anim=False, path_mode='COPY', embed_textures=False,
                             apply_scale_options='FBX_SCALE_ALL', axis_forward='-Z', axis_up='Y')
    print("EXPORTADO", saida, "OSSOS", len(rig.data.bones))


def fotos_de_teste(rig, corpo, pasta):
    """Dobra braço, perna e tronco e fotografa: mostra se a pele/roupa estica ou rasga."""
    os.makedirs(pasta, exist_ok=True)
    scn = bpy.context.scene
    scn.render.engine = 'BLENDER_EEVEE_NEXT' if 'BLENDER_EEVEE_NEXT' in [e.identifier for e in bpy.types.RenderSettings.bl_rna.properties['engine'].enum_items] else 'BLENDER_EEVEE'
    scn.render.resolution_x, scn.render.resolution_y = 600, 900
    w = bpy.data.worlds.new("w")
    scn.world = w
    w.use_nodes = True
    w.node_tree.nodes["Background"].inputs[0].default_value = (0.6, 0.62, 0.65, 1)
    sol = bpy.data.objects.new("sol", bpy.data.lights.new("sol", 'SUN'))
    scn.collection.objects.link(sol)
    sol.data.energy = 3.5
    sol.rotation_euler = (math.radians(50), 0, math.radians(30))
    cam = bpy.data.objects.new("cam", bpy.data.cameras.new("cam"))
    scn.collection.objects.link(cam)
    scn.camera = cam
    mn, mx = caixa([corpo])
    centro = (mn + mx) / 2
    alt = mx.z - mn.z
    nomes = [b.name for b in rig.data.bones]
    print("OSSOS_NOMES", nomes[:70])

    def osso(sufixo):
        for n in nomes:
            if n.lower().endswith(sufixo.lower()):
                return rig.pose.bones[n]
        return None

    poses = {
        "neutro": {},
        # guarda do bets: tronco inclinado, joelhos dobrados, braços para a frente
        "guarda": {"Spine": (25, 0, 0), "LeftUpLeg": (-30, 0, 0), "RightUpLeg": (-30, 0, 0), "LeftLeg": (45, 0, 0),
                   "RightLeg": (45, 0, 0), "LeftArm": (0, 0, -40), "RightArm": (0, 0, 40),
                   "LeftForeArm": (0, 0, -30), "RightForeArm": (0, 0, 30)},
        # batida: giro do tronco e braço esticado
        "batida": {"Spine": (0, 40, 0), "Spine1": (0, 25, 0), "LeftArm": (-60, 0, -10), "RightArm": (-60, 0, 10),
                   "LeftUpLeg": (-20, 0, 10), "RightLeg": (30, 0, 0)},
        # corrida: perna para a frente, joelho dobrado
        "corrida": {"LeftUpLeg": (-70, 0, 0), "LeftLeg": (80, 0, 0), "RightUpLeg": (25, 0, 0), "RightLeg": (40, 0, 0),
                    "LeftArm": (40, 0, 0), "RightArm": (-40, 0, 0), "LeftForeArm": (0, 0, -60), "RightForeArm": (0, 0, 60)},
        # mão fechada (o taco)
        "mao": {"RightArm": (-70, 0, 20)},
    }
    for nome, giros in poses.items():
        for pb in rig.pose.bones:
            pb.rotation_mode = 'XYZ'
            pb.rotation_euler = (0, 0, 0)
        for o, (x, y, z) in giros.items():
            pb = osso(o)
            if pb is not None:
                pb.rotation_euler = (math.radians(x), math.radians(y), math.radians(z))
        if nome == "mao":
            for pb in rig.pose.bones:
                n = pb.name.lower()
                if "righthand" in n and ("index" in n or "middle" in n or "ring" in n or "pinky" in n):
                    pb.rotation_euler = (math.radians(70), 0, 0)
        bpy.context.view_layer.update()
        for vista, ang in (("frente", -90), ("lado", 0)):
            d = alt * 2.2
            alvo = centro.copy()
            if nome == "mao":
                d = alt * 0.9
            a = math.radians(ang)
            cam.location = alvo + mathutils.Vector((math.cos(a) * d, math.sin(a) * d, alt * 0.05))
            cam.rotation_euler = (alvo - cam.location).to_track_quat('-Z', 'Y').to_euler()
            scn.render.filepath = os.path.join(pasta, f"{nome}_{vista}.png")
            bpy.ops.render.render(write_still=True)
    for pb in rig.pose.bones:
        pb.rotation_euler = (0, 0, 0)


main()
