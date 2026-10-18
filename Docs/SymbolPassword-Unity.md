# Reparos elétricos — Symbol Password

Montado em `Minigames/SymbolPassword/Canvas`, na `ControllerScene`. O prefab `Assets/Prefabs/SymbolPasswordButton.prefab` usa a malha do `BtnSenha` que estava nesse Canvas, com material próprio. Dez teclas mostram somente símbolos dentro dos botões; a última linha contém **Clear / símbolo de zero / Ok**. Os valores internos seguem 1–9 nas três primeiras linhas, da esquerda para a direita.

## Regras da POC

- Senha fixa **41036**, correspondente a `1 + 3`, `6 − 5`, `9 × 0`, `7 − 4`, `2 × 3` no manual fornecido.
- `LINHA 1.png` a `LINHA 9.png` representam 1–9; `LINHA 10.png` representa zero.
- O monitor acima das teclas mostra até cinco símbolos em ordem. Entradas extras são ignoradas quando está cheio. Preencher o monitor não valida automaticamente.
- **Clear** apaga a tentativa inteira. **Ok** compara a tentativa completa com a solução. Entrada vazia ou incompleta também recebe feedback de erro.
- No erro, monitor e todas as doze teclas piscam três vezes em vermelho. O conteúdo permanece no monitor. A interação fica bloqueada durante o feedback; depois, o jogador pode limpar e tentar novamente.
- Somente o foco `CameraPositions/SymbolPassword` permite interação; overview, transição e outros focos bloqueiam as teclas. Sair do foco preserva o conteúdo.
- Acertar e pressionar **Ok** deixa o monitor verde, mantém a senha visível, dispara `OnCompleted` uma vez e bloqueia novas entradas. O evento ainda não tem consumidor global.
- `RestartPuzzle()` limpa a tentativa e a conclusão. Desativar o Board cancela o piscar sem apagar a tentativa.

## Assets e Inspector

O componente `SymbolPasswordBoard` fica no Canvas. Contém câmera/alvo, as doze teclas, dez sprites indexados por dígito, cinco posições do monitor, `Correct Order`, cores/tempos de erro e `On Completed`. Cada tecla expõe `Action` (Digit/Clear/Submit), `Digit`, `On Click`, Renderer, visual móvel e parâmetros de pressão. `On Click` é adicional ao processamento da tecla pelo Board.

Os PNGs originais foram copiados sem alteração para `Assets/Art/Minigames/SymbolPassword/Textures`. Os assets `Symbol_0.asset` a `Symbol_9.asset` apenas recortam a transparência ao redor dos símbolos, referenciando essas texturas. Não há números visíveis no teclado ou monitor.

A pressão usa deslocamento local de 0,008 em Z, 0,06 s para pressionar, 0,10 s para retornar e pressão mínima de 0,08 s. O collider fica fixo na raiz; o modelo e os caracteres se movem juntos. Animação e feedback usam tempo não escalado. Rótulos não recebem raycasts; os colliders 3D usam o PhysicsRaycaster da câmera.

`Tools > Minigames > Symbol Password > Assemble` serve para a montagem inicial e recusa um Canvas já montado. Para ajustes posteriores, edite os objetos, o prefab e os campos do Inspector.

## Verificação

- `Tests/Test-SymbolPassword.ps1`: testa tentativas incompletas/incorretas sem perda de entrada, limite de cinco, confirmação explícita, limpar, nova tentativa, conclusão, bloqueio, reinício, dígitos inválidos e cópia da solução.
- `Tests/SymbolPasswordUnityChecks.cs`, entrada `SymbolPasswordUnityChecks.Run`, executado via Unity Pipeline em Play Mode: referências das doze teclas, foco/persistência, monitor, erro vermelho, preservação da entrada, conclusão única e reinício.
- `Tests/SymbolPasswordEffectsChecks.cs`, entrada `SymbolPasswordEffectsChecks.Run`, após o teste anterior: deslocamento do visual, saída do ponteiro, restauração ao desativar, raycasts físicos até cada tecla e alternância das três piscadas.

Compilação no Unity 6000.3.10f1 sem erros. Capturas da câmera foram inspecionadas para enquadramento e legibilidade. Os testes automatizados despacham eventos e avançam explicitamente o iterador dos flashes; a sensação dos tempos e o fluxo físico do mouse ainda precisam de conferência manual no Editor.
