# Bets de Rua

Versão digital do **bets** (taco, bete, tacobol), o jogo de rua brasileiro. Duas duplas: uma com os tacos, outra com a bola. Cada cruzada dos tacos no meio vale 1 ponto; quem fecha 10 ganha.

PC (Steam) primeiro, controle recomendado, teclado e mouse suportados. Estilo FIFA: câmera de transmissão lateral, poucos botões, partidas curtas.

## Estado atual

**Versão de teste 0.5**: pessoas realistas e rua com objetos de verdade.

- **Pessoas do MakeHuman (CC0)**: 14 pessoas, entre homens, mulheres, adolescentes e crianças, com tons de pele variados, pele e olhos de foto, cabelos (afro, curtos, rabo de cavalo, trança), camiseta, polo ou regata, bermuda jeans ou calça cargo e tênis. A camiseta vira a camisa do time. Geradas por `Game/Ferramentas/Blender/gerar_pessoas.py` (Blender + MPFB) e trazidas pelo menu **Bets > Importar pessoas MakeHuman**.
- **Pegada do bets**: as duas mãos no cabo, na frente do corpo, e a ponta do taco no chão (postura de golfista: tronco inclinado, joelhos dobrados). O boné se ajusta pelos olhos de cada pessoa.
- **Objetos da rua de verdade (Poly Haven, CC0)**: portas de aço de enrolar (algumas pichadas), ar-condicionado nas paredes, lixeiras de lata, pneu velho, tampas de bueiro e carros cobertos com lona.
- **Rastro da bola** nas bolas rápidas (como o traço do golfe) e reflexo do céu nos metais.
- Quatro lugares novos no sorteio: favela, avenida, rodovia e pontilhão.

**Versão de teste 0.4**: lugares sorteados, imagem mais realista e jogo mais justo.

- **Quatro lugares, sorteados a cada partida**: Rua do Bar do Zé (asfalto e meio-fio), Terrão da Vila (terra que quica torto), Campinho do Bairro (grama com alambrado) e Fazenda do Tio (pasto, cerca de madeira, vacas e celeiro). Cada chão tem o seu quique e atrito (`ConfigMapas`).
- **Imagem mais real** (referência: teaser do FC 26): texturas de foto (Poly Haven, CC0) no asfalto, calçada, tijolo, reboco, terra e grama; céu de foto de fim de tarde; morro com casinhas de tijolo, laje, janela e mato; grão de filme, borrão de movimento leve e fundo desfocado nos closes e na câmera lenta; poeira quando a bola quica.
- **Garrafa PET de 2 litros** com líquido até um pouco acima da metade e rótulo inventado (cola e guaraná).
- **Taco no estilo da marca** (pá larga e cabo com fita), as **duas mãos no taco**, ninguém mais corre de costas, o rebatedor parado olha para a bola, área do taco maior.
- **Torcida maior**, em rodinhas, com crianças na beira do campo; todo mundo levanta nos lances grandes.
- **Queimadas sem querer corrigidas**: a bola do arremesso não derruba mais a garrafa de quem arremessou.

Da versão 0.3:

- **Personagens com corpo e animação de captura** (Quaternius, CC0, ver `docs/CREDITOS.md`): andar, correr, arremessar, bater, pegar, deslizar, comemorar; as duas mãos seguram o taco e a cabeça acompanha a bola. Roupas (camisa do time, bermuda, tênis) geradas a partir do corpo.
- **Câmeras como no MLB The Show**: atrás de quem arremessa antes do lance, atrás da casinha quando você rebate, transmissão depois da batida, close em quem comemora e **câmera lenta** na queimada, sempre com corte seco.
- **Apresentação**: cartão "NO TACO / ARREMESSA", narração do "moleque do muro" em legenda, anel no chão e barra de fôlego de quem você controla, torcida na calçada.
- **Menu no estilo FC 26**: lista vertical de modos, monograma, tomada de cinema diferente para cada modo, fundo desfocado.

Da versão 0.2 (continua valendo):

- **Menu no estilo FIFA**: abertura "APERTE START", início com abas (L1/R1) e blocos: Jogar, Treino, Meu Jogador, Online e Ajustes.
- **Partida rápida** (você e um parceiro bot contra a CPU) e **multiplayer local** de 1 a 4 controles. Na escolha de lado, cada controle vai para o Lado 1 (começa no taco) ou para o Lado 2 (começa na bola), e vaga vazia vira bot.
- **Criador de personagem**: idade, altura, corpo, pele, cabelo, barba, boné, camisa, bermuda, tênis, acessórios, fita do taco e **estilo de jogo com barras** (velocidade, força, precisão, alcance, fôlego). Cada pessoa joga sempre com o seu personagem.
- **Treino de arremesso** com placar de acertos, sequência e velocidade.
- **HUD de transmissão** (placar, mensagens, dicas do momento), pausa e tela de fim de partida.
- **Rua de bairro** gerada por código: asfalto gasto, calçada, casas, sobrado de tijolo, comércio com placa, pichação, caixas d'água, postes com fios e o morro ao fundo.
- **Sons** sintetizados (taco, quique, garrafa, "TEC!" dos tacos, vinhetas, menu e clima de rua).
- O **online** (grupo, convites e amigos) tem as telas prontas; a conexão entra com a Steam.

### Jogar a versão de teste (sem abrir o Unity)

1. No editor, menu **Bets > Gerar build Windows** (ou peça ao Claude Code). Sai em `Game/Builds/BetsDeRua/` (e há um .zip pronto em `Game/Builds/`).
2. Copie a pasta `BetsDeRua` inteira (sem a subpasta `..._BackUpThisFolder_ButDontShipItWithYourGame`) para o PC do amigo, por pendrive ou num .zip.
3. Abra `BetsDeRua.exe`. Controle recomendado (PlayStation ou Xbox); teclado e mouse também funcionam.

### Jogar no editor

Abra `Game/` no Unity e aperte Play na cena `Assets/Scenes/Menu`. A cena `Prototipo` aberta direto continua funcionando do jeito antigo (tela de escolha do time; F2 = só assistir). Todos os números ficam em `Game/Assets/Config` (ConfigFisica, ConfigPartida, ConfigCamera, ConfigHUD, ConfigMapas). As cenas são montadas por código: menu **Bets > Montar cena Prototipo** e **Bets > Montar cena Menu**.

## Estrutura

- `Game/` — projeto Unity 6 (URP, Input System)
- `docs/GDD.md` — documento de design completo
- `CLAUDE.md` — contexto do projeto para o Claude Code

## Como abrir

Abra a pasta `Game/` no Unity Hub (versão do editor em `Game/ProjectSettings/ProjectVersion.txt`).
