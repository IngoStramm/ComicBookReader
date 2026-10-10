# Tarefas: ComicBookReader

Status: 7/30 tarefas concluídas; verificação registrada em docs/build-log.md.

Contexto, decisões, comandos V-* e condição comum de conclusão: [plan.md](plan.md). Todos os caminhos são relativos à raiz do projeto. **S** = 1–2 arquivos; **M** = 3–5. T01 é preparação de ambiente com um documento de evidência, sem promessa de duração.

Os modelos indicados são recomendações para a execução. A raiz concentra decisões e validação; não há delegação ou troca automática de modelo. Cada marco apresenta evidência para revisão, sem acrescentar aprovações para operações rotineiras já autorizadas.

<a id="t01"></a>

## [x] T01: Preparar o SDK x64

**Descrição:** Disponibilizar o SDK .NET 10 x64 necessário para a implementação e confirmar a base de pacotes da spec. Esta tarefa só será executada na fase de construção.

**Critérios de aceitação:**

- [x] O executável x64 lista um SDK 10 estável; o host x86 do PATH não é usado.
- [x] Patch do SDK, arquitetura e versões escolhidas ficam registrados; qualquer incompatibilidade é resolvida antes do scaffold.

**Verificação:** [V-SDK](plan.md#v-sdk); conferir as fontes oficiais ao obter o SDK e registrar sua versão.

**Dependências:** Nenhuma.

**Arquivos prováveis:**

- `docs/decisions/0001-toolchain.md`

**Escopo estimado:** Ambiente (1 documento; depende da obtenção do SDK).

Roteamento: gpt-6.1-sol/high — raiz — Motivo: decidir a base técnica uma vez e evitar correções de ambiente em várias tarefas — Validação: evidências da verificação acima.

<a id="t02"></a>

## [x] T02: Fixar as configurações da solução

**Descrição:** Criar as configurações comuns e uma solução inicialmente vazia, com o patch validado em T01.

**Critérios de aceitação:**

- [x] global.json fixa o SDK; nullable, estilo e warnings como erros ficam definidos.
- [x] As versões da spec são centralizadas; a solução vazia é válida, sem projetos fictícios.

**Verificação:** [V-SDK](plan.md#v-sdk); [V-RESTORE](plan.md#v-restore) na solução; inspecionar os cinco arquivos.

**Dependências:** [T01](#t01).

**Arquivos prováveis:**

- `global.json`
- `Directory.Build.props`
- `Directory.Packages.props`
- `.editorconfig`
- `ComicBookReader.slnx`

**Escopo estimado:** M (5 arquivos).

Roteamento: gpt-6-luna/medium — raiz — Motivo: configurações delimitadas com requisitos já definidos — Validação: evidências da verificação acima.

<a id="t03"></a>

## [x] T03: Criar Core e Infrastructure com runners de teste

**Descrição:** Criar os dois projetos de biblioteca e os dois projetos executáveis de teste. Core não referencia WPF nem bibliotecas de adapters.

**Critérios de aceitação:**

- [x] Referências seguem Core ← Infrastructure; testes têm runner xUnit v3 executável.
- [x] Restore e build são válidos; não criar testes vazios para aparentar cobertura.

**Verificação:** [V-RESTORE](plan.md#v-restore); [V-BUILD](plan.md#v-build). Só executar testes quando houver casos reais em T05/T07.

**Dependências:** [T02](#t02).

**Arquivos prováveis:**

- `src/ComicBookReader.Core/ComicBookReader.Core.csproj`
- `src/ComicBookReader.Infrastructure/ComicBookReader.Infrastructure.csproj`
- `tests/ComicBookReader.Core.Tests/ComicBookReader.Core.Tests.csproj`
- `tests/ComicBookReader.Integration.Tests/ComicBookReader.Integration.Tests.csproj`
- `ComicBookReader.slnx`

**Escopo estimado:** M (5 arquivos).

Roteamento: gpt-6-luna/high — raiz — Motivo: montagem mecânica de projetos e referências — Validação: evidências da verificação acima.

### Marco após T01–T03: Base e runners

- [ ] Critérios do bloco atendidos e comandos aplicáveis registrados, sem regressões no caminho já disponível.
- [ ] SDK fixado; restore/build da base válidos. Sem testes fictícios.
- [ ] Evidência disponível para revisão; decisões/ajustes registrados antes de alterar comportamento confirmado ou escopo.

Roteamento: gpt-6.1-sol/high — raiz — Motivo: integrar a evidência do bloco e resolver dúvidas sem separar estado compartilhado — Validação: critérios e demonstração do marco.

<a id="t04"></a>

## [x] T04: Criar a entrada WPF

**Descrição:** Adicionar o aplicativo Windows à solução e iniciar/encerrar uma aplicação WPF mínima. A janela de leitura será entregue em T08.

**Critérios de aceitação:**

- [x] App usa net10.0-windows, alvo win-x64 para publicação e referências aos projetos corretos.
- [x] O processo inicia e encerra limpo, sem código de leitura provisório nem implementações que lançam NotImplementedException.

**Verificação:** [V-RESTORE](plan.md#v-restore); [V-BUILD](plan.md#v-build); [V-APP](plan.md#v-app) para verificar o ciclo de inicialização/encerramento.

**Dependências:** [T03](#t03).

**Arquivos prováveis:**

- `src/ComicBookReader.App/ComicBookReader.App.csproj`
- `src/ComicBookReader.App/App.xaml`
- `src/ComicBookReader.App/App.xaml.cs`
- `ComicBookReader.slnx`

**Escopo estimado:** M (4 arquivos).

Roteamento: gpt-6-luna/high — raiz — Motivo: scaffold WPF pequeno e verificável — Validação: evidências da verificação acima.

<a id="t05"></a>

## [x] T05: Estabelecer contratos e posse de imagem

**Descrição:** Materializar os contratos da spec usados pela primeira fatia: erros, metadados, sessão, buffer descartável e enum de classificação. Definir a propriedade do buffer sem tipos de terceiros. Incluir um contrato opcional para relatar progresso da abertura, independente de WPF, preservando a operação mínima da spec.

**Critérios de aceitação:**

- [x] BookId/PageKey têm identidade determinística; imagem valida dimensões, stride e validade de acesso.
- [x] Dispose idempotente e referências independentes permitem liberar uma imagem sem invalidar outra; cancelamento não é ReaderException.
- [x] Core não expõe tipos WPF, SkiaSharp ou SharpCompress.

**Verificação:** [V-CORE](plan.md#v-core); [V-BUILD](plan.md#v-build); revisar contratos contra SPEC-comic-files.

**Dependências:** [T03](#t03).

**Arquivos prováveis:**

- `src/ComicBookReader.Core/Contracts/ReaderErrors.cs`
- `src/ComicBookReader.Core/ComicFiles/ComicContracts.cs`
- `src/ComicBookReader.Core/ComicFiles/PageImage.cs`
- `src/ComicBookReader.Core/Preferences/PageClassificationOverride.cs`
- `tests/ComicBookReader.Core.Tests/ComicFiles/ComicContractsTests.cs`

**Escopo estimado:** M (5 arquivos).

Roteamento: gpt-6.1-sol/high — raiz — Motivo: contratos e posse compartilhados exigem decisões coerentes — Validação: evidências da verificação acima.

<a id="t06"></a>

## [x] T06: Indexar imagens de CBZ em ordem natural

**Descrição:** Implementar o índice ZIP somente para leitura, filtragem e ordenação natural dos caminhos completos, antes da sessão de decodificação.

**Critérios de aceitação:**

- [x] 1, 2, 10, subpastas, empates e números enormes têm ordem determinística; auxiliares são ignorados.
- [x] Arquivos não ZIP ou sem imagens têm erros específicos; entradas duplicadas recebem PageKeys distintos.

**Verificação:** [V-CORE](plan.md#v-core); [V-INTEGRATION](plan.md#v-integration); [V-BUILD](plan.md#v-build).

**Dependências:** [T05](#t05).

**Arquivos prováveis:**

- `src/ComicBookReader.Core/ComicFiles/NaturalEntryComparer.cs`
- `src/ComicBookReader.Infrastructure/Archives/ZipPageIndex.cs`
- `tests/ComicBookReader.Core.Tests/ComicFiles/NaturalEntryComparerTests.cs`
- `tests/ComicBookReader.Integration.Tests/Archives/ZipPageIndexTests.cs`

**Escopo estimado:** M (4 arquivos).

Roteamento: gpt-6.1-sol/high — raiz — Motivo: ordenação e identidade precisam tratar casos reais sem overflow — Validação: evidências da verificação acima.

### Marco após T04–T06: Contratos e índice

- [ ] Critérios do bloco atendidos e comandos aplicáveis registrados, sem regressões no caminho já disponível.
- [ ] Core independente de UI/adapters; testes reais de identidade, buffers e ordenação passando.
- [ ] Evidência disponível para revisão; decisões/ajustes registrados antes de alterar comportamento confirmado ou escopo.

Roteamento: gpt-6.1-sol/high — raiz — Motivo: integrar a evidência do bloco e resolver dúvidas sem separar estado compartilhado — Validação: critérios e demonstração do marco.

<a id="t07"></a>

## [ ] T07: Ler pixels de uma página CBZ

**Descrição:** Conectar índice, sessão e decoder em background. Fixtures de imagens/ZIP são produzidas nos testes a partir de conteúdo sintético; nenhum arquivo pessoal é necessário.

**Critérios de aceitação:**

- [ ] JPEG/PNG/WebP e EXIF normalizado retornam dimensões, BGRA32 premultiplicado e stride esperados. Validar tamanho codificado/dimensões antes de alocações grandes onde a API permitir.
- [ ] ReadPageAsync permite voltar a uma página anterior; página inválida mantém seu índice.
- [ ] Cancelamento e fechamento liberam stream/buffers; hash do CBZ permanece igual.

**Verificação:** [V-INTEGRATION](plan.md#v-integration); [V-BUILD](plan.md#v-build); conferir pixels conhecidos, não só sucesso de abertura.

**Dependências:** [T06](#t06).

**Arquivos prováveis:**

- `src/ComicBookReader.Infrastructure/Imaging/SkiaPageDecoder.cs`
- `src/ComicBookReader.Infrastructure/Archives/ZipComicSession.cs`
- `src/ComicBookReader.Infrastructure/Archives/ComicOpener.cs`
- `tests/ComicBookReader.Integration.Tests/Fixtures/SyntheticComicFactory.cs`
- `tests/ComicBookReader.Integration.Tests/Archives/ZipComicSessionTests.cs`

**Escopo estimado:** M (5 arquivos).

Roteamento: gpt-6.1-sol/high — raiz — Motivo: integração nativa, orientação e recursos exigem julgamento — Validação: evidências da verificação acima.

<a id="t08"></a>

## [ ] T08: Mostrar a primeira página em tela cheia

**Descrição:** Entregar o primeiro fluxo completo: janela escura, seletor Ctrl+O ou arrastar e soltar, leitura CBZ e pixels apresentados em WPF, com loading e erro compreensível.

**Critérios de aceitação:**

- [ ] Abrir CBZ por seletor ou arrastar e soltar mostra a primeira imagem sem bloquear a janela; fundo/margens seguem a spec.
- [ ] Primeira execução começa em tela cheia; F11/Esc e abertura cancelada funcionam.
- [ ] Abrir arquivo inválido conserva a imagem anterior; buffers substituídos são liberados.

**Verificação:** [V-BUILD](plan.md#v-build); [V-APP](plan.md#v-app): abrir CBZ sintético, cancelar seletor e tentar arquivo inválido; registrar resultado.

**Dependências:** [T04](#t04), [T07](#t07).

**Arquivos prováveis:**

- `src/ComicBookReader.App/MainWindow.xaml`
- `src/ComicBookReader.App/MainWindow.xaml.cs`
- `src/ComicBookReader.App/Presentation/BitmapPresenter.cs`
- `src/ComicBookReader.App/App.xaml.cs`
- `docs/manual-validation.md`

**Escopo estimado:** M (5 arquivos).

Roteamento: gpt-6.1-sol/high — raiz — Motivo: primeira integração entre arquivo, UI e ciclo de vida — Validação: evidências da verificação acima.

<a id="t09"></a>

## [ ] T09: Comprovar RAR4, RAR5 e RAR sólido

**Descrição:** Antecipar o risco CBR com três fixtures pequenas redistribuíveis. Provar o caminho de acesso aleatório e o Reader sequencial de RAR sólido antes de prometer suporte na interface.

**Critérios de aceitação:**

- [ ] Cada fixture tem procedência/licença, hash e conteúdo esperado registrados.
- [ ] RAR4/RAR5 podem ser lidos; o Reader percorre o sólido e permite materializar suas entradas.
- [ ] Se o adapter falhar, registrar a causa e revisar a decisão técnica; o requisito CBR não pode ser reduzido.

**Verificação:** [V-INTEGRATION](plan.md#v-integration); confirmar acesso à página anterior a partir dos dados materializados; conferir procedência.

**Dependências:** [T07](#t07).

**Arquivos prováveis:**

- `tests/fixtures/rar4.cbr`
- `tests/fixtures/rar5.cbr`
- `tests/fixtures/solid.cbr`
- `tests/fixtures/README.md`
- `tests/ComicBookReader.Integration.Tests/Archives/ArchiveCompatibilityTests.cs`

**Escopo estimado:** M (5 arquivos).

Roteamento: gpt-6.1-sol/high — raiz — Motivo: investigar cedo a compatibilidade com evidência de arquivos — Validação: evidências da verificação acima.

### Marco após T07–T09: Primeira leitura e risco CBR

- [ ] Critérios do bloco atendidos e comandos aplicáveis registrados, sem regressões no caminho já disponível.
- [ ] CBZ abre na janela; codecs e três tipos RAR têm evidência. Resolver qualquer incompatibilidade antes de ampliar a integração.
- [ ] Evidência disponível para revisão; decisões/ajustes registrados antes de alterar comportamento confirmado ou escopo.

Roteamento: gpt-6.1-sol/high — raiz — Motivo: integrar a evidência do bloco e resolver dúvidas sem separar estado compartilhado — Validação: critérios e demonstração do marco.

<a id="t10"></a>

## [x] T10: Calcular escala de página simples e dupla

**Descrição:** Implementar geometria pura da spec, incluindo centralização, offsets, visão completa e validação numérica. Não limitar a escala de leitura pela altura.

**Critérios de aceitação:**

- [x] Os cinco exemplos PL-02 conferem até 0,001 DIP, incluindo resoluções dobradas.
- [x] Paisagem é dupla; retrato/quadrado são simples; override prevalece sem cortar a imagem.
- [x] Offsets ficam nos limites e entradas inválidas são rejeitadas; visão completa contém a imagem.

**Verificação:** [V-CORE](plan.md#v-core); [V-BUILD](plan.md#v-build); usar exemplos da spec como oráculo independente.

**Dependências:** [T05](#t05).

**Arquivos prováveis:**

- `src/ComicBookReader.Core/Layout/LayoutContracts.cs`
- `src/ComicBookReader.Core/Layout/PageLayoutCalculator.cs`
- `tests/ComicBookReader.Core.Tests/Layout/PageLayoutCalculatorTests.cs`

**Escopo estimado:** M (3 arquivos).

Roteamento: gpt-6.1-sol/high — raiz — Motivo: geometria é pequena, mas define o comportamento central do leitor — Validação: evidências da verificação acima.

<a id="t11"></a>

## [ ] T11: Controlar carregamento e alvo de leitura

**Descrição:** Introduzir o controller testável e substituir o caminho inicial da janela por ele. Controller é a única fonte de estado, com geração de requests e descarte de resultados obsoletos. A janela passa a apresentar a geometria calculada pelo controller.

**Critérios de aceitação:**

- [ ] Estado diferencia alvo e página apresentada; scrolling fica suspenso durante loading.
- [ ] Apenas a geração atual apresenta pixels/erros; resultado antigo é descartado mesmo se ignorar cancelamento.
- [ ] Trocar livro/fechar libera recursos e mantém o livro anterior se a nova abertura falhar.

**Verificação:** [V-CORE](plan.md#v-core) com tarefas controladas; [V-INTEGRATION](plan.md#v-integration); [V-APP](plan.md#v-app): navegação/abertura concorrente e cancelamento.

**Dependências:** [T08](#t08), [T10](#t10).

**Arquivos prováveis:**

- `src/ComicBookReader.Core/Reading/ReaderState.cs`
- `src/ComicBookReader.Core/Reading/ReaderController.cs`
- `src/ComicBookReader.App/MainWindow.xaml.cs`
- `tests/ComicBookReader.Core.Tests/Reading/ReaderLoadingTests.cs`
- `tests/ComicBookReader.Integration.Tests/Reading/ReaderLifecycleTests.cs`

**Escopo estimado:** M (5 arquivos).

Roteamento: gpt-6.1-sol/xhigh — raiz — Motivo: concorrência e propriedade de buffers têm alto custo de retrabalho — Validação: evidências da verificação acima.

<a id="t12"></a>

## [ ] T12: Navegar por comandos separados

**Descrição:** Adicionar Page Up/Page Down com uma mudança por pressão física e indicadores de página. Centralizar a tradução de teclado, reservando as setas exclusivamente para rolagem.

**Critérios de aceitação:**

- [ ] Repetição do Windows não causa várias trocas por uma pressão; primeira/última página são limites.
- [ ] Página nova começa no topo/esquerda e reaplica layout; imagem dupla ocupa um índice.
- [ ] Teclas em diálogo/campo editável não navegam na HQ.

**Verificação:** [V-CORE](plan.md#v-core); [V-BUILD](plan.md#v-build); [V-APP](plan.md#v-app): segurar Page Down e testar limites/foco.

**Dependências:** [T11](#t11).

**Arquivos prováveis:**

- `src/ComicBookReader.App/Input/ReaderKeyRouter.cs`
- `src/ComicBookReader.App/MainWindow.xaml`
- `src/ComicBookReader.App/MainWindow.xaml.cs`
- `src/ComicBookReader.Core/Reading/ReaderController.cs`
- `tests/ComicBookReader.Core.Tests/Reading/ReaderNavigationTests.cs`

**Escopo estimado:** M (5 arquivos).

Roteamento: gpt-6.1-sol/high — raiz — Motivo: navegação depende do mesmo estado usado pelo loading — Validação: evidências da verificação acima.

### Marco após T10–T12: Geometria e navegação

- [ ] Critérios do bloco atendidos e comandos aplicáveis registrados, sem regressões no caminho já disponível.
- [ ] Dupla recebe compensação correta; loading antigo não sobrescreve a página; navegação está separada.
- [ ] Evidência disponível para revisão; decisões/ajustes registrados antes de alterar comportamento confirmado ou escopo.

Roteamento: gpt-6.1-sol/high — raiz — Motivo: integrar a evidência do bloco e resolver dúvidas sem separar estado compartilhado — Validação: critérios e demonstração do marco.

<a id="t13"></a>

## [ ] T13: Calcular rolagem contínua no Core

**Descrição:** Adicionar direção pressionada e avanço temporal independente de eventos de repetição do teclado. Implementar clamp, diagonal, oposição e pausa.

**Critérios de aceitação:**

- [ ] 30/60/120 Hz por 1 s produzem deslocamento equivalente até 1 DIP, sem borda.
- [ ] Direções opostas anulam movimento; diagonal normalizada; intervalos são limitados a 50 ms.
- [ ] Soltar/desativar limpa estado e bordas nunca alteram o índice, inclusive após nova pressão.

**Verificação:** [V-CORE](plan.md#v-core); [V-BUILD](plan.md#v-build); verificar UI-01/02/03 com relógio simulado.

**Dependências:** [T11](#t11).

**Arquivos prováveis:**

- `src/ComicBookReader.Core/Reading/ScrollState.cs`
- `src/ComicBookReader.Core/Reading/ReaderController.cs`
- `tests/ComicBookReader.Core.Tests/Reading/ContinuousScrollTests.cs`

**Escopo estimado:** M (3 arquivos).

Roteamento: gpt-6.1-sol/high — raiz — Motivo: regra temporal precisa ser única e verificável fora da janela — Validação: evidências da verificação acima.

<a id="t14"></a>

## [ ] T14: Integrar rolagem ao quadro WPF

**Descrição:** Traduzir KeyDown/KeyUp em estado e CompositionTarget.Rendering em tempo monotônico. Incluir roda/Shift+roda, foco, diálogos e limpeza das assinaturas de eventos.

**Critérios de aceitação:**

- [ ] Setas mantidas movem a imagem continuamente; soltar/perder foco/abrir diálogo interrompe imediatamente.
- [ ] Após retomar o foco não há movimento até uma nova pressão, nem salto após pausa.
- [ ] Teste visual de 10 s registra hardware, monitor/DPI e cadência se houver trancos; setas e roda não trocam página.

**Verificação:** [V-CORE](plan.md#v-core); [V-BUILD](plan.md#v-build); [V-APP](plan.md#v-app) para UI-02/03/09; anexar observações visuais reais.

**Dependências:** [T12](#t12), [T13](#t13).

**Arquivos prováveis:**

- `src/ComicBookReader.App/Input/ReaderKeyRouter.cs`
- `src/ComicBookReader.App/Presentation/RenderingClock.cs`
- `src/ComicBookReader.App/MainWindow.xaml.cs`
- `docs/manual-validation.md`

**Escopo estimado:** M (4 arquivos).

Roteamento: gpt-6.1-sol/high — raiz — Motivo: o ganho central só se comprova com a janela em execução — Validação: evidências da verificação acima.

### Marco após T13–T14: Rolagem suave

- [ ] Critérios do bloco atendidos e comandos aplicáveis registrados, sem regressões no caminho já disponível.
- [ ] Teste temporal passando e janela real exercitada por 10 s, incluindo foco e bordas.
- [ ] Evidência disponível para revisão; decisões/ajustes registrados antes de alterar comportamento confirmado ou escopo.

Roteamento: gpt-6.1-sol/high — raiz — Motivo: integrar a evidência do bloco e resolver dúvidas sem separar estado compartilhado — Validação: critérios e demonstração do marco.

<a id="t15"></a>

## [ ] T15: Alternar visão completa e restaurar leitura

**Descrição:** Adicionar V como alternância por pressão física, com snapshot vinculado à imagem/livro. Reutilizar o layout da T10.

**Critérios de aceitação:**

- [ ] V duas vezes no mesmo viewport restaura escala/offsets até 1 DIP.
- [ ] Navegação descarta snapshot; mudança de viewport restaura âncora válida; toggle limpa teclas pressionadas.
- [ ] Snapshot fornece o ponto de leitura original para a futura persistência, nunca offsets do overview.

**Verificação:** [V-CORE](plan.md#v-core); [V-APP](plan.md#v-app): repetir V em página dupla com pan e após redimensionar.

**Dependências:** [T14](#t14).

**Arquivos prováveis:**

- `src/ComicBookReader.Core/Reading/ReadingSnapshot.cs`
- `src/ComicBookReader.Core/Reading/ReaderController.cs`
- `src/ComicBookReader.App/Input/ReaderKeyRouter.cs`
- `src/ComicBookReader.App/MainWindow.xaml.cs`
- `tests/ComicBookReader.Core.Tests/Reading/OverviewTests.cs`

**Escopo estimado:** M (5 arquivos).

Roteamento: gpt-6.1-sol/high — raiz — Motivo: restauração depende do estado de leitura e dos limites geométricos — Validação: evidências da verificação acima.

<a id="t16"></a>

## [ ] T16: Ajustar largura e classificar manualmente

**Descrição:** Expor tamanho relativo de uma página e Auto/Simples/Dupla. Aplicar +/− e Ctrl+roda preservando âncora. Os ajustes ficam em memória nesta fatia; gravação chega em T20.

**Critérios de aceitação:**

- [ ] Compensação de páginas duplas conserva a largura lógica e permite movimento lateral.
- [ ] Zoom respeita 0,25–2, preserva cursor/centro e não depende da resolução do scan.
- [ ] Alterar tamanho durante overview encerra esse modo; um snapshot obsoleto não pode ser restaurado.

**Verificação:** [V-CORE](plan.md#v-core); [V-APP](plan.md#v-app): comparar páginas 1000×1500, 2000×1500 e 4000×3000 com o mesmo viewport.

**Dependências:** [T15](#t15).

**Arquivos prováveis:**

- `src/ComicBookReader.App/MainWindow.xaml`
- `src/ComicBookReader.App/MainWindow.xaml.cs`
- `src/ComicBookReader.App/Input/ReaderKeyRouter.cs`
- `src/ComicBookReader.Core/Reading/ReaderController.cs`
- `tests/ComicBookReader.Core.Tests/Reading/ReadingSizeTests.cs`

**Escopo estimado:** M (5 arquivos).

Roteamento: gpt-6.1-sol/high — raiz — Motivo: tamanho, classificação e overview compartilham invariantes — Validação: evidências da verificação acima.

### Marco após T15–T16: Visão completa e tamanho

- [ ] Critérios do bloco atendidos e comandos aplicáveis registrados, sem regressões no caminho já disponível.
- [ ] V restaura o ponto; duas resoluções de página dupla conservam a escala; ajustes manuais funcionam.
- [ ] Evidência disponível para revisão; decisões/ajustes registrados antes de alterar comportamento confirmado ou escopo.

Roteamento: gpt-6.1-sol/high — raiz — Motivo: integrar a evidência do bloco e resolver dúvidas sem separar estado compartilhado — Validação: critérios e demonstração do marco.

<a id="t17"></a>

## [ ] T17: Resolver preferências e progresso

**Descrição:** Implementar snapshots validados, precedência livro/global/default, posições relativas e regras de atalhos. Separar dados de preferência de DTOs JSON.

**Critérios de aceitação:**

- [ ] Livro prevalece sobre global; outro livro conserva seu default; campos ausentes recebem defaults.
- [ ] Valores não finitos, fora da faixa e atalhos duplicados/reservados recebem tratamento explícito.
- [ ] Posição relativa é válida em 0–1 e eixo sem overflow salva/restaura zero.

**Verificação:** [V-CORE](plan.md#v-core); [V-BUILD](plan.md#v-build); verificar RP-01/05/06.

**Dependências:** [T05](#t05).

**Arquivos prováveis:**

- `src/ComicBookReader.Core/Preferences/ReaderPreferences.cs`
- `src/ComicBookReader.Core/Preferences/PreferencesResolver.cs`
- `src/ComicBookReader.Core/Preferences/IReadingPreferencesStore.cs`
- `tests/ComicBookReader.Core.Tests/Preferences/PreferencesResolverTests.cs`
- `tests/ComicBookReader.Core.Tests/Preferences/ReadingPositionTests.cs`

**Escopo estimado:** M (5 arquivos).

Roteamento: gpt-6.1-sol/high — raiz — Motivo: defaults e validação são contratos usados por UI e armazenamento — Validação: evidências da verificação acima.

<a id="t18"></a>

## [ ] T18: Persistir JSON com recuperação

**Descrição:** Implementar o store em LOCALAPPDATA com DTO versionado, temporário no mesmo diretório, substituição e último backup válido.

**Critérios de aceitação:**

- [ ] Salvar/carregar recupera todos os dados de leitura sem escrever no Registro ou na HQ.
- [ ] Falha antes da substituição preserva a última versão válida; JSON corrompido é preservado e tenta backup/default.
- [ ] Versão futura incompatível não é sobrescrita; IO vira StorageUnavailable e cancelamento continua cancelamento.

**Verificação:** [V-INTEGRATION](plan.md#v-integration) em diretórios temporários; [V-BUILD](plan.md#v-build); simular escrita interrompida e versão futura.

**Dependências:** [T17](#t17).

**Arquivos prováveis:**

- `src/ComicBookReader.Infrastructure/Storage/PreferencesDto.cs`
- `src/ComicBookReader.Infrastructure/Storage/JsonReadingPreferencesStore.cs`
- `src/ComicBookReader.Infrastructure/Storage/AtomicFileWriter.cs`
- `tests/ComicBookReader.Integration.Tests/Storage/PreferencesStoreTests.cs`
- `tests/ComicBookReader.Integration.Tests/Storage/PreferencesRecoveryTests.cs`

**Escopo estimado:** M (5 arquivos).

Roteamento: gpt-6.1-sol/high — raiz — Motivo: recuperação e compatibilidade precisam de falhas simuladas — Validação: evidências da verificação acima.

<a id="t19"></a>

## [ ] T19: Serializar autosave e flush

**Descrição:** Consolidar alterações por 300 ms e garantir ordem de snapshots. Navegar e fechar pedem flush sem bloquear a UI; falha mantém preferências em memória.

**Critérios de aceitação:**

- [ ] Gravações nunca se sobrepõem e snapshot antigo não substitui o mais novo.
- [ ] Flush grava o último estado em troca de página/fechamento; dispose/cancelamento têm dono explícito.
- [ ] Falha de armazenamento não trava leitura e produz um estado de aviso consumível pela UI.

**Verificação:** [V-CORE](plan.md#v-core) com scheduler/store controlados; [V-INTEGRATION](plan.md#v-integration); [V-BUILD](plan.md#v-build).

**Dependências:** [T18](#t18).

**Arquivos prováveis:**

- `src/ComicBookReader.Core/Preferences/PreferencesSaveCoordinator.cs`
- `tests/ComicBookReader.Core.Tests/Preferences/PreferencesSaveCoordinatorTests.cs`
- `tests/ComicBookReader.Integration.Tests/Storage/PreferencesFlushTests.cs`

**Escopo estimado:** M (3 arquivos).

Roteamento: gpt-6.1-sol/xhigh — raiz — Motivo: corridas na gravação podem perder preferências sem erro visível — Validação: evidências da verificação acima.

### Marco após T17–T19: Persistência confiável

- [ ] Critérios do bloco atendidos e comandos aplicáveis registrados, sem regressões no caminho já disponível.
- [ ] Precedência, recuperação e flush com falhas/concorrência simuladas.
- [ ] Evidência disponível para revisão; decisões/ajustes registrados antes de alterar comportamento confirmado ou escopo.

Roteamento: gpt-6.1-sol/high — raiz — Motivo: integrar a evidência do bloco e resolver dúvidas sem separar estado compartilhado — Validação: critérios e demonstração do marco.

<a id="t20"></a>

## [ ] T20: Recuperar tamanho e ponto de leitura

**Descrição:** Integrar preferências à janela/controller. Persistir ajustes do livro, override por PageKey, último fullscreen e progresso relativo; overview fornece o snapshot original.

**Critérios de aceitação:**

- [ ] Reabrir/reiniciar recupera página, tamanho, tipo e ponto válido; página inexistente usa fallback limitado.
- [ ] Salvar/fechar durante overview recupera o ponto original de leitura na reabertura.
- [ ] Falha de IO mostra aviso discreto e permite continuar lendo; nenhum dado legado é migrado.

**Verificação:** [V-CORE](plan.md#v-core); [V-INTEGRATION](plan.md#v-integration); [V-APP](plan.md#v-app): ajustar, mover, fechar em overview e reabrir.

**Dependências:** [T16](#t16), [T19](#t19).

**Arquivos prováveis:**

- `src/ComicBookReader.Core/Reading/ReaderController.cs`
- `src/ComicBookReader.App/MainWindow.xaml.cs`
- `src/ComicBookReader.App/MainWindow.xaml`
- `tests/ComicBookReader.Core.Tests/Reading/ReadingRestoreTests.cs`
- `tests/ComicBookReader.Integration.Tests/Reading/ReadingPreferencesContractTests.cs`

**Escopo estimado:** M (5 arquivos).

Roteamento: gpt-6.1-sol/high — raiz — Motivo: persistência deve refletir o estado correto do controller — Validação: evidências da verificação acima.

<a id="t21"></a>

## [ ] T21: Configurar velocidade e atalhos

**Descrição:** Adicionar painel de preferências com padrão global distinto do ajuste por livro, velocidade e bindings. Usar a mesma validação de T17.

**Critérios de aceitação:**

- [ ] Velocidade e largura global respeitam faixas; alterar global não remove overrides de livros.
- [ ] Conflitos são apresentados; nenhuma configuração pode associar setas à navegação.
- [ ] Controles têm rótulo, foco por teclado e diálogo suspende movimento; salvar recupera configurações ao reiniciar.

**Verificação:** [V-CORE](plan.md#v-core); [V-BUILD](plan.md#v-build); [V-APP](plan.md#v-app): reconfigurar, criar conflito, reiniciar e navegar por Tab.

**Dependências:** [T20](#t20).

**Arquivos prováveis:**

- `src/ComicBookReader.App/Preferences/PreferencesWindow.xaml`
- `src/ComicBookReader.App/Preferences/PreferencesWindow.xaml.cs`
- `src/ComicBookReader.App/Input/ReaderKeyRouter.cs`
- `src/ComicBookReader.App/MainWindow.xaml.cs`
- `tests/ComicBookReader.Core.Tests/Preferences/KeyBindingTests.cs`

**Escopo estimado:** M (5 arquivos).

Roteamento: gpt-6.1-sol/high — raiz — Motivo: UI de configuração precisa respeitar comandos e persistência existentes — Validação: evidências da verificação acima.

### Marco após T20–T21: Leitura lembrada

- [ ] Critérios do bloco atendidos e comandos aplicáveis registrados, sem regressões no caminho já disponível.
- [ ] Reiniciar recupera o livro e o ponto correto; preferências configuráveis e conflitos apresentados.
- [ ] Evidência disponível para revisão; decisões/ajustes registrados antes de alterar comportamento confirmado ou escopo.

Roteamento: gpt-6.1-sol/high — raiz — Motivo: integrar a evidência do bloco e resolver dúvidas sem separar estado compartilhado — Validação: critérios e demonstração do marco.

<a id="t22"></a>

## [ ] T22: Abrir CBR não sólido no leitor

**Descrição:** Adicionar a sessão RAR4/RAR5 e seleção por assinatura do conteúdo. Reutilizar identidade, decoder e ciclo de vida dos contratos.

**Critérios de aceitação:**

- [ ] Fixtures RAR4/RAR5 abrem na interface e permitem ir/voltar com ordem correta.
- [ ] Extensão incorreta não decide o formato; senha e multivolume retornam UnsupportedArchive.
- [ ] Cancelar/fechar libera o arquivo, e hash antes/depois é igual.

**Verificação:** [V-INTEGRATION](plan.md#v-integration); [V-BUILD](plan.md#v-build); [V-APP](plan.md#v-app) com as duas fixtures não sólidas.

**Dependências:** [T09](#t09), [T20](#t20).

**Arquivos prováveis:**

- `src/ComicBookReader.Infrastructure/Archives/RarComicSession.cs`
- `src/ComicBookReader.Infrastructure/Archives/ComicOpener.cs`
- `tests/ComicBookReader.Integration.Tests/Archives/RarComicSessionTests.cs`
- `tests/ComicBookReader.Integration.Tests/Archives/UnsupportedArchiveTests.cs`

**Escopo estimado:** M (4 arquivos).

Roteamento: gpt-6.1-sol/high — raiz — Motivo: suporte CBR precisa integrar navegação e erros reais — Validação: evidências da verificação acima.

<a id="t23"></a>

## [ ] T23: Materializar RAR sólido com segurança

**Descrição:** Implementar Reader sequencial, cache privado por sessão e progresso/cancelamento antes de oferecer acesso arbitrário. Limites são explícitos, sem usar os nomes da entrada como destino.

**Critérios de aceitação:**

- [ ] RAR sólido permite página seguinte/anterior após materialização; loading mostra progresso.
- [ ] Cache usa nomes gerados, fica restrito à sessão e respeita 2 GiB/128 MiB por entrada.
- [ ] Cancelamento/fechamento remove cache; próxima inicialização limpa resíduos pertencentes ao aplicativo.

**Verificação:** [V-INTEGRATION](plan.md#v-integration): sólido, cancelamento e entradas com traversal; [V-APP](plan.md#v-app): progresso e abertura de outro livro.

**Dependências:** [T22](#t22).

**Arquivos prováveis:**

- `src/ComicBookReader.Infrastructure/Archives/SolidRarMaterializer.cs`
- `src/ComicBookReader.Infrastructure/Archives/SessionCache.cs`
- `src/ComicBookReader.Infrastructure/Archives/ComicOpener.cs`
- `src/ComicBookReader.App/MainWindow.xaml.cs`
- `tests/ComicBookReader.Integration.Tests/Archives/SolidRarSessionTests.cs`

**Escopo estimado:** M (5 arquivos).

Roteamento: gpt-6.1-sol/xhigh — raiz — Motivo: extração sequencial, cancelamento e caminhos exigem propriedade clara — Validação: evidências da verificação acima.

<a id="t24"></a>

## [ ] T24: Aplicar limites e apresentar falhas de entrada

**Descrição:** Fechar os caminhos de erro do MVP: tamanho codificado, dimensões/pixels, arquivo inválido, imagem corrompida e acesso negado. Aplicar limites antes da alocação onde o decoder permitir.

**Critérios de aceitação:**

- [ ] 96 milhões de pixels/128 MiB por entrada produzem ResourceLimit e permitem abrir outro livro.
- [ ] Erro de página não renumera o índice; erros são compreensíveis em português e sem stack de terceiros.
- [ ] Arquivos malformados e entradas fora do cache não provocam escrita externa; HQs conservam seus hashes.

**Verificação:** [V-INTEGRATION](plan.md#v-integration) com fixtures sintéticas inválidas/protegidas; [V-APP](plan.md#v-app) para mensagens; [V-BUILD](plan.md#v-build).

**Dependências:** [T23](#t23).

**Arquivos prováveis:**

- `src/ComicBookReader.Infrastructure/Imaging/SkiaPageDecoder.cs`
- `src/ComicBookReader.Infrastructure/Archives/ComicOpener.cs`
- `src/ComicBookReader.Infrastructure/Archives/ZipComicSession.cs`
- `src/ComicBookReader.App/MainWindow.xaml.cs`
- `tests/ComicBookReader.Integration.Tests/Archives/ResourceAndErrorTests.cs`

**Escopo estimado:** M (5 arquivos).

Roteamento: gpt-6.1-sol/high — raiz — Motivo: defesas precisam cobrir os dois formatos sem reduzir o suporte — Validação: evidências da verificação acima.

### Marco após T22–T24: CBR integrado e limites

- [ ] Critérios do bloco atendidos e comandos aplicáveis registrados, sem regressões no caminho já disponível.
- [ ] RAR4/RAR5/sólido abrem no leitor; recursos/caches são liberados; falhas/limites têm tratamento explícito.
- [ ] Evidência disponível para revisão; decisões/ajustes registrados antes de alterar comportamento confirmado ou escopo.

Roteamento: gpt-6.1-sol/high — raiz — Motivo: integrar a evidência do bloco e resolver dúvidas sem separar estado compartilhado — Validação: critérios e demonstração do marco.

<a id="t25"></a>

## [ ] T25: Priorizar leitura e limitar cache de imagens

**Descrição:** Adicionar cache adicional de 256 MiB, contabilizado por stride×altura, e prefetch anterior/seguinte com baixa prioridade. Página atual tem contabilização separada.

**Critérios de aceitação:**

- [ ] Leitura pedida tem prioridade; prefetch cancela/cede e não publica imagens antigas.
- [ ] Cache adicional não excede 256 MiB, preserva leases ativos e descarta buffers ao fechar.
- [ ] Não há crescimento persistente de recursos ao alternar livros; prefetch pode ser desligado para diagnosticar comportamento.

**Verificação:** [V-INTEGRATION](plan.md#v-integration) instrumentada; [V-BUILD](plan.md#v-build); [V-APP](plan.md#v-app): alternar páginas/livros e observar recursos.

**Dependências:** [T24](#t24).

**Arquivos prováveis:**

- `src/ComicBookReader.Infrastructure/Imaging/PageImageCache.cs`
- `src/ComicBookReader.Infrastructure/Archives/CachedComicSession.cs`
- `src/ComicBookReader.Infrastructure/Archives/ComicOpener.cs`
- `tests/ComicBookReader.Integration.Tests/Imaging/PageImageCacheTests.cs`
- `tests/ComicBookReader.Integration.Tests/Reading/ReadPriorityTests.cs`

**Escopo estimado:** M (5 arquivos).

Roteamento: gpt-6.1-sol/high — raiz — Motivo: prioridades e descarte compartilham os contratos já estabilizados — Validação: evidências da verificação acima.

<a id="t26"></a>

## [ ] T26: Validar viewport, DPI e suavidade real

**Descrição:** Completar a reação a resize/monitor/DPI e a apresentação dos controles temporários. Registrar medidas da janela real; corrigir a causa de trancos se encontrados.

**Critérios de aceitação:**

- [ ] Mudança de viewport conserva proporção e âncora válida; DPI não é aplicado duas vezes.
- [ ] Overlay não altera o tamanho de leitura e controles continuam acessíveis em janela/tela cheia.
- [ ] UI-07/09 são exercitados de verdade; resultados indicam ambientes testados e limitações.

**Verificação:** [V-CORE](plan.md#v-core); [V-BUILD](plan.md#v-build); [V-APP](plan.md#v-app): resize, fullscreen, diálogo, pan e 10 s de movimento; usar monitor/DPI alternativo quando disponível.

**Dependências:** [T21](#t21), [T25](#t25).

**Arquivos prováveis:**

- `src/ComicBookReader.App/MainWindow.xaml`
- `src/ComicBookReader.App/MainWindow.xaml.cs`
- `src/ComicBookReader.Core/Reading/ReaderController.cs`
- `tests/ComicBookReader.Core.Tests/Reading/ViewportChangeTests.cs`
- `docs/manual-validation.md`

**Escopo estimado:** M (5 arquivos).

Roteamento: gpt-6.1-sol/high — raiz — Motivo: percepção de suavidade e DPI não se comprovam só por cálculo — Validação: evidências da verificação acima.

<a id="t27"></a>

## [ ] T27: Revisar contratos, recursos e segurança

**Descrição:** Revisar o diff completo por qualidade, limites de entrada, lifecycle, persistência e licenças. Esta é uma tarefa de revisão; correções descobertas entram como tarefas pequenas antes de prosseguir.

**Critérios de aceitação:**

- [ ] Nenhum achado relevante de correção, recursos ou entrada fica sem tratamento/evidência.
- [ ] Ausência de stubs, supressões e testes enfraquecidos é verificada; cobertura dos IDs da spec é conferida.
- [ ] Revisão registra bibliotecas/licenças, acesso somente leitura à HQ e preservação do CDisplayEx.

**Verificação:** [V-CORE](plan.md#v-core); [V-INTEGRATION](plan.md#v-integration); [V-BUILD](plan.md#v-build); [V-FORMAT](plan.md#v-format); inspeção de código e registro de achados.

**Dependências:** [T26](#t26).

**Arquivos prováveis:**

- `docs/review.md`

**Escopo estimado:** S (1 arquivos).

Roteamento: gpt-6.1-sol/xhigh — raiz — Motivo: auditoria transversal reduz risco antes de preparar a distribuição — Validação: evidências da verificação acima.

### Marco após T25–T27: Recursos e revisão

- [ ] Critérios do bloco atendidos e comandos aplicáveis registrados, sem regressões no caminho já disponível.
- [ ] Cache limitado, prioridades e DPI validados; revisão concluída, achados relevantes resolvidos.
- [ ] Evidência disponível para revisão; decisões/ajustes registrados antes de alterar comportamento confirmado ou escopo.

Roteamento: gpt-6.1-sol/high — raiz — Motivo: integrar a evidência do bloco e resolver dúvidas sem separar estado compartilhado — Validação: critérios e demonstração do marco.

<a id="t28"></a>

## [ ] T28: Fixar lockfiles restauráveis

**Descrição:** Gerar e versionar um lockfile por projeto com a base aprovada. Esta tarefa delimita os cinco arquivos gerados e valida restore em modo bloqueado.

**Critérios de aceitação:**

- [ ] Cinco lockfiles registram dependências resolvidas sem atualização silenciosa.
- [ ] Restore bloqueado e build passam na arquitetura x64; dependências nativas do decoder estão resolvidas.

**Verificação:** [V-LOCK-GENERATE](plan.md#v-lock-generate); [V-LOCK-CHECK](plan.md#v-lock-check); [V-BUILD](plan.md#v-build); [V-CORE](plan.md#v-core); [V-INTEGRATION](plan.md#v-integration).

**Dependências:** [T27](#t27).

**Arquivos prováveis:**

- `src/ComicBookReader.Core/packages.lock.json`
- `src/ComicBookReader.Infrastructure/packages.lock.json`
- `src/ComicBookReader.App/packages.lock.json`
- `tests/ComicBookReader.Core.Tests/packages.lock.json`
- `tests/ComicBookReader.Integration.Tests/packages.lock.json`

**Escopo estimado:** M (5 arquivos).

Roteamento: gpt-6-luna/medium — raiz — Motivo: operação delimitada com validação automática objetiva — Validação: evidências da verificação acima.

<a id="t29"></a>

## [ ] T29: Gerar pacote Windows x64

**Descrição:** Publicar distribuição self-contained, sem instalador nem associação de arquivos. Documentar iniciar, dados locais e atalhos; incluir notices de terceiros.

**Critérios de aceitação:**

- [ ] Executável publicado abre CBZ/CBR sem depender do runtime instalado no computador.
- [ ] Artefato contém bibliotecas nativas necessárias e avisos de terceiros; saídas não entram no Git.
- [ ] README descreve comandos de uso e limitações reais; diretório do CDisplayEx permanece intacto.

**Verificação:** [V-PUBLISH](plan.md#v-publish); executar artifacts/win-x64/ComicBookReader.App.exe e abrir fixtures CBZ/CBR; [V-LOCK-CHECK](plan.md#v-lock-check).

**Dependências:** [T28](#t28).

**Arquivos prováveis:**

- `README.md`
- `THIRD-PARTY-NOTICES.md`
- `scripts/publish.ps1`

**Escopo estimado:** M (3 arquivos).

Roteamento: gpt-6-luna/high — raiz — Motivo: empacotamento com comandos definidos e smoke test claro — Validação: evidências da verificação acima.

<a id="t30"></a>

## [ ] T30: Validar a primeira versão completa

**Descrição:** Executar a matriz final na distribuição publicada e consolidar a evidência por requisito. Registrar feedback do usuário sobre suavidade e tamanho no ambiente disponível.

**Critérios de aceitação:**

- [ ] G-01 a G-09 e todos os IDs CF/RP/PL/UI têm evidência vinculada, sem declarar cenário não testado.
- [ ] Build/format/testes passam e smoke test publicado inclui dupla, retorno de overview, reinício e troca rápida.
- [ ] Resultado fica pronto para revisão humana; falhas geram tarefas de correção, sem marcar a versão pronta nem publicar externamente.

**Verificação:** [V-LOCK-CHECK](plan.md#v-lock-check); [V-BUILD](plan.md#v-build); [V-FORMAT](plan.md#v-format); [V-CORE](plan.md#v-core); [V-INTEGRATION](plan.md#v-integration); smoke test [V-PUBLISH](plan.md#v-publish) e matriz da spec no executável.

**Dependências:** [T29](#t29).

**Arquivos prováveis:**

- `docs/manual-validation.md`
- `docs/acceptance.md`

**Escopo estimado:** S (2 arquivos).

Roteamento: gpt-6.1-sol/high — raiz — Motivo: verificação final e síntese da evidência devem ficar com o responsável principal — Validação: evidências da verificação acima.

### Marco após T28–T30: Distribuição validada

- [ ] Critérios do bloco atendidos e comandos aplicáveis registrados, sem regressões no caminho já disponível.
- [ ] Restore bloqueado, publicação x64 e matriz de aceitação com evidência; resultado disponível para revisão humana.
- [ ] Evidência disponível para revisão; decisões/ajustes registrados antes de alterar comportamento confirmado ou escopo.

Roteamento: gpt-6.1-sol/high — raiz — Motivo: integrar a evidência do bloco e resolver dúvidas sem separar estado compartilhado — Validação: critérios e demonstração do marco.
