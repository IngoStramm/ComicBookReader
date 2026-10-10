# ComicBookReader

Leitor local de HQs para Windows x64: CBZ e CBR (RAR4, RAR5 e RAR sólido), imagens JPEG/PNG/WebP, rolagem contínua e tamanho de uma página preservado nas imagens duplas.

## Usar a versão local

Abra `artifacts/win-x64-keyboard/ComicBookReader.App.exe` para avaliar a atualização de teclado. Mantenha os demais arquivos dessa pasta juntos. O pacote inclui o runtime .NET e o decoder nativo; não exige instalar o SDK. É uma versão para avaliação local, com validação visual de suavidade e de outros monitores/DPI pendente.

Ctrl+O abre uma HQ; também é possível arrastar um arquivo para a janela ou passar seu caminho como argumento ao executável. A primeira execução começa em tela cheia. Os controles somem após dois segundos de leitura; mouse, Tab ou Alt os revelam sem mudar o tamanho da página.

| Tecla/controle | Ação |
| --- | --- |
| ↑/↓ e ←/→ mantidas | Rolar continuamente; as bordas mantêm a mesma página. |
| Page Down / Page Up | Próxima / anterior, uma troca por pressão. |
| Espaço | Descer 90% da altura útil; no fim, o próximo toque abre a página seguinte. |
| V | Página inteira; repetir restaura o ponto de leitura. |
| F11 / Esc | Alternar tela cheia / sair da tela cheia. |
| +/− na barra, teclas da linha superior ou Ctrl+roda | Ajustar a largura de leitura do livro. |
| Roda / Shift+roda | Rolar verticalmente / horizontalmente. |
| Automática / Simples / Dupla | Corrigir a classificação da imagem atual. |
| Preferências | Padrão global, velocidade e atalhos; setas são reservadas. |
| Largura padrão | Remover o ajuste de largura do livro e usar o padrão global. |

A largura é relativa à área de leitura, independentemente da resolução do scan. Paisagem sugere dupla; o ajuste manual corrige ilustrações horizontais. A imagem inteira é preservada. O atalho de visão completa não sobrescreve o progresso de leitura guardado.

As setas laterais rolam quando a imagem ultrapassa a largura disponível. Fechar o seletor de classificação ou clicar na imagem devolve o foco à leitura. Espaço ignora repetição automática; se já estiver atribuído a um atalho personalizado, essa configuração tem prioridade. Na visão completa, Espaço avança à página seguinte.

## Dados e limites

Preferências/progresso: `%LOCALAPPDATA%\ComicBookReader\preferences.json`, com backup e recuperação de corrupção. Ao reabrir o mesmo arquivo, a página, a posição e os ajustes são restaurados. Mover ou modificar o arquivo pode mudar sua identidade; não há migração automática nem reabertura automática do último caminho.

RAR sólido é preparado em `%LOCALAPPDATA%\ComicBookReader\solid-cache`, em uma sessão privada com nomes gerados. O cache é removido ao fechar ou cancelar; sessões abandonadas reconhecidas são recuperadas na próxima inicialização. Limites: 128 MiB descompactados por entrada, 96 milhões de pixels por imagem, 2 GiB de cache sólido e 256 MiB adicionais de imagens; a imagem atual é contabilizada separadamente. Senhas, múltiplos volumes, PDF, BMP/GIF/TIFF e animação estão fora desta versão.

O leitor abre as HQs somente para leitura. Não modifica o CDisplayEx, seu Registro ou os arquivos das HQs. Para diagnosticar prefetch, iniciar o processo com `COMICBOOKREADER_DISABLE_PREFETCH=1`.

## Construir e verificar

Na raiz, usar o SDK fixado por `global.json` (10.0.401 x64). Os comandos exatos com o SDK privado estão em [tasks/plan.md](tasks/plan.md#verificação-e-comandos). Com um SDK 10 x64 acessível como `dotnet`:

```powershell
rtk proxy dotnet restore ComicBookReader.slnx --locked-mode
rtk proxy dotnet build ComicBookReader.slnx --configuration Release --no-restore -warnaserror
rtk proxy dotnet run --project tests/ComicBookReader.Core.Tests --configuration Release --no-build
rtk proxy dotnet run --project tests/ComicBookReader.Integration.Tests --configuration Release --no-build
rtk proxy dotnet format ComicBookReader.slnx --verify-no-changes --no-restore
rtk proxy powershell -NoProfile -File scripts/publish.ps1 -OutputDirectory artifacts/win-x64-keyboard
rtk proxy powershell -NoProfile -File scripts/verify-app.ps1 -Executable artifacts/win-x64-keyboard/ComicBookReader.App.exe -EvidenceDirectory artifacts/keyboard-package-validation
```

`publish.ps1` aceita `-DotNet` para outro caminho do SDK e `-OutputDirectory` para uma pasta local (padrão `artifacts/win-x64`). Usa restore bloqueado e copia as licenças dos pacotes efetivamente resolvidos. O modo self-contained segue a [documentação oficial do .NET](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-publish).

O smoke test usa HQ sintética e fixtures redistribuíveis, preferências isoladas e janela fora da tela. Testa input roteado, renderização, leitura, retomada entre processos e hashes. Não substitui a avaliação visual nem a pressão física de teclas; o roteiro e a evidência estão em [docs/manual-validation.md](docs/manual-validation.md) e [docs/acceptance.md](docs/acceptance.md).

Os avisos de dependências ficam em [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md) e no diretório `licenses` do pacote. Artefatos e caches não são versionados.
