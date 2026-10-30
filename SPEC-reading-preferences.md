# Spec: reading-preferences

Produto: ComicBookReader. Status: contrato autorizado por `$build`; evidência em [docs/acceptance.md](docs/acceptance.md). Módulo do [mapa aprovado](CAPABILITY-MAP.md); requisitos comuns em [SPEC.md](SPEC.md).

## Objetivo

Recuperar preferências e ajustes para que abrir uma HQ não exija configurar novamente suas dimensões. Consumidores: `page-layout` recebe valores resolvidos; `reader-ui` atualiza preferências, classificações e progresso. Este módulo não abre HQs nem desenha imagens.

## Modelo e defaults propostos

| Dado | Default/regra |
| --- | --- |
| SchemaVersion | 1; validar antes de usar. |
| PreferredPageWidthRatio | 1, relativo à largura útil do viewport. Faixa configurável 0,25 a 2. |
| ScrollSpeed | 0,6 alturas úteis do viewport por segundo; faixa 0,1 a 2. |
| SpaceScrollPercent | Inteiro, padrão 90; faixa de 10 a 100% da largura útil ao percorrer uma faixa e da altura útil ao descer. |
| ReadingDirection | LeftToRight por padrão; LeftToRight/RightToLeft global, persistido. Perfis antigos usam LeftToRight; valores desconhecidos são inválidos. |
| Language | `pt-BR` por padrão; opções `pt-BR` e `en`. |
| PageClassification | Auto por padrão; Auto/Single/Spread global. |
| KeyBindings | Padrões da spec `reader-ui`, modificáveis sem associar setas à troca de página. |
| LastFullscreen | true na primeira execução; memorizar a escolha posterior. |
| Ajustes do livro | WidthRatio opcional; classificações antigas por PageKey preservadas como dados legados, sem prevalecer sobre a escolha global. |
| Progresso | BookId, PageKey, índice anterior como fallback, posição relativa X/Y de 0 a 1. |

Uma mudança manual do tamanho durante a leitura salva o ajuste daquele livro. Um controle distinto permite alterar o padrão global para livros sem override. Precedência: ajuste do livro → global → default. Não memorizar percentuais de zoom baseados na resolução original do scan.

Posição relativa é offset dividido pelo limite de rolagem naquele eixo; se o limite é zero, salvar zero. Ao restaurar, multiplicar pelo novo limite. Assim, mudar a resolução da janela não produz offsets impossíveis.

## Contratos fornecidos

```csharp
namespace ComicBookReader.Core.Preferences;

public enum PageClassificationOverride { Auto, Single, Spread }

public interface IReadingPreferencesStore
{
    Task<ReaderPreferences> LoadAsync(CancellationToken cancellationToken);
    Task SaveAsync(ReaderPreferences preferences, CancellationToken cancellationToken);
}
```

`ReaderPreferences` é um snapshot com versão, defaults e mapa de BookPreferences por BookId. `BookPreferences` contém ajustes, mapa de classificações e LastReadingPosition. Tipos próprios transportam dados validados; não expor objetos JSON ao layout/UI. BookId e PageKey são strings opacas recebidas do consumidor, sem dependência de adapters de arquivos.

Validar números finitos/faixas, versão e atalhos duplicados ao carregar/salvar. Campos ausentes conhecidos recebem default; versão futura incompatível não é sobrescrita. IO não resolvido retorna ReaderException com StorageUnavailable. Cancelamento permanece OperationCanceledException.

F12 alterna a barra, Ctrl+P abre preferências, R restaura a largura padrão e M alterna o sentido de leitura. Aumentar/diminuir largura usam OemPlus/OemMinus, com aliases de teclado descritos em reader-ui. Ao carregar um perfil antigo que já usa F12/Ctrl+P/R/M em outro comando, manter a atribuição existente e escolher para o novo comando uma combinação livre de Ctrl+Alt+Shift+F1…F24. Conflitos explícitos entre comandos continuam rejeitados. Campos de largura, velocidade e passo na janela são inteiros percentuais, com limites e validação de digitação/colagem; o domínio mantém largura/velocidade proporcionais.

