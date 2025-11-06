using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Solis.AgentePDV.Migrations
{
    /// <inheritdoc />
    public partial class AddTokenFieldsToConfiguracao : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Caixas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    NumeroTerminal = table.Column<int>(type: "INTEGER", nullable: false),
                    OperadorId = table.Column<string>(type: "TEXT", nullable: true),
                    OperadorNome = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    DataAbertura = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ValorAbertura = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DataFechamento = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ValorFechamento = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    TotalVendas = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalDinheiro = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalDebito = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalCredito = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalPix = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalOutros = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    QuantidadeVendas = table.Column<int>(type: "INTEGER", nullable: false),
                    Diferenca = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Observacoes = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    Sincronizado = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Caixas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Configuracoes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Chave = table.Column<string>(type: "TEXT", nullable: false),
                    Valor = table.Column<string>(type: "TEXT", nullable: false),
                    Token = table.Column<string>(type: "TEXT", nullable: true),
                    TenantId = table.Column<string>(type: "TEXT", nullable: true),
                    TokenValidoAte = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ApiBaseUrl = table.Column<string>(type: "TEXT", nullable: true),
                    NomeAgente = table.Column<string>(type: "TEXT", nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "TEXT", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Configuracoes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Enquadramentos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Codigo = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    Nome = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Descricao = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Ativo = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Enquadramentos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FormasPagamento",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Descricao = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Tipo = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    ExternalId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Codigo = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    Ativa = table.Column<bool>(type: "INTEGER", nullable: false),
                    PermiteTroco = table.Column<bool>(type: "INTEGER", nullable: false),
                    MaximoParcelas = table.Column<int>(type: "INTEGER", nullable: true),
                    TaxaJuros = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                    RequerTEF = table.Column<bool>(type: "INTEGER", nullable: false),
                    Bandeira = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Ordem = table.Column<int>(type: "INTEGER", nullable: false),
                    Sincronizado = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FormasPagamento", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OutboxMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TipoEntidade = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Operacao = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    EntidadeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PayloadJson = table.Column<string>(type: "TEXT", nullable: false),
                    EndpointApi = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    MetodoHttp = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    TentativasEnvio = table.Column<int>(type: "INTEGER", nullable: false),
                    MaxTentativas = table.Column<int>(type: "INTEGER", nullable: false),
                    UltimoErro = table.Column<string>(type: "TEXT", nullable: true),
                    UltimoStatusCode = table.Column<int>(type: "INTEGER", nullable: true),
                    CriadoEm = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UltimaTentativaEm = table.Column<DateTime>(type: "TEXT", nullable: true),
                    EnviadoEm = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ProximaTentativaEm = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Prioridade = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OutboxMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Produtos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CodigoBarras = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    CodigoInterno = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    Nome = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Descricao = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    NCM = table.Column<string>(type: "TEXT", maxLength: 8, nullable: true),
                    CEST = table.Column<string>(type: "TEXT", maxLength: 7, nullable: true),
                    UnidadeMedida = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    Ativo = table.Column<bool>(type: "INTEGER", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "TEXT", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "TEXT", nullable: false),
                    SincronizadoEm = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Produtos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RegimesEspeciaisTributacao",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Codigo = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    Nome = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Descricao = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Ativo = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegimesEspeciaisTributacao", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RegimesTributarios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Codigo = table.Column<int>(type: "INTEGER", nullable: false),
                    Nome = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Descricao = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    AliquotaPadrao = table.Column<decimal>(type: "TEXT", nullable: true),
                    Ativo = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegimesTributarios", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StatusVendas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Codigo = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Descricao = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Cor = table.Column<string>(type: "TEXT", maxLength: 7, nullable: true),
                    Ativa = table.Column<bool>(type: "INTEGER", nullable: false),
                    Ordem = table.Column<int>(type: "INTEGER", nullable: false),
                    PermiteEdicao = table.Column<bool>(type: "INTEGER", nullable: false),
                    PermiteCancelamento = table.Column<bool>(type: "INTEGER", nullable: false),
                    StatusFinal = table.Column<bool>(type: "INTEGER", nullable: false),
                    Sincronizado = table.Column<bool>(type: "INTEGER", nullable: false),
                    SincronizadoEm = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StatusVendas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProdutoPrecos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProdutoId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PrecoVenda = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Ativo = table.Column<bool>(type: "INTEGER", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "TEXT", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "TEXT", nullable: false),
                    SincronizadoEm = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProdutoPrecos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProdutoPrecos_Produtos_ProdutoId",
                        column: x => x.ProdutoId,
                        principalTable: "Produtos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Empresas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    RazaoSocial = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    NomeFantasia = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    CNPJ = table.Column<string>(type: "TEXT", maxLength: 14, nullable: false),
                    InscricaoEstadual = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    InscricaoMunicipal = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    Logradouro = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Numero = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    Complemento = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Bairro = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    CEP = table.Column<string>(type: "TEXT", maxLength: 8, nullable: false),
                    Cidade = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    UF = table.Column<string>(type: "TEXT", maxLength: 2, nullable: false),
                    Telefone = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    Email = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    RegimeTributarioId = table.Column<int>(type: "INTEGER", nullable: false),
                    EnquadramentoId = table.Column<int>(type: "INTEGER", nullable: true),
                    RegimeEspecialTributacaoId = table.Column<int>(type: "INTEGER", nullable: true),
                    CNAE = table.Column<string>(type: "TEXT", maxLength: 10, nullable: true),
                    MensagemCupom = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Ativo = table.Column<bool>(type: "INTEGER", nullable: false),
                    CriadoEm = table.Column<DateTime>(type: "TEXT", nullable: false),
                    AtualizadoEm = table.Column<DateTime>(type: "TEXT", nullable: false),
                    SincronizadoEm = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Empresas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Empresas_Enquadramentos_EnquadramentoId",
                        column: x => x.EnquadramentoId,
                        principalTable: "Enquadramentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Empresas_RegimesEspeciaisTributacao_RegimeEspecialTributacaoId",
                        column: x => x.RegimeEspecialTributacaoId,
                        principalTable: "RegimesEspeciaisTributacao",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Empresas_RegimesTributarios_RegimeTributarioId",
                        column: x => x.RegimeTributarioId,
                        principalTable: "RegimesTributarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Vendas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    NumeroCupom = table.Column<long>(type: "INTEGER", nullable: false),
                    EstabelecimentoId = table.Column<Guid>(type: "TEXT", nullable: true),
                    PdvId = table.Column<Guid>(type: "TEXT", nullable: true),
                    UsuarioId = table.Column<Guid>(type: "TEXT", nullable: true),
                    CaixaId = table.Column<Guid>(type: "TEXT", nullable: true),
                    ClienteCpf = table.Column<string>(type: "TEXT", nullable: true),
                    ClienteNome = table.Column<string>(type: "TEXT", nullable: true),
                    ClienteEmail = table.Column<string>(type: "TEXT", nullable: true),
                    ValorBruto = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    ValorDesconto = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    ValorLiquido = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    StatusVendaId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Observacoes = table.Column<string>(type: "TEXT", nullable: true),
                    Sincronizado = table.Column<bool>(type: "INTEGER", nullable: false),
                    SincronizadoEm = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TentativasSync = table.Column<int>(type: "INTEGER", nullable: false),
                    ErroSync = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vendas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Vendas_Caixas_CaixaId",
                        column: x => x.CaixaId,
                        principalTable: "Caixas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Vendas_StatusVendas_StatusVendaId",
                        column: x => x.StatusVendaId,
                        principalTable: "StatusVendas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VendaItens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    VendaId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ProdutoId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Sequencia = table.Column<int>(type: "INTEGER", nullable: false),
                    CodigoProduto = table.Column<string>(type: "TEXT", nullable: false),
                    NomeProduto = table.Column<string>(type: "TEXT", nullable: false),
                    Quantidade = table.Column<decimal>(type: "decimal(10,3)", nullable: false),
                    PrecoUnitario = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    DescontoItem = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    ValorTotal = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VendaItens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VendaItens_Vendas_VendaId",
                        column: x => x.VendaId,
                        principalTable: "Vendas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "VendaPagamentos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    VendaId = table.Column<Guid>(type: "TEXT", nullable: false),
                    FormaPagamentoId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Valor = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    ValorTroco = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    Parcelas = table.Column<int>(type: "INTEGER", nullable: true),
                    NSU = table.Column<string>(type: "TEXT", nullable: true),
                    Autorizacao = table.Column<string>(type: "TEXT", nullable: true),
                    Bandeira = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VendaPagamentos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VendaPagamentos_FormasPagamento_FormaPagamentoId",
                        column: x => x.FormaPagamentoId,
                        principalTable: "FormasPagamento",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VendaPagamentos_Vendas_VendaId",
                        column: x => x.VendaId,
                        principalTable: "Vendas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Caixas_DataAbertura",
                table: "Caixas",
                column: "DataAbertura");

            migrationBuilder.CreateIndex(
                name: "IX_Caixas_NumeroTerminal",
                table: "Caixas",
                column: "NumeroTerminal");

            migrationBuilder.CreateIndex(
                name: "IX_Caixas_NumeroTerminal_Status",
                table: "Caixas",
                columns: new[] { "NumeroTerminal", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Caixas_Sincronizado",
                table: "Caixas",
                column: "Sincronizado");

            migrationBuilder.CreateIndex(
                name: "IX_Caixas_Status",
                table: "Caixas",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Configuracoes_Chave",
                table: "Configuracoes",
                column: "Chave",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Empresas_Ativo",
                table: "Empresas",
                column: "Ativo");

            migrationBuilder.CreateIndex(
                name: "IX_Empresas_CNPJ",
                table: "Empresas",
                column: "CNPJ",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Empresas_EnquadramentoId",
                table: "Empresas",
                column: "EnquadramentoId");

            migrationBuilder.CreateIndex(
                name: "IX_Empresas_RegimeEspecialTributacaoId",
                table: "Empresas",
                column: "RegimeEspecialTributacaoId");

            migrationBuilder.CreateIndex(
                name: "IX_Empresas_RegimeTributarioId",
                table: "Empresas",
                column: "RegimeTributarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Enquadramentos_Codigo",
                table: "Enquadramentos",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FormasPagamento_Ativa",
                table: "FormasPagamento",
                column: "Ativa");

            migrationBuilder.CreateIndex(
                name: "IX_FormasPagamento_Codigo",
                table: "FormasPagamento",
                column: "Codigo");

            migrationBuilder.CreateIndex(
                name: "IX_FormasPagamento_Descricao",
                table: "FormasPagamento",
                column: "Descricao");

            migrationBuilder.CreateIndex(
                name: "IX_FormasPagamento_Ordem",
                table: "FormasPagamento",
                column: "Ordem");

            migrationBuilder.CreateIndex(
                name: "IX_FormasPagamento_Sincronizado",
                table: "FormasPagamento",
                column: "Sincronizado");

            migrationBuilder.CreateIndex(
                name: "IX_FormasPagamento_Tipo",
                table: "FormasPagamento",
                column: "Tipo");

            migrationBuilder.CreateIndex(
                name: "IX_FormasPagamento_Tipo_Ativa",
                table: "FormasPagamento",
                columns: new[] { "Tipo", "Ativa" });

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_CriadoEm",
                table: "OutboxMessages",
                column: "CriadoEm");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_ProximaTentativaEm",
                table: "OutboxMessages",
                column: "ProximaTentativaEm");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_Status",
                table: "OutboxMessages",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_OutboxMessages_Status_ProximaTentativaEm_Prioridade",
                table: "OutboxMessages",
                columns: new[] { "Status", "ProximaTentativaEm", "Prioridade" });

            migrationBuilder.CreateIndex(
                name: "IX_ProdutoPrecos_Ativo",
                table: "ProdutoPrecos",
                column: "Ativo");

            migrationBuilder.CreateIndex(
                name: "IX_ProdutoPrecos_ProdutoId",
                table: "ProdutoPrecos",
                column: "ProdutoId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Produtos_Ativo",
                table: "Produtos",
                column: "Ativo");

            migrationBuilder.CreateIndex(
                name: "IX_Produtos_CodigoBarras",
                table: "Produtos",
                column: "CodigoBarras");

            migrationBuilder.CreateIndex(
                name: "IX_Produtos_CodigoInterno",
                table: "Produtos",
                column: "CodigoInterno");

            migrationBuilder.CreateIndex(
                name: "IX_Produtos_Nome",
                table: "Produtos",
                column: "Nome");

            migrationBuilder.CreateIndex(
                name: "IX_RegimesEspeciaisTributacao_Codigo",
                table: "RegimesEspeciaisTributacao",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RegimesTributarios_Codigo",
                table: "RegimesTributarios",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StatusVendas_Ativa",
                table: "StatusVendas",
                column: "Ativa");

            migrationBuilder.CreateIndex(
                name: "IX_StatusVendas_Ativa_Ordem",
                table: "StatusVendas",
                columns: new[] { "Ativa", "Ordem" });

            migrationBuilder.CreateIndex(
                name: "IX_StatusVendas_Codigo",
                table: "StatusVendas",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StatusVendas_Ordem",
                table: "StatusVendas",
                column: "Ordem");

            migrationBuilder.CreateIndex(
                name: "IX_StatusVendas_Sincronizado",
                table: "StatusVendas",
                column: "Sincronizado");

            migrationBuilder.CreateIndex(
                name: "IX_VendaItens_VendaId_Sequencia",
                table: "VendaItens",
                columns: new[] { "VendaId", "Sequencia" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VendaPagamentos_FormaPagamentoId",
                table: "VendaPagamentos",
                column: "FormaPagamentoId");

            migrationBuilder.CreateIndex(
                name: "IX_VendaPagamentos_VendaId",
                table: "VendaPagamentos",
                column: "VendaId");

            migrationBuilder.CreateIndex(
                name: "IX_Vendas_CaixaId",
                table: "Vendas",
                column: "CaixaId");

            migrationBuilder.CreateIndex(
                name: "IX_Vendas_CreatedAt",
                table: "Vendas",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Vendas_NumeroCupom",
                table: "Vendas",
                column: "NumeroCupom");

            migrationBuilder.CreateIndex(
                name: "IX_Vendas_Sincronizado",
                table: "Vendas",
                column: "Sincronizado");

            migrationBuilder.CreateIndex(
                name: "IX_Vendas_StatusVendaId",
                table: "Vendas",
                column: "StatusVendaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Configuracoes");

            migrationBuilder.DropTable(
                name: "Empresas");

            migrationBuilder.DropTable(
                name: "OutboxMessages");

            migrationBuilder.DropTable(
                name: "ProdutoPrecos");

            migrationBuilder.DropTable(
                name: "VendaItens");

            migrationBuilder.DropTable(
                name: "VendaPagamentos");

            migrationBuilder.DropTable(
                name: "Enquadramentos");

            migrationBuilder.DropTable(
                name: "RegimesEspeciaisTributacao");

            migrationBuilder.DropTable(
                name: "RegimesTributarios");

            migrationBuilder.DropTable(
                name: "Produtos");

            migrationBuilder.DropTable(
                name: "FormasPagamento");

            migrationBuilder.DropTable(
                name: "Vendas");

            migrationBuilder.DropTable(
                name: "Caixas");

            migrationBuilder.DropTable(
                name: "StatusVendas");
        }
    }
}
