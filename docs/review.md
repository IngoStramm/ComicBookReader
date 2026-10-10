# Revisão de implementação

Revisão local em 10/10/2026: correção, clareza, fronteiras, entradas e recursos. Não equivale a revisão independente nem aprovação humana para merge/lançamento. A avaliação perceptual e de outro DPI permanece em [manual-validation.md](manual-validation.md).

## Achados tratados

| Achado | Correção e evidência |
| --- | --- |
| Cache existia sem participar de `ComicOpener` | Aberturas normais usam `CachedComicSession`; `OpenerCacheTests` falhou com duas decodificações antes da mudança e passou com uma. |
| Ajuste de largura durante loading reativava leitura | Operações geométricas não alteram o estado durante loading; reprodução RED/GREEN em `ReaderLoadingTests`. |
| Decoder que ignorava cancelamento podia publicar pixels | Token é conferido após decodificar na abertura/navegação; leases descartados e overview anterior preservado. `ReaderNavigationTests` e `ReaderLoadingTests`. |
| Progresso de materialização não chegava à UI | Estado recebe progresso da geração atual; resultado antigo não o altera. `ReaderLoadingTests` e tratamento visual da janela. |
| Entrada auxiliar do RAR sólido podia exceder o limite | Limite por entrada também cobre dados não exibidos, nos metadados e na leitura incremental. RED retornava NoImages, GREEN retorna ResourceLimit. |
| Controles cobriam permanentemente a imagem | Overlay temporário acessível por mouse/Tab/Alt; smoke WPF observou a falha antes da mudança. |
| Snapshot completo de preferências a cada quadro | Captura consolidada em 300 ms; flush antes de navegar/abrir/fechar. Nenhuma cópia de pixels no caminho de rolagem. |
| Fechamento podia entrar novamente durante flush | Fechamento pendente impede outra entrada, limpa input e aguarda saves/recursos antes de liberar a janela. |
| Restore bloqueado não incluía RID das bibliotecas | `RuntimeIdentifiers=win-x64` comum; lockfiles regenerados sem alterar versões; publicação bloqueada passou. |
| Stackalloc em loop e inconsistências de formato | Buffer fora do loop; formatador da solução aplicado em commit separado; build com warnings como erros e format passam. |
| Page Down entregue por WPF como `Next` | Nome canônico explícito para Page Down/Page Up/Enter; Page Down falhou no smoke antes da correção e Page Down/Page Up passaram depois. |
| Espaço ainda não descia/avançava | Operação no Core com sobreposição e limites; teste de API ausente e smoke de tecla falharam antes, passaram depois. Atalhos personalizados existentes têm prioridade. |
| Foco continuava no seletor de classificação | Fechamento devolve foco ao viewport; clique na imagem também. Smoke reproduziu foco perdido antes da correção. |
| Origem da tecla podia ser um filho de controle editável | Guard percorre ancestrais e reconhece itens do dropdown. Smoke com tecla no ComboBoxItem falhou antes da correção e passou depois. |

## Fronteiras e posse

A revisão folha adicional de foco/preferências apontou interceptação de Espaço em botão focado e ausência de reabertura pelo mouse: corrigidas com deferência a ButtonBase e botão ☰. Atalhos globais funcionam no seletor, mas setas continuam locais durante edição. A restauração de foco dos botões foi consolidada no dispatcher, respeitando diálogos visíveis. IntegerInput tem provider RangeValue, limites e labels nos dois idiomas. O ícone é original, gerado de formas locais em 16/32/48/256 px; recurso WPF e ApplicationIcon usam o mesmo ICO. A política anterior de atalhos ainda permite Tab; associá-lo a um comando pode competir com a travessia de foco e não é recomendado. Não foi alterada a compatibilidade de perfis antigos para essa questão preexistente.

Core não referencia WPF/SharpCompress/SkiaSharp. Arquivos e imagens externos entram pelos adapters. Assinatura ZIP/RAR decide o formato; senha/multivolume são rejeitados. Decodificação/extração ocorrem em background. Imagens inválidas conservam o índice; mensagens de interface usam o idioma escolhido, sem stack trace.

`PageImage` usa leases independentes. Cache adicional contabiliza stride × altura, mantém somente vizinhas e preserva consumidores após eviction/dispose. Prefetch cede a pedidos explícitos e não publica estado de leitura. Testes de fechamento bruto de ZIP/RAR usam `useCache:false` para preservar o contrato desses adapters; testes distintos cobrem cancelamento, descarte e prioridade do wrapper agora usado pelo aplicativo. Nenhum teste foi removido ou enfraquecido.

O RAR sólido escreve somente arquivos de nome gerado dentro da sessão privada. Recuperação exige marcadores do aplicativo, pasta filha de GUID, nomes esperados e ausência de reparse points. Limpeza não é recursiva e recusa conteúdo inesperado; sessões vivas mantêm lease exclusivo. Testes cobrem traversal, cache não pertencente ao app, limite, cancelamento e recuperação de resíduos.

Persistência mantém temporário, backup e arquivo corrompido para recuperação. Versão futura fica read-only. Um worker serializa snapshots, debounce e flush; falhas são avisadas e a leitura continua. HQs são abertas com acesso somente leitura; não há chamadas ao Registro/instalação do CDisplayEx, envio de HQs ou serviço externo.

## Evidência de teste e dependências

145 testes Core e 75 Integration passaram após a atualização de preferências/interface, sem skip. Build Release: zero avisos/erros. Format verify passou. Mutação de `image.ByteLength > budgetBytes` para `>=` fez três dos quatro testes de cache falharem; condição restaurada e os quatro passaram. Isso comprova sensibilidade desses testes à fronteira do orçamento, não um mutation score da solução.

Cinco lockfiles fixam versões e content hashes. Auditoria NuGet com `list ComicBookReader.slnx package --vulnerable --include-transitive --no-restore` não reportou vulnerabilidades conhecidas nas fontes consultadas em 10/10/2026. Isso não é prova de ausência de vulnerabilidades desconhecidas. Não foi feito upgrade de dependências.

SharpCompress/SkiaSharp e runtimes possuem licenças incluídas no pacote; xUnit fica fora da distribuição. As fixtures são redistribuíveis e suas licenças ficam no repositório. Nenhum recurso/binário do CDisplayEx foi incorporado.

## Resultado

Os achados concretos acima foram corrigidos. O pacote local pode ser avaliado. A aceitação completa continua pendente das verificações humanas listadas na matriz; não houve merge nem publicação externa.
