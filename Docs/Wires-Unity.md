# Combinação de fios — pane elétrica

## Estado

Especificação e esboço aprovados pelo autor em 17/10/2026, com detalhes de interação definidos em 18/10/2026. **Protótipo 3D implementado, montado em `ControllerScene` e validado no Unity em 18/10/2026.** Testes com eventos sintéticos do Input System passaram; a avaliação manual de sensibilidade e acabamento pelo autor continua pendente. O evento de sucesso está exposto, ainda sem integração a um gerenciador global de partida.

![Esboço aprovado do painel horizontal e dos fios](Images/Wires-Concept.png)

A imagem é referência de composição e volume, não um modelo 3D ou textura pronta. Os símbolos nela são aproximações; usar no painel e no manual os mesmos desenhos finais, mantendo a ambiguidade intencional. A imagem maior mostra uma solução; a bandeja no quadro menor mostra o estado inicial.

### Capturas do protótipo montado

![Protótipo 3D na cena: fios soltos na bandeja](Images/Wires-Prototype-Initial.png)

![Protótipo 3D na cena: solução conectada](Images/Wires-Prototype-Solved.png)

As dimensões e posições dos dois canvases do autor foram preservadas; por isso, o painel atual é mais alto que o desenho conceitual. O alvo de câmera foi criado com inclinação para evidenciar a profundidade. As capturas usam a câmera e iluminação da cena; não são imagens conceituais geradas.

## Painel e identificadores

Caixa horizontal, com o interior voltado para cima e câmera inclinada vista de cima. Base com espaço para curvas e cruzamentos dos cabos. Quatro fios visualmente iguais, cada um com duas pontas móveis, inicialmente numa caixinha lateral. Sem gameplay de abrir a tampa ou remover parafusos nesta versão.

| Identificador interno | Posição no esboço | Capacidade |
| --- | --- | --- |
| `fio_z` | Topo, centro | Duas pontas em dois encaixes visíveis da mesma porta |
| `fio_x` | Esquerda, topo | Uma ponta |
| `fio_infinito` | Esquerda, meio | Uma ponta |
| `fio_m` | Esquerda, base | Uma ponta |
| `fio_musica` | Direita, topo | Uma ponta |
| `fio_e` | Direita, meio | Uma ponta |
| `fio_p` | Direita, base | Uma ponta |

Esses nomes identificam portas, apesar do prefixo `fio_`. Não identificam os quatro cabos. O jogador vê os símbolos; não substituir a comunicação sobre símbolos por esses nomes internos na interface.

São sete portas lógicas e oito encaixes físicos. Todos ficam ocupados na solução. A proposta anterior de deixar uma porta vazia foi explicitamente descartada.

## Regras do manual e solução

- A porta X não pode estar ligada à porta música.
- A porta P precisa estar ligada à porta Z.
- A porta música precisa estar ligada à porta M.
- A porta Z deve estar ligada à porta infinito.
- Apenas Z aceita duas conexões.

Com quatro cabos de duas pontas e as capacidades acima, a solução é o conjunto de pares sem orientação: **P–Z, música–M, Z–infinito, X–E**. Permutar os cabos, inverter suas pontas ou trocar qual encaixe de Z recebe P/infinito não muda o resultado.

É permitido ligar as duas pontas de um mesmo cabo em Z. Isso ocupa seus dois encaixes, mas não satisfaz a solução. Não impedir essa montagem como forma de orientar o jogador. Nas demais portas, a capacidade de uma ponta já impede usar o mesmo terminal nas duas extremidades.

## Interação aprovada

1. Clicar e arrastar uma ponta solta move essa ponta; a outra permanece onde estava.
2. Arrastar uma ponta conectada retira-a do encaixe, liberando-o e permitindo reposicionamento. O cabo continua existindo e sua outra ponta permanece onde estava.
3. Encaixar **somente ao soltar o botão esquerdo do mouse**, se houver uma entrada livre dentro da tolerância. Aproximar ou passar sobre uma entrada enquanto o botão estiver pressionado nunca conecta a ponta. Os dois encaixes de Z são selecionáveis separadamente.
4. Passar sobre uma entrada ocupada não causa ação: não substitui outra ponta, não bloqueia nem encerra o arraste. Em Z, isso se aplica a cada encaixe físico; o outro continua disponível se estiver livre.
5. Permitir cruzamentos. Um cabo deve passar visualmente por cima do outro, sem atravessá-lo e sem exigir simulação física de corda. O comprimento aumenta ou diminui continuamente conforme a ponta é arrastada; não há limite de alcance do cabo como regra do puzzle.
6. Sair do painel e retornar preserva a montagem, inclusive após sucesso.

Não há iluminação de acerto, energização ou sinal de erro ao conectar cada ponta. Não há reset por erro nem botão de teste.

### Tolerância de encaixe e desempate

