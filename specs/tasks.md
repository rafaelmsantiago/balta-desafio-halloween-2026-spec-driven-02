# Tarefas

| ID | Tarefa | Origem | Depende de | Concluída quando |
|---|---|---|---|---|
| T-01 | Criar projeto e estrutura de pastas | Constituição (Stack, Arquitetura) | | Projeto compila |
| T-02 | Modelar a entidade Link e a migração | RF-01, RF-08, D-01, D-04 | T-01 | Migração aplica no banco com índice único em Code |
| T-03 | Gerar código aleatório de 7 caracteres | RF-02, RN-05, D-03 | T-01 | Teste unitário cobre formato e alfabeto |
| T-04 | Validar esquema e tamanho da URL de destino | RN-02, borda 2048 | T-01 | Teste unitário cobre http, https, esquema inválido e URL com mais de 2048 caracteres |
| T-05 | Criar link com código automático | RF-01, D-11 | T-02, T-03, T-04 | Teste de integração cria e retorna 201; encurtar a mesma URL duas vezes gera dois códigos diferentes com contadores independentes |
| T-06 | Aceitar código próprio com unicidade | RF-03, RN-01, D-04, D-09 | T-05, T-12 | Teste unitário do LinkService recusa código em uso, inclusive de link expirado; teste de integração: código duplicado retorna 409 com ProblemDetails, inclusive quando o link existente está expirado ou quando o código difere só em maiúsculas e minúsculas |
| T-07 | Redirecionar por código | RF-04, D-10, borda código inexistente | T-05, T-18 | Teste de integração retorna 302, inclusive com o código em minúsculas; código inexistente retorna 404 com ProblemDetails dizendo que o link não existe |
| T-08 | Incrementar contador de forma atômica | RF-05, RN-04, D-02 | T-07 | Teste concorrente não perde contagem; acessos repetidos incrementam o contador a cada requisição |
| T-09 | Endpoint público de estatísticas | RF-07, D-10 | T-05, T-15, T-18 | Teste de integração retorna 200 com o total de acessos, inclusive de link expirado e com o código em minúsculas; código inexistente retorna 404 |
| T-11 | Verificar se a URL de destino é publicamente acessível | RN-02, D-07, borda não acessível | T-04 | Teste unitário com resolvedor falso recusa host que não resolve, loopback, privado e link-local, e aceita host público |
| T-12 | Validar formato do código próprio | RF-03, RN-05, D-09, D-10 | T-18 | Teste unitário aceita 4 e 20 caracteres e recusa 3 e 21 caracteres, o dígito 0, espaço e caracteres especiais; código em minúsculas é aceito e gravado em maiúsculas |
| T-13 | Gerar outro código em caso de colisão | RN-06, D-08, borda limite de tentativas | T-05 | Teste unitário com gerador falso: colisão gera outro código e, após 5 colisões, retorna erro; teste de integração: limite atingido retorna 503 com ProblemDetails orientando a tentar de novo |
| T-14 | Aceitar data de expiração na criação | RF-06, RN-07, D-06, borda data no passado | T-05 | Teste unitário: data anterior a hoje em UTC é recusada, a data de hoje é aceita, ExpiresAtUtc é o início do dia seguinte em UTC e, sem data, ExpiresAtUtc fica nulo |
| T-15 | Bloquear redirecionamento de link expirado | RN-03, RF-06, D-06, D-09 | T-08, T-14 | Teste unitário do LinkService com relógio controlado: link vale até 23:59:59 UTC da data e é considerado expirado no instante seguinte; link sem data nunca expira; teste de integração com relógio controlado: link vale até 23:59:59 UTC da data; depois retorna 410 com ProblemDetails dizendo que o link expirou e o contador não incrementa; link sem data continua redirecionando |
| T-16 | Responder erros de validação com ProblemDetails | D-09, Constituição (Qualidade) | T-04, T-11, T-12, T-14 | Teste de integração: entrada inválida retorna 400 com ProblemDetails e mensagem em português |
| T-17 | Garantir persistência após reinício | RF-08, D-01 | T-07 | Teste de integração cria link, reinicia a aplicação sobre o mesmo banco SQLite e o redirecionamento funciona |
| T-18 | Normalizar código para maiúsculas | RF-03, RF-04, RN-05, D-10 | T-01 | Teste unitário: código com minúsculas é convertido para maiúsculas antes da validação, da gravação e da busca, com o mesmo resultado sob a cultura tr-TR |

T-10 (cache em memória) removida: não tem origem em requisito nem em decisão do
plano, e um redirect servido pelo cache arriscaria pular o contador (RF-05) e a
expiração (RN-03).
