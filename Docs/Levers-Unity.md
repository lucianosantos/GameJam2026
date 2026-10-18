# Alavancas de pressurização

Implementado em `ControllerScene`, em `Minigames/Levers`. Três hastes do modelo `Assets/Art/Panel/new/Alavanca.fbx` ocupam as três canaletas existentes no painel.

- Cada alavanca tem quatro posições: **1 embaixo e 4 em cima**. Quatro ticks sem números, afastados das hastes, marcam as posições sem indicar acertos individuais. A interpretação dos símbolos fica entre manual e operador.
- Clique esquerdo avança uma posição (4 volta a 1); clique direito recua (1 volta a 4). Arrastar ajusta entre as quatro posições, com limites nas extremidades. A haste gira sobre um pivô fixo na base, sem transladar ao longo da canaleta.
- A interação funciona apenas com a câmera focada neste minigame. Escape retorna à visão geral. As posições persistem ao sair e voltar.
- A verificação é automática após cada mudança de posição. Tentativas erradas não reiniciam as alavancas nem produzem flashes ou feedback individual.
- Somente `ShipPanel/Painel/pSphere4` indica o resultado: vermelho enquanto pendente, verde quando as três posições estão corretas. Cores e emissão iguais aos valores usados pelo `LightSequenceStatusLight`: vermelho `(0.65, 0.16, 0.12)`, verde `(0.18, 0.55, 0.24)`, emissão `0.025`.
- A conclusão dispara `On Completed` uma vez e bloqueia todas as alavancas, inclusive após trocar de vista. O evento está disponível para a futura integração ao fluxo global.

## Configuração

No componente `LeversBoard` de `Minigames/Levers`, `Correct Positions` contém três inteiros de 1 a 4, **da esquerda para a direita**. A montagem usa **[1, 2, 3] provisoriamente para teste**; o autor ainda deve definir a combinação correspondente ao manual. Não representa uma regra inferida dos símbolos do esboço. Não foram inventados símbolos ou regras de modelo de espaçonave.

`RestartPuzzle()` reinicia o jogo. As alavancas começam em 1; se a solução configurada for `[1, 1, 1]`, começam em 4 para que o painel não inicie resolvido. Os componentes `LeverHandle` permitem ajustar os ângulos extremos de rotação e a distância de arraste no Inspector. As hastes e marcações ficam separadas na hierarquia.

A montagem usa ângulos locais de -40°, -29°, -18° e -7°: 33° de amplitude total, com pivô fixo. Cada tick é alinhado ao centro da face da ponta na visão focada do minigame, considerando a perspectiva da câmera e a superfície do painel. O espaçamento físico resulta desse alinhamento; não usa um passo fixo. As quatro posições das três alavancas foram conferidas por projeção e renderização. Ao alterar o modelo, os ângulos ou o alvo da câmera, recalibrar os ticks com `LeversSetup.AddPositionMarks` em Edit Mode.

`Tools > Minigames > Levers > Assemble` monta o jogo apenas quando ainda não existe na cena; não executar novamente para ajustar uma montagem existente. `LeversSetup` mantém o procedimento de autoria reproduzível.

## Verificação

`Tests/Test-Levers.ps1`: todas as 64 soluções possíveis e as 64 combinações para a solução provisória, ausência de conclusão parcial, persistência dos erros, bloqueio, reinício, cópia da configuração e entradas inválidas.

`Tests/LeversUnityChecks.cs`, via Pipeline `run_script`, entrada `LeversUnityChecks.Run`, em Play Mode: indicador em pSphere4, material original restaurado em pSphere5, pivô fixo nas quatro rotações, quatro ticks afastados sem números, referências reais da cena, raycasts físicos das três alavancas, cliques nos dois sentidos, eventos de arraste com câmera real, bloqueio fora do foco, troca de vistas, cores exatas do indicador, ausência de mudança de material individual, evento único, bloqueio e reinício. Testes passaram. Renderização inicial e resolvida inspecionada. Sensibilidade do arraste com mouse físico ainda requer avaliação do autor.
