# Audio

## Musica do menu

`MainMenu.unity` usa `MenuMusic` no objeto `MenuManager`, com o arquivo
`Assets/Audio/title.mp3`. A primeira reproducao comeca em 0s; depois, repete
somente o trecho de 25,6s ate o fim. `Loop Start Seconds` e `Volume` podem ser
ajustados no Inspector. A musica para ao sair da cena.

O clipe usa PCM e `Decompress On Load`: o componente extrai o trecho de loop
em amostras e agenda a transicao com o relogio DSP. O trecho extra ocupa memoria
enquanto o menu esta aberto e e liberado ao destruir o componente.

## Audio do jogo

Na cena `Assets/Scenes/ControllerScene.unity`, o primeiro objeto da hierarquia e
`Sound Manager`. Todos os clipes e volumes sao configurados no componente
`SoundManager` desse objeto:

- **Light Sequence Key**: pressionar uma tecla interativa da sequencia de luzes.
- **Password Key**: pressionar qualquer tecla interativa da senha, incluindo
  limpar e confirmar.
- **Puzzle Completed**: concluir um dos cinco minijogos, junto ao feedback visual.
- **Victory**: vencer a partida, quando todos os minijogos estao concluidos antes do fim do tempo.
- **Music**: musica de fundo, iniciada automaticamente e repetida em loop.

Cada entrada tem `Clip` e `Volume`. `Effects Volume` controla os efeitos em
conjunto; `Music Volume` controla a musica separadamente. `Puzzle Completed`
usa `Assets/Audio/success.mp3` e `Victory` usa `Assets/Audio/victory.mp3`.
As teclas usam `keystroke.flac`; `Music` continua sem clipe atribuido.
Sem um clipe atribuido, a entrada fica silenciosa. As fontes dos dois efeitos
do Pixabay estao registradas no README.

Os prefabs nao armazenam clipes ou referencias a objetos da cena. As teclas
chamam `SoundManager.Play(GameSound.LightSequenceKey)` ou
`SoundManager.Play(GameSound.PasswordKey)`; os paineis chamam
`SoundManager.Play(GameSound.PuzzleCompleted)` apenas na conclusao. Novos
consumidores usam a mesma API. O som de tecla acontece no pressionamento, sem
repeticao ao segurar ou ao soltar e sem tocar quando a interacao esta bloqueada.

O manager cria duas fontes 2D no proprio objeto: uma de efeitos com
`PlayOneShot`, permitindo sobreposicao, e uma de musica em loop. A camera da
cena ja possui `AudioListener`. Ao sair ou reiniciar a cena, as fontes sao
destruidas com ela; nao ha managers persistentes acumulados. A musica esta
do jogo e configurada na cena de jogo; a musica do menu usa o componente acima.
Sons de erro e aviso de tempo ainda
dependem de autorizacao.
