# Espaço e sentido de leitura — 30/10/2026

Versão local 0.2.0, branch `feat/space-reading-order`. O Espaço percorre cada faixa horizontal no sentido configurado, mostra sua borda final, desce e retorna ao início da faixa seguinte. O trecho inferior final aparece antes da troca de página. A distância usa o percentual existente sobre a largura/altura útil conforme o eixo. Setas e comandos explícitos de navegação conservam sua função.

Preferências oferece esquerda → direita (padrão) e direita → esquerda (mangá), em pt-BR/en. Mudar o sentido mantém a altura e retorna ao início da faixa atual; páginas novas começam no topo e na borda escolhida. Reabrir recupera o ponto salvo. A escolha é global e não inverte o índice dos arquivos.

## Evidência

- RED inicial: três casos reproduziram o avanço vertical/página antes de mostrar o conteúdo horizontal. GREEN após corrigir o percurso.
- RED de direção: enum/opção ausentes no Core e controle ausente na janela; roundtrip retornava LeftToRight após salvar RightToLeft. GREEN após integrar modelo, validação, DTO e preferências.
- 171 testes Core e 75 Integration passaram, sem skips. Cobrem dois sentidos, percurso completo, overflow somente horizontal, bordas finais, última página, passos 10/45/100%, overview, retomada e perfil antigo.
- Mutação temporária inverteu `hasMoreHorizontally`; os testes de regressão falharam. O arquivo foi restaurado e a suíte completa Core passou. Log local: `artifacts/reading-order-mutation.txt`.
- Build Release com avisos como erros passou; format verify e diff check passaram. O checkout CRLF contrariava `.editorconfig`; `.gitattributes` mantém fontes C# em LF. Renormalizar o índice não gerou alterações de conteúdo em arquivos alheios à tarefa.
- Janela WPF em `artifacts/reading-order-runtime`: CBZ em dois processos, RAR4/RAR5/sólido, hashes das HQs preservados. `SpaceReadingSmokeChecks` envia Espaço pelo input real da janela, percorre todas as faixas nos dois sentidos e exige as duas bordas antes de trocar a página. O segundo processo confere a direção salva. Capturas pt-BR/en do seletor foram inspecionadas.
- Portátil 0.2.0 em `artifacts/reading-order-portable`: os quatro formatos e a segunda inicialização CBZ passaram; `SpaceReadsAllRowsInBothDirections` e `RestartVerified` são true. Os módulos carregados comprovam runtime incorporado ao EXE e decoder extraído do bundle. Licenças incorporadas também conferidas.

## Pacotes locais

`artifacts/distribution/ComicBookReader-0.2.0-Portable.exe` e `ComicBookReader-0.2.0-Setup.exe` foram gerados. Os hashes dos dois executáveis 0.1.0 permanecem iguais aos registrados. O compilador do instalador foi executado; o ciclo de instalação/desinstalação não foi repetido nesta alteração, que não modifica o script de instalação.

| Arquivo | SHA256 |
| --- | --- |
| ComicBookReader-0.2.0-Portable.exe | `6a209901deecbedc6fb9ac99a08c1281ee5dd39735a1f7f573c95b06e795601d` |
| ComicBookReader-0.2.0-Setup.exe | `320f7cb46e152cbde09aa8e3844cf9468fe7678e853d2cad740a31b10c8f31e8` |

## Revisão e limites

Revisados comportamento, fronteiras, clareza, separação Core/WPF/DTO, validação do enum e custo do comando. Nenhuma dependência nova, supressão, teste pulado ou alteração na ordem dos arquivos. O reset horizontal ocorre junto da nova geometria, antes de emitir o estado para persistência; overview não deixa snapshot obsoleto. Não foram encontrados bloqueadores.

Os testes WPF usam input roteado e janela fora da tela. Interação física, percepção de suavidade e outros monitores/DPI continuam no [roteiro humano](manual-validation.md); não houve merge, tag ou publicação externa da 0.2.0.

## Seletor na barra — 0.2.1

Solicitação do usuário: expor sentido de leitura na barra. Adicionado ComboBox ao lado do tipo de página, reutilizando textos pt-BR/en e a preferência global existente. Alterar a escolha aplica o comportamento imediatamente, persiste e sincroniza o diálogo; alterações nas Preferências também atualizam a barra. Fechar os dois seletores usa o mesmo retorno de foco. O getter do controller evita copiar o perfil a cada quadro apenas para atualizar a seleção.

RED: teste WPF não compilava com `ReadingDirectionChoice` ausente da janela. GREEN após integrar XAML, handler e apresentação. O primeiro smoke detectou duas janelas de Preferências criadas pelo teste sem fechamento; o helper passou a encerrá-las em finally. Não foi alterado o tratamento de erros de produção para esconder a falha.

171 Core e 75 Integration passaram, sem skips. Build Release com avisos como erros, format verify e diff check passaram. `ReadingDirectionToolbarSmokeChecks` comprova seleção nos dois sentidos, sincronização com o diálogo, aplicação inversa por preferências, manutenção da geometria/viewport, setas locais ao seletor, retorno de foco e ausência de clipping na largura mínima de 640 DIPs. Capturas de ambas as traduções foram inspecionadas. Mutação temporária inverteu a condição do handler; o teste falhou porque a seleção deixou de alterar a preferência. Fonte original restaurada e aplicação recompilada.

Evidência: `artifacts/direction-toolbar-runtime-03` e `artifacts/direction-toolbar-portable`. CBZ com duas inicializações e RAR4/RAR5/sólido passaram, com hashes das fontes preservados. O portátil inclui runtime/decoder e licenças conferidos. As versões anteriores foram preservadas. O instalador foi compilado; instalação/desinstalação não foram repetidas, pois o script não mudou.

Pacotes 0.2.1 em `artifacts/distribution`: portátil SHA256 `f4c124628de89b76f499c274e4f7f34cfc259248f1b1c24e8dda4d54b22a9197`; instalador SHA256 `11398d7d649d8d915aef88e7bb4e440117c4bac5b7dd11cf8323d99d87c1987f`. Revisão sem bloqueadores, dependências novas ou supressões. Limites humanos de teclado, DPI e percepção continuam no roteiro; não houve merge, tag ou publicação externa da 0.2.1.
