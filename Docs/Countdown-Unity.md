# Cronômetro da partida

`GameSession` está no objeto `Main Camera` da `ControllerScene`. `Initial Seconds` começa em **600 segundos (10 minutos)**. A contagem começa ao entrar na cena, usa tempo não escalado e continua nas transições e nos cinco minigames.

O HUD é criado em Play Mode em `Main Camera/GameSessionHUD`: Canvas Screen Space Overlay, centralizado no topo, apenas com números vermelhos de sete segmentos em `MM:SS`, fundo escuro e escala de referência 1920 × 1080. A geometria dos dígitos dispensa uma fonte digital externa. `SevenSegmentDisplay` exige `CanvasRenderer` e desativa `useLegacyMeshGeneration` para que o Unity chame `OnPopulateMesh(VertexHelper)`. A verificação no editor reproduziu a ausência de `CanvasRenderer` que impedia a renderização. O HUD não captura cliques durante a partida.

## Aproximação e alertas

- `ArtistPlanet` est� ligado ao campo `GameSession/Planet` na cena. Cresce linearmente de di�metro **1 para 9 unidades em 600 segundos**: `D(t) = 1 + (8/600) � t`, ou **0,013333 unidade de di�metro por segundo** (raio: 0,006667/s). O multiplicador final continua 9� para manter o planeta grande.
- `Ship Geometry` cont�m os renderers da nave/painel, minigames e vidro do monitor. A posi��o do planeta respeita um plano al�m da maior proje��o dos bounds dessas pe�as na dire��o de aproxima��o, somando o raio atual e **0,5 unidade de folga**. Isso substitui a prote��o antiga de 0,05 at� apenas o piv� do painel, que permitia invadir a geometria ao redor. A valida��o no Unity amostra todos os 601 segundos (0 a 600) contra os 154 bounds configurados. Se a geometria da nave mudar, atualize as refer�ncias; elas s�o medidas ao iniciar a partida.
- A partir de **04:00**, entra um tremor leve da câmera, com transição de 5 segundos. `CameraController/Time Shake Degrees` controla a amplitude, inicialmente 0,18°. O deslocamento de rotação é removido antes do próximo movimento para evitar acumulação e manter as transições entre minigames.
- A partir de **01:00**, `ScreenWarningOverlay` pulsa vermelho em ciclos de 1,2 segundo, usando uma vinheta oval com degradê amplo e centro transparente. A região de cor máxima fica fora da tela, inclusive nos cantos; uma grade amostra a curva suave para eliminar as emendas diagonais dos quatro trapézios anteriores. A intensidade do pulso varia entre 6% e 48%, multiplicada pelo degradê. O overlay não captura cliques. Este alerta é visual; não adiciona som de sirene.
- Os limites são ajustáveis em `Shake Starts At` e `Red Warning Starts At`, em segundos restantes. Os efeitos usam a mesma contagem não escalada do HUD. Ao terminar a partida, o planeta para de crescer, o tremor é removido e a borda vermelha desaparece.

### Modelo do artista

O planeta ativo usa `Assets/Art/Planet/ArtistPlanet.prefab`, com o `Planeta.fbx` e a textura `Planeta_BC.png` fornecidos pelo artista, copiados sem alterações. `M_ArtistPlanet.mat` usa URP Unlit para preservar a pintura e a iluminação já presentes na imagem. O FBX permanece como modelo aninhado no prefab, com seus UVs e rotação de importação.

O modelo foi centralizado e normalizado para diâmetro de 1 unidade, preservando o tamanho inicial, a posição e os parâmetros de aproximação do planeta anterior. O `SphereCollider` centralizado, de raio 0,5, fica desabilitado: o cronômetro consulta seu raio, mas ele não intercepta cliques dos minigames. O planeta usa a camada Ignore Raycast.

O GameObject antigo `Planet` continua na `ControllerScene` com `m_IsActive: 0`; sua malha e seu material foram preservados. A referência do cronômetro aponta para `ArtistPlanet`.

Em `00:00`, `GameSession` encerra a partida uma vez, bloqueia a navegação e as interações dos minigames e mostra **Time's Up** e **The ship has crashed**, com botões **Try Again** e **Back to Menu**. Uma rotação hidráulica pendente é cancelada no término. Não há animação de colisão implementada.

Concluir os cinco minigames libera o botão à direita do monitor, sem encerrar a partida. O cronômetro, o planeta e os alertas continuam até o clique final. A superfície do botão fica azul, com emissão, halo e luz local variando suavemente em ciclos de 2 segundos, sempre acesa. É possível apertá-lo na visão geral ou no foco do monitor; interface de tela, transições e foco em outros minigames bloqueiam o clique. Se um puzzle for reiniciado, o botão volta ao estado original.

O clique válido antes do prazo encerra com **Congratulations!**, **You won the game!**, **Play Again** e **Back to Menu**. O t�tulo e os bot�es usam texto branco; **You won the game!** usa verde claro `#7ADB9E`. A derrota usa textos vermelhos. O prazo tem prioridade: em `00:00`, mesmo com todos os puzzles resolvidos, a partida termina em derrota. A verificação usa os estados `IsCompleted` dos cinco tabuleiros. Os eventos `On Time Expired` e `On Victory` podem receber efeitos pelo Inspector. Reiniciar recarrega a cena e restaura os 10 minutos e os puzzles. Todos os textos exibidos ao jogador devem estar em inglês, conforme o README.

`ShipPanel/Painel/CompletionButton` contém `ShipCompletionButton` e um MeshCollider da superfície original do botão. `Assets/Art/CompletionButton` guarda essa superfície e uma cópia do painel sem ela; o FBX do artista permanece intacto. **Tools > Ship > Set Up Completion Button** reconecta as referências. **Tools > Validation > Check Ship Completion In Play Mode** verifica bloqueios, prontidão, continuidade do tempo, brilho, raios físicos, vitória e expiração; execute em uma partida nova de teste, pois a verificação resolve os puzzles e encerra a partida.

Validação: `pwsh -NoProfile -File Tests/Test-Countdown.ps1` cobre os limites de segundos e minutos, arredondamento, expiração, frames longos e entradas inválidas. `dotnet build Assembly-CSharp.csproj -nologo --no-restore` verifica a compilação. `Tests/CountdownEffectsUnityChecks.cs`, executável via `CountdownEffectsUnityChecks.Run()` no Unity, verifica a malha real dos LEDs em 10:00 e 09:59, transparência das bordas, crescimento do planeta, limites de 4/1 minutos e pulso. A aparência na câmera e os cliques ainda precisam ser conferidos em Play Mode.

Quando os cinco sistemas est�o resolvidos, o monitor mostra **All systems check. Press the button!** abaixo de Lock. A frase � um filho edit�vel de `StatusMonitor/Display content` e fica oculta enquanto houver sistema pendente.

A tela final fica salva na raiz da cena em `ResultScreen`, desativada durante a partida. `Victory` e `Defeat` cont�m `Title`, `Subtitle`, `Replay` e `Menu`, todos edit�veis no Inspector antes do Play Mode. T�tulo: 88; subt�tulo: 56; bot�es: 30. `GameSession` referencia esses objetos e alterna os grupos; n�o cria a tela final nem define seu layout em runtime. Para visualizar no editor, ative `ResultScreen` e o grupo desejado.
