# Houston, We Have a Problem

**Nome provisório.** Jogo cooperativo para **dois jogadores**, desenvolvido em Unity para uma game jam. Cada jogador abre uma instância do mesmo jogo em um computador separado e escolhe um papel no menu: um consulta o **manual** e o outro opera o **controlador**, no painel da nave.

O controlador encontra os minigames, mas precisa das instruções disponíveis na visão do manual. O outro jogador lê essas instruções e as comunica ao controlador. Os dois veem informações diferentes e precisam conversar para consertar a nave antes que ela colida com um planeta.

Este README é o ponto de entrada para entender o projeto e continuar o desenvolvimento em outros chats. Ele distingue a proposta de design, o que já está implementado e as decisões ainda abertas. `GameJam2026` é o nome da pasta do projeto.

**Idioma do jogo: inglês.** Todos os textos apresentados aos jogadores devem estar em inglês, incluindo menus, manual, instruções, mensagens de vitória/derrota e botões. A documentação de desenvolvimento pode continuar em português. Na tela de derrota: **Time's Up**, **The ship has crashed**, **Try Again** e **Back to Menu**.

## Proposta e decisões abertas

| Assunto | Situação confirmada |
| --- | --- |
| Nome do jogo | **Houston, We Have a Problem**, provisório. |
| Contexto visual | Painel de nave em uma cena 3D, com regiões dedicadas aos minigames. |
| Estrutura | Cooperação entre dois jogadores, com papéis de manual e controlador, em instâncias separadas. Cinco minigames previstos. |
| Cadeado celeste | Quatro roletes no painel com lua, estrela, sol e Saturno; clique para avançar e botão CONFIRM. Texto SUN, MOON, SATURN, STAR cifrado pelo diagrama do manual, com Q² para os dois O de MOON. |
| Tema e nome da jam | Ainda não informados. |
| Prazo | **18/10/2026**, conforme a informação de que o prazo termina no dia seguinte a 17/10/2026. Horário de entrega não informado. |
| Narrativa e objetivo | Consertar a nave a tempo de evitar uma colisão com um planeta. |
| Tempo de partida | **10 minutos**, ajustáveis em `GameSession/Initial Seconds`. Cronômetro vermelho de sete segmentos fixo no topo central da tela, inclusive nos minigames. Implementação e validação em [Docs/Countdown-Unity.md](Docs/Countdown-Unity.md). |
| Derrota | Não consertar a nave antes do fim do tempo; a nave colide com o planeta. Outras penalidades não foram especificadas. |
| Vitória | `GameSession` encerra com sucesso quando os cinco minigames estão concluídos antes do fim do tempo. Integração ainda pendente de teste em Play Mode. |
| Plataforma da demo | **PC, com mouse e teclado.** Dispositivos móveis não fazem parte do escopo atual. |

## Experiência cooperativa e escopo da demo

O menu inicial deve permitir abrir duas visões:

- **Manual:** páginas de instruções dos minigames, com navegação entre páginas. Um jogador consulta esse conteúdo e orienta verbalmente o controlador.
- **Controlador:** visão do painel da nave, com acesso aos minigames e um cronômetro visível para o reparo.

O princípio de design é que o jogador não consulte o manual e o controlador ao mesmo tempo. A separação de informações entre os papéis é essencial para a cooperação. A forma de impedir ou limitar a troca entre essas visões durante a partida ainda não foi especificada nem confirmada como implementada.

Os dois jogadores abrem o mesmo jogo em computadores separados. Sincronização entre instâncias, lobby, servidores e comunicação integrada não foram especificados; não presumir uma infraestrutura de multiplayer em rede como requisito apenas pela existência dos dois papéis.

O objetivo imediato é uma **demo para a jam**, com menu, manual paginado e controlador com os minigames no painel. Esses itens descrevem o escopo desejado, não uma confirmação de que todo esse fluxo já está pronto. O cronômetro e as condições globais de término estão ligados à `ControllerScene`; sua duração pode ser ajustada no Inspector. Compilação e lógica de contagem foram verificadas; a integração visual e o término da partida ainda precisam ser testados em Play Mode.

## Cinco minigames previstos

