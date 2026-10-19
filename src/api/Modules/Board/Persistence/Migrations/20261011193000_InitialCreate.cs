using System;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using ProjectTracker.Api.Modules.Board.Persistence;

namespace ProjectTracker.Api.Modules.Board.Persistence.Migrations;

[DbContext(typeof(BoardDbContext))]
[Migration("20261011193000_InitialCreate")]
public partial class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "boards",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                name = table.Column<string>(type: "text", nullable: false),
                created_by = table.Column<Guid>(type: "uuid", nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_boards", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "board_columns",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                board_id = table.Column<Guid>(type: "uuid", nullable: false),
                name = table.Column<string>(type: "text", nullable: false),
                sort_order = table.Column<string>(type: "character varying(64)", nullable: false, collation: "C"),
                column_type = table.Column<string>(type: "character varying(32)", nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_board_columns", x => x.id);
                table.UniqueConstraint("AK_board_columns_board_id_id", x => new { x.board_id, x.id });
                table.ForeignKey(
                    name: "FK_board_columns_boards_board_id",
                    column: x => x.board_id,
                    principalTable: "boards",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.CheckConstraint("CK_board_columns_column_type", "column_type IN ('initiative_only', 'initiative_task')");
            });

        migrationBuilder.CreateTable(
            name: "tags",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                board_id = table.Column<Guid>(type: "uuid", nullable: false),
                name = table.Column<string>(type: "text", nullable: false),
                color = table.Column<string>(type: "text", nullable: true),
                deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_tags", x => x.id);
                table.UniqueConstraint("AK_tags_board_id_id", x => new { x.board_id, x.id });
                table.ForeignKey(
                    name: "FK_tags_boards_board_id",
                    column: x => x.board_id,
                    principalTable: "boards",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "task_sub_stages",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                board_id = table.Column<Guid>(type: "uuid", nullable: false),
                parent_column_id = table.Column<Guid>(type: "uuid", nullable: false),
                name = table.Column<string>(type: "text", nullable: false),
                sort_order = table.Column<string>(type: "character varying(64)", nullable: false, collation: "C"),
                is_inactive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_task_sub_stages", x => x.id);
                table.UniqueConstraint("AK_task_sub_stages_board_id_id", x => new { x.board_id, x.id });
                table.ForeignKey(
                    name: "FK_task_sub_stages_board_columns_board_id_parent_column_id",
                    columns: x => new { x.board_id, x.parent_column_id },
                    principalTable: "board_columns",
                    principalColumns: new[] { "board_id", "id" },
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "initiatives",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                board_id = table.Column<Guid>(type: "uuid", nullable: false),
                column_id = table.Column<Guid>(type: "uuid", nullable: false),
                title = table.Column<string>(type: "text", nullable: false),
                description = table.Column<string>(type: "text", nullable: true),
                sort_order = table.Column<string>(type: "character varying(64)", nullable: false, collation: "C"),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_initiatives", x => x.id);
                table.UniqueConstraint("AK_initiatives_board_id_id", x => new { x.board_id, x.id });
                table.ForeignKey(
                    name: "FK_initiatives_board_columns_board_id_column_id",
                    columns: x => new { x.board_id, x.column_id },
                    principalTable: "board_columns",
                    principalColumns: new[] { "board_id", "id" },
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "tasks",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                board_id = table.Column<Guid>(type: "uuid", nullable: false),
                created_by = table.Column<Guid>(type: "uuid", nullable: false),
                initiative_id = table.Column<Guid>(type: "uuid", nullable: false),
                task_sub_stage_id = table.Column<Guid>(type: "uuid", nullable: true),
                dormant_sub_stage_id = table.Column<Guid>(type: "uuid", nullable: true),
                title = table.Column<string>(type: "text", nullable: false),
                description = table.Column<string>(type: "text", nullable: true),
                due_date = table.Column<DateOnly>(type: "date", nullable: true),
                priority = table.Column<string>(type: "character varying(16)", nullable: false),
                sort_order = table.Column<string>(type: "character varying(64)", nullable: false, collation: "C"),
                row_version = table.Column<long>(type: "bigint", nullable: false, defaultValue: 1L),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                deleted_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_tasks", x => x.id);
                table.UniqueConstraint("AK_tasks_board_id_id", x => new { x.board_id, x.id });
                table.ForeignKey(
                    name: "FK_tasks_initiatives_board_id_initiative_id",
                    columns: x => new { x.board_id, x.initiative_id },
                    principalTable: "initiatives",
                    principalColumns: new[] { "board_id", "id" },
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_tasks_task_sub_stages_board_id_dormant_sub_stage_id",
                    columns: x => new { x.board_id, x.dormant_sub_stage_id },
                    principalTable: "task_sub_stages",
                    principalColumns: new[] { "board_id", "id" },
                    onDelete: ReferentialAction.SetNull);
                table.ForeignKey(
                    name: "FK_tasks_task_sub_stages_board_id_task_sub_stage_id",
                    columns: x => new { x.board_id, x.task_sub_stage_id },
                    principalTable: "task_sub_stages",
                    principalColumns: new[] { "board_id", "id" },
                    onDelete: ReferentialAction.Restrict);
                table.CheckConstraint("CK_tasks_priority", "priority IN ('Low', 'Medium', 'High', 'Urgent')");
            });

        migrationBuilder.CreateTable(
            name: "task_assignees",
            columns: table => new
            {
                board_id = table.Column<Guid>(type: "uuid", nullable: false),
                task_id = table.Column<Guid>(type: "uuid", nullable: false),
                user_id = table.Column<Guid>(type: "uuid", nullable: false),
                assigned_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                assigned_by = table.Column<Guid>(type: "uuid", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_task_assignees", x => new { x.board_id, x.task_id, x.user_id });
                table.ForeignKey(
                    name: "FK_task_assignees_tasks_board_id_task_id",
                    columns: x => new { x.board_id, x.task_id },
                    principalTable: "tasks",
                    principalColumns: new[] { "board_id", "id" },
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "task_tags",
            columns: table => new
            {
                board_id = table.Column<Guid>(type: "uuid", nullable: false),
                task_id = table.Column<Guid>(type: "uuid", nullable: false),
                tag_id = table.Column<Guid>(type: "uuid", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_task_tags", x => new { x.board_id, x.task_id, x.tag_id });
                table.ForeignKey(
                    name: "FK_task_tags_tags_board_id_tag_id",
                    columns: x => new { x.board_id, x.tag_id },
                    principalTable: "tags",
                    principalColumns: new[] { "board_id", "id" },
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_task_tags_tasks_board_id_task_id",
                    columns: x => new { x.board_id, x.task_id },
                    principalTable: "tasks",
                    principalColumns: new[] { "board_id", "id" },
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_boards_id",
            table: "boards",
            column: "id",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_board_columns_board_id_id",
            table: "board_columns",
            columns: new[] { "board_id", "id" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_board_columns_board_id_sort_order",
            table: "board_columns",
            columns: new[] { "board_id", "sort_order" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_initiatives_board_id_id",
            table: "initiatives",
            columns: new[] { "board_id", "id" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_initiatives_board_id_column_id_sort_order",
            table: "initiatives",
            columns: new[] { "board_id", "column_id", "sort_order" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_tags_board_id_id",
            table: "tags",
            columns: new[] { "board_id", "id" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_task_assignees_user_id",
            table: "task_assignees",
            column: "user_id");

        migrationBuilder.CreateIndex(
            name: "IX_task_assignees_board_id_task_id_user_id",
            table: "task_assignees",
            columns: new[] { "board_id", "task_id", "user_id" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_task_sub_stages_board_id_id",
            table: "task_sub_stages",
            columns: new[] { "board_id", "id" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_task_sub_stages_board_id_parent_column_id_sort_order",
            table: "task_sub_stages",
            columns: new[] { "board_id", "parent_column_id", "sort_order" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_tasks_board_id_id",
            table: "tasks",
            columns: new[] { "board_id", "id" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_tasks_board_id_initiative_id",
            table: "tasks",
            columns: new[] { "board_id", "initiative_id" });

        migrationBuilder.CreateIndex(
            name: "IX_tasks_board_id_task_sub_stage_id",
            table: "tasks",
            columns: new[] { "board_id", "task_sub_stage_id" });

        migrationBuilder.CreateIndex(
            name: "IX_task_tags_board_id_task_id_tag_id",
            table: "task_tags",
            columns: new[] { "board_id", "task_id", "tag_id" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_task_tags_board_id_tag_id",
            table: "task_tags",
            columns: new[] { "board_id", "tag_id" });

        migrationBuilder.Sql(@"
CREATE UNIQUE INDEX ""IX_tasks_board_id_initiative_id_task_sub_stage_id"" 
    ON ""tasks"" (""board_id"", ""initiative_id"", ""task_sub_stage_id"") NULLS NOT DISTINCT;");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP INDEX IF EXISTS \"IX_tasks_board_id_initiative_id_task_sub_stage_id\";");

        migrationBuilder.DropTable(name: "task_assignees");
        migrationBuilder.DropTable(name: "task_tags");
        migrationBuilder.DropTable(name: "tasks");
        migrationBuilder.DropTable(name: "tags");
        migrationBuilder.DropTable(name: "initiatives");
        migrationBuilder.DropTable(name: "task_sub_stages");
        migrationBuilder.DropTable(name: "board_columns");
        migrationBuilder.DropTable(name: "boards");
    }
}
