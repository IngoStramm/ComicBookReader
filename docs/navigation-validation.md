# Navegação por páginas — versão 0.4.0

Os botões e atalhos de primeira página, última página e miniaturas usam as mesmas preferências de teclado dos demais comandos (Ctrl+Home, Ctrl+End e T por padrão). As dicas da barra exibem a combinação configurada. A seleção de uma miniatura preserva o comportamento de carregamento e salvamento de progresso do leitor. A página atual recebe uma borda amarela de 3 pixels para ficar clara na grade.

O portátil foi verificado com HQ sintética e fixtures CBZ, RAR4, RAR5 e RAR sólido. A janela fora da tela carregou uma miniatura, selecionou uma página e executou os saltos de início e fim por atalhos personalizados; as dicas foram conferidas. O smoke test confirma o carregamento e a seleção, mas a avaliação visual da cor de destaque e a interação física em monitores diferentes ainda dependem de conferência humana.
