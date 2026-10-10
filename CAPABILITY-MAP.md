# Mapa de funcionalidades: ComicBookReader

Status: aprovado pelo usuário na conversa. Nome público escolhido pelo usuário: **ComicBookReader**. A pasta documental existente permanece em `C:\Users\ingos\Documents\LeitorHQ`.

## Objetivo

Criar um leitor de HQs para Windows, usando o CDisplayEx instalado como referência de comportamento e incorporando as melhorias de leitura definidas pelo usuário.

## Premissas

- Aplicativo independente para Windows, lendo arquivos locais CBZ e CBR.
- Leitura prioritariamente em tela cheia; dimensões calculadas a partir da área disponível no monitor.
- Cada imagem do arquivo continua sendo uma página navegável, mesmo quando contém duas páginas escaneadas juntas.
- Imagens em paisagem são candidatas a página dupla, com possibilidade de corrigir a classificação.
- A implementação será própria; os achados de engenharia reversa orientam a comparação de comportamento.
- Os documentos deste projeto ficam em `C:\Users\ingos\Documents\LeitorHQ`.

## Módulos e dependências

| ID estável | Responsabilidade | Depende de | Especificação futura |
| --- | --- | --- | --- |
| `comic-files` | Abrir CBZ/CBR, identificar e ordenar imagens, disponibilizar a imagem e suas dimensões, comunicar falhas de leitura. | — | `SPEC-comic-files.md` |
| `reading-preferences` | Guardar tamanho de leitura, velocidade, atalhos, ajustes por livro e correções manuais de classificação; recuperar a posição de leitura. | — | `SPEC-reading-preferences.md` |
| `page-layout` | Classificar página simples/dupla, calcular escala e limites de rolagem, compensar páginas duplas e calcular a visão completa usando as dimensões da imagem e da área de leitura. | `comic-files`, `reading-preferences` | `SPEC-page-layout.md` |
| `reader-ui` | Exibir em tela cheia, aplicar rolagem contínua pelas setas, executar navegação por atalhos separados, alternar visão completa/restauração e oferecer os controles de leitura. | `comic-files`, `reading-preferences`, `page-layout` | `SPEC-reader-ui.md` |

Ordem de construção: `comic-files` e `reading-preferences` → `page-layout` → `reader-ui`.

Os módulos não dependem da interface para funcionar. O layout calcula enquadramentos; a interface aplica os comandos e apresenta os resultados. Os contratos de dados serão definidos nas especificações dos módulos que os fornecem.

## Comportamentos já definidos pelo usuário

- Segurar ↑ ou ↓ rola suavemente a imagem atual. Nas bordas, o movimento para; essas teclas nunca trocam de página.
- A troca de página usa comandos separados. Page Up/Page Down são uma proposta de padrão, não uma escolha confirmada de teclas.
- O tamanho de leitura deve se adaptar a arquivos com resoluções diferentes e ficar memorizado.
- Uma imagem dupla preserva aproximadamente a mesma largura de cada página simples, mantendo a composição inteira e permitindo movimento lateral.
- Um atalho mostra a imagem inteira. Acioná-lo novamente restaura o zoom e a posição anterior, sem sobrescrever o tamanho de leitura preferido.

## Limites da primeira versão

O foco é abrir e ler CBZ/CBR com os comportamentos acima. Biblioteca de coleções, serviços online, PDF/DjVu, extensão do Explorer, gestos, impressão e reprodução de todos os filtros do CDisplayEx ficam fora deste primeiro escopo.

Linguagem, framework, bibliotecas, comandos de construção e testes serão propostos na etapa de especificação, com verificação da documentação e do ambiente disponível. Este mapa não fixa essas escolhas.

## Evidências e sequência do workflow

O relatório de análise e as preferências coletadas estão em [RELATORIO.md](../CDisplayEx-investigacao/2026-10-09/RELATORIO.md). Ele distingue observações estáticas de comportamentos propostos.

O mapa foi revisado e aprovado antes da criação das especificações. `SPEC.md` é o índice e cada módulo possui uma especificação com objetivo, comandos, estrutura, estilo, estratégia de testes, limites e critérios de aceitação. Planejamento de tarefas e implementação são fases posteriores.

Esta sequência vem da skill [spec-driven-development](C:/Users/ingos/.codex-cli-conta2/skills/spec-driven-development/SKILL.md), fase 0: “The human reviews module boundaries, dependency direction, and build order before any module spec is written.”
