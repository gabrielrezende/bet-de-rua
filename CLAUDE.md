# Bets de Rua — contexto do projeto para o Claude Code

## O que é
Versão digital do **bets** (taco, bete, tacobol), o jogo de rua brasileiro: duas duplas, uma com os tacos e outra com a bola. Cada cruzada dos tacos no meio vale 1 ponto; quem fecha 10 ganha. PC (Steam) primeiro, controle recomendado, teclado e mouse suportados. Estilo FIFA: câmera de transmissão lateral, poucos botões, partidas curtas, Modo Temporada.

O documento completo de design está em `docs/GDD.md`. Leia a seção relevante antes de implementar qualquer sistema. As seções mais usadas no protótipo:
- "Controles" — esquema de botões (PlayStation, com Xbox entre parênteses)
- "Dinâmica do lance: campo, física e gravidade" — números de campo, gravidade, quique, velocidades e o lance passo a passo
- "Regras adaptadas para o digital" e "Rodízio / Fim da partida"
- "Câmeras: a visão de cada jogador" — câmera de transmissão
- "Protótipo cinza — guia de início" — o escopo das 6 etapas

## Quem está construindo
Gabriel não é dev de jogos (é engenheiro de dados: Python, SQL, Spark). Por isso:
- Explique em português, em linguagem simples, o que fez e por quê, ao fim de cada etapa.
- Quando algo precisar ser feito à mão no editor do Unity, diga exatamente onde clicar.
- Pare ao fim de cada etapa para ele testar com o controle. Não avance sozinho.

## Escopo atual: protótipo cinza (MVP de diversão)
Objetivo único: descobrir se o lance é divertido no controle, jogando no sofá. **Sem arte, sem menus, sem online, sem som obrigatório.** Formas cinzas e cápsulas. Tudo o que estiver no GDD além disso (temporada, loja, skins, online, marca) fica para depois — não implemente sem ele pedir.

## Stack
- Unity 6 LTS, URP (modelo Universal 3D), C#
- Input System (novo) com suporte a 1 a 4 controles e teclado/mouse
- Física da bola **própria e simples** (trajetória, quique por superfície, efeito), não o Rigidbody da bola. A garrafa pode usar Rigidbody.
- Git desde o primeiro commit, com `.gitignore` de Unity
- Editor Unity **6000.6.6f1** (mesma versão em todos os computadores). O Claude Code comanda o editor pelo Unity CLI: leia `docs/NOTAS_UNITY_CLI.md` antes (instalação, pegadinhas, como testar no Play mode).

## Regras de código
- Pastas: `Assets/Scripts/Jogadores`, `Assets/Scripts/Bola`, `Assets/Scripts/Partida`, `Assets/Scripts/Camera`, `Assets/Scripts/Config`.
- Comentários e nomes de variáveis em português.
- **Todos os números de jogo** (gravidade, velocidades, janelas de tempo, distâncias, raios) num ScriptableObject `ConfigFisica` (e `ConfigPartida` para regras), ajustável com o jogo rodando. Nada de número mágico espalhado.
- Uma máquina de estados para o lance: Arremesso → BolaViva → BolaMorta → TrocaDePapel → FimDePartida.
- Commit ao fim de cada etapa que funcionar, com mensagem em português.

## Estrutura de pastas
```
~/Projetos/bet-de-rua/         ← raiz do repositório (rode o Claude Code aqui)
├── CLAUDE.md
├── README.md
├── docs/
│   ├── GDD.md                 ← documento completo de design
│   └── PROMPT_INICIAL.md      ← primeiro pedido
└── Game/                      ← projeto Unity (criado pelo Unity Hub)
```
