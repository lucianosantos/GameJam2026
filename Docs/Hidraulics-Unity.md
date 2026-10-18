# Montagem do minigame de hidráulica

## Comportamento desta versão

- Grade 6 colunas × 6 linhas, com peças retas e curvas.
- Clique gira 90° no sentido horário em uma animação suave de 300 ms. Novos giros ficam bloqueados durante a animação; água e conclusão são atualizadas ao terminar o giro. Reiniciar ou desativar o Board cancela um giro pendente sem alterar a orientação lógica. Sem cronômetro próprio ou derrota local.
- Entrada fixa no canto superior esquerdo, aberta para fora pela esquerda.
- Saída fixa no canto inferior direito, aberta para fora pela direita.
- Fundo azul identifica a entrada; fundo amarelo identifica a saída.
- Água aparece imediatamente em todas as peças conectadas à entrada por aberturas recíprocas. Ao desconectar uma peça, o trecho perde água.
- Chegar à saída dispara `On Completed` uma vez e bloqueia os cliques até `RestartPuzzle()`.
- O caminho principal é pré-definido, com rotações embaralhadas e peças de distração. Reiniciar não gera novos formatos de caminho; isso pode ser uma evolução posterior.
- Não há simulação física, animação gradual de enchimento nem integração com um gerenciador geral de vitória.

Os scripts estão em `Assets/Scripts/Hydraulics`. A cena não foi montada automaticamente. As instruções abaixo são para edição fora do Play Mode.

## 1. Importar cinco sprites

Selecione estes PNGs e configure no Inspector: **Texture Type = Sprite (2D and UI)**, **Sprite Mode = Single**, **Mesh Type = Full Rect**. Clique em **Apply**. Mantenha a imagem inteira, sem recortar suas bordas transparentes: elas participam do alinhamento entre células.

| Uso | Arquivo dentro de Assets/Art/Minigames/Hidraulics |
| --- | --- |
| Fundo | `BackTile.png` |
| Reta seca | `pipeGreen-flatEmpty.png` |
| Reta com água | `pipeGreen-flatFull.png` |
| Curva seca | `pipeGreen-curveEmpty.png` |
| Curva com água | `pipeGreen-curveFull.png` |

Essas retas, sem rotação, têm aberturas em cima e embaixo. Essas curvas têm aberturas à direita e embaixo. O código usa essas orientações como referência.

## 2. Criar o container no painel

Na `ControllerScene`, crie um Canvas chamado `HidraulicsCanvas`, como filho do objeto `Hidraulics` que está sob `Minigames`. Existe outro `Hidraulics` sob `CameraPositions`: esse é o alvo da câmera, não o container.

No Canvas:

- **Render Mode = World Space**.
- **Event Camera = Main Camera**.
- RectTransform: **Width = 600**, **Height = 600**, pivot central, como referência para a grade quadrada. O Canvas pode ter outras dimensões para acompanhar o painel.
- Mantenha o **Graphic Raycaster**. Para este protótipo, use **Blocking Objects = None**, evitando que o collider da área de aproximação intercepte os cliques de UI.
- Reduza a escala uniformemente e posicione/rotacione o Canvas sobre a superfície marcada no print, com uma pequena distância da malha para evitar sobreposição visual. A escala é `largura desejada em unidades / 600`; por exemplo, uma largura de 1,2 unidade usa escala `0.002` nos três eixos.

As coordenadas exatas devem ser ajustadas na Scene View; o print não fornece medidas suficientes para fixá-las com precisão. Verifique o encaixe também pela câmera aproximada.

A grade usa células quadradas e fica centralizada no Board. Em um container retangular, pode sobrar margem no eixo maior; o script adapta o tamanho das 36 células à área disponível.

Crie um filho UI vazio chamado `Board`, com RectTransform esticado para ocupar todo o Canvas e offsets zerados. Adicione **Grid Layout Group** e **Hydraulics Board**. No Grid, deixe **Spacing = (0, 0)** e **Padding = 0**. O script ajusta tamanho das células, alinhamento e número de colunas.

