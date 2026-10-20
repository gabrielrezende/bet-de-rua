"""
Gera as pessoas do Bets de Rua com o MakeHuman (extensão MPFB 2 do Blender, recursos CC0) e
exporta cada uma em FBX com esqueleto que o Unity entende como Humanoid ("mixamo_unity").

Rodar (sem abrir a janela do Blender):
  blender --background --python gerar_pessoas.py -- <pasta_saida> [<nomes separados por vírgula>]

Precisa dos pacotes de recursos CC0 instalados na MPFB (pasta de dados do usuário): makehuman_system_assets,
shirts01, pants01, eyebrows01, eyelashes01 (https://static.makehumancommunity.org/assets/assetpacks.html).

O nome do arquivo diz quem é (lido pelo Editor/ImportadorPessoas no Unity):
  Genero_Faixa_Tom_Cabelo_N  ->  M/F, Adulto/Adolescente/Crianca, tom de pele 0 a 100, Curto/Comprido
As peças saem com nomes em português (Corpo, Cabelo, Sobrancelha, Cilios, Olhos, Camisa, Bermuda, Tenis)
para o jogo achar a camisa (vira a do time), a bermuda e o tênis (pegam a cor da ficha).
"""
import os
import sys

import bpy

from bl_ext.blender_org.mpfb.services.humanservice import HumanService
from bl_ext.blender_org.mpfb.services.exportservice import ExportService
from bl_ext.blender_org.mpfb.services.assetservice import AssetService
from bl_ext.blender_org.mpfb.services.objectservice import ObjectService
from bl_ext.blender_org.mpfb.services.targetservice import TargetService

# idade no MakeHuman: 0 = 1 ano, 0.1875 = 11 anos, 0.5 = 25 anos, 1 = 90 anos
CAMISA_M, CAMISA_F, POLO, REGATA = "elvs_crude_t-shirt_male", "joepal_crude_t-shirt_female", "namuhekam_male_polo_shirt", "toigo_keyhole_tank_top"
BERMUDA, CALCA = "cortu_jeans_shorts", "cortu_cargo_pants"

# nome: (gênero 0=mulher 1=homem, idade, músculo, peso, altura, proporções, (africano, europeu, asiático), pele, cabelo, camisa, baixo, tênis)
RECEITAS = {
    "M_Adulto_15_Curto_1":       (1.0, 0.50, 0.60, 0.50, 0.55, 0.60, (0.10, 0.85, 0.05), "young_caucasian_male", "short02", CAMISA_M, BERMUDA, "shoes05"),
    "M_Adulto_50_Curto_2":       (1.0, 0.55, 0.55, 0.58, 0.50, 0.55, (0.45, 0.50, 0.05), "young_caucasian_male2", "short04", POLO, CALCA, "shoes02"),
    "M_Adulto_85_Curto_3":       (1.0, 0.48, 0.72, 0.48, 0.60, 0.62, (0.90, 0.08, 0.02), "young_african_male", "short01", CAMISA_M, BERMUDA, "shoes06"),
    "M_Adulto_80_Curto_4":       (1.0, 0.45, 0.58, 0.45, 0.55, 0.60, (0.85, 0.13, 0.02), "young_african_male", "afro01", CAMISA_M, BERMUDA, "shoes05"),
    "F_Adulto_25_Comprido_1":    (0.0, 0.48, 0.50, 0.48, 0.50, 0.60, (0.15, 0.80, 0.05), "young_caucasian_female", "ponytail01", CAMISA_F, BERMUDA, "shoes05"),
    "F_Adulto_55_Comprido_2":    (0.0, 0.52, 0.48, 0.55, 0.45, 0.58, (0.45, 0.50, 0.05), "young_caucasian_female2", "braid01", CAMISA_F, BERMUDA, "shoes06"),
    "F_Adulto_85_Curto_3":       (0.0, 0.46, 0.55, 0.50, 0.52, 0.60, (0.88, 0.10, 0.02), "young_african_female", "afro01", REGATA, BERMUDA, "shoes05"),
    "M_Adolescente_40_Curto_1":  (1.0, 0.36, 0.45, 0.42, 0.50, 0.55, (0.35, 0.60, 0.05), "young_caucasian_male", "short04", CAMISA_M, BERMUDA, "shoes06"),
    "M_Adolescente_80_Curto_2":  (1.0, 0.37, 0.50, 0.40, 0.55, 0.55, (0.85, 0.13, 0.02), "young_african_male", "short01", CAMISA_M, CALCA, "shoes02"),
    "F_Adolescente_50_Comprido_1": (0.0, 0.36, 0.42, 0.45, 0.48, 0.55, (0.45, 0.50, 0.05), "young_caucasian_female2", "ponytail01", CAMISA_F, BERMUDA, "shoes05"),
    "M_Crianca_30_Curto_1":      (1.0, 0.19, 0.40, 0.45, 0.50, 0.50, (0.25, 0.70, 0.05), "young_caucasian_male", "short02", CAMISA_M, BERMUDA, "shoes05"),
    "M_Crianca_80_Curto_2":      (1.0, 0.18, 0.40, 0.48, 0.48, 0.50, (0.85, 0.13, 0.02), "young_african_male", "afro01", CAMISA_M, BERMUDA, "shoes06"),
    "F_Crianca_40_Comprido_1":   (0.0, 0.19, 0.38, 0.45, 0.50, 0.50, (0.35, 0.60, 0.05), "young_caucasian_female", "braid01", CAMISA_F, BERMUDA, "shoes05"),
    "F_Crianca_80_Comprido_2":   (0.0, 0.18, 0.38, 0.47, 0.48, 0.50, (0.85, 0.13, 0.02), "young_african_female", "ponytail01", CAMISA_F, BERMUDA, "shoes06"),
}


