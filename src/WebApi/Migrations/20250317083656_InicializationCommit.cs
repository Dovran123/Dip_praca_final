using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApi.Migrations
{
    /// <inheritdoc />
    public partial class InicializationCommit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Diagnozy",
                columns: table => new
                {
                    KodDiagnozy = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Nazov = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Diagnozy", x => x.KodDiagnozy);
                });

            migrationBuilder.CreateTable(
                name: "KategoriaLiekov",
                columns: table => new
                {
                    Kod = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Nazov = table.Column<string>(type: "nvarchar(800)", maxLength: 800, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KategoriaLiekov", x => x.Kod);
                });

            migrationBuilder.CreateTable(
                name: "LimityPredpisov",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    YearsLimit = table.Column<int>(type: "int", nullable: true),
                    MonthsLimit = table.Column<int>(type: "int", nullable: true),
                    WeeksLimit = table.Column<int>(type: "int", nullable: true),
                    LimitValue = table.Column<int>(type: "int", nullable: true),
                    CasovyOkamih = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LimityPredpisov", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OdbornostiLekarov",
                columns: table => new
                {
                    Identifikator = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    PopisOdbornosti = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OdbornostiLekarov", x => x.Identifikator);
                });

            migrationBuilder.CreateTable(
                name: "Poistenia",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ICP = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    Datum = table.Column<DateTime>(type: "datetime2", nullable: true),
                    KodPoistovne = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: true),
                    PoistnyVztahPoistenca = table.Column<int>(type: "int", nullable: false),
                    JeNeplatic = table.Column<bool>(type: "bit", nullable: false),
                    ZaciatokEuPoistenia = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MaNarokNaOdkladnuZS = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Poistenia", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Pouzivatelia",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Meno = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Priezvisko = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    RodneCislo = table.Column<string>(type: "nvarchar(11)", maxLength: 11, nullable: false),
                    Heslo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Typ = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    JePrimatel = table.Column<bool>(type: "bit", nullable: false),
                    TokenPrimatela = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pouzivatelia", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Lieky",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    KodKategorie = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Kod = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Nazov = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NazovDoplnku = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PO = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lieky", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Lieky_KategoriaLiekov_KodKategorie",
                        column: x => x.KodKategorie,
                        principalTable: "KategoriaLiekov",
                        principalColumn: "Kod",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Kategoria",
                columns: table => new
                {
                    Kod = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Nazov = table.Column<string>(type: "nvarchar(800)", maxLength: 800, nullable: false),
                    NadriadenaKategorieKod = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Discriminator = table.Column<string>(type: "nvarchar(21)", maxLength: 21, nullable: false),
                    TypDoplnku = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LimitPredpisuId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TypPotraviny = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Kategoria", x => x.Kod);
                    table.ForeignKey(
                        name: "FK_Kategoria_Kategoria_NadriadenaKategorieKod",
                        column: x => x.NadriadenaKategorieKod,
                        principalTable: "Kategoria",
                        principalColumn: "Kod");
                    table.ForeignKey(
                        name: "FK_Kategoria_LimityPredpisov_LimitPredpisuId",
                        column: x => x.LimitPredpisuId,
                        principalTable: "LimityPredpisov",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Lekari",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LicencneCislo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PouzivatelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    KodOdbornoti = table.Column<string>(type: "nvarchar(3)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lekari", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Lekari_OdbornostiLekarov_KodOdbornoti",
                        column: x => x.KodOdbornoti,
                        principalTable: "OdbornostiLekarov",
                        principalColumn: "Identifikator",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Lekari_Pouzivatelia_PouzivatelId",
                        column: x => x.PouzivatelId,
                        principalTable: "Pouzivatelia",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Pacienti",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PouzivatelId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PoistenieId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pacienti", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Pacienti_Poistenia_PoistenieId",
                        column: x => x.PoistenieId,
                        principalTable: "Poistenia",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Pacienti_Pouzivatelia_PouzivatelId",
                        column: x => x.PouzivatelId,
                        principalTable: "Pouzivatelia",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Doplnky",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    KodKategorie = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Kod = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Nazov = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NazovDoplnku = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PO = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Doplnky", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Doplnky_Kategoria_KodKategorie",
                        column: x => x.KodKategorie,
                        principalTable: "Kategoria",
                        principalColumn: "Kod",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Potraviny",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    KodKategorie = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Kod = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Nazov = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NazovDoplnku = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PO = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Potraviny", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Potraviny_Kategoria_KodKategorie",
                        column: x => x.KodKategorie,
                        principalTable: "Kategoria",
                        principalColumn: "Kod",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Ambulancie",
                columns: table => new
                {
                    Kod = table.Column<string>(type: "nvarchar(12)", maxLength: 12, nullable: false),
                    Nazov = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Adresa = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Mesto = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PSC = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    LekarId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ambulancie", x => x.Kod);
                    table.ForeignKey(
                        name: "FK_Ambulancie_Lekari_LekarId",
                        column: x => x.LekarId,
                        principalTable: "Lekari",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PreskripcneZaznamy",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PacientId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LekarId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AmbulanciaKod = table.Column<string>(type: "nvarchar(12)", nullable: true),
                    DatumPredpisu = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PocetDni = table.Column<int>(type: "int", nullable: false),
                    Stav = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    DiagnozaKod = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PreskripcneZaznamy", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PreskripcneZaznamy_Ambulancie_AmbulanciaKod",
                        column: x => x.AmbulanciaKod,
                        principalTable: "Ambulancie",
                        principalColumn: "Kod");
                    table.ForeignKey(
                        name: "FK_PreskripcneZaznamy_Diagnozy_DiagnozaKod",
                        column: x => x.DiagnozaKod,
                        principalTable: "Diagnozy",
                        principalColumn: "KodDiagnozy");
                    table.ForeignKey(
                        name: "FK_PreskripcneZaznamy_Lekari_LekarId",
                        column: x => x.LekarId,
                        principalTable: "Lekari",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PreskripcneZaznamy_Pacienti_PacientId",
                        column: x => x.PacientId,
                        principalTable: "Pacienti",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PrekricnyZaznamDoplnok",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DoplnokId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Mnozstvo = table.Column<int>(type: "int", nullable: false),
                    PrekricnyZaznamId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrekricnyZaznamDoplnok", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PrekricnyZaznamDoplnok_Doplnky_DoplnokId",
                        column: x => x.DoplnokId,
                        principalTable: "Doplnky",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PrekricnyZaznamDoplnok_PreskripcneZaznamy_PrekricnyZaznamId",
                        column: x => x.PrekricnyZaznamId,
                        principalTable: "PreskripcneZaznamy",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PrekricnyZaznamLiekov",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LiekId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PrekricnyZaznamId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrekricnyZaznamLiekov", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PrekricnyZaznamLiekov_Lieky_LiekId",
                        column: x => x.LiekId,
                        principalTable: "Lieky",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PrekricnyZaznamLiekov_PreskripcneZaznamy_PrekricnyZaznamId",
                        column: x => x.PrekricnyZaznamId,
                        principalTable: "PreskripcneZaznamy",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PrekricnyZaznamPotraviny",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PotravinaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PrekricnyZaznamId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrekricnyZaznamPotraviny", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PrekricnyZaznamPotraviny_Potraviny_PotravinaId",
                        column: x => x.PotravinaId,
                        principalTable: "Potraviny",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PrekricnyZaznamPotraviny_PreskripcneZaznamy_PrekricnyZaznamId",
                        column: x => x.PrekricnyZaznamId,
                        principalTable: "PreskripcneZaznamy",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Ambulancie_LekarId",
                table: "Ambulancie",
                column: "LekarId");

            migrationBuilder.CreateIndex(
                name: "IX_Doplnky_KodKategorie",
                table: "Doplnky",
                column: "KodKategorie");

            migrationBuilder.CreateIndex(
                name: "IX_Kategoria_LimitPredpisuId",
                table: "Kategoria",
                column: "LimitPredpisuId");

            migrationBuilder.CreateIndex(
                name: "IX_Kategoria_NadriadenaKategorieKod",
                table: "Kategoria",
                column: "NadriadenaKategorieKod");

            migrationBuilder.CreateIndex(
                name: "IX_Lekari_KodOdbornoti",
                table: "Lekari",
                column: "KodOdbornoti");

            migrationBuilder.CreateIndex(
                name: "IX_Lekari_PouzivatelId",
                table: "Lekari",
                column: "PouzivatelId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Lieky_KodKategorie",
                table: "Lieky",
                column: "KodKategorie");

            migrationBuilder.CreateIndex(
                name: "IX_Pacienti_PoistenieId",
                table: "Pacienti",
                column: "PoistenieId");

            migrationBuilder.CreateIndex(
                name: "IX_Pacienti_PouzivatelId",
                table: "Pacienti",
                column: "PouzivatelId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Potraviny_KodKategorie",
                table: "Potraviny",
                column: "KodKategorie");

            migrationBuilder.CreateIndex(
                name: "IX_Pouzivatelia_RodneCislo",
                table: "Pouzivatelia",
                column: "RodneCislo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PrekricnyZaznamDoplnok_DoplnokId",
                table: "PrekricnyZaznamDoplnok",
                column: "DoplnokId");

            migrationBuilder.CreateIndex(
                name: "IX_PrekricnyZaznamDoplnok_PrekricnyZaznamId",
                table: "PrekricnyZaznamDoplnok",
                column: "PrekricnyZaznamId");

            migrationBuilder.CreateIndex(
                name: "IX_PrekricnyZaznamLiekov_LiekId",
                table: "PrekricnyZaznamLiekov",
                column: "LiekId");

            migrationBuilder.CreateIndex(
                name: "IX_PrekricnyZaznamLiekov_PrekricnyZaznamId",
                table: "PrekricnyZaznamLiekov",
                column: "PrekricnyZaznamId");

            migrationBuilder.CreateIndex(
                name: "IX_PrekricnyZaznamPotraviny_PotravinaId",
                table: "PrekricnyZaznamPotraviny",
                column: "PotravinaId");

            migrationBuilder.CreateIndex(
                name: "IX_PrekricnyZaznamPotraviny_PrekricnyZaznamId",
                table: "PrekricnyZaznamPotraviny",
                column: "PrekricnyZaznamId");

            migrationBuilder.CreateIndex(
                name: "IX_PreskripcneZaznamy_AmbulanciaKod",
                table: "PreskripcneZaznamy",
                column: "AmbulanciaKod");

            migrationBuilder.CreateIndex(
                name: "IX_PreskripcneZaznamy_DiagnozaKod",
                table: "PreskripcneZaznamy",
                column: "DiagnozaKod");

            migrationBuilder.CreateIndex(
                name: "IX_PreskripcneZaznamy_LekarId",
                table: "PreskripcneZaznamy",
                column: "LekarId");

            migrationBuilder.CreateIndex(
                name: "IX_PreskripcneZaznamy_PacientId",
                table: "PreskripcneZaznamy",
                column: "PacientId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PrekricnyZaznamDoplnok");

            migrationBuilder.DropTable(
                name: "PrekricnyZaznamLiekov");

            migrationBuilder.DropTable(
                name: "PrekricnyZaznamPotraviny");

            migrationBuilder.DropTable(
                name: "Doplnky");

            migrationBuilder.DropTable(
                name: "Lieky");

            migrationBuilder.DropTable(
                name: "Potraviny");

            migrationBuilder.DropTable(
                name: "PreskripcneZaznamy");

            migrationBuilder.DropTable(
                name: "KategoriaLiekov");

            migrationBuilder.DropTable(
                name: "Kategoria");

            migrationBuilder.DropTable(
                name: "Ambulancie");

            migrationBuilder.DropTable(
                name: "Diagnozy");

            migrationBuilder.DropTable(
                name: "Pacienti");

            migrationBuilder.DropTable(
                name: "LimityPredpisov");

            migrationBuilder.DropTable(
                name: "Lekari");

            migrationBuilder.DropTable(
                name: "Poistenia");

            migrationBuilder.DropTable(
                name: "OdbornostiLekarov");

            migrationBuilder.DropTable(
                name: "Pouzivatelia");
        }
    }
}
