# Notas para o Claude Code: comandar o Unity pelo CLI

Aprendido nas Etapas 0 a 6 do protótipo (no Mac). Leia antes de mexer no editor por linha de comando, em qualquer computador.

## Instalar e conectar (máquina nova)

1. Unity Hub + editor **6000.6.6f1** (a mesma versão do projeto: ver `Game/ProjectSettings/ProjectVersion.txt`). Nunca abrir com outra versão: o Unity converte o projeto e dá conflito entre as máquinas.
2. Unity CLI:
   - Windows (PowerShell): `irm https://unity.com/install.ps1 | iex`
   - macOS / Linux: `curl -fsSL https://unity.com/install.sh | bash`
   - Se o comando `unity` não for encontrado no shell do Claude Code, ele fica em `~/.unity/bin` (Mac) — no Mac foi preciso `export PATH="$HOME/.unity/bin:$PATH"`. No Windows, abrir um terminal novo depois de instalar.
3. Skill do CLI para o Claude Code: `unity skill install claude-code`. As skills do projeto já vêm no repositório em `.claude/skills` (unity-cli e unity-pipeline).
4. Abrir o projeto `Game/` no Unity e conferir: `unity status` precisa mostrar o projeto com estado `ready`. O pacote que conecta o editor (`com.unity.pipeline`) já está no `Game/Packages/manifest.json`.
5. A primeira abertura reconstrói a pasta `Library` (não vai para o Git) e demora alguns minutos.

## Pegadinhas

- **Nunca rodar pelo `eval` algo que abra janela de diálogo** no Unity. Ex.: `EditorSceneManager.SaveScene` numa cena "Untitled" abre "Save Scene". Um diálogo trava a thread principal e todo comando passa a dar timeout; só resolve alguém clicar na janela (Cancel).
- **Testar no Play mode com o editor sem foco:** antes de tudo `unity command set_autotick`; já no Play, `unity command eval 'UnityEngine.Application.runInBackground = true; return 1;'`. Sem isso o jogo fica parado no frame 1. Voltar para `false` antes de `editor_stop`. (O comando `editor_set_run_in_background` não existe no pacote pipeline 0.8.0-exp.1.)
- Com o editor sem foco, o jogo roda devagar (~10 quadros/s) e o tempo da batida fica impreciso. Para medir tempo direito: `UnityEngine.Time.captureDeltaTime = 1f/60f` (o tempo do jogo anda 1/60 s por quadro) e voltar para `0f` no fim.
- Teclado simulado por `InputSystem.QueueStateEvent`: `isPressed` funciona com o editor sem foco, mas `wasPressedThisFrame` não (o evento cai fora do quadro do jogo). Para testar "apertou agora", chamar o método direto por reflexão.
- Controle virtual para teste: `InputSystem.AddDevice<Gamepad>("ControleVirtualTeste")` e `QueueStateEvent(g, new GamepadState{...})`; remover no fim com `InputSystem.RemoveDevice`.
- `capture_game_view --save_path X` grava em `Game/Assets/X` (não aceita caminho absoluto). Usar `Temp/...` e apagar `Game/Assets/Temp` e `Temp.meta` depois, para não ir para o Git.
- Depois de `unity recompile`, esperar `unity status --until-ready` antes do próximo `eval` (a recarga dos scripts derruba a conexão por alguns segundos).
- Logo depois de criar um asset (ScriptableObject) no mesmo `eval`, `EditorSceneManager.NewScene` pode descarregá-lo da memória e as referências ficam vazias: criar a cena primeiro e carregar os assets depois.

## Cena e configs

- O menu **Bets > Montar cena Prototipo** (`BetsDeRua.Editor.MontadorCena.Montar()`) recria a cena inteira a partir dos configs. Se a cena der conflito no Git, é só montar de novo.
- Todos os números ficam em `Game/Assets/Config`: `ConfigFisica`, `ConfigPartida`, `ConfigCamera`, `ConfigHUD`. Mudanças feitas com o jogo rodando ficam salvas (são assets).
- **Modo assistir** (os 4 jogadores bots): `ConfigPartida → soAssistir`, ou **F2** com o jogo rodando.

## Trabalhando em dois computadores

Antes de trocar de computador: commit e push. Ao chegar no outro: pull antes de abrir o Unity. Nunca mexer na mesma cena nos dois ao mesmo tempo.

## Aprendido na versão 0.2 (menus, rua, build)

- **Montar cena demora mais que 5 s** (a rua gera texturas e malhas): o `eval` responde "Main thread operation timed out after 5000ms", mas o trabalho **continua** no editor. Esperar o arquivo `.unity` mudar de data (ou `unity status --until-ready`) antes do próximo comando.
- **Autotick cai depois de recompilar**: rodar `unity command set_autotick` antes de cada `editor_play` em teste automático; sem isso o jogo fica parado no tempo 0.
- **Recompilar com o Play ligado não compila** (ScriptCompilationDuringPlay=1): dar `editor_stop` antes do `unity recompile`, senão dá tempo esgotado.
- **Partida aberta pelo menu** (SessaoDeJogo) muda `ConfigPartida` (pontos, dificuldade, assistir) e devolve os valores ao sair do Play. Para simulação de bots, ajustar o config **pelo editor** (eval com `AssetDatabase.LoadAssetAtPath` + `SaveAssets`), não com `sed` no arquivo enquanto o Play está ligado.
- **Testar o menu sem controle**: chamar os métodos direto pelo eval, ex.: `FindFirstObjectByType<BetsDeRua.MenuPrincipal>().Abrir(new BetsDeRua.TelaPersonagem(0))` ou `ComecarPartida(...)`.
- **Prints**: `ScreenCapture.CaptureScreenshot("caminho absoluto")` pelo eval funciona no Play. Fora do Play, a ferramenta `BetsDeRua.Editor.FotoBoneco.Foto(caminho)` fotografa os 4 personagens num estúdio.
- **Build de Windows**: `unity command build --target StandaloneWindows64 --outputPath "Builds/BetsDeRua/BetsDeRua.exe" --confirm true` e esperar `build_status` = completed (ou menu Bets > Gerar build Windows). O log do jogo instalado fica onde for passado em `-logFile`.
- Avisos de "GaussianDepthOfField / BokehDepthOfField / PaniniProjection stripped" no jogo instalado são normais: o jogo não usa esses efeitos.
