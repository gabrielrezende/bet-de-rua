using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace BetsDeRua.Editor
{
    /// <summary>
    /// Importa as capturas de movimento de verdade da CMU (Assets/Personagens/Animacoes/CMU, ver o
    /// LEIAME de lá) como Humanoid, com o mapa dos ossos da CMU para o esqueleto do Unity: assim a
    /// rebatida gravada de uma pessoa de verdade toca em qualquer corpo do jogo.
    /// Também corta os clipes: a guarda (parado esperando a bola) e a batida (carregar, passada,
    /// giro e finalização).
    /// </summary>
    public static class ImportadorCapturas
    {
        public const string Pasta = "Assets/Personagens/Animacoes/CMU";

        // Nome do osso na CMU -> osso do humanoide do Unity
        private static readonly (string cmu, string unity)[] Mapa =
        {
            ("root", "Hips"),
            ("lfemur", "LeftUpperLeg"), ("ltibia", "LeftLowerLeg"), ("lfoot", "LeftFoot"), ("ltoes", "LeftToes"),
            ("rfemur", "RightUpperLeg"), ("rtibia", "RightLowerLeg"), ("rfoot", "RightFoot"), ("rtoes", "RightToes"),
            ("lowerback", "Spine"), ("upperback", "Chest"), ("thorax", "UpperChest"), ("lowerneck", "Neck"), ("head", "Head"),
            ("lclavicle", "LeftShoulder"), ("lhumerus", "LeftUpperArm"), ("lradius", "LeftLowerArm"), ("lwrist", "LeftHand"),
            ("rclavicle", "RightShoulder"), ("rhumerus", "RightUpperArm"), ("rradius", "RightLowerArm"), ("rwrist", "RightHand"),
        };

        /// <summary>Clipes cortados de cada arquivo: nome, primeiro e último quadro (120 quadros por segundo), se repete.</summary>
        private static readonly Dictionary<string, (string nome, int ini, int fim, bool loop)[]> Clipes = new Dictionary<string, (string, int, int, bool)[]>
        {
            { "Rebatida_CMU", new[] { ("GuardaCMU", 0, 40, true), ("BatidaCMU", 40, 239, false) } },
            { "Arremesso_CMU", new[] { ("ArremessoCMU", 0, 131, false) } },
        };

        /// <summary>Giro (graus) para a frente da gravação bater com a frente do jogador.</summary>
        private const float GiroDaCaptura = 180f;

        [MenuItem("Bets/Importar capturas de movimento (CMU)")]
        public static string Importar()
        {
            var log = new List<string>();
            foreach (string guid in AssetDatabase.FindAssets("t:Model", new[] { Pasta }))
            {
                string caminho = AssetDatabase.GUIDToAssetPath(guid);
                ModelImporter imp = AssetImporter.GetAtPath(caminho) as ModelImporter;
                if (imp == null) continue;
                imp.globalScale = 1f;
                imp.materialImportMode = ModelImporterMaterialImportMode.None;
                imp.importAnimation = true;
                imp.animationType = ModelImporterAnimationType.Human;
                imp.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                imp.autoGenerateAvatarMappingIfUnspecified = true;
                // Primeiro deixa o Unity montar o esqueleto; depois troca o mapa dos ossos pelo da CMU
                imp.SaveAndReimport();
                HumanDescription d = imp.humanDescription;
                var ossos = new List<HumanBone>();
                foreach (var (cmu, unity) in Mapa)
                {
                    HumanBone b = new HumanBone { boneName = cmu, humanName = HumanNome(unity) };
                    b.limit.useDefaultValues = true;
                    ossos.Add(b);
                }
                d.human = ossos.ToArray();
                imp.humanDescription = d;

                string arquivo = System.IO.Path.GetFileNameWithoutExtension(caminho);
                if (Clipes.TryGetValue(arquivo, out var cortes))
                {
                    var lista = new List<ModelImporterClipAnimation>();
                    ModelImporterClipAnimation modelo = imp.defaultClipAnimations.Length > 0 ? imp.defaultClipAnimations[0] : null;
                    foreach (var (nome, ini, fim, loop) in cortes)
                    {
                        var c = new ModelImporterClipAnimation
                        {
                            name = nome,
                            takeName = modelo != null ? modelo.takeName : nome,
                            firstFrame = ini,
                            lastFrame = fim,
                            loopTime = loop,
                            // No lugar: quem anda é o jogo; o corpo gira e se mexe como na captura
                            lockRootRotation = true,
                            lockRootHeightY = true,
                            lockRootPositionXZ = true,
                            keepOriginalOrientation = true,
                            keepOriginalPositionY = true,
                            keepOriginalPositionXZ = true,
                            // A pessoa da captura passa o pé para o lado de trás do jogo: meia-volta para a
                            // passada ir na direção de quem arremessa (que fica à esquerda do rebatedor destro)
                            rotationOffset = GiroDaCaptura,
                        };
                        lista.Add(c);
                    }
                    imp.clipAnimations = lista.ToArray();
                }
                imp.SaveAndReimport();
                Avatar avatar = AssetDatabase.LoadAssetAtPath<Avatar>(caminho);
                log.Add($"{arquivo}: avatar {(avatar != null && avatar.isValid && avatar.isHuman ? "ok" : "COM PROBLEMA")}");
            }
            AssetDatabase.SaveAssets();
            return string.Join("\n", log);
        }

        /// <summary>Nome do osso como o Unity escreve no HumanTrait ("LeftUpperLeg" -> "Left UpperLeg").</summary>
        private static string HumanNome(string unity)
        {
            foreach (string n in HumanTrait.BoneName)
                if (n.Replace(" ", "") == unity) return n;
            return unity;
        }
    }
}
