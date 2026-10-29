# Plano técnico

## Contexto
Implementa a especificação do encurtador de links, respeitando a constituição
do projeto.

## Arquitetura
- API HTTP em Minimal API, um arquivo de endpoints por recurso
- Camada de aplicação com LinkService, dona de todas as regras de negócio:
  RN-01 a RN-06, a expiração do RF-06, o formato do código próprio do RF-03 e
  os limites dos casos de borda (2048 caracteres, data no passado)
- Camada de dados com EF Core e um repositório por agregado
- Endpoint só faz validação estrutural do payload (campos obrigatórios
  presentes, tipos e formatos parseáveis), chama o serviço e traduz o
  resultado em resposta; nenhuma regra de negócio é checada no endpoint

## Decisões
- D-01: SQLite como armazenamento, porque o RF-08 exige persistência
  entre reinícios
- D-02: contador incrementado por UPDATE atômico no banco, e não lido e
  reescrito na aplicação, para atender a RN-04 sob acesso concorrente
- D-03: código do link gerado com o alfabeto definido em RF-02 e RN-05
  (A-Z maiúsculo e dígitos de 1 a 9); a ausência do 0 já evita a confusão
  entre O e 0 em link lido em voz alta
- D-04: unicidade do código garantida por índice único no banco, além da
  checagem no serviço, porque a checagem sozinha tem corrida
- D-05: removida; a chave de gerenciamento saiu do plano porque o RF-07 torna
  as estatísticas públicas
- D-06: expiração informada como data (expiresOn, formato yyyy-MM-dd, em UTC)
  e armazenada em ExpiresAtUtc como o início do dia seguinte em UTC; o link é
  válido enquanto o instante atual for menor que ExpiresAtUtc, o que mantém o
  link válido até o fim da data informada (RF-06). Data anterior à data atual
  em UTC é recusada; a data de hoje é aceita
- D-07: "publicamente acessível" (RN-02) verificado no serviço resolvendo o
  host via DNS e recusando host que não resolve ou que resolve para endereço
  loopback, privado, link-local ou reservado; não é feita requisição HTTP ao
  destino, para evitar SSRF e latência na criação. O resolvedor fica atrás de
  uma interface para permitir teste unitário. Usa System.Net.Dns, sem
  dependência nova
- D-08: na geração automática, colisão de código (RN-06) é detectada pela
  violação do índice único da D-04 e o serviço gera outro código, até 5
  tentativas; esgotadas as tentativas, responde 500 com ProblemDetails
- D-09: contrato de erro, conforme a constituição: toda falha de validação
  (RN-02, RN-05, tamanho do RF-03, limite de 2048 caracteres, data no passado,
  payload malformado) retorna 400 com ProblemDetails e mensagem em português.
  Código próprio já em uso (RN-01) retorna 409 com ProblemDetails, por ser
  conflito com um estado existente e não erro de validação da entrada.
  Respostas 404 e 410 também usam ProblemDetails

## Modelo de dados
Link
- Id, Code (único), TargetUrl, CreatedAtUtc, ExpiresAtUtc (nulo; limite
  exclusivo, D-06), AccessCount

## Contratos
- POST /api/links entrada: targetUrl, code opcional, expiresOn opcional (D-06)
- POST /api/links saída 201: shortUrl, code, expiresOn
- POST /api/links erros: 400 ou 409 com ProblemDetails (D-09)
- GET /{code} redireciona com 302, ou 404 se não existe, ou 410 se expirou
- GET /api/links/{code}/stats é público, conforme RF-07: devolve code e
  accessCount, ou 404 se o código não existe

## Riscos
- Concorrência na criação de código próprio, mitigada por D-04
- Crescimento da tabela de links sem política de limpeza, fora de escopo por ora
- A verificação da D-07 vale só no momento da criação; um host pode passar a
  resolver para endereço não público depois (DNS rebinding). Como o sistema só
  redireciona e nunca acessa o destino, o impacto fica no visitante