def caminho(subpasta, nome, ext):
    p = AssetService.find_asset_absolute_path(f"{nome}/{nome}.{ext}", asset_subdir=subpasta)
    if p is None:
        print(f"FALTA {subpasta}/{nome}", flush=True)
    return p


def vestir(corpo, subpasta, nome, tipo, novo_nome):
    p = caminho(subpasta, nome, "mhclo")
    if p is None:
        return None
    peca = HumanService.add_mhclo_asset(p, corpo, asset_type=tipo, subdiv_levels=0, material_type="GAMEENGINE")
    if peca is not None:
        peca.name = novo_nome
        for i, slot in enumerate(peca.material_slots):
            if slot.material:
                slot.material.name = f"{novo_nome}_{nome}" if i == 0 else f"{novo_nome}_{nome}_{i}"
    return peca


def criar(nome, receita):
    (genero, idade, musculo, peso, altura, proporcoes, (africano, europeu, asiatico),
     pele, cabelo, camisa, baixo, tenis) = receita
    macro = {
        "gender": genero, "age": idade, "muscle": musculo, "weight": peso, "proportions": proporcoes,
        "height": altura, "cupsize": 0.5, "firmness": 0.5,
        "race": {"african": africano, "caucasian": europeu, "asian": asiatico},
    }
    corpo = HumanService.create_human(mask_helpers=True, detailed_helpers=True, extra_vertex_groups=True,
                                      feet_on_ground=True, scale=0.1, macro_detail_dict=macro)
    corpo.name = "Corpo"
    rig = HumanService.add_builtin_rig(corpo, "mixamo_unity", import_weights=True)
    rig.name = nome
    # Pele de verdade (textura de foto), num material simples que o FBX leva para o Unity
    mhmat = AssetService.find_asset_absolute_path(f"{pele}/{pele}.mhmat", asset_subdir="skins")
    if mhmat:
        HumanService.set_character_skin(mhmat, corpo, skin_type="GAMEENGINE", material_instances=False)
    for slot in corpo.material_slots:
        if slot.material:
            slot.material.name = "Pele_" + pele
    # Olhos, sobrancelha, cílios, cabelo e roupa (ajustam no corpo e no esqueleto)
    olhos = AssetService.find_asset_absolute_path("high-poly/high-poly.mhclo", asset_subdir="eyes")
    if olhos:
        o = HumanService.add_mhclo_asset(olhos, corpo, asset_type="Eyes", subdiv_levels=0, material_type="GAMEENGINE")
        if o is not None:
            o.name = "Olhos"
    vestir(corpo, "eyebrows", "eyebrow001" if genero > 0.5 else "eyebrow010", "Eyebrows", "Sobrancelha")
    vestir(corpo, "eyelashes", "eyelashes01", "Eyelashes", "Cilios")
    vestir(corpo, "hair", cabelo, "Hair", "Cabelo")
    vestir(corpo, "clothes", camisa, "Clothes", "Camisa")
    vestir(corpo, "clothes", baixo, "Clothes", "Bermuda")
    vestir(corpo, "clothes", tenis, "Clothes", "Tenis")
    return corpo, rig


def exportar(corpo, rig, pasta, nome):
    # O formato do corpo (idade, peso...) é uma mistura de formas: congela na malha e tira os ossos
    # de ajuda (o Unity receberia o corpo neutro e pedaços escondidos)
    TargetService.bake_targets(corpo)
    ExportService.bake_modifiers_remove_helpers(corpo, bake_masks=True, bake_subdiv=False, remove_helpers=True)
    for filho in rig.children:
        if filho.type == "MESH" and filho.data.shape_keys is not None:
            TargetService.bake_targets(filho)
    ObjectService.deselect_and_deactivate_all()
    rig.select_set(True)
    for filho in rig.children:
        filho.select_set(True)
    bpy.context.view_layer.objects.active = rig
    bpy.ops.export_scene.fbx(filepath=os.path.join(pasta, f"{nome}.fbx"), use_selection=True, apply_unit_scale=True,
                             apply_scale_options="FBX_SCALE_ALL", object_types={"ARMATURE", "MESH"}, use_mesh_modifiers=True,
                             add_leaf_bones=False, bake_anim=False, path_mode="COPY", embed_textures=False,
                             axis_forward="-Z", axis_up="Y")


def main():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    saida = argv[0] if argv else os.path.abspath("Pessoas")
    nomes = argv[1].split(",") if len(argv) > 1 else list(RECEITAS.keys())
    os.makedirs(saida, exist_ok=True)
    for nome in nomes:
        bpy.ops.wm.read_homefile(use_empty=True)
        try:
            corpo, rig = criar(nome, RECEITAS[nome])
            exportar(corpo, rig, saida, nome)
            print(f"GERADO {nome}", flush=True)
        except Exception as e:
            import traceback
            traceback.print_exc()
            print(f"ERRO {nome}: {e}", flush=True)


main()
