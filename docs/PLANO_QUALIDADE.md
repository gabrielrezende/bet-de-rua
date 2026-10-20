# Plano para chegar perto da qualidade de FIFA / NBA / MLB

Pesquisa de 20/10/2026. Objetivo: entender por que o jogo ainda parece de PS1/PS2 e qual o caminho mais curto para uma imagem e um movimento realistas.

## Por que ainda parece antigo

1. **Movimento** (o que mais denuncia): as animações são genéricas (espada, beisebol) e corrigidas por código. Ninguém gravou o movimento do bets.
2. **Cenário**: as casas são caixas com foto colada. Nos jogos grandes, cada objeto é modelado à mão por artista.
3. **Personagens**: MakeHuman serve para protótipo. Não tem rosto expressivo, cabelo bom nem roupa que se mexe.
4. **Luz**: a Unity no modo atual (URP) é a mais simples; a luz é o que mais dá sensação de "real".

O código (regras, física da bola, câmeras, bots, menus) está bom e não é o problema. O que falta é **arte e animação de verdade**, que não se faz só com programação.

## Um limite importante: o computador

O seu PC tem **placa de vídeo integrada (AMD Radeon, 6 GB compartilhados)**. A Unreal Engine 5 com luz Lumen e MetaHumans foi feita para placas dedicadas. No seu PC o editor vai ficar lento, e o jogo pode não rodar bem nos PCs dos amigos. Antes de trocar de motor, é preciso decidir **em que máquina vamos desenvolver** e **em que PC o jogo tem que rodar**.

## O que a pesquisa encontrou

| Peça | O que é | Custo | Observação |
| --- | --- | --- | --- |
| **Unreal Engine 5** | Motor com a melhor luz (Lumen) e geometria pesada (Nanite) | Grátis até US$ 1 milhão de faturamento; depois 5% | Precisa de placa de vídeo dedicada. Dá para o Claude comandar o editor (servidores MCP como o UEMCP, pela execução remota em Python) |
| **MetaHuman** | Pessoas fotorrealistas, com rosto e expressão | Grátis na Unreal | **Não faz criança oficialmente** (está no plano da Epic). Contorno: pacotes pagos de criança ou o Character Creator |
| **Character Creator 5 (Reallusion)** | Criador de personagens realistas, exporta para Unity, Unreal e MetaHuman | ~US$ 299 a 479, licença para sempre | Alternativa para fazer as crianças e a turma da rua |
| **Megascans / Fab** | Objetos e superfícies escaneados do mundo real | Desde 2025 **a maior parte é paga** | Ainda há pacotes grátis; os pacotes de favela/rua brasileira achados são "estilizados", não realistas |
| **Move.ai** | Captura de movimento com 2 a 6 iPhones, sem roupa especial | ~US$ 99/mês ou ~US$ 0,012 por segundo | Mais precisa; boa para movimento rápido de esporte |
| **Rokoko Vision** | Captura com 1 câmera (celular ou webcam) | Plano grátis com exportação FBX | Menos precisa em movimento rápido; ótima para testar |
| **Artista 3D freelancer (Brasil)** | Monta a rua realista e ajusta personagens | ~US$ 12 a 25/hora (Upwork); pedir orçamento por escopo | Workana, ArtStation, Behance, Fiverr |

## Três caminhos

### Caminho A — Ficar na Unity e subir o nível (mais barato, roda no seu PC)
- Gravar o **movimento real do bets** com celular (Rokoko grátis para testar, Move.ai para a versão final).
- Comprar **personagens realistas** (Character Creator 5) e **um pacote de rua** bom.
- Contratar **um artista 3D** por 3 a 4 semanas para a rua principal.
- Ajustar a luz (mudar para HDRP, que é a versão bonita da Unity) se a placa aguentar.
- **Resultado esperado:** um jogo bonito de nível indie atual, longe do PS2, mas não FIFA.
- **Custo estimado:** US$ 300 a 500 em ferramentas + o artista (≈ US$ 2.000 a 4.000 por 4 semanas).

