# Plano de implementação: ComicBookReader

Status: plano proposto para revisão, criado em 2026-10-09 a pedido do usuário. Nenhuma tarefa de implementação foi executada nesta etapa.

## Resultado esperado

Um leitor Windows de CBZ/CBR, em tela cheia, que rola suavemente com setas mantidas, preserva a escala aproximada em imagens duplas e alterna entre visão completa e ponto de leitura. As bordas nunca trocam a página. Preferências e progresso evitam reconfiguração a cada arquivo.

Base: [spec principal](../SPEC.md), [mapa de módulos](../CAPABILITY-MAP.md) e specs de [arquivos](../SPEC-comic-files.md), [preferências](../SPEC-reading-preferences.md), [layout](../SPEC-page-layout.md) e [interface](../SPEC-reader-ui.md).

O estado das tarefas e seus critérios ficam exclusivamente em [todo.md](todo.md). Esta página é o índice e o registro de decisões. Todos os caminhos de código abaixo são propostos: ainda não existem.

## Decisões de implementação

- Preservar os quatro módulos lógicos em três projetos: Core para contratos/regras, Infrastructure para adapters e App para WPF. Nenhuma dependência de WPF, SharpCompress ou SkiaSharp no Core.
- Usar a base já especificada: .NET 10/C# 14, WPF, SharpCompress 0.50.5, SkiaSharp 4.153.1 e xunit.v3 4.0.2. T01 confirma versões/SDK; T07 e T09 comprovam integração de codecs e arquivos.
- Um controller é dono do estado de leitura, da geração de carregamento e do snapshot de visão completa. A UI traduz eventos; a geometria e o tempo permanecem testáveis.
- Imagens duplas continuam inteiras e ocupam um índice. Escala de leitura depende da largura lógica de uma página, sem ajuste pela altura. A largura é relativa ao viewport, em DIPs.
- Rolagem usa estado de teclas e tempo monotônico por quadro. Repetição do Windows não define a velocidade. Pausa/foco/diálogo/troca de página limpam teclas e relógio.
- Começar com um caminho completo CBZ → pixels → janela. Investigar RAR sólido logo após a primeira leitura; integrar o suporte CBR na janela depois que o estado de leitura estiver estável.
- Buffers têm dono e leases explícitos. Decodificação/arquivo rodam fora do thread da interface. Limites básicos entram no decoder inicial; T24 fecha a matriz de defesas.
- Persistência usa JSON local versionado, escrita atômica, backup e fila de snapshots. Visão completa é temporária e salva o ponto de leitura guardado.
- Distribuição inicial é pasta self-contained Windows x64. Instalador, associação de arquivos, serviços online e publicação externa ficam fora deste plano.

## Ordem das tarefas

