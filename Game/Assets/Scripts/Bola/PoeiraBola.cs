using UnityEngine;

namespace BetsDeRua
{
    /// <summary>
    /// Poeirinha que levanta do chão quando a bola quica forte ou a garrafa cai (referência: o
    /// teaser do FC 26, com a grama levantando no chute). Um sistema de partículas só, reaproveitado.
    /// </summary>
    public static class PoeiraBola
    {
        private static ParticleSystem sistema;

        /// <summary>Solta poeira em 'onde', com 'forca' de 0 a 1 (quanto mais forte, mais poeira).</summary>
        public static void Soltar(Vector3 onde, float forca, ConfigHUD config)
        {
            if (config == null || config.materialPoeira == null || forca <= 0.05f) return;
            if (sistema == null) sistema = Criar(config);
            var emitir = new ParticleSystem.EmitParams { applyShapeToPosition = true, position = onde };
            sistema.Emit(emitir, Mathf.RoundToInt(Mathf.Lerp(3f, config.particulasPoeira, Mathf.Clamp01(forca))));
        }

        private static ParticleSystem Criar(ConfigHUD config)
        {
            GameObject g = new GameObject("Poeira");
            ParticleSystem ps = g.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var principal = ps.main;
            principal.playOnAwake = false;
            principal.loop = false;
            principal.simulationSpace = ParticleSystemSimulationSpace.World;
            principal.startLifetime = new ParticleSystem.MinMaxCurve(config.duracaoPoeira * 0.6f, config.duracaoPoeira);
            principal.startSpeed = new ParticleSystem.MinMaxCurve(0.3f, 1.2f);
            principal.startSize = new ParticleSystem.MinMaxCurve(config.tamanhoPoeira * 0.5f, config.tamanhoPoeira);
            principal.startColor = config.corPoeira;
            principal.gravityModifier = -0.02f;
            principal.maxParticles = 300;
            var forma = ps.shape;
            forma.shapeType = ParticleSystemShapeType.Hemisphere;
            forma.radius = 0.08f;
            var tamanho = ps.sizeOverLifetime;
            tamanho.enabled = true;
            tamanho.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.6f, 1f, 1.8f));
            var cor = ps.colorOverLifetime;
            cor.enabled = true;
            Gradient gradiente = new Gradient();
            gradiente.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                              new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.1f), new GradientAlphaKey(0f, 1f) });
            cor.color = gradiente;
            var arrasto = ps.limitVelocityOverLifetime;
            arrasto.enabled = true;
            arrasto.drag = 2.5f;
            ParticleSystemRenderer desenho = g.GetComponent<ParticleSystemRenderer>();
            desenho.sharedMaterial = config.materialPoeira;
            desenho.sortMode = ParticleSystemSortMode.Distance;
            return ps;
        }
    }
}
