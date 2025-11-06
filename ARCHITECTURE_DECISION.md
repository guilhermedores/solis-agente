# 🎯 Decisão Arquitetural: PWA → Agente → Nuvem

## Contexto

Precisávamos decidir como estruturar a comunicação entre os componentes do sistema PDV, considerando:
- Operação offline (sem internet)
- Acesso a periféricos (impressoras, gaveta, TEF)
- Sincronização de dados
- Performance

## Opções Consideradas

### Opção 1: PWA → API Nuvem (Direto)
```
PWA ──────────────→ API Nuvem ──────→ PostgreSQL
  ↓
IndexedDB (offline)
```

**Prós:**
- Arquitetura mais simples
- Menos componentes
- Comunicação HTTP padrão

**Contras:**
- ❌ Não funciona offline
- ❌ Sem acesso a periféricos
- ❌ Dependente de internet
- ❌ PWA não pode acessar portas seriais

### Opção 2: PWA → Agente Local (Escolhida ✅)
```
PWA ──→ Agente PDV (localhost:5000) ──→ API Nuvem
   ↓          ↓                              ↓
IndexedDB  SQLite                      PostgreSQL
           ↓
      Periféricos
```

**Prós:**
- ✅ **Funciona offline** (SQLite local)
- ✅ **Acesso a periféricos** (Serial Port)
- ✅ **Sincronização automática** (Background Service)
- ✅ **Performance** (comunicação local)
- ✅ **Resiliência** (fila de retry)
- ✅ **Cache inteligente** (produtos locais)

**Contras:**
- Requer instalação de serviço
- Um componente adicional

## Decisão: PWA → Agente → Nuvem ✅

### Justificativa

1. **Realidade do Varejo Brasileiro**
   - Internet pode falhar
   - PDV não pode parar de vender
   - Sincronização posterior é aceitável

2. **Periféricos são Essenciais**
   - Impressora térmica (obrigatório)
   - Gaveta de dinheiro (segurança)
   - TEF/SAT (fiscal)
   - PWA não acessa hardware diretamente

3. **Performance**
   - Comunicação local (localhost) é instantânea
   - Sem latência de rede
   - Cache de produtos no SQLite

4. **Experiência do Usuário**
   - Venda não trava se internet cair
   - Impressão imediata do cupom
   - Sincronização transparente

## Arquitetura Implementada

### Fluxo de Venda

```
1. Usuário registra venda no PWA
2. PWA envia para Agente (localhost:5000)
3. Agente:
   - Salva no SQLite local
   - Imprime cupom
   - Tenta sincronizar com nuvem
   - Se falhar, adiciona à fila de sync
4. Background Service:
   - A cada 5 minutos tenta sincronizar pendências
   - Atualiza cache de produtos
```

### Fluxo de Busca de Produto

```
1. PWA consulta Agente
2. Agente:
   - Busca no SQLite local (cache)
   - Se não encontrar, busca na API Nuvem
   - Cacheia produto localmente
   - Retorna para PWA
```

### Sincronização

```
Background Service (a cada 5 min):
1. Verifica vendas não sincronizadas
2. Tenta enviar para API Nuvem
3. Se sucesso:
   - Marca como sincronizado
   - Registra timestamp
4. Se falhar:
   - Incrementa contador de tentativas
   - Registra erro
   - Tenta novamente no próximo ciclo
```

## Componentes Implementados

### 1. Agente PDV (.NET 8)
- ✅ Web API REST
- ✅ Entity Framework + SQLite
- ✅ Background Services (Sync, HealthCheck)
- ✅ Serial Port (Impressora/Gaveta)
- ✅ HTTP Client (API Nuvem)
- ✅ Windows Service Support
- ✅ Logging (Serilog)

### 2. Banco Local (SQLite)
- ✅ Vendas, Itens, Pagamentos
- ✅ Produtos (cache)
- ✅ Flags de sincronização
- ✅ Retry tracking

### 3. Serviços de Periféricos
- ✅ ImpressoraService (ESC/POS)
- ✅ GavetaService (Serial)
- ✅ TEF (placeholder)
- ✅ SAT (placeholder)

### 4. Sincronização
- ✅ SyncService (background)
- ✅ Retry automático
- ✅ Fila de vendas pendentes
- ✅ Atualização de cache de produtos

## Vantagens Confirmadas

### Para o Negócio
- ✅ **PDV nunca para** - Funciona offline
- ✅ **Conformidade Fiscal** - Impressão obrigatória
- ✅ **Segurança** - Gaveta controlada
- ✅ **Escalabilidade** - Múltiplos PDVs sincronizam

### Para o Desenvolvedor
- ✅ **Separação de responsabilidades**
- ✅ **Testabilidade** - Componentes isolados
- ✅ **Manutenibilidade** - Lógica clara
- ✅ **Extensibilidade** - Fácil adicionar periféricos

### Para o Usuário
- ✅ **Velocidade** - Sem esperar internet
- ✅ **Confiabilidade** - Sempre funciona
- ✅ **Feedback imediato** - Cupom na hora
- ✅ **Transparente** - Não vê sincronização

## Próximos Passos

1. ✅ Agente PDV implementado
2. ⏳ Implementar API Nuvem (Node.js)
3. ⏳ Implementar PWA (React)
4. ⏳ Testes de integração E2E
5. ⏳ Documentação de deployment

## Referências

- [Offline-First Design Pattern](https://offlinefirst.org/)
- [Progressive Web Apps](https://web.dev/progressive-web-apps/)
- [Event Sourcing for Sync](https://martinfowler.com/eaaDev/EventSourcing.html)

---

**Data da Decisão**: 27/10/2025  
**Status**: ✅ Implementado  
**Revisão**: Após 3 meses de uso em produção