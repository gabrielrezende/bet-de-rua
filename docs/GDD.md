# BETS DE RUA — Game Design Document

Oct 18, 2026 · @Gabriel Rezende

## Visão geral

BETS DE RUA é o jogo de taco da rua levado para PC e consoles: partidas 2 contra 2 de 3 a 5 minutos, online ou no sofá, com controle na mão, no estilo FIFA e Madden.

- **O que é o jogo real:** bete-ombro, também chamado de bets, bete, betes, taco, tacobol, casinha, castelo ou pau na lata. É um jogo brasileiro de rua, parente do críquete, disputado entre duas duplas.
- **Pitch:** a mesma bagunça boa da infância — base de giz no asfalto, garrafa PET como casinha, taco de madeira, criança correndo atrás da bolinha — com controles simples e uma camada online de times, convites e ranking.
- **Plataformas:** PC (Steam e Steam Deck) primeiro, feito para joystick e também teclado e mouse; depois PlayStation, Xbox e Switch, com crossplay. Celular só no futuro, em versão simplificada, se houver demanda.
- **Público:** quem jogou na rua nos anos 80, 90 e 2000 (nostalgia) e a geração nova que nunca viu o jogo (descoberta).
- **Diferencial:** quase nenhum concorrente digital do bets; jogos de críquete e beisebol existem, mas nenhum tem a dinâmica de cruzar os tacos com a dupla.

O maior desafio de design é que cada um dos 4 jogadores vive o mesmo lance de um jeito diferente: quem arremessa, quem rebate, quem guarda a outra casinha e quem corre atrás da bola. As seções de câmera e controles resolvem isso.

**Propósito:** relembrar a rua para quem viveu, e mostrar para a geração nova o que dava para ter na rua, assim como o futebol de rua. O jogo é ponte entre quem jogou de chinelo no asfalto e quem nunca viu uma garrafa PET virar casinha.

## Veredito do Conselho de Inovadores

**Vale fazer, mas menor do que este documento descreve.** O conselho votou 3 GO e 4 PIVOT: a ideia é original e tem público; o escopo atual é de estúdio, não de um fundador com emprego full-time. Os conselheiros são arquétipos baseados em frameworks públicos, não as pessoas reais.

**Placar**

| Critério | Nota (0 a 10) |
| --- | --- |
| Problema real (desejo de jogar) | 7 |
| Tamanho do mercado | 5 |
| Diferenciação | 8 |
| Viabilidade para este fundador | 5 |
| Modelo de receita | 5 |
| Timing | 6 |

**Votos**

| Conselheiro | Voto | Por quê |
| --- | --- | --- |
| Engenheiro de Primeiros Princípios | PIVOT | Posicionar lá fora como "street cricket", não só nostalgia brasileira |
| Obcecado pelo Cliente | GO | O cliente existe e a manchete se vende sozinha |
| Investidor de Valor | PIVOT | Sem fosso técnico; escopo menor e comunidade como defesa |
| Fundador de Startup | GO | Testar com protótipo barato e decidir com dados |
| Arquiteto da Oferta | PIVOT | Trocar free-to-play por jogo pago barato com passe de amigo |
| Contrarian | GO | Nicho que ninguém digitalizou direito |
| Designer de Produto | PIVOT | Cortar até sobrarem a batida, a cruzada e a casinha caindo |

**Principais desacordos**

- Online no lançamento ou não: venceu lançar com modo sofá + Steam Remote Play Together, sem netcode próprio no início.
- Pago ou gratuito: venceu preço baixo (R$ 20 a 40, estimativa) com passe de amigo, para a turma toda conseguir jogar.
- Mercado pequeno: o Brasil sustenta o início; para crescer fora, vender como "street cricket".

**As 3 formas mais prováveis de dar errado e a defesa**

1. Não ser divertido depois de 20 partidas → testar cedo com a turma; variar física por arena, tipos de arremesso e regras da vila.
2. Fila online vazia → lançar com sofá, Remote Play e bots; matchmaking só com base de jogadores.
3. O projeto nunca terminar → vertical slice com 1 arena e 2x2 no sofá; avaliar parceria com dev de jogos.

**Versão aprovada:** PC/Steam, 1 arena, 2x2 no sofá + Remote Play, jogo pago barato com passe de amigo. Online próprio, ranqueada, consoles e o resto deste documento só depois de provar tração.

**O que viraria NO-GO:** a turma jogar o protótipo e não pedir "mais uma" depois de 5 partidas.

**Próximos passos**

- [ ] Em 7 dias: protótipo cinza com 1 campo, 4 controles locais, arremesso, batida, corrida e cruzada; jogar com 3 amigos e contar quantas partidas pedem
- [ ] Em 30 dias: perfil nas redes e página de captura com a enquete "como chamava na sua rua?"; meta de 1.000 seguidores ou 500 e-mails
- [ ] Em 60 dias: se os dois passos derem certo, página na Steam para lista de desejos e decisão entre parceria ou contratação

## Regras do jogo de rua

O bets tem muitas variações regionais; abaixo estão as regras mais comuns encontradas nas fontes, que viram a base do jogo digital.

**Campo e equipamento**

| Item | Como é na rua |
| --- | --- |
| Casinhas | Duas, uma em cada ponta, a cerca de 20 m uma da outra. Garrafa PET, lata ou três gravetos. |
| Base | Círculo de giz com cerca de 60 cm de diâmetro em volta de cada casinha. |
| Tacos | Dois, de madeira, cerca de 73 cm, faces planas. |
| Bola | Bolinha de tênis, de borracha ou até de meia. |

**Papéis**

- **Dupla do taco (ataque):** cada um fica numa casinha, com o taco encostado no chão dentro da base ("taco no chão"). Objetivo: proteger a casinha e marcar pontos.
- **Dupla da bola (defesa):** um arremessa da casinha oposta tentando derrubar a casinha do rebatedor; o outro fica atrás para buscar a bola e devolver.

**Como o jogo anda**

1. Decide-se quem começa com o taco no par ou ímpar, ou no "molhado ou seco" (molha um lado do taco e joga para o alto).
2. O arremessador joga a bola na casinha do rebatedor.
3. Se o rebatedor acertar a bola, enquanto a defesa corre atrás dela a dupla do taco corre entre as bases, batendo os tacos no meio do campo. Cada cruzada conta ponto (1 ou 2 pontos, conforme a regra combinada).
4. Ao chegar na outra base, o taco precisa encostar dentro do círculo para validar.
5. A defesa pega a bola e tenta derrubar uma casinha enquanto o taco está fora da base. Se conseguir, as duplas trocam de papel.

**Como se perde o taco (troca de papel)**

- A bola arremessada derruba a casinha.
- A casinha é derrubada durante a corrida, com o taco fora da base (o rebatedor é "queimado").
- A bola é rebatida para trás da casinha 3 vezes.
- A bola rebatida acerta o próprio rebatedor, o parceiro, o taco ou a casinha da própria dupla.
- A defesa pega a bola no ar, sem quicar: em algumas regras perde o taco, em outras perde o jogo direto.

**Como se ganha**

- A meta é combinada antes: 10 pontos é o mais comum na rua, 12 ou 24/25 em torneios.
- Ao atingir a meta, a dupla ainda precisa fechar o jogo: rebater mais uma vez, cruzar os tacos no meio e, em muitas regras, derrubar a casinha onde o parceiro está.
- Se a dupla com a pontuação máxima perder o taco antes de fechar, os pontos caem pela metade.

**Costumes que entram no jogo**

- "Licença, bets!": pausa combinada para trocar de taco ou de lugar; quem sai da base sem pedir pode ser queimado.
- Bola perdida: o jogo para até alguém achar a bolinha.
- Desempate "taco-ombro": a dupla arremessadora tem 5 chances para derrubar a casinha 3 vezes.

## Regras adaptadas para o digital

O modo padrão usa a regra que você lembra: 2x2, cada cruzada no meio vale 1 ponto, quem fecha 10 ganha. As variações viram opções de sala privada.

| Regra | Modo padrão (ranqueado) | Opção em sala privada |
| --- | --- | --- |
| Valor da cruzada | 1 ponto | 2 pontos |
| Meta | 10 pontos + cruzada final de fechamento | 5, 12 ou 24 |
| Bola pega no ar | Perde o taco | Perde o jogo ("regra de vila") |
| Bola para trás | 3 para trás = perde o taco | Desligado |
| Perdeu o taco com a meta batida | Pontos caem pela metade | Desligado |
| Licença, bets | 1 pausa de 30 s por dupla por partida | Ilimitada |
| Tempo limite | 8 min; ao fim, ganha quem tem mais pontos, empate vai para o taco-ombro | Sem limite |

**Ajustes que o digital exige**

- **Bola perdida:** não existe. A bola tem limite de campo (muros, carros, portão); se sair, volta automaticamente para o fielder mais perto após 2 s e conta como "bola no mato": a dupla do taco ganha 1 ponto bônus.
- **Queimar o rebatedor:** a casinha só é derrubada se a bola acertar a garrafa enquanto o taco do dono está fora do círculo de giz; a física decide, sem juiz.
- **Taco no chão:** o rebatedor parado dentro do círculo está sempre protegido; o jogo mostra o círculo verde quando está seguro e vermelho quando está exposto.
- **Cruzada:** é validada automaticamente quando os dois tacos se encontram na faixa central, com animação e som de "tec!" de madeira.
- **Quem começa:** minigame de "molhado ou seco" com o taco girando no ar, decidido por sorteio.

**Rodízio: quem arremessa e quem rebate**

- **Os dois rebatem.** A defesa arremessa alternando as casinhas, como na rua: o arremessador joga na casinha B; depois do lance, quem está com a bola do lado B (o guardião) arremessa na casinha A. Assim os dois rebatedores enfrentam arremessos e os dois defensores arremessam e guardam.
- **Quem começa:** o sorteio de molhado ou seco define a dupla do taco e de que lado sai o primeiro arremesso.
- **Troca de papel:** depois de uma queimada, bola pega no ar ou terceira bola para trás, os quatro trocam de função sem andar: a dupla da bola pega os tacos onde está e a outra dupla vira defesa. Um contador de 3 s mostra a troca na tela.
- **Posição do guardião:** o defensor que não arremessa vai sempre para trás da casinha que vai receber o arremesso; no 1x1, a IA posiciona sozinha.

**Fim da partida**

1. **Match point:** ao chegar na meta (10 pontos no padrão), a dupla entra em match point; o placar pisca e a trilha muda.
2. **Fechar o jogo:** precisa de mais uma batida com cruzada validada nas duas bases. Na regra de vila, além disso, um dos dois derruba a casinha onde o parceiro está, com o próprio taco, para fechar como na rua.
3. **Perdeu o taco no match point:** os pontos caem pela metade (opção ligada no padrão).
4. **Tempo limite:** 8 minutos no ranqueado. Esgotado, vence quem tem mais pontos.
5. **Empate:** desempate taco-ombro, com a dupla da bola tendo 5 chances para derrubar a casinha 3 vezes; depois inverte. Persistindo, morte súbita: o primeiro ponto validado vence.

## Câmeras: a visão de cada jogador

A câmera muda conforme o papel e a fase do lance: ninguém controla a câmera manualmente, ela se posiciona sozinha para o que aquele jogador precisa fazer agora.

**Os 4 papéis num lance**

| Papel | Quem | O que precisa ver | Câmera |
| --- | --- | --- | --- |
| Arremessador | Defesa, na casinha A | A casinha alvo, o rebatedor e a trajetória da bola | Terceira pessoa por trás do ombro, olhando para a casinha B a 20 m; zoom leve no alvo durante a mira |
| Fielder (catador) | Defesa, atrás da casinha B | A bola vindo, para onde ela vai depois da rebatida, o parceiro | Antes da batida: atrás da casinha B, de frente para o arremessador. Depois: câmera aérea seguindo a bola |
| Rebatedor | Ataque, na casinha B | A bola chegando, o momento da batida | Terceira pessoa baixa, ao lado da casinha, de frente para o arremesso; a bola cresce na tela |
| Parceiro de base | Ataque, na casinha A | O lance inteiro e a hora de correr | Visão lateral do campo inteiro, como de cima do muro; ele vê o arremesso, a batida e onde a bola caiu |

**As 3 fases do lance e o que muda**

1. **Arremesso (2 a 4 s):** tela do arremessador fica em mira; tela do rebatedor fica em espera tensa, com a bola vindo; os outros dois veem o campo de lado.
2. **Bola rebatida (corrida):** todas as câmeras abrem para uma visão aérea inclinada do campo, porque agora todo mundo precisa saber onde está a bola e onde estão os tacos. A defesa vê uma seta para a bola; o ataque vê a faixa do meio e o parceiro.
3. **Retorno (defesa com a bola):** quem pegou a bola ganha câmera por trás do ombro com mira na casinha mais vulnerável ou no parceiro para passar. O ataque vê em tela dividida pequena (picture-in-picture) quem está com a bola.

**Regras de ouro da câmera**

- A bola nunca sai da tela de ninguém: quando sai, aparece um indicador na borda com a distância.
- Transições de câmera duram no máximo 0,4 s, para não perder o tempo de reação.
- O jogador pode trocar entre câmera automática e aérea fixa. No modo sofá, a tela se divide em 2 ou 4, ou todos usam a câmera aérea compartilhada, como no FIFA.
- Espectadores e replays usam uma câmera "de cima do poste", mostrando o campo inteiro em diagonal.

O diagrama no fim desta seção mostra o campo e de onde cada câmera olha.

&#91;embedded content: campo de bets · 4 papéis e suas câmeras\]

No arremesso, o ataque fica um em cada casinha; a defesa tem o arremessador na casinha A e o fielder atrás da casinha B. Depois da batida, todas as câmeras abrem para a vista aérea.

**Câmera de transmissão (padrão do 1x1 e do sofá)**

No modo em que cada pessoa controla a dupla inteira, a câmera padrão é a de transmissão, igual ao FIFA: lateral, elevada, mostrando a rua inteira de uma casinha à outra. Como o campo do bets é comprido e estreito, essa vista mostra tudo de uma vez: as duas casinhas, a bola, quem corre atrás dela e a faixa do meio.

- **Sempre os quatro na tela:** a câmera enquadra os 4 jogadores e a bola o tempo todo, deslizando de lado e se afastando o quanto precisar (até 2x). Se a bola for longe demais para caber todo mundo, a bola tem prioridade e a câmera vai atrás dela. (Decisão do protótipo, Etapa 3: substitui o "segue a bola" e o zoom no arremesso.)
- **Sem zoom no arremesso:** a câmera não aproxima na mira, para não tirar ninguém da tela; a sombra da bola e as marcas no chão resolvem a profundidade da vista de lado.
- **Linha de trajetória:** enquanto mira, uma linha pontilhada no chão sai de quem arremessa ou rebate. No sofá todo mundo vê a mesma tela, então a linha é **curta** (cerca de 5 m): mostra a direção sem entregar o ponto exato. Online, cada um poderá ver a linha inteira na própria tela.
- **Marcadores:** um triângulo colorido em cima de cada personagem controlado por você; o ativo pisca. O nome do jogador aparece embaixo, como no FIFA.
- **Minimapa:** faixa fina no rodapé com as duas casinhas, os 4 jogadores e a bola, útil quando a bola vai para fora da tela.
- **Alternativas no menu:** câmera de transmissão aproximada, câmera alta (de cima do poste) e a câmera por papel descrita acima, padrão no online 2x2.

