using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CentroInvestigaciones.Data.Migraciones
{
    /// <inheritdoc />
    public partial class NucleoInicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "nucleo");

            migrationBuilder.CreateTable(
                name: "Areas",
                schema: "nucleo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Codigo = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Areas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EventosAuditoria",
                schema: "nucleo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FechaUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Accion = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Entidad = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    EntidadId = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Resultado = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ValoresAnteriores = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ValoresNuevos = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DireccionRed = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    AgenteUsuario = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventosAuditoria", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Permisos",
                schema: "nucleo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Codigo = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permisos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                schema: "nucleo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Codigo = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Usuarios",
                schema: "nucleo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IdentificadorProveedor = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    CorreoElectronico = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Nombres = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Apellidos = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    NumeroContacto = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Extension = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Cargo = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    TemaPreferido = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    AreaInteresId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FechaCreacionUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UltimoAccesoUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Usuarios_Areas_AreaInteresId",
                        column: x => x.AreaInteresId,
                        principalSchema: "nucleo",
                        principalTable: "Areas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RolesPermisos",
                schema: "nucleo",
                columns: table => new
                {
                    RolId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PermisoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolesPermisos", x => new { x.RolId, x.PermisoId });
                    table.ForeignKey(
                        name: "FK_RolesPermisos_Permisos_PermisoId",
                        column: x => x.PermisoId,
                        principalSchema: "nucleo",
                        principalTable: "Permisos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RolesPermisos_Roles_RolId",
                        column: x => x.RolId,
                        principalSchema: "nucleo",
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AsignacionesRol",
                schema: "nucleo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RolId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AreaId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AsignacionesRol", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AsignacionesRol_Areas_AreaId",
                        column: x => x.AreaId,
                        principalSchema: "nucleo",
                        principalTable: "Areas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AsignacionesRol_Roles_RolId",
                        column: x => x.RolId,
                        principalSchema: "nucleo",
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AsignacionesRol_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalSchema: "nucleo",
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AsignacionesPermiso",
                schema: "nucleo",
                columns: table => new
                {
                    UsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PermisoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AsignacionesPermiso", x => new { x.UsuarioId, x.PermisoId });
                    table.ForeignKey(
                        name: "FK_AsignacionesPermiso_Permisos_PermisoId",
                        column: x => x.PermisoId,
                        principalSchema: "nucleo",
                        principalTable: "Permisos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AsignacionesPermiso_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalSchema: "nucleo",
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UsuariosAreas",
                schema: "nucleo",
                columns: table => new
                {
                    UsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AreaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsuariosAreas", x => new { x.UsuarioId, x.AreaId });
                    table.ForeignKey(
                        name: "FK_UsuariosAreas_Areas_AreaId",
                        column: x => x.AreaId,
                        principalSchema: "nucleo",
                        principalTable: "Areas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UsuariosAreas_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalSchema: "nucleo",
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Areas_Codigo",
                schema: "nucleo",
                table: "Areas",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AsignacionesPermiso_PermisoId",
                schema: "nucleo",
                table: "AsignacionesPermiso",
                column: "PermisoId");

            migrationBuilder.CreateIndex(
                name: "IX_AsignacionesRol_AreaId",
                schema: "nucleo",
                table: "AsignacionesRol",
                column: "AreaId");

            migrationBuilder.CreateIndex(
                name: "IX_AsignacionesRol_RolId",
                schema: "nucleo",
                table: "AsignacionesRol",
                column: "RolId");

            migrationBuilder.CreateIndex(
                name: "IX_AsignacionesRol_UsuarioId",
                schema: "nucleo",
                table: "AsignacionesRol",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_EventosAuditoria_FechaUtc",
                schema: "nucleo",
                table: "EventosAuditoria",
                column: "FechaUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Permisos_Codigo",
                schema: "nucleo",
                table: "Permisos",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Roles_Codigo",
                schema: "nucleo",
                table: "Roles",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RolesPermisos_PermisoId",
                schema: "nucleo",
                table: "RolesPermisos",
                column: "PermisoId");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_AreaInteresId",
                schema: "nucleo",
                table: "Usuarios",
                column: "AreaInteresId");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_IdentificadorProveedor",
                schema: "nucleo",
                table: "Usuarios",
                column: "IdentificadorProveedor",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UsuariosAreas_AreaId",
                schema: "nucleo",
                table: "UsuariosAreas",
                column: "AreaId");

            migrationBuilder.CreateTable(
                name: "Notas",
                schema: "nucleo",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UsuarioId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Titulo = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Contenido = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    FechaCreacionUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaActualizacionUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notas_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalSchema: "nucleo",
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Notas_UsuarioId",
                schema: "nucleo",
                table: "Notas",
                column: "UsuarioId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Notas",
                schema: "nucleo");

            migrationBuilder.DropTable(
                name: "AsignacionesPermiso",
                schema: "nucleo");

            migrationBuilder.DropTable(
                name: "AsignacionesRol",
                schema: "nucleo");

            migrationBuilder.DropTable(
                name: "EventosAuditoria",
                schema: "nucleo");

            migrationBuilder.DropTable(
                name: "RolesPermisos",
                schema: "nucleo");

            migrationBuilder.DropTable(
                name: "UsuariosAreas",
                schema: "nucleo");

            migrationBuilder.DropTable(
                name: "Permisos",
                schema: "nucleo");

            migrationBuilder.DropTable(
                name: "Roles",
                schema: "nucleo");

            migrationBuilder.DropTable(
                name: "Usuarios",
                schema: "nucleo");

            migrationBuilder.DropTable(
                name: "Areas",
                schema: "nucleo");
        }
    }
}
