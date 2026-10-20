# Capturas de movimento da CMU → Unity

Animações gravadas de pessoas de verdade (CMU Graphics Lab Motion Capture Database, uso livre: <http://mocap.cs.cmu.edu/>).
O jogo usa o sujeito 124: `124_07` (Baseball Swing → batida) e `124_01` (Baseball Pitch → arremesso, ainda não ligado).

## Passo a passo (exemplo da rebatida)

1. Baixar o esqueleto e o movimento: `http://mocap.cs.cmu.edu/subjects/124/124.asf` e `.../124_07.amc`.
2. Converter para BVH, cortando os quadros (120 por segundo) e já com as pernas retas na pose de repouso (o Unity espera pose T):
   `uv run --with numpy --with scipy python amc2bvh.py 124.asf 124_07.amc Rebatida_CMU.bvh 230 470`
3. Conferir o movimento desenhando um boneco de palito (frente e lado):
   `uv run --with numpy --with scipy --with matplotlib python palito.py Rebatida_CMU.bvh palito.png 0,60,95,110,130`
4. Gerar o FBX no Blender (sem abrir a janela):
   `"C:/Program Files/Blender Foundation/Blender 5.2/blender.exe" -b --python bvh2fbx.py -- C:/caminho/Rebatida_CMU.bvh C:/caminho/Rebatida_CMU.fbx Rebatida_CMU`
5. Copiar o FBX para `Game/Assets/Personagens/Animacoes/CMU/` e rodar no Unity **Bets > Importar capturas de movimento (CMU)**
   (mapa dos ossos e cortes dos clipes em `ImportadorCapturas.cs`) e depois **Bets > Preparar corpos de verdade** (controlador de animação).

`folha_quadros.py` junta quadros gravados pelo `GravadorQuadros` numa tira, para comparar antes e depois.
