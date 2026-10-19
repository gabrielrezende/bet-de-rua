# Referências visuais (FC 26, futebol e beisebol) aplicadas ao Bets de Rua

Análise de 14 prints enviados pelo Gabriel: FC 26 (câmera de transmissão, câmera alta, pênalti, menu), um jogo de futebol indie feito em Unity, e jogos de beisebol (beisebol japonês estilo Pro Yakyu, MLB The Show, Super Mega Baseball 4 e MVP Baseball 2004). Cada item diz o que a referência faz e como fica no Bets de Rua. Os números são ponto de partida: tudo vai para `ConfigCamera` / `ConfigHUD` para ajustar jogando.

> Para o Claude Code: leia este arquivo antes de implementar câmera, marcador de jogador, mira, HUD ou menus.

---

## 1. Câmera

### O que as referências mostram
| Print | Câmera | O que aprender |
| --- | --- | --- |
| FC 26, Brasil x França | Transmissão baixa e próxima: lateral, ~15° a 20° de inclinação, segue a bola de perto | Ótima para drama e leitura de jogadores, mas mostra só um pedaço do campo |
| FC 26, câmera alta (Brasil x Haiti, Trujillo x Mixes) | Transmissão alta e aberta: lateral, ~35° a 45° de inclinação, mostra metade do campo | É a que mais se parece com o que o bets precisa: vê quem corre atrás da bola e onde está o adversário |
| Indie em Unity (Alemanha x Hungria) | Quase de cima: ~55° a 60° | Muito legível e fácil de fazer; tira um pouco da sensação de "transmissão" |
| FC 26, pênalti | Atrás do ombro do cobrador, baixa, olhando para o gol | É exatamente a câmera do arremesso (zoom rápido atrás do arremessador) |

### Como fica no Bets de Rua
O campo do bets é **comprido e estreito** (50 m x 14 m, proporção 3,5 para 1; o de futebol é 1,5 para 1). Isso favorece a câmera lateral: a largura inteira da rua cabe na tela e a câmera só precisa deslizar de lado.

**Câmera padrão: "Transmissão alta"** (inspirada no FC 26 alto)
- Posição: na calçada de um lado da rua, a ~14 m de altura e ~20 m de distância da linha central do campo.
- Inclinação: ~35° para baixo. Campo de visão: ~40°.
- Enquadramento: as duas casinhas visíveis quando a bola está perto do meio; quando a bola vai para o fundo, a câmera desliza na direção dela (amortecimento ~0,3 s) mas mantém a casinha mais próxima em quadro.
- Zoom dinâmico: abre até ~15% quando os 4 jogadores e a bola se espalham; fecha quando o lance se concentra num lado.
- Nunca gira: a rua fica sempre na horizontal da tela, para o jogador não perder a orientação.

**Decisão final: a câmera de transmissão lateral vale o lance inteiro, inclusive arremesso e batida** (decidido pelo Gabriel depois das referências de beisebol, ver seção 9.1)
- No bets o rebatedor **não fica parado esperando a bola**: se ela vier torta, ele pode sair do círculo, correr para cima ou para baixo e rebater mesmo assim, para isolar a bola e ganhar tempo para cruzar. O risco é que, se errar fora do círculo, o guardião atrás pega a bola e queima a casinha na hora.
- Por isso a câmera não pode travar atrás do rebatedor (ele nunca se mexeria). A vista lateral de longe, como num campo de futebol, mostra o rebatedor, a bola chegando e o guardião atrás dele ao mesmo tempo, com o triângulo de seleção em cima de cada um, e deixa todo mundo se movimentar.
- No momento do arremesso, a câmera só **aproxima um pouco** (~10% a 15%) do lado da casinha que vai receber a bola, sem mudar de ângulo, e abre de novo quando a bola é rebatida.
- Como na vista lateral a profundidade é mais difícil de ler, o tempo da batida ganha ajudas visuais (seção 9.1).

**Opções no menu (depois do protótipo):** Transmissão próxima (estilo FC 26 baixo), Transmissão alta (padrão), Vista de cima (estilo indie), e a câmera por papel do online 2x2.

---

## 2. Marcador do jogador controlado

### O que as referências mostram
- FC 26 (câmera alta): **triângulo pequeno** na cor do time, flutuando acima da cabeça.
- Indie: **círculo tracejado amarelo no chão**, sob os pés, mais **etiqueta com nome e barra** acima da cabeça.
- FC 26 (transmissão próxima): o nome do jogador controlado vai para o **canto inferior** da tela, com barra de estamina em cima do nome.