### Caminho B — Trocar para a Unreal 5 (maior salto de imagem)
- Tudo do caminho A, mais MetaHumans para os adultos e luz Lumen.
- Eu reescrevo regras, física, câmeras e bots (umas semanas).
- **Precisa de um PC com placa dedicada** (ex.: RTX 4060 ou Radeon equivalente) para desenvolver.
- **Resultado esperado:** o mais perto de FIFA/NBA que um time pequeno consegue.
- **Custo estimado:** o do caminho A + o PC (se não tiver) + o tempo de reescrita.

### Caminho C — Estilo próprio caprichado (mais seguro, mais rápido)
- Em vez de imitar a foto, um visual estilizado bem feito (como Rocket League, Fortnite).
- Funciona bem em PC fraco e envelhece melhor. Ainda precisa do movimento gravado.
- Para o público adulto e nostálgico pode funcionar, mas foge do "realista" que você quer.

## Minha recomendação

1. **Primeiro, o movimento.** É o que mais incomoda e serve para qualquer caminho: um dia na rua com você e amigos jogando bets, filmado com 2 ou 3 celulares. Começo testando com o Rokoko grátis e, se a qualidade não bastar, partimos para o Move.ai. Eu ponho as animações no jogo.
2. **Uma fatia vertical**: um lugar só (a rua) e 4 personagens, com qualidade alta, em vez de 8 mapas.
3. **Decidir o motor** depois de responder: tem acesso a um PC com placa de vídeo dedicada? Se sim, **Caminho B**. Se não, **Caminho A**.
4. **Um artista 3D freelancer** para a rua, nos dois caminhos. Eu escrevo o pedido de orçamento (o que modelar, nível de detalhe, prazo).

## Fontes

- Unreal (royalty): [Meta Developers](https://developers.meta.com/horizon/documentation/unreal/unreal-oculus-license/), [Road to VR](https://roadtovr.com/unreal-engine-royalty-free-first-1-million-revenue/)
- MetaHuman e crianças: [MetaHuman 5.7](https://www.metahuman.com/news/metahuman-5-7-brings-major-improvements-to-body-conforming-with-more-to-come), [guia 5.6/5.7](https://medium.com/@Jamesroha/a-beginners-guide-to-metahumans-in-unreal-engine-5-6-and-5-7-e9b14fadbf3d), [fórum Epic: crianças](https://forums.unrealengine.com/t/athian-games-metahuman-child-characters/2701955)
- Megascans pago desde 2025: [80.lv](https://80.lv/articles/megascans-no-longer-free-after-2024/), [CG Channel](https://www.cgchannel.com/2025/05/get-260-free-3d-scanned-assets-for-creating-a-derelict-interior-in-ue5/)
- Pacotes de favela: [Fab](https://www.fab.com/listings/e77acbf0-596d-4a80-b470-7fb7ae092cff?lang=pt-br)
- Captura de movimento: [Studiovity](https://blog.studiovity.com/top-ai-mocap-alternatives-that-dont-require-a-suit-or-studio/), [Spybroski 2026](https://www.spybroski.com/blog/posts/best-markerless-motion-capture-tools-a-2026-comparison), [Neolemon](https://www.neolemon.com/blog/best-ai-motion-capture-tools-for-character-animation/)
- Character Creator 5: [CG Channel](https://www.cgchannel.com/2025/07/reallusion-unveils-character-creator-5/), [loja Reallusion](https://reallusion.com/store)
- Freelancers: [Upwork Brasil](https://www.upwork.com/hire/3d-modelers/br/), [CGHero](https://new.cghero.com/industry_roles/unreal-developer)
- Claude comandando a Unreal: [UEMCP](https://pypi.org/project/uemcp/)
- Luz em placa fraca: [Unreal 5.8 Lumen Lite](https://overclock3d.net/news/software/unreal-engine-5-8-delivers-megalights-and-lumen-lite-with-60-fps-performance-targets/)
- Conselhos de qualidade indie: [GameDev.net](https://gamedev.net/forums/topic/673941-what-about-3d-development-can-be-simplified-to-make-it-more-practical), [Wayline](https://www.wayline.io/blog/indie-games-aaa-graphics-innovation)
