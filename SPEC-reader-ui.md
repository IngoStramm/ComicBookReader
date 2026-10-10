# Spec: reader-ui

Produto: ComicBookReader. Status: proposta para revisão. Módulo do [mapa aprovado](CAPABILITY-MAP.md); requisitos comuns em [SPEC.md](SPEC.md).

## Objetivo

Oferecer leitura em tela cheia com rolagem contínua e controlada, navegação separada e consulta temporária à página inteira. A interface compõe os contratos de arquivos, preferências e layout; é a dona da sessão de leitura e dos estados temporários.

## Janela e controles

- Abrir em tela cheia na primeira execução e recuperar a escolha nas seguintes. F11 alterna janela/tela cheia; Esc sai da tela cheia e fecha um painel/diálogo ativo antes de afetar a leitura.
- Usar o monitor da janela atual, sem resolução fixa. Área útil é o viewport menos margens propostas de 12 DIPs por lado; o layout recebe essa área já descontada.
- Fundo escuro neutro, imagem proporcional e controles discretos para abrir arquivo, página atual/total, tamanho de leitura, tipo de página e preferências.
- Controles temporários sobrepõem a imagem sem recalcular seu tamanho a cada aparição. Diálogos suspendem a rolagem; setas em campos de edição não comandam a página.
- Abrir arquivo por Ctrl+O ou arrastar e soltar. Exibir estado de carregamento; um erro de abertura mantém o livro anterior utilizável. Se cancelar a abertura, retornar à leitura anterior.
- Ao abrir o livro, recuperar última página válida e enquadramento; primeira leitura começa na primeira imagem, na borda superior/esquerda dos eixos com overflow.

## Comandos e padrões propostos

| Comando | Padrão | Semântica |
| --- | --- | --- |
| Rolar verticalmente | ↑/↓ mantidas | Movimento contínuo dentro da imagem atual. |
| Rolar horizontalmente | ←/→ mantidas | Movimento contínuo em imagens que ultrapassam a largura. |
| Próxima/anterior página | Page Down/Page Up | Uma troca por pressão física, ignorando auto-repeat. |
| Visão completa/restaurar | V | Alternância, uma ação por pressão física. |
| Tela cheia | F11 | Alternância sem perder a página. |
| Abrir arquivo | Ctrl+O | Seletor de CBZ/CBR. |
| Aumentar/diminuir tamanho | +/−; Ctrl+roda | Ajustar largura preferida do livro; passo inicial proposto de 5%. |
| Rolar com mouse | Roda; Shift+roda | Vertical/horizontal, sem troca automática de página. |

Atalhos são configuráveis e conflitos devem ser apresentados. As setas permanecem comandos de rolagem: a configuração não deve permitir associá-las à troca de página. O nome/tipo de comando é a regra; as teclas não confirmadas pelo usuário são defaults propostos.

## Rolagem contínua

Capturar KeyDown/KeyUp como estado pressionado e atualizar a posição a cada quadro, com tempo monotônico. Não enfileirar um deslocamento por evento de repetição do Windows. Padrão de velocidade: 0,6 × altura útil em DIPs por segundo, configurável conforme o contrato de preferências.

Enquanto uma direção está ativa, deslocamento é velocidade × tempo decorrido e é limitado pelas bordas calculadas pelo layout. Direções opostas no mesmo eixo se anulam. Em diagonal, normalizar a direção para não aumentar a velocidade total. Ao atingir uma borda, permanecer nela; continuar segurando ou pressionar novamente a seta nunca muda de página.

Soltar tecla, perder foco, abrir um diálogo, trocar de página ou alternar visão completa limpa o estado pressionado. Ao recuperar o foco, só uma nova pressão inicia movimento. O movimento não tem inércia após soltar. Reinicializar o relógio ao retomar e limitar o intervalo aplicado a no máximo 50 ms para evitar um salto após pausa do processo.

