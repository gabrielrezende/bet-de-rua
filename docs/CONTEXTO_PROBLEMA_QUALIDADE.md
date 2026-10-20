# Bets de Rua — contexto do problema de qualidade (para pesquisa)

## O projeto
- Jogo de PC (Steam) do **bets / taco / bete**, o jogo de rua brasileiro: duas duplas, uma com tacos de madeira defendendo garrafas PET, outra arremessando uma bola para derrubar a garrafa. Quem rebate corre e cruza os tacos no meio para fazer ponto.
- Feito por **uma pessoa (sem experiência em jogos) + IA (Claude Code)**, que escreve todo o código e comanda o editor e o Blender por linha de comando.
- **Motor:** Unity 6 (URP, o modo de luz mais simples). **PC de desenvolvimento:** placa de vídeo integrada AMD Radeon (sem placa dedicada).
- **Meta visual:** realista, perto de EA FC / NBA 2K / MLB The Show. Público adulto que jogava bets na rua.

## O que já funciona bem
Regras completas, física da bola (quique por tipo de chão), câmeras de transmissão (estilo MLB), bots em 3 níveis, 8 cenários sorteados, menus, multiplayer local, gravador de teste quadro a quadro. **O código não é o problema.**

## O problema: a imagem parece de PS1/PS2
Três frentes, em ordem de gravidade:

### 1. Movimento (o mais grave)
- Não existe captura de movimento do **bets**. A pose de espera é parecida com a do **golfe** (taco com a ponta no chão, as duas mãos juntas no cabo, tronco inclinado); a rebatida é um giro lateral parecido com o do **beisebol/críquete**; tem corrida, mergulho (carrinho) e o "cruzar os tacos".
- Hoje usamos animações genéricas gratuitas (Quaternius, CC0: golpe de espada, arremesso) + uma rebatida de beisebol da base de dados CMU, e **corrigimos braço e mão por código** (cinemática inversa). O resultado é duro e às vezes errado (mãos fora do taco, torções estranhas).
- **Dificuldade:** conseguir animações realistas **específicas do bets** (guarda, rebatida, corrida segurando o taco, cruzada) sem estúdio de captura.
- **Ideias em teste:**
  - Gerar imagens de pessoas nas poses do bets por IA → extrair a pose 3D (SAM 3D Body) → montar a animação entre as poses (risco: ficar "robótico").
  - Gerar **vídeo** por IA (ou filmar com um celular) → **vídeo para animação** (Motion2MixamoRig/GVHMR, Uthana, DeepMotion, Rokoko Vision, Move.ai). Cuidado com a licença: modelos SMPL/SMPL-X não são livres para uso comercial.

### 2. Personagens
- Testamos: Quaternius (estilizado), **MakeHuman/MPFB** (CC0, realista "de manequim", sem rosto expressivo, cabelo ruim) e **personagem gerado por IA** (imagem → 3D com Tripo, pelo Higgsfield).
- O personagem da IA ficou **bem mais realista** (rosto, pele, roupa). Problemas que tivemos e como resolvemos:
  - O esqueleto automático da IA não tinha dedos e deformava mal (bermuda virava "saia") → trocamos pelo esqueleto do MakeHuman e o Blender calcula os pesos pelo corpo da IA (resolvido).
  - Malha de IA vem em "retalhos" soltos → costurar antes de calcular os pesos (resolvido).
- **Ainda falta:** cabelo vira um bloco liso; o corpo vem numa peça só (não dá para trocar a cor da camisa por time sem separar a roupa); mão sem dedos no modelo original (o esqueleto novo tem, mas a malha da mão é simples); custo de créditos (~14 por personagem completo).
- **Dificuldade:** gerar uma turma variada (crianças, adolescentes, adultos, homens e mulheres, peles diferentes) **realista, animável, com a roupa separada e cabelo bom**, de forma barata e repetível.

### 3. Cenário e luz
- As casas são caixas geradas por código com textura de foto; jogos grandes usam objetos modelados à mão (janela com batente, reboco descascado, fios, grades). Já usamos alguns objetos prontos (Poly Haven, CC0) e textura de foto, mas o conjunto ainda parece "maquete".
- A luz da Unity URP é simples; Unreal 5 (Lumen) daria um salto, mas exige placa de vídeo dedicada (o PC atual tem integrada).
- **Dificuldade:** gerar ruas brasileiras realistas (casas, comércio, favela, terrão, campinho) com IA ou pacotes prontos, leves o bastante para rodar em PC comum.

## Perguntas boas para pesquisar
1. Qual a melhor ferramenta (2026) de **vídeo → animação** para uso **comercial** em jogo, com uma câmera só, que exporte FBX para Unity Humanoid?
2. Dá para gerar por IA um **vídeo realista de alguém fazendo o movimento do bets** (guarda de golfe + giro lateral) e usar como fonte de animação?
3. Qual gerador **imagem → 3D** faz personagens **com roupa e cabelo em peças separadas** (para trocar cor do time) e com rig de dedos? (Tripo, Meshy, Rodin/Hyper3D, Hunyuan3D, Character Creator 5, outros)
4. Existe biblioteca de **animações de esporte com taco** (críquete, beisebol, golfe, hóquei) com licença comercial barata (Mixamo, ActorCore, Rokoko, Unity Asset Store/Fab)?
5. Qual o caminho mais realista para um **time de 1 pessoa + IA**: Unity URP/HDRP com ativos de IA, ou Unreal 5 com MetaHuman (e qual PC mínimo)?
6. Como outros devs indie fazem **personagens realistas com IA** e resolvem cabelo e roupa separada?

## Termos para busca
`video to motion capture commercial license`, `markerless mocap single camera FBX Unity`, `AI image to 3D rigged character separate clothing`, `Tripo / Meshy character hair cards`, `cricket batting animation pack`, `golf swing mocap animation pack`, `Unity humanoid retarget AI generated character`, `MetaHuman child characters`, `Character Creator 5 game ready export Unity`.