O `Board` deve conter somente as peças geradas. Coloque eventuais rótulos `ENTRADA` e `SAÍDA` como irmãos do Board, junto às respectivas bordas, com **Raycast Target desativado**.

## 3. Criar o prefab de célula

Crie temporariamente um **UI > Button** (pode ser Button - TextMeshPro) fora do `Board` e nomeie `HydraulicsTile`. Remova o filho de texto.

No objeto raiz:

- RectTransform **100 × 100**.
- **Image** com `BackTile`, cor branca e **Raycast Target ligado**.
- **Button** com **Transition = None**, **Navigation = None** e lista **On Click vazia**. O script registra o clique.
- Componente **Hydraulics Tile View**.

A raiz do prefab deve ter **rotação local `(0, 0, 0)`**, **escala local `(1, 1, 1)`** e **Pos Z = 0**. A inclinação para acompanhar o painel fica apenas no Canvas; não a aplique novamente ao prefab.

Crie um filho **UI > Image**, chamado `Pipe`, com anchors esticados nos dois eixos, offsets zero, pivot `(0.5, 0.5)`, escala `(1, 1, 1)` e rotação zero. Use `pipeGreen-flatEmpty` como imagem provisória, **Preserve Aspect ligado** e **Raycast Target desligado**.

Confira explicitamente **Left, Right, Top, Bottom e Pos Z = 0** no `Pipe`. Apenas trocar os anchors para Stretch pode manter os deslocamentos antigos; zere também esses valores.

No `Hydraulics Tile View`, arraste:

- **Pipe Image**: o Image do filho `Pipe`.
- **Background Image**: o Image da raiz do botão.

Arraste a raiz para uma pasta de prefabs no Project para salvar `HydraulicsTile.prefab`. Remova a instância temporária da cena. Mantenha o prefab ativo.

## 4. Ligar as referências

No componente `Hydraulics Board` do `Board`, preencha:

| Campo | Referência |
| --- | --- |
| Tile Prefab | prefab `HydraulicsTile` |
| Straight Dry | `pipeGreen-flatEmpty` |
| Straight Wet | `pipeGreen-flatFull` |
| Elbow Dry | `pipeGreen-curveEmpty` |
| Elbow Wet | `pipeGreen-curveFull` |
| Camera Controller | componente `CameraController` da `Main Camera` |
| Camera Target | `Hidraulics` sob `CameraPositions` |

Em `On Completed`, futuramente adicione o objeto que gerencia os minigames e escolha seu método público de conclusão. Pode ficar vazio durante o primeiro teste. O estado também está disponível em `IsCompleted`, e o evento pode ser registrado via `OnCompleted.AddListener(...)`.

## 5. Configurar o EventSystem

Se a cena não tiver um, crie **GameObject > UI > Event System**. Deve existir somente um EventSystem ativo.

Como o projeto usa o novo Input System, use **Input System UI Input Module** em vez de **Standalone Input Module**. Se o Inspector oferecer a substituição, use-a. Se as ações estiverem vazias, use **Assign Default Actions** no módulo. O Canvas precisa apontar para a câmera que vê o tabuleiro.

O `CameraController` informa três estados: `Overview`, `Transitioning` e `Minigame`. Cada `CameraClickArea` desativa seus colliders durante a transição e enquanto a câmera está em um minigame. Ao terminar o retorno ao overview, restaura os colliders que estavam habilitados originalmente. Isso afeta as áreas de aproximação, sem desligar colliders de outros objetos da cena.

O `HydraulicsBoard` usa um `CanvasGroup` (criado automaticamente se necessário) para controlar `Interactable` e `Blocks Raycasts`. No overview e nas transições, as peças ficam visíveis, mas não capturam cliques. Assim, clicar sobre um cano dentro da área do BoxCollider faz apenas a aproximação da câmera. A interação com peças é habilitada quando a câmera chega ao `Camera Target` configurado. Aproximar outro minigame não habilita o tabuleiro de hidráulica.

