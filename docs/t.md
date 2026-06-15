• **RF7: Reenvio automático de registros pendentes de envio para Holmes**

○ **RF7.1:** O sistema deve executar periodicamente um processo para buscar registros pendentes de envio para Holmes no banco de dados.

○ **RF7.2:** O sistema deve permitir configurar a quantidade máxima de registros a serem buscados a cada execução.

○ **RF7.3:** O sistema deve reservar temporariamente os registros selecionados, evitando que sejam capturados por mais de uma execução ao mesmo tempo.

○ **RF7.4:** O sistema deve enviar os registros selecionados para a fila de processamento de envio para Holmes.

○ **RF7.5:** O sistema deve registrar quais itens foram enviados com sucesso para a fila, incluindo a data e hora do envio.

○ **RF7.6:** Em caso de falha durante o processo, os registros que não forem concluídos devem ficar disponíveis para uma nova tentativa após o tempo de reserva expirar.