## Controles

**Controle recomendado, teclado e mouse suportados.** Como no FIFA, o jogo inteiro funciona nos dois, mas é desenhado e balanceado para o controle (Xbox, PlayStation ou genérico): analógico dá corrida e mira com intensidade, gatilho dá força progressiva e a vibração faz parte do feedback.

- Na primeira abertura com teclado, uma tela avisa "Recomendado jogar com controle" e mostra os comandos de teclado, sem obrigar nada.
- Os ícones dos botões na tela mudam sozinhos para o dispositivo em uso (Xbox, PlayStation ou teclas), inclusive se trocar no meio da partida.
- No modo sofá, cada controle conectado é um jogador; teclado e mouse contam como mais um.
- No teclado, a força da batida e do arremesso usa o tempo segurando o clique, para compensar a falta do gatilho analógico.
- Na página da Steam: "Suporte total a controle", com teclado e mouse listados como alternativa.
- No online, não há vantagem para nenhum dos dois: as janelas de tempo são as mesmas.

O esquema segue três princípios do FIFA: **poucos botões**, **cada botão com o mesmo sentido em qualquer situação** e **o contexto decide o resto** (o mesmo botão rebate ou passa, conforme o momento). Os botões aparecem no padrão PlayStation, com Xbox entre parênteses.

**O sentido de cada botão**

- **○ (B) = ação forte**, como o chute: batida forte, arremesso forte, chute na garrafa. Segurar enche a força; soltar executa.
- **X (A) = ação curta e segura**, como o passe: batida de toque, arremesso quicado, passe, cruzar os tacos.
- **△ (Y) = por cima**, como o lançamento: cavadinha, pingo, lançamento longo.
- **□ (X) = pegar a bola**, do chão ou no ar; com direção, mergulho.
- **R1 (RB) + ação = colocado**, com precisão, como o chute colocado. O efeito na bola vai no analógico direito.
- **R2 (RT) = correr**; para o rebatedor, é também sair da base.
- **L1 (LB) = trocar de jogador**; o analógico direito escolhe um jogador específico.
- **L2 (LT) = andar devagar / postura**: o guardião anda de lado olhando a bola, como a marcação do FIFA.

**Com a bola na mão para arremessar, o esquema vira "twin-stick"** (decisão do protótipo, Etapa 3): o analógico esquerdo anda, o direito mira, e os gatilhos e botões de cima arremessam — **R2 (RT) forte, R1 (RB) quicado, L2 (LT) pingo**, sempre segurando para encher o arco de força e soltando na faixa verde. Como quem está com a bola não dá sprint (4 m/s), o R2 fica livre para arremessar. Os botões da frente (○, X, △, □) ficam livres nesse momento para efeitos e jogadas especiais (a definir).

**Taco no chão automático.** Parado dentro do círculo, o taco do rebatedor fica encostado no chão sozinho e ele não pode ser queimado. Sair do círculo (R2 ou empurrar o analógico para fora) levanta o taco e deixa a casinha exposta. A tensão continua toda na decisão de quando sair, que é exatamente o que o guardião espera. Para quem quiser a experiência mais fiel à rua, existe o **modo clássico** nas opções: segurar L2 (LT) para manter o taco no chão.

**Atacando (rebatedor)**

| Ação | PlayStation (Xbox) | Como funciona |
| --- | --- | --- |
| Mirar a batida | Analógico esquerdo, parado no círculo | A linha no chão mostra para onde a bola vai |
| Batida forte | ○ (B), segurar e soltar | Segura enquanto a bola vem; solta no tempo certo. Bola longe, mais tempo para cruzar |
| Batida de toque | X (A) | Rasteira e curta: quase nunca é pega no ar, mas dá pouco tempo |
| Cavadinha | △ (Y) | Passa por cima do fielder; vai longe, mas arrisca ser pega no ar |
| Batida colocada | R1 + ○ (RB + B) | Menos força, mais precisão: manda a bola no canto vazio |
| Não rebater | Não apertar nada | O taco no chão protege a garrafa; sem chance de ponto |
| Trocar de rebatedor (1x1) | L1 (LB) | Normalmente automático: fica ativo quem vai receber o arremesso |

**Correndo e cruzando**

| Ação | PlayStation (Xbox) | Como funciona |
| --- | --- | --- |
| Sair correndo | R2 (RT) | Levanta o taco e sai da base; no 1x1, manda a dupla junta |
| Mover na corrida | Analógico esquerdo | Desviar do fielder e escolher a linha até a faixa do meio |
| Cruzar | X (A) no anel | Dentro da faixa e a até 1,5 m do parceiro, aparece um anel; apertar quando fica verde bate os tacos |
| Ficar na base | Parar dentro do círculo | Encosta o taco: valida o ponto e deixa protegido |
| Mais uma | R2 (RT) de novo | Sai da base para outra cruzada |

**Arremessando**

| Ação | PlayStation (Xbox) | Como funciona |
| --- | --- | --- |
| Andar com a bola | Analógico esquerdo | Até 4 m/s, sem sprint. Pode ir para trás e para os lados, mas não passa à frente da própria garrafa (rumo ao alvo) |
| Mirar | Analógico direito | Solto = meio da garrafa alvo; empurrado = até 2 m para o lado. Círculo amarelo na garrafa e linha curta no chão |
| Arremesso forte | R2 (RT), segurar e soltar | Reto e rápido, 55 a 90 km/h |
| Arremesso quicado | R1 (RB), segurar e soltar | Quica pouco antes da garrafa e sobe nela, 45 a 65 km/h; mais difícil de rebater bem |
| Pingo | L2 (LT), segurar e soltar | Alto e lento, cai por cima da garrafa, 32 a 45 km/h |
| Arco de força | Segurando qualquer um dos três | Enche ao lado do jogador. Soltar na faixa verde = força e precisão máximas; antes = mais lento e um pouco torto; depois = bola desgovernada |
| Efeito | Botões da frente (a definir) | Antes o efeito ia no analógico direito; com ele mirando, o efeito passa para os botões da frente (ver Arsenal de efeitos) |
| Finta | A definir (botão da frente) | Ameaça arremessar e faz o rebatedor bater antes da hora |

No teclado: WASD anda, o mouse mira, clique esquerdo = forte, clique direito = quicado, E = pingo.

**Arsenal de efeitos**

Como o beisebol tem nome para cada arremesso, o bets ganha o seu repertório com nome de rua. O tipo de arremesso (○, X ou △) combinado com a direção do analógico direito forma cada um.

> Atualização (Etapa 3 do protótipo): os tipos agora saem de R2 (forte), R1 (quicado) e L2 (pingo), e o analógico direito virou a mira. Na tabela abaixo, leia ○ = R2, X = R1, △ = L2; como o efeito vai ser escolhido (provavelmente pelos botões da frente) ainda está em aberto.

| Nome | Como sai | O que a bola faz |
| --- | --- | --- |
| Reta | ○ sem efeito | Rápida e direta na garrafa |
| Banana | ○ + analógico para o lado | Faz curva no ar e entra pela lateral |
| Folha seca | ○ + analógico para cima | Vem alta e cai de repente perto da garrafa |
| Boia | ○ segurando pouco + analógico para baixo | Lenta e enganosa, quebra o tempo do rebatedor |
| Quique torto | X + analógico para o lado | Quica e muda de direção no quique |
| Morta | X + analógico para baixo | Quica e quase não sobe; difícil de levantar |
| Pingo envenenado | △ + analógico para o lado | Cai por cima da garrafa com curva |

- **Custo:** quanto mais efeito, mais a mira treme e menor a margem de acerto na garrafa. Efeito é risco, não vantagem grátis.
- **Leitura:** a bola mostra a rotação (a costura girando e um rastro sutil de cor diferente por efeito). Com prática, o rebatedor lê o arremesso; no modo assistido aparece um ícone com o nome.
- **Rebatida com efeito:** o rebatedor também pode usar o analógico direito na batida para cortar a bola para longe do fielder (batida cortada), com menos força.
- **Assinatura:** cada jogador tem estatística de qual efeito mais usa e mais acerta; o efeito favorito aparece no perfil e vira provocação entre amigos.

**Defendendo em campo (fielder e guardião)**

| Ação | PlayStation (Xbox) | Como funciona |
| --- | --- | --- |
| Mover / correr | Analógico esquerdo + R2 (RT) | O círculo no chão marca onde a bola cai ou para |
| Pegar a bola | □ (X) | Até 1 m da bola. No ar, um anel encolhendo marca o tempo; pegou antes de quicar, o ataque perde o taco |
| Mergulho | □ (X) + direção | Alcança bola longe; errou, a bola escapa e dá tempo ao ataque |
| Postura de goleiro | L2 (LT), segurar | O guardião anda de lado sempre de frente para a bola |
| Chutar na garrafa | ○ (B), segurar e soltar | Mira automática na casinha exposta (fica vermelha); de longe a mira treme |
| Passe | X (A) | Rasteiro, na direção do analógico; sem direção, vai direto no colega |
| Lançamento | △ (Y) | Por cima, até o colega do outro lado |
| Trocar de defensor | L1 (LB) ou analógico direito | Normalmente automático: fica ativo quem está mais perto da bola |

**Sempre disponíveis**

| Ação | PlayStation (Xbox) |
| --- | --- |
| Comandos rápidos ("Corre!", "Fica!", "Joga pra mim!", "Licença, bets!") | Direcional |
| Placar e replay do último lance | Touchpad (View) |
| Pausa (licença, bets) | Options (Menu) |

**Teclado e mouse**

| Ação | Tecla |
| --- | --- |
| Mover | WASD |
| Mirar | Mouse |
| Correr / sair da base / mais uma | Shift |
| Andar devagar / postura de goleiro | Alt |
| Ação forte | Clique esquerdo (segurar e soltar) |
| Arremesso forte / quicado / pingo (com a bola) | Clique esquerdo / clique direito / E (segurar e soltar) |
| Ação curta (toque, quicado, passe, cruzar) | Espaço |
| Por cima | E |
| Colocado / efeito | Ctrl + ação |
| Pegar / mergulho | R (+ direção) |
| Trocar de jogador | Q |
| Finta | F |
| Comandos rápidos | 1 a 4 |
| Pausa | Esc |

**Assistências (como o assistido e o manual do FIFA)**

Cada jogador escolhe o próprio nível, e no lobby aparece o nível de cada um.

| Assistência | Assistido | Semi | Manual |
| --- | --- | --- | --- |
| Tempo da batida | Janela larga | Janela média | Janela curta, mais força no acerto perfeito |
| Mira do arremesso e do chute | Puxa para a garrafa | Puxa pouco | Livre, linha de trajetória curta |
| Pegar a bola | Automática ao passar por cima | □ com raio maior | □ com raio normal |
| Troca de jogador | Totalmente automática | Automática com L1 | Só manual |
| Cruzada | Automática ao se encontrarem | Anel largo | Anel curto |

No ranqueado, quem joga no manual ganha um pequeno bônus de XP, nunca de pontos de partida.

Tudo remapeável no menu de controles.

**Um jogador, dois personagens (estilo FIFA)**

- No modo sofá e contra a CPU, cada pessoa pode controlar a dupla inteira; o outro personagem é da IA e segue comportamento padrão (fica na base, posiciona-se para receber passe).
- A troca de personagem é automática para o mais perto da bola, com troca manual no L1 (LB) ou pelo analógico direito, como no FIFA.
- No ataque, um único comando de correr leva os dois juntos; a cruzada é um aperto só.

**Vibração e comunicação**

- Vibração forte na batida perfeita, curta na cruzada e longa quando a sua casinha cai.
- Roda de comandos de voz no direcional, com sotaque de rua: "Corre!", "Fica!", "Mais uma!", "Joga pra mim!", "Licença, bets!". Chat de voz opcional online.

## Dinâmica do lance: campo, física e gravidade

Depois da batida, o bets vira um jogo de campo como o futebol: a bola está viva, a defesa corre até ela, domina, passa ou chuta na garrafa, e o ataque corre para cruzar e protege as casinhas. Esta seção define tudo isso com números iniciais, que serão ajustados nos testes. Os botões aparecem no padrão PlayStation, com o equivalente Xbox entre parênteses.

**O campo**

| Elemento | Medida no jogo | Por quê |
| --- | --- | --- |
| Distância entre as casinhas | 20 m (16 a 22 m conforme a arena) | Igual à rua; dá tempo de 1 a 3 cruzadas por batida boa |
| Largura da rua | 10 m de asfalto + 2 m de calçada de cada lado | Bola rebatida de lado bate no meio-fio, nos portões e nos carros |
| Área atrás de cada casinha | 15 m até o limite do campo (muro, carro, portão) | É para onde vai a batida forte; quanto mais longe, mais tempo para o ataque |
| Círculo da base | 1,2 m de diâmetro (o real tem 60 cm; ampliado para ficar legível na tela) | Taco dentro = seguro; fora = pode ser queimado |
| Faixa do meio | 3 m de largura, marcada com giz e um chinelo | Só ali a cruzada vale |

**Limites do campo e enquadramento**

A rua não é infinita: o campo jogável é um retângulo fechado de cerca de **50 m por 14 m** (20 m entre as casinhas + 15 m atrás de cada uma, por 10 m de asfalto + calçadas). Assim a câmera de transmissão enquadra o jogo inteiro, mesmo com os quatro correndo para lados diferentes.

- **Limites naturais, nunca paredes invisíveis:** rua sem saída com muro no fundo, carro estacionado atravessado, portão de garagem, caçamba de entulho, cerca da praça. O jogador entende o limite só de olhar.
- **A bola rebate no limite** e volta para o campo, com o quique da tabela de superfícies.
- **Bola por cima do muro** é "bola no mato": volta ao fielder mais perto em 2 s e o ataque ganha 1 ponto bônus.
- **Jogadores não saem do retângulo:** encostam no limite e deslizam ao longo dele.
- **Câmera:** o retângulo inteiro cabe na tela na vista aberta; quando a bola e os jogadores estão todos de um lado, a câmera aproxima sozinha e volta a abrir quando o jogo se espalha.
- **Cada arena tem o mesmo tamanho jogável**; o que muda é o cenário em volta, o chão e os obstáculos, para o jogo continuar justo no ranqueado.

**Gravidade e a bola**

- **Gravidade:** 8,5 m/s², um pouco abaixo da real (9,8). A bola fica um tiquinho mais no ar, o suficiente para a defesa reagir e a cavadinha ficar bonita, sem parecer lua.
- **Bola:** bolinha de tênis (cerca de 58 mm, 57 g), com resistência do ar: batida forte perde velocidade no fim, cavadinha cai mais em pé.
- **Quique por chão** (quanto da velocidade sobra a cada quique):

