# Revisão de realismo dos vídeos da v3 (20/10/2026)

Método: medição automática de saltos quadro a quadro (`saltos.py`) + revisão visual a 12 quadros/s de cada trecho usado no teaser rápido, olhando física (gravidade, contato), forma dos objetos e continuidade.

## Cortes escondidos dentro do clipe (o Kling troca de cena no meio)

05 (1,75 s), 09 (2,25 s), 12 (2,92 s), 14 (2,58 s), 15 (2,79 s), 16 (2,38 s), 17 (4,95 s), 20 (3,25 s).

## Física e comportamento

| Plano | Momento | Resultado | O que acontece de errado |
| --- | --- | --- | --- |
| 18 | Batida | Reprovado | O taco passa ao lado da bola sem tocar; a bola fica parada no ar ao lado do taco, com um rastro laranja que não existe |
| 07 | Queimada | Reprovado | A bola gruda na garrafa; a garrafa sobe e flutua inclinada, sem cair |
| 20 | Cruzada | Reprovado depois de 0,5 s | Os irmãos se trombam e se abraçam em vez de passar um pelo outro; os tacos não batem em X |
| 16 | Arremesso | Reprovado | A Duda nunca solta a bola: ela gira o corpo com a bola na mão |
| 13 | Giz | Reprovado | A linha de giz aparece à frente da mão, não onde o giz encosta |
| 19 | Corrida | Duvidoso | O Davi corre a partir da base errada (a regra do teaser diz da direita para a esquerda) |
| 17 | Bola vindo | Aceitável | A bola vem em arco e cresce ao chegar perto da câmera |
| 22 | Abraço | Aprovado | Só movimento de pessoas, sem contato com objetos |

## Conclusão

Os modelos de vídeo atuais erram justamente no que é o coração do bets: **contato** (taco na bola, bola na garrafa, taco no taco) e **gravidade** de objetos pequenos. Em cenas só de pessoas (rosto, abraço, olhar, andar) eles vão bem. Isso não se resolve escrevendo prompts melhores; é um limite dos modelos.