### Como fica no Bets de Rua
- **Triângulo acima da cabeça** na cor da dupla (azul ou laranja); pisca de leve quando a troca de jogador acontece.
- **Círculo de giz no chão** sob o jogador ativo, desenhado à mão (combina com a marca "giz no asfalto"). Bom porque continua visível quando a cabeça sai da tela ou há jogadores sobrepostos.
- No 2x2 local, cada pessoa tem uma cor de contorno própria no triângulo (P1, P2, P3, P4), como no FIFA.
- Barra de estamina do sprint fina, colada embaixo do nome no canto da tela (não em cima da cabeça, para não poluir).

---

## 3. Mira e trajetória

- Pênalti do FC 26: **círculo luminoso no chão** marca onde a bola está/vai, e o goleiro tem um marcador acima.
- No Bets de Rua:
  - Arremesso e chute na garrafa: **círculo amarelo no chão em volta da garrafa alvo** + linha pontilhada da trajetória no chão.
  - Batida: a linha pontilhada mostra para onde a bola vai sair; some quando a bola é rebatida.
  - Bola no ar: **círculo no chão onde ela vai cair** (já está no GDD), na cor amarela "bolinha".
  - Garrafa exposta (taco fora do círculo): o círculo da base fica **vermelho**.

---

## 4. HUD (informações na tela)

### O que as referências mostram
- **Placar no canto superior esquerdo, sempre compacto:** bandeira/escudo, sigla, placar, relógio e, às vezes, a fase ("Grupo B"). Nos prints de câmera alta, o placar é ainda menor.
- **Minimapa embaixo no centro**, semitransparente, com pontos coloridos por time (FC 26 e indie).
- **Nomes dos jogadores ativos nos cantos inferiores** (esquerdo = seu time, direito = adversário), com um rótulo de estado ("ATTACKING").
- **Painel de comandos contextuais** no canto superior direito no pênalti ("LMB SHOOT", "Mouse AIM"): mostra só os botões que valem naquele momento.
- Marca do jogo discreta no canto superior direito.

### Como fica no Bets de Rua
| Posição | Elemento |
| --- | --- |
| Superior esquerdo | Placar compacto: escudo da dupla, nome, pontos, `AZUL 7 x 4 LARANJA`, relógio. Abaixo, pequeno: "bolas para trás: ●●○" de quem está rebatendo. Em match point, o placar pisca e ganha a etiqueta MATCH POINT. |
| Inferior centro | **Minimapa da rua**: uma faixa fina (proporção 3,5:1, perfeita para o campo do bets) com as duas casinhas, os 4 jogadores e a bola. Útil quando a bola vai longe. |
| Inferior esquerdo | Jogador ativo: nome + barra de estamina + estado ("REBATENDO", "CORRENDO", "DEFENDENDO"). |
| Inferior direito | Jogador ativo do adversário (no 1x1) ou o seu parceiro (no 2x2). |
| Superior direito | Comandos contextuais (modo assistido e tutorial): só os botões válidos agora. Ex.: na batida, "○ Forte · X Toque · △ Cavadinha". |
| Centro, temporário | Feedback de tempo: "PERFEITO", "BOM", "ATRASOU 40 ms"; "+1" amarelo na cruzada que fica verde ao validar; "QUEIMOU!". |

Regra geral: no padrão, o HUD ocupa menos de 15% da tela. Tudo que é temporário some em ~1,5 s.

---

## 5. Clima e chão (do print indie com chuva)
- A chuva no indie é feita com **riscos verticais de partícula e manchas escuras de chão molhado**: barato e lê bem. Serve de referência direta para a chuva do Bets de Rua no asfalto (manchas de poça que freiam a bola).
- As faixas claras e escuras no gramado ajudam a perceber velocidade e distância. No asfalto, o equivalente são **remendos, tampas de bueiro e marcas de giz** espalhados, que dão referência de movimento sem custar nada.

---

## 6. Menu principal (do print do FC 26)
- Lista vertical de modos à esquerda, arte grande à direita, rodapé com os botões válidos ("A Selecionar").
- No Bets de Rua: lista à esquerda escrita a giz no muro, arte da rua ao entardecer à direita com a turma, rodapé com os botões.