| Superfície | Quique | Como a bola se comporta |
| --- | --- | --- |
| Asfalto | 70% | Quica alto e rola longe; arena mais rápida |
| Cimento liso (quadra) | 75% | Quique perfeito e previsível |
| Pedra portuguesa (calçadão) | 60% | Desvia um pouco para os lados a cada quique |
| Terra batida | 45% | Quique baixo e meio torto, levanta poeira |
| Grama | 30% | Morre rápido; ótima para quem defende |
| Parede, portão, carro | 55% | Rebate; dá para usar a parede de propósito |

- **Rolar:** depois de quicar, a bola rola e perde velocidade pelo atrito do chão; no asfalto ela vai longe, na grama para em poucos metros.
- **Efeito:** o efeito curva a bola até cerca de 1,5 m em 20 m e muda a direção no quique, o famoso quique torto.
- **Ladeira:** a arena "Ladeira" é inclinada; a bola rola sozinha rua abaixo e quem defende o lado de baixo sofre. A gravidade passa a ser parte da estratégia.
- **Velocidades de referência:** arremesso de 55 a 90 km/h; batida perfeita sai a 80 a 110 km/h; batida de toque de 30 a 45 km/h; cavadinha sai com ângulo de 35° a 50°.

**Os jogadores em movimento**

| Situação | Velocidade máxima | Observação |
| --- | --- | --- |
| Andando | 2 m/s | Analógico leve |
| Correndo | 5 m/s | Analógico todo |
| Sprint (R2 / RT) | 7 m/s | Gasta estamina: cerca de 6 s de sprint seguido; recarrega em 4 s |
| Rebatedor com o taco | 6,5 m/s no sprint | O taco pesa um pouco |
| Fielder com a bola na mão | 4 m/s, sem sprint | Não dá para levar a bola correndo até a casinha: força o passe e o chute de longe, como no futebol |

Aceleração e freada têm peso: mudar de direção bruscamente custa meio passo, o que deixa a bola rebatida no contrapé difícil de alcançar.

**O lance passo a passo**

1. **Arremesso.** O arremessador mira a garrafa do outro lado com o analógico direito e arremessa com R2 (RT) forte, R1 (RB) quicado ou L2 (LT) pingo. O rebatedor espera dentro do círculo com o taco no chão (automático enquanto estiver parado no círculo).
2. **Batida.** O rebatedor rebate com ○ (B) forte, X (A) toque ou △ (Y) cavadinha. A bola fica viva: começa a parte "futebol".
3. **Bola viva: a defesa vai na bola.** O jogador de defesa mais perto vira o ativo automaticamente (troca com L1 / LB). Ele corre com o analógico e R2, guiado pelo círculo no chão que mostra onde a bola vai cair ou parar.
4. **Dominar a bola.** Perto da bola (até 1 m), □ (X) pega. No chão: dominada em 0,3 s. No ar: aparece um anel encolhendo; apertar □ quando ele fecha pega antes de quicar e o ataque perde o taco. Errar o tempo faz a bola espirrar da mão.
5. **Com a bola na mão, três escolhas:**
   - **Chutar na garrafa:** ○ (B), segurando para a força e mirando com o analógico, igual ao chute a gol. Uma linha pontilhada mostra a trajetória; de longe a mira treme mais. A garrafa da casinha com o taco fora do círculo fica vermelha.
   - **Passar para o colega:** X (A), passe rasteiro na direção do analógico. O colega perto da outra casinha recebe e chuta de pertinho.
   - **Lançamento:** △ (Y), passe por cima, quando tem rebatedor no caminho.
6. **Enquanto isso, o ataque corre e cruza.** Os dois rebatedores correm um na direção do outro (R2 manda a dupla; no 2x2 cada um corre o seu). Para valer a cruzada, os dois precisam estar dentro da faixa do meio e a no máximo 1,5 m um do outro. Nesse momento aparece um anel; apertar X (A) quando fica verde bate os tacos ("tec!"). No 2x2, os dois apertam numa janela de 0,3 s; se só um apertar, é cruzada torta e não vale. No 1x1, um aperto só controla os dois.
7. **Validar o ponto.** O ponto aparece amarelo (+1 pendente) e só fica verde quando cada um encosta o taco no círculo da base oposta, como na rua. Se a casinha for derrubada antes, o ponto pendente é perdido.
8. **Mais uma ou fica?** Na base, parar dentro do círculo encosta o taco e deixa seguro; R2 (RT) sai para outra cruzada. É aqui que mora a decisão: olhar onde está a bola e se o fielder já dominou.
9. **O rebatedor defende a casinha.** Quando a defesa chuta na garrafa, o dono daquela casinha pode correr na direção da bola e tentar rebatê-la com ○ (B) se ela passar a até 1,5 m do taco. Rebateu: casinha salva e a bola fica viva de novo (novas cruzadas valem). Errou estando fora do círculo: risco de queimada. Dentro do círculo parado com o taco no chão ele já está protegido, e rebater é bônus.
10. **Fim do lance (bola morta).** Acontece quando: a casinha cai com o taco fora (queimada, troca de papel); a bola é pega no ar (troca de papel); a terceira bola vai para trás (troca de papel); ou a defesa devolve a bola ao arremessador dentro do círculo dele com os dois rebatedores seguros nas bases. Aí recomeça o arremesso.

**O guardião: quem fica atrás da casinha**

O parceiro do arremessador fica atrás da casinha do rebatedor, como um goleiro. É a posição mais tensa da defesa: ele precisa ficar esperto para não deixar a bola passar e para queimar quem der bobeira.

- **Posição:** começa 3 a 5 m atrás do círculo; pode se mover para os lados e para frente com o analógico, andando ou correndo; segurando L2 (LT) entra em postura de goleiro, andando de lado sempre de frente para a bola.
- **Segurar a bola que passa:** se o rebatedor erra, deixa passar ou a bola vai para trás, o guardião pega com □ (X). Com □ + direção ele mergulha, como goleiro, para alcançar bola longe; mergulho errado deixa a bola escapar para o fundo e dá tempo de cruzada para o ataque.
- **Queimar na bobeira:** com a bola na mão a 2 ou 3 m da casinha, um ○ (B) rápido derruba a garrafa. Se o rebatedor tiver saído do círculo antes da hora, está queimado. Por isso o rebatedor só deve sair da base quando a bola estiver longe.
- **Virar arremessador:** quando o lance termina com a bola na mão do guardião, ele arremessa dali para a outra casinha, como na rua: a defesa alterna os lados e os dois defensores arremessam e guardam.
- **IA:** no 1x1 e no 2x1, quando você está controlando o arremessador, a IA cuida do guardião; trocar para ele com L1 (LB) é a jogada para tentar a queimada.

Isso cria um jogo de nervos dos dois lados: o rebatedor quer sair correndo cedo para cruzar mais, e o guardião fica de olho exatamente nesse momento.

**Quando a garrafa cai**

- A garrafa tem física própria: bola acima de 15 km/h derruba; abaixo disso ela balança e não cai (animação de "quase!").
- Casinha derrubada com o taco do dono dentro do círculo não vale: a defesa só perde a jogada e a bola volta.
- Toda queimada apertada (menos de 0,2 s entre a garrafa cair e o taco entrar no círculo) ganha replay automático em câmera lenta.

**Ambiente que interfere no jogo**

- **Carros estacionados, muros e portões:** a bola rebate neles; jogadores contornam.
- **Bueiro:** bola que cai é "bola no mato": volta ao fielder em 2 s e o ataque ganha 1 ponto bônus.
- **Meio-fio:** degrau de 15 cm que desvia o quique.
- **Eventos raros (desligáveis no ranqueado):** carro passando pausa o lance ("Olha o carro!"); cachorro corre atrás da bola por alguns segundos; vizinha devolve a bola pela janela.
- **Noite no poste:** fora da luz a bola fica difícil de ver; o círculo no chão de onde ela cai continua aparecendo.
- **Vento (só em arenas abertas, como a praia):** desvia levemente a cavadinha; uma bandeirinha mostra a direção.

**Clima**

O clima é escolhido no lobby (ou sorteado) e muda a física, não só o visual.

| Clima | O que muda no jogo |
| --- | --- |
| Sol de fim de tarde | Padrão: física da tabela, visibilidade total |
| Nublado | Igual ao padrão, luz mais fria; só visual |
| Garoa | Quique 10% menor; a bola escorrega um pouco mais ao dominar (janela do □ um pouco menor) |
| Chuva | Quique 25% menor e a bola desliza no chão molhado; poças freiam a bola de vez; personagens escorregam ao frear em sprint; giz do círculo fica borrado, mas o contorno do jogo continua visível |
| Noite no poste | Só a área iluminada é clara; o marcador de onde a bola cai continua aparecendo |

- No ranqueado o clima é sempre o padrão, ou sorteado igual para os dois lados, para não virar desculpa.
- Na chuva, as crianças do cenário correm para debaixo das marquises e alguém sempre grita "só mais uma!".
- Personagens ficam com a roupa molhada e respingos nos tacos; skins de taco e bola ganham variação molhada automática.

**Exemplo de um lance (segundos)**

| Tempo | Defesa | Ataque |
| --- | --- | --- |
| 0,0 | Arremessador solta a bola | Rebatedor espera no círculo |
| 0,9 | — | Batida forte para o fundo |
| 1,2 | Fielder sai em sprint | Os dois saem correndo (R2) |
| 2,8 | — | Cruzada no meio (X): +1 pendente |
| 4,3 | Fielder alcança a bola rolando e domina (□) | Param nas bases: ponto verde. R2: mais uma |
| 4,9 | Passe (X) para o arremessador perto da casinha B | Correndo de volta |
| 5,6 | Arremessador chuta na garrafa (○) | Rebatedor ainda fora do círculo tenta rebater (○) e erra |
| 5,9 | Garrafa cai: queimada | Ponto da segunda cruzada perdido; troca de papel |

Os números desta seção são o ponto de partida do protótipo. Cada um vira uma variável ajustável no editor, para calibrar jogando.

## IA e dificuldade

A IA é metade do jogo: ela joga no 1x1, completa o 2x1, substitui quem cai e é a adversária da temporada offline e do treino.

| Nível | Como joga |
| --- | --- |
| Moleque da rua | Erra o tempo com frequência, sai cedo da base, chuta longe da garrafa |
| Da turma | Erra às vezes; corre atrás da bola sem pensar muito |
| Do bairro | Escolhe o tipo de batida pela posição do fielder; passa a bola quando está longe |
| Lenda da Vila | Lê o jogo: guarda a casinha certa, finta, cruza só quando é seguro |

- **Erra como gente:** a IA nunca é perfeita; errar o tempo por poucos milissegundos, hesitar e escolher mal de vez em quando faz parecer humano.
- **Personalidade por posição:** guardião paciente ou afoito, rebatedor medroso ou arriscado. Cada bot tem um perfil.
- **IA parceira:** quando ela é sua dupla, obedece aos comandos rápidos ("Corre!", "Fica!", "Joga pra mim!").
- **Dificuldade adaptativa só fora do ranqueado:** na temporada offline e no treino, a IA ajusta o nível se você ganha ou perde demais.

## Multiplayer: amigos, times e partidas

O jogo é social por natureza: a unidade básica é a dupla, e o jeito mais rápido de jogar é chamar um amigo.

**Lobby aberto: todo mundo entra e joga**

O lobby funciona como os amistosos online do FIFA com amigos: não existe "modo 1x1" ou "modo 2x2" para escolher antes. Cada pessoa entra no grupo, escolhe um lado (time laranja ou azul) e a partida se monta com quem estiver ali.

| Formação | Quem controla o quê |
| --- | --- |
| 1x1 | Cada pessoa controla a dupla inteira, trocando de personagem com LB / L1 |
| 2x1 | O lado com dois tem um personagem por pessoa; quem está sozinho controla a dupla inteira, com a IA ajudando no personagem parado |
| 2x2 | Cada pessoa controla um personagem |
| 1x0 ou 2x0 | Contra a CPU, enquanto espera mais gente entrar |

- **Entrar e sair no meio:** quem chega no meio da partida entra assumindo o personagem da IA do lado que escolheu; quem sai devolve o personagem para a IA, sem acabar o jogo.
- **Troca de lado entre partidas:** na tela de fim todos podem mudar de lado ou embaralhar os times antes da revanche.
- **Equilíbrio no 2x1:** quem joga sozinho controla a dupla com troca automática de personagem e a IA do outro personagem fica na base; no ranqueado o 2x1 não conta pontos, só nos amistosos.
- **Grupo contra grupo ou individual:** o matchmaking aceita qualquer combinação. Um grupo de 2 pode enfrentar outro grupo de 2 ou dois jogadores sozinhos que o jogo juntou; um jogador sozinho pode pedir partida 1x1 ou entrar numa dupla com um desconhecido.
- **Grupo grande (até 8):** com mais de 4 amigos no grupo, o jogo faz rodízio estilo "quem perde sai", como na rua; quem está esperando assiste com a câmera de transmissão e conversa com todos.

**Conversa por voz de time**

- **Canal do time:** durante a partida, cada lado tem seu canal de voz próprio, para combinar "corre", "fica" e "joga pra mim" sem o adversário ouvir.
- **Canal do grupo:** no lobby, na tela de fim e entre as partidas, todos falam juntos, como na resenha da rua.
- **Apertar para falar ou voz aberta**, a escolha de cada um; quem não quer falar usa os comandos rápidos no direcional.
- **Silenciar e denunciar** qualquer jogador com dois cliques; no matchmaking com desconhecidos, a voz começa desligada e cada um ativa se quiser.
- **Integração com Discord e com o chat de grupo do console**, para quem já está numa chamada com a turma.

**Modos de jogo**

| Modo | Como funciona |
| --- | --- |
| Rápida | Entra sozinho; o matchmaking acha um parceiro e uma dupla adversária |
| Dupla | Entra com um amigo; o matchmaking acha só a dupla adversária |
| Sala privada | Cria uma sala com código de 6 letras; 4 amigos, regras customizadas (meta, valor da cruzada, regra de vila) |
| Ranqueada | Modo Temporada: divisões solo, por dupla e por grupo, detalhado abaixo |
| Treino | Área de treino de ataque e defesa, com desafios e medalhas, detalhada abaixo |
| Rua do bairro (futuro) | Torneio entre "turmas" (clubes de até 20 jogadores), estilo copa de rua |
| Sofá 1x1 | Dois jogadores no mesmo PC ou console, cada um controlando uma dupla inteira e trocando de personagem |
| Sofá 2x2 | Quatro controles na mesma tela, cada um com seu personagem: a turma da rua na sala de casa |

Todos os modos funcionam **local (no sofá), online ou misturado**: dois amigos no mesmo sofá podem entrar juntos numa partida online contra outra dupla.

**Modo Temporada (estilo FIFA)**

O jogo não acaba: a cada temporada você, sua dupla ou seu grupo sobe ou desce de divisão.

