# Roda dentro do Blender: blender -b --python bvh2fbx.py -- entrada.bvh saida.fbx nomeDoClipe
import sys
import bpy

args = sys.argv[sys.argv.index('--') + 1:]
entrada, saida, nome = args[0], args[1], args[2]
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_anim.bvh(filepath=entrada, global_scale=1.0, frame_start=1, use_fps_scale=False,
                        update_scene_fps=True, update_scene_duration=True, rotate_mode='NATIVE',
                        axis_forward='-Z', axis_up='Y')
arm = [o for o in bpy.context.scene.objects if o.type == 'ARMATURE'][0]
arm.name = 'CMU'
if arm.animation_data and arm.animation_data.action:
    arm.animation_data.action.name = nome
bpy.ops.export_scene.fbx(filepath=saida, object_types={'ARMATURE'}, bake_anim=True, bake_anim_use_all_actions=False,
                         bake_anim_use_nla_strips=False, add_leaf_bones=False, axis_forward='-Z', axis_up='Y',
                         apply_scale_options='FBX_SCALE_ALL', bake_anim_simplify_factor=0.0)
print('FBX_OK', saida, bpy.context.scene.frame_end)
