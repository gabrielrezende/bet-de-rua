using UnityEngine;

namespace BetsDeRua
{
    /// <summary>
    /// Cinemática inversa de dois ossos (braço: ombro-cotovelo-mão; perna: quadril-joelho-pé),
    /// resolvida direto nos ossos depois da animação. Respeita o comprimento dos ossos: se o alvo
    /// estiver longe, o braço estica até o limite em vez de deslocar o ombro.
    /// </summary>
    public static class IKDoisOssos
    {
        /// <summary>
        /// Leva a ponta ('fim') até o 'alvo', dobrando a junta do meio para o lado da 'dica'
        /// (o cotovelo ou o joelho apontam para ela). 'peso' de 0 (animação) a 1 (alvo).
        /// </summary>
        public static void Resolver(Transform raiz, Transform meio, Transform fim, Vector3 alvo, Vector3 dica, float peso)
        {
            if (raiz == null || meio == null || fim == null || peso <= 0f) return;
            Vector3 a = raiz.position;
            float la = Vector3.Distance(a, meio.position), lb = Vector3.Distance(meio.position, fim.position);
            Vector3 destino = Vector3.Lerp(fim.position, alvo, peso);
            Vector3 ate = destino - a;
            float d = Mathf.Clamp(ate.magnitude, Mathf.Abs(la - lb) + 0.001f, la + lb - 0.001f);
            Vector3 dir = ate.normalized;
            // Ângulo no ombro/quadril pela lei dos cossenos
            float cos = (la * la + d * d - lb * lb) / (2f * la * d);
            float angulo = Mathf.Acos(Mathf.Clamp(cos, -1f, 1f)) * Mathf.Rad2Deg;
            Vector3 normal = Vector3.Cross(dir, dica - a);
            if (normal.sqrMagnitude < 1e-6f) normal = Vector3.Cross(dir, Vector3.up);
            Vector3 posMeio = a + Quaternion.AngleAxis(angulo, normal.normalized) * dir * la;
            raiz.rotation = Quaternion.FromToRotation(meio.position - a, posMeio - a) * raiz.rotation;
            Vector3 destinoFinal = a + dir * d;
            meio.rotation = Quaternion.FromToRotation(fim.position - meio.position, destinoFinal - meio.position) * meio.rotation;
        }

        /// <summary>Gira um osso em volta do eixo vertical do mundo (o tronco torcendo).</summary>
        public static void Torcer(Transform osso, float graus, Vector3 eixo)
        {
            if (osso == null || Mathf.Abs(graus) < 0.01f) return;
            osso.rotation = Quaternion.AngleAxis(graus, eixo) * osso.rotation;
        }
    }
}