| Divisão | Nome |
| --- | --- |
| 10 a 8 | Calçada |
| 7 a 5 | Rua |
| 4 e 3 | Bairro |
| 2 | Cidade |
| 1 | Lenda da Vila |

- **Como funciona:** cada temporada tem 10 partidas. Vitória vale 3 pontos e vitória sem levar ponto ("lavada") vale 4. Atingiu a meta da divisão, sobe; ficou abaixo do mínimo, cai; no meio, mantém. Ganhar a Divisão 1 dá o título da temporada.
- **Três escadas separadas:**
  - **Solo:** o seu ranking, jogando 1x1 ou caindo numa dupla com desconhecido.
  - **Dupla fixa:** a dupla tem divisão própria, nome e escudo.
  - **Grupo / turma:** até 8 amigos; as partidas de qualquer combinação do grupo somam para o ranking da turma, que enfrenta outras turmas.
- **Partidas de colocação:** as 5 primeiras definem a divisão inicial.
- **Temporada global:** além das 10 partidas, existe um ranking mensal com placar dos melhores do Brasil e por estado. No fim do mês, recompensas pela melhor divisão alcançada: taco e bola exclusivos da temporada, giz de cor especial, emblema no perfil.
- **Matchmaking por divisão:** você enfrenta gente do seu nível; grupos enfrentam grupos parecidos.
- **Abandono:** sair no meio conta derrota para quem saiu, não para o parceiro.
- **Temporada offline:** a mesma estrutura contra a CPU, com dificuldade subindo por divisão, para jogar sem internet.
- **Liga do sofá:** no multiplayer local, o jogo guarda o histórico entre os amigos da casa (quem ganhou mais, maior sequência, mais queimadas) e mostra a tabela no fim de cada noite.

**Área de treino**

Um campinho próprio para treinar ataque e defesa, sozinho, com amigo no sofá ou online.

- **Treino livre:** escolhe o papel, a arena e o clima; bola infinita com lançadora automática ou com a IA. A tela mostra números ao vivo: velocidade da bola, força, e por quantos milissegundos o tempo da batida, da cruzada ou da pegada ficou adiantado ou atrasado.
- **Desafios de ataque:**
  - Rebater cada tipo de arremesso (reto, quicado, pingo, com efeito).
  - Mandar a bola em alvos no chão (pneus) longe do fielder.
  - Corrida e cruzada contra o relógio.
  - Decidir "mais uma ou fica" com a defesa da IA indo na bola.
- **Desafios de defesa:**
  - Derrubar a garrafa de distâncias cada vez maiores.
  - Pegar bolas no ar e mergulhar como guardião.
  - Passe para o colega em movimento e chute de pertinho.
  - Queimar um rebatedor da IA que sai cedo da base.
- **Medalhas e ranking:** cada desafio dá bronze, prata ou ouro, com placar entre amigos. Desafios diários dão XP e figurinhas.
- **Treino em dupla:** no sofá ou online, um arremessa e o outro rebate, ou os dois treinam a cruzada juntos.
- **Rachão:** partida livre sem placar e sem ranking, para aquecer ou ensinar um amigo.

**Convidar amigos**

- Link de convite compartilhável no WhatsApp, Instagram e Discord: abre o jogo direto na sala (deep link); quem não tem o jogo cai na loja e entra na sala depois de instalar.
- Lista de amigos com status (online, em partida) e convite com um toque.
- Login com Steam, PlayStation Network, Xbox ou Discord; a lista de amigos da plataforma entra automaticamente.
- Após a partida, botão "Revanche" mantém os 4 na mesma sala.

**Times e turmas**

- **Dupla fixa:** dois amigos formam uma dupla com nome, escudo (desenhado com giz no editor) e histórico próprio.
- **Turma:** grupo de até 20 jogadores, como a turma da rua, com chat, ranking interno e desafios entre turmas.
- **Taco e casinha personalizados** aparecem para todo mundo na partida, reforçando a identidade da dupla.

**Regras de rede pensadas para o jogo**

- Partida de 4 jogadores em servidor autoritativo: o servidor decide física da bola, queimada e cruzada, evitando trapaça.
- Arremesso e batida usam previsão local para parecer instantâneos mesmo com 100 a 150 ms de latência; o servidor confirma.
- Se alguém cair, um bot assume até ele voltar (60 s); sem retorno, a partida continua com bot e não conta derrota para o parceiro.
- Servidores em São Paulo primeiro, para latência baixa no Brasil.

**Segurança e classificação indicativa**

Crianças vão jogar, então isso é obrigação desde o início.

- **Classificação indicativa livre** (ClassInd no Brasil, IARC e ESRB/PEGI fora), sem violência e sem qualquer mecânica de aposta ou caixa de sorte paga.
- **Voz e chat de texto desligados por padrão para contas de menores**; liberados só com controle dos pais da plataforma (Steam, PlayStation, Xbox).
- **Com desconhecidos, voz começa desligada para todos**; cada um ativa se quiser.
- **Filtro de palavrões e nomes ofensivos**, silenciar e denunciar com dois cliques, e punição progressiva (aviso, suspensão de voz, suspensão de conta).
- **Antichatice de jogo:** quem abandona partidas ou queima o parceiro de propósito com frequência cai numa fila separada.
- **Privacidade:** nenhum dado de localização precisa; o ranking por estado usa só o estado escolhido no perfil.

## Telas e fluxo de usabilidade

Do ícone até a primeira batida em menos de 60 segundos: o tutorial é a primeira partida, contra bots.

1. **Abertura:** rua de fim de tarde, crianças desenhando o círculo de giz; aperte qualquer botão para entrar.
2. **Primeira partida guiada:** o jogador faz uma rodada em cada papel (rebater, arremessar, buscar a bola) com dicas na tela, contra bots, até 5 pontos.
3. **Tela inicial (a rua):** botão grande JOGAR, atalhos para Dupla, Sala privada e Ranqueada, lista de amigos online à direita, turma e loja embaixo.
4. **Lobby:** os 4 jogadores aparecem na calçada com seus tacos; o dono da sala ajusta regras; botão PRONTO.
5. **Sorteio:** minigame de molhado ou seco decide quem começa com o taco.
6. **Partida:** HUD mínimo — placar no topo, indicador de bolas para trás, círculo da base verde ou vermelho, roda de comandos no canto.
7. **Fim:** replay do melhor lance (a cruzada decisiva ou a queimada), XP, moedas e botões Revanche, Trocar dupla e Voltar à rua.

**Princípios de usabilidade**

- Controle primeiro: menus e partida inteira navegáveis só com o joystick; teclado e mouse remapeáveis.
- Botões contextuais: só aparece na tela o que a pessoa pode fazer naquele momento.
- Feedback em tudo: vibração na batida, som de madeira na cruzada, som de garrafa PET caindo na queimada.
- Acessibilidade: modo de timing assistido, cores com padrão além da cor (verde/vermelho também têm ícone), legendas nos comandos de voz.

## Arte, ambientação e som

Visual 3D estilizado e colorido (low poly com textura pintada à mão), leve para rodar bem no Steam Deck e em PC de entrada.

- **Campos (arenas):** rua de asfalto com bueiro e carro estacionado, rua de terra batida, pracinha com banco, beco entre prédios, quintal de vó, quadra da escola. Cada uma muda o quique da bola.
- **Detalhes da infância:** círculo de giz que borra quando pisam, garrafa PET de refrigerante como casinha, chinelo marcando o meio, cachorro que atravessa o campo, vizinha gritando da janela, carro passando que pausa o jogo ("Olha o carro!").
- **Personagens:** crianças e adultos customizáveis, com boné, chinelo, camisa de time genérica, joelho ralado.
- **Itens cosméticos:** tacos (cabo de vassoura, tábua de caixote, taco profissional de pinus, taco com fita isolante), casinhas (PET, lata, três gravetos, tijolo), bolas (tênis, borracha, de meia).
- **Som:** "tec" da madeira na cruzada, garrafa PET caindo, bola quicando no asfalto, crianças gritando "CORRE!" e "QUEIMOU!", trilha de fim de tarde com violão e percussão.
- **Horário:** sol de fim de tarde no padrão; partidas noturnas sob poste de luz como variante.

**Narração de rua**

No lugar do narrador de TV do FIFA, quem narra é um moleque sentado no muro, com bordões de rua: "Ih, queimou!", "Vai que dá, vai que dá!", "Pegou no ar, acabou pra vocês!", "Olha o carro!". Ele reage ao placar, provoca quem perde de lavada e lembra lances da mesma partida. Pode ser desligado ou trocado por outro narrador (a vó na janela, o tiozão do bar) como item da loja.

**Concept arts iniciais**

Foram geradas no Higgsfield as primeiras artes de referência: rua ao entardecer, personagens e o mascote, arremesso por cima do ombro, cruzada em câmera lenta, queimada, protótipo cinza, lobby na calçada e duas cenas na câmera de transmissão. Elas estão na galeria do Higgsfield e servem só como direção visual.

Monetização sem pay-to-win: só cosméticos e passe de temporada; nenhum taco dá vantagem.

## Marca e identidade visual

A marca vende nostalgia de rua com energia de esporte: parece pichado com giz no asfalto, mas tem a força de um logo de campeonato.

**Missão**

Levar a rua para dentro de casa: transformar o bets, o jogo que juntava a turma no asfalto, num jogo competitivo e divertido que reúne amigos e gerações no sofá e online.

**Visão**

Ser o jogo de rua brasileiro que o mundo reconhece: assim como o futebol de rua virou cultura, fazer o bets voltar para as ruas, para as escolas e para as telas, com uma comunidade que disputa temporada após temporada.

**Valores**

| Valor | Na prática |
| --- | --- |
| A rua é de todos | Fácil de aprender, jogável com controle ou teclado, livre para todas as idades; ninguém fica de fora da turma |
| Ganha quem joga melhor | Nada de pagar para ganhar ou sorte decidindo; a derrota tem que ser justa para dar vontade de revanche |
| Respeito à origem | Regras, gírias e cenários fiéis ao bets de verdade, com as variações de cada bairro |
| Resenha faz parte | Provocar, comemorar e rir junto; competir sem tóxico |
| Do sofá para a rua | O jogo incentiva sair e jogar de verdade, com taco, bola e garrafa |

**Frase-assinatura (slogan)**

- Principal: **"A rua voltou."**
- Para a geração nova: **"Seu pai jogava isso na rua. Agora é sua vez."**
- Para a competição: **"Licença, bets. Agora é sério."**

**Personalidade da marca**

O moleque gente boa da rua: zoeiro, competitivo, nostálgico sem ser velho, brasileiro sem estereótipo. Fala como se fala na calçada, nunca como propaganda de aposta.

**Nome**

| Opção | Pró | Contra |
| --- | --- | --- |
| BETS! | Curto, forte, é o grito do jogo | Confunde com casa de aposta ("bets") em busca e anúncio |
| TACO! | Nome mais nacional e fácil de entender | Confunde com a comida mexicana fora do Brasil |
| BETE DE RUA | Diferencia de aposta e traz a rua no nome | Mais longo |
| CASINHA | Afetivo, único | Pouco claro para quem não jogou |
| BETS DE RUA | Diz na hora o que é e onde se joga; mesma família de "futebol de rua"; em português, "de rua" afasta a ideia de aposta | Fora do Brasil precisa de um nome próprio |
| STREET BETS | Soa internacional | Em inglês, "bet" é aposta: vira literalmente "apostas de rua" e lembra o fórum de ações WallStreetBets. Arriscado em loja, busca e anúncios lá fora |

**Nome escolhido para testar: BETS DE RUA.** Resume a proposta (o jogo da rua, como o futebol de rua) e o "de rua" deixa claro que não é casa de aposta. No logo, "BETS" grande em giz e "DE RUA" menor embaixo. Para fora do Brasil, usar um nome em inglês que não contenha "bet" sozinho, como **Street Bat** ou **Street Cricket**, com o subtítulo "the Brazilian street game". Antes de fechar: registro no INPI, domínio, @ nas redes e nome livre na Steam e nas lojas de console.

**Logo (3 conceitos para esboçar)**

1. **Giz no asfalto:** "BETS!" escrito à mão com traço de giz, o ponto de exclamação é um taco em pé com a bolinha embaixo.
2. **Tacos cruzados:** dois tacos formando um X atrás do nome, como brasão de time de várzea; o X vira o ícone sozinho.
3. **A casinha:** garrafa PET estilizada caindo, com linhas de movimento; o ícone do app é só a garrafa tombando dentro do círculo de giz.

O ícone curto em qualquer conceito: círculo de giz + tacos cruzados, legível em 32 px (ícone da Steam e favicon).

**Logo escolhido: Giz no asfalto.** "BETS" grande em letra de giz, "DE RUA" menor embaixo, um taco em pé com a bolinha amarela como acento e um traço laranja sublinhando. Versões do kit: principal em fundo asfalto, invertida para fundo claro, ícone só com o taco e a bola dentro do círculo de giz, e aplicações (camiseta, tela de título, perfil nas redes). Os arquivos estão na galeria do Higgsfield; o logo final deve passar por retrabalho humano em vetor antes do registro, para garantir proteção da marca.

**Paleta**

| Cor | Hex | Uso |
| --- | --- | --- |
| Asfalto | #2B2D31 | Fundo principal, telas e logo invertido |
| Giz | #F4F1E8 | Texto, traços do logo, círculo da base |
| Laranja fim de tarde | #FF7A1A | Cor de destaque, botão JOGAR, ação |
| Amarelo bolinha | #E8E337 | Bola, alertas, pontos |
| Verde PET | #2FBF71 | Base segura, sucesso, cruzada |
| Vermelho queimada | #E5484D | Casinha exposta, queimada, perigo |
| Azul céu | #5AA9E6 | Time azul, céu, telas de lobby |

**Tipografia**

- Títulos e logo: fonte manuscrita grossa com textura de giz (desenhada sob medida ou base em fontes livres tipo "Permanent Marker").
- Interface e placar: sans condensada e forte, estilo placar esportivo (ex.: Barlow Condensed ou Oswald, ambas livres).

**Mascote**

"Tampinha": um moleque de boné virado, joelho ralado e chinelo, segurando um taco remendado com fita isolante. Aparece no tutorial, nas telas de carregamento e nas redes sociais.

**Tom de voz**

Gíria de rua, bem-humorado, nunca forçado: "Licença, bets!", "Queimou!", "Olha o carro!", "Última e eu vou embora". Nada de linguagem de aposta ("aposte", "ganhe dinheiro") em nenhum material.

## Ambientes, skins e loja

Cada ambiente tem um jeito próprio de quicar a bola e um detalhe de infância; as skins são só visuais e contam história, nunca dão vantagem.

**Ambientes (arenas)**

