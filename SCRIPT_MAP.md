# Mapa de Scripts Centrais

## Orquestracao geral

### `Assets/SourceFiles/Scripts/MiniGameManager.cs`

Responsabilidade:

- ponto central do minigame;
- cria e mantem o fluxo principal em runtime;
- prepara arena, campo, limites, iluminacao e fallback de camera;
- encontra player e bot na cena;
- converte ambos em combatants;
- controla spawn da bola;
- desenha HUD atual via `OnGUI()`.

Dependencias principais:

- `ArenaBallService`
- `ArenaCombatant`
- `ArenaRuntimeRig`
- `ArenaPlayerShooter`
- `ArenaBotController`
- `ArenaThrowClipPlayer`
- `ArenaPlayerActionAnimator`

Pontos de atencao:

- depende fortemente dos nomes `Arena`, `PlayField`, `PlayerRobotScene`, `AIRobotScene`;
- concentra muita responsabilidade;
- mexer aqui pode impactar arena, camera, spawn, HUD e fluxo de combate ao mesmo tempo.

## Player

### `Assets/SourceFiles/Scripts/ThirdPersonController.cs`

Responsabilidade:

- movimento principal do personagem;
- pulo e double jump;
- block e roll;
- rotacao do personagem;
- rotacao da camera via `CinemachineCameraTarget`.

Pontos de atencao:

- mistura locomocao, estado de acoes e camera;
- e um script sensivel a regressao;
- ha sinais de customizacao incremental sobre a base original do Starter Assets.

### `Assets/SourceFiles/InputSystem/StarterAssetsInputs.cs`

Responsabilidade:

- ponte de input para movimento, look, pulo, sprint, block e roll;
- controle de cursor;
- consumo de eventos frame-a-frame para jump e roll.

Pontos de atencao:

- deve continuar sendo a fonte principal de input do controller;
- mudancas aqui tendem a impactar camera, controle e UI de pausa/configuracao.

### `Assets/SourceFiles/Scripts/ArenaPlayerShooter.cs`

Responsabilidade:

- tiro/arremesso do player;
- resolve ponto de mira usando camera;
- dispara projectile com delay sincronizado no throw.

Dependencias principais:

- `ArenaCombatant`
- `ArenaThrowClipPlayer`
- `ThirdPersonController`
- `ArenaProjectileFactory`

### `Assets/SourceFiles/Scripts/ArenaPlayerActionAnimator.cs`

Responsabilidade:

- aplica poses adicionais de block, roll e double jump;
- manipula ossos em `LateUpdate`.

Pontos de atencao:

- qualquer erro aqui pode deformar o personagem visualmente;
- deve sempre restaurar pose base antes de aplicar offsets.

### `Assets/SourceFiles/Scripts/CameraSensitivityMenu.cs`

Responsabilidade:

- menu persistente de sensibilidade da camera;
- toggle de inverter eixo Y;
- controle de cursor e pausa do tempo ao abrir.

Pontos de atencao:

- persiste entre cenas;
- interage diretamente com `ThirdPersonController` e `StarterAssetsInputs`;
- se quebrar, afeta experiencia de camera e foco/cursor.

## Combate e estado comum

### `Assets/SourceFiles/Scripts/ArenaCombatant.cs`

Responsabilidade:

- estado compartilhado de player e bot;
- vida, bola em maos, respawn, block e pontos de anexacao;
- dano e knockback.

Dependencias principais:

- `ArenaKnockbackMotor`
- `ThirdPersonController`
- `MiniGameManager`

Pontos de atencao:

- ainda e um ponto central de estado compartilhado e deve ser alterado com cuidado;
- o respawn agora usa API explicita do `ThirdPersonController` para reset de movimento, em vez de reflection.

### `Assets/SourceFiles/Scripts/ArenaKnockbackMotor.cs`

Responsabilidade:

- aplica impulso/knockback no combatant.

### `Assets/SourceFiles/Scripts/ArenaThrowClipPlayer.cs`

Responsabilidade:

- sincroniza o timing visual/animado do arremesso.

## Bot / IA

### `Assets/SourceFiles/Scripts/ArenaBotController.cs`

Responsabilidade:

- IA do bot;
- navegacao simples no campo;
- decisao entre aproximar, strafar, recuar, recuperar e finalizar;
- percepcao, line of sight e arremesso.

Dependencias principais:

- `ArenaCombatant`
- `ArenaThrowClipPlayer`
- `CharacterController`

Pontos de atencao:

- bastante logica num unico script;
- risco de regressao comportamental alto;
- existe warning atual de campo nao usado (`preferredRange`);
- esse warning e pequeno, mas indica inconsistencia entre tuning exposto no Inspector e logica real usada pela IA.

## Bola e projeteis

### `Assets/SourceFiles/Scripts/ArenaBallService.cs`

Responsabilidade:

- servico central da bola solta;
- spawn, limpeza e registro da bola da arena;
- criacao da representacao visual da bola.

### `Assets/SourceFiles/Scripts/ArenaBallPickup.cs`

Responsabilidade:

- comportamento da bola coletavel no chao;
- bobbing/rotacao;
- entrega da bola ao combatant proximo.

### `Assets/SourceFiles/Scripts/ArenaProjectile.cs`

Responsabilidade:

- comportamento do projetil arremessado;
- acerto em combatants;
- transicao de projetil para pickup no chao.

### `Assets/SourceFiles/Scripts/ArenaProjectileFactory.cs`

Responsabilidade:

- fabrica o projetil da bola com rigidbody, collider e material fisico.

### `Assets/SourceFiles/Scripts/ArenaBallistics.cs`

Responsabilidade:

- utilitario de balistica/calculo relacionado aos arremessos.

## Setup runtime

### `Assets/SourceFiles/Scripts/ArenaRuntimeRig.cs`

Responsabilidade:

- liga/desliga controller, input e camera conforme player ou bot;
- administra camera principal e audio listener.

Pontos de atencao:

- afeta camera global da cena;
- qualquer mudanca aqui pode quebrar visao do player ou audio.

## Scripts legados / secundarios

### `Assets/SourceFiles/Scripts/Pickup.cs`

Responsabilidade:

- sistema anterior de colecionaveis.

### `Assets/SourceFiles/Scripts/UpdateCollectibleCount.cs`

Responsabilidade:

- atualiza UI do fluxo de colecionaveis antigos.

### `Assets/SourceFiles/Scripts/RespawnPlayer.cs`

Responsabilidade:

- fluxo anterior de respawn do personagem;
- hoje o `MiniGameManager` desabilita esse comportamento no player/bot do minigame.

## Observacoes gerais

- O minigame atual e fortemente orientado por runtime setup em vez de prefabs dedicados ja prontos para combate.
- `MiniGameManager` e `ThirdPersonController` sao os dois arquivos mais sensiveis para alteracoes.
- `ArenaCombatant` ainda tem um ponto fragil de reflection e deve ser tratado como inconsistencia estrutural.