Detalhamento aprovado em 18/10/2026: definir uma tolerância inicial ajustável no Inspector, escolhida na implementação conforme a escala dos conectores. O autor avaliará a sensibilidade durante o teste; não há necessidade de nova aprovação antes de escolher esse valor.

Ao soltar, percorrer os encaixes em uma ordem estável e usar o primeiro que esteja livre e dentro da tolerância. Ignorar os ocupados. Se os dois encaixes de Z forem elegíveis, inclusive com distâncias exatamente iguais, usar o primeiro encontrado; não é necessário tratamento especial nem seleção obrigatória do mais próximo. Sem encaixe livre elegível, a ponta permanece solta.

A tolerância se refere à distância da ponta até o encaixe ao soltar, não ao comprimento do cabo.

## Validação e conclusão

Verificar a solução sempre que uma alteração deixar os quatro cabos com ambas as pontas encaixadas. Não verificar apenas os acertos individuais nem memorizar um fio como correto independentemente da montagem atual.

Se a montagem completa estiver incorreta, manter todos os fios no lugar, sem feedback de erro, permitindo retirar e mover pontas. Uma nova montagem completa deve ser verificada novamente.

Se estiver correta, marcar conclusão antes de disparar o evento de sucesso, emitir esse evento uma única vez e impedir novas alterações do jogador. Sair e voltar não desbloqueia. Esta é a aplicação da regra geral de conclusão do README.

## Montagem e assets implementados

Hierarquia na cena:

```text
Minigames/Wires                          WiresBoard
  GameCanvas                            Transform de referência do plano
    Panel                               Base e bordas em 3D
    fio_z                               Um módulo, dois conectores
    fio_x, fio_infinito, fio_m           Conectores à esquerda
    fio_musica, fio_e, fio_p             Conectores à direita
    Symbol_*                            Placas com textura PNG
    WiresClickArea                      Aproximação no overview
  WiresCanvas
    WireTray                            Caixinha 3D
    Cable 1 … Cable 4                    Instâncias do mesmo prefab
    WiresClickArea                      Aproximação pela bandeja
CameraPositions/Wires                   Alvo da aproximação
```

Os canvases continuam em World Space. A interação dos cabos usa colliders 3D e o Input System, não eventos de UI; seus `GraphicRaycaster` foram desativados. Entrar pelo painel ou pela bandeja aproxima ao mesmo alvo. Os demais minigames, posições dos canvases e configuração da câmera principal foram preservados.

| Local | Conteúdo / edição |
| --- | --- |
| `Assets/Art/Minigames/Wires/Meshes` | Malhas Unity `.asset` de base, anel do conector, cavidade, corpo e contato do plug, placa dos símbolos |
| `Assets/Art/Minigames/Wires/Prefabs` | `Panel`, `WireTray`, `Connector`, `DoubleConnectorZ`, `Plug`, `Cable`; alterar a fonte para propagar às instâncias |
| `Assets/Art/Minigames/Wires/Materials` | Materiais URP de metal, borracha, interior dos conectores, contato e símbolos; cores e texturas editáveis pelo Inspector |
| `Assets/Art/Minigames/Wires/Textures` | `BrushedMetal.png` e sete PNGs dos símbolos; substituir as imagens para revisar a arte sem alterar scripts |
| `Assets/Art/Minigames/Wires/WiresRules.asset` | Quatro pares de solução configurados no Inspector; IDs internos das portas |

Os símbolos são desenhos iniciais inspirados na referência fornecida, com ambiguidade intencional. O manual ainda precisa usar as mesmas imagens finais; este trabalho não alterou a cena do manual.

### Parâmetros no Inspector

Em `Minigames/Wires`, componente **WiresBoard**:

- `Surface`, `Sockets`, `Cables`, `Rules`, `Camera Controller`, `Camera Target` e `Interaction Camera`: referências já atribuídas. A ordem de `Sockets` determina o desempate; os dois encaixes de Z vêm primeiro.
- `Snap Tolerance`: **0.025** nas unidades locais do `GameCanvas`. Aumentar facilita soltar perto; diminuir exige mais precisão. Não limita comprimento.
- `Drag Threshold Pixels`: **4** pixels para distinguir clique de arraste. Um clique sem movimento não desconecta.
- `Loose Tip Depth`: **-0.038**, coordenada Z local usada durante o arraste; -Z é acima da superfície.
- `Loose End Clearance`: **0.034**. Ao soltar uma ponta sem encaixe exatamente sobre outro plug, ela fica acima dele para continuar visível e selecionável; não ocupa nem substitui a entrada.
- `On Completed`: evento emitido uma única vez, após bloquear as pontas. Conectar ao fluxo global quando estiver implementado.

No prefab **Cable**, componente **WiresCable**:

