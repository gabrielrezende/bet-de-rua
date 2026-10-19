using UnityEngine;

namespace BetsDeRua
{
    /// <summary>
    /// Sinal em cima da cabeça do rebatedor: verde = seguro (na zona segura, taco no chão);
    /// vermelho = zona de perigo (a garrafa dele pode ser queimada). Ajuda a ler, de longe,
    /// quem está protegido. Aparece para pessoas e bots.
    /// </summary>
    public class SinalZona : MonoBehaviour
    {
        [SerializeField] private ConfigHUD configHUD;
        [SerializeField] private ConfigFisica configFisica;
        [SerializeField] private Jogador jogador;
        [SerializeField] private Taco taco;
        [SerializeField] private Material materialSeguro;
        [SerializeField] private Material materialPerigo;

        private Transform sinal;
        private MeshRenderer desenho;
        private bool? estavaSeguro;

        private void Awake()
        {
            GameObject bola = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bola.name = "SinalZona";
            Destroy(bola.GetComponent<Collider>());
            bola.transform.SetParent(transform, false);
            desenho = bola.GetComponent<MeshRenderer>();
            desenho.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            sinal = bola.transform;
        }

        private void LateUpdate()
        {
            // Quem a pessoa controla já tem a seta verde/vermelha: a bolinha some nele
            sinal.gameObject.SetActive(jogador.Controlador == null);
            bool seguro = jogador.DentroDoCirculo() && taco != null && taco.NoChao;
            if (estavaSeguro != seguro)
            {
                desenho.sharedMaterial = seguro ? materialSeguro : materialPerigo;
                estavaSeguro = seguro;
            }
            sinal.localPosition = Vector3.up * (jogador.Altura + configHUD.alturaSinalZona);
            sinal.localScale = Vector3.one * configHUD.tamanhoSinalZona;
        }
    }
}
