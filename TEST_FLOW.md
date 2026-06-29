# Fluxo de Teste Manual no Editor

## Cena base para validar

- Abrir `Assets/Scenes/GetStarted_Scene.unity`

## Checklist minimo apos mudancas de gameplay

1. Entrar em Play Mode.
2. Confirmar que o `MiniGameManager` sobe sem erros no Console.
3. Confirmar que existem:
   - player controlavel;
   - bot ativo;
   - bola presente na arena;
   - HUD visivel.

## Teste de movimento e camera

1. Mover com `WASD`.
2. Girar camera com mouse.
3. Confirmar que a camera segue o comportamento esperado de eixo Y.
4. Abrir menu com `Esc`.
5. Testar:
   - slider de sensibilidade;
   - toggle de inverter eixo Y;
   - cursor desbloqueando ao abrir;
   - cursor voltando ao estado correto ao fechar.

## Teste de combate

1. Pegar a bola.
2. Arremessar com `botao esquerdo do mouse`.
3. Defender com `botao direito do mouse`.
4. Rolar com `Ctrl`.
5. Pular e double jump com `Space`.
6. Confirmar que:
   - o arremesso sai do personagem corretamente;
   - o hit no bot reduz HP;
   - block reduz dano/impacto;
   - roll nao quebra locomocao;
   - double jump nao quebra animacao/pose.

## Teste de bot

1. Confirmar que o bot:
   - anda;
   - busca a bola quando nao tem;
   - arremessa quando armado;
   - reaparece apos derrota.
2. Confirmar que o visual do bot esta correto:
   - cor/material esperado;
   - camera do bot nao assumindo `MainCamera`;
   - sem efeitos colaterais visuais no player.

## Teste de arena

1. Confirmar que `Arena` e `PlayField` foram localizados corretamente.
2. Confirmar que:
   - player e bot nao caem para fora imediatamente;
   - paredes invisiveis do campo estao funcionando;
   - bola nao spawna fora do campo.

## Teste de debug atual

1. `F2`:
   - alterna estado do inimigo.
2. `F3`:
   - recupera a bola para o player.

## Console

Sempre verificar no Console:

- erros (`Errors`);
- warnings novos;
- logs repetitivos por frame;
- null references;
- mensagens suspeitas de camera, input, prefab ou material.

## Quando a mudanca for visual

Validar tambem:

- prefab/material correto usado em vez de workaround;
- se a mudanca afetou apenas player ou apenas bot, conforme esperado;
- se nao houve impacto em assets compartilhados.

## Quando a mudanca for estrutural

Validar tambem:

- recompilacao sem erros;
- warnings novos introduzidos;
- se a mudanca mexeu em script central (`MiniGameManager`, `ThirdPersonController`, `ArenaCombatant`, `ArenaRuntimeRig`), fazer o checklist completo.

