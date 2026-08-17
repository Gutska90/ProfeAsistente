using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProfeAsistente.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Asignaturas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Codigo = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Nombre = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Asignaturas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CurriculumImportBatches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    FechaInicio = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FechaTermino = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Estado = table.Column<int>(type: "INTEGER", nullable: false),
                    CantidadFuentes = table.Column<int>(type: "INTEGER", nullable: false),
                    CantidadRegistrosNuevos = table.Column<int>(type: "INTEGER", nullable: false),
                    CantidadActualizados = table.Column<int>(type: "INTEGER", nullable: false),
                    CantidadSinCambios = table.Column<int>(type: "INTEGER", nullable: false),
                    CantidadAdvertencias = table.Column<int>(type: "INTEGER", nullable: false),
                    CantidadErrores = table.Column<int>(type: "INTEGER", nullable: false),
                    DiffJson = table.Column<string>(type: "TEXT", nullable: true),
                    ExtractionJson = table.Column<string>(type: "TEXT", nullable: true),
                    Mensaje = table.Column<string>(type: "TEXT", nullable: true),
                    CurriculumDocumentId = table.Column<Guid>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CurriculumImportBatches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CurriculumSources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Nombre = table.Column<string>(type: "TEXT", nullable: false),
                    Url = table.Column<string>(type: "TEXT", nullable: false),
                    Dominio = table.Column<string>(type: "TEXT", nullable: false),
                    TipoFuente = table.Column<int>(type: "INTEGER", nullable: false),
                    Formato = table.Column<int>(type: "INTEGER", nullable: false),
                    NivelEsperado = table.Column<string>(type: "TEXT", nullable: true),
                    AsignaturaEsperada = table.Column<string>(type: "TEXT", nullable: true),
                    Activo = table.Column<bool>(type: "INTEGER", nullable: false),
                    FechaRegistro = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CurriculumSources", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Niveles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Codigo = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Nombre = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    Ciclo = table.Column<string>(type: "TEXT", nullable: false),
                    Orden = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Niveles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CurriculumDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CurriculumSourceId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Titulo = table.Column<string>(type: "TEXT", nullable: false),
                    UrlOriginal = table.Column<string>(type: "TEXT", nullable: false),
                    TipoDocumento = table.Column<string>(type: "TEXT", nullable: false),
                    NumeroDecreto = table.Column<string>(type: "TEXT", nullable: true),
                    FechaPublicacion = table.Column<DateTime>(type: "TEXT", nullable: true),
                    FechaDescarga = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ETag = table.Column<string>(type: "TEXT", nullable: true),
                    LastModified = table.Column<string>(type: "TEXT", nullable: true),
                    HashSha256 = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    RutaArchivoLocal = table.Column<string>(type: "TEXT", nullable: false),
                    VersionDetectada = table.Column<string>(type: "TEXT", nullable: false),
                    EstadoProcesamiento = table.Column<int>(type: "INTEGER", nullable: false),
                    TextoExtraido = table.Column<string>(type: "TEXT", nullable: true),
                    ErrorProcesamiento = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CurriculumDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CurriculumDocuments_CurriculumSources_CurriculumSourceId",
                        column: x => x.CurriculumSourceId,
                        principalTable: "CurriculumSources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Actitudes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    NivelAsignaturaId = table.Column<Guid>(type: "TEXT", nullable: true),
                    NivelId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Codigo = table.Column<string>(type: "TEXT", nullable: true),
                    Descripcion = table.Column<string>(type: "TEXT", nullable: false),
                    Vigente = table.Column<bool>(type: "INTEGER", nullable: false),
                    EstadoRevision = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Actitudes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Actitudes_Niveles_NivelId",
                        column: x => x.NivelId,
                        principalTable: "Niveles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NivelesAsignaturas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    NivelId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AsignaturaId = table.Column<Guid>(type: "TEXT", nullable: false),
                    NombreEnNivel = table.Column<string>(type: "TEXT", nullable: false),
                    Activa = table.Column<bool>(type: "INTEGER", nullable: false),
                    EstadoRevision = table.Column<int>(type: "INTEGER", nullable: false),
                    Vigente = table.Column<bool>(type: "INTEGER", nullable: false),
                    ConfianzaExtraccion = table.Column<double>(type: "REAL", nullable: false),
                    FuenteTipo = table.Column<string>(type: "TEXT", nullable: false),
                    EsContenidoOficial = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NivelesAsignaturas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NivelesAsignaturas_Asignaturas_AsignaturaId",
                        column: x => x.AsignaturaId,
                        principalTable: "Asignaturas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_NivelesAsignaturas_Niveles_NivelId",
                        column: x => x.NivelId,
                        principalTable: "Niveles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Oats",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    NivelId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Codigo = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Dimension = table.Column<string>(type: "TEXT", nullable: true),
                    Descripcion = table.Column<string>(type: "TEXT", nullable: false),
                    Vigente = table.Column<bool>(type: "INTEGER", nullable: false),
                    Version = table.Column<string>(type: "TEXT", nullable: false),
                    EstadoRevision = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Oats", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Oats_Niveles_NivelId",
                        column: x => x.NivelId,
                        principalTable: "Niveles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CurriculumRecordSources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CurriculumDocumentId = table.Column<Guid>(type: "TEXT", nullable: false),
                    TipoEntidad = table.Column<string>(type: "TEXT", nullable: false),
                    EntidadId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PaginaInicio = table.Column<int>(type: "INTEGER", nullable: true),
                    PaginaFin = table.Column<int>(type: "INTEGER", nullable: true),
                    FragmentoFuente = table.Column<string>(type: "TEXT", nullable: true),
                    FechaVigenciaDesde = table.Column<DateTime>(type: "TEXT", nullable: true),
                    FechaVigenciaHasta = table.Column<DateTime>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CurriculumRecordSources", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CurriculumRecordSources_CurriculumDocuments_CurriculumDocumentId",
                        column: x => x.CurriculumDocumentId,
                        principalTable: "CurriculumDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "EjesCurriculares",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    NivelAsignaturaId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Codigo = table.Column<string>(type: "TEXT", nullable: true),
                    Nombre = table.Column<string>(type: "TEXT", nullable: false),
                    Descripcion = table.Column<string>(type: "TEXT", nullable: true),
                    EstadoRevision = table.Column<int>(type: "INTEGER", nullable: false),
                    Vigente = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EjesCurriculares", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EjesCurriculares_NivelesAsignaturas_NivelAsignaturaId",
                        column: x => x.NivelAsignaturaId,
                        principalTable: "NivelesAsignaturas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Habilidades",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    NivelAsignaturaId = table.Column<Guid>(type: "TEXT", nullable: false),
                    EjeCurricularId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Codigo = table.Column<string>(type: "TEXT", nullable: true),
                    Descripcion = table.Column<string>(type: "TEXT", nullable: false),
                    Vigente = table.Column<bool>(type: "INTEGER", nullable: false),
                    EstadoRevision = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Habilidades", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Habilidades_NivelesAsignaturas_NivelAsignaturaId",
                        column: x => x.NivelAsignaturaId,
                        principalTable: "NivelesAsignaturas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Unidades",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    NivelAsignaturaId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Numero = table.Column<int>(type: "INTEGER", nullable: false),
                    Nombre = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Descripcion = table.Column<string>(type: "TEXT", nullable: true),
                    HorasPedagogicasSugeridas = table.Column<int>(type: "INTEGER", nullable: true),
                    Orden = table.Column<int>(type: "INTEGER", nullable: false),
                    EstadoRevision = table.Column<int>(type: "INTEGER", nullable: false),
                    Vigente = table.Column<bool>(type: "INTEGER", nullable: false),
                    FuenteTipo = table.Column<string>(type: "TEXT", nullable: false),
                    EsContenidoOficial = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Unidades", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Unidades_NivelesAsignaturas_NivelAsignaturaId",
                        column: x => x.NivelAsignaturaId,
                        principalTable: "NivelesAsignaturas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ObjetivosAprendizaje",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    NivelAsignaturaId = table.Column<Guid>(type: "TEXT", nullable: false),
                    EjeCurricularId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Codigo = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Numero = table.Column<int>(type: "INTEGER", nullable: true),
                    Descripcion = table.Column<string>(type: "TEXT", nullable: false),
                    Tipo = table.Column<int>(type: "INTEGER", nullable: false),
                    EsObligatorio = table.Column<bool>(type: "INTEGER", nullable: false),
                    Vigente = table.Column<bool>(type: "INTEGER", nullable: false),
                    Version = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    EstadoRevision = table.Column<int>(type: "INTEGER", nullable: false),
                    ConfianzaExtraccion = table.Column<double>(type: "REAL", nullable: false),
                    ObservacionRevision = table.Column<string>(type: "TEXT", nullable: true),
                    FuenteTipo = table.Column<string>(type: "TEXT", nullable: false),
                    EsContenidoOficial = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ObjetivosAprendizaje", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ObjetivosAprendizaje_EjesCurriculares_EjeCurricularId",
                        column: x => x.EjeCurricularId,
                        principalTable: "EjesCurriculares",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ObjetivosAprendizaje_NivelesAsignaturas_NivelAsignaturaId",
                        column: x => x.NivelAsignaturaId,
                        principalTable: "NivelesAsignaturas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Planificaciones",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    NivelId = table.Column<Guid>(type: "TEXT", nullable: false),
                    NivelAsignaturaId = table.Column<Guid>(type: "TEXT", nullable: false),
                    UnidadId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Nombre = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    FechaInicio = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    FechaFin = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Estado = table.Column<int>(type: "INTEGER", nullable: false),
                    FechaCreacion = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Planificaciones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Planificaciones_NivelesAsignaturas_NivelAsignaturaId",
                        column: x => x.NivelAsignaturaId,
                        principalTable: "NivelesAsignaturas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Planificaciones_Niveles_NivelId",
                        column: x => x.NivelId,
                        principalTable: "Niveles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Planificaciones_Unidades_UnidadId",
                        column: x => x.UnidadId,
                        principalTable: "Unidades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "IndicadoresEvaluacion",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ObjetivoAprendizajeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    UnidadId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Codigo = table.Column<string>(type: "TEXT", nullable: true),
                    Descripcion = table.Column<string>(type: "TEXT", nullable: false),
                    EsSugerido = table.Column<bool>(type: "INTEGER", nullable: false),
                    Orden = table.Column<int>(type: "INTEGER", nullable: false),
                    Vigente = table.Column<bool>(type: "INTEGER", nullable: false),
                    EstadoRevision = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IndicadoresEvaluacion", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IndicadoresEvaluacion_ObjetivosAprendizaje_ObjetivoAprendizajeId",
                        column: x => x.ObjetivoAprendizajeId,
                        principalTable: "ObjetivosAprendizaje",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UnidadObjetivos",
                columns: table => new
                {
                    UnidadId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ObjetivoAprendizajeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Orden = table.Column<int>(type: "INTEGER", nullable: false),
                    EsPrincipal = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnidadObjetivos", x => new { x.UnidadId, x.ObjetivoAprendizajeId });
                    table.ForeignKey(
                        name: "FK_UnidadObjetivos_ObjetivosAprendizaje_ObjetivoAprendizajeId",
                        column: x => x.ObjetivoAprendizajeId,
                        principalTable: "ObjetivosAprendizaje",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UnidadObjetivos_Unidades_UnidadId",
                        column: x => x.UnidadId,
                        principalTable: "Unidades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Clases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    PlanificacionId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Numero = table.Column<int>(type: "INTEGER", nullable: false),
                    Fecha = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    ObjetivoAprendizajeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    NivelBloom = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    DescripcionInicio = table.Column<string>(type: "TEXT", nullable: true),
                    DescripcionDesarrollo = table.Column<string>(type: "TEXT", nullable: true),
                    DescripcionCierre = table.Column<string>(type: "TEXT", nullable: true),
                    Estado = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Clases_ObjetivosAprendizaje_ObjetivoAprendizajeId",
                        column: x => x.ObjetivoAprendizajeId,
                        principalTable: "ObjetivosAprendizaje",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Clases_Planificaciones_PlanificacionId",
                        column: x => x.PlanificacionId,
                        principalTable: "Planificaciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ClaseCurriculumSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ClaseId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ObjetivoAprendizajeId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CodigoOA = table.Column<string>(type: "TEXT", nullable: false),
                    DescripcionOA = table.Column<string>(type: "TEXT", nullable: false),
                    IndicadoresJson = table.Column<string>(type: "TEXT", nullable: false),
                    HabilidadesJson = table.Column<string>(type: "TEXT", nullable: false),
                    ActitudesJson = table.Column<string>(type: "TEXT", nullable: false),
                    VersionCurricular = table.Column<string>(type: "TEXT", nullable: false),
                    CurriculumDocumentId = table.Column<Guid>(type: "TEXT", nullable: true),
                    FechaSnapshot = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClaseCurriculumSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClaseCurriculumSnapshots_Clases_ClaseId",
                        column: x => x.ClaseId,
                        principalTable: "Clases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ClaseIndicadores",
                columns: table => new
                {
                    ClaseId = table.Column<Guid>(type: "TEXT", nullable: false),
                    IndicadorEvaluacionId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClaseIndicadores", x => new { x.ClaseId, x.IndicadorEvaluacionId });
                    table.ForeignKey(
                        name: "FK_ClaseIndicadores_Clases_ClaseId",
                        column: x => x.ClaseId,
                        principalTable: "Clases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ClaseIndicadores_IndicadoresEvaluacion_IndicadorEvaluacionId",
                        column: x => x.IndicadorEvaluacionId,
                        principalTable: "IndicadoresEvaluacion",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Documentos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Tipo = table.Column<int>(type: "INTEGER", nullable: false),
                    ClaseId = table.Column<Guid>(type: "TEXT", nullable: true),
                    NivelId = table.Column<Guid>(type: "TEXT", nullable: false),
                    AsignaturaId = table.Column<Guid>(type: "TEXT", nullable: false),
                    UnidadId = table.Column<Guid>(type: "TEXT", nullable: true),
                    Nivel = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    Asignatura = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Unidad = table.Column<string>(type: "TEXT", nullable: true),
                    Tema = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    ObjetivoAprendizaje = table.Column<string>(type: "TEXT", nullable: true),
                    ContenidoGenerado = table.Column<string>(type: "TEXT", nullable: false),
                    Instrucciones = table.Column<string>(type: "TEXT", nullable: true),
                    FechaCreacion = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Estado = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Documentos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Documentos_Asignaturas_AsignaturaId",
                        column: x => x.AsignaturaId,
                        principalTable: "Asignaturas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Documentos_Clases_ClaseId",
                        column: x => x.ClaseId,
                        principalTable: "Clases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Documentos_Niveles_NivelId",
                        column: x => x.NivelId,
                        principalTable: "Niveles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DocumentoObjetivos",
                columns: table => new
                {
                    DocumentoId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ObjetivoAprendizajeId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentoObjetivos", x => new { x.DocumentoId, x.ObjetivoAprendizajeId });
                    table.ForeignKey(
                        name: "FK_DocumentoObjetivos_Documentos_DocumentoId",
                        column: x => x.DocumentoId,
                        principalTable: "Documentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DocumentoObjetivos_ObjetivosAprendizaje_ObjetivoAprendizajeId",
                        column: x => x.ObjetivoAprendizajeId,
                        principalTable: "ObjetivosAprendizaje",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Items",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DocumentoId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Tipo = table.Column<int>(type: "INTEGER", nullable: false),
                    Enunciado = table.Column<string>(type: "TEXT", nullable: false),
                    AlternativasJson = table.Column<string>(type: "TEXT", nullable: false),
                    RespuestaCorrecta = table.Column<string>(type: "TEXT", nullable: true),
                    Puntaje = table.Column<int>(type: "INTEGER", nullable: false),
                    Orden = table.Column<int>(type: "INTEGER", nullable: false),
                    IndicadorEvaluacionId = table.Column<Guid>(type: "TEXT", nullable: true),
                    NivelBloom = table.Column<string>(type: "TEXT", nullable: true),
                    VerboBloom = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Items_Documentos_DocumentoId",
                        column: x => x.DocumentoId,
                        principalTable: "Documentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Items_IndicadoresEvaluacion_IndicadorEvaluacionId",
                        column: x => x.IndicadorEvaluacionId,
                        principalTable: "IndicadoresEvaluacion",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "SesionesPlanificadas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    DocumentoId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Numero = table.Column<int>(type: "INTEGER", nullable: false),
                    Descripcion = table.Column<string>(type: "TEXT", nullable: false),
                    Actividades = table.Column<string>(type: "TEXT", nullable: false),
                    NivelBloom = table.Column<string>(type: "TEXT", nullable: true),
                    VerboBloom = table.Column<string>(type: "TEXT", nullable: true),
                    ObjetivoAprendizajeId = table.Column<Guid>(type: "TEXT", nullable: true),
                    IndicadorEvaluacion = table.Column<string>(type: "TEXT", nullable: true),
                    CriterioLogro = table.Column<string>(type: "TEXT", nullable: true),
                    MinutosEstimados = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SesionesPlanificadas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SesionesPlanificadas_Documentos_DocumentoId",
                        column: x => x.DocumentoId,
                        principalTable: "Documentos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SesionesPlanificadas_ObjetivosAprendizaje_ObjetivoAprendizajeId",
                        column: x => x.ObjetivoAprendizajeId,
                        principalTable: "ObjetivosAprendizaje",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Actitudes_NivelId",
                table: "Actitudes",
                column: "NivelId");

            migrationBuilder.CreateIndex(
                name: "IX_Asignaturas_Codigo",
                table: "Asignaturas",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClaseCurriculumSnapshots_ClaseId",
                table: "ClaseCurriculumSnapshots",
                column: "ClaseId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClaseIndicadores_IndicadorEvaluacionId",
                table: "ClaseIndicadores",
                column: "IndicadorEvaluacionId");

            migrationBuilder.CreateIndex(
                name: "IX_Clases_ObjetivoAprendizajeId",
                table: "Clases",
                column: "ObjetivoAprendizajeId");

            migrationBuilder.CreateIndex(
                name: "IX_Clases_PlanificacionId_Numero",
                table: "Clases",
                columns: new[] { "PlanificacionId", "Numero" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CurriculumDocuments_CurriculumSourceId",
                table: "CurriculumDocuments",
                column: "CurriculumSourceId");

            migrationBuilder.CreateIndex(
                name: "IX_CurriculumDocuments_HashSha256",
                table: "CurriculumDocuments",
                column: "HashSha256");

            migrationBuilder.CreateIndex(
                name: "IX_CurriculumRecordSources_CurriculumDocumentId",
                table: "CurriculumRecordSources",
                column: "CurriculumDocumentId");

            migrationBuilder.CreateIndex(
                name: "IX_CurriculumRecordSources_TipoEntidad_EntidadId",
                table: "CurriculumRecordSources",
                columns: new[] { "TipoEntidad", "EntidadId" });

            migrationBuilder.CreateIndex(
                name: "IX_CurriculumSources_Url",
                table: "CurriculumSources",
                column: "Url");

            migrationBuilder.CreateIndex(
                name: "IX_DocumentoObjetivos_ObjetivoAprendizajeId",
                table: "DocumentoObjetivos",
                column: "ObjetivoAprendizajeId");

            migrationBuilder.CreateIndex(
                name: "IX_Documentos_AsignaturaId",
                table: "Documentos",
                column: "AsignaturaId");

            migrationBuilder.CreateIndex(
                name: "IX_Documentos_ClaseId",
                table: "Documentos",
                column: "ClaseId");

            migrationBuilder.CreateIndex(
                name: "IX_Documentos_NivelId",
                table: "Documentos",
                column: "NivelId");

            migrationBuilder.CreateIndex(
                name: "IX_EjesCurriculares_NivelAsignaturaId",
                table: "EjesCurriculares",
                column: "NivelAsignaturaId");

            migrationBuilder.CreateIndex(
                name: "IX_Habilidades_NivelAsignaturaId",
                table: "Habilidades",
                column: "NivelAsignaturaId");

            migrationBuilder.CreateIndex(
                name: "IX_IndicadoresEvaluacion_ObjetivoAprendizajeId",
                table: "IndicadoresEvaluacion",
                column: "ObjetivoAprendizajeId");

            migrationBuilder.CreateIndex(
                name: "IX_Items_DocumentoId",
                table: "Items",
                column: "DocumentoId");

            migrationBuilder.CreateIndex(
                name: "IX_Items_IndicadorEvaluacionId",
                table: "Items",
                column: "IndicadorEvaluacionId");

            migrationBuilder.CreateIndex(
                name: "IX_Niveles_Codigo",
                table: "Niveles",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Niveles_Orden",
                table: "Niveles",
                column: "Orden");

            migrationBuilder.CreateIndex(
                name: "IX_NivelesAsignaturas_AsignaturaId",
                table: "NivelesAsignaturas",
                column: "AsignaturaId");

            migrationBuilder.CreateIndex(
                name: "IX_NivelesAsignaturas_NivelId_AsignaturaId",
                table: "NivelesAsignaturas",
                columns: new[] { "NivelId", "AsignaturaId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Oats_Codigo_NivelId_Version",
                table: "Oats",
                columns: new[] { "Codigo", "NivelId", "Version" });

            migrationBuilder.CreateIndex(
                name: "IX_Oats_NivelId",
                table: "Oats",
                column: "NivelId");

            migrationBuilder.CreateIndex(
                name: "IX_ObjetivosAprendizaje_EjeCurricularId",
                table: "ObjetivosAprendizaje",
                column: "EjeCurricularId");

            migrationBuilder.CreateIndex(
                name: "IX_ObjetivosAprendizaje_NivelAsignaturaId_Codigo_Version",
                table: "ObjetivosAprendizaje",
                columns: new[] { "NivelAsignaturaId", "Codigo", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Planificaciones_NivelAsignaturaId",
                table: "Planificaciones",
                column: "NivelAsignaturaId");

            migrationBuilder.CreateIndex(
                name: "IX_Planificaciones_NivelId",
                table: "Planificaciones",
                column: "NivelId");

            migrationBuilder.CreateIndex(
                name: "IX_Planificaciones_UnidadId",
                table: "Planificaciones",
                column: "UnidadId");

            migrationBuilder.CreateIndex(
                name: "IX_SesionesPlanificadas_DocumentoId_Numero",
                table: "SesionesPlanificadas",
                columns: new[] { "DocumentoId", "Numero" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SesionesPlanificadas_ObjetivoAprendizajeId",
                table: "SesionesPlanificadas",
                column: "ObjetivoAprendizajeId");

            migrationBuilder.CreateIndex(
                name: "IX_Unidades_NivelAsignaturaId_Numero",
                table: "Unidades",
                columns: new[] { "NivelAsignaturaId", "Numero" });

            migrationBuilder.CreateIndex(
                name: "IX_UnidadObjetivos_ObjetivoAprendizajeId",
                table: "UnidadObjetivos",
                column: "ObjetivoAprendizajeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Actitudes");

            migrationBuilder.DropTable(
                name: "ClaseCurriculumSnapshots");

            migrationBuilder.DropTable(
                name: "ClaseIndicadores");

            migrationBuilder.DropTable(
                name: "CurriculumImportBatches");

            migrationBuilder.DropTable(
                name: "CurriculumRecordSources");

            migrationBuilder.DropTable(
                name: "DocumentoObjetivos");

            migrationBuilder.DropTable(
                name: "Habilidades");

            migrationBuilder.DropTable(
                name: "Items");

            migrationBuilder.DropTable(
                name: "Oats");

            migrationBuilder.DropTable(
                name: "SesionesPlanificadas");

            migrationBuilder.DropTable(
                name: "UnidadObjetivos");

            migrationBuilder.DropTable(
                name: "CurriculumDocuments");

            migrationBuilder.DropTable(
                name: "IndicadoresEvaluacion");

            migrationBuilder.DropTable(
                name: "Documentos");

            migrationBuilder.DropTable(
                name: "CurriculumSources");

            migrationBuilder.DropTable(
                name: "Clases");

            migrationBuilder.DropTable(
                name: "ObjetivosAprendizaje");

            migrationBuilder.DropTable(
                name: "Planificaciones");

            migrationBuilder.DropTable(
                name: "EjesCurriculares");

            migrationBuilder.DropTable(
                name: "Unidades");

            migrationBuilder.DropTable(
                name: "NivelesAsignaturas");

            migrationBuilder.DropTable(
                name: "Asignaturas");

            migrationBuilder.DropTable(
                name: "Niveles");
        }
    }
}
