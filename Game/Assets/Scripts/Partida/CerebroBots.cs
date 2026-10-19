using System.Collections.Generic;
using UnityEngine;

namespace BetsDeRua
{
    /// <summary>
    /// Os bots (CPU) de campo: comandam todo jogador sem pessoa controlando.
    /// O arremesso e a batida da CPU ficam no Arremessador e no Rebatedor; aqui fica o
    /// que acontece depois que a bola está viva:
    /// - Defesa: o defensor que chega antes onde a bola vai estar corre nela e pega (ou mergulha); com a bola,
    ///   vai para cima da garrafa exposta e chuta, ou passa se o colega estiver bem mais perto.
    ///   O outro volta para a posição de guarda do lance (ControladorLance.PosicaoDeGuarda).
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

        // Só troca quem persegue a bola se o outro chegar este tanto antes (s)
        private const float VantagemParaTrocarPerseguidor = 0.4f;

        private readonly Dictionary<Jogador, float> reacao = new Dictionary<Jogador, float>();
        private Defensor perseguidorAtual;
        private Vector3 pontoDeInterceptacao;
        private bool viagemDecidida;
        private float tempoDesdeBatida;
        private bool vaoCorrer;
        private readonly Dictionary<Jogador, Vector3> destinoDaViagem = new Dictionary<Jogador, Vector3>();
        private readonly Dictionary<Jogador, Vector3> origemDaViagem = new Dictionary<Jogador, Vector3>();
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
                    origemDaViagem.Clear();
                }
                estadoAnterior = lance.Estado;
            }
            if (lance.Estado == EstadoLance.Arremesso && configPartida.defesaAutomatica) PosicionarParaOArremesso();
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
            Defensor perseguidor = portador == null && !esperandoBatida ? EscolherPerseguidor() : null;
            if (perseguidor == null) perseguidorAtual = null;

            foreach (Defensor defensor in defensores)
            {
                Jogador jogador = defensor.Jogador;
                if (jogador.Controlador != null) continue;

                if (portador != null && portador.Jogador == jogador) JogarComABola(defensor, portador);
                else if (defensor == perseguidor) PerseguirBola(defensor);
                else IrPara(jogador, esperandoBatida ? lance.PosicaoDeGuarda(jogador) : PosicaoDeApoio(jogador));
            }
        }

        /// <summary>
        /// Quem não vai na bola: a defesa anda pelo campo inteiro. Com a bola longe da casinha
        /// que ele guarda, sai para o meio do caminho para receber o passe (e o colega não ter
        /// que correr tudo de volta); com a bola perto, fica guardando a casinha.
        /// </summary>
        private Vector3 PosicaoDeApoio(Jogador jogador)
        {
            Vector3 guarda = lance.PosicaoDeGuarda(jogador);
            Vector3 casinha = lance.CasinhaMaisPerto(guarda).transform.position;
            Vector3 bolaNoChao = new Vector3(bola.Posicao.x, 0f, bola.Posicao.z);
            // Só sai de trás da casinha com a bola solta e isolada lá longe; senão espera atrás
            // (passar no meio, entre o rebatedor e a garrafa, só atrapalha)
            if (lance.Portador != null || DistanciaNoChao(casinha, bolaNoChao) <= configPartida.distanciaParaApoio) return guarda;
            Vector3 apoio = Vector3.Lerp(casinha, bolaNoChao, configPartida.fracaoApoio);
            float limiteZ = configFisica.larguraCampo * 0.5f - 1f;
            apoio.z = Mathf.Clamp(apoio.z, -limiteZ, limiteZ);
            return apoio;
        }

        /// <summary>
        /// Arremesso sem bola morta: ninguém é teleportado, então cada defensor anda até o lugar
        /// dele. O arremessador vai para o meio, atrás da garrafa, para sair de trás do parceiro
        /// rebatedor (senão a bola pega nele: lancha); o outro vai guardar atrás da casinha alvo.
        /// </summary>
        private void PosicionarParaOArremesso()
        {
            foreach (Defensor defensor in defensores)
            {
                Jogador jogador = defensor.Jogador;
                if (jogador.Controlador != null) continue;
                IrPara(jogador, lance.PosicaoDeGuarda(jogador));
            }
        }

        private void PerseguirBola(Defensor defensor)
        {
            Jogador jogador = defensor.Jogador;
            Vector3 velocidadeBola = bola.Velocidade;
            velocidadeBola.y = 0f;
            // Longe: corre para onde vai alcançar a bola; perto: vai direto nela
            bool perto = DistanciaNoChao(jogador.transform.position, bola.Posicao) < configFisica.alcanceMergulho;
            Vector3 alvo = perto ? bola.Posicao + velocidadeBola * Antecipacao : pontoDeInterceptacao;
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
                // Nada exposto: volta para trás da casinha mais perto para arremessar de novo
                // (sem bola morta: o próximo arremesso sai dali)
                IrPara(jogador, lance.PontoParaArremessar(jogador.transform.position));
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
                    RegistroPartida.Evento($"{jogador.Nome} (bot) passa para {colega.Nome}");
                    portador.ExecutarPeloBot(TipoArremesso.Passe, 0f, colega.transform.position);
                    return;
                }
            }

            // Vale jogar? Só se a bola chega na garrafa antes do dono voltar e encostar o taco.
            // Se não chega, errar só dá tempo para o ataque cruzar de novo: anda com a bola
            // para perto da garrafa e prende o ataque na base.
            float rapidezChute = configFisica.forteMaximaKmh / 3.6f;
            // O bot solta na hora (não enche o arco como a pessoa): só a viagem da bola e uma reação
            float tempoBola = configPartida.reacaoChuteBot + minhaDistancia / Mathf.Max(1f, rapidezChute);
            // O dono ainda precisa reagir (perceber e correr) antes de começar a voltar
            float tempoDono = lance.TempoParaProteger(alvo);
            if (tempoDono > 0f) tempoDono += configPartida.reacaoRebatedor;
            bool chegaAntes = tempoBola + configPartida.folgaParaChutarBot < tempoDono;
            if (minhaDistancia > configPartida.distanciaChuteBot || !chegaAntes)
            {
                IrPara(jogador, alvo.transform.position);
                return;
            }
            IrPara(jogador, jogador.transform.position);
            if (Reagiu(jogador))
            {
                float carga = (configFisica.inicioFaixaVerde + configFisica.fimFaixaVerde) * 0.5f;
                RegistroPartida.Evento($"{jogador.Nome} (bot) chuta na {alvo.name}: bola chega em {tempoBola:0.00}s, " +
                                       $"dono protege em {tempoDono:0.00}s");
                // Mira com um desvio de lado ao acaso: o bot não acerta sempre na mosca
                Vector3 rumo = alvo.transform.position - jogador.transform.position;
                rumo.y = 0f;
                Vector3 lado = Vector3.Cross(Vector3.up, rumo.sqrMagnitude > 0.0001f ? rumo.normalized : Vector3.forward);
                float desvioChute = NiveisBots.De(configPartida, jogador).desvioChute;
                Vector3 mira = alvo.transform.position + lado * Random.Range(-desvioChute, desvioChute);
                portador.ExecutarPeloBot(TipoArremesso.Forte, carga, mira);
            }
        }

        /// <summary>
        /// Quem vai na bola: quem chega antes onde ela vai estar (não quem está mais perto
        /// agora). Só troca de perseguidor se o outro chegar bem antes, para não ficar
        /// trocando a cada frame.
        /// </summary>
        private Defensor EscolherPerseguidor()
        {
            Interceptacao.Prever(bola);
            Defensor melhor = Interceptacao.QuemChegaAntes(configFisica, defensores, out Vector3 ponto, out float tempo);
            if (perseguidorAtual != null && melhor != perseguidorAtual)
            {
                float tempoAtual = Interceptacao.TempoParaAlcancar(configFisica, perseguidorAtual.transform.position,
                                                                   out Vector3 pontoAtual);
                if (tempoAtual - tempo < VantagemParaTrocarPerseguidor)
                {
                    pontoDeInterceptacao = pontoAtual;
                    return perseguidorAtual;
                }
            }
            perseguidorAtual = melhor;
            pontoDeInterceptacao = ponto;
            return melhor;
        }

        // ------------------------------------------------------------------ corrida

        private void ComandarCorrida()
        {
            Jogador humano = null;
            foreach (Jogador rebatedor in rebatedores)
                if (rebatedor.Controlador != null) humano = rebatedor;

            // Rebatedor bot sem nada para fazer (antes da batida, ou sem viagem): fica na zona segura,
            // não parado do lado de fora (ex.: logo depois que a pessoa troca de jogador com o L1)
            if (!lance.Rebatida || !corrida.Ativa)
            {
                foreach (Jogador rebatedor in rebatedores)
                {
                    if (rebatedor.Controlador != null || (lance.RebatedorDaVez != null && lance.RebatedorDaVez.Jogador == rebatedor
                                                          && (lance.Estado == EstadoLance.Arremesso || lance.RebatedorDaVez.Ativo))) continue;
                    if (!rebatedor.DentroDoCirculo()) IrPara(rebatedor, PontoNaBase(rebatedor.transform.position));
                }
                return;
            }

            if (humano != null)
            {
                ComandarParceiroDaPessoa(humano);
                return;
            }

            // Ninguém no taco: depois que a bola anda um pouco, decide se vale correr. Chegou na
            // outra base com o ponto feito: decide de novo se arrisca mais uma viagem (regra da rua:
            // dá para ir e voltar quantas vezes quiser enquanto a defesa não pega a bola).
            tempoDesdeBatida += Time.deltaTime;
            if (viagemDecidida && vaoCorrer && ChegaramAoDestino() && !corrida.EsperandoChegarNaBase)
            {
                viagemDecidida = false;
                tempoDesdeBatida = 0f;
            }
            // Ficaram na base mas a bola continua solta (a defesa ainda não pegou): olham de novo
            // de tempo em tempo, porque a bola pode ter ido longe ou a defesa errado
            if (viagemDecidida && !vaoCorrer && lance.Portador == null
                && tempoDesdeBatida >= EsperaParaDecidirCorrida + configPartida.intervaloReavaliarCorrida)
            {
                viagemDecidida = false;
                tempoDesdeBatida = EsperaParaDecidirCorrida;
            }
            if (!viagemDecidida && tempoDesdeBatida >= EsperaParaDecidirCorrida)
            {
                viagemDecidida = true;
                // Dá tempo? A defesa precisa chegar na bola, pegar e jogar na garrafa; eles precisam
                // chegar na zona da outra base. Corre só se a defesa demora mais (com uma folga de risco).
                float tempoDefesa = TempoDaDefesaQueimar();
                float tempoViagem = TempoDaViagem();
                // Com a bola já na mão da defesa, ninguém sai da base
                float margemRisco = NiveisBots.De(configPartida, rebatedores[0]).margemRiscoCorrida;
                vaoCorrer = lance.Portador == null && tempoDefesa > tempoViagem + margemRisco;
                RegistroPartida.Evento($"bots do taco {(vaoCorrer ? "VÃO CORRER" : "ficam na base")} " +
                                       $"(defesa queima em {tempoDefesa:0.0}s, viagem leva {tempoViagem:0.0}s, folga pedida {margemRisco:0.0}s)");
                destinoDaViagem.Clear();
                origemDaViagem.Clear();
                if (vaoCorrer)
                {
                    // Cada um vai para a outra base, um de cada lado, para passarem lado a lado no meio
                    float meio = configFisica.distanciaBases * 0.5f;
                    float lado = configPartida.afastamentoParceiroEspelho * 0.5f;
                    foreach (Jogador rebatedor in rebatedores)
                    {
                        bool estaNaA = rebatedor.transform.position.x < 0f;
                        // Termina ao lado da base (onde o rebatedor fica), fora da linha do arremesso;
                        // um pouco mais para dentro e outro mais para fora, para passarem lado a lado no meio
                        // Termina DENTRO da zona segura, no lugar do rebatedor (a faixa de cada um
                        // para passarem lado a lado no meio fica só no caminho: ver NoCaminho)
                        destinoDaViagem[rebatedor] = ZonaDoTaco.LugarDoRebatedor(new Vector3(estaNaA ? meio : -meio, 0f, 0f),
                                                                                configFisica, configPartida.afastamentoRebatedor);
                        origemDaViagem[rebatedor] = rebatedor.transform.position;
                    }
                }
            }
            foreach (Jogador rebatedor in rebatedores)
            {
                if (vaoCorrer && destinoDaViagem.TryGetValue(rebatedor, out Vector3 destino)) IrPara(rebatedor, NoCaminho(rebatedor, ParaOndeVai(rebatedor, destino)));
                // Sem viagem: fica (ou volta para) dentro da zona segura, nunca parado do lado de fora
                else IrPara(rebatedor, rebatedor.DentroDoCirculo() ? rebatedor.transform.position : PontoNaBase(rebatedor.transform.position));
            }
        }

        /// <summary>
        /// A defesa pegou a bola no meio da viagem: quem ainda não passou do meio desiste e volta
        /// para a base de onde saiu (perde o ponto, mas não é queimado); quem já passou, segue.
        /// </summary>
        private Vector3 ParaOndeVai(Jogador rebatedor, Vector3 destino)
        {
            if (lance.Portador == null || !origemDaViagem.TryGetValue(rebatedor, out Vector3 origem)) return destino;
            bool passouDoMeio = Mathf.Sign(rebatedor.transform.position.x) != Mathf.Sign(origem.x);
            return passouDoMeio ? destino : origem;
        }

        // Parceiro bot de uma pessoa no taco: de que base ele saiu (-1 = A, +1 = B)
        private float baseDoParceiro;
        private bool parceiroViajando;
        private float proximaDecisaoParceiro;   // evita o parceiro ficar indo e voltando a cada passo da pessoa

        /// <summary>
        /// Uma pessoa no taco e o parceiro bot: ele NÃO copia cada passo (senão os dois ficam
        /// expostos juntos). Ele lê a intenção: a pessoa saiu da base correndo para o meio, ele
        /// faz a viagem dele até a outra base; a pessoa desistiu e voltou, ele volta também.
        /// </summary>
        private void ComandarParceiroDaPessoa(Jogador humano)
        {
            Jogador parceiro = rebatedores[0] == humano ? rebatedores[1] : rebatedores[0];
            if (parceiro.Controlador != null) return;

            float meio = configFisica.distanciaBases * 0.5f;
            Vector3 h = humano.transform.position;
            float ladoDaPessoa = h.x < 0f ? -1f : 1f;
            // Velocidade da pessoa rumo ao meio do campo (negativa = voltando para a base dela)
            float rumoAoMeio = -ladoDaPessoa * humano.Velocidade.x;
            float distanciaDaBase = meio - Mathf.Abs(h.x);
            bool podeDecidir = Time.time >= proximaDecisaoParceiro;

            if (!parceiroViajando)
            {
                if (parceiro.DentroDoCirculo()) baseDoParceiro = parceiro.transform.position.x < 0f ? -1f : 1f;
                // Só sai quando a pessoa sai de verdade: já longe da base E correndo para o meio
                bool pessoaFoi = !humano.DentroDoCirculo() && distanciaDaBase >= configPartida.distanciaParaParceiroSair
                                 && rumoAoMeio > configPartida.velocidadeParaParceiroSair;
                if (podeDecidir && pessoaFoi && !corrida.EsperandoChegarNaBase)
                {
                    parceiroViajando = true;
                    proximaDecisaoParceiro = Time.time + configPartida.intervaloDecisaoParceiro;
                    RegistroPartida.Evento($"{parceiro.Nome} (bot) acompanha a corrida de {humano.Nome}");
                }
            }
            else
            {
                // Chegou do outro lado: a viagem acabou
                bool chegou = parceiro.DentroDoCirculo() && (parceiro.transform.position.x < 0f ? -1f : 1f) != baseDoParceiro;
                if (chegou) { parceiroViajando = false; baseDoParceiro = -baseDoParceiro; }
                else if (podeDecidir && !corrida.EsperandoChegarNaBase)
                {
                    // Antes de cruzar, a pessoa virou e está voltando (ou já voltou): ele desiste junto,
                    // se ainda não passou do meio
                    bool pessoaVoltando = humano.DentroDoCirculo() || rumoAoMeio < -configPartida.velocidadeParaParceiroSair;
                    bool passouDoMeio = Mathf.Sign(parceiro.transform.position.x) != baseDoParceiro;
                    if (pessoaVoltando && !passouDoMeio)
                    {
                        parceiroViajando = false;
                        proximaDecisaoParceiro = Time.time + configPartida.intervaloDecisaoParceiro;
                        RegistroPartida.Evento($"{parceiro.Nome} (bot) volta: {humano.Nome} desistiu");
                    }
                }
            }

            Vector3 destino = parceiroViajando
                ? NoCaminho(parceiro, ZonaDoTaco.LugarDoRebatedor(new Vector3(-baseDoParceiro * meio, 0f, 0f),
                                                                  configFisica, configPartida.afastamentoRebatedor))
                : PontoNaBase(parceiro.transform.position);
            IrPara(parceiro, destino);
        }

        /// <summary>
        /// No meio do caminho para a base, cada um corre numa faixa (um mais para dentro, outro mais
        /// para fora), para passarem lado a lado no meio sem trombar; perto da base, vai direto ao ponto.
        /// </summary>
        private Vector3 NoCaminho(Jogador rebatedor, Vector3 destino)
        {
            if (Mathf.Abs(rebatedor.transform.position.x - destino.x) < configPartida.distanciaSairDaFaixa) return destino;
            float faixa = Mathf.Sign(destino.x) * configPartida.afastamentoParceiroEspelho * 0.5f;
            return new Vector3(destino.x, 0f, destino.z + faixa);
        }

        /// <summary>Um ponto dentro da zona segura da base mais perto (onde o rebatedor fica parado).</summary>
        private Vector3 PontoNaBase(Vector3 de)
        {
            float meio = configFisica.distanciaBases * 0.5f;
            return ZonaDoTaco.LugarDoRebatedor(new Vector3(de.x < 0f ? -meio : meio, 0f, 0f), configFisica, configPartida.afastamentoRebatedor);
        }

        /// <summary>
        /// Quanto tempo (s) a defesa leva para queimar: o mais rápido chegar na bola, pegar e o chute
        /// voar até a garrafa. Com a bola já na mão de alguém, só o chute.
        /// </summary>
        private float TempoDaDefesaQueimar()
        {
            float chegar = 0f;
            if (lance.Portador == null)
            {
                Interceptacao.Prever(bola);
                if (Interceptacao.QuemChegaAntes(configFisica, defensores, out _, out float tempo) == null) return float.MaxValue;
                chegar = tempo;
            }
            return chegar + configPartida.tempoDefesaPegarEJogar;
        }

        /// <summary>Quanto tempo (s) o rebatedor mais lento leva até a zona segura da outra base, em sprint.</summary>
        private float TempoDaViagem()
        {
            float meio = configFisica.distanciaBases * 0.5f;
            float pior = 0f;
            foreach (Jogador rebatedor in rebatedores)
            {
                Vector3 p = rebatedor.transform.position;
                Vector3 garrafaDoOutroLado = new Vector3(p.x < 0f ? meio : -meio, 0f, 0f);
                float distancia = ZonaDoTaco.DistanciaAteZona(p, garrafaDoOutroLado, configFisica);
                pior = Mathf.Max(pior, distancia / Mathf.Max(0.1f, configFisica.velocidadeSprint) + configPartida.tempoArrancadaCorrida);
            }
            return pior;
        }

        /// <summary>True se os dois rebatedores bot terminaram a viagem: chegaram no destino ou voltaram para a origem.</summary>
        private bool ChegaramAoDestino()
        {
            foreach (Jogador rebatedor in rebatedores)
            {
                if (!destinoDaViagem.TryGetValue(rebatedor, out Vector3 destino)) continue;
                bool noDestino = rebatedor.DentroDoCirculo()
                                 && Mathf.Sign(rebatedor.transform.position.x) == Mathf.Sign(destino.x);
                // Voltar para a origem só encerra a viagem se eles desistiram (a defesa pegou a bola);
                // no começo da viagem eles ainda estão na origem e isso não é "chegar"
                bool naOrigem = lance.Portador != null && origemDaViagem.TryGetValue(rebatedor, out Vector3 origem)
                                && DistanciaNoChao(rebatedor.transform.position, origem) <= Chegou;
                if (!noDestino && !naOrigem) return false;
            }
            return true;
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
            if (tempo >= NiveisBots.De(configPartida, jogador).reacao)
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
