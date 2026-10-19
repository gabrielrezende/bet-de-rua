# Primeiro pedido para o Claude Code

Antes de colar: crie o projeto no Unity Hub (Universal 3D, **local** `~/Projetos/bet-de-rua`, **nome** `Game`), deixe o Unity aberto com o projeto, abra o Terminal e rode:

```bash
cd ~/Projetos/bet-de-rua
claude
```

Depois copie e cole tudo abaixo:

---

Leia o `CLAUDE.md` e as seções "Controles", "Dinâmica do lance: campo, física e gravidade" e "Protótipo cinza — guia de início" do `docs/GDD.md`. Vamos começar o protótipo cinza do Bets de Rua. Eu não sou dev de jogos: explique em português o que fez ao fim de cada etapa e pare para eu testar com o controle.

**Etapa 0 — Ambiente**
- Confira se o projeto Unity está em `Game/`, com Unity 6 e URP.
- O repositório Git já existe na raiz (`~/Projetos/bet-de-rua`, ramo main, com remoto). Crie um `.gitignore` de Unity que cubra `Game/Library`, `Game/Temp`, `Game/Logs`, `Game/obj`, `Game/Build`, `Game/Builds` e `Game/UserSettings`, atualize o README com uma descrição curta do jogo e faça o primeiro commit com o projeto Unity, o CLAUDE.md e a pasta docs.
- O Unity CLI (`unity`) já está instalado no Mac. Configure a conexão do Claude Code com o Unity Editor por ele: rode `unity skill show` para ler o guia do CLI, use `unity setup` / `unity skill install` para o Claude Code e confira com `unity status` que o editor com o projeto `Game` aparece conectado. Se precisar que eu clique em algo no editor ou no Unity Hub, me diga o passo exato.
- Instale o pacote Input System (se o Unity pedir para reiniciar, me avise).
- Teste a conexão criando um cubo na cena e me peça para confirmar que ele apareceu. Depois apague o cubo.

**Etapa 1 — Cena e campo**
- Cena "Prototipo": chão de 50 m x 14 m (cinza escuro), muros baixos nas bordas.
- Duas bases (círculos brancos de 1,2 m de diâmetro) a 20 m uma da outra.
- Em cada base, uma garrafa (cilindro de 0,3 m) com Rigidbody, que só cai se atingida acima de 15 km/h.
- Faixa do meio de 3 m marcada no chão.
- Câmera lateral estilo transmissão de futebol, enquadrando o campo inteiro e seguindo a bola suavemente.

**Etapa 2 — Jogadores**
- 4 cápsulas: dupla do taco (azul), uma em cada base, com taco (cubo fino de 0,73 m); dupla da bola (laranja): arremessador numa base e guardião atrás da outra.
- 1 a 4 controles; um controle pode controlar a dupla inteira, trocando de jogador com L1 (LB). Teclado e mouse também funcionam.
- Andar 2 m/s, correr 5 m/s, sprint 7 m/s com R2 (RT). Marcador colorido em cima do jogador controlado.

**Etapa 3 — Arremesso**
- Leia antes as seções 1 e 9 do `docs/REFERENCIAS_FIFA.md`. A câmera continua a de transmissão lateral; no arremesso ela só aproxima um pouco do lado da casinha alvo.
- Mira com o analógico esquerdo: círculo amarelo em volta da garrafa e linha pontilhada no chão, visível só enquanto mira.
- Força num arco que enche segurando o botão, com faixa verde; passar da faixa erra o alvo.
- ○ (B) forte, segurar e soltar (55 a 90 km/h); X (A) quicado; △ (Y) pingo.
- Física própria da bola: gravidade 8,5 m/s², quique de 70% no asfalto, atrito ao rolar.
- Tudo no ScriptableObject `ConfigFisica`.

**Etapa 4 — Batida**
- Rebatedor parado no círculo = taco no chão automático, seguro. Ele pode andar ou correr para fora do círculo para alcançar bola torta; fora do círculo o taco sai do chão e a casinha fica exposta (o guardião pode queimar).
- Ajudas de tempo na vista lateral: sombra da bola sempre visível, marca de giz no chão onde a bola vai passar, raio de alcance do taco (~1,5 m) em volta do rebatedor e anel que encolhe na bola quando ela entra no raio.
- ○ (B) batida forte (segurar e soltar), X (A) toque, △ (Y) cavadinha; direção pelo analógico esquerdo.
- Janela de tempo: perfeito ±50 ms, bom ±120 ms. Mostrar na tela o atraso em ms após cada batida.

**Etapa 5 — Corrida, cruzada e defesa**
- R2 (RT) sai correndo (levanta o taco). Na faixa do meio, a até 1,5 m do parceiro, X (A) no anel = cruzada (+1 pendente). O ponto só vale ao parar dentro do círculo da outra base.
- Defesa: □ (X) pega a bola (no ar = ataque perde o taco); X (A) passa; ○ (B) chuta na garrafa. Garrafa derrubada com o taco fora do círculo = queimada, troca de papel.
- Bola na mão: máximo 4 m/s, sem sprint.

**Etapa 6 — Partida**
- Placar na tela; 10 pontos + cruzada final fecha o jogo. Defesa alterna as casinhas a cada lance. Start reinicia.

**Regras de trabalho**
- Uma etapa por vez. Ao fim de cada uma: explique, diga como testar e espere meu retorno.
- Commit ao fim de cada etapa que funcionar.

Comece pela Etapa 0.
