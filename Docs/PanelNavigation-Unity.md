# Navegação do painel

Na `ControllerScene`, todos os componentes `CameraClickArea` ficam diretamente em `ShipPanel`. A mudança de hierarquia preserva a posição, rotação e escala no mundo. A área antiga de Hydraulics continua desativada, como já estava na cena.

`ShipPanel/StatusMonitorClickArea` leva a `CameraPositions/StatusMonitor`, que enquadra o monitor central de status. Esse Transform é um ponto de destino da câmera principal; a câmera `StatusMonitor/Display camera` continua responsável pela textura do monitor.

`ShipPanelNavigation`, na câmera principal, cria a interface durante o jogo:

- Na visão geral, cantos cianos delimitam as áreas ativas e o texto inferior orienta o clique. Ao passar o mouse, a área ganha destaque e aparece seu nome.
- Ao focar um minigame ou o monitor, aparece **Back to panel**, com indicação do atalho **Esc**.
- Durante o movimento da câmera, as áreas e os botões de retorno ficam desativados. Ao terminar a transição para o painel, as áreas voltam a responder.
- Concluir um minigame mantém o foco. O jogador usa o botão ou Esc para voltar; o progresso é preservado.
- Ao encerrar a partida, as indicações e o botão somem para dar lugar ao resultado.

Os indicadores não interceptam cliques. A seleção usa os colliders reais das áreas de aproximação, priorizando a área disponível mais próxima. Colliders de peças dos minigames não bloqueiam essa seleção na visão geral. O clique de aproximação ignora a interface que recebe eventos de ponteiro.

Para ajustar o texto do bot�o, edite `Back Label` em `ShipPanelNavigation` no Inspector.

Validação no Unity:

- Em Edit Mode: **Tools > Validation > Check Panel Navigation**, **Check Minigame Interaction** e **Check Click Area UI Blocking**. Este �ltimo verifica que o PhysicsRaycaster permite navegar e que a interface de tela continua bloqueando cliques no cen�rio.
- Em Play Mode, após a inicialização: **Tools > Validation > Check Panel Navigation In Play Mode**. O teste verifica os raios físicos, retorno por botão em cada destino e conclusão sem retorno automático; resolve e reinicia Levers durante a verificação.

**Tools > Ship > Assemble Panel Navigation** reaplica a organização e adiciona os elementos ausentes. Se o ponto do monitor já existir, mantém seu enquadramento editado.
