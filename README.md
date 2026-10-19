# Bets de Rua

Versão digital do **bets** (taco, bete, tacobol), o jogo de rua brasileiro. Duas duplas: uma com os tacos, outra com a bola. Cada cruzada dos tacos no meio vale 1 ponto; quem fecha 10 ganha.

PC (Steam) primeiro, controle recomendado, teclado e mouse suportados. Estilo FIFA: câmera de transmissão lateral, poucos botões, partidas curtas.

## Estado atual

**Protótipo cinza**: formas simples para descobrir se o lance é divertido no controle. Sem arte, sem menus, sem online.

As 6 etapas do protótipo estão prontas: campo, jogadores, arremesso, batida, corrida/cruzada/defesa e partida (placar até 10 + cruzada final). Há bots para jogar contra a CPU e um modo assistir (F2) com os 4 jogadores bots.

Para jogar: abra `Game/` no Unity, cena `Assets/Scenes/Prototipo`, e aperte Play. Controles no GDD (seção "Controles", com as decisões do protótipo marcadas). Todos os números ficam em `Game/Assets/Config` (ConfigFisica, ConfigPartida, ConfigCamera, ConfigHUD), ajustáveis com o jogo rodando.

## Estrutura

- `Game/` — projeto Unity 6 (URP, Input System)
- `docs/GDD.md` — documento de design completo
- `CLAUDE.md` — contexto do projeto para o Claude Code

## Como abrir

Abra a pasta `Game/` no Unity Hub (versão do editor em `Game/ProjectSettings/ProjectVersion.txt`).
