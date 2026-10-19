# Cadeado de símbolos celestes

Protótipo 3D montado em `ControllerScene` em 18/10/2026, a partir do esboço aprovado. Nome interno: `CelestialLock`.

![Captura real do protótipo no Unity](Images/CelestialLock-Prototype.png)

## Interação

- Quatro roletes independentes, com lua, estrela, sol e Saturno. Clique avança um símbolo: lua → estrela → sol → Saturno → lua.
- Um giro leva inicialmente 0,18 s; novos cliques nesse rolete ficam bloqueados durante seu giro. Outros roletes podem girar simultaneamente. A linha dos dois ponteiros laterais identifica a seleção.
- O botão físico `CONFIRM` verifica somente a combinação completa. Não aceita confirmação enquanto houver giro em andamento.
- Uma tentativa incorreta conserva os quatro símbolos; o botão fica vermelho brevemente (0,35 s), sem reset nem penalidade de tempo. Esse é o feedback inicial do protótipo.
- Acerto deixa o botão verde, bloqueia a interação e dispara `On Completed` uma única vez. Sair e retornar ao painel ou desativar/reativar o objeto preserva o estado. Um giro pendente termina na posição selecionada ao desativar o minigame.
- `pSphere5` é o indicador do cadeado: vermelho desde a cena salva e durante o estado incompleto, verde ao concluir, com as mesmas cores e intensidade de emissão das alavancas. Reiniciar restaura o vermelho. Material próprio `Assets/Art/Materials/M_CelestialLockStatus.mat`; a mudança em runtime usa `MaterialPropertyBlock` e preserva o material vermelho original.
- A interação usa raycasts 3D e Input System, somente com a câmera focada no cadeado. Escape retorna pela implementação existente da câmera.
- `GameSession` exige a conclusão do cadeado para a vitória, junto aos outros quatro minigames. A validação anterior de Play Mode confirmou seu registro; não simulou uma partida completa resolvendo os cinco minigames.

## Canvas e hierarquia

```text
Minigames/CelestialLock                  CelestialLockBoard
  Canvas                                World Space, área de 0,9 × 0,9
    LockAssembly
      Backing, Recess, guards, pointers  Geometria 3D
      Roller_1 ... Roller_4              Colliders fixos
        Roller/Drum                     Instâncias do prefab, malha cilíndrica e textura
      CipherPlate, CipherWord_1 ... 4    Uma palavra em cada linha
      ConfirmButton/MovingVisual        Botão 3D e rótulo CONFIRM
      CelestialLockClickArea            Aproximação no overview
CameraPositions/CelestialLock           Enquadramento do novo painel
```

O GameObject fica em `Minigames/CelestialLock`. Seu Canvas ocupa uma área de 0,9 × 0,9. O modelo cabe nessa área; a câmera principal e os alvos dos demais minigames não foram reposicionados.

## Campos editáveis e pendências

Selecione `Minigames/CelestialLock`, componente `Celestial Lock Board`:

- `Cipher Words`: quatro strings, uma palavra sem espaços por elemento: `USL`, `HQ²L`, `UCVSZL`, `UVCZ`. Correspondem a **SUN, MOON, SATURN, STAR**, nessa ordem, cifradas seguindo as linhas do diagrama fornecido. Em MOON, os dois O viram QQ, exibidos como `Q²`: `HQQL` → `HQ²L`. A placa tem quatro linhas largas; os rótulos podem ser editados visualmente, mas o Board aplica esses valores ao iniciar.
- `Correct Order`: sol, lua, Saturno, estrela, da esquerda para a direita. Confirmar com `CONFIRM`.
- `Initial Order`: Saturno, sol, estrela, lua. Reinício explícito restaura essa configuração.
- `Turn Duration`: duração de um avanço por clique.
- `On Completed`: evento disponível para integração. O gerenciador atual o acompanha automaticamente.
- `Status Light`: renderer de `pSphere5`; `Pending Color`, `Completed Color` e `Emission Intensity` controlam sua aparência durante a partida.

A cifra substitui cada letra pela outra ponta da mesma linha do diagrama. Cruzamentos não conectam linhas. Pares: A↔C, B↔D, E↔W, F↔I, G↔P, H↔M, J↔K, L↔N, O↔Q, R↔Z, S↔U, T↔V, X↔Y. A mesma operação cifra e decifra; antes de decifrar `HQ²L`, expandir `Q²` para `QQ`. A alteração atual aplica os quatro textos ao painel; **não altera `ManualScene`**. Ainda falta integrar o diagrama e a indicação de repetição com ² às páginas do manual no jogo.

## Assets

`Assets/Art/Minigames/CelestialLock` contém malhas, materiais URP, `Textures/DrumSymbols.png` e `Prefabs/Roller.prefab`. A textura tem quatro regiões, uma para cada símbolo; usa proporção compensada pela curvatura do cilindro. Os símbolos acompanham fisicamente o giro. Não são criadas texturas ou materiais em runtime.

`Tools > Minigames > Celestial Lock > Assemble` é apenas para montagem inicial e recusa um cadeado já existente. `Rebuild Symbol Texture` regenera a textura a partir dos desenhos do script, sobrescrevendo a textura gerada; não usar após pintar uma substituição manual sem guardar a versão desejada.

## Verificação

- `Tests/Test-CelestialLock.ps1`, em **PowerShell 7**: todas as 256 combinações, solução única, confirmação explícita, correção, wraparound, símbolos repetidos, cópia dos arrays, bloqueio após sucesso e reinício. Passou.
- `CelestialLockSetup.ValidateAndCapture`: verifica a área do Canvas, alvo de câmera e referências dos quatro roletes/linhas; captura pela câmera real da cena. A montagem inicial usa os valores de layout definidos no script.
- `Tests/CelestialLockUnityChecks.cs`, entrada `CelestialLockUnityChecks.Run` via Unity Pipeline em Play Mode: eventos sintéticos do Input System selecionam os colliders reais, entrada pelo overview, giro, bloqueio durante animação, confirmação física, persistência entre vistas e ao reativar, conclusão única, reinício e registro no `GameSession`. Confere também `pSphere5` vermelho inicialmente e após erro, verde no sucesso e após reativar, vermelho após reiniciar, emissão correspondente e preservação do material compartilhado vermelho.

Captura final inspecionada. A sensação dos cliques e o acabamento ainda precisam da avaliação manual do autor.
