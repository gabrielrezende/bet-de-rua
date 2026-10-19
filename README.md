# Bets de Rua

Versão digital do **bets** (taco, bete, tacobol), o jogo de rua brasileiro. Duas duplas: uma com os tacos, outra com a bola. Cada cruzada dos tacos no meio vale 1 ponto; quem fecha 10 ganha.

PC (Steam) primeiro, controle recomendado, teclado e mouse suportados. Estilo FIFA: câmera de transmissão lateral, poucos botões, partidas curtas.

## Estado atual

**Versão de teste 0.2** (deixou de ser só o protótipo cinza): já parece um jogo.

- **Menu no estilo FIFA**: abertura "APERTE START", início com abas (L1/R1) e blocos: Jogar, Treino, Meu Jogador, Online e Ajustes.
- **Partida rápida** (você e um parceiro bot contra a CPU) e **multiplayer local** de 1 a 4 controles. Na escolha de lado, cada controle vai para o Lado 1 (começa no taco) ou para o Lado 2 (começa na bola), e vaga vazia vira bot.
- **Criador de personagem**: idade, altura, corpo, pele, cabelo, barba, boné, camisa, bermuda, tênis, acessórios, fita do taco e **estilo de jogo com barras** (velocidade, força, precisão, alcance, fôlego). Cada pessoa joga sempre com o seu personagem.
- **Treino de arremesso** com placar de acertos, sequência e velocidade.
- **HUD de transmissão** (placar, mensagens, dicas do momento), pausa e tela de fim de partida.
- **Rua de bairro** gerada por código: asfalto gasto, calçada, casas, sobrado de tijolo, comércio com placa, pichação, caixas d'água, postes com fios e o morro ao fundo.
- **Sons** sintetizados (taco, quique, garrafa, "TEC!" dos tacos, vinhetas, menu e clima de rua).
- O **online** (grupo, convites e amigos) tem as telas prontas; a conexão entra com a Steam.

### Jogar a versão de teste (sem abrir o Unity)

1. No editor, menu **Bets > Gerar build Windows** (ou peça ao Claude Code). Sai em `Game/Builds/BetsDeRua/`.
2. Copie a pasta `BetsDeRua` inteira (sem a subpasta `..._BackUpThisFolder_ButDontShipItWithYourGame`) para o PC do amigo, por pendrive ou num .zip.
3. Abra `BetsDeRua.exe`. Controle recomendado (PlayStation ou Xbox); teclado e mouse também funcionam.

### Jogar no editor

Abra `Game/` no Unity e aperte Play na cena `Assets/Scenes/Menu`. A cena `Prototipo` aberta direto continua funcionando do jeito antigo (tela de escolha do time; F2 = só assistir). Todos os números ficam em `Game/Assets/Config` (ConfigFisica, ConfigPartida, ConfigCamera, ConfigHUD). As cenas são montadas por código: menu **Bets > Montar cena Prototipo** e **Bets > Montar cena Menu**.

## Estrutura

- `Game/` — projeto Unity 6 (URP, Input System)
- `docs/GDD.md` — documento de design completo
- `CLAUDE.md` — contexto do projeto para o Claude Code

## Como abrir

Abra a pasta `Game/` no Unity Hub (versão do editor em `Game/ProjectSettings/ProjectVersion.txt`).