Primeira página, última página e miniaturas usam Ctrl+Home, Ctrl+End e T por padrão e são configuráveis. Se um perfil anterior já utiliza uma dessas combinações, sua atribuição é preservada e o novo comando recebe uma combinação livre.

## Persistência e ciclo de vida

Guardar `preferences.json` em `%LOCALAPPDATA%\ComicBookReader`, identificado pelo nome público escolhido. Usar System.Text.Json do .NET 10. A memória da sessão continua funcionando se o arquivo não puder ser escrito; apresentar uma mensagem discreta de que as alterações não serão salvas.

Salvar por escrita em arquivo temporário no mesmo diretório e substituição do arquivo anterior após sucesso, mantendo último backup válido. Serializar gravações e consolidar alterações frequentes com atraso proposto de 300 ms. Mudança de página e fechamento pedem flush do último estado; o snapshot mais novo nunca pode ser substituído por uma gravação antiga.

JSON corrompido é preservado para recuperação, tenta-se o backup e depois defaults. Dados válidos recuperados não apagam silenciosamente o arquivo com problema. BookId é fornecido pelo contrato de arquivos: mover ou alterar uma HQ pode criar outra identidade. Nenhuma leitura ou migração do Registro do CDisplayEx nesta versão.

Visão completa é temporária: não salvar sua escala nem seus offsets como preferência/progresso. Ao salvar progresso enquanto ela está ativa, usar a posição de leitura que ficou guardada. Reabrir aplica tamanho de leitura e última página válida.

## Estrutura e estilo

Modelos/validação em `src/ComicBookReader.Core/Preferences`; store em `src/ComicBookReader.Infrastructure/Storage`. Seguir o [estilo comum](SPEC.md#estilo-de-código). Separar snapshot validado de DTO de serialização; usar a biblioteca padrão sem framework de persistência adicional.

## Comandos

Na raiz, após preparar os projetos:

```powershell
rtk proxy "C:\Users\ingos\AppData\Local\ComicBookReader-dev\dotnet-10.0.401\dotnet.exe" build ComicBookReader.slnx --configuration Release -warnaserror
rtk proxy "C:\Users\ingos\AppData\Local\ComicBookReader-dev\dotnet-10.0.401\dotnet.exe" run --project tests/ComicBookReader.Core.Tests/ComicBookReader.Core.Tests.csproj --configuration Release
rtk proxy "C:\Users\ingos\AppData\Local\ComicBookReader-dev\dotnet-10.0.401\dotnet.exe" run --project tests/ComicBookReader.Integration.Tests/ComicBookReader.Integration.Tests.csproj --configuration Release
```

## Estratégia de testes e critérios de aceitação

| ID | Caso e resultado esperado | Evidência |
| --- | --- | --- |
| RP-01 | Perfil vazio aplica defaults; override do livro prevalece sobre global e não altera outros livros. | Unidade. |
| RP-02 | Reiniciar recupera largura, velocidade, atalhos, classificação manual e última página/posição. | Integração com diretório temporário. |
| RP-03 | Gravação interrompida não destrói a última versão válida; JSON inválido tenta backup e defaults. | Integração com falhas simuladas. |
| RP-04 | Visão completa não muda as preferências/progresso de leitura persistidos. | Teste de contrato com `reader-ui`. |
| RP-05 | Valores inválidos, versão futura e conflitos de atalhos recebem tratamento explícito; um snapshot mais antigo não substitui o mais novo. | Unidade e integração. |
| RP-06 | Mudança do viewport restaura posição relativa válida, respeitando eixos sem overflow. | Unidade e teste integrado com layout. |

## Limites e perguntas abertas

Sempre persistir só dados necessários à leitura e manter recuperação de escrita; revisar com o usuário qualquer migração do CDisplayEx ou sincronização online; nunca gravar no Registro ou na instalação do aplicativo legado. Defaults são ajustáveis. Importação de preferências e portabilidade entre máquinas ficam fora desta versão.
