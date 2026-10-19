# Janela e controles do painel

Cena: `Assets/Scenes/ControllerScene.unity`.

- `ShipPanel/CockpitWindow` usa `Assets/Art/Panel/janela.fbx`, com material `M_CockpitGlass` e shader URP `Ship/Cockpit Glass`. O vidro tem uma leve tonalidade, reflexão de ambiente, brilho e efeito Fresnel nas bordas. Não projeta sombras nem possui collider que intercepte cliques.
- As três alavancas usam `Assets/Art/Panel/new2/Alavanca.fbx`, mantendo os pivôs, quatro posições, áreas de clique, marcas e lógica existentes. A escala vertical compensa a diferença de altura entre as exportações.
- `Assets/Prefabs/LightSequenceButton.prefab` usa `Assets/Art/Panel/new2/Botão luz.fbx`. O modelo foi orientado e dimensionado para a área existente. Os oito símbolos zodiacais continuam separados, com seus sprites e associação aos IDs preservados. O botão e o símbolo se movimentam juntos ao pressionar. O renderer novo recebe as cores de acerto e erro pelo componente existente.

Para reaplicar em Edit Mode: `Tools > Ship > Apply Window and New Controls`. Para verificar as referências: `Tools > Validation > Check Window and New Controls`. A ferramenta só altera a cena quando chamada explicitamente.

`Tests/PanelVisualsUnityChecks.cs` pode ser executado em Play Mode pelo comando `run_script` do Unity Pipeline, com entrada `PanelVisualsUnityChecks.Run`. Verifica os pivôs, quatro posições, conclusão das alavancas, animação dos botões e símbolos, iluminação individual, solução da sequência e ausência de collider no vidro.
