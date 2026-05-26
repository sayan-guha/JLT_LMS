using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace JLT.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddLearningContentManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "content_tags",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_content_tags", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "learning_content",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    content_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    version = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    mime_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    storage_url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    file_size = table.Column<long>(type: "bigint", nullable: true),
                    duration_seconds = table.Column<int>(type: "integer", nullable: true),
                    external_url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    config = table.Column<string>(type: "jsonb", nullable: true),
                    thumbnail_url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    updated_by = table.Column<Guid>(type: "uuid", nullable: false),
                    published_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    valid_from = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    valid_till = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    retired_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    next_review_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    reviewed_by = table.Column<Guid>(type: "uuid", nullable: true),
                    reviewed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    content_source = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    source_url = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    author = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    publisher = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    copyright = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    license_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    language = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    locale = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    estimated_duration_minutes = table.Column<int>(type: "integer", nullable: true),
                    category = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    tags = table.Column<string>(type: "jsonb", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_learning_content", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "xapi_statements",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor_json = table.Column<string>(type: "jsonb", nullable: false),
                    verb_id = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    object_json = table.Column<string>(type: "jsonb", nullable: false),
                    result_json = table.Column<string>(type: "jsonb", nullable: true),
                    context_json = table.Column<string>(type: "jsonb", nullable: true),
                    timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    stored_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_xapi_statements", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "content_progress",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    learning_content_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    progress_percent = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    bookmark_data = table.Column<string>(type: "jsonb", nullable: true),
                    time_spent_seconds = table.Column<int>(type: "integer", nullable: false),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_accessed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_content_progress", x => x.id);
                    table.ForeignKey(
                        name: "fk_content_progress_learning_content_learning_content_id",
                        column: x => x.learning_content_id,
                        principalTable: "learning_content",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "scorm_packages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    learning_content_id = table.Column<Guid>(type: "uuid", nullable: false),
                    entry_point = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    scorm_version = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    manifest_data = table.Column<string>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_scorm_packages", x => x.id);
                    table.ForeignKey(
                        name: "fk_scorm_packages_learning_content_learning_content_id",
                        column: x => x.learning_content_id,
                        principalTable: "learning_content",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "scorm_runtime_states",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scorm_package_id = table.Column<Guid>(type: "uuid", nullable: false),
                    lesson_status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    lesson_location = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    suspend_data = table.Column<string>(type: "text", nullable: true),
                    raw_score = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    min_score = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    max_score = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    session_time = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    total_time = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    entry = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_scorm_runtime_states", x => x.id);
                    table.ForeignKey(
                        name: "fk_scorm_runtime_states_scorm_packages_scorm_package_id",
                        column: x => x.scorm_package_id,
                        principalTable: "scorm_packages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_content_progress_learning_content_id",
                table: "content_progress",
                column: "learning_content_id");

            migrationBuilder.CreateIndex(
                name: "ix_content_progress_user_id",
                table: "content_progress",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_content_progress_user_id_learning_content_id",
                table: "content_progress",
                columns: new[] { "user_id", "learning_content_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_content_tags_tenant_id",
                table: "content_tags",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_content_tags_tenant_id_name",
                table: "content_tags",
                columns: new[] { "tenant_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_learning_content_tags",
                table: "learning_content",
                column: "tags")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "jsonb_path_ops" });

            migrationBuilder.CreateIndex(
                name: "ix_learning_content_tenant_id",
                table: "learning_content",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_learning_content_tenant_id_category",
                table: "learning_content",
                columns: new[] { "tenant_id", "category" });

            migrationBuilder.CreateIndex(
                name: "ix_learning_content_tenant_id_content_type",
                table: "learning_content",
                columns: new[] { "tenant_id", "content_type" });

            migrationBuilder.CreateIndex(
                name: "ix_learning_content_tenant_id_status",
                table: "learning_content",
                columns: new[] { "tenant_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_scorm_packages_learning_content_id",
                table: "scorm_packages",
                column: "learning_content_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_scorm_runtime_states_scorm_package_id",
                table: "scorm_runtime_states",
                column: "scorm_package_id");

            migrationBuilder.CreateIndex(
                name: "ix_scorm_runtime_states_user_id",
                table: "scorm_runtime_states",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_scorm_runtime_states_user_id_scorm_package_id",
                table: "scorm_runtime_states",
                columns: new[] { "user_id", "scorm_package_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_xapi_statements_actor_json",
                table: "xapi_statements",
                column: "actor_json")
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "jsonb_path_ops" });

            migrationBuilder.CreateIndex(
                name: "ix_xapi_statements_tenant_id",
                table: "xapi_statements",
                column: "tenant_id");

            migrationBuilder.CreateIndex(
                name: "ix_xapi_statements_tenant_id_timestamp",
                table: "xapi_statements",
                columns: new[] { "tenant_id", "timestamp" });

            migrationBuilder.CreateIndex(
                name: "ix_xapi_statements_tenant_id_verb_id",
                table: "xapi_statements",
                columns: new[] { "tenant_id", "verb_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "content_progress");

            migrationBuilder.DropTable(
                name: "content_tags");

            migrationBuilder.DropTable(
                name: "scorm_runtime_states");

            migrationBuilder.DropTable(
                name: "xapi_statements");

            migrationBuilder.DropTable(
                name: "scorm_packages");

            migrationBuilder.DropTable(
                name: "learning_content");
        }
    }
}
