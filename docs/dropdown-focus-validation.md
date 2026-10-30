# Correção dos seletores da barra — 0.2.2

30/10/2026, branch `fix/toolbar-dropdown-focus`.

## Causa e correção

Com uma HQ aberta, clicar em um seletor abria a lista e também propagava o evento Click do ToggleButton interno até o handler geral da barra. Esse handler solicitava retorno de foco à página. A operação assíncrona de foco fechava a lista antes de permitir interação. Não foi necessário alterar z-index, imagem ou estilo do popup.

O handler agora retorna foco somente para botões de ação (`Button`), excluindo o ToggleButton interno dos seletores. O callback de foco também respeita qualquer uma das duas listas abertas, inclusive quando uma ação anterior ainda está pendente. Escolher um item ou fechar com Esc continua devolvendo o foco à leitura.

O [ToggleButton do WPF](https://github.com/dotnet/wpf/blob/v10.0.0/src/Microsoft.DotNet.Wpf/src/PresentationFramework/System/Windows/Controls/Primitives/ToggleButton.cs) alterna o estado e depois propaga o clique. O [provider de automação](https://github.com/dotnet/wpf/blob/v10.0.12/src/Microsoft.DotNet.Wpf/src/PresentationFramework/System/Windows/Automation/Peers/ToggleButtonAutomationPeer.cs) sozinho apenas alterna o estado, sem essa propagação. Os testes anteriores, que abriam a lista pela propriedade, não cobriam essa diferença.

## Evidência

- RED: `ToolbarDropdownSmokeChecks` falhou com `ClassificationChoice closed immediately after its toggle click.` em `artifacts/dropdown-red/failure.txt`.
- GREEN: o teste usa o ToggleButton real do template, seu provider público e o evento Click roteado, depois aguarda o dispatcher. Confere lista aberta, popup visível em outra PresentationSource, escolha de item por eventos de mouse, fechamento com Esc e devolução de foco. Também cobre uma ação anterior da barra pendente enquanto o menu abre.
- Capturas de ambos os popups em `artifacts/dropdown-green` e `artifacts/dropdown-portable/cbz` foram inspecionadas: itens legíveis e disponíveis.
- Mutação temporária de `||` para `&&` na proteção dos seletores voltou a fechar a lista e foi detectada. Fonte original restaurada e solução recompilada. Evidência em `artifacts/dropdown-mutation/failure.txt`.
- 171 Core + 75 Integration passaram, sem skips. Build Release com avisos como erros: zero avisos/erros. Format verify e diff check passaram. Nenhuma dependência nova, supressão ou teste removido.
- Portátil em `artifacts/dropdown-portable`: CBZ com duas inicializações, RAR4, RAR5 e sólido passaram, mantendo hashes das fontes; retomada de leitura, preferências e módulos incorporados conferidos. O novo teste de interação dos menus executa no CBZ sintético com a HQ aberta.

## Entrega e revisão

Executáveis em `artifacts/distribution`: portátil 0.2.2, SHA256 `c5819c5a7ba990b6f029a093bd55ef40455edee1044df8028b028f42a2383279`; instalador, SHA256 `7df2d38c4c201cfe24205d056874d14a2bc3f9ff53072889ed1f65bc8cc319e1`. O instalador foi compilado; instalação/desinstalação não foram repetidas, pois o script não mudou. Versões anteriores preservadas.

Revisados origem do evento, ciclo de foco, ações assíncronas pendentes, seleção/cancelamento, legibilidade e escopo. Sem bloqueadores. A automação não movimenta o cursor físico: reproduz a sequência de ativação e eventos do WPF na janela real. Os limites humanos de suavidade e DPI continuam no roteiro existente. Não houve merge, tag ou publicação externa da 0.2.2.
