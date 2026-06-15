# ADR-0002: Reenvio automático de registros pendentes para Holmes via Cloud Run Job

## Status

Proposto

## Data

2026-06-15

## Contexto

Existe uma demanda mandatória para garantir que **100% dos contratos estejam disponíveis na Holmes**. Atualmente, podem existir registros de contratos que não foram enviados corretamente, falharam durante o processo de integração ou permaneceram pendentes de envio.

Como a Holmes é a base esperada para consulta e recuperação desses contratos, é necessário criar um mecanismo automático que identifique registros pendentes de envio no banco de dados SQL Server e os reenvie para processamento por meio da fila já existente no Pub/Sub.

O processo precisa considerar os seguintes pontos:

* Todos os contratos elegíveis devem ser enviados para Holmes.
* A quantidade de registros buscados por execução deve ser configurável.
* O processo deve evitar que o mesmo registro seja capturado por mais de uma execução ao mesmo tempo.
* Registros reservados para envio não podem ficar bloqueados indefinidamente caso o processo falhe.
* Após o envio para a fila, os registros enviados com sucesso devem ser atualizados no banco de dados.
* O processo deve ser executado automaticamente a cada intervalo configurado.
* A infraestrutura deve ser provisionada via Terraform.
* O tópico Pub/Sub já existe e será reutilizado.

Uma API HTTP dedicada não é necessária para esse fluxo, pois a necessidade é executar uma rotina periódica, finita e sem interação direta de usuário.

## Decisão

Adotar um **Cloud Run Job** para executar periodicamente o processo de reenvio de registros pendentes para Holmes, com o objetivo de garantir que todos os contratos elegíveis sejam encaminhados para a integração.

A execução será agendada por meio do **Cloud Scheduler**, que acionará o Cloud Run Job a cada intervalo configurado.

O Cloud Run Job será responsável por:

* Ler as configurações da aplicação, como tamanho do lote e grau de paralelismo.
* Conectar no banco de dados SQL Server.
* Buscar registros pendentes de envio para Holmes.
* Reservar temporariamente os registros selecionados por meio de um lock lógico com expiração.
* Publicar os registros na fila do Pub/Sub já existente.
* Atualizar em lote os registros publicados com sucesso, registrando data/hora do envio e identificador da publicação quando aplicável.

A captura dos registros no SQL Server deve ser feita de forma atômica, utilizando uma estratégia equivalente a `UPDATE ... OUTPUT`, para que os registros sejam buscados e marcados como reservados na mesma operação.

O lock dos registros deve possuir tempo de expiração. Dessa forma, caso o Job falhe antes de concluir o envio, os registros reservados voltam a ficar elegíveis para uma nova tentativa após o prazo definido.

A configuração inicial recomendada para o Cloud Run Job é:

* `tasks = 1`
* `parallelism = 1`
* timeout explícito
* retry controlado
* service account própria
* variáveis de ambiente para configurações não sensíveis
* Secret Manager para credenciais e connection string

## Consequências

### Benefícios

* Atende à demanda mandatória de garantir que 100% dos contratos elegíveis estejam na Holmes.
* Remove a necessidade de intervenção manual para reenviar registros pendentes.
* Permite execução automática e recorrente do processo.
* Reduz o risco de dois processos capturarem o mesmo registro simultaneamente.
* Evita que registros fiquem bloqueados indefinidamente em caso de falha.
* Mantém rastreabilidade dos registros enviados para a fila.
* Reutiliza o Pub/Sub existente, sem necessidade de criação de nova fila.
* Mantém a infraestrutura declarada e versionada via Terraform.
* Usa um modelo adequado para processamento batch, sem necessidade de expor endpoint HTTP.

### Trade-offs / custos

* A solução precisa lidar com possibilidade de duplicidade, pois não existe transação distribuída entre SQL Server e Pub/Sub.
* O consumidor da fila deve ser idempotente, garantindo que uma mesma mensagem não gere efeitos colaterais indevidos caso seja recebida mais de uma vez.
* É necessário manter controle de status dos registros no banco de dados.
* É necessário configurar corretamente permissões de service account, acesso ao Pub/Sub, acesso ao Secret Manager e conectividade com o SQL Server.
* Caso o tempo de execução do Job seja maior que o intervalo do Scheduler, pode haver sobreposição de execuções. Essa situação deve ser tratada por lock lógico no banco ou por controle de execução.
* A garantia de que 100% dos contratos estejam na Holmes depende também da correta identificação dos registros elegíveis, do monitoramento de falhas e da idempotência do processamento no consumidor.

## Alternativas consideradas

1. **Cloud Run Service com endpoint HTTP**

   * Prós: simples de acionar via requisição HTTP.
   * Contras: expõe um serviço desnecessário para um processo que não precisa responder requisições de usuário; menos adequado para execução batch finita.

2. **Worker contínuo**

   * Prós: adequado para consumidores que ficam ativos continuamente.
   * Contras: mantém processo rodando mesmo quando não há trabalho; aumenta custo e complexidade operacional para uma rotina periódica.

3. **Execução manual ou via pipeline**

   * Prós: implementação inicial simples.
   * Contras: não atende bem ao requisito de recorrência automática e aumenta dependência operacional, o que não é adequado para uma demanda mandatória de completude dos contratos na Holmes.

4. **Cloud Scheduler chamando diretamente uma API existente**

   * Prós: reaproveita infraestrutura já existente.
   * Contras: mistura responsabilidade de API online com processamento batch; pode aumentar acoplamento e dificultar controle de timeout, retry e rastreabilidade do processo.
