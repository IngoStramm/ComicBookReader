# ComicBookReader

Leitor local de HQs para Windows x64: CBZ e CBR (RAR4, RAR5 e RAR sólido), imagens JPEG/PNG/WebP, rolagem contínua e tamanho de uma página preservado nas imagens duplas.

A versão local 0.2.0 está em `artifacts/distribution`, nos formatos portátil e instalador. Inclui percurso por faixas com Espaço e sentido de leitura configurável; [evidência de validação](docs/reading-order-validation.md). Os downloads públicos abaixo continuam associados à versão 0.1.0.

## Baixar e compartilhar

Baixe a [versão 0.1.0](https://github.com/IngoStramm/ComicBookReader/releases/tag/v0.1.0) para Windows x64:

- [Portátil — ComicBookReader-0.1.0-Portable.exe](https://github.com/IngoStramm/ComicBookReader/releases/download/v0.1.0/ComicBookReader-0.1.0-Portable.exe): abra diretamente. Basta compartilhar esse único arquivo.
- [Instalador — ComicBookReader-0.1.0-Setup.exe](https://github.com/IngoStramm/ComicBookReader/releases/download/v0.1.0/ComicBookReader-0.1.0-Setup.exe): instalador em português do Brasil e inglês. Instala para o usuário atual, cria um atalho no menu Iniciar, oferece atalho na área de trabalho e permite desinstalar pelas configurações do Windows.

Todas as versões ficam em [Releases](https://github.com/IngoStramm/ComicBookReader/releases), cada uma associada à sua tag. Abra a versão desejada e escolha o arquivo em **Assets**. As mudanças são registradas no [CHANGELOG.md](CHANGELOG.md).

Ambos incluem o .NET e o decoder de imagens; a pessoa não precisa instalar o SDK, baixar bibliotecas nem receber a pasta do projeto. As licenças completas estão no botão **Sobre**. Preferências e progresso ficam em `%LOCALAPPDATA%\ComicBookReader`, são compartilhados entre os dois formatos e permanecem após desinstalar. A versão portátil dispensa instalação, mas usa esse diretório de dados e extrai bibliotecas nativas para o cache temporário do .NET.

Esta é a primeira versão pública. A validação visual de suavidade e de outros monitores/DPI continua no roteiro de avaliação. Builds locais ficam em `artifacts/distribution`; a distribuição local anterior em `artifacts/win-x64-drag` foi preservada.

Ctrl+O abre uma HQ; também é possível arrastar um arquivo para a janela ou passar seu caminho como argumento ao executável. A primeira execução começa em tela cheia. F12 mostra/esconde a barra; o botão ☰ também a reabre. Mover o mouse sobre a imagem não revela os controles. Tab/Alt permitem acesso pelo teclado sem mudar o tamanho da página.

| Tecla/controle | Ação |
| --- | --- |
| ↑/↓ e ←/→ mantidas | Rolar continuamente; as bordas mantêm a mesma página. |
| Page Down / Page Up | Próxima / anterior, uma troca por pressão. |
| Espaço | Percorrer a faixa horizontal no sentido escolhido; na borda, descer e retornar ao início da faixa seguinte. Após o último trecho, avançar a página. |
| F12 / Ctrl+P | Mostrar/esconder a barra / abrir preferências. |
| V | Página inteira; repetir restaura o ponto de leitura. |
| F11 / Esc | Alternar tela cheia / sair da tela cheia. |
| +/− na barra, teclas da linha superior ou numérico; Ctrl+roda | Ajustar a largura do livro em passos de 5 pontos percentuais. |
| Segurar o botão esquerdo e mover o mouse | Arrastar a página em qualquer direção, respeitando suas bordas. |
| Roda / Shift+roda | Rolar verticalmente / horizontalmente. |
| Automática / Simples / Dupla | Escolher a classificação global, aplicada a todas as páginas/livros. |
| Preferências | Sentido de leitura, largura, velocidade, passo do Espaço, classificação global, idioma e atalhos. |
| R / Largura padrão | Remover o ajuste de largura do livro e usar o padrão global. |

A largura é relativa à área de leitura, independentemente da resolução do scan. Paisagem sugere dupla; o ajuste manual corrige ilustrações horizontais. A imagem inteira é preservada. O atalho de visão completa não sobrescreve o progresso de leitura guardado.

As setas laterais rolam quando a imagem ultrapassa a largura disponível. Fechar o seletor de classificação ou clicar na imagem devolve o foco à leitura. Espaço ignora repetição automática; se já estiver atribuído a um atalho personalizado, essa configuração tem prioridade. Na visão completa, Espaço avança à página seguinte.

Em Preferências (Ctrl+P), **Sentido de leitura** permite escolher esquerda → direita ou direita → esquerda para mangás. Espaço avança primeiro na horizontal; só ao chegar à borda desce e se alinha ao início da próxima faixa. O passo padrão é 90% da largura ou da altura útil, conforme o eixo, para deixar sobreposição. O último trecho de cada faixa aparece antes de descer, inclusive na parte inferior da imagem. Uma página que cabe inteira avança diretamente; no fim do livro, permanece no último trecho.

Alterar o sentido mantém a altura atual e reposiciona a leitura no início da faixa. Páginas novas começam no topo/esquerda no modo ocidental e no topo/direita no modo mangá. Reabrir uma HQ recupera a posição salva. A preferência é global; a ordem dos arquivos e os comandos Próxima/Anterior permanecem os mesmos.

Os três atalhos de largura são configuráveis em Preferências. O padrão de aumento aceita a tecla `+` com Shift e o `+` numérico; o padrão de diminuição aceita `−` na linha superior e no numérico. Atalhos explícitos têm prioridade e os aliases numéricos só são usados enquanto o comando conserva seu padrão. Se um perfil antigo já usa R, o novo comando recebe uma combinação livre. Durante o arraste, a imagem acompanha o cursor mesmo fora da área de leitura até soltar o botão. Perder foco/captura, mudar tamanho/página ou usar um comando encerra o arraste; ele não troca de página nas bordas. Eixos sem overflow ficam imóveis.

Os botões da barra devolvem o foco à leitura após suas ações. Quando um botão é selecionado pelo teclado, Espaço mantém sua ativação normal. Os campos numéricos aceitam inteiros, com controles de incremento e bloqueio de texto/colagem inválidos. Velocidade é exibida como porcentagem da altura por segundo; passo do Espaço vai de 10% a 100%. Português (Brasil) e English são aplicados ao salvar, sem reiniciar, e persistidos. A classificação antiga por imagem fica preservada no JSON para recuperação, mas o leitor usa a escolha global.

## Dados e limites

Preferências/progresso: `%LOCALAPPDATA%\ComicBookReader\preferences.json`, com backup e recuperação de corrupção. Ao reabrir o mesmo arquivo, a página, a posição e os ajustes são restaurados. Mover ou modificar o arquivo pode mudar sua identidade; não há migração automática nem reabertura automática do último caminho.

RAR sólido é preparado em `%LOCALAPPDATA%\ComicBookReader\solid-cache`, em uma sessão privada com nomes gerados. O cache é removido ao fechar ou cancelar; sessões abandonadas reconhecidas são recuperadas na próxima inicialização. Limites: 128 MiB descompactados por entrada, 96 milhões de pixels por imagem, 2 GiB de cache sólido e 256 MiB adicionais de imagens; a imagem atual é contabilizada separadamente. Senhas, múltiplos volumes, PDF, BMP/GIF/TIFF e animação estão fora desta versão.

O leitor abre as HQs somente para leitura. Não modifica o CDisplayEx, seu Registro ou os arquivos das HQs. Para diagnosticar prefetch, iniciar o processo com `COMICBOOKREADER_DISABLE_PREFETCH=1`.

## Construir e verificar

Na raiz, usar o SDK fixado por `global.json` (10.0.401 x64). Os comandos exatos com o SDK privado estão em [tasks/plan.md](tasks/plan.md#verificação-e-comandos). `rtk proxy` é apenas o proxy usado no ambiente de desenvolvimento; sem RTK, execute o comando que aparece depois desse prefixo. Com um SDK 10 x64 acessível como `dotnet`:

```powershell
rtk proxy dotnet restore ComicBookReader.slnx --locked-mode
rtk proxy dotnet build ComicBookReader.slnx --configuration Release --no-restore -warnaserror
rtk proxy dotnet run --project tests/ComicBookReader.Core.Tests --configuration Release --no-build
rtk proxy dotnet run --project tests/ComicBookReader.Integration.Tests --configuration Release --no-build
rtk proxy dotnet format ComicBookReader.slnx --verify-no-changes --no-restore
rtk proxy powershell -NoProfile -File scripts/publish.ps1 -OutputDirectory artifacts/win-x64-drag
rtk proxy powershell -NoProfile -File scripts/verify-app.ps1 -Executable artifacts/win-x64-drag/ComicBookReader.App.exe -EvidenceDirectory artifacts/drag-package-validation
```

`publish.ps1` aceita `-DotNet` para outro caminho do SDK e `-OutputDirectory` para uma pasta local (padrão `artifacts/win-x64`). Usa restore bloqueado e copia as licenças dos pacotes efetivamente resolvidos. O modo self-contained segue a [documentação oficial do .NET](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-publish).

Para gerar os dois arquivos, instalar o compilador [Inno Setup 6.7.3 oficial](https://github.com/jrsoftware/issrc/releases/tag/is-6_7_3), respeitando sua licença de uso. Os caminhos privados são os padrões dos scripts; `-DotNet` e `-Iscc` permitem informar outros caminhos. O pré-processador verifica a versão do compilador. A publicação em arquivo único segue a [documentação do .NET](https://learn.microsoft.com/en-us/dotnet/core/deploying/single-file/overview), com bibliotecas nativas incluídas, compressão e sem trimming.

```powershell
rtk proxy powershell -NoProfile -File scripts/build-distribution.ps1
rtk proxy powershell -NoProfile -File scripts/verify-distribution.ps1
```

O primeiro comando gera os executáveis, instruções em pt-BR/en e `SHA256SUMS-0.1.0.txt`. O segundo copia somente o executável portátil para uma pasta com espaços, renomeia-o e testa os quatro formatos, conferindo que o runtime e o decoder carregados vêm da distribuição. Também instala/reinstala/desinstala em uma pasta exclusiva da validação, confere atalhos, Registro por usuário e preservação dos dados. Recusa sobrescrever uma instalação ou atalho já existentes. Os instaladores são executados de forma silenciosa; logs e hashes ficam em `artifacts/distribution-validation/<id>`.

O smoke test usa HQ sintética e fixtures redistribuíveis, preferências isoladas e janela fora da tela. Testa input roteado, renderização, leitura, retomada entre processos e hashes. Não substitui a avaliação visual nem a pressão física de teclas; o roteiro e a evidência estão em [docs/manual-validation.md](docs/manual-validation.md) e [docs/acceptance.md](docs/acceptance.md).

Os avisos de dependências ficam em [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md), no diretório `licenses` da versão instalada e incorporados ao botão Sobre dos dois executáveis. Artefatos e caches não são versionados.

O procedimento de tags, validação e publicação de novas versões está em [docs/releases.md](docs/releases.md).