Não é necessário reservar uma margem fora das peças para clicar e aproximar. `Escape` retorna ao overview e volta a habilitar as áreas de aproximação ao fim do movimento. Ao criar novos minigames, conecte sua UI ao mesmo estado de câmera antes de permitir interação.

As imagens retas e curvas têm proporções diferentes. O comprimento da reta (`235` pixels nos arquivos atuais) define a escala comum da célula. A reta ocupa `118 × 235` pixels e a curva `176 × 176`, posicionada no canto inferior direito antes da rotação. Assim, as aberturas direita e inferior da curva encontram os centros das bordas, com a mesma espessura da reta. O pivot da imagem deslocada é ajustado para que ela gire em torno do centro da célula. `Preserve Aspect` é desligado em runtime porque o script já define explicitamente as dimensões corretas; não aumente manualmente a escala do prefab.

**Camera Target não é a Main Camera.** Deve apontar para o objeto de posição `CameraPositions/Hidraulics`, o mesmo usado como `Target Position` pela área de aproximação de hidráulica. Se apontar para a própria câmera, o tabuleiro não reconhecerá a chegada ao alvo e não aceitará cliques.

## 6. Verificar em Play Mode

1. No overview, clique diretamente sobre um cano dentro da área de aproximação: a câmera deve aproximar sem girar a peça. Durante a transição, as duas áreas de aproximação devem estar com seus colliders desativados.
2. Clique numa peça intermediária: ela deve girar 90°. Os dois terminais ficam fixos.
3. Conecte peças à entrada azul: o trecho deve passar para sprites com água. Desconecte: a água deve desaparecer do trecho isolado.
4. Complete o caminho até a saída amarela: o tabuleiro deve bloquear novas rotações.
5. Para testar o evento, conecte provisoriamente `On Completed` ao `GameObject.SetActive(false)` de um indicador de teste separado do Canvas.
6. Para reiniciar em runtime, conecte um botão externo a `HydraulicsBoard.RestartPuzzle()`; a lista de eventos aceita esse método público.
7. Pressione Escape: a UI deve perder interação imediatamente e os colliders de aproximação devem voltar ao terminar o retorno. Aproxime o outro minigame e confira que o tabuleiro de hidráulica continua sem interação.
8. Confira o encaixe e a mesma espessura nas conexões entre retas e curvas, nas orientações horizontal e vertical, com e sem água.

Não coloque o prefab de célula como filho do Board na cena: as 36 instâncias são criadas no Start. Pare o Play Mode antes de fazer alterações que deseja salvar.

## Verificação automatizada disponível

Na raiz do projeto, execute `./Tests/Test-Hydraulics.ps1` em um processo novo do PowerShell. O teste verifica 1.000 sementes, solução garantida, início incompleto, rotação, conectividade recíproca, limites da grade, remoção de água e bloqueio após vitória.

Execute também `./Tests/Test-HydraulicsAssets.ps1`: ele mede as aberturas nos PNGs secos e com água para conferir espessura e centralização nas quatro rotações (tolerância de meio pixel), e verifica se o alvo de câmera do Board coincide com o de uma área de aproximação na cena salva.

Com o Play Mode parado, o menu **Tools > Validation > Check Minigame Interaction** cria componentes temporários para verificar os estados de câmera, desativação/restauração das duas áreas e interação apenas no alvo de hidráulica. Os objetos temporários são removidos ao terminar. Esse teste não substitui a verificação visual nem a sequência real de cliques em Play Mode.

Os scripts também foram compilados contra as referências locais do projeto Unity. A montagem visual e os cliques ainda precisam ser validados em Play Mode após a criação do Canvas e do prefab.

Referência de posicionamento de Canvas: https://docs.unity3d.com/Packages/com.unity.ugui@2.0/manual/HOWTO-UIWorldSpace.html

