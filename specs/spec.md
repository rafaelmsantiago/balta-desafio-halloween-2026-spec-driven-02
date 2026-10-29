# Encurtador de links

## Problema
Links longos quebram em mensagens, e-mails e materiais impressos, e não dá para
medir quantos acessos eles receberam.

## Objetivo
Permitir que uma pessoa transforme uma URL longa em um link curto, compartilhe
esse link e acompanhe quantos acessos ele recebeu.

## Usuários
- Criador do link: gera links curtos e acompanha os acessos
- Visitante: clica no link curto e chega ao destino

## Histórias
- Como criador, quero encurtar uma URL para compartilhar um link menor
- Como criador, quero escolher o código do link para usar um nome legível
- Como criador, quero ver quantos acessos meu link recebeu
- Como visitante, quero ser redirecionado ao destino sem passar por tela intermediária

## Requisitos funcionais
- RF-01: o sistema recebe uma URL de destino e devolve um link curto; cada pedido
  gera um link novo e independente, com seu próprio contador, mesmo que a URL de
  destino já tenha sido encurtada antes
- RF-02: o código do link tem 7 caracteres gerados automaticamente, usando apenas
  letras maiúsculas de A a Z e números de 1 a 9
- RF-03: o criador pode informar um código próprio de 4 a 20 caracteres, usando
  apenas letras de A a Z e números de 1 a 9, sem espaços; letras minúsculas
  informadas são convertidas para maiúsculas
- RF-04: acessar um link curto existente e válido redireciona para a URL de
  destino; o código informado no acesso é comparado sem diferenciar maiúsculas
  de minúsculas
- RF-05: cada requisição a um link válido incrementa o contador daquele link,
  incluindo acessos repetidos e automatizados (por exemplo, pré-visualização de
  links em aplicativos de mensagem); o contador representa requisições, não
  pessoas
- RF-06: o criador pode, opcionalmente, definir uma data de expiração para o
  link (somente data, sem hora); a data é interpretada em UTC e o link permanece
  válido até 23:59:59 UTC da data informada; link sem data de expiração nunca
  expira
- RF-07: o sistema devolve o total de acessos de um link a qualquer pessoa que
  conheça seu código, inclusive depois que o link expira, inclusive depois que o link expirou
- RF-08: os links e seus dados devem ser persistidos em banco de dados para que
  não se percam após reinício da aplicação

## Regras de negócio
- RN-01: o código do link é único em todo o sistema e nunca é reutilizado,
  mesmo depois que o link expira
- RN-02: a URL de destino precisa usar http ou https e ser publicamente
  acessível, ou seja, não pode apontar para a rede interna, para a própria
  máquina nem para endereços reservados; a verificação acontece somente na
  criação do link
- RN-03: link expirado não redireciona e não conta acesso
- RN-04: o contador de acessos nunca diminui
- RN-05: códigos automáticos e personalizados são armazenados somente com A-Z
  maiúsculo e os dígitos de 1 a 9; o dígito 0, espaços e caracteres especiais
  não são permitidos; minúsculas são aceitas na entrada e convertidas para
  maiúsculas
- RN-06: se um código gerado automaticamente já estiver em uso, o sistema tenta
  gerar outro código, até um número máximo e finito de tentativas
- RN-07: a menor data de expiração aceita é a data de hoje em UTC

## Casos de borda
- Código já em uso (inclusive por link expirado): recusar a criação e explicar
  o motivo
- Código próprio com minúsculas: converter para maiúsculas e seguir a criação
- Código gerado automaticamente já em uso: gerar outro código e tentar
  novamente
- Limite de tentativas de geração atingido: recusar a criação e orientar o
  criador a tentar de novo
- Código inexistente no acesso: responder que o link não existe
- Código de link expirado no acesso: responder que o link expirou, de forma
  distinta de "link não existe"
- URL de destino com mais de 2048 caracteres: recusar
- URL de destino não publicamente acessível: recusar
- Data de expiração anterior à data de hoje em UTC: recusar na criação
- Data de expiração igual à data de hoje em UTC: aceitar; o link vale até
  23:59:59 UTC de hoje
- Mesma URL de destino encurtada mais de uma vez: gerar um link novo a cada
  pedido

## Fora de escopo
- Cadastro e login de usuários
- Domínio personalizado
- Relatório com origem geográfica do acesso
- Edição do destino de um link já criado
- Distinção entre acessos humanos e automatizados, ou contagem de visitantes
  únicos

## Critérios de aceite
- Criar um link e acessá-lo redireciona para o destino informado
- Dois links não podem ter o mesmo código, mesmo que um deles esteja expirado
- Encurtar a mesma URL duas vezes gera dois códigos diferentes, com contadores
  independentes
- Código próprio informado em minúsculas é salvo em maiúsculas, e acessar o
  link em minúsculas redireciona normalmente
- Link com data de expiração vencida responde "link expirado", distinto de
  "link não existe", e não incrementa o contador
- Um link ainda válido funciona até 23:59:59 UTC de sua data de expiração
- Link criado sem data de expiração continua funcionando indefinidamente
- Data de expiração anterior à data de hoje em UTC é recusada; a data de hoje é
  aceita
- URL apontando para a rede interna, para a própria máquina ou para endereço
  reservado é recusada na criação
- Qualquer pessoa que conheça o código pode consultar o total de acessos,
  inclusive de links expirados
- Após reiniciar a aplicação, os links persistidos no banco continuam funcionando
