# Monitor dos minigames

`ControllerScene` contém `StatusMonitor` na raiz. O vidro usa uma cópia da malha curva original (`pCylinder4`), com coordenadas UV próprias. A moldura original permanece no painel.

As cinco linhas são Zodiac (Light Sequence), Wires, Password, Levers e Lock (Celestial Lock). Os nomes usam `#333333`; o estado mostra `FAIL` vermelho enquanto pendente e `OK` verde após a conclusão. Reiniciar um puzzle também atualiza o monitor.

No componente `StatusMonitor`, altere **Font**, **Name Color**, **Fail Color** e **OK Color**. Os textos ficam em **Display content**. Uma câmera auxiliar filma esse Canvas fora do cockpit e envia a imagem ao vidro por uma RenderTexture de 1024 × 640. O Canvas não recebe cliques. A prévia é atualizada também em Edit Mode.

`Tools > Ship > Validate Status Monitor` verifica as referências, as cinco transições independentes, o retorno a FAIL e as cores. O teste usa estados temporários de puzzles nos componentes reais e restaura os estados anteriores; não é uma partida completa em Play Mode. Capturas e resultado ficam em `Temp/MonitorAfter.png`, `Temp/MonitorContent.png` e `Temp/MonitorValidation.txt`.

![Monitor no painel](Images/StatusMonitor.png)
