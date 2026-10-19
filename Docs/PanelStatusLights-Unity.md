# Lâmpadas de conclusão do painel

`ShipPanel/Painel/Painel_LP` em `ControllerScene` é uma instância de `Assets/Art/Panel/new2/Painel (4).fbx`, substituindo a instância do FBX anterior. A malha derivada `Panel4WithoutCompletionCap.asset` compensa a origem do novo export e separa a superfície do botão final, mantendo o alinhamento dos minigames. Os quatro soquetes são da geometria do novo FBX.

A derivada é construída com os setters de vértices, normais, tangentes, UVs e índices de um `Mesh` novo. A abordagem anterior com `EditorUtility.CopySerialized` exibia geometria antiga no renderer apesar de os vértices lidos pela CPU corresponderem ao novo FBX. Por isso a conferência visual inclui renders neutros da derivada e do FBX original, com as lâmpadas ocultas, para verificar os soquetes reais.

- `ShipPanel/Painel/Luz_Alavancas`: lâmpada grande original, controlada por `LeversBoard`.
- `ShipPanel/Painel/Luz_Cadeado`: reposicionada à direita do cadeado, com cerca de 66,5% do tamanho da lâmpada grande; continua controlada por `CelestialLockBoard`.
- `ShipPanel/Painel/Luz_Fios`: ao lado da gaveta de fios, com cerca de 39,9% do tamanho da lâmpada grande.
- `ShipPanel/Painel/Luz_Senha`: ao lado do teclado de senha, com o mesmo tamanho da lâmpada de fios.

As duas lâmpadas novas usam `PuzzleCompletionLight`, materiais independentes e o brilho existente de `PuzzleStatusGlow`. Começam vermelhas e passam a verde quando o respectivo Board está completo. A senha aguarda a confirmação em **Ok**; reiniciar o Board restaura o vermelho. Reativar a lâmpada recupera a cor do estado atual.

**Tools > Ship > Update Panel Status Lights** instala a instância do novo FBX, reaplica a geometria e os encaixes medidos nos quatro soquetes. A rotina verifica dimensões, topologia da superfície do botão final e coordenadas UV antes de gravar a malha derivada. `ShipPanel/Painel` é o contêiner de cena que preserva as referências dos minigames; seu filho `Painel_LP` referencia o novo FBX e usa a derivada como sobrescrita de malha.

Validação: executar `Tests/PanelStatusLightsUnityChecks.cs`, entrada `PanelStatusLightsUnityChecks.Run`, com Pipeline `run_script` em uma sessão nova de Play Mode. Verifica associações, tamanhos, ausência de colliders nas lâmpadas, vermelho/verde independente, ligação incorreta de fios, confirmação da senha, conclusão/reinício do cadeado e reativação da luz de fios. As capturas ficam em `Temp/PanelStatus-*-Pending.png` e `Temp/PanelStatus-*-Completed.png`. Sair de Play Mode após o teste.