| ID | Entrega | Modelo/esforço recomendado | Dependências |
| --- | --- | --- | --- |
| [T01](todo.md#t01) | Preparar o SDK x64 | gpt-6.1-sol/high | Nenhuma |
| [T02](todo.md#t02) | Fixar as configurações da solução | gpt-6-luna/medium | T01 |
| [T03](todo.md#t03) | Criar Core e Infrastructure com runners de teste | gpt-6-luna/high | T02 |
| [T04](todo.md#t04) | Criar a entrada WPF | gpt-6-luna/high | T03 |
| [T05](todo.md#t05) | Estabelecer contratos e posse de imagem | gpt-6.1-sol/high | T03 |
| [T06](todo.md#t06) | Indexar imagens de CBZ em ordem natural | gpt-6.1-sol/high | T05 |
| [T07](todo.md#t07) | Ler pixels de uma página CBZ | gpt-6.1-sol/high | T06 |
| [T08](todo.md#t08) | Mostrar a primeira página em tela cheia | gpt-6.1-sol/high | T04, T07 |
| [T09](todo.md#t09) | Comprovar RAR4, RAR5 e RAR sólido | gpt-6.1-sol/high | T07 |
| [T10](todo.md#t10) | Calcular escala de página simples e dupla | gpt-6.1-sol/high | T05 |
| [T11](todo.md#t11) | Controlar carregamento e alvo de leitura | gpt-6.1-sol/xhigh | T08, T10 |
| [T12](todo.md#t12) | Navegar por comandos separados | gpt-6.1-sol/high | T11 |
| [T13](todo.md#t13) | Calcular rolagem contínua no Core | gpt-6.1-sol/high | T11 |
| [T14](todo.md#t14) | Integrar rolagem ao quadro WPF | gpt-6.1-sol/high | T12, T13 |
| [T15](todo.md#t15) | Alternar visão completa e restaurar leitura | gpt-6.1-sol/high | T14 |
| [T16](todo.md#t16) | Ajustar largura e classificar manualmente | gpt-6.1-sol/high | T15 |
| [T17](todo.md#t17) | Resolver preferências e progresso | gpt-6.1-sol/high | T05 |
| [T18](todo.md#t18) | Persistir JSON com recuperação | gpt-6.1-sol/high | T17 |
| [T19](todo.md#t19) | Serializar autosave e flush | gpt-6.1-sol/xhigh | T18 |
| [T20](todo.md#t20) | Recuperar tamanho e ponto de leitura | gpt-6.1-sol/high | T16, T19 |
| [T21](todo.md#t21) | Configurar velocidade e atalhos | gpt-6.1-sol/high | T20 |
| [T22](todo.md#t22) | Abrir CBR não sólido no leitor | gpt-6.1-sol/high | T09, T20 |
| [T23](todo.md#t23) | Materializar RAR sólido com segurança | gpt-6.1-sol/xhigh | T22 |
| [T24](todo.md#t24) | Aplicar limites e apresentar falhas de entrada | gpt-6.1-sol/high | T23 |
| [T25](todo.md#t25) | Priorizar leitura e limitar cache de imagens | gpt-6.1-sol/high | T24 |
| [T26](todo.md#t26) | Validar viewport, DPI e suavidade real | gpt-6.1-sol/high | T21, T25 |
| [T27](todo.md#t27) | Revisar contratos, recursos e segurança | gpt-6.1-sol/xhigh | T26 |
| [T28](todo.md#t28) | Fixar lockfiles restauráveis | gpt-6-luna/medium | T27 |
| [T29](todo.md#t29) | Gerar pacote Windows x64 | gpt-6-luna/high | T28 |
| [T30](todo.md#t30) | Validar a primeira versão completa | gpt-6.1-sol/high | T29 |

## Marcos verificáveis

Os marcos aparecem após dois ou três itens no checklist. Os primeiros três passos preparam o ambiente; os demais marcos exibem comportamento ou evidência de integração.

- T01–T03: **Base e runners**. SDK fixado; restore/build da base válidos. Sem testes fictícios.
- T04–T06: **Contratos e índice**. Core independente de UI/adapters; testes reais de identidade, buffers e ordenação passando.
- T07–T09: **Primeira leitura e risco CBR**. CBZ abre na janela; codecs e três tipos RAR têm evidência. Resolver qualquer incompatibilidade antes de ampliar a integração.
- T10–T12: **Geometria e navegação**. Dupla recebe compensação correta; loading antigo não sobrescreve a página; navegação está separada.
- T13–T14: **Rolagem suave**. Teste temporal passando e janela real exercitada por 10 s, incluindo foco e bordas.
- T15–T16: **Visão completa e tamanho**. V restaura o ponto; duas resoluções de página dupla conservam a escala; ajustes manuais funcionam.
- T17–T19: **Persistência confiável**. Precedência, recuperação e flush com falhas/concorrência simuladas.
- T20–T21: **Leitura lembrada**. Reiniciar recupera o livro e o ponto correto; preferências configuráveis e conflitos apresentados.
- T22–T24: **CBR integrado e limites**. RAR4/RAR5/sólido abrem no leitor; recursos/caches são liberados; falhas/limites têm tratamento explícito.
- T25–T27: **Recursos e revisão**. Cache limitado, prioridades e DPI validados; revisão concluída, achados relevantes resolvidos.
- T28–T30: **Distribuição validada**. Restore bloqueado, publicação x64 e matriz de aceitação com evidência; resultado disponível para revisão humana.

A cada marco, disponibilizar a evidência para revisão. Ajustes de defaults podem ser avaliados no protótipo. Uma decisão que altere preferência confirmada, escopo ou contratos já consumidos exige revisão antes da mudança. Revisão/aprovação humana antecede merge ou publicação; este plano não inicia implementação, merge ou envio externo.

## Escolha de modelos

Cada tarefa tem uma linha de roteamento em todo.md, com modelo, esforço, responsável, motivo e evidência de validação. A recomendação vale para a sessão que executar a tarefa; ela não muda o modelo desta conversa.

- **GPT-6.1 Sol/high:** regras, integração, UI e verificação que exigem julgamento sobre várias etapas.
- **GPT-6.1 Sol/xhigh:** apenas T11, T19, T23 e T27: concorrência/lifecycle, gravação concorrente, cache de RAR sólido e auditoria transversal.
- **GPT-6 Luna/medium:** T02 e T28: configurações e lockfiles com decisões já fixadas e validação automática.
- **GPT-6 Luna/high:** T03, T04 e T29: projetos, entrada WPF e pacote delimitados com verificação objetiva.

Não recomendar versões anteriores de Sol nem Astra. Terra não está disponível nesta sessão; suas tarefas de implementação/revisão ficam com GPT-6.1 Sol. Não há medição para quantificar economia de cota.

O responsável padrão é a raiz. Não criar um subagente por tarefa pequena nem um subagente só para trocar o modelo. Caso a raiz não possa mudar, executar localmente ou usar a recomendação numa sessão futura. Reavaliar a delegação somente se houver unidade independente, benefício de paralelo/espera/revisão e ganho superior ao custo de transferência.

T09, T10 e T17 podem ter evidência preparada de forma independente após suas dependências, pois usam pastas distintas. Qualquer paralelismo futuro precisa de contratos fixados e propriedade exclusiva de arquivos. Controller, MainWindow, roteador de teclas, solução, configurações de pacote e integração final têm um dono por vez. A ordem da tabela é a opção padrão.

## Verificação e comandos

Executar da raiz `C:\Users\ingos\Documents\LeitorHQ`. Os identificadores V-* de todo.md apontam para os comandos exatos abaixo. Eles não foram executados nesta etapa documental.

### V-SDK

```powershell
rtk proxy "C:\Users\ingos\AppData\Local\ComicBookReader-dev\dotnet-10.0.401\dotnet.exe" --list-sdks
```

### V-RESTORE

```powershell
rtk proxy "C:\Users\ingos\AppData\Local\ComicBookReader-dev\dotnet-10.0.401\dotnet.exe" restore ComicBookReader.slnx
```

### V-BUILD

```powershell
rtk proxy "C:\Users\ingos\AppData\Local\ComicBookReader-dev\dotnet-10.0.401\dotnet.exe" build ComicBookReader.slnx --configuration Release --no-restore -warnaserror
```

### V-FORMAT

```powershell
rtk proxy "C:\Users\ingos\AppData\Local\ComicBookReader-dev\dotnet-10.0.401\dotnet.exe" format ComicBookReader.slnx --verify-no-changes --no-restore
```

### V-CORE

```powershell
rtk proxy "C:\Users\ingos\AppData\Local\ComicBookReader-dev\dotnet-10.0.401\dotnet.exe" run --project tests/ComicBookReader.Core.Tests/ComicBookReader.Core.Tests.csproj --configuration Release
```

### V-INTEGRATION

```powershell
rtk proxy "C:\Users\ingos\AppData\Local\ComicBookReader-dev\dotnet-10.0.401\dotnet.exe" run --project tests/ComicBookReader.Integration.Tests/ComicBookReader.Integration.Tests.csproj --configuration Release
```

### V-APP

```powershell
rtk proxy "C:\Users\ingos\AppData\Local\ComicBookReader-dev\dotnet-10.0.401\dotnet.exe" run --project src/ComicBookReader.App/ComicBookReader.App.csproj --configuration Debug
```

### V-LOCK-GENERATE

```powershell
rtk proxy "C:\Users\ingos\AppData\Local\ComicBookReader-dev\dotnet-10.0.401\dotnet.exe" restore ComicBookReader.slnx -p:RestorePackagesWithLockFile=true
```

### V-LOCK-CHECK

```powershell
rtk proxy "C:\Users\ingos\AppData\Local\ComicBookReader-dev\dotnet-10.0.401\dotnet.exe" restore ComicBookReader.slnx --locked-mode -p:RestorePackagesWithLockFile=true
```

### V-PUBLISH

```powershell
rtk proxy "C:\Users\ingos\AppData\Local\ComicBookReader-dev\dotnet-10.0.401\dotnet.exe" publish src/ComicBookReader.App/ComicBookReader.App.csproj --configuration Release --runtime win-x64 --self-contained true --output artifacts/win-x64 -p:RestoreLockedMode=true -p:RestorePackagesWithLockFile=true
```

Antes de T03, a solução ainda não contém projetos; um restore vazio valida somente o arquivo da solução. Não executar runners vazios como evidência de testes. V-CORE começa em T05 e V-INTEGRATION em T06/T07, quando existirem casos reais.

V-APP depende da janela em T08 para validar leitura. Em T04 verifica somente inicialização/encerramento. V-PUBLISH vem após T28, com referências win-x64 já configuradas e lockfiles validados. Gerados de build, caches e artifacts não são arquivos de fonte para os limites de tamanho das tarefas.

Executar teste relevante e build por tarefa; repetir a suíte mais ampla quando a mudança ou uma falha justificar. Formatação e conjunto completo entram nos marcos finais. Para lógica, usar casos que falham sem a mudança; para configuração/documentação simples, inspeção e comandos bastam.

## Condição de conclusão comum

Além dos critérios de cada tarefa:

1. Evidência real do comportamento, incluindo bordas e falhas relevantes; build e testes aplicáveis passando, sem testes apagados/ignorados ou supressões para obter verde.
2. Código restrito ao objetivo, sem regras duplicadas na UI, stubs ou recursos sem dono. Contratos/documentos permanecem coerentes.
3. Integração com o caminho já utilizável demonstrada; descarte/cancelamento e preservação dos arquivos verificados quando afetados.
4. Marcos finais incluem revisão de entradas não confiáveis, licenças e dados locais. Artefatos ficam locais; remoção da pasta publicada permite reverter a distribuição sem tocar no CDisplayEx.
5. Revisão humana antes de merge/publicação. Não declarar suporte a formato, DPI alternativo ou suavidade visual sem evidência do caso.

Arquivos prováveis são uma estimativa de até cinco por tarefa. Se a implementação precisar de mais arquivos ou exceder uma sessão focada, dividir o item antes de continuar, manter seus critérios e registrar novas dependências. Correções de revisão seguem a mesma regra; não empurrar uma lista aberta de fixes para T27/T30.

## Rastreabilidade

| Requisitos da spec | Tarefas com evidência |
| --- | --- |
| CF-01 | T05–T07, T22 |
| CF-02 | T09, T22, T23 |
| CF-03 | T07 |
| CF-04 | T07, T11, T22, T23 |
| CF-05 | T06, T07, T22, T24 |
| CF-06 | T07, T22–T24, T27 |
| CF-07 | T11, T25 |
| RP-01 | T17, T20, T21 |
| RP-02 | T18, T20, T21 |
| RP-03 | T18, T19 |
| RP-04 | T15, T20 |
| RP-05 | T17–T19, T21 |
| RP-06 | T17, T20, T26 |
| PL-01–PL-05, PL-07 | T10, T15, T16 |
| PL-06 | T10, T26 |
| UI-01–UI-03 | T13, T14 |
| UI-04 | T12 |
| UI-05 | T15, T20 |
| UI-06 | T11, T25 |
| UI-07 | T15, T26 |
| UI-08 | T16, T20 |
| UI-09 | T14, T26, T30 |
| G-01 | T07, T09, T22, T23 |
| G-02, G-03 | T12–T14 |
| G-04 | T10, T16 |
| G-05 | T15 |
| G-06 | T18–T21 |
| G-07 | T26 |
| G-08 | T11, T25 |
| G-09 | T07, T22–T24, T27, T29 |

T30 consolida a matriz completa e liga cada ID à evidência correspondente. Critérios agrupados na tabela acima precisam ser desdobrados em linhas individuais na aceitação final.

## Riscos e resposta

| Risco | Impacto | Resposta concreta |
| --- | --- | --- |
| Nenhum SDK x64 instalado | Bloqueia código/build | T01 prepara SDK estável e fixa patch; usar executável x64 explícito. |
| Pacotes/nativos incompatíveis | Bloqueia primeira leitura/publicação | T07 testa três codecs; T09 testa RAR; T28/T29 validam pacote publicado. |
| Fixture RAR sem procedência | Impede comprovar subtipo | Obter fixtures pequenas redistribuíveis em T09; não usar HQs pessoais ou presumir que decoder escreve RAR. |
| RAR sólido sem acesso arbitrário | Navegação lenta/incorreta | Reader sequencial e materialização privada em T23, comprovados em T09. |
| Resultado antigo ou lease inválido | Página errada/recursos retidos | Geração e descartes testados em T11; prefetch não muda o estado publicado em T25. |
| Persistência fora de ordem | Perda de ajuste/progresso | Falhas e concorrência simuladas em T18/T19; flush e overview testados em T20. |
| Paisagem que não é dupla | Zoom inadequado | Heurística explícita mais override por PageKey em T16/T20. |
| Scan com resolução/margens diferentes | Ajustes frequentes | Largura lógica relativa ao viewport; override por livro e padrão global distinto. |
| Teste temporal passar com movimento visual ruim | Meta principal não atendida | Janela real por 10 s em T14/T26; investigar cadência se necessário; não substituir por fórmula. |
| Só um monitor/DPI disponível | Cobertura manual parcial | Registrar limitação; usar alteração de escala quando possível; não afirmar teste entre monitores sem executá-lo. |
| Entrada malformada/grande | Escrita externa ou memória excessiva | Limites iniciais no decoder, destinos gerados e matriz de erros em T23/T24; revisão T27. |

## Pontos a confirmar durante execução

Nenhuma informação funcional essencial está faltando para este plano. Patch do SDK, integração de bibliotecas e origem dos fixtures são verificações técnicas de T01/T07/T09. Page Up/Page Down, V, velocidade e tamanho inicial continuam defaults propostos, ajustáveis após o protótipo.

Caso a suavidade ou o tamanho percebido não satisfaçam o usuário, registrar o caso concreto antes de alterar a regra. Não ampliar escopo ou reduzir suporte CBR para contornar um teste que falhou.

## Retomada

Ler a spec, este plano e todo.md; verificar o estado real do Git e os pré-requisitos da próxima tarefa. Atualizar os checkboxes somente com evidência. A próxima tarefa planejada é T01. A implementação deve começar após a revisão do plano.
