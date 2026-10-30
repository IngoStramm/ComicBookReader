# Validação da janela

Data: 10/10/2026. Esta página distingue execução automatizada de avaliação humana. Os arquivos portátil e instalador 0.1.0 estão em `artifacts/distribution`. O pacote anterior `artifacts/win-x64-drag` foi preservado.

## Ambiente observado

Windows x64; AMD Ryzen 7 5800H; NVIDIA GeForce RTX 3050 Laptop GPU; resolução reportada 1920 × 1080 a 60 Hz. Existe também um adaptador virtual Parsec; não houve troca de monitor. A janela usada pelo smoke test mediu 96 DPI. Janela de 1000 × 760 DIPs e redimensionamento para 800 × 600 DIPs.

## Execução automatizada real

`scripts/verify-app.ps1` gera um CBZ sintético com imagens 1000 × 12000, 2000 × 1500 e 4000 × 3000. Executa a aplicação WPF fora da tela, abre esses arquivos e as fixtures RAR4/RAR5/sólida. Usa preferências privadas dentro do diretório de evidência. Nenhuma HQ pessoal é usada.

Resultados da execução de desenvolvimento em `artifacts/runtime-validation` e do pacote em `artifacts/published-validation`: quatro formatos passaram, hashes das fontes preservados. CBZ usa dois processos sucessivos; o segundo comprova página, largura, classificação e posição salvos ao fechar em visão completa.

A execução adicional em `artifacts/self-contained-validation` aponta `DOTNET_ROOT` e `DOTNET_ROOT_X64` para uma pasta inexistente, para verificar que o host publicado usa o runtime que acompanha o pacote.

Após feedback do usuário sobre Espaço, setas laterais e Page Down, a execução em `artifacts/keyboard-fix-validation-09` validou a atualização de desenvolvimento. A entrega usa outra pasta para preservar a versão em avaliação. A verificação do pacote atualizado fica em `artifacts/keyboard-package-validation`, também sem fallback para runtime instalado.

| Caso executado | Resultado |
| --- | --- |
| Abrir CBZ/RAR4/RAR5/RAR sólido e apresentar pixels | Passou. PNG da janela e `runtime.json` por formato. |
| Arquivo ausente e abertura cancelada por token | Livro anterior preservado. |
| F11/Esc pelo evento roteado da janela | Modo da janela alternado. |
| Roteamento de ↓ por 10 s e liberação | Deslocamento positivo; soltar encerra movimento. |
| Evento roteado de perda de foco | Direções limpas. |
| Pressionar ↓ na borda | Mesma página. |
| Page Down/Page Up por input roteado | Avançam/voltam um índice; aliases WPF normalizados. |
| Espaço por input roteado | Desce 90% da altura; mostra o fim e avança no toque seguinte. |
| ←/→ com largura 150% | Movem horizontalmente; a borda mantém a página. |
| Tecla no item do dropdown e fechamento | Não rola durante edição; fechar recupera o foco da área de leitura. |
| Barra manual | F12 alterna; mouse sobre a imagem não mostra a barra; botão ☰ reabre; Tab permite acesso por teclado. Layout não muda. |
| Duplas com duas resoluções distintas | Mesma largura de leitura. |
| V/V | Escala e offsets restaurados exatamente. |
| Navegação concorrente e resize | Último alvo apresentado; offsets limitados. |
| Dois processos; fechar em overview | Progresso de leitura original restaurado. |
| Largura padrão e seletor com foco | Botão retorna foco; V funciona logo após. F12 funciona a partir do seletor. |
| Preferências pelo comando configurado | Modal abre, aplica opções e retorna foco à leitura. |
| Campos numéricos | Texto/decimal/valor acima do máximo e colagem inválida rejeitados; vazio normalizado. |
| pt-BR/en | Diálogo e janela traduzidos; captura visual inspecionada para contraste. |
| Classificação global / passo do Espaço | Aplicados, persistidos e recuperados pelo processo seguinte. |
| R, +/− e Add/Subtract | Restauram/aumentam/diminuem a largura; personalizações F6/F7/F8 e ausência de alias após personalizar verificadas. |
| Botão esquerdo e arraste | Press/release roteados, captura WPF real e deslocamento em dois eixos por coordenadas determinísticas. Perda de foco/captura, tecla R e navegação liberam captura/cursor. |

Evidência de desenvolvimento final em `artifacts/reader-options-final-validation`; pacote final em `artifacts/reader-options-package-validation`. As capturas de preferências pt-BR/en e da barra em inglês acompanham o JSON. Os testes de Core/Storage cobrem 10/45/100% de passo, opções inválidas e perfis legados com F12/Ctrl+P personalizados. A revisão visual corrigiu rótulos que herdavam foreground escuro. Pressão física, suavidade e outro DPI continuam pendentes.

