using System.Collections.Generic;
using UnityEngine;

namespace BetsDeRua
{
    /// <summary>
    /// Os bots (CPU) de campo: comandam todo jogador sem pessoa controlando.
    /// O arremesso e a batida da CPU ficam no Arremessador e no Rebatedor; aqui fica o
    /// que acontece depois que a bola está viva:
    /// - Defesa: o defensor mais perto da bola corre nela e pega (ou mergulha); com a bola,
    ///   vai para cima da garrafa exposta e chuta, ou passa se o colega estiver bem mais perto.
    ///   O outro volta para perto da sua garrafa.
    /// - Ataque: com uma pessoa no taco, o parceiro bot corre em espelho (eles se cruzam no meio).
    ///   Sem ninguém, os dois saem correndo quando a batida dá folga, cruzam e param na outra base.
    /// Comportamento simples de propósito: o GDD ("IA e dificuldade") melhora isso depois.
    /// </summary>
    public class CerebroBots : MonoBehaviour
    {
        [SerializeField] private ConfigPartida configPartida;
        [SerializeField] private ConfigFisica configFisica;
        [SerializeField] private ControladorLance lance;
        [SerializeField] private Corrida corrida;
        [SerializeField] private Bola bola;
        [SerializeField] private Jogador[] rebatedores;
        [SerializeField] private Defensor[] defensores;

        // Folga para considerar que chegou no ponto (m)
        private const float Chegou = 0.25f;
        // Acima desta distância do destino, o bot dá sprint (m)
        private const float DistanciaParaSprint = 3f;
        // O bot mira um pouco à frente da bola em movimento (s)
        private const float Antecipacao = 0.3f;
        // Os rebatedores bot esperam a bola andar um pouco antes de decidir se correm (s)
        private const float EsperaParaDecidirCorrida = 0.5f;
        // E olham para onde a bola vai estar daqui a este tempo (s)
        private const float OlharAFrente = 1f;

        private readonly Dictionary<Jogador, float> reacao = new Dictionary<Jogador, float>();
        private bool viagemDecidida;
        private float tempoDesdeBatida;
        private bool vaoCorrer;
        private readonly Dictionary<Jogador, Vector3> destinoDaViagem = new Dictionary<Jogador, Vector3>();
        private EstadoLance estadoAnterior;

        private void Update()
        {
            if (lance.Estado != estadoAnterior)
            {
                // Lance novo: esquece as decisões do anterior
                if (lance.Estado == EstadoLance.Arremesso)
                {
                    viagemDecidida = false;
                    tempoDesdeBatida = 0f;
                    destinoDaViagem.Clear();
                }
                estadoAnterior = lance.Estado;
            }
            if (lance.Estado != EstadoLance.BolaViva) return;

            if (configPartida.defesaAutomatica) ComandarDefesa();
            if (configPartida.corridaAutomatica) ComandarCorrida();
        }

        // ------------------------------------------------------------------ defesa

        private void ComandarDefesa()
        {
            Arremessador portador = lance.Portador;
            // Enquanto o arremesso ainda pode ser rebatido, a defesa espera no lugar
            bool esperandoBatida = !lance.Rebatida && lance.RebatedorDaVez != null && lance.RebatedorDaVez.BolaEmJogo;
            Defensor perseguidor = portador == null && !esperandoBatida ? MaisPertoDaBola() : null;

            foreach (Defensor defensor in defensores)
            {
                Jogador jogador = defensor.Jogador;
                if (jogador.Controlador != null) continue;

                if (portador != null && portador.Jogador == jogador) JogarComABola(defensor, portador);
                else if (defensor == perseguidor) PerseguirBola(defensor);
                else IrPara(jogador, PosicaoDeGuarda(jogador));
            }
        }

        private void PerseguirBola(Defensor defensor)
        {
            Jogador jogador = defensor.Jogador;
            Vector3 velocidadeBola = bola.Velocidade;
            velocidadeBola.y = 0f;
            Vector3 alvo = bola.Posicao + velocidadeBola * Antecipacao;
            IrPara(jogador, alvo);

            if (defensor.AoAlcance(configFisica.raioPegar))
            {
                if (Reagiu(jogador)) defensor.TentarPegar();
                return;
            }
            // Bola fugindo mais rápido do que ele corre: tenta o mergulho
            bool fugindo = velocidadeBola.magnitude > configFisica.velocidadeSprint;
            if (fugindo && defensor.AoAlcance(configFisica.alcanceMergulho) && Reagiu(jogador)) defensor.TentarPegar();
            if (!defensor.AoAlcance(configFisica.alcanceMergulho)) reacao.Remove(jogador);
        }