| Ambiente | Chão e quique | Detalhe marcante | Liberação |
| --- | --- | --- | --- |
| Rua de asfalto | Quique alto e rápido | Carro estacionado, bueiro, vizinha na janela | Inicial |
| Rua de terra | Quique baixo e irregular | Poeira na corrida, poste de madeira | Inicial |
| Pracinha | Grama: bola morre rápido | Banco, pipoqueiro, pombos | Nível 5 |
| Quadra da escola | Cimento liso, quique perfeito | Tabela de basquete, sinal do recreio | Nível 10 |
| Beco entre prédios | Campo estreito, bola bate na parede | Varal de roupa, gato no muro | Nível 15 |
| Quintal da vó | Piso de caco de cerâmica | Galinha atravessando, vó chamando para o lanche | Nível 20 |
| Beira-mar | Calçadão de pedra portuguesa | Coqueiro, carrinho de mate e biscoito | Temporada de verão |
| Rua de festa junina | Terra com bandeirinhas | Fogueira, quadrilha ao fundo | Evento de junho |
| Noite no poste | Qualquer chão, luz de poste | Mariposas, só a área iluminada é clara | Variante noturna |

**Skins de taco**

| Raridade | Exemplos |
| --- | --- |
| Comum | Cabo de vassoura, tábua de caixote, taco de pinus cru |
| Raro | Taco com fita isolante colorida, taco pintado com o escudo da dupla, taco de remo de canoa |
| Épico | Taco com chama pintada, taco de madeira de lei envernizado, taco com adesivos de figurinha |
| Lendário | "Taco do Vovô" (gasto e com iniciais entalhadas), taco de neon para a arena noturna |

**Skins de bola**

- Bolinha de tênis gasta, bola de borracha colorida, bola de meia, bola de jornal com fita, bola de frescobol, bolinha de pingue-pongue gigante (evento).
- Rastro opcional na bola: poeira de giz, faíscas, confete de festa junina.

**Skins de casinha**

- Garrafa PET (várias cores de rótulo inventado), lata, três gravetos, tijolo em pé, chinelo dobrado, cone de obra.
- Cada uma tem animação e som próprios ao cair.

**Personagens e extras**

- Visual: bonés, camisas de time de várzea inventado, chinelo, tênis de mola, joelheira de skate, curativo no joelho.
- Comemorações: dancinha, "aviaozinho", bater os tacos no chão, apontar para a janela da vizinha.
- Giz personalizado: cor e desenho do círculo da base e do escudo da dupla.

**Loja e progressão**

- Moeda ganha jogando ("figurinhas") e moeda paga para cosméticos.
- Passe de temporada com trilha grátis e trilha paga, tema por temporada (verão na praia, festa junina, volta às aulas).
- Itens de dupla: quando os dois têm o mesmo kit, ganham um efeito visual extra na cruzada.

## Progressão, estatísticas e recompensas

O que faz a pessoa voltar amanhã: subir de nível, ver os próprios números melhorando e destravar coisas para mostrar aos amigos.

**Progressão**

- **Nível do jogador:** XP por partida, por desafio de treino e por missão; cada nível dá figurinhas (moeda do jogo) e, a cada 5 níveis, um item cosmético.
- **Missões diárias e semanais:** "faça 3 cruzadas numa batida", "queime 2 rebatedores como guardião", "vença uma partida na chuva".
- **Conquistas:** marcos permanentes, como 100 queimadas, primeira lavada, vencer na Ladeira, chegar à Lenda da Vila.
- **Álbum de figurinhas:** cada taco, bola, casinha e arena é uma figurinha no álbum; completar páginas dá recompensas, puxando a nostalgia do álbum de Copa.
- **Passe de temporada:** trilha gratuita e trilha paga, só com cosméticos.

**Estatísticas pós-jogo**

| Ataque | Defesa | Geral |
| --- | --- | --- |
| Cruzadas e pontos validados | Queimadas | Tempo de bola viva |
| Batidas perfeitas e por tipo | Pegadas no ar | Distância percorrida |
| Bolas para trás | Precisão do chute na garrafa | Jogador destaque ("o craque da rua") |
| Cruzadas perdidas (queimado com ponto pendente) | Passes certos | Maior sequência de pontos |

- **Perfil com histórico:** médias por temporada, mapa de calor de onde suas batidas caem e evolução do tempo médio de batida.
- **Comparação com amigos** e com a média da sua divisão.
- Os mesmos eventos alimentam o balanceamento e o matchmaking (ver Desafios técnicos).

## Competição: "odeio, mas não largo"

O jogo só dá certo se o Modo Temporada virar disputa de verdade: a pessoa perde, fica brava, e aperta revanche na hora. É o efeito FIFA. A estressada faz parte, desde que a derrota pareça justa e a próxima partida esteja a um botão de distância.

**O que cria a vontade de voltar**

- **Partidas curtas:** 5 a 8 minutos. Perder dói pouco o suficiente para jogar outra.
- **Revanche em um botão:** os mesmos 4 de volta à rua em segundos, sem passar pelo menu.
- **Quase deu:** o jogo mostra o quanto faltou ("a garrafa balançou e não caiu", "faltou 1 ponto pra subir"). A sensação de quase é o que segura.
- **Jogo decisivo da temporada:** a última partida mostra "vale acesso" ou "vale rebaixamento" na tela, com trilha diferente. Pressão de final.
- **Fim de semana da Várzea:** torneio semanal de sexta a domingo, com número limitado de partidas e recompensa por vitórias, como o Champions do FIFA. Cria compromisso com o fim de semana.
- **Rivalidade entre amigos:** histórico confronto a confronto ("você ganhou 23, ele ganhou 31"); quem perde muito para alguém ganha o título de "freguês" no perfil até virar o jogo.
- **Provocação na medida:** comemorações e comandos rápidos depois de um ponto ("Vai, freguês!"), sem chat livre com desconhecidos.
- **Sequências:** vitórias seguidas mostram a sequência no placar; quebrar a sequência do rival vale bônus de XP.
- **Nêmesis:** o jogo marca quem mais te venceu na temporada e avisa quando ele está online.

**Derrota que parece justa (a raiva tem que ser consigo mesmo)**

- **Nada de sorte decidindo:** o quique tem variação pequena e previsível; o resultado vem de tempo, mira e decisão.
- **Replay automático nos lances polêmicos** (queimada apertada, pegada no ar no limite) mostra que foi justo.
- **Números depois da derrota:** "sua batida atrasou 40 ms em média", com atalho para o treino daquele fundamento. A derrota vira motivo para treinar, não para desinstalar.
- **Sem pagar para ganhar:** nenhum item melhora desempenho; quem ganha é quem joga melhor.

**Limites saudáveis**

- Sem caixas de sorte pagas e sem moeda paga para pular progressão.
- Abandonar no meio por raiva conta derrota e, se for frequente, coloca o jogador numa fila separada.
- Aviso opcional de pausa depois de muitas partidas seguidas.

**O que medir para saber se está funcionando**

| Métrica | Sinal de sucesso |
| --- | --- |
| Revanche após derrota | Mais da metade das derrotas seguidas de nova partida |
| Partidas por sessão | 4 ou mais |
| Retorno no dia seguinte | Acima de 40% |
| Partidas da temporada concluídas | Maioria dos jogadores termina as 10 |
| Abandono no meio | Abaixo de 5% |

As metas são estimativas iniciais para orientar o balanceamento, não números de mercado.

## Marketing, redes e propaganda

A estratégia é transformar a memória de rua das pessoas em conteúdo: o público já tem as histórias, o jogo só dá o palco.

**Instagram e TikTok**

- **Perfil:** @betsdorua ou @bets.game (checar disponibilidade). Bio: "O taco da rua agora no PC. Lista de desejos na Steam ↓".
- **Grade visual:** fundo asfalto, tipografia de giz, sempre com um elemento laranja; carrosséis com cara de quadro-negro de rua.
- **Séries fixas:**
  - "Como era na sua rua?" — seguidores mandam como chamavam o jogo e qual era a regra; o jogo ganha um mapa dos nomes pelo Brasil.
  - "Lance da semana" — clipe de 10 s da melhor cruzada ou queimada dos playtests.
  - "Bastidores do taco" — devlog curto mostrando câmera, física, skins sendo desenhadas.
  - "Regra da vila" — enquete de regra polêmica (bola pega no ar acaba o jogo?) que vira modo de jogo.
- **Formato:** Reels e TikTok primeiro (lances curtos, trends com "Licença, bets!"), carrossel para regras e skins, Stories para enquetes.
- **Frequência:** 3 Reels e 2 carrosséis por semana no pré-lançamento.

**Propaganda e campanhas**

- **Conceito de campanha:** "A rua voltou." Filme curto: adultos no escritório ouvem um "tec!" de taco e lembram da infância; corte para a turma jogando no sofá.
- **Trailer de anúncio:** 60 s, começa com giz desenhando o círculo no asfalto, garrafa PET, primeira batida, cruzada em câmera lenta, "Lista de desejos na Steam".
- **Influenciadores:** streamers brasileiros de jogos de futebol e de nostalgia; partidas 2x2 de streamer contra streamer no pré-lançamento.
- **Ação de rua:** kits físicos de bets (taco, bola e casinha com a marca) distribuídos em eventos e para influenciadores, com QR code para a Steam.
- **Eventos:** demo jogável na BGS (Brasil Game Show) e participação no Steam Next Fest.
- **Anúncios pagos:** Meta e TikTok segmentados por 25 a 45 anos e interesse em games e nostalgia; YouTube com o trailer.
- **Cuidado de marca:** evitar qualquer associação com sites de aposta; termos negativos nas campanhas pagas ("aposta", "cassino") para não atrair o público errado.

**Duas mensagens, dois públicos**

| Público | Mensagem | Exemplo de peça |
| --- | --- | --- |
| Quem jogou na rua | "A rua voltou." | Adulto ouve o "tec" de taco no escritório, lembra da infância e chama a turma antiga |
| Geração nova | "Seu pai jogava isso na rua. Agora é sua vez." | Pai e filho jogando juntos no sofá; o filho descobre o jogo e ganha do pai |

- **Pai e filho, tio e sobrinho:** o modo sofá é o ponto de encontro entre as gerações; vale campanha no Dia dos Pais e nas férias escolares.
- **Escolas e educação física:** o bets já aparece em aulas de educação física; kits físicos e o jogo podem andar juntos em ações com escolas.
- **Jogar de verdade também:** a marca incentiva o jogo físico na rua (kits com a marca, eventos de rua), como o futebol de rua convive com o FIFA.

**Teaser de apresentação: "A rua nova"**

Cerca de 90 segundos. Uma família se muda, dois irmãos (um menino e uma menina) descobrem o bets na rua nova e são puxados para a turma. A história conta o propósito do jogo: quem nunca viu o bets descobre na rua e vira parte dela. Meninas jogam de igual para igual em todas as cenas.

1. **A chegada.** Fim de tarde. Um carro com caixas de mudança no teto entra numa rua nova. Pai e mãe na frente; no banco de trás, os irmãos com a cara colada no vidro. O carro diminui: no meio da rua, crianças jogando. Um taco bate na bola, "tec!", dois tacos se cruzam no meio. Os irmãos se olham sem entender. "O que é isso?"
2. **Os dias passam.** Montagem curta: os dois voltando da escola de mochila. A rua sempre cheia: quatro jogando, outros sentados no meio-fio esperando a vez, gritando, comemorando. Uma menina da turma derruba a garrafa e todo mundo explode. Os irmãos olham de longe, curiosos, e seguem.
3. **Falta gente.** Um dia, a caminho do mercado, os irmãos passam a pé. Só tem dois jogando, com um taco largado no chão. Um garoto da turma grita: "Ei! Falta dois. Bora?" A irmã: "A gente não sabe jogar." O garoto: "Ninguém nasce sabendo."
4. **Aprendendo.** A turma ensina: o círculo de giz, a garrafa PET, "taco no chão!", "corre e cruza no meio!". O irmão fura a primeira batida, todo mundo ri. A irmã erra o arremesso. Tentam de novo.
5. **O primeiro ponto.** O irmão acerta uma batida limpa. A bola voa longe. A irmã, do outro lado: "Corre!" Os dois correm, se encontram no meio e batem os tacos: "tec!" Câmera lenta, giz levantando, a turma pulando no meio-fio.
6. **Viciados.** Montagem: os dois jogando até escurecer, debaixo do poste; a mãe chamando da janela, "Janta!"; os dois: "Só mais uma!". Corte para o sofá de casa, anos depois: os mesmos irmãos, adultos, de controle na mão, jogando o Bets de Rua.
7. **Fecho.** Tela preta, som de taco: "tec!". Logo BETS DE RUA em giz. Texto: "A rua voltou." Lista de desejos na Steam.

- **Versões:** 90 s para YouTube e Steam; 30 s com as cenas 1, 3, 5 e 7 para anúncio; cortes verticais de 15 s para TikTok e Reels.
- **Estilo:** o mesmo visual 3D estilizado do jogo, no motor do jogo quando possível, para mostrar que aquilo é o que se joga.

**Do teaser para o tutorial: "O irmão novo da rua"**

A mesma história vira a primeira partida do jogo. O jogador escolhe ser o irmão ou a irmã e aprende tudo dentro da história, sem tela de instrução.

1. **Chegada:** cena curta do carro chegando; a turma jogando ao fundo.
2. **"Falta um, bora?":** a turma chama o jogador. A partir daqui, cada personagem da turma ensina um fundamento falando como criança de rua.
3. **Taco no chão e batida:** o garoto da turma arremessa devagar; o jogador aprende a ficar no círculo e rebater (○ forte, X toque, △ cavadinha).
4. **Corre e cruza:** o irmão ou a irmã, controlado pela IA, faz dupla com o jogador; aprende a sair da base e cruzar no meio (X no anel).
5. **Agora é a defesa:** a menina da turma ensina a pegar a bola (□), passar (X) e chutar na garrafa (○).
6. **Guardião:** aprende a ficar atrás da casinha e queimar quem sai cedo.
7. **Primeira partida de verdade:** até 5 pontos, irmãos contra dois da turma. Ganhando ou perdendo, a mãe chama para jantar, os dois pedem "só mais uma" e o jogo abre o menu principal e o Modo Temporada.

- **Duração:** 8 a 12 minutos; dá para pular a qualquer momento.
- **Personagens fixos:** os irmãos e a turma da rua viram personagens do jogo, aparecem no lobby, na narração e no modo história futuro.

**Comunidade**

- Discord oficial com canais de turmas, procura de dupla e sugestão de regras.
- Torneio comunitário "Copa da Rua" por temporada, com transmissão e skin exclusiva para a dupla campeã.
- Modo de regras da comunidade: a regra mais votada do mês vira evento.

**Clipes compartilháveis**

O jogo grava sozinho os últimos 30 segundos. No fim da partida, um botão gera o clipe do melhor lance em vídeo vertical, com a marca do jogo e um efeito de câmera lenta, pronto para TikTok, Reels e WhatsApp. É marketing feito pelos próprios jogadores.

**Linha do tempo de marketing**

1. **Fase 0:** reservar nome, domínio e @; perfil com devlog e enquetes de regras.
2. **Fase 1:** página da Steam com lista de desejos, primeiro trailer, Discord aberto.
3. **Fase 2:** Next Fest, playtest aberto, influenciadores, campanha "A rua voltou".
4. **Fase 3:** BGS, Copa da Rua, campanhas de console.