| Minigame | Informação disponível |
| --- | --- |
| **Combinação de fios / pane elétrica** | Protótipo 3D montado na cena, com sete portas, Z com dois encaixes e quatro fios iguais. Assets editáveis, arraste, validação e bloqueio implementados e testados no Unity. Montagem em [Docs/Wires-Unity.md](Docs/Wires-Unity.md). |
| **Sequência de luz** | Oito botões 3D; sequência configurável em array, inicialmente 1–8. Acerto fica verde; erro pisca vermelho e reinicia o progresso. Completar dispara `On Completed` uma vez. Montagem e limites de validação em [Docs/LightSequence-Unity.md](Docs/LightSequence-Unity.md). |
| **Cadeado de símbolos celestes** | Protótipo 3D em `Minigames/CelestialLock`, com Canvas em World Space, quatro roletes, placa de quatro palavras e CONFIRM. Texto cifrado: USL / HQ²L / UCVSZL / UVCZ; solução sol, lua, Saturno, estrela. Valores configuráveis. Testes de lógica e de cliques no Unity passaram. Detalhes em [Docs/CelestialLock-Unity.md](Docs/CelestialLock-Unity.md). |
| **Alavanca de pressurização** | Três alavancas 3D, quatro posições (1–4), clique e arraste. Apenas `pSphere4` indica conclusão, com vermelho/verde do Light Sequence. Solução configurável, provisoriamente `[1, 2, 3]`; bloqueio após sucesso. Montagem e validações em [Docs/Levers-Unity.md](Docs/Levers-Unity.md). |
| **Senha de símbolos** | Nome confirmado. Regras serão fornecidas quando começar sua implementação. |

As regras dos minigames são fornecidas gradualmente pelo autor. A combinação de fios já tem sua especificação registrada em [Docs/Wires-Unity.md](Docs/Wires-Unity.md). Não inventar regras a partir dos nomes nem tratar a ausência de detalhes neste README como autorização para redefini-las.

### Regra geral de conclusão

**Todo minigame concluído com sucesso deve bloquear alterações do jogador.** Não deve ser possível desfazer um reparo concluído, inclusive saindo do painel e voltando a ele durante a mesma partida. O bloqueio começa com o sucesso, não com uma tentativa incompleta ou incorreta. O evento de conclusão deve ser emitido uma única vez por partida; um reinício explícito da partida pode restaurar o estado inicial. Esta é uma regra geral de design; sua presença aqui não confirma a integração de todos os minigames nem a existência de um gerenciador global de partida.

## Combinação de fios / pane elétrica

Protótipo implementado e montado em `ControllerScene`, no objeto `Minigames/Wires`. O `GameCanvas` contém o painel e os conectores; o `WiresCanvas` contém a bandeja e os quatro fios. Ambos organizam objetos 3D em World Space. Detalhes, assets, parâmetros e imagens reais em [Docs/Wires-Unity.md](Docs/Wires-Unity.md).

- Caixa horizontal 3D, vista de cima com inclinação. Z no topo; X, infinito e M à esquerda; música, E e P à direita. Os nomes são identificadores internos; os símbolos visuais devem manter a ambiguidade intencional e corresponder aos usados no manual.
- Sete portas e quatro fios iguais, inicialmente soltos numa caixinha lateral. Z tem dois encaixes visíveis e aceita duas pontas; cada outra porta aceita uma. Na solução, todas as portas ficam ocupadas; a ideia anterior de deixar uma vazia foi descartada.
- Clique e arraste para mover cada ponta. Arrastar uma ponta conectada retira-a da porta e permite reposicioná-la. Uma porta ocupada é ignorada durante o arraste, sem substituir a ponta existente nem interromper o movimento.
- O encaixe acontece **somente ao soltar o botão esquerdo do mouse**, nunca apenas por aproximar a ponta durante o arraste. Tolerância inicial ajustável, a calibrar em teste. Ao soltar, usar o primeiro encaixe livre dentro da tolerância em ordem estável; isso também resolve empates entre as duas entradas de Z.
- É permitido conectar as duas pontas do mesmo fio aos dois encaixes de Z; essa montagem não satisfaz a solução. A ordem das pontas e a identidade dos quatro fios não importam.
- Ligações corretas, sem orientação: **P–Z, música–M, Z–infinito e X–E**. A regra do manual que proíbe X–música continua válida.
- Não há indicação intermediária de acerto/erro, energização, reset por erro ou botão de teste. Sempre que os quatro fios estiverem totalmente conectados, verificar o conjunto: se correto, concluir e bloquear; se incorreto, manter a montagem e permitir correções. Reavaliar nas alterações posteriores que deixarem os quatro fios completos.
- Cruzamentos permitidos, com um cabo visualmente acima do outro, sem exigir simulação física. Comprimento ajustado automaticamente. Preservar conexões ao sair e voltar ao painel.
- Abertura de tampa e remoção de parafusos ficam fora do escopo atual; podem ser consideradas depois.

