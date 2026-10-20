# Marca BETS DE RUA — logo

Logo da direção A do GDD ("Giz no asfalto"): **BETS** grande em letra de giz, o taco em pé fazendo o ponto de exclamação com a bolinha amarela, traço laranja sublinhando e **DE RUA** menor embaixo.

Isto é a **base em vetor** para o artista refinar antes do registro no INPI (ver GDD, "Marca e identidade visual"). Pode usar já em devlog, redes, Discord e tela de título do protótipo.

## Arquivos (`logo/`)

| Arquivo | Quando usar |
| --- | --- |
| `logo-principal.svg` | Padrão: fundo asfalto, com textura de giz |
| `logo-principal-limpo.svg` | Mesmo logo sem textura: tamanhos pequenos, bordado, impressão barata |
| `logo-transparente.svg` | Por cima de vídeo, foto ou tela escura do jogo |
| `logo-invertido.svg` | Fundo claro (papel, camiseta branca) |
| `logo-mono-asfalto.svg` / `logo-mono-giz.svg` | Uma cor só: carimbo, kit físico, gravação no taco |
| `icone.svg` | Avatar das redes, ícone da Steam, capa do Discord |
| `icone-limpo.svg` | Favicon e ícone do executável (16 a 64 px) |

PNGs prontos em `logo/png/` (o número é a largura em pixels).

## Regras rápidas

- **Respiro:** deixe em volta do logo pelo menos a altura do "DE RUA" livre de outros elementos.
- **Tamanho mínimo:** logo completo com 160 px de largura; abaixo disso, use o ícone.
- **Cores:** só as da paleta do GDD (asfalto `#2B2D31`, giz `#F4F1E8`, laranja `#FF7A1A`, amarelo `#E8E337`).
- **Não fazer:** esticar ou achatar, trocar a fonte, girar, pôr sombra ou brilho, separar o taco do BETS, usar sobre fundo poluído sem a versão transparente, e nunca junto de palavras de aposta ("aposte", "odds", "ganhe").

## Como gerar de novo

Tudo sai do script `gerar_logo.py` (letras viram contorno, então não precisa ter a fonte instalada). Da raiz do repositório:

```bash
uv run --no-project --with fonttools --with resvg-py python docs/marca/gerar_logo.py
```

Fonte base: Permanent Marker, licença Apache 2.0 (`fontes/LICENSE.txt`), livre para uso comercial.

## Pendências

- O ícone do jogo hoje (`Game/Assets/UI/Icone.png`) usa tacos cruzados com garrafa, que é o conceito 2 do GDD. Trocar pelo `logo/png/icone-512.png` quando a outra frente do jogo puder.
- Retrabalho humano em vetor antes de registrar a marca.