## Balanceamento e pontos de estudo

**Jogadas que podem quebrar o jogo e o contra de cada uma**

| Risco | Por que preocupa | Contra previsto |
| --- | --- | --- |
| Só dar toque | Seguro demais, partida vira lenta | Toque dá no máximo uma cruzada; guardião perto da casinha pega fácil |
| Só cavadinha | Bola longe sempre | Pegada no ar tira o taco; fielder com sprint chega embaixo |
| Nunca sair da base | Partida sem ponto | Tempo limite e arremessos alternados forçam risco; IA e jogadores vão pressionar no pingo |
| Arremesso só de pingo | Difícil de rebater | Cai devagar e dá tempo de mirar; batida perfeita no pingo vai mais longe |
| Guardião colado na casinha | Queima tudo | Batida para trás passa por ele e conta como bola livre; colado demais não alcança a bola rebatida para frente |
| Correr com a bola até a garrafa | Elimina o passe | Velocidade máxima de 4 m/s com bola na mão, sem sprint |

Os dados de partida mostram qual jogada vence mais do que deveria; o ajuste é feito nos números da seção de dinâmica, sem atualizar o jogo inteiro.

**O que estudar antes e durante o desenvolvimento**

- **Sensação do jogo (game feel):** como Wii Sports Baseball, Super Mega Baseball e Rocket League fazem o impacto: pausa de 2 a 3 quadros no contato, tremida de câmera, som e vibração. Pesa mais que gráfico.
- **Jogos de referência gravados:** gravar partidas de FIFA, Super Mega Baseball e jogos de críquete e analisar câmera, tempo das animações, HUD e feedback lance a lance. Abrir os arquivos desses jogos para copiar código ou modelos não é permitido (licença e direitos autorais); o estudo é pela observação, por palestras da GDC sobre câmera e game feel e por jogos esportivos open source.
- **Jogo sem repetição:** eventos de rua, física por arena, clima e regras da vila como modificadores para a partida 50 não parecer a 1.
- **Modo história (futuro):** "da rua ao campeonato", começando com a turma do bairro até a Copa da Rua, para quem joga sozinho.
- **Versão internacional:** testar se "street cricket" funciona como posicionamento fora do Brasil.
- **Questões legais:** registro do nome no INPI, associação de "bets" com aposta (classificação e anúncios), nenhuma marca real de refrigerante nas garrafas, declaração de conteúdo gerado por IA na Steam.

## Desafios técnicos e stack

O jogo é simples de entender e difícil de construir porque 4 jogadores veem o mesmo lance com câmeras e controles diferentes, em tempo real, com física da bola decidindo tudo.

| Desafio | Por que é difícil | Abordagem |
| --- | --- | --- |
| 4 visões sincronizadas | Cada cliente renderiza uma câmera e uma UI diferente para o mesmo estado | Estado de jogo único no servidor; cada cliente só escolhe câmera e HUD pelo papel e pela fase do lance (máquina de estados) |
| Timing da batida com latência | A janela de batida perfeita tem cerca de 100 ms | O cliente do rebatedor decide o timing localmente com o relógio sincronizado; o servidor valida a janela com tolerância |
| Física da bola determinística | Quique e efeito precisam dar o mesmo resultado em todos os aparelhos | Física simplificada própria (trajetória parabólica + quique com coeficiente por superfície), rodando no servidor a 30 ticks/s |
| Queimada justa | Casinha cair perto do taco entrar no círculo gera briga | Servidor decide pelo instante exato do impacto; replay automático em câmera lenta nos lances apertados |
| Matchmaking de duplas | Juntar desconhecidos que precisam cooperar | Rating por jogador e por dupla; prioridade para duplas de amigos |
| Desempenho em PC de entrada e Steam Deck | Boa parte do público joga em PC modesto | Low poly, 60 fps alvo, perfil de qualidade automático |

**Stack sugerida**

