# Mapeamento Principal do Projeto

## Cena principal

- Cena principal de trabalho atual: `Assets/Scenes/GetStarted_Scene.unity`
- Cena secundaria relevante: `Assets/Scenes/SampleScene.unity`

## Player

- Prefab principal do personagem: `Assets/Prefabs/PlayerRobot.prefab`
- Cópia em bootstrap/resources: `Assets/Resources/Bootstrap/PlayerRobot.prefab`
- Nome raiz do prefab: `PlayerRobot`
- Objetos importantes dentro do prefab:
  - `PlayerFollowCamera`
  - `RobotCamera`
- Scripts centrais ligados ao player em runtime pelo `MiniGameManager`:
  - `ThirdPersonController`
  - `StarterAssetsInputs`
  - `ArenaPlayerShooter`
  - `ArenaPlayerActionAnimator`
  - `ArenaThrowClipPlayer`
  - `ArenaCombatant`
  - `ArenaRuntimeRig`

## Bot

- Na cena `GetStarted_Scene`, o bot aparece como instancia raiz `AIRobotScene`
- O player aparece como instancia raiz `PlayerRobotScene`
- O fluxo atual do minigame encontra esses dois roots na cena e converte ambos em combatants via `MiniGameManager`
- O bot usa a mesma base estrutural do personagem e depois recebe configuracao de IA em runtime:
  - `ArenaBotController`
  - `ArenaCombatant`
  - `ArenaThrowClipPlayer`
  - `ArenaRuntimeRig`
- Material/pacote visual base do dummy:
  - azul: `Assets/Kevin Iglesias/Human Character Dummy/Materials/HumanDummy_Blue.mat`
  - vermelho: `Assets/Kevin Iglesias/Human Character Dummy/Materials/HumanDummy_Red.mat`
- Prefabs do pacote de personagem com variantes prontas:
  - `Assets/Kevin Iglesias/Human Character Dummy/Prefabs/HumanDummy_M Blue.prefab`
  - `Assets/Kevin Iglesias/Human Character Dummy/Prefabs/HumanDummy_M Red.prefab`

## Arena

- Root principal da arena na cena:
  - `Arena`
- Root principal do campo:
  - `PlayField`
- O `MiniGameManager` depende explicitamente desses nomes para:
  - localizar a arena;
  - localizar o playfield;
  - expandir a escala horizontal da arena;
  - reconstruir colisores e paredes de limite;
  - resolver spawn de player, bot e bola.

## UI

- Prefab principal de UI identificado:
  - `Assets/UI/Remaining_Collectibles_UI.prefab`
- Nome raiz do prefab:
  - `Remaining_Collectibles_UI`
- Estrutura principal:
  - `Canvas`
  - texto TMP com label inicial `Collectibles remaining: X`
- Observacao:
  - essa UI parece vir do fluxo anterior de colecionaveis e nao do HUD atual do minigame.
  - o HUD atual do minigame esta sendo desenhado por `OnGUI()` em `MiniGameManager.cs`.

## Fluxo atual do minigame

- `MiniGameManager` e criado automaticamente antes da cena carregar.
- Ao preparar a arena, ele:
  - acha `Arena` e `PlayField`;
  - ajusta limites e collider do campo;
  - remove colecionaveis antigos;
  - encontra `PlayerRobotScene` e `AIRobotScene`;
  - configura player e bot em runtime;
  - cria a bola da arena;
  - garante camera fallback se necessario.

## Ponto de atencao

- `MiniGameManager` depende de nomes concretos de objetos de cena (`Arena`, `PlayField`, `PlayerRobotScene`, `AIRobotScene`).
- Se esses nomes mudarem, o fluxo atual pode quebrar.
- Sempre que possivel, ao mexer nesse fluxo, preferir referencias mais robustas ou replicar o padrao existente com cuidado.

