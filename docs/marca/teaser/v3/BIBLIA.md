# Teaser v3 — bíblia visual

Por que existe: na v2 cada imagem foi descrita solta, e a IA inventou onde fica a garrafa, para que lado o rebatedor olha e como cai a luz. Esta bíblia fixa o mundo do teaser com base nas regras do GDD e no que o jogo já faz (levantado no código em 20/10/2026). **Todo plano** é gerado com as duas referências fixas (rua e elenco) e segue as regras abaixo.

## O campo (igual ao jogo)

```
  lado das casas (fundo, +Z): calçada, meio-fio com crianças agachadas, muros, portões, postes e fios
  ────────────────────────────────────────────────────────────────────────────────────────────
     guardião A              BASE A                 chinelo                BASE B              guardião B
     (4 m atrás)   [■ verde] [ área do taco ]   ...... meio ......   [ área do taco ] [■ cola]   (4 m atrás)
                   garrafa    2,6 x 1,4 m              (20 m entre as garrafas)          garrafa
  ────────────────────────────────────────────────────────────────────────────────────────────
  lado da câmera (frente, -Z): muro baixo de 80 cm
  O sol de fim de tarde fica na ponta ESQUERDA da rua (atrás da base A).
```

- **Base:** quadradinho de giz de 35 cm com a garrafa no centro + retângulo de giz de 2,6 m (atravessando a rua) por 1,4 m (área do taco) encostado nele, do lado de onde vem o arremesso.
- **Garrafa A:** PET 2 L verde, guaraná (líquido âmbar até um terço), rótulo verde com onda branca, sem marca, tampa verde.
- **Garrafa B:** PET 2 L transparente, cola (líquido escuro até um terço), rótulo vermelho com onda branca, sem marca, tampa vermelha.
- **Taco:** 1 m, madeira clara. Cabo redondo (30% do comprimento) com pomo e três faixas de fita isolante preta, ombro curvo que abre numa pá chata e larga de ponta reta, com um friso no meio.
- **Bola:** de tênis, amarelo-esverdeada.
- **Meio:** um chinelo no asfalto. A rua tem faixa amarela tracejada no centro.

## As posições no lance (igual ao jogo)

- **Rebatedor:** de lado (como no críquete), em pé ao lado da área do taco, com a ponta do taco encostada no chão **dentro** da área. A garrafa dele fica **atrás e ao lado** dele, nunca na frente. Ele olha por cima do ombro para quem arremessa.
- **Parceiro do rebatedor:** na outra base, na mesma postura, com o taco no chão na área dele.
- **Arremessador:** 1,2 m atrás da garrafa da base do parceiro, olhando para a garrafa alvo do outro lado.
- **Guardião:** 4 m atrás da garrafa alvo (atrás do rebatedor), agachado como goleiro.
- **Batida:** sempre para a frente (de volta na direção de quem arremessou), abrindo no máximo 60° para os lados. Nunca para trás.
- **Corrida:** depois da batida, os dois rebatedores correm um na direção do outro, cada um pelo seu lado da rua. No meio (no chinelo), batem os tacos em **X no alto** ("tec!") sem parar de correr, e cada um encosta a ponta do taco na área do taco do outro lado.
- **Queimada:** a bola acerta a garrafa e ela cai e rola.

## O lance que o teaser mostra (direção de tela fixa)

Na câmera de transmissão: **base A à esquerda, base B à direita**.
- **Duda** (laranja) arremessa de trás da garrafa A (esquerda), **da esquerda para a direita**.
- **Davi** (azul) rebate na base B (direita), e **Bia** (azul) espera na base A (esquerda), ao lado da Duda.
- **Caio** (laranja) é o guardião, 4 m atrás da garrafa B (mais à direita).
- Davi rebate **de volta para a esquerda**, subindo. Depois Davi corre para a esquerda e Bia para a direita.

Câmeras atrás do arremessador olham para a direita da rua (o sol fica às costas, iluminando o Davi de frente). Câmeras atrás do rebatedor olham para a esquerda (o sol aparece no quadro, em contraluz atrás da Duda).

## Elenco (referência: ficha frente e costas)

As cores seguem os times do jogo.
- **Dupla azul (os irmãos):**
  - **Davi**, 10 anos, cabelo cacheado curto: camiseta azul-céu, bermuda jeans, tênis branco.
  - **Bia**, 12 anos, tranças longas: camiseta azul-royal, short branco com friso azul, chinelo.
- **Dupla laranja (a turma):**
  - **Caio**, 11 anos, boné vermelho para trás, joelho ralado: regata laranja, short preto, chinelo.
  - **Duda**, 11 anos, rabo de cavalo alto: camiseta laranja de gola preta, short preto, tênis preto.
- **Turma no meio-fio:** 6 a 8 crianças de todas as cores de pele, com roupas lisas **verdes, brancas, vermelhas, amarelas, roxas ou cinzas — nunca azul nem laranja** (essas cores são só das duas duplas; senão parecem cópias dos irmãos).
- Nenhuma roupa tem marca, número ou texto.

## Luz e tempo

- Atos 1 a 5: o mesmo fim de tarde, com o sol baixo à esquerda da rua. A luz vai ficando mais laranja até a cruzada.
- Ato 6: noite com um poste de luz de sódio, e depois a sala de estar.

## Câmeras do teaser (as mesmas do jogo, mais as de cinema)

| Código | Câmera | Uso |
| --- | --- | --- |
| C1 | Transmissão: alta, do lado do muro baixo, rua da esquerda para a direita | Visão do jogo inteiro |
| C2 | Atrás do arremessador, por cima do ombro | Arremesso |
| C3 | Atrás da garrafa do rebatedor, olhando para o arremessador | Bola vindo |
| C4 | Close de comemoração, 85 mm, fundo desfocado | Reações |
| C5 | Baixa no asfalto, perto da garrafa | Queimada e detalhes |
| C6 | Drone e dentro do carro | Chegada |

## Montagem

- **Sem texto na tela** até o cartão final. As falas viram ação: quem chama, chama com o gesto.
- **Som:** música de fundo + som ambiente de rua (crianças, passarinhos, um cachorro longe) + os sons do jogo (estalo da batida, "tec!" da cruzada, garrafa PET caindo).
- **Cortes:** corte seco, como as câmeras do jogo. Mergulho no preto só na virada de ato.
- **Câmera lenta:** só onde o jogo usa (batida, queimada) e na cruzada, a 0,3×.
- **Continuidade:** cada vídeo é gerado com quadro inicial e quadro final, os dois desenhados com as referências. Assim o movimento entre eles é o da regra, não o que a IA inventar.