| Parte | Tecnologia | Observação |
| --- | --- | --- |
| Engine | **Unity 6** (C#) | Mais fácil para começar, ótima para low poly, vários controles no mesmo PC (Input System) e Steam Input. Alternativas: Godot 4 (gratuita, mais simples) ou Unreal (visual melhor, curva mais pesada) |
| Física da bola | Própria, simples | Trajetória, quique e efeito com os números da seção de dinâmica, iguais em todos os aparelhos |
| Rede (online) | Photon Fusion ou Netcode for GameObjects | Servidor autoritativo em São Paulo, com predição para o arremesso e a batida |
| Lobby, amigos e voz | Epic Online Services ou Unity Lobby + Vivox; Steamworks | Lobby aberto, canais de voz por time, convites e Remote Play Together |
| Backend | Nakama ou PlayFab | Contas, temporadas, ranking, missões e loja |
| Modelos 3D | Blender | Com IA para gerar e ajustar (ver seção de IA) |
| Animação | Mixamo, Move AI / Rokoko, Cascadeur | Esqueleto automático, captura pelo celular e polimento |
| Som | FMOD | Sons que mudam com a superfície, o clima e a força da batida |
| Dados | Eventos de partida no Databricks | Balanceamento, matchmaking, antichatice |
| Build e testes | GameCI no GitHub | Gera versões de teste automaticamente a cada mudança |

## Ferramentas de IA para construir o jogo

Sem experiência em desenvolvimento de jogos, o caminho é usar IA em cada etapa, com o Claude Code como peça central ligado direto na engine.

| Etapa | Ferramenta | O que faz |
| --- | --- | --- |
| Programar | Claude Code + Unity MCP (Unity CLI, `unity mcp`, exige Unity 6) | Cria cenas e objetos, escreve e corrige o C#, lê o console, roda testes e gera builds a partir de pedidos em português. O servidor antigo dentro do editor foi descontinuado |
| Programar (alternativa) | Claude + Godot MCP | A mesma ideia na Godot |
| Modelos 3D | Tripo ou Meshy | Personagens, tacos, garrafas e carros a partir de texto ou imagem, já com esqueleto para animar; Tripo entrega malha mais limpa |
| Modelos com mais detalhe | Rodin (Hyper3D) | Texturas melhores; a malha precisa ser simplificada |
| De concept art para 3D | Higgsfield 3D | Transforma as artes já geradas em modelo .glb |
| Montar e ajustar no Blender | Blender MCP + Claude | Cria e edita objetos, roda scripts, puxa texturas e cenários do Poly Haven |
| Animação | Mixamo (gratuito) | Esqueleto automático e biblioteca de corrida, arremesso e comemoração |
| Animação própria | Move AI, Rokoko Video ou DeepMotion | Grava alguém rebatendo com o celular e vira animação 3D |
| Polimento de animação | Cascadeur | Poses de ação com física realista |
| Efeitos sonoros | ElevenLabs Sound Effects | "Tec" da madeira, garrafa caindo, bola no asfalto; uso comercial em plano pago |
| Trilha | ElevenLabs Music ou Stable Audio | Licenças comerciais mais limpas; evitar Suno pelos processos de gravadoras |
| Arte 2D, ícones, marketing | Higgsfield, Kling, Recraft | Concept art, logo, interface e posts |

**Cuidados**

- **Steam:** arte, modelos, voz e sons gerados por IA precisam ser declarados na página do jogo; IA usada só para programar não precisa.
- **Direitos autorais:** arte 100% gerada por IA provavelmente não tem proteção. Logo, mascote e personagens principais precisam de retrabalho humano para serem protegíveis.
- **Comprovantes:** guardar a assinatura ativa de cada ferramenta na data em que cada asset ou música foi gerado.
- **O papel que nenhuma IA faz:** jogar, sentir e dizer "isso está gostoso" ou "isso está ruim". Esse é o trabalho do criador.

## Protótipo cinza — guia de início

**Preparação (uma vez)**

1. Instalar o Unity Hub e, nele, o Unity 6 LTS (conta gratuita, plano Personal).
2. Criar projeto com o modelo **Universal 3D**, nome **BetsDeRua**.
3. Instalar o pacote **Input System** pelo Package Manager (o Unity pede para reiniciar).
4. Conectar o Claude Code ao Unity pelo MCP oficial da Unity (seguir a documentação atual da Unity para o comando de conexão).
5. Iniciar um repositório Git na pasta do projeto, com o .gitignore padrão de Unity.
6. Ligar 2 controles no PC.

**Pedido 1 para o Claude Code (copiar e colar)**

```
Você vai me ajudar a criar o protótipo cinza do jogo "Bets de Rua", versão digital do bets (taco) brasileiro. Projeto Unity 6, URP, Input System. Estou conectado pelo Unity MCP. Eu não sou dev de jogos: explique em português o que fez ao fim de cada etapa e pare para eu testar.

Objetivo do protótipo: testar se o lance é divertido no controle. Sem arte, sem menu, sem online. Só formas cinzas.

Etapa 1 — Cena e campo
- Crie a cena "Prototipo" com um chão plano de 50 m x 14 m (cinza escuro) e muros baixos nas bordas.
- Duas bases (círculos brancos de 0,6 m de raio) a 20 m uma da outra, no centro do campo.
- Em cada base, uma "casinha": cilindro de 0,3 m de altura (garrafa), com Rigidbody, que cai se for atingida acima de 15 km/h.
- Linha do meio marcada no chão.
- Câmera lateral estilo transmissão de futebol, mostrando o campo inteiro e seguindo a bola suavemente.

Etapa 2 — Jogadores
- 4 cápsulas: dupla A (azul) com tacos, uma em cada base; dupla B (vermelha) na defesa, uma atrás de cada casinha.
- Cada cápsula de rebatedor tem um taco (cubo fino de 0,73 m).
- Suporte a 1 a 4 controles; um controle pode controlar a dupla inteira trocando de jogador com LB/L1.
- Movimento: andar 2 m/s, correr 5 m/s (RT/R2), sem sprint ainda.

Etapa 3 — Arremesso
- O defensor com a bola mira com o analógico esquerdo na casinha oposta (linha de trajetória no chão).
- B/Círculo = arremesso forte (75 km/h), A/X = quicado (55 km/h), Y/Triângulo = por cima.
- Física própria da bola: gravidade 8,5 m/s², quique no asfalto 70%.
- Todos os números num ScriptableObject "ConfigFisica" para eu ajustar sem mexer no código.

Etapa 4 — Batida
- O rebatedor parado no círculo está seguro (taco no chão automático).
- B/Círculo = batida forte, A/X = toque, Y/Triângulo = cavadinha. Direção pelo analógico esquerdo.
- Janela de tempo: perfeito (±50 ms), bom (±120 ms), ruim. Mostre na tela o atraso em ms depois de cada batida.

Etapa 5 — Corrida e cruzada
- Depois da batida, a dupla corre trocando de base; cruzar no meio com A/X quando os tacos se encontram = 1 ponto.
- O ponto só vale quando o taco encosta na base de novo.
- Defesa: pegar a bola (Quadrado/X), passar ao parceiro (A/X) e derrubar a casinha com a bola enquanto o rebatedor está fora da base = perde o taco e os times trocam.
- Bola pega no ar = perde o taco.

Etapa 6 — Placar
- Placar simples na tela; quem fizer 10 e fechar com uma cruzada ganha. Botão Start reinicia.

Regras de trabalho
- Faça uma etapa por vez e espere eu testar antes da próxima.
- Código em C#, organizado em pastas (Scripts/Jogadores, Scripts/Bola, Scripts/Partida), com comentários em português.
- Commit no Git ao fim de cada etapa que funcionar.
- Se algo não der para fazer pelo MCP, me diga exatamente o que clicar no editor.

Comece pela Etapa 1.
```

**Depois de cada etapa:** jogue 5 minutos, anote o que ficou estranho ("a bola quica demais", "a batida é difícil") e mande ao Claude Code em frases simples. Os números no ConfigFisica dão para ajustar na hora, com o jogo rodando.

## Conselho de Inovadores — 2ª rodada

O conselho analisou o documento completo (mercado, personas, orçamento, equipe, jurídico, teaser, tutorial e riscos).

**Veredito: GO, com uma condição — parar de documentar e construir.** 5 GO, 2 PIVOT de foco, nenhum NO-GO.

| Critério | 1ª rodada | 2ª rodada | Por quê |
| --- | --- | --- | --- |
| Problema real | 7 | 7 | Nostalgia confirmada, mas ainda sem prova com jogadores |
| Mercado | 5 | 7 | Público de 30 a 54 anos dimensionado; 10 a 50 mil cópias é alcançável |
| Diferenciação | 8 | 8 | Ninguém fez o bets para PC e console |
| Viabilidade para este fundador | 5 | 6 | Custos baixos e caminho com IA claros; tempo do Gabriel segue o gargalo |
| Receita | 5 | 6 | Preço, passe de amigo e meta de listas de desejos definidos |
| Timing | 6 | 7 | Marco Legal regulamentado, retrô em alta |
| **Execução (novo)** | — | **3** | Documento enorme, zero protótipo |

**O que cada conselheiro disse**

- **Primeiros princípios (GO):** "O núcleo é um só: o lance de rebater, correr e cruzar é gostoso no controle? Nenhuma das 40 páginas responde isso. Só o protótipo responde."
- **Obsessão pelo cliente (GO):** "As personas são o melhor avanço. O Marcelo tem pouco tempo e quer jogar com o filho: isso valida a partida curta e o modo sofá como coração do MVP, não o online. Teste com um pai e um filho de verdade na semana 2."
- **Investidor de valor (GO):** "A conta fecha: R$ 2 a 4 mil para descobrir se presta. Mas o funil tem uma suposição pesada — metade dessa geração ter jogado bets. A enquete 'Como chamava na sua rua?' vira um dado real e precisa rodar já, em paralelo ao protótipo."
- **Fundador (PIVOT de foco):** "Risco número um mudou: antes era o jogo não ser divertido, agora é o fundador se apaixonar pelo documento. Planejar temporadas, skins lendárias e modo história é gostoso e não custa nada; construir o lance custa. Congelar o documento até o protótipo existir."
- **Oferta (GO):** "Passe de amigo mais preço de lanche é a oferta certa para a turma do Rafa. A frase 'Você jogava isso na rua. Agora ensina seu filho' é mais forte que todas as outras — testar como anúncio já com as concept arts."
- **Contrarian (PIVOT de foco):** "45,7% dos jogadores brasileiros se preocupam com IA no processo criativo. Seu público é justamente o mais desconfiado de 'coisa feita às pressas'. Ou a arte da vitrine é humana e caprichada, ou a nostalgia vira deboche nos comentários. Outra coisa: o teaser lindo não pode vir antes do jogo existir."
- **Designer (GO):** "O tutorial com a história dos irmãos é a melhor ideia nova: ensina, emociona e vende ao mesmo tempo. Mas no protótipo, nada de história — cápsulas cinzas e um campo. Se for divertido cinza, vai ser divertido bonito."

**O que mudou desde a 1ª rodada**

- Mercado e público deixaram de ser chute; o tamanho é suficiente.
- O risco migrou do "vai dar certo?" para o "vai sair do papel?".
- Personas confirmam o MVP: sofá primeiro, online depois.

**Decisões do conselho**

1. **Documento congelado** até o protótipo ser testado. Só entram no documento aprendizados de teste.
2. **Protótipo cinza em até 14 dias:** um campo, 2 a 4 controles no sofá, arremesso, batida, corrida e cruzada.
3. **Teste com as personas reais:** 3 amigos da turma e pelo menos um pai jogando com o filho.
4. **Em paralelo, 1 hora por semana de marketing:** perfil no Instagram e a enquete "Como chamava na sua rua?" para validar o funil.
5. **Critério de corte mantido:** se depois de 5 partidas ninguém pedir "mais uma", ajustar o lance ou parar.

**Frase final do conselho:** "O projeto está pronto para existir. Agora falta ele existir."

## Mercado e concorrência

O Brasil é o maior mercado de games da América Latina, com mais de 100 milhões de jogadores e receita projetada perto de US$ 2,8 bilhões em 2026 (PwC). Para o Bets de Rua, o recorte que importa é PC e console, onde está o jogador mais engajado.

| Dado | Número | Fonte |
| --- | --- | --- |
| Brasileiros que jogam | 75,3% (era 82,8% em 2025) | Pesquisa Game Brasil 2026 |
| PC como plataforma principal | 21,2% dos jogadores | Pesquisa Game Brasil 2026 |
| Console como plataforma principal | 24% dos jogadores | Pesquisa Game Brasil 2026 |
| Receita do mercado brasileiro | cerca de US$ 2,8 bi em 2026 | PwC, via Estado de Minas |
| Tendência de preço em PC e console | jogos de US$ 30 a 50 em alta; indies vendem mais na faixa de até US$ 20 | Newzoo 2026 |
| Preocupação com IA | Jogadores brasileiros demonstram desconfiança com jogos feitos com IA | Pesquisa Game Brasil 2026 |

**Concorrência**

| Tipo | Exemplos | O que tiram de nós | Nossa diferença |
| --- | --- | --- | --- |
| Futebol | EA Sports FC, eFootball | O tempo da turma no sofá | Partida curta, jogo novo e brasileiro, nostalgia de rua |
| Taco e bola | Super Mega Baseball, jogos de críquete, Wii Sports | Quem gosta de rebater | Dinâmica de cruzar os tacos com a dupla e o caos de rua |
| Party games | Rocket League, jogos de sofá | A noite com os amigos | Tem disputa séria com temporada, não só zoeira |
| Bets digital | Pequenos jogos mobile e web | Quase nada | Ninguém fez o bets de verdade para PC e console |

**Ponto de atenção:** parte do público rejeita jogos que "parecem feitos por IA". A saída é direção de arte forte e retrabalho humano nos assets que aparecem na vitrine (logo, mascote, personagens principais, trailer).

**Público-alvo inicial: a geração que jogou na rua (anos 80 e 90)**

O primeiro cliente é quem foi criança nos anos 80 e 90, hoje com cerca de 30 a 54 anos: já conhece o bets, compra pela nostalgia e depois ensina os filhos. Não existe pesquisa específica sobre quem jogou bets; o tamanho abaixo combina dados públicos com suposições marcadas como tal.

| Dado | Número | Fonte |
| --- | --- | --- |
| População do Brasil | 212,7 milhões (2025) | IBGE / PNAD |
| Faixas que mais jogam | 25 a 29 anos (16,2%), 30 a 34 (16,1%), 35 a 39 (15,5%) | Pesquisa Game Brasil 2026 |
| Millennials (30 a 44 anos) entre os jogadores | 33,7% (eram maioria em 2025; hoje a Geração Z lidera com 36,5%) | Pesquisa Game Brasil 2026 |
| Jogadores com 45 anos ou mais | 15,8% | Pesquisa Game Brasil 2026 |
| Jogadores de classe média | 54,9% | Pesquisa Game Brasil 2026 |
| Aceitariam comprar jogo feito com bastante IA | 39,3% (45,7% se preocupam com IA no processo criativo) | Pesquisa Game Brasil 2026 |
| Mercado global de games retrô e nostalgia | cerca de US$ 4,2 bilhões em 2026, puxado por millennials e Geração X com renda | Pesquisas de mercado de retro gaming |

**Funil estimado do público nostálgico (só Brasil)**

| Etapa | Tamanho aproximado | Como chegamos |
| --- | --- | --- |
| Jogadores de 30 a 54 anos | 35 a 45 milhões | Mais de 100 milhões de jogadores × millennials (33,7%) + parte dos 45 ou mais |
| Que jogam principalmente em PC ou console | 15 a 20 milhões | PC (21,2%) + console (24%) como plataforma principal |
| Que jogaram bets na infância | 6 a 12 milhões | Suposição: 40% a 60% jogaram ou conhecem; jogo muito popular nas cidades nos anos 80 e 90 |
| Alcançáveis no 1º ano | 10 a 50 mil compradores | 0,1% a 0,5% do grupo acima, coerente com os cenários de vendas |

**O que isso diz**

- O público nostálgico é grande o bastante: mesmo uma fração pequena paga o projeto no cenário base.
- Ele tem renda, joga PC e console e compra jogo pago, ao contrário do público mais jovem e mobile.
- Esse grupo está diminuindo como fatia dos jogadores, então o segundo público (os filhos, via modo sofá) é o que sustenta o jogo no longo prazo.
- Em pesquisas de retro gaming, a nostalgia aparece como principal motivo de compra e é comum pais apresentarem jogos antigos aos filhos: exatamente a mensagem "Seu pai jogava isso na rua".
- **Validar antes de gastar:** a enquete "Como chamava na sua rua?" e a página de captura medem esse público de verdade; meta de 1.000 seguidores ou 500 e-mails, majoritariamente acima de 30 anos.

**Personas**

**1. Marcelo, o pai nostálgico (público principal)**

- **Quem é:** 41 anos, analista ou técnico, classe média, mora em cidade média ou grande. Casado, dois filhos de 8 e 12 anos.
- **Infância:** jogava bets na rua até a mãe chamar para jantar, com taco de cabo de vassoura e garrafa de refrigerante. Lembra das regras da rua dele e das brigas por "foi queimada!".
- **Como joga hoje:** PlayStation ou PC, umas 5 horas por semana, à noite e no fim de semana. Joga FIFA com os amigos, às vezes online, cada vez mais com os filhos.
- **O que o faz comprar:** ver o bets em um vídeo e pensar "não acredito que fizeram isso"; poder jogar com os filhos e com os amigos da época; preço de um lanche.
- **O que o faz desistir:** controles complicados, partida longa, sensação de jogo barato ou feito às pressas, coisa que lembre aposta.
- **Onde encontrá-lo:** Instagram e Facebook (páginas de nostalgia anos 80 e 90), YouTube, grupos de WhatsApp da turma antiga, TikTok cada vez mais.
- **Frase dele:** "Meu filho precisa saber o que era brincar na rua."

**2. Lucas, o filho (público que sustenta)**

- **Quem é:** 12 anos, joga Roblox, Minecraft e FIFA. Nunca jogou bets na rua.
- **O que o prende:** competição com os amigos, temporada, skins, clipes para mostrar, ganhar do pai.
- **Como chega:** pelo pai no sofá ou por um amigo da escola com passe de amigo.
- **Cuidado:** é menor; voz e chat desligados com desconhecidos, nada de compra por impulso.

**3. Rafa, a turma de amigos (público do online)**

- **Quem é:** 33 anos, grupo de 4 a 8 amigos de infância que hoje moram em cidades diferentes e jogam juntos no Discord.
- **O que procura:** algo novo para a noite de jogo, partidas rápidas, resenha, ranking entre eles.
- **Gatilho de compra:** um amigo compra e chama o resto pelo passe de amigo.

**Como a persona muda o produto**

| Necessidade do Marcelo | Resposta no jogo |
| --- | --- |
| Pouco tempo | Partida de 5 a 8 minutos |
| Jogar com os filhos | Modo sofá, assistências, IA fraca "Moleque da rua" |
| Memória das regras da rua dele | Regras da vila configuráveis |
| Mostrar para os amigos | Clipes verticais e passe de amigo |
| Não parecer aposta | Nome, linguagem e loja sem nenhum termo de aposta |

**Mensagens por persona**

- **Marcelo:** "Seu pai jogava isso na rua. Agora é sua vez." (e a variação para ele: "Você jogava isso na rua. Agora ensina seu filho.")
- **Lucas:** "Duvido ganhar do seu pai."
- **Rafa:** "Chama a turma. A rua voltou."

## Equipe: precisa de dev?

Não é preciso um dev de jogos contratado em tempo integral no começo. Hoje solo devs já lançam jogos completos na Steam com o código quase todo feito por agentes como o Claude Code. Mas a IA não substitui três coisas: gosto (saber se está divertido), revisão de quem conhece jogo e a parte que o público vê.

| Papel | Quem faz | Quando |
| --- | --- | --- |
| Diretor do jogo e produtor | Gabriel | Sempre: decide, testa, prioriza |
| Programação | Claude Code + Unity MCP | Desde o protótipo |
| Revisor técnico de jogos | Freelancer sênior em Unity, por hora | Algumas horas por mês a partir da Fase 1, para revisar rede, física e desempenho |
| Modelos 3D e animação | IA (Tripo, Meshy, Mixamo) | Desde a Fase 1 |
| Direção de arte e retrabalho | Artista freelancer | Logo, mascote, personagens principais e capa da Steam antes do lançamento |
| Áudio | IA (ElevenLabs) + ajuste humano | Fase 1 e 2 |
| Testes | A turma de amigos e, depois, a comunidade no Discord | Desde a semana 2 |
| Comunidade e redes | Gabriel com apoio de IA; depois um social media freelancer | A partir da Fase 2 |

Regra prática: contratar gente só para o que a IA faz mal ou para o que o jogador vê primeiro. O gargalo do projeto é o tempo do Gabriel, não a programação.

## Modelo de negócio e projeção

**Como ganha dinheiro**

- **Jogo pago:** cerca de US$ 9,99 (com preço regional: perto de R$ 29,90 no Brasil). Faixa onde indies multiplayer vendem bem.
- **Passe de amigo:** uma cópia libera até 3 amigos jogarem juntos no sofá ou via Remote Play, e dá desconto para o amigo comprar a própria.
- **Cosméticos e passe de temporada:** só visuais, a partir da Fase 3.
- **Produtos físicos:** kits de taco, bola e garrafa com a marca, como marketing e receita extra.

**Taxas da Steam:** US$ 100 para publicar cada jogo e 30% da receita até US$ 10 milhões (25% acima disso, 20% acima de US$ 50 milhões).

**Projeção simples (estimativa, não promessa)**

Considerando um valor médio recebido de US$ 7 por cópia (preço regional e promoções), menos 30% da Steam e cerca de 15% de impostos, sobram perto de US$ 4,20 por cópia.

| Cenário | Cópias no 1º ano | Receita líquida aproximada |
| --- | --- | --- |
| Conservador | 2.000 | US$ 8 mil |
| Base | 10.000 | US$ 42 mil |
| Otimista | 50.000 | US$ 210 mil |

Para referência: a receita mediana de um indie na Steam fica entre US$ 5 mil e 15 mil. Jogos que chegam ao lançamento com 10 mil listas de desejos costumam faturar US$ 40 a 100 mil no primeiro mês, e algo entre 15% e 25% das listas de desejos vira venda na primeira semana. Por isso a meta de marketing é **10 mil listas de desejos antes do lançamento**.

## Orçamento

Valores mensais e totais aproximados, em reais, para o caminho enxuto (Gabriel + IA + freelancers pontuais). São estimativas para planejar, que precisam ser conferidas nos sites de cada ferramenta.

| Item | Custo estimado | Observação |
| --- | --- | --- |
| Claude (plano que inclui Claude Code) | R$ 500 a 1.100/mês | Peça central de programação |
| Unity Personal | Gratuito | Pago só acima de um limite de receita anual; Unity Pro custa cerca de US$ 2.310 por ano por pessoa |
| Geração 3D (Tripo ou Meshy) | R$ 100 a 300/mês | Só nos meses de produção de assets |
| Áudio (ElevenLabs) | R$ 30 a 120/mês | Plano com licença comercial |
| Imagens e marketing (Higgsfield etc.) | R$ 100 a 300/mês | Já em uso |
| Taxa da Steam | cerca de R$ 550 | Uma vez por jogo |
| Controles extras para teste | R$ 600 a 1.000 | 2 controles para o modo sofá |
| Artista (logo, mascote, capa) | R$ 3.000 a 8.000 | Uma vez, antes da página da Steam |
| Revisor técnico freelancer | R$ 150 a 300/hora | 5 a 10 horas por mês a partir da Fase 1 |
| Registro de marca no INPI | algumas centenas de reais | Taxas oficiais por classe |
| Abertura de empresa e contador | R$ 150 a 500/mês | A partir da venda |
| Marketing (anúncios, kits, eventos) | R$ 5.000 a 20.000 | Concentrado na Fase 2 e no lançamento |

| Fase | Total aproximado |
| --- | --- |
| Fase 0 — protótipo (1 a 2 meses) | R$ 2 mil a 4 mil |
| Fase 1 — MVP online (2 a 3 meses) | R$ 8 mil a 20 mil |
| Fase 2 — acesso antecipado e marketing | R$ 10 mil a 35 mil |

## Financiamento e incentivos

- **Recursos próprios primeiro:** o protótipo cabe no bolso e prova a diversão antes de buscar dinheiro de fora.
- **Marco Legal dos Games (Lei 14.852/2024):** reconhece o desenvolvimento de jogos como pesquisa, desenvolvimento e inovação. O Decreto 13.138/2026 regulamentou a lei: a Ancine vai definir como jogos acessam a Lei do Audiovisual e o Fundo Setorial do Audiovisual, e foi criada a plataforma GameBrasil para divulgar jogos nacionais. Vale acompanhar os primeiros editais.
- **Editais municipais e estaduais:** a Spcine abriu em 2026 a Incubadora de Games (inscrições em julho e agosto); outros estados têm editais de cultura que aceitam games. Florianópolis e Santa Catarina têm polo de tecnologia ativo, vale procurar editais locais.
- **Programas das plataformas:** PlayStation empresta kit de desenvolvimento de PS5 de graça para parceiros registrados; o ID@Xbox oferece kits e também permite usar console comum em modo de desenvolvedor; Nintendo exige cadastro e aprovação.
- **Publisher indie:** só se fizer sentido depois da demo; publisher adianta dinheiro e marketing em troca de uma parte da receita.
- **Eventos com rodada de negócios:** BIG Festival e BGS conectam desenvolvedores a publishers e investidores.

## Jurídico e empresa

- **Empresa:** abrir quando for vender. Para um estudo de jogos, a lei já reconhece profissões e prevê código de atividade próprio; um contador define se cabe Simples Nacional e qual enquadramento.
- **Como Gabriel já trabalha com contrato para empresa estrangeira:** checar com o contador como separar a empresa do jogo da atual forma de receber.
- **Marca:** registrar "Bets de Rua" e o logo no INPI nas classes de jogos eletrônicos e de vestuário e brinquedos (para os kits físicos).
- **Direitos autorais:** a lei protege o autor dos elementos do jogo; arte 100% gerada por IA tende a não ter proteção, por isso os elementos centrais precisam de retrabalho humano.
- **Contratos com freelancers:** sempre com cessão de direitos autorais para a empresa.
- **Classificação indicativa:** a lei dispensa autorização para lançar jogo, mas exige classificação etária; na Steam e nos consoles ela sai pelo questionário IARC.
- **Proteção de menores:** a lei pede canal de denúncia e retorno ao usuário em jogos com interação entre jogadores. Já está previsto na seção de segurança online.
- **LGPD:** política de privacidade, coleta mínima de dados, consentimento dos responsáveis para menores e opção de apagar a conta.
- **Termos de uso:** regras de conduta, punições, propriedade dos itens cosméticos.

## Arquitetura técnica e dados

- **Estrutura do jogo:** uma máquina de estados para o lance (arremesso, bola viva, bola morta, troca de papel, fim de partida); cada jogador só escolhe câmera e HUD a partir desse estado.
- **Física:** código próprio e determinístico, com todos os números da seção de dinâmica em um arquivo de configuração para ajustar sem mexer no código.
- **Online:** servidor autoritativo em São Paulo; os clientes preveem o próprio movimento e a batida, e o servidor confirma.
- **Serviços:** contas, amigos, lobby e voz pela plataforma (Steam e Epic Online Services); temporada, ranking e loja no backend (Nakama ou PlayFab).
- **Eventos de dados registrados em toda partida:** início e fim, cada arremesso (tipo, efeito, velocidade), cada batida (tempo, tipo, distância), cada cruzada, queimada, pegada no ar, abandono, revanche. Vão para o Databricks para balancear, ajustar o matchmaking e detectar trapaça.
- **Salvamento:** progresso na nuvem da plataforma, com cópia no backend.
- **Versões:** código no GitHub, builds automáticos com GameCI, um ramo de testes e um ramo de lançamento.

## Bíblia de personagens

A turma da rua é fixa e aparece no teaser, no tutorial, na narração e nos menus. Nomes provisórios.

| Personagem | Quem é | Jeito de jogar |
| --- | --- | --- |
| Tampinha | Mascote: moleque de boné virado, joelho ralado, taco com fita isolante | Rebatedor arriscado, sempre quer "mais uma" |
| Duda | A irmã que chega na rua nova; observadora, aprende rápido | Arremessadora precisa, especialista em banana |
| Theo | O irmão novo; tímido no começo, viciado no fim | Rebatedor de toque, seguro |
| Bia | A menina que manda na rua; derruba garrafa de longe | Guardiã temida, queima quem sai cedo |
| Caio | O dono do taco bom | Batida forte, pouca paciência |
| Seu Zé | O tiozão do bar que narra e dá pitaco | Narrador alternativo |
| Dona Cida | A vizinha que devolve (ou não) a bola pela janela | Evento de cenário |
| Pipoca | O cachorro da rua | Evento: corre atrás da bola |

- **Diversidade:** turma com meninos e meninas, diferentes cores de pele, tipos de corpo e regiões do Brasil, sem estereótipo. Apelidos que soem ofensivos são evitados.
- **Personalização:** o jogador cria o próprio personagem, e a turma fixa aparece como bots e no modo história.

## Áudio

- **Efeitos:** "tec" de madeira (varia com o taco), garrafa PET caindo (varia com a casinha), quique por superfície, chuva, carro passando, cachorro latindo, torcida do meio-fio.
- **Trilha:** violão, percussão e um toque de funk e samba-rock; muda no match point e no jogo decisivo da temporada.
- **Narração:** o moleque no muro com centenas de falas curtas, gravadas por um ator de voz (melhor que voz sintética para personalidade) ou geradas com IA e revisadas.
- **Áudio dinâmico:** FMOD ajusta som pela força da batida, pela distância da câmera e pelo clima.
- **Licença:** guardar comprovante de cada ferramenta usada; música licenciada evita bloqueio de vídeo de streamers.
- **Modo streamer:** opção que troca a trilha por músicas 100% livres de direitos.

## Acessibilidade e idiomas

- **Controles:** remapeamento total, alternativa a "segurar" (apertar para alternar), assistências de tempo e mira.
- **Visão:** modos para daltonismo; casinha exposta usa cor e ícone; tamanho de texto ajustável; contraste alto.
- **Audição:** legendas para narração e comandos de voz, indicadores visuais para sons importantes (bola quicando atrás da câmera).
- **Movimento:** reduzir tremida de câmera e câmera lenta.
- **Idiomas no lançamento:** português do Brasil (com gírias) e inglês; espanhol logo depois, por causa da América Latina. Tradução com IA e revisão humana das gírias.

## Plano de testes

| Etapa | Quem testa | O que medir |
| --- | --- | --- |
| Protótipo (semana 2) | Gabriel + 3 amigos no sofá | Pedem "mais uma"? Quantas partidas seguidas? |
| Fase 1 | 10 a 20 pessoas, presencial e online | Entendem os controles sem ajuda em 2 minutos? Onde travam? |
| Playtest fechado na Steam | 100 a 500 pessoas da lista e do Discord | Revanche após derrota, abandono, lag, bugs |
| Steam Next Fest (demo) | Público aberto | Listas de desejos ganhas, tempo médio na demo |
| Acesso antecipado | Compradores | Retorno no dia seguinte, avaliações, temporadas completas |

- Testar sempre com quem nunca jogou bets e com quem jogou na rua.
- Gravar a tela e o rosto (com permissão) nos testes presenciais.
- Um formulário curto no fim: o que foi divertido, o que irritou, jogaria de novo?

## Pós-lançamento

- **Temporadas mensais** com tema, skins e uma regra da vila em destaque.
- **Calendário brasileiro:** verão e praia (dezembro a fevereiro), carnaval, festa junina, Dia dos Pais, volta às aulas, férias de julho, Dia das Crianças, Copa da Rua no fim do ano.
- **Conteúdo novo a cada 2 a 3 meses:** uma arena, um narrador, um conjunto de tacos.
- **Escuta da comunidade:** votação mensal de regras e de qual arena vem a seguir.
- **Correções:** atualização de balanceamento a cada 2 semanas no acesso antecipado.
- **Expansão:** consoles, versão internacional, modo história e, se houver demanda, versão mobile simplificada.

## Riscos e plano B

| Risco | Chance | Impacto | Plano B |
| --- | --- | --- | --- |
| Não ser divertido | Média | Alto | Descobrir no protótipo e ajustar ou parar cedo, com pouco gasto |
| Fila online vazia | Alta no início | Alto | Sofá, Remote Play e bots no lançamento; online próprio só com base |
| Projeto não terminar (tempo do Gabriel) | Alta | Alto | Escopo mínimo, metas semanais, revisor freelancer para destravar |
| Rejeição por parecer "feito por IA" | Média | Médio | Retrabalho humano na vitrine, direção de arte coesa, transparência na página da Steam |
| Confusão com apostas | Média | Médio | Nome Bets de Rua, nome internacional sem "bet", nenhum termo de aposta |
| Jogo copiado por estúdio maior | Baixa a média | Médio | Marca registrada, comunidade e ser o primeiro |
| Problemas técnicos de rede | Média | Alto | Usar serviços prontos (Photon, Epic) e revisor experiente |
| Pouca visibilidade na Steam | Alta | Alto | Começar marketing na Fase 0; meta de 10 mil listas de desejos antes de lançar |
| Questões jurídicas com menores | Baixa | Alto | Voz desligada para menores, denúncia, LGPD desde o início |

## Plano de MVP e roadmap

O MVP prova uma coisa só: que um lance 2x2 online é divertido com as 4 visões. Tudo o que não serve a isso fica para depois. Prazos estimados para um time pequeno (2 a 3 devs + 1 artista).

**Fase 0 — Protótipo local (4 a 6 semanas)**

- [ ] Campo único (rua de asfalto), arte provisória
- [ ] Física da bola: arremesso, quique, batida
- [ ] Os 4 papéis com câmeras automáticas
- [ ] Corrida, cruzada e queimada
- [ ] Jogo contra bots ou 4 controles no mesmo PC

* **Portão:** 10 pessoas testam; a maioria quer jogar de novo

**Roteiro das duas primeiras semanas**

1. **Semana 1:** instalar Unity 6 e Claude Code com o Unity MCP; pedir "monta uma rua cinza com duas casinhas, quatro cápsulas e uma bola que quica". Física da bola com os números da seção de dinâmica.
2. **Semana 2:** arremesso, batida, corrida, cruzada e queimada com controle de Xbox ou PlayStation, ainda com formas simples, na câmera de transmissão.
3. **Fim da semana 2:** jogar com 3 amigos e contar quantas vezes pedem "mais uma". Só depois trocar as cápsulas por modelos do Tripo ou Meshy, animações do Mixamo e sons da ElevenLabs.

**Fase 1 — MVP online (8 a 10 semanas)**

- [ ] Rede com servidor autoritativo e predição
- [ ] Sala privada com código, convite pela Steam e link pelo WhatsApp; modo sofá 1x1 e 2x2
- [ ] Regra padrão: cruzada = 1 ponto, meta 10
- [ ] Tutorial jogável e HUD final
- [ ] Playtest fechado na Steam

* **Portão:** partidas completas sem queda com latência de até 150 ms

**Fase 2 — Lançamento suave (6 a 8 semanas)**

- [ ] Matchmaking rápido e de dupla
- [ ] Amigos, revanche, comandos de voz rápidos
- [ ] 3 campos e primeiros cosméticos
- [ ] Analytics de partida
- [ ] Acesso antecipado na Steam, com página de lista de desejos aberta desde a Fase 1

**Fase 3 — Crescimento**

- [ ] Ranqueada com temporadas e divisões
- [ ] Turmas e torneios entre turmas
- [ ] Passe de temporada
- [ ] PlayStation, Xbox e Switch com crossplay; avaliar versão mobile simplificada

**Perguntas em aberto**

- Nome final: BETS!, Taco!, Bete ou Casinha? Vale testar com público de regiões diferentes, já que o nome muda pelo Brasil.
- Cruzada vale 1 ou 2 pontos no ranqueado?
- Bola pega no ar tira o taco ou termina a partida?

* Taco no chão automático ou modo clássico (segurar L2) como padrão? Decidir no protótipo.
* O 2x1 deve ter alguma vantagem compensatória para quem joga sozinho?
* Quantas divisões e quantas partidas por temporada deixam a subida motivante sem cansar?

## Fontes

- [Bete-ombro — Wikipedia](https://en.wikipedia.org/wiki/Bete-ombro)
- [Regras de Bets (torneio) — Scribd](https://pt.scribd.com/document/782676171/Bets)
- [Os pastéizinhos da minha avó — Recanto das Letras](https://www.recantodasletras.com.br/causos/4478502)
- [Kit Jogo Taco Betes Bel — Magazine Luiza](https://www.magazineluiza.com.br/produto/p/bf58eefd29/te/smsa/)
- [Quais são as regras do jogo de taco?](https://www.auditorioibirapuera.com.br/quais-sao-as-regras-do-jogo-de-taco/)
- [Como jogar tacos?](https://faq.blackpilado.com.br/2026/03/como-jogar-tacos.html)

* [Unity MCP: 10 Things You Can Do](https://mcpplaygroundonline.com/blog/unity-mcp-server-guide.md)
* [How to Use Claude Code with Unity Engine](https://ai-game.dev/blog/claude-code-unity-tutorial)
* [Claude for Godot](https://www.summerengine.com/blog/claude-for-godot)
* [Blender MCP](https://landscape.jimmysong.io/projects/blender-mcp/)
* [Best AI Game Asset Generators in 2026](https://www.3daistudio.com/blog/best-ai-game-asset-generators-2026)
* [Best AI Tools for 3D Animation in 2026](https://www.3daistudio.com/blog/best-ai-tools-for-3d-animation-2026)
* [Best AI Sound Effect Generators for Games](https://app.cinevva.com/guides/ai-sound-effect-generators)
* [Best AI Music Generators for Games](https://app.cinevva.com/guides/ai-music-generators-games)
* [AI Game Assets in 2026: Copyright and Steam Disclosure](https://blog.promise.legal/ai-game-assets-copyright-steam-disclosure-2026/)

- [Pesquisa Game Brasil 2026 — resumo (Shonen Hardware)](https://shonenhardware.com/pesquisa-game-brasil-2026-o-retrato-atual-do-gamer-brasileiro/)
- [Pesquisa Game Brasil 2026 — PSX Brasil](https://psxbrasil.com.br/pesquisa-game-brasil-2026/)
- [Mercado de games no Brasil em 2026 — Estado de Minas](https://www.em.com.br/trends/2026/04/7394288-mercado-de-games-no-brasil-vai-crescer-em-2026-veja-as-tendencias.html)
- [Newzoo: PC and Console Market in 2026 — GameDev Reports](https://gamedevreports.substack.com/p/newzoo-pc-and-console-market-in-2026)
- [Decreto 13.138/2026 — Planalto](https://www.planalto.gov.br/ccivil_03/_ato2023-2026/2026/decreto/d13138.htm)
- [Marco Legal dos Games — Jornal dos Jogos](https://newsletter.jornaldosjogos.com.br/p/o-que-diabos-e-o-marco-legal-dos)
- [Editais Spcine](https://spcine.com.br/editais-spcine/)
- [Steam revenue share explained — Steam Page Analyzer](https://www.steampageanalyzer.com/blog/steam-revenue-share-explained)
- [Indie game revenue data — Steam Page Analyzer](https://www.steampageanalyzer.com/blog/indie-game-revenue-data)
- [Steam wishlist conversion — presskit.gg](https://presskit.gg/field-guides/how-much-does-steam-take)
- [Porting indie games to consoles in 2026 — Gamedō](https://gamedo.live/news/how-to-port-indie-game-consoles-2026/)
- [Indie game development cost 2026 — Juego Studio](https://www.juegostudio.com/blog/indie-game-development-cost)
- [Solo developers are shipping real games — Cinevva](https://app.cinevva.com/signals/2026-03-23-solo-devs-shipping-not-vibing)

* [PGB 2026: faixas etárias e gerações — Meio & Mensagem](https://www.meioemensagem.com.br/marketing/apos-pico-numero-de-jogadores-brasileiros-cai-segundo-pgb)
* [PGB 2026: faixas etárias — Meio & Mensagem (mídia)](https://www.meioemensagem.com.br/midia/games-diversao-brasileiros)
* [PGB 2026 e IA — Omelete](https://www.omelete.com.br/games/pesquisa-game-brasil-2026-ia-jogos)
* [População do Brasil envelhecendo — Agência Brasil](https://agenciabrasil.ebc.com.br/geral/noticia/2026-04/populacao-no-brasil-cresce-em-ritmo-menor-e-esta-envelhecendo)
* [Retro gaming statistics 2026 — Shane the Gamer](https://www.shanethegamer.com/research/retro-gaming-statistics/)