Modelos, materiais, PNGs dos símbolos e regras ficam em `Assets/Art/Minigames/Wires`, com referências serializadas pelo Inspector. O cabo gera apenas sua malha deformável durante a interação; não cria materiais ou texturas em runtime. `WiresBoard` expõe `Snap Tolerance` (**`0.045`**, aumentada em 80% sobre `0.025`, nas unidades locais do painel), referências e `On Completed`. O evento está disponível, mas ainda não está ligado a um gerenciador global de partida. As dimensões e posições dos canvases criados pelo autor foram preservadas; o formato atual é mais alto que o esboço original.

O painel também contém **quatro entradas decorativas com quatro símbolos novos e dois fios já conectados**, em posições e rotações irregulares. São apenas cenário: não aceitam pontas jogáveis, não podem ser retirados e não participam da solução. O prefab `WiresScenery` fica em `GameCanvas/Scenery`; seus cabos têm malhas estáticas salvas, sem colliders de seleção nem componentes de encaixe. Os fios jogáveis passam por cima dos fios fixos nos cruzamentos.

## Ambiente

- **Unity:** `6000.3.10f1`, conforme `ProjectSettings/ProjectVersion.txt`.
- **Linguagem:** C#.
- **Renderização:** Universal Render Pipeline (`17.3.0`).
- **Entrada:** Input System (`1.18.0`).
- **UI:** uGUI (`2.0.0`), com Canvas em World Space nos minigames.

As dependências estão declaradas em [Packages/manifest.json](Packages/manifest.json). Abra a pasta raiz pelo Unity Hub com a versão indicada e aguarde a importação dos assets e pacotes.

## Cenas e execução

A versão desktop inicia em janela de **1280 × 720**, configurada em Player Settings; a resolução nativa automática está desativada. O redimensionamento está habilitado. `WindowAspectRatio` permanece entre cenas e reajusta a janela para **16:9** após 0,2 segundo sem mudança de tamanho, respeitando a área disponível do monitor com margem para bordas. Durante o arraste pode haver uma adaptação momentânea. A correção atua na versão desktop em modo janela; não altera tela cheia nem a Game View do Editor. Compilação no Unity e `Tests/WindowAspectRatioUnityChecks.cs` passaram (alteração por largura/altura, proporção e limites em mais de 7.000 tamanhos). A interação com a janela do executável ainda requer teste manual.

As três cenas abaixo estão habilitadas em `ProjectSettings/EditorBuildSettings.asset`, nesta ordem:

| Cena | Papel atual |
| --- | --- |
| `Assets/Scenes/MainMenu.unity` | Entrada pelo menu. `MainMenuController` oferece métodos para abrir a cena de jogo, abrir o manual, alternar créditos e sair. |
| `Assets/Scenes/ManualScene.unity` | Cena reservada para o manual. A existência da cena não significa que seu conteúdo esteja concluído. |
| `Assets/Scenes/ControllerScene.unity` | Cena do painel da nave, câmera e cinco minigames. |

Para testar o fluxo de entrada, abra `MainMenu`. Para trabalhar diretamente no painel, abra `ControllerScene`. Faça alterações persistentes de cena e prefab com o Play Mode parado e salve depois.

## Navegação e interação

O `CameraController` tem três estados: `Overview`, `Transitioning` e `Minigame`.

1. No overview, um clique esquerdo em uma área de aproximação com `CameraClickArea` inicia o movimento para a posição configurada.
2. Durante o movimento e enquanto a câmera está em um minigame, os colliders das áreas de aproximação ficam desativados. Isso não desativa os colliders de toda a cena.
3. Cada minigame libera sua interação somente ao chegar ao alvo de câmera correspondente.
4. `Escape` inicia o retorno ao overview. Os colliders de aproximação são restaurados ao terminar o retorno.