A atualização seguinte de largura/arraste foi verificada em `artifacts/drag-green-validation-04`; evidência do pacote atualizado em `artifacts/drag-package-validation`, com `PointerChecks` no JSON. 12 casos DragPanTests cobrem movimento em ambos os eixos, liberação, reversão nas bordas, eixos sem overflow, posição persistida, transições e coordenadas inválidas. Core/Storage verificam R personalizado em perfil antigo e roundtrip dos três atalhos. O smoke não movimenta o cursor físico: combinar movimento real com botão mantido, sair do viewport e trocar de monitor/DPI permanece uma verificação humana.

A segunda execução de desenvolvimento registrou 597 intervalos em dez segundos, mediana 16,6617 ms, máximo 43,571 ms e deslocamento 4178,843 DIPs. Isso é cadência de callbacks WPF de uma janela fora da tela; não comprova a percepção visual nem a cadência final do monitor. A captura `reader.png` foi inspecionada: imagem proporcional, margens escuras e controles ocultos na leitura.

## Distribuição em arquivo único e instalador

`scripts/verify-distribution.ps1` valida os dois executáveis usando uma pasta exclusiva em `artifacts/distribution-validation/<id>`. O portátil é copiado sozinho e renomeado para `portable only/Reader standalone.exe`. Com DOTNET_ROOT/DOTNET_ROOT_X64 inexistentes, CBZ (incluindo segundo processo), RAR4, RAR5 e sólido passaram. O diálogo Sobre renderiza em pt-BR/en; o SHA256 do texto incorporado coincide com a agregação integral das licenças, e capturas ficam em `about-pt-BR.png`/`about-en.png`.

Para comprovar a origem do runtime, o smoke verifica os módulos carregados. Na versão instalada, `coreclr.dll` e `libSkiaSharp.dll` devem estar sob a pasta da instalação. O host single-file self-contained do .NET 10 [liga CoreCLR estaticamente](https://github.com/dotnet/runtime/blob/v10.0.12/src/native/corehost/apphost/static/CMakeLists.txt): no portátil, o teste exige o export `DotNetRuntimeInfo` no próprio executável e o decoder sob a extração exclusiva definida por DOTNET_BUNDLE_EXTRACT_BASE_DIR. Os caminhos são registrados em `RuntimeModules` no JSON. Assim, o teste não depende somente de variáveis DOTNET_ROOT para inferir a ausência de fallback global.

O instalador foi executado silenciosamente por usuário, em caminho com espaços, com idioma brasileiro e reinstalação em inglês. Foram conferidos executável, decoder nativo, licenças, versão no Registro HKCU e destinos dos atalhos do menu Iniciar/área de trabalho. A aplicação instalada passou os mesmos quatro formatos/retomada, sem runtime externo. O instalador e o portátil excluem PDBs de depuração.

A desinstalação removeu executável, atalhos e registro de desinstalação. Conteúdo adicional colocado na pasta de instalação e um marcador exclusivo na pasta de dados permaneceram; o hash das preferências reais não mudou. O script recusa sobrescrever uma instalação/atalho existente e confere a posse/caminho antes de executar o desinstalador. Evidência: `result.json`, `install.log`, `reinstall.log` e `uninstall.log` no diretório da execução. Não houve interação com HQs pessoais nem encerramento da aplicação em uso.

## Verificações humanas pendentes

Não houve aprovação visual do movimento, pressão física das teclas, interação humana com o seletor/preferências nem alteração do DPI. Não marcar UI-09/G-02/G-07 como aceitação completa com base nos testes acima.

1. Abrir uma HQ de teste em tela cheia e segurar ↓ por dez segundos; observar trancos. Soltar, alternar foco e abrir o seletor enquanto a tecla estiver pressionada; o movimento deve parar e exigir nova pressão.
2. Segurar Page Down ou Espaço: apenas uma ação por pressão física. Em uma imagem com overflow horizontal e vertical, Espaço percorre cada faixa na horizontal antes de descer e se alinhar ao início da próxima; só depois do último trecho inferior avança. Repetir nos dois sentidos em Preferências e conferir retomada após reiniciar. Pressionar setas nas quatro bordas: nenhuma troca de página. Ajustar largura para mais de 100%, testar ←/→ e repetir depois de fechar o seletor de classificação.
3. Abrir Ctrl+O e cancelar o seletor: conservar a HQ. Arrastar um CBZ e um CBR para abrir pela outra entrada.
4. Usar Tab para acessar controles e preferências. Em campos, setas editam o controle e não movem a HQ. Testar um atalho duplicado/seta, aplicar um válido e reiniciar.
5. Ajustar largura/classificação, rolar, consultar V, fechar e reabrir o arquivo. Confirmar legibilidade e recuperação do ponto guardado.
6. Alterar escala do Windows ou mover para um monitor com outro DPI; testar leitura e V/V. Registrar monitor, escala, resolução e resultado real.

Ao identificar uma falha, registrar arquivo sintético/caso, dimensão da janela, DPI e comportamento antes de mudar os defaults. O pacote permanece uma versão de avaliação local até essa aceitação.
