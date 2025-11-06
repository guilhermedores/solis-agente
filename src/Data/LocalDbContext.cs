using Solis.AgentePDV.Models;
using Microsoft.EntityFrameworkCore;

namespace Solis.AgentePDV.Data;

/// <summary>
/// Contexto do banco de dados local (SQLite) para operação offline
/// </summary>
public class LocalDbContext : DbContext
{
    public LocalDbContext(DbContextOptions<LocalDbContext> options) : base(options)
    {
    }

    public DbSet<Venda> Vendas { get; set; }
    public DbSet<VendaItem> VendaItens { get; set; }
    public DbSet<VendaPagamento> VendaPagamentos { get; set; }
    public DbSet<Produto> Produtos { get; set; }
    public DbSet<ProdutoPreco> ProdutoPrecos { get; set; }
    public DbSet<Empresa> Empresas { get; set; }
    public DbSet<RegimeTributario> RegimesTributarios { get; set; }
    public DbSet<Enquadramento> Enquadramentos { get; set; }
    public DbSet<RegimeEspecialTributacao> RegimesEspeciaisTributacao { get; set; }
    public DbSet<OutboxMessage> OutboxMessages { get; set; }
    public DbSet<Caixa> Caixas { get; set; }
    public DbSet<FormaPagamento> FormasPagamento { get; set; }
    public DbSet<StatusVenda> StatusVendas { get; set; }
    public DbSet<Configuracao> Configuracoes { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configurações de Venda
        modelBuilder.Entity<Venda>(entity =>
        {
            entity.ToTable("Vendas");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.NumeroCupom);
            entity.HasIndex(e => e.Sincronizado);
            entity.HasIndex(e => e.CreatedAt);

            entity.HasMany(e => e.Itens)
                .WithOne(e => e.Venda)
                .HasForeignKey(e => e.VendaId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Pagamentos)
                .WithOne(e => e.Venda)
                .HasForeignKey(e => e.VendaId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasOne(e => e.StatusVenda)
                .WithMany(s => s.Vendas)
                .HasForeignKey(e => e.StatusVendaId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configurações de VendaItem
        modelBuilder.Entity<VendaItem>(entity =>
        {
            entity.ToTable("VendaItens");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.VendaId, e.Sequencia }).IsUnique();
        });

        // Configurações de VendaPagamento
        modelBuilder.Entity<VendaPagamento>(entity =>
        {
            entity.ToTable("VendaPagamentos");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.VendaId);
            entity.HasIndex(e => e.FormaPagamentoId);
            
            entity.HasOne(e => e.FormaPagamento)
                .WithMany(f => f.Pagamentos)
                .HasForeignKey(e => e.FormaPagamentoId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configurações de Produto
        modelBuilder.Entity<Produto>(entity =>
        {
            entity.ToTable("Produtos");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.CodigoBarras);
            entity.HasIndex(e => e.CodigoInterno);
            entity.HasIndex(e => e.Nome);
            entity.HasIndex(e => e.Ativo);
            
            // Relacionamento com Preço (1:1)
            entity.HasOne(e => e.PrecoAtual)
                .WithOne(e => e.Produto)
                .HasForeignKey<ProdutoPreco>(e => e.ProdutoId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configurações de ProdutoPreco
        modelBuilder.Entity<ProdutoPreco>(entity =>
        {
            entity.ToTable("ProdutoPrecos");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ProdutoId).IsUnique();
            entity.HasIndex(e => e.Ativo);
        });

        // Configurações de Empresa
        modelBuilder.Entity<Empresa>(entity =>
        {
            entity.ToTable("Empresas");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.CNPJ).IsUnique();
            entity.HasIndex(e => e.Ativo);
            
            entity.HasOne(e => e.RegimeTributario)
                .WithMany()
                .HasForeignKey(e => e.RegimeTributarioId)
                .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasOne(e => e.Enquadramento)
                .WithMany()
                .HasForeignKey(e => e.EnquadramentoId)
                .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasOne(e => e.RegimeEspecialTributacao)
                .WithMany()
                .HasForeignKey(e => e.RegimeEspecialTributacaoId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configurações de RegimeTributario
        modelBuilder.Entity<RegimeTributario>(entity =>
        {
            entity.ToTable("RegimesTributarios");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Codigo).IsUnique();
        });

        // Configurações de Enquadramento
        modelBuilder.Entity<Enquadramento>(entity =>
        {
            entity.ToTable("Enquadramentos");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Codigo).IsUnique();
        });

        // Configurações de RegimeEspecialTributacao
        modelBuilder.Entity<RegimeEspecialTributacao>(entity =>
        {
            entity.ToTable("RegimesEspeciaisTributacao");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Codigo).IsUnique();
        });

        // Configurações de OutboxMessage
        modelBuilder.Entity<OutboxMessage>(entity =>
        {
            entity.ToTable("OutboxMessages");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CriadoEm);
            entity.HasIndex(e => e.ProximaTentativaEm);
            entity.HasIndex(e => new { e.Status, e.ProximaTentativaEm, e.Prioridade });
        });

        // Configurações de Caixa
        modelBuilder.Entity<Caixa>(entity =>
        {
            entity.ToTable("Caixas");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.NumeroTerminal);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.DataAbertura);
            entity.HasIndex(e => new { e.NumeroTerminal, e.Status });
            entity.HasIndex(e => e.Sincronizado);
            
            entity.HasMany(e => e.Vendas)
                .WithOne(e => e.Caixa)
                .HasForeignKey(e => e.CaixaId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Configurações de FormaPagamento
        modelBuilder.Entity<FormaPagamento>(entity =>
        {
            entity.ToTable("FormasPagamento");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Descricao);
            entity.HasIndex(e => e.Tipo);
            entity.HasIndex(e => e.Codigo);
            entity.HasIndex(e => e.Ativa);
            entity.HasIndex(e => e.Sincronizado);
            entity.HasIndex(e => new { e.Tipo, e.Ativa });
            entity.HasIndex(e => e.Ordem);
            
            entity.HasMany(e => e.Pagamentos)
                .WithOne(p => p.FormaPagamento)
                .HasForeignKey(p => p.FormaPagamentoId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configurações de StatusVenda
        modelBuilder.Entity<StatusVenda>(entity =>
        {
            entity.ToTable("StatusVendas");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Codigo).IsUnique();
            entity.HasIndex(e => e.Ativa);
            entity.HasIndex(e => e.Sincronizado);
            entity.HasIndex(e => e.Ordem);
            entity.HasIndex(e => new { e.Ativa, e.Ordem });
        });

        // Configurações de Configuracao
        modelBuilder.Entity<Configuracao>(entity =>
        {
            entity.ToTable("Configuracoes");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Chave).IsUnique();
        });
    }
}