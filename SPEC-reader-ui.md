# Spec: reader-ui

Produto: ComicBookReader. Status: contrato autorizado por `$build`; implementação local com aceitação humana pendente em [docs/acceptance.md](docs/acceptance.md). Módulo do [mapa aprovado](CAPABILITY-MAP.md); requisitos comuns em [SPEC.md](SPEC.md).

## Objetivo

Oferecer leitura em tela cheia com rolagem contínua e controlada, navegação separada e consulta temporária à página inteira. A interface compõe os contratos de arquivos, preferências e layout; é a dona da sessão de leitura e dos estados temporários.

## Janela e controles

- Sentido de leitura também fica acessível no seletor da barra, ao lado do tipo de página. Barra e Preferências usam a mesma escolha global, com aplicação imediata, persistência e tradução pt-BR/en. Fechar o seletor retorna o foco à leitura.
- Cliques nos botões internos dos seletores não são ações de retorno à leitura. A barra mantém o foco no menu enquanto a lista estiver aberta; selecionar um item ou cancelar com Esc fecha a lista e retorna o foco à página.
- Abrir em tela cheia na primeira execução e recuperar a escolha nas seguintes. F11 alterna janela/tela cheia; Esc sai da tela cheia e fecha um painel/diálogo ativo antes de afetar a leitura.
- Usar o monitor da janela atual, sem resolução fixa. Área útil é o viewport menos margens propostas de 12 DIPs por lado; o layout recebe essa área já descontada.
- Fundo escuro neutro, imagem proporcional e controles discretos para abrir arquivo, página atual/total, tamanho de leitura, tipo de página e preferências.
- A barra sobrepõe a imagem sem recalcular seu tamanho. F12 mostra/esconde; movimento do mouse não a revela. Um botão ☰ permite reabrir pelo mouse. Tab/Alt revelam controles para acesso por teclado. Diálogos suspendem a rolagem; setas em campos de edição não comandam a página.
- Abrir arquivo por Ctrl+O ou arrastar e soltar. Exibir estado de carregamento; um erro de abertura mantém o livro anterior utilizável. Se cancelar a abertura, retornar à leitura anterior.
- Ao abrir o livro, recuperar última página válida e enquadramento; primeira leitura começa na primeira imagem, no topo e na borda inicial do sentido de leitura: esquerda no ocidental, direita no modo mangá.

## Comandos e padrões propostos

| Comando | Padrão | Semântica |
| --- | --- | --- |
| Rolar verticalmente | ↑/↓ mantidas | Movimento contínuo dentro da imagem atual. |
| Rolar horizontalmente | ←/→ mantidas | Movimento contínuo em imagens que ultrapassam a largura. |
| Próxima/anterior página | Page Down/Page Up | Uma troca por pressão física, ignorando auto-repeat. |
| Primeira/última página | Ctrl+Home/Ctrl+End | Ir diretamente à primeira ou última página da HQ. |
| Miniaturas | T | Exibir as páginas em miniatura e navegar ao selecionar uma delas; Esc fecha o painel. |
| Percorrer ou avançar | Espaço | Percorrer horizontalmente no sentido escolhido (padrão 90% da largura útil); na borda, descer o passo configurado da altura útil e retornar ao início da faixa. Após o último trecho da última faixa, avançar uma página. Ignorar auto-repeat. |
| Alternar barra | F12 | Mostrar/esconder sem depender de mouse ou timer. |
| Preferências | Ctrl+P | Abrir o diálogo com rolagem suspensa. |
| Visão completa/restaurar | V | Alternância, uma ação por pressão física. |
| Tela cheia | F11 | Alternância sem perder a página. |
| Abrir arquivo | Ctrl+O | Seletor de CBZ/CBR. |
| Aumentar/diminuir tamanho | +/−; Ctrl+roda | Ajustar largura preferida do livro; passo inicial proposto de 5%. |
| Largura padrão | R | Remover o override de largura do livro e usar a preferência global. |
| Alternar sentido de leitura | M | Inverter entre esquerda→direita e direita→esquerda, preservando a página atual e salvando a preferência global. |
| Rolar com mouse | Roda; Shift+roda | Vertical/horizontal, sem troca automática de página. |
| Arrastar a página | Botão esquerdo mantido + movimento | Acompanhar o cursor nos eixos com overflow, sem trocar página nas bordas. |

Atalhos são configuráveis e conflitos devem ser apresentados. As setas permanecem comandos de rolagem: a configuração não deve permitir associá-las à troca de página. O nome/tipo de comando é a regra; as teclas não confirmadas pelo usuário são defaults propostos.

Aumentar/diminuir e Largura padrão têm campos de atalho nas preferências e dicas na barra. O padrão OemPlus também aceita Shift+OemPlus e Add; OemMinus também aceita Subtract. Uma atribuição explícita à combinação tem prioridade; personalizar ZoomIn/ZoomOut remove seus aliases. O arraste captura o mouse no viewport, usa coordenadas em DIPs e deslocamentos relativos ao último movimento para inverter imediatamente nas bordas. Soltar o botão, perder foco/captura, redimensionar, navegar, abrir livro, abrir diálogo ou usar teclado/roda encerra o gesto. Overview/loading/ausência de overflow não iniciam arraste.

