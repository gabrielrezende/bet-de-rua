# Créditos

## Personagens e animações

- **Universal Base Characters** (versão Standard) — [Quaternius](https://quaternius.itch.io/universal-base-characters). Licença CC0 1.0 (domínio público). Corpos masculino e feminino, cabelos, barba e sobrancelhas.
- **Universal Animation Library** e **Universal Animation Library 2** (versões Standard) — [Quaternius](https://quaternius.itch.io/universal-animation-library). Licença CC0 1.0. Animações de captura de movimento: parado, andar, trotar, sprint, arremesso por cima do ombro, golpe lateral (batida), pegar do chão, deslizar, comemorar, torcida (braços cruzados, conversando, no celular, sentado).

Os arquivos originais e as licenças estão em `Game/Assets/Personagens`. As roupas (camiseta, regata, manga longa, bermuda, calção, calça, meia, tênis) são geradas pelo jogo a partir desses corpos (`Editor/PreparadorCorpos`).

## Pessoas realistas (versão 0.5)

- **MakeHuman** ([makehumancommunity.org](https://static.makehumancommunity.org/assets/assetpacks.html)) pela extensão **MPFB 2** do Blender: corpos, peles, olhos, sobrancelhas, cílios, cabelos (afro, curtos, rabo de cavalo, trança) e roupas (camisetas, polo, regata, bermuda jeans, calça cargo, tênis). Pacotes usados, todos CC0: makehuman_system_assets, shirts01, pants01, eyebrows01, eyelashes01. As pessoas são geradas por `Game/Ferramentas/Blender/gerar_pessoas.py` e ficam em `Game/Assets/Personagens/MakeHuman`.

## Objetos da rua (versão 0.5)

- **Poly Haven**, licença CC0: portas de aço de enrolar (com e sem pichação), ar-condicionado, lixeira de lata, pneu velho, tampa de bueiro, carro coberto com lona, caixa de luz, poste, rádio, barreira de concreto, alambrado (`Game/Assets/Modelos/PolyHaven`, baixados por `Game/Ferramentas/baixar_polyhaven.py`, texturas reduzidas para 1024 px).

## Texturas e céu

- **Poly Haven** ([polyhaven.com](https://polyhaven.com)), licença CC0 1.0: asphalt_02, brushed_concrete, brick_wall_02, beige_wall_001, baseball_playground (terra), leafy_grass (grama) e o céu HDRI belfast_sunset_puresky (`Game/Assets/Texturas/Fotos`).

## Fontes

- Anton, Barlow, Barlow Condensed e Bangers — Google Fonts, licença SIL Open Font License 1.1 (`Game/Assets/UI/Fontes`).

## Feito pelo jogo (código)

Rua, terrão, campinho, fazenda (cerca, alambrado, vacas, celeiro), casas, morro, garrafa PET, poeira, texturas de reserva (asfalto, calçada, reboco, tijolo, portão, telha, pichação, giz), sons (taco, bola, garrafa, vinhetas, rua) e o ícone são gerados por código no próprio projeto.
