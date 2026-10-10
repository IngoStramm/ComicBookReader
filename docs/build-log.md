# Registro de construção

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