Espaço percorre a imagem por faixas, no sentido global escolhido em Preferências: LeftToRight (padrão ocidental) ou RightToLeft (mangá). Primeiro mostra o trecho horizontal seguinte, limitado à borda e com sobreposição conforme o passo configurado. Somente quando a borda final da faixa já estiver visível, desce e retorna à borda inicial. O último trecho horizontal da última faixa deve aparecer antes de avançar; overflow somente horizontal também precisa ser percorrido. Uma página que cabe inteira avança diretamente; na última página, permanece no trecho final. Alterar o sentido mantém a altura e se alinha ao início da faixa atual, encerrando overview; reabrir mantém o ponto salvo. A direção não inverte o índice nem muda comandos explícitos de navegação. Na visão completa, Espaço avança à próxima em modo de leitura; durante loading, não interfere no alvo. Uma atribuição personalizada existente a Espaço tem prioridade sobre essa ação padrão, sem migração do perfil. Fechar o dropdown de classificação ou clicar na área de leitura recupera seu foco; teclas dentro dos controles de edição continuam locais ao controle.

## Rolagem contínua

Capturar KeyDown/KeyUp como estado pressionado e atualizar a posição a cada quadro, com tempo monotônico. Não enfileirar um deslocamento por evento de repetição do Windows. Padrão de velocidade: 0,6 × altura útil em DIPs por segundo, configurável conforme o contrato de preferências.

Enquanto uma direção está ativa, deslocamento é velocidade × tempo decorrido e é limitado pelas bordas calculadas pelo layout. Direções opostas no mesmo eixo se anulam. Em diagonal, normalizar a direção para não aumentar a velocidade total. Ao atingir uma borda, permanecer nela; continuar segurando ou pressionar novamente a seta nunca muda de página.

Soltar tecla, perder foco, abrir um diálogo, trocar de página ou alternar visão completa limpa o estado pressionado. Ao recuperar o foco, só uma nova pressão inicia movimento. O movimento não tem inércia após soltar. Reinicializar o relógio ao retomar e limitar o intervalo aplicado a no máximo 50 ms para evitar um salto após pausa do processo.

WPF fornece o evento de renderização em [CompositionTarget.Rendering](https://learn.microsoft.com/en-us/dotnet/api/system.windows.media.compositiontarget.rendering?view=windowsdesktop-10.0); é uma possibilidade de integração, não uma garantia de taxa de quadros. A regra temporal fica testável sem depender da janela.

## Navegação e carregamento

Comando separado de navegação solicita o índice anterior/seguinte. Primeira e última página são limites; não carregar outro arquivo automaticamente. Ao confirmar uma mudança, encerrar visão completa e aplicar preferência/classificação da nova imagem, começando no topo e na borda inicial do sentido escolhido. Não reutilizar posição da imagem anterior.

Cada solicitação tem uma geração. Cancelar a anterior e, mesmo se um decoder não parar imediatamente, descartar resultados/erros cuja geração não seja a atual; liberar o recurso retornado. Durante carregamento, a página mostrada pode permanecer visível com indicador; seus controles de rolagem ficam suspensos para não salvar estado no alvo errado. Somente a imagem correspondente ao alvo atual é apresentada como leitura pronta.

O tamanho manual permanece relativo à largura de uma página e usa a faixa do contrato de preferências; não mostrar uma porcentagem do scan como único controle. Zoom manual preserva o ponto sob o cursor quando houver cursor útil, ou o centro da área de leitura. Classificação oferece Automática/Simples/Dupla global, aplicada a todas as imagens e livros. Botões retornam o foco à leitura após a ação/finalização de diálogos, no dispatcher; Espaço ativa normalmente um botão selecionado pelo teclado. Preferências oferecem idioma pt-BR/en aplicado ao salvar e campos numéricos inteiros com incremento, limites e validação de colagem.

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
rtk proxy "C:\Users\ingos\AppData\Local\ComicBookReader-dev\dotnet-10.0.401\dotnet.exe" build ComicBookReader.slnx --configuration Release -warnaserror
rtk proxy "C:\Users\ingos\AppData\Local\ComicBookReader-dev\dotnet-10.0.401\dotnet.exe" run --project tests/ComicBookReader.Core.Tests/ComicBookReader.Core.Tests.csproj --configuration Release
rtk proxy "C:\Users\ingos\AppData\Local\ComicBookReader-dev\dotnet-10.0.401\dotnet.exe" run --project src/ComicBookReader.App/ComicBookReader.App.csproj --configuration Debug
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
| UI-10 | Espaço percorre cada faixa nos dois sentidos, com sobreposição/clamp horizontal e vertical, mostra o trecho inferior final antes de avançar; respeita a última página, perfil antigo e loading. | Unidade, persistência e input roteado na janela. |

Para UI-09, registrar também cadência dos quadros se houver trancos; a aprovação visual não é substituída pelo teste de deslocamento. Testes manuais precisam incluir página dupla, diálogo durante tecla pressionada, troca rápida de páginas e retomada da leitura.

## Limites e perguntas abertas

Setas mantêm rolagem e navegação separadas; Espaço combina as ações conforme a solicitação do usuário após testar o protótipo. Preservar foco e liberar snapshots obsoletos; revisar com o usuário outras mudanças desses comportamentos; nunca alterar o tamanho preferido só porque a imagem entrou em paisagem ou visão completa. Teclas/velocidade propostas poderão ser ajustadas no protótipo. A suavidade percebida exige validação em execução.
