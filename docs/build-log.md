# Registro de construção

Plano autorizado pelo comando `$build`. Registros abaixo contêm verificações executadas; nenhum teste manual é inferido de build ou cálculo.

## T01

SDK oficial 10.0.401 x64 instalado em pasta privada, SHA512 conferido e --info executado. Caminhos dos comandos atualizados. Pacotes permanecem fixados; integração será comprovada por T03/T07/T09.

## T02

global.json fixa 10.0.401; configurações centrais e solução vazia criadas. --info resolveu o global.json; restore executado com aviso esperado de solução ainda sem projetos.

## T03

Restore dos quatro projetos e build Release com warnings como erros passaram. xUnit 4 requer UseAppHost=true, corrigido conforme erro explícito do pacote. Nenhum runner vazio foi contado como teste.
