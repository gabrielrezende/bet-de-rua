# Teaser "A rua nova" — versão 1

Teaser de 83 s feito com IA, no estilo cinema realista, contando a história do GDD: a família chega na rua nova, os irmãos veem o bets, a turma chama ("Falta dois. Bora?"), eles aprendem, fazem a primeira cruzada e, anos depois, jogam o Bets de Rua no sofá. Fecha com o logo, "A rua voltou." e "Lista de desejos na Steam".

- **Vídeo final:** `teaser_a_rua_nova.mp4` (1920x1080, 24 fps, 60 MB, fica só no seu computador — vídeo grande não vai para o git).
- **Roteiro e análise do teaser do FC 26:** [ROTEIRO.md](ROTEIRO.md)
- **Storyboard (todos os quadros):** [storyboard.jpg](storyboard.jpg)

## Como foi feito

1. **Elenco fixo:** uma imagem com as 4 crianças (Davi, Bia, Caio, Duda) gerada no Higgsfield (GPT Image 2.5) e usada como referência em todos os quadros, para os rostos e as roupas não mudarem.
2. **Quadros-chave:** um por plano (24), em 2K. Os que saíram errados (criança trocada, taco esquisito) foram refeitos.
3. **Vídeo:** cada quadro animado no Kling 3.0 (modo pro, 5 s, com som de rua gerado junto).
4. **Falas:** viraram letreiros de giz na tela, como os balões de comentário do teaser do FC 26. Não há voz infantil em português no Higgsfield, e voz adulta em boca de criança soaria falsa.
5. **Trilha:** violão de fim de tarde e percussão de rua sintetizados por código (`trilha.py`), com o "tec!" de madeira no começo, na cruzada e no logo. Nada de música de terceiros, então não há problema de direitos.
6. **Montagem:** `montar.py` corta, aplica câmera lenta, põe os letreiros, junta tudo e mistura o som.

Custo: cerca de 400 créditos do Higgsfield (de 946 para 545), contando as refações; quadros ~0,25 cada, vídeos ~12,5 cada.

## Como refazer ou ajustar

Os vídeos brutos ficam em `brutos/` e os quadros em `quadros/` (fora do git; os links para baixar de novo estão em `urls_higgsfield.txt`). Para mudar o corte de um plano, a duração, um letreiro ou a câmera lenta, edite a lista `MONTAGEM` no começo de `montar.py` e rode, da raiz do repositório (precisa de um ffmpeg; o `imageio-ffmpeg` do pip traz um):

```bash
uv run --no-project --with pillow --with numpy python docs/marca/teaser/montar.py CAMINHO_DO_FFMPEG
```

## Pontos para revisar

- A trilha é sintetizada: serve para o corte, mas a versão de lançamento merece uma trilha de verdade (compositor ou banco de músicas livre de direitos).
- As falas podem ganhar voz de crianças de verdade (gravar com os sobrinhos/amigos no celular resolve e fica mais autêntico).
- Faltam o corte de 30 s para anúncio e os cortes verticais de 15 s para TikTok e Reels.
- A Steam pede que o uso de IA seja declarado na página da loja.