O Canvas do Light Sequence tem `LightSequenceInput`, que libera os colliders de seus botões apenas no alvo de câmera correspondente. Novos minigames precisam integrar sua interação ao estado de câmera.

## Mapa do código

| Arquivo | Responsabilidade |
| --- | --- |
| `Assets/Scripts/MainMenuController.cs` | Métodos de navegação do menu, créditos e saída. |
| `Assets/Scripts/CameraController.cs` | Movimento da câmera, estado de visão, alvo ativo e retorno com Escape. |
| `Assets/Scripts/CameraClickArea.cs` | Clique por raycast 3D e ativação dos colliders de aproximação conforme o estado da câmera. |
| `Assets/Scripts/Wires/WiresPuzzle.cs` | Estado das oito pontas, ocupação dos encaixes, pares sem orientação e bloqueio após solução; sem dependência de Unity. |
| `Assets/Scripts/Wires/WiresBoard.cs` | Arraste pelo Input System, encaixe ao soltar, integração com câmera e evento de conclusão. |
| `Assets/Scripts/Wires/WiresCable.cs` | Malha tubular variável, curvas e ordem visual dos cruzamentos. |
| `Assets/Editor/WiresSetup.cs` | Ferramenta de autoria dos assets e montagem inicial; não substitui assets existentes nem remonta um painel já configurado. |
| `Assets/Editor/MinigameInteractionChecks.cs` | Validação de componentes pelo menu do Editor. |
| `Tests/` | Verificações da lógica, dos assets e das referências na cena salva. |

Arte está em `Assets/Art`, prefabs em `Assets/Prefabs` e documentação complementar em `Docs`.

## Créditos de áudio