- `Radius`: **0.006**; `Segments`: **64**; `Sides`: **10**; `Slack`: **0.025**; `Crossing Clearance`: **0.002**.
- O material do objeto `Cable tube` controla o revestimento; o prefab `Plug` controla a aparência e o collider das pontas.
- Os cabos usam uma ordem estável de sobreposição, conforme o array `Cables`. A malha é recalculada quando as pontas, o plano ou os parâmetros mudam. Não há limite de comprimento nem criação de materiais/texturas durante a partida.

### Organização do código

- Separar a lógica de portas, ocupação e pares da apresentação Unity para validar as regras sem depender da geometria.
- `WiresSocket` identifica cada encaixe físico e seu ponto de entrada; duas instâncias compartilham `fio_z`.
- `WiresCable` gera tubos ao longo de curvas, com folga e ajuste visual de altura nos cruzamentos. Não usa física de corda.
- `WiresBoard` aceita interação apenas no alvo correto do `CameraController`. Retornar ao overview bloqueia interação e preserva as conexões. Sair durante um arraste ou perder foco cancela a captura sem encaixar automaticamente; pontas já retiradas ficam soltas na última posição.
- `WiresPuzzle` mantém a ocupação e valida todos os pares sem orientação, sem depender de Unity. O estado concluído é imutável para os métodos de interação.
- `Assets/Editor/WiresSetup.cs` é uma ferramenta de autoria: cria arquivos e atribui referências no Editor. Seus valores de criação não são usados para redefinir os materiais durante a partida. **Tools > Minigames > Wires > Create Assets and Assemble** serve para uma montagem inicial; recusa remontar um objeto com `WiresBoard` e reutiliza assets existentes, preservando suas edições.

## Ajustes a avaliar manualmente

Não são novas regras de solução; são ajustes de apresentação e sensibilidade do protótipo já implementado:

- Calibrar a tolerância inicial de encaixe e o tamanho das áreas de clique no protótipo; o comportamento ao soltar e o desempate já estão definidos acima.
- Conforto ao selecionar plugs e ao retirar uma ponta conectada. O arraste não fica limitado ao retângulo do painel/bandeja.
- Ordem visual e altura dos cabos nos cruzamentos, evitando interpenetração e símbolos encobertos.
- Escala e enquadramento na cena existente, especialmente se as dimensões dos canvases forem alteradas depois. Nesse caso, ajustar os objetos e o alvo `CameraPositions/Wires` pelo Editor.

## Validação executada

- `Tests/Test-Wires.ps1`: passou, compilando a lógica pura e enumerando as **40.320** permutações dos oito encaixes, com **768** soluções equivalentes. Também verifica Z–Z, ocupação, índices inválidos, correção, bloqueio e cópia defensiva da configuração.
- Compilação do projeto no Unity `6000.3.10f1`: passou sem erros.
- `Tests/WiresUnityChecks.cs`, executado via Pipeline `run_script` em Play Mode: passou com eventos sintéticos do Input System e raycasts reais das pontas. Confere os comportamentos abaixo e a distância entre os segmentos de cabos na solução testada.
- Capturas da câmera em Play Mode inspecionadas: estado inicial e solução, conforme imagens acima. Não surgiram novos erros ou avisos no console durante os testes.
- O Play Mode foi encerrado após os testes. A cena salva mantém os quatro cabos soltos na bandeja.

Para repetir a lógica, abrir um PowerShell novo na raiz e executar `./Tests/Test-Wires.ps1`. Para o teste Unity, entrar em Play Mode com `ControllerScene` no estado inicial e executar o arquivo `Tests/WiresUnityChecks.cs`, entrada `WiresUnityChecks.Run`, pelo comando Pipeline `run_script`; sair de Play Mode depois para descartar a montagem de teste.

Comportamentos cobertos:

- Aceitar a solução com cabos permutados, pontas invertidas e os encaixes de Z trocados.
- Permitir Z–Z sem concluir; impedir excesso de ocupação por encaixe.
- Ignorar entradas ocupadas durante o arraste, mantendo o movimento da ponta.
- Passar sobre entrada livre com o botão pressionado não conecta; soltar dentro da tolerância conecta. Em sobreposição de tolerâncias, usar o primeiro encaixe livre elegível em ordem estável, inclusive em Z.
- Montagem completa incorreta permanece editável e pode ser corrigida até o sucesso.
- Não emitir sucesso com pontas soltas; emitir uma única vez com a solução completa.
- Bloquear remoção e reconexão após sucesso, inclusive saindo e retornando ao painel.
- Preservar montagem entre aproximações da câmera e bloquear interação no overview/transições.
- A solução testada tem separação geométrica entre os tubos nos cruzamentos. Isso não constitui uma prova para todas as possíveis posições livres durante o arraste; avaliar também com o mouse. Testes da lógica, isoladamente, não comprovam os aspectos visuais.
