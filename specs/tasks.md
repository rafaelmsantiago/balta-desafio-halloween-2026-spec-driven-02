# Tarefas

| ID | Tarefa | Origem | Depende de | Concluída quando |
|---|---|---|---|---|
| T-01 | Criar projeto e estrutura de pastas | Constituição (Stack, Arquitetura) | | Projeto compila |
| T-02 | Modelar a entidade Link e a migração | RF-01, RF-08, D-01, D-04 | T-01 | Migração aplica no banco com índice único em Code |
| T-03 | Gerar código aleatório de 7 caracteres | RF-02, RN-05, D-03 | T-01 | Teste unitário cobre formato e alfabeto |
| T-04 | Validar esquema e tamanho da URL de destino | RN-02, CB-05 | T-01 | Teste unitário cobre http, https, esquema inválido e URL com mais de 2048 caracteres |
| T-05 | Criar link com código automático | RF-01 | T-02, T-03, T-04 | Teste de integração cria e retorna 201 |
| T-06 | Aceitar código próprio com unicidade | RF-03, RN-01, D-04, D-09, CB-01, CA-02 | T-05, T-12 | Teste unitário do LinkService recusa código em uso; teste de integração com código duplicado retorna 409 com ProblemDetails |
| T-07 | Redirecionar por código | RF-04, CB-04, CA-01 | T-05 | Teste de integração retorna 302; código inexistente retorna 404 com ProblemDetails |
| T-08 | Incrementar contador de forma atômica | RF-05, RN-04, D-02 | T-07 | Teste concorrente não perde contagem |
| T-09 | Endpoint público de estatísticas | RF-07, CA-05 | T-08 | Teste de integração retorna 200 com o total de acessos maior que zero após acessos; código inexistente retorna 404 com ProblemDetails |
| T-11 | Verificar se a URL de destino é publicamente acessível | RN-02, D-07, CB-06 | T-04 | Teste unitário com resolvedor falso recusa host que não resolve, loopback, privado e link-local, e aceita host público |
| T-12 | Validar formato do código próprio | RF-03, RN-05, D-09 | T-01 | Teste unitário aceita 4 e 20 caracteres e recusa 3 e 21 caracteres, minúsculas, o dígito 0, espaço e caracteres especiais |
| T-13 | Gerar outro código em caso de colisão | RN-06, D-08, CB-02, CB-03 | T-05 | Teste unitário com gerador falso: colisão gera outro código; após 5 colisões retorna erro |
| T-14 | Aceitar data de expiração na criação | RF-06, D-06, CB-07 | T-05 | Teste unitário: data passada é recusada, a data de hoje é aceita e ExpiresAtUtc é o início do dia seguinte em UTC |
| T-15 | Bloquear redirecionamento de link expirado | RN-03, RF-06, D-06, CA-03, CA-04 | T-08, T-14 | Teste unitário do LinkService com relógio controlado decide a validade no limite da data; teste de integração: link vale até o fim da data em UTC, depois retorna 410 e o contador não incrementa |
| T-16 | Responder erros de validação com ProblemDetails | D-09, Constituição (Qualidade) | T-04, T-11, T-12, T-14 | Teste de integração: entrada inválida retorna 400 com ProblemDetails e mensagem em português |
| T-17 | Garantir persistência após reinício | RF-08, D-01, CA-06 | T-07 | Teste de integração cria link, reinicia a aplicação sobre o mesmo banco SQLite e o redirecionamento funciona |
| T-18 | Manter estatísticas de link expirado | RF-07, CA-05 | T-09, T-15 | Teste de integração: /stats de link expirado retorna 200 com o total de acessos |

T-10 (cache em memória) removida: não tem origem em requisito nem em decisão do
plano, e um redirect servido pelo cache arriscaria pular o contador (RF-05) e a
expiração (RN-03).
