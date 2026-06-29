# Projeto: Setup Guide In-Editor Tutorial

## Objetivo de trabalho

Ao atuar neste projeto, a prioridade e o padrão esperado são:

- escrever codigo limpo, claro e defensavel;
- manter baixo acoplamento entre componentes;
- fazer a implementacao da forma mais correta possivel;
- modificar o minimo de coisas necessarias para resolver o problema;
- evitar workarounds quando existir uma solucao estrutural melhor;
- preferir assets, prefabs, materiais e fluxos oficiais do projeto quando eles ja existirem.

## Referencias do projeto

- Consultar `PROJECT_MAP.md` para o mapeamento rapido de prefabs, roots de cena, arena, player, bot e UI.
- Consultar `SCRIPT_MAP.md` para responsabilidades dos scripts centrais.
- Consultar `TEST_FLOW.md` para o fluxo padrao de validacao manual no Editor.

## Regras de implementacao

- Sempre preferir a menor mudanca correta, em vez de refatoracoes amplas sem necessidade.
- Sempre que houver duas abordagens, preferir a mais desacoplada e com menor risco de regressao.
- Nao introduzir gambiarra se houver um asset, prefab, material, configuracao ou fluxo correto ja existente no projeto.
- De preferencia, replicar coisas que ja existem no projeto e que atendam a necessidade, em vez de criar algo totalmente novo do zero.
- Quando a escolha for entre reutilizar diretamente algo compartilhado ou replicar uma estrutura existente, preferir replicar se isso reduzir o risco de quebrar outras partes do projeto.
- Nao modificar componentes reutilizados/globalmente compartilhados sem necessidade clara; considerar criar uma variante/duplicacao controlada quando isso for mais seguro.
- Nao duplicar logica sem necessidade.
- Nao criar solucoes genericas demais para problemas locais, a menos que isso realmente reduza complexidade.
- Preservar o comportamento existente que nao faz parte da tarefa.
- Em Unity, preferir mexer na origem correta do problema:
  - asset/prefab/material oficial, quando existir;
  - configuracao/local de spawn correto;
  - dependencia correta entre componentes;
  - logica de runtime apenas quando isso for realmente o local certo.

## Qualidade tecnica esperada

- O codigo deve ficar legivel e facil de manter.
- O codigo deve evitar acoplamento desnecessario entre gameplay, UI, input e setup de cena.
- O codigo deve evitar efeitos colaterais escondidos.
- O codigo deve evitar depender de nomes frageis, strings arbitrarias ou heuristicas fracas se houver referencia mais confiavel.
- O codigo deve evitar hardcode desnecessario quando o projeto ja possui asset ou configuracao apropriada.

## Pesquisa e validacao

- Se houver duvida tecnica relevante sobre a melhor forma de implementar, pesquisar antes de decidir.
- Se a melhor pratica puder ter mudado, confirmar antes de assumir.
- Se a tarefa envolver API, comportamento do Unity, pacote, padrao tecnico ou integracao com chance real de ambiguidade, investigar antes.

## Revisao critica obrigatoria

Ao trabalhar neste projeto, sempre:

- apontar inconsistencias no codigo encontrado;
- apontar gambiarras existentes quando perceber;
- sinalizar riscos de manutencao, acoplamento ou regressao;
- ficar atento a erros, warnings e logs;
- informar quando um problema aparente for sintoma de uma causa mais estrutural;
- diferenciar claramente entre:
  - correcao correta;
  - workaround;
  - solucao temporaria.

## Validacao minima ao finalizar mudancas

Sempre que viavel:

- compilar o codigo afetado;
- verificar erros;
- verificar warnings relevantes;
- chamar atencao para logs suspeitos;
- informar o que foi validado e o que nao foi possivel validar.

## Comunicacao esperada

- Ser direto e tecnico.
- Nao mascarar problema com solucao fraca sem deixar isso explicito.
- Se houver tradeoff, explicar de forma objetiva.
- Se existir uma alternativa melhor que a pedida, registrar isso com clareza.