- Vitória: [Victory Bell Success Fanfare — emand_edroff, Pixabay](https://pixabay.com/sound-effects/musical-victory-bell-success-fanfare-576275/). Arquivo: `Assets/Audio/victory.mp3`, tocado pelo `SoundManager` ao vencer o jogo.
- Sucesso: [Success Notification — Universfield, Pixabay](https://pixabay.com/sound-effects/film-special-effects-success-notification-132473/). Arquivo: `Assets/Audio/success.mp3`, tocado pelo `SoundManager` quando um minigame é concluído com sucesso.

- Aviso de último minuto: [Warning — Pixabay](https://pixabay.com/sound-effects/film-special-effects-warning-545568/). Arquivo: `Assets/Audio/warning.mp3`, configurado no `SoundManager`; toca uma vez por partida ao entrar nos últimos 60 segundos, enquanto a partida estiver em andamento.
- Derrota: [Explosion FX — Pixabay](https://pixabay.com/sound-effects/search/explosion/). Arquivo: `Assets/Audio/explosion.mp3` (original: `dragon-studio-explosion-fx-425453.mp3`), configurado no `SoundManager`; toca ao perder. O link fornecido é uma busca de explosões, não a página específica desse arquivo.

### Créditos visuais e licença

O painel de créditos do menu informa, em inglês, que algumas imagens e efeitos sonoros vieram do Pixabay, disponíveis gratuitamente sob a Pixabay Content License, e que todas as demais artes e sons foram produzidos pelo time. A [seção 6 dos termos oficiais](https://pixabay.com/service/terms/) e o [resumo da licença](https://pixabay.com/service/license-summary/) dispensam atribuição ao autor/Pixabay; links individuais não são obrigatórios. Os links dos sons são mantidos acima como créditos voluntários e registro de origem. Essa dispensa não elimina as demais condições da licença.

`Assets/Art/UI/btn-v2.png` é o fundo dos botões do menu (incluindo saída e fechar créditos), do retorno ao menu no manual e dos botões de vitória/derrota. Importado como sprite com bordas para redimensionamento em nove regiões, sem alterar os eventos dos botões. Os botões de retorno do manual e do controlador exibem **Back to Menu**, com tamanho **240 × 72**, fonte **28** e posição no canto superior esquerdo do manual e superior direito do controlador; ambos chamam `SceneNavigation.QuitToMenu`.

## Verificações

Ajustes de mídia: compilação C# e teste do cronômetro passaram. No Unity, foram verificados a importação e as referências dos dois novos sons, os dez botões e o espaço dos créditos; menu, créditos e telas de resultado foram inspecionados por capturas. Em Play Mode, passaram as verificações de ausência de aviso acima de 60 segundos, disparo abaixo desse limite, bloqueio de repetição e derrota única. Volume percebido e mixagem precisam de conferência auditiva.

Na raiz do projeto, em um processo novo do PowerShell:

```powershell
./Tests/Test-Wires.ps1
```

- Com o Play Mode parado, **Tools > Validation > Check Minigame Interaction** verifica estados e componentes temporários no Editor. Esse teste não simula uma partida completa nem o fluxo real de eventos do mouse.
- `Test-Wires.ps1` verifica as 40.320 permutações de encaixes e as 768 soluções equivalentes, Z–Z, capacidade, correção e bloqueio após sucesso. `Tests/WiresUnityChecks.cs`, via Pipeline `run_script` em Play Mode, passou com eventos sintéticos reais do Input System: seleção por raycast, retirada, encaixe somente ao soltar, porta ocupada, empate em Z, correção, persistência entre vistas, evento único e bloqueio. A solução testada também passou na verificação geométrica de distância entre os cabos nos cruzamentos. Sensibilidade e acabamento ainda devem ser avaliados pelo autor com o mouse.

Compilação e testes automatizados não comprovam a apresentação ou os cliques em Play Mode. Depois de mudar câmera, Canvas, prefab, sprites ou referências, confira: primeiro clique apenas aproxima; a interação funciona no alvo correto; concluir bloqueia alterações; Escape restaura o overview.

## Estado e próximos trabalhos

- O Light Sequence contém oito instâncias de `Assets/Prefabs/LightSequenceButton.prefab`, com pressão, verde nos acertos e flashes vermelhos no erro. `LightSequenceBoard` no Canvas configura a ordem em array e expõe `On Completed` para integração ao fluxo global. Compilação, testes da lógica e verificações de componentes passaram; cores renderizadas foram inspecionadas. Clique físico e ritmo das animações ainda precisam de conferência manual. Veja [Docs/LightSequence-Unity.md](Docs/LightSequence-Unity.md).
- Integrar o fluxo global de partida: reparos concluídos, cronômetro visível e ajustável, vitória por consertar a nave e derrota por esgotar o tempo. Ainda falta detalhar o critério técnico de conclusão e a progressão entre os cinco minigames.
- Implementar ou concluir o manual com instruções e navegação entre páginas, conforme as regras que o autor fornecer para cada minigame.
- Avaliar com o mouse o protótipo de fios já montado, calibrar a tolerância pelo Inspector e revisar acabamento dos modelos, materiais e símbolos. Integrar `On Completed` ao fluxo global quando ele existir. Veja [Docs/Wires-Unity.md](Docs/Wires-Unity.md).
- Incorporar as regras restantes a partir do documento do autor e verificar seu estado nos arquivos atuais antes de iniciar trabalho; outros chats podem estar desenvolvendo partes do projeto.
- Confirmar a duração inicial da partida (5 ou 10 minutos), o mecanismo de separação dos papéis e o horário de entrega da jam.

## Contexto para continuar em outro chat

Leia este README, a documentação do minigame afetado e os arquivos atuais antes de alterar o projeto. A intenção de colaboração estabelecida é: o autor pode criar e posicionar assets, prefabs e objetos no Unity com orientação; o assistente pode implementar a lógica e corrigir arquivos quando solicitado.

Ao documentar ou desenvolver:

- Diferencie intenção de design, código implementado e comportamento confirmado em Play Mode.
- Não deduza tema da jam, narrativa ou regras globais a partir do cenário ou do nome do diretório.
- Preserve alterações existentes e referências de cenas, prefabs e arquivos `.meta`; outros chats podem estar trabalhando no mesmo projeto.
- Se o Inspector não corresponder ao arquivo salvo, confira o estado aberto no Editor antes de afirmar que a mudança foi aplicada ali.
- Atualize este README quando uma decisão de escopo, regra, integração ou estado de validação mudar. Detalhes específicos de montagem devem ficar em `Docs`.