**Lista de modos sugerida:**
1. Jogar rápido
2. Temporada
3. Torneios
4. Copa dos Estados / Mundial de Bets
5. Treino
6. Turma (grupo de amigos)
7. Carreira "da rua ao campeonato" (futuro)
8. Loja e álbum
9. Configurações

---

## 7. Modos de torneio

### Torneio (montável)
- De 4 a 32 duplas, mata-mata ou grupos + mata-mata.
- Local (sofá, até 4 jogadores revezando), online entre amigos ou misturado.
- O criador escolhe regras (meta de pontos, regra de vila, clima, arena) e o prêmio é só cosmético (taco ou giz exclusivo, troféu no perfil).
- Chaveamento visual estilo copa, desenhado a giz no muro.

### Copa dos Estados (modo "seleções" nacional)
- As 27 unidades da federação como seleções, cada uma com uniforme, escudo e arena típica (ladeira em Salvador, calçadão no Rio, rua de paralelepípedo em Ouro Preto...).
- Fase de grupos regionais, mata-mata nacional. Pode rodar contra a CPU (offline) ou como evento online temporário.
- Puxa rivalidade regional e conversa com a pesquisa "como chamava na sua rua?" (cada estado com o nome local do jogo).

### Mundial de Bets
- Seleções de países (o jogo como "street cricket": Brasil, Índia, Inglaterra, Austrália, Paquistão, Caribe...). Gancho para a versão internacional.
- Já existiu um "Mundial de Bets" real no Brasil em 2016, com equipes de quatro cidades: bom gancho de marketing e de convite para a comunidade.
- Atenção ao nome: evitar "Copa do Mundo" e qualquer marca da FIFA. "Mundial de Bets" é mais seguro.

### Eventos sazonais
- "Copa de Férias" (janeiro e julho), "Copa Junina" (arena de festa junina), "Fim de semana da Várzea" (já no GDD, torneio semanal).

---

## 9. Lições dos jogos de beisebol

O bets é parente do críquete e do beisebol: o momento arremesso-batida é o mesmo problema que esses jogos resolvem há 20 anos. Estas lições mudam algumas decisões do protótipo.

### 9.1 Câmera da batida: por que NÃO usamos a vista de trás
- Todos os prints de rebatida do beisebol usam a câmera atrás do rebatedor, porque no beisebol o rebatedor fica parado na caixa e só escolhe tempo e altura.
- **No bets é diferente:** o rebatedor pode sair do círculo e ir atrás da bola torta, e o guardião fica atrás pronto para queimar quem erra fora do círculo. Com a câmera de trás, o jogador nunca se movimentaria e não veria o guardião.
- Decisão: **câmera lateral de transmissão o tempo todo**, com aproximação leve no arremesso (seção 1).
- Ajudas para ler o tempo na vista lateral:
  - **Sombra da bola no chão**, sempre visível: mostra a profundidade melhor que a própria bola.
  - **Marca no chão de onde a bola vai passar** perto do rebatedor (um "X" de giz pequeno), aparecendo assim que a bola sai da mão.
  - **Raio de alcance do taco**: um semicírculo fino em volta do rebatedor ativo (~1,5 m). Bola fora do raio = precisa se mexer para alcançar.
  - **Anel de tempo** que encolhe em volta da bola quando ela entra no raio de alcance; apertar quando fecha = batida perfeita (mesma lógica da pegada no ar).
- **Sair do círculo para rebater:** o rebatedor pode andar ou correr para fora do círculo para alcançar a bola. Fora do círculo o taco sai do chão: se rebater, ótimo; se errar ou deixar passar, a casinha fica exposta e o guardião pode pegar a bola e queimar de pertinho. Voltar ao círculo protege de novo.

### 9.2 Mira do arremessador
- O beisebol usa um quadro de zona de strike visto de trás. Na vista lateral do bets, a mira fica **no chão**: um círculo amarelo em volta da garrafa alvo e a linha pontilhada da trajetória.
- A linha só aparece para quem arremessa enquanto mira; ao soltar, ela some e fica só a marca de onde a bola vai passar (9.1). Como na rua o rebatedor vê o arremessador mirando, não é preciso esconder mais nada.

### 9.3 Medidor de força em arco ou barra
- MVP 2004: **arco colorido** que enche e tem uma faixa certa (verde). MLB The Show: **barra vertical em cima do jogador** no arremesso de longe, com a faixa ideal marcada.
- No Bets de Rua: segurar ○ (B) enche um arco ao lado do jogador; soltar na faixa verde = força e precisão máximas; passar da faixa = bola desgovernada (erra a garrafa). Vale para o arremesso e para o chute do fielder na garrafa.

