# Registro de construção

Atualização 30/10/2026, versão local 0.2.1: seletor de sentido de leitura adicionado à barra, sincronizado com Preferências, traduzido e persistido. 246 testes e smoke WPF/portátil passaram; detalhamento em [reading-order-validation.md](reading-order-validation.md#seletor-na-barra--021).

Atualização 30/10/2026: Espaço por faixas, sentido direita → esquerda, 246 testes e distribuição local 0.2.0. Evidência: [reading-order-validation.md](reading-order-validation.md).

Plano autorizado pelo comando `$build`. Registros abaixo contêm verificações executadas; nenhum teste manual é inferido de build ou cálculo.

## T01

SDK oficial 10.0.401 x64 instalado em pasta privada, SHA512 conferido e --info executado. Caminhos dos comandos atualizados. Pacotes permanecem fixados; integração será comprovada por T03/T07/T09.

## T02

global.json fixa 10.0.401; configurações centrais e solução vazia criadas. --info resolveu o global.json; restore executado com aviso esperado de solução ainda sem projetos.

## T03

Restore dos quatro projetos e build Release com warnings como erros passaram. xUnit 4 requer UseAppHost=true, corrigido conforme erro explícito do pacote. Nenhum runner vazio foi contado como teste.

## T04

Aplicativo net10.0-windows configurado para win-x64. Restore/build Release passaram; aplicação WPF mínima iniciou e encerrou com código zero.

## T05

RED observado por namespace de contratos ausente; GREEN: três testes reais de identidade, validade do buffer e lease independente passaram. Build Release sem avisos. Core sem bibliotecas de terceiros.

## T06

RED por classes ausentes observado; GREEN: ordenação/índice passaram (5 Core antes de Layout e 2 Integration). Build sem avisos após integrar Layout; números enormes, subpastas, duplicidades e ZIP vazio conferidos.

## T10

Execução antecipada independente após T05. RED observado por Core.Layout ausente; GREEN: 46 testes Core incluindo 41 de layout passaram, build sem avisos. Exemplos de escala, classificação, centros, overview, viewport e entradas inválidas verificados.

## T07

RED observado por decoder/opener ausentes; GREEN: 19 testes Integration passaram, inclusive JPEG/PNG/WebP, oito orientações EXIF, alpha premultiplicado, hash, fechamento concorrente e limites. Build Release sem avisos. APIs obsoletas e tokens de testes corrigidos sem supressões.

## T17

Implementação independente antecipada após T05; RED pelos modelos/resolver ausentes. GREEN: 51 casos de preferências dentro de 99 Core passaram; build completo agora validado. Precedência, snapshots separados, bindings e posições relativas conferidos.

## T18

RED observado por Storage/AtomicFileWriter ausentes. GREEN: 41 testes Integration totais (19 novos de JSON, 19 anteriores e 3 RAR) passaram; build sem avisos. Backup, corrupção preservada, futuro read-only, cancelamento e escrita interrompida conferidos.

## T09

Três casos reais RAR passaram dentro de 41 Integration: RAR4/RAR5 não sólidos e RAR5 sólido via Reader sequencial. SHA e cinco pixels JPEG independentes conferidos, arquivo liberado/imutável, JPEG materializado lido duas vezes. Fixtures MIT e limites do caso documentados.

## T13

RED API ausente; GREEN 107 testes Core. Deslocamento 600 DIPs em 30/60/120Hz; oposição, diagonal, pausa 50ms, liberação e bordas verificados. App build Release sem warnings.

## Retomada automática — 10/10/2026

A pasta inicial da sessão continha a investigação; o repositório foi recuperado em Documents/LeitorHQ, branch `feat/comicbookreader`. Mudanças anteriores de persistência, CBR e cache foram preservadas, inspecionadas e validadas. Não se inferiu RED retroativo para esses arquivos. A integração inicialmente não compilava por CA2014 num teste: stackalloc foi movido para fora do loop, sem supressão. Base recuperada: 122 Core e 72 Integration passaram.

## T08/T11/T12/T14/T15/T16

Janela WPF, controller, teclas separadas, geometria e overview já estavam implementados nos commits anteriores. A retomada ampliou o smoke real: imagem, failed/canceled open, F11/Esc, resize, overview exato, duas resoluções duplas, geração tardia, eventos de rolagem/release/foco e dez segundos de callbacks. Controles temporários foram reproduzidos como falha em WPF e corrigidos sem mudar o viewport. T08/T12/T14 conservam casos físicos/visuais pendentes; T11/T15/T16 possuem evidência automática.

RED/GREEN adicional: ajustar a largura em loading mudava o modo para Reading; o controller agora preserva Loading. Cancelar durante decode que ignora token publicava pixels; os testes passaram após a verificação do token na abertura/navegação e preservação de overview. Progresso do RAR sólido agora passa ao estado/UI e ignora gerações antigas.

## T19/T20/T21

Fila de autosave validada com concorrência, debounce, flush, falha/retry e snapshots independentes. Preferências recuperadas por livro/página; captura em overview salva o ponto original. Dois processos WPF sucessivos comprovam retomada de página, largura, classificação e posição. Captura completa de preferências saiu do quadro de rolagem e foi consolidada em 300 ms. T21 tem regras/controles implementados; interação física com campos/atalhos permanece pendente.

## T22/T23/T24/T25

Fixtures RAR4/RAR5/sólida abriram na janela e em testes, com arquivo liberado e hash igual. RAR sólido usa cache privado, nomes gerados, marcadores/lease de posse, recuperação não recursiva e limites. Traversal, proteção, multivolume, limite, cancelamento e cache abandonado testados.

RED/GREEN novo: opener normal decodificava a mesma página duas vezes; wrapper de cache integrado reduz para uma e preserva pixels. Testes específicos dos adapters brutos continuam exercendo seus fechamentos, enquanto testes do wrapper cobrem cancelamento/prioridade/leases. RED/GREEN de entrada auxiliar grande retornava NoImages em vez de ResourceLimit; limite aplicado também aos auxiliares.

## T26/T27

Revisão em docs/review.md, com achados corrigidos. Smoke em 96 DPI, resize 1000×760 → 800×600, hardware e cadência registrados em docs/manual-validation.md. Testes não substituem percepção visual nem troca real de DPI. T26 permanece aberto. Controle de mutação `>` → `>=` na fronteira do cache fez três testes falharem; condição restaurada e quatro casos passaram. Formatação corrigida em commit separado; nenhuma supressão ou teste ignorado introduzido.

## T28/T29

Cinco lockfiles gerados, restore bloqueado verificado. Primeira publicação bloqueada apontou NU1004 por RID ausente nos projetos referenciados; `RuntimeIdentifiers=win-x64` comum e regeneração resolveram sem atualizar versões. `scripts/publish.ps1` gerou pacote self-contained em artifacts/win-x64 com decoder nativo, licenças e README.

Auditoria NuGet dos cinco projetos, incluindo transitivas: nenhum pacote vulnerável conhecido reportado pelas fontes consultadas. Build final Release com warnings como erros: zero avisos/erros. 126 testes Core e 74 Integration passaram, sem skips; format verify passou. Não há meta percentual de cobertura.

O pacote executou CBZ/RAR4/RAR5/sólido e dois processos de retomada, com hashes preservados. Uma execução adicional apontou DOTNET_ROOT e DOTNET_ROOT_X64 para pasta inexistente; os quatro formatos passaram usando o runtime do pacote. Evidência local em artifacts/self-contained-validation. Não houve envio externo ou merge.

## T30 / próxima retomada

docs/acceptance.md relaciona todos os IDs à evidência. 24 tarefas concluídas; T08/T12/T14/T21/T26/T30 mantêm verificações humanas específicas em docs/manual-validation.md. A distribuição de avaliação está preparada para realizar esses casos no executável final. Não declarar a aceitação visual completa com base nos smoke tests.

## Feedback de teclado — 10/10/2026

O usuário solicitou Espaço para descer/avançar, rolagem lateral com overflow e correção de Page Down. O enum WPF `Key.PageDown.ToString()` produziu `Next`; o smoke roteado reproduziu a falha antes da normalização e passou depois. Page Up e Enter também recebem nomes canônicos explícitos.

`ScrollForwardAsync` desce 90% do viewport, preserva X e mostra o fim antes de avançar no próximo toque. Páginas sem overflow avançam diretamente; última página e loading são respeitados. Espaço ignora auto-repeat e mantém prioridade de eventual atalho personalizado existente, sem modificar o schema/perfil. Três testes novos falharam por API ausente e passaram após implementação; o smoke de Espaço também falhou antes de integrar a tecla.

Setas laterais já tinham cálculo de deslocamento; três casos adicionais verificam largura sem/com overflow e bordas. O smoke reproduziu foco ainda no seletor após fechar e vazamento de setas a partir do ComboBoxItem. O fechamento/clique na imagem recupera foco; guard inclui ancestrais de controles editáveis. O fechamento do popup é assíncrono; teste aguarda condição com prazo de três segundos. Uma tentativa de reaproveitar evidência após smoke interrompido encontrou snapshot esperado antigo; as execuções seguintes usaram diretórios novos, preservando a evidência anterior.

Verificação de desenvolvimento em `artifacts/keyboard-fix-validation-09`: CBZ com duas inicializações e RAR4/RAR5/sólido passaram, hashes preservados. 132 Core + 74 Integration passaram, sem skips; build Release com zero avisos/erros e format verify passou. A entrada roteada observa modificadores físicos do Windows e não substitui o teste humano de teclado/repetição. Pacote atualizado preparado em `artifacts/win-x64-keyboard`, preservando a distribuição anterior. O pacote passou os quatro formatos e retomada, sem runtime instalado e com hashes preservados; evidência em `artifacts/keyboard-package-validation`.

## Preferências, foco, idiomas e ícone — 10/10/2026

Os oito itens do novo feedback foram implementados. Botões da barra e fechamento de diálogos/seletor devolvem foco ao viewport pelo dispatcher; diálogos visíveis são respeitados e Espaço mantém a ativação nativa de botões focados. F12 alterna a barra, Ctrl+P abre preferências e ambos são configuráveis. A barra não reage mais ao movimento do mouse ou a um timer; um botão ☰ permite reabri-la pelo mouse. Os handlers da janela foram separados em parciais de comandos e apresentação.

Preferências globais agora incluem passo do Espaço entre 10% e 100%, classificação Auto/Single/Spread e idioma pt-BR/en. A classificação vale para todas as páginas/livros; mapas antigos por imagem ficam preservados no JSON. Perfis antigos recebem valores padrão; se F12/Ctrl+P já estiverem associados, os novos comandos recebem combinações livres sem sobrescrever os atalhos existentes. Campos inteiros têm controle numérico com incremento, limites, rejeição de texto/colagem inválidos e provider de acessibilidade RangeValue. O idioma é aplicado ao salvar sem reiniciar. O ícone original de HQ inclui versões 16/32/48/256 px, fonte SVG e gerador local reproduzível.

RED observado para as novas opções ausentes e roundtrip do passo de 45% retornando 90%; GREEN após implementar domínio, resolver e DTO. 145 Core + 75 Integration passaram, sem skips; build Release com warnings como erros sem avisos/erros e format verify passou. Os testes cobrem limites 10/45/100%, valores inválidos, classificação global e compatibilidade de perfis/atalhos antigos. A revisão adicional encontrou consumo de Espaço em botão focado e falta de reabertura pelo mouse; ambos foram corrigidos. A inspeção das capturas encontrou rótulos escuros no diálogo; foreground claro aplicado e nova captura inspecionada.

Smoke de desenvolvimento em `artifacts/reader-options-final-validation`: quatro formatos e duas inicializações de CBZ passaram. O pacote self-contained em `artifacts/win-x64-options` passou CBZ, RAR4, RAR5 e RAR sólido, com hashes das fontes preservados e sem runtime instalado. Evidência em `artifacts/reader-options-package-validation`: foco após Largura padrão, ausência de revelação por mouse, F12 a partir do seletor, abertura modal pelo comando configurado, campos numéricos, tradução ao aplicar, capturas pt-BR/en e recuperação de idioma/classificação/passo de 45% pelo processo seguinte. A entrada roteada usa F10 temporário para abrir o diálogo sem simular modificadores físicos; o padrão Ctrl+P é coberto no domínio. Pressão física, suavidade e outro DPI continuam nas verificações humanas já registradas. Nenhuma dependência foi atualizada; não houve merge ou publicação externa.

## Atalhos de largura e arraste — 10/10/2026

R restaura a largura padrão removendo o override do livro; aparece nas preferências e na dica do botão. Os comandos de aumentar/diminuir já existentes receberam dicas legíveis e aliases Add/Subtract e Shift+OemPlus enquanto usam os padrões OemPlus/OemMinus. Combinações explícitas têm prioridade; personalizar um comando remove seus aliases. Perfis antigos que já usam R mantêm essa atribuição e recebem uma combinação livre para ResetWidth. Core/Storage validam compatibilidade, defaults e roundtrip dos três atalhos; o smoke testa padrões e remapeamentos F6/F7/F8.

O arraste com botão esquerdo usa captura WPF e posições em DIPs, acompanhando o cursor nos eixos com overflow. Deslocamento incremental permite inverter imediatamente nas bordas, sem trocar página. Release, perda de foco/captura, teclado/roda, navegação, mudança geométrica e fechamento cancelam o gesto. A posição continua sendo persistida pelo fluxo existente. O Core conserva a regra de gesto separado do adapter WPF, que controla captura/cursor e se desinscreve no fechamento.

RED: comando ResetWidth ausente, APIs de arraste ausentes e botão esquerdo sem iniciar gesto na janela. GREEN: 12 novos casos DragPanTests e o smoke passaram. A primeira expectativa de offset assumia que zoom deixava X em zero; o teste passou a estabelecer explicitamente a posição inicial, pois o zoom preserva o centro. Captura pode emitir movimento síncrono: ela é adquirida antes de ativar o gesto. Um smoke completo reproduziu acesso fora da thread WPF no progresso de RAR sólido; ReaderChanged agora usa o dispatcher e o caso passou. O teste de defaults da persistência passou a esperar os dez comandos e verificar R.

Validação final: 158 Core + 75 Integration passaram, sem skips; build Release com warnings como erros sem avisos/erros, format verify e diff check passaram. Desenvolvimento em artifacts/drag-green-validation-04 e pacote self-contained em artifacts/drag-package-validation passaram CBZ, RAR4, RAR5 e RAR sólido; CBZ também passou a segunda inicialização com retomada e hashes das fontes preservados. PointerChecks registra captura/liberação, movimento nos dois eixos por coordenadas determinísticas, perda de foco/captura e interrupção por tecla/navegação. A automação não movimenta o cursor físico; movimento real fora do viewport, outro DPI e avaliação de suavidade continuam na matriz humana.

Novo pacote em artifacts/win-x64-drag, preservando as versões anteriores. Nenhuma dependência nova, supressão, merge ou publicação externa. Contrato de captura conferido na [documentação oficial de UIElement.CaptureMouse](https://learn.microsoft.com/en-us/dotnet/api/system.windows.uielement.capturemouse?view=windowsdesktop-10.0); eventos diretos conferidos em [MouseLeftButtonDown](https://learn.microsoft.com/en-us/dotnet/api/system.windows.uielement.mouseleftbuttondown?view=windowsdesktop-10.0).

## Distribuição portátil e instalador — 10/10/2026

Autorização do usuário para gerar ambos os formatos. Versão 0.1.0, ícone existente e runtime .NET incluído. `build-distribution.ps1` publica a pasta, agrega as licenças completas dos pacotes efetivamente resolvidos, incorpora esses textos ao recurso da App e gera portátil comprimido sem trimming e instalador Inno Setup 6.7.3. O portátil contém somente um EXE; PDB nativo de 96 MB é excluído pelo target de publicação e `*.pdb` fica fora do instalador. README pt-BR/en e SHA256SUMS acompanham a pasta de entrega, mas não são necessários para executar/compartilhar um dos arquivos.

Sobre apresenta versão, ícone e texto copiável das licenças, com rótulos em pt-BR/en. RED: o smoke não compilava com AboutWindow ausente. GREEN: o diálogo renderiza e os textos têm SHA256 igual à agregação integral das licenças nos dois formatos. Capturas finais de ambos os idiomas inspecionadas: rótulos e botão Fechar/Close legíveis.

A primeira publicação single-file bloqueada apontou ausência de Microsoft.NET.ILLink.Tasks no lockfile. Habilitar o analyzer na App e usar BuildPortable somente nela fixou o pacote 10.0.12 sem atualizar as bibliotecas do leitor nem desativar restore bloqueado. O compilador Inno foi obtido da versão oficial, com SHA256 `9c73c3bae7ed48d44112a0f48e66742c00090bdb5bef71d9d3c056c66e97b732` e Authenticode válido de Pyrsys B.V. Sua FileVersion é 0.0.0.0; a checagem correta usa Ver/EncodeVer no pré-processador e compilação real 6.7.3.

A revisão adicional apontou que DOTNET_ROOT inexistente sozinho não registra a origem do runtime. O smoke passou a verificar módulos reais. A primeira expectativa de coreclr.dll separado falhou no portátil; o [código oficial .NET 10.0.12](https://github.com/dotnet/runtime/blob/v10.0.12/src/native/corehost/apphost/static/CMakeLists.txt) confirmou CoreCLR estático no singlefilehost para Windows. O teste exige export DotNetRuntimeInfo no próprio EXE, além de libSkiaSharp na extração privada; na versão instalada, exige ambos os DLLs sob a instalação. O JSON registra esses caminhos. Não foi alterado o carregamento de produção nem enfraquecida a verificação de dependências incluídas.

Validação final em `artifacts/distribution-validation/e04dcbeba0cc4b53b4365f90e5af4259`: portátil sozinho, renomeado e em pasta com espaços; CBZ com duas inicializações, RAR4/RAR5/sólido, licenças completas, leitura/atalhos/arraste e hashes das HQs preservados. Instalação por usuário em pt-BR, atalhos do menu Iniciar/desktop, reinstalação em inglês e execução dos mesmos formatos na versão instalada passaram. Desinstalação removeu executável, atalhos e HKCU, preservando conteúdo extra na instalação e dados/preferências reais. Nenhuma HQ pessoal ou processo do usuário foi alterado.

158 Core + 75 Integration passaram, sem skips; build Release com warnings como erros sem avisos/erros; format verify e diff check passaram. Auditoria NuGet de todos os projetos/transitivas não reportou vulnerabilidades conhecidas nas fontes consultadas. Artefatos finais: `ComicBookReader-0.1.0-Portable.exe` (71.519.511 bytes, SHA256 `91ffe336178f6d020f61a54fecc8506f36c79c7202e2802243724e5b208d75c3`) e `ComicBookReader-0.1.0-Setup.exe` (51.405.553 bytes, SHA256 `1ab2f752039e77a208d92ccc55781b72b7b95a3ff3d3044e7f8eb0f852f7158a`). A distribuição anterior foi preservada. Não houve merge, publicação externa ou declaração de aceitação visual completa do leitor.
