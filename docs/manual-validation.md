# Validação da janela

Data: 10/10/2026. Esta página distingue execução automatizada de avaliação humana. A versão local está disponível em `artifacts/win-x64/ComicBookReader.App.exe`.

## Ambiente observado

Windows x64; AMD Ryzen 7 5800H; NVIDIA GeForce RTX 3050 Laptop GPU; resolução reportada 1920 × 1080 a 60 Hz. Existe também um adaptador virtual Parsec; não houve troca de monitor. A janela usada pelo smoke test mediu 96 DPI. Janela de 1000 × 760 DIPs e redimensionamento para 800 × 600 DIPs.

## Execução automatizada real

`scripts/verify-app.ps1` gera um CBZ sintético com imagens 1000 × 12000, 2000 × 1500 e 4000 × 3000. Executa a aplicação WPF fora da tela, abre esses arquivos e as fixtures RAR4/RAR5/sólida. Usa preferências privadas dentro do diretório de evidência. Nenhuma HQ pessoal é usada.

Resultados da execução de desenvolvimento em `artifacts/runtime-validation` e do pacote em `artifacts/published-validation`: quatro formatos passaram, hashes das fontes preservados. CBZ usa dois processos sucessivos; o segundo comprova página, largura, classificação e posição salvos ao fechar em visão completa.

A execução adicional em `artifacts/self-contained-validation` aponta `DOTNET_ROOT` e `DOTNET_ROOT_X64` para uma pasta inexistente, para verificar que o host publicado usa o runtime que acompanha o pacote.

| Caso executado | Resultado |
| --- | --- |
| Abrir CBZ/RAR4/RAR5/RAR sólido e apresentar pixels | Passou. PNG da janela e `runtime.json` por formato. |
| Arquivo ausente e abertura cancelada por token | Livro anterior preservado. |
| F11/Esc pelo evento roteado da janela | Modo da janela alternado. |
| Roteamento de ↓ por 10 s e liberação | Deslocamento positivo; soltar encerra movimento. |
| Evento roteado de perda de foco | Direções limpas. |
| Pressionar ↓ na borda | Mesma página. |
| Controles temporários | Ocultam-se após dois segundos; Tab os revela; layout não muda. |
| Duplas com duas resoluções distintas | Mesma largura de leitura. |
| V/V | Escala e offsets restaurados exatamente. |
| Navegação concorrente e resize | Último alvo apresentado; offsets limitados. |
| Dois processos; fechar em overview | Progresso de leitura original restaurado. |

A segunda execução de desenvolvimento registrou 597 intervalos em dez segundos, mediana 16,6617 ms, máximo 43,571 ms e deslocamento 4178,843 DIPs. Isso é cadência de callbacks WPF de uma janela fora da tela; não comprova a percepção visual nem a cadência final do monitor. A captura `reader.png` foi inspecionada: imagem proporcional, margens escuras e controles ocultos na leitura.

## Verificações humanas pendentes

Não houve aprovação visual do movimento, pressão física das teclas, interação humana com o seletor/preferências nem alteração do DPI. Não marcar UI-09/G-02/G-07 como aceitação completa com base nos testes acima.

1. Abrir uma HQ de teste em tela cheia e segurar ↓ por dez segundos; observar trancos. Soltar, alternar foco e abrir o seletor enquanto a tecla estiver pressionada; o movimento deve parar e exigir nova pressão.
2. Segurar Page Down: apenas uma troca por pressão física. Pressionar setas nas quatro bordas: nenhuma troca de página.
3. Abrir Ctrl+O e cancelar o seletor: conservar a HQ. Arrastar um CBZ e um CBR para abrir pela outra entrada.
4. Usar Tab para acessar controles e preferências. Em campos, setas editam o controle e não movem a HQ. Testar um atalho duplicado/seta, aplicar um válido e reiniciar.
5. Ajustar largura/classificação, rolar, consultar V, fechar e reabrir o arquivo. Confirmar legibilidade e recuperação do ponto guardado.
6. Alterar escala do Windows ou mover para um monitor com outro DPI; testar leitura e V/V. Registrar monitor, escala, resolução e resultado real.

Ao identificar uma falha, registrar arquivo sintético/caso, dimensão da janela, DPI e comportamento antes de mudar os defaults. O pacote permanece uma versão de avaliação local até essa aceitação.
