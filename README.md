<img width="100%" alt="Halloween 2026" src="https://baltaio.blob.core.windows.net/static/images/v4/challenges/halloween-2026/banner.jpg" />

## 🎃 Halloween - Desafio 2

Oi, eu sou o Rafael Santiago e este é o espaço onde compartilho minha jornada de aprendizado durante o desafio **Halloween 2026**, realizado pelo [balta.io](https://balta.io). 👻

Aqui você vai encontrar projetos, exercícios e códigos que estou desenvolvendo durante o desafio.

### Sobre este desafio
Neste desafio o objetivo é criar um encurtador de URLs completo utilizando os conceitos de Spec-Driven Development

#### O que foi implementado
Uma API de encurtador de links em **.NET 10 (Minimal API)**, com **SQLite via EF Core** e testes em **xUnit**, construída a partir dos artefatos em [specs/](specs/): especificação, constituição, plano técnico e tarefas.

- **Criar link** (`POST /api/links`): gera um código aleatório de 7 caracteres (A-Z e 1-9, sem o 0) e cada pedido cria um link novo, mesmo para uma URL já encurtada
- **Código próprio**: de 4 a 20 caracteres, convertido para maiúsculas, único em todo o sistema e nunca reutilizado, nem depois que o link expira (409 em caso de conflito)
- **Expiração opcional**: informada como data e interpretada em UTC; o link vale até 23:59:59 UTC da data, e data no passado é recusada
- **Redirecionamento** (`GET /{code}`): 302 para o destino, 404 se o link não existe e 410 se expirou, sem diferenciar maiúsculas de minúsculas
- **Contador de acessos**: incrementado direto no banco, sem perder contagem sob acessos simultâneos; link expirado não conta acesso
- **Estatísticas públicas** (`GET /api/links/{code}/stats`): total de acessos, inclusive de links expirados
- **Segurança da URL de destino**: só http/https, até 2048 caracteres, e o host precisa resolver para um endereço público (rede interna, loopback, link-local e reservados são recusados, sem fazer requisição ao destino)
- **Colisão de código**: se o código gerado já existir, tenta outro até 5 vezes e depois responde 503
- **Erros padronizados**: todas as falhas respondem com ProblemDetails e mensagem em português
- **Persistência**: os links sobrevivem ao reinício da aplicação
- **115 testes** unitários e de integração, cobrindo os critérios de aceite de cada tarefa

Para rodar: `dotnet run --project src/LinkShortener.Api` (sobe em http://localhost:5069). Para testar: `dotnet test`.

Neste processo eu aprendi:

* ✅ Escrever a especificação antes do código: requisitos, regras de negócio, casos de borda e critérios de aceite numerados (RF, RN) viram a referência de toda a implementação
* ✅ Separar o que é inegociável (constituição) das escolhas técnicas (plano) e das unidades de trabalho (tarefas), cada uma com seu papel
* ✅ Usar a governança a meu favor: quando o plano não justificava as dependências que a constituição exige, quem mudou foi o plano (D-12), em commit separado
* ✅ Rastrear cada commit até o requisito que ele atende, com os IDs na mensagem
* ✅ Transformar o "concluída quando" de cada tarefa em teste automatizado, para que pronto signifique verificado
* ✅ Pedir uma tarefa de cada vez à IA, com contexto e regras explícitos, para manter o escopo sob controle
* ✅ Que detalhes pequenos da spec evitam bugs reais, como converter para maiúsculas sem depender da cultura do servidor (em turco, "i" vira "İ")
* ✅ Revisar e reverter decisões na própria spec antes de virarem código, quando é mais barato mudar

#### Como o projeto foi conduzido
Cada artefato em [specs/](specs/) alimenta o próximo:

1. [spec.md](specs/spec.md): o problema, os requisitos (RF-01 a RF-08), as regras de negócio (RN-01 a RN-07), os casos de borda e os critérios de aceite
2. [constitution.md](specs/constitution.md): as regras que valem para todo o projeto, como stack, arquitetura, qualidade e convenções
3. [plan.md](specs/plan.md): as decisões técnicas (D-01 a D-12), cada uma ligada ao requisito que atende
4. [tasks.md](specs/tasks.md): as tarefas (T-01 a T-18), com dependências e o critério de "concluída quando"

A implementação seguiu uma tarefa por vez, na ordem das dependências, com um commit por tarefa citando os IDs atendidos.

#### Exemplo de uso

```bash
# Criar um link com código próprio e data de expiração
curl -X POST http://localhost:5069/api/links \
  -H "Content-Type: application/json" \
  -d '{"targetUrl":"https://balta.io","code":"balta26","expiresOn":"2026-12-31"}'
# {"shortUrl":"http://localhost:5069/BALTA26","code":"BALTA26","expiresOn":"2026-12-31"}

# Acessar o link curto (302 para o destino)
curl -i http://localhost:5069/balta26

# Consultar o total de acessos
curl http://localhost:5069/api/links/BALTA26/stats
# {"code":"BALTA26","accessCount":1}
```

## Badge
<img src="https://baltaio.blob.core.windows.net/static/images/v4/challenges/halloween-2026/02.png" width="200" />

## Problema
Links longos quebram em mensagens, e-mails e materiais impressos, e não dá para medir quantos acessos eles receberam. O encurtador transforma uma URL longa em um link curto, que pode ser compartilhado e acompanhado pelo total de acessos.

## Sobre o Halloween 2026
O desafio **Halloween 2026** consiste em implementar o modelo Spec Driven Development de ponta a ponta, criando apps completas com IA.

### Veja meu progresso no desafio
https://github.com/balta-io/balta-desafio-halloween-2026-spec-driven-02