### 9.4 Nome do efeito e velocidade na tela
- O beisebol japonês mostra **"Forkball · 129 km/h"** depois de cada arremesso.
- No Bets de Rua: depois de cada arremesso, uma etiqueta curta: **"Banana · 72 km/h"**, "Folha seca · 64 km/h". Ensina o arsenal de efeitos sem tutorial e alimenta a provocação entre amigos.

### 9.5 Feedback do contato
- Beisebol japonês: o **taco aparece como um rastro azul** mostrando onde passou, um **anel dourado** no ponto de contato e o texto **"Perfect Pitch!"**.
- Super Mega Baseball: texto enorme e estilizado na tela no momento grande (**"HOME RUN"**).
- No Bets de Rua: rastro do taco + anel no contato; textos grandes e estilizados a giz nos momentos-chave: **"PERFEITA!"**, **"CRUZOU!"**, **"QUEIMOU!"**, **"PEGOU NO AR!"**, **"LAVADA!"**. Duram ~1 s e não cobrem a bola.

### 9.6 Ícones de botão sobre os alvos
- MLB The Show e MVP 2004: com a bola na mão, cada base mostra **o ícone do botão** que lança para ela (Y numa base, B em outra), num mini-diamante na tela.
- No Bets de Rua: quando o fielder pega a bola, aparecem ícones pequenos sobre os alvos: **○ sobre a garrafa mais vulnerável, X sobre o parceiro (passe), △ sobre a outra garrafa (lançamento longo)**. O jogador não precisa mirar com cuidado para decidir: olha, vê o botão, aperta. No modo manual os ícones somem.
- Para o ataque, no 2x2: ícone de **R2 "corre"** ou **"fica"** sobre o parceiro, mostrando a decisão dele no minimapa.

### 9.7 Posicionamento da defesa (estilo "shift")
- Os prints de beisebol mostram o **"Double Play Shift" / "Pull Hitter Shift"**: um mini-diamante no canto superior direito com a posição dos defensores, trocado por um botão.
- No Bets de Rua: o guardião tem 3 posições pré-definidas, trocadas no direcional entre os lances: **Colado** (perto da garrafa, para queimar), **Normal** e **Fundo** (para pegar batida forte e bola no ar). Um mini-ícone no canto superior direito mostra a escolha. É a camada tática estilo FIFA, sem complicar o controle.

### 9.8 Placar com contadores em bolinhas
- O beisebol usa **bolinhas para B, S e O** (bolas, strikes e eliminações) ao lado do placar.
- No Bets de Rua: ao lado do placar, **"para trás: ●●○"** e um indicador de qual casinha recebe o próximo arremesso (A ou B), no mesmo estilo de bolinhas.

### 9.9 Cartão do jogador com traços
- O beisebol japonês mostra cartões com notas e traços ("Push Hitter", "Spin Leak").
- No Bets de Rua: **só no modo Carreira (futuro)** e só como personalidade e estilo de animação. No ranqueado e nos amistosos, todos os personagens jogam igual, para não virar pagar para ganhar.

### 9.10 Estilo de arte
- **Super Mega Baseball 4** é a melhor referência de arte para o Bets de Rua: estádio de rua estilizado, bandeirinhas, cores fortes, personagens caricatos, feito por um estúdio indie. Mostra que um visual arcade bem feito vende sem gráfico realista. Bom alvo para a fase de arte, depois do protótipo.

### 9.11 Tela dividida (MVP 2004)
- O MVP 2004 tinha tela dividida no arremesso. Não é necessário no Bets de Rua: a câmera lateral mostra arremessador, rebatedor e guardião numa tela só.

## 8. Ordem de implementação sugerida
1. Protótipo (agora): câmera de transmissão lateral o lance inteiro (aproximação leve no arremesso), sombra da bola + marca de passagem + raio de alcance + anel de tempo, rebatedor podendo sair do círculo, mira no chão em volta da garrafa, medidor de força em arco, triângulo + círculo de giz, placar simples, feedback de tempo da batida com rastro do taco.
2. Depois do teste com amigos: minimapa, comandos contextuais, ícones de botão sobre os alvos, nome do efeito e velocidade, textos grandes a giz, posições do guardião, cantos com nome e estamina.
3. Com o jogo validado: menu principal, Torneio montável, Copa dos Estados, Mundial.