        private void JogarComABola(Defensor defensor, Arremessador portador)
        {
            Jogador jogador = defensor.Jogador;
            if (!lance.AlgumaExposta())
            {
                // Nada exposto: segura a bola e espera (o lance acaba com todos seguros)
                IrPara(jogador, jogador.transform.position);
                return;
            }
            Garrafa alvo = lance.EscolherAlvo(jogador.transform.position);
            float minhaDistancia = Vector3.Distance(jogador.transform.position, alvo.transform.position);

            // Colega bem mais perto da garrafa exposta: passa para ele
            Jogador colega = portador.Parceiro;
            if (colega != null)
            {
                float distanciaColega = Vector3.Distance(colega.transform.position, alvo.transform.position);
                if (distanciaColega + configPartida.distanciaChuteBot < minhaDistancia && Reagiu(jogador))
                {
                    portador.ExecutarPeloBot(TipoArremesso.Passe, 0f, colega.transform.position);
                    return;
                }
            }

            if (minhaDistancia > configPartida.distanciaChuteBot)
            {
                IrPara(jogador, alvo.transform.position);
                return;
            }
            IrPara(jogador, jogador.transform.position);
            if (Reagiu(jogador))
            {
                float carga = (configFisica.inicioFaixaVerde + configFisica.fimFaixaVerde) * 0.5f;
                portador.ExecutarPeloBot(TipoArremesso.Forte, carga, alvo.transform.position);
            }
        }

        /// <summary>Onde cada defensor espera: perto da sua garrafa (arremessador na A, guardião na B).</summary>
        private Vector3 PosicaoDeGuarda(Jogador jogador)
        {
            float meio = configFisica.distanciaBases * 0.5f;
            return jogador.Papel == Papel.Arremessador
                ? new Vector3(-meio - configPartida.afastamentoArremessador, 0f, 0f)
                : new Vector3(meio + configPartida.distanciaGuardiao, 0f, 0f);
        }

        private Defensor MaisPertoDaBola()
        {
            Defensor melhor = null;
            float melhorDistancia = float.MaxValue;
            foreach (Defensor defensor in defensores)
            {
                float d = DistanciaNoChao(defensor.transform.position, bola.Posicao);
                if (d < melhorDistancia) { melhor = defensor; melhorDistancia = d; }
            }
            return melhor;
        }

        // ------------------------------------------------------------------ corrida

        private void ComandarCorrida()
        {
            if (!lance.Rebatida || !corrida.Ativa) return;

            Jogador humano = null;
            foreach (Jogador rebatedor in rebatedores)
                if (rebatedor.Controlador != null) humano = rebatedor;

            if (humano != null)
            {
                // Uma pessoa no taco: o parceiro corre em espelho, um pouco para o lado
                foreach (Jogador rebatedor in rebatedores)
                {
                    if (rebatedor == humano) continue;
                    Vector3 h = humano.transform.position;
                    IrPara(rebatedor, new Vector3(-h.x, 0f, h.z - configPartida.afastamentoParceiroEspelho));
                }
                return;
            }

            // Ninguém no taco: depois que a bola anda um pouco, decide uma vez se vale correr
            tempoDesdeBatida += Time.deltaTime;
            if (!viagemDecidida && tempoDesdeBatida >= EsperaParaDecidirCorrida)
            {
                viagemDecidida = true;
                Vector3 velocidadeBola = bola.Velocidade;
                velocidadeBola.y = 0f;
                Vector3 ondeVaiEstar = bola.Posicao + velocidadeBola * OlharAFrente;
                float folga = float.MaxValue;
                foreach (Defensor defensor in defensores)
                    folga = Mathf.Min(folga, DistanciaNoChao(defensor.transform.position, ondeVaiEstar));
                vaoCorrer = folga > configPartida.folgaParaBotCorrer;
                if (vaoCorrer)
                {
                    // Cada um vai para a outra base, um de cada lado, para passarem lado a lado no meio
                    float meio = configFisica.distanciaBases * 0.5f;
                    float lado = configPartida.afastamentoParceiroEspelho * 0.5f;
                    foreach (Jogador rebatedor in rebatedores)
                    {
                        bool estaNaA = rebatedor.transform.position.x < 0f;
                        destinoDaViagem[rebatedor] = new Vector3(estaNaA ? meio : -meio, 0f, estaNaA ? lado : -lado);
                    }
                }
            }
            foreach (Jogador rebatedor in rebatedores)
            {
                if (vaoCorrer && destinoDaViagem.TryGetValue(rebatedor, out Vector3 destino)) IrPara(rebatedor, destino);
                else IrPara(rebatedor, rebatedor.transform.position);
            }
        }

        // ------------------------------------------------------------------ ajudas

        /// <summary>Anda até o ponto; sprint quando está longe; para quando chega.</summary>
        private static void IrPara(Jogador jogador, Vector3 destino)
        {
            Vector3 rumo = destino - jogador.transform.position;
            rumo.y = 0f;
            float distancia = rumo.magnitude;
            if (distancia < Chegou)
            {
                jogador.DefinirEntradaBot(Vector2.zero, false);
                return;
            }
            // Chegando perto, vai mais devagar (para não passar do ponto)
            float intensidade = Mathf.Clamp01(distancia);
            Vector3 direcao = rumo / distancia;
            jogador.DefinirEntradaBot(new Vector2(direcao.x, direcao.z) * intensidade, distancia > DistanciaParaSprint);
        }

        /// <summary>Conta o tempo de reação do bot; true quando já reagiu.</summary>
        private bool Reagiu(Jogador jogador)
        {
            if (!reacao.TryGetValue(jogador, out float tempo)) tempo = 0f;
            tempo += Time.deltaTime;
            if (tempo >= configPartida.reacaoBot)
            {
                reacao.Remove(jogador);
                return true;
            }
            reacao[jogador] = tempo;
            return false;
        }

        private static float DistanciaNoChao(Vector3 a, Vector3 b)
        {
            return new Vector2(a.x - b.x, a.z - b.z).magnitude;
        }
    }
}
