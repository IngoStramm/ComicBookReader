# Registro de construção

Plano autorizado pelo comando `$build`. Registros abaixo contêm verificações executadas; nenhum teste manual é inferido de build ou cálculo.

## T01

SDK oficial 10.0.401 x64 instalado em pasta privada, SHA512 conferido e --info executado. Caminhos dos comandos atualizados. Pacotes permanecem fixados; integração será comprovada por T03/T07/T09.

## T02

global.json fixa 10.0.401; configurações centrais e solução vazia criadas. --info resolveu o global.json; restore executado com aviso esperado de solução ainda sem projetos.

## T03

Restore dos quatro projetos e build Release com warnings como erros passaram. xUnit 4 requer UseAppHost=true, corrigido conforme erro explícito do pacote. Nenhum runner vazio foi contado como teste.

## T04

Aplicativo net10.0-windows configurado para win-x64. Restore/build Release passaram; aplicação WPF mínima iniciou e encerrou com código zero.

## T05

RED observado por namespace de contratos ausente; GREEN: três testes reais de identidade, validade do buffer e lease independente passaram. Build Release sem avisos. Core sem bibliotecas de terceiros.

## T06

RED por classes ausentes observado; GREEN: ordenação/índice passaram (5 Core antes de Layout e 2 Integration). Build sem avisos após integrar Layout; números enormes, subpastas, duplicidades e ZIP vazio conferidos.

## T10

Execução antecipada independente após T05. RED observado por Core.Layout ausente; GREEN: 46 testes Core incluindo 41 de layout passaram, build sem avisos. Exemplos de escala, classificação, centros, overview, viewport e entradas inválidas verificados.

## T07

RED observado por decoder/opener ausentes; GREEN: 21 testes Integration passaram, inclusive JPEG/PNG/WebP, oito orientações EXIF, alpha premultiplicado, hash, fechamento concorrente e limites. Build Release sem avisos. APIs obsoletas e tokens de testes corrigidos sem supressões.