WPF fornece o evento de renderização em [CompositionTarget.Rendering](https://learn.microsoft.com/en-us/dotnet/api/system.windows.media.compositiontarget.rendering?view=windowsdesktop-10.0); é uma possibilidade de integração, não uma garantia de taxa de quadros. A regra temporal fica testável sem depender da janela.

## Navegação e carregamento

Comando separado de navegação solicita o índice anterior/seguinte. Primeira e última página são limites; não carregar outro arquivo automaticamente. Ao confirmar uma mudança, encerrar visão completa e aplicar preferência/classificação da nova imagem, começando no topo/esquerda. Não reutilizar posição da imagem anterior.

Cada solicitação tem uma geração. Cancelar a anterior e, mesmo se um decoder não parar imediatamente, descartar resultados/erros cuja geração não seja a atual; liberar o recurso retornado. Durante carregamento, a página mostrada pode permanecer visível com indicador; seus controles de rolagem ficam suspensos para não salvar estado no alvo errado. Somente a imagem correspondente ao alvo atual é apresentada como leitura pronta.

O tamanho manual permanece relativo à largura de uma página e usa a faixa do contrato de preferências; não mostrar uma porcentagem do scan como único controle. Zoom manual preserva o ponto sob o cursor quando houver cursor útil, ou o centro da área de leitura. Classificação oferece Automática/Simples/Dupla e grava override do livro.

## Visão completa e restauração

V, no modo de leitura, salva um snapshot associado a BookId e PageKey com escala, offsets, preferência e viewport, interrompe rolagem e aplica o layout de visão completa. V novamente restaura o snapshot. No mesmo viewport, restaurar escala/offsets com erro máximo de 1 DIP.

Se o viewport mudou, reaplicar a preferência salva e restaurar a âncora da imagem, limitada aos novos limites. Se a página/livro mudou, descartar o snapshot anterior. Navegar no modo completo retorna ao modo de leitura na nova página. Zoom manual no modo completo encerra a alternância e passa a usar o novo tamanho escolhido; não restaurar depois um snapshot obsoleto.

Consultar visão completa não grava sua escala/posição como preferência/progresso. Salvar ou fechar nesse modo usa o ponto de leitura guardado. O estado temporário não precisa sobreviver ao reinício.

## Contratos e estado

Controller testável em `Core/Reading`: `ReaderMode` (Empty, Loading, Reading, Overview, PageError) e estado com livro, índice alvo, página apresentada, layout, offsets e geração. Operações incluem PressDirection, ReleaseDirection, AdvanceFrame(elapsed), Navigate(delta), ToggleOverview e Deactivate. A UI traduz eventos para operações; não implementa uma segunda versão das regras nas teclas/botões.

Estado geométrico usa tipos fornecidos por layout; dados/recursos de imagem usam contratos de arquivos; ajustes usam contratos de preferências. A parte WPF converte pixels para BitmapSource sem introduzir objetos WPF no Core. Snapshot pertence à sessão da interface, não ao store. Renderizar e descartar recursos deve respeitar o ciclo de vida combinado com `comic-files`.

## Estrutura, estilo e comandos

Janela/controles em `src/ComicBookReader.App`; estado testável em `src/ComicBookReader.Core/Reading`; seguir o [estilo comum](SPEC.md#estilo-de-código). Nomes de comandos expressam intenção, e rótulos não expõem detalhes internos de arquivos ou geometria.

```powershell
rtk proxy "C:\Program Files\dotnet\dotnet.exe" build ComicBookReader.slnx --configuration Release -warnaserror
rtk proxy "C:\Program Files\dotnet\dotnet.exe" run --project tests/ComicBookReader.Core.Tests/ComicBookReader.Core.Tests.csproj --configuration Release
rtk proxy "C:\Program Files\dotnet\dotnet.exe" run --project src/ComicBookReader.App/ComicBookReader.App.csproj --configuration Debug
```

## Estratégia de testes e critérios de aceitação

| ID | Caso e resultado esperado | Evidência |
| --- | --- | --- |
| UI-01 | Manter ↓ por 1 s em quadros simulados de 30/60/120 Hz produz o mesmo deslocamento, com erro máximo de 1 DIP, se nenhuma borda for atingida. | Unidade de estado/tempo. |
| UI-02 | Repetir KeyDown não aumenta a velocidade; KeyUp/foco perdido/diálogo interrompe o movimento. | Unidade e teste da janela real. |
| UI-03 | Atingir qualquer borda com seta e pressioná-la novamente mantém o índice, sem comandos enfileirados. | Unidade e teste manual. |
| UI-04 | Page Up/Page Down mudam um índice por pressão; limites do livro não abrem outro arquivo. | Unidade e teste manual. |
| UI-05 | V duas vezes recupera escala e offsets do mesmo viewport com erro ≤1 DIP; no modo completo, persistência mantém o ponto de leitura. | Unidade e integração. |
| UI-06 | Resultado atrasado de leitura anterior nunca substitui o alvo atual nem apresenta erro fora de contexto. | Unidade com tarefas controladas e integração. |
| UI-07 | Redimensionar/mudar DPI alternando modo de visão mantém proporção, âncora válida e controles acessíveis. | Unidade e manual Windows 11. |
| UI-08 | Corrigir a classificação e reabrir o livro recupera a preferência; uma imagem dupla continua sendo um índice. | Integração e manual. |
| UI-09 | Rolagem real por 10 s em página com overflow é visualmente contínua; não depende de ajustar a taxa de repetição do Windows. | Manual, registrando monitor/DPI e hardware. |

Para UI-09, registrar também cadência dos quadros se houver trancos; a aprovação visual não é substituída pelo teste de deslocamento. Testes manuais precisam incluir página dupla, diálogo durante tecla pressionada, troca rápida de páginas e retomada da leitura.

## Limites e perguntas abertas

Sempre manter rolagem e navegação separadas, preservar foco e liberar snapshots obsoletos; revisar com o usuário mudanças desses comportamentos; nunca alterar o tamanho preferido só porque a imagem entrou em paisagem ou visão completa. Teclas/velocidade propostas poderão ser ajustadas no protótipo. A suavidade percebida exige validação em execução; nenhuma foi afirmada nesta etapa documental.
