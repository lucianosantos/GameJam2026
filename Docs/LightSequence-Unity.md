# Minigame de sequência de luz

O prefab `Assets/Prefabs/LightSequenceButton.prefab` reutiliza o modelo 3D `Botão luz.fbx`. A cena `ControllerScene` contém oito instâncias no Canvas do Light Sequence, preservando as posições montadas pelo autor.

## Regras e configuração

No objeto `Minigames/LightSequence/Canvas`, o componente `LightSequenceBoard` contém:

- `Buttons`: referências aos oito botões da cena.
- `Correct Order`: array de IDs que define a solução, inicialmente **1, 2, 3, 4, 5, 6, 7, 8**. Pode ser reordenado no Inspector antes da partida; `RestartPuzzle()` relê esse array.
- `On Completed`: UnityEvent de sucesso, disparado uma vez ao completar a sequência. Está disponível para conectar ao fluxo global de partida; nenhum gerenciador global de vitória foi encontrado nos scripts atuais.

Cada instância tem um `Button Id` único. A configuração atual segue a ordem da Hierarchy: coluna esquerda 1 em cima e 2 embaixo; segunda coluna 3 e 4; terceira 5 e 6; quarta 7 e 8. O primeiro dos dois objetos que se chamavam `LightSequenceButton6` foi renomeado para `LightSequenceButton5`.

Um acerto mantém o botão verde. Um erro apaga imediatamente os acertos anteriores, zera o progresso e pisca o botão errado três vezes em vermelho. Durante esse feedback, novos cliques são ignorados. O próximo clique começa novamente pelo primeiro ID da ordem; o clique errado não é reaproveitado como início da tentativa seguinte. Apertar um botão já verde fora da ordem também é erro.

Completar mantém os botões corretos verdes, bloqueia novas entradas e dispara `On Completed`. `RestartPuzzle()` limpa as luzes e permite nova tentativa. Sair do foco da câmera preserva o progresso. Desativar o Board durante um erro cancela o piscar e mantém o progresso zerado.

Para adicionar botões, duplique uma instância ou arraste o prefab para o mesmo Canvas, ajuste sua posição, defina um ID único e adicione a referência ao array `Buttons` e à solução. As cópias usam automaticamente o `LightSequenceInput` do Canvas; não precisam de referências individuais à câmera. Configurações vazias, IDs duplicados ou IDs da solução sem botão correspondente geram erro e bloqueiam o minigame.

## Animação e interação

- Clique esquerdo afunda o modelo no eixo Z local da raiz do prefab e mantém a pressão enquanto o mouse estiver segurado.
- Soltar ou tirar o ponteiro do botão inicia o retorno. Cliques rápidos têm uma duração mínima de pressão para o movimento ficar perceptível.
- Desativar o objeto ou bloquear a interação restaura a posição de repouso.
- O BoxCollider fica na raiz e não se move com o modelo, preservando a área de clique.
- O `PhysicsRaycaster` da Main Camera entrega os eventos pelo EventSystem existente. O GraphicRaycaster sozinho não atende esse modelo 3D.
- O `LightSequenceInput` libera os colliders apenas quando a câmera chega a `CameraPositions/LightSequence`. Overview, transição e outros alvos bloqueiam os botões.

No componente `LightSequenceButton`, ajuste `Pressed Offset`, `Press Duration`, `Release Duration` e `Minimum Press Time` para alterar a sensação da pressão. Valores iniciais: deslocamento de 0,008 unidade em Z, pressão de 0,06 s, retorno de 0,10 s e pressão mínima de 0,08 s. Os tempos usam o relógio não escalado.

## Cores no prefab

O componente `LightSequenceButton` contém `Light Renderer`, `Correct Color`, `Error Color`, `Emission Intensity`, `Error Flash Count`, `Error Flash Duration` e `Error Flash Interval`. Os valores de fábrica produzem verde nos acertos e três flashes vermelhos de 0,12 s com intervalos apagados de 0,10 s. O material dedicado é `Assets/Art/Materials/M_LightSequenceButton.mat` (URP/Lit, emissão habilitada). Não foi necessário criar arte adicional.

Cada instância usa `MaterialPropertyBlock` para sua própria cor e emissão, preservando a independência das cópias. Em repouso, a emissão é preta e a cor volta à cor base do material. O material mantém uma emissão mínima para que o validador do URP preserve o keyword de emissão; em runtime o prefab substitui esse valor por zero quando apagado. A cor afeta o modelo inteiro, pois o asset atual contém um único Renderer/material.

`On Click` continua disponível no botão para outros consumidores. O Board registra seus listeners automaticamente e os remove quando desativado.

## Validação realizada

Scripts compilados no Unity 6000.3.10f1 sem erros. Execute `./Tests/Test-LightSequence.ps1` em um PowerShell novo para validar erros em cada uma das oito posições, reinício do progresso, nova tentativa, conclusão, bloqueio após sucesso, ordem alternativa, cópia do array, IDs repetidos na solução e rejeição de ordem vazia.

`Tests/LightSequenceUnityChecks.cs` foi executado pelo `run_script` da API local Unity Pipeline, em Play Mode. Passaram: IDs e referências das oito instâncias, emissão habilitada, verde independente por Renderer, bloqueio por câmera, erro apagando os acertos e bloqueando entradas, alternância temporizada vermelho/apagado, conclusão com evento único, reinício e desativação/reativação sem duplicar listeners. Renderizações da câmera também foram inspecionadas: três acertos verdes e um erro vermelho com os outros botões apagados.

Limitação: o Player Loop do Editor automatizado não avançou normalmente. Os testes despacharam eventos via `ExecuteEvents` e avançaram explicitamente o iterador de flashes. Isso valida os componentes, cores renderizadas e referências, mas não confirma o fluxo físico do mouse nem a duração percebida do movimento e do piscar.

Conferência manual: entre em Play Mode, aproxime o Light Sequence e complete alguns acertos; provoque um erro e confira três flashes vermelhos com apagamento dos anteriores. Depois complete a ordem e confira o evento de sucesso e o bloqueio. Verifique também o afundamento/retorno dos botões e o bloqueio após Escape.
