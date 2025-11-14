using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NotificationRules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ConditionJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TargetRole = table.Column<int>(type: "int", nullable: false),
                    MessageTemplate = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    Type = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Priority = table.Column<int>(type: "int", nullable: true, defaultValue: 5),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotificationRules", x => x.Id);
                });

            var seedTimestamp = new DateTime(2025, 11, 14, 18, 27, 4, DateTimeKind.Utc);

            migrationBuilder.InsertData(
                table: "NotificationRules",
                columns: new[]
                {
                    "Id", "Name", "ConditionJson", "TargetRole", "MessageTemplate",
                    "IsActive", "Type", "Priority", "CreatedAt", "UpdatedAt"
                },
                values: new object[,]
                {
                    {
                        1,
                        "Student inactivity > 3 days",
                        "{\"type\":\"inactive_days\",\"threshold\":3}",
                        5,
                        "You have been inactive for {daysInactive} days. Jump back in to stay on track.",
                        true,
                        "inactivity",
                        1,
                        seedTimestamp,
                        null
                    },
                    {
                        2,
                        "Assignment deadline within 24h",
                        "{\"type\":\"deadline_approaching\",\"threshold\":24}",
                        5,
                        "Assignment \"{assignmentTitle}\" is due in {hoursUntilDeadline} hours.",
                        true,
                        "deadline_approaching",
                        2,
                        seedTimestamp,
                        null
                    },
                    {
                        3,
                        "Average score below 60",
                        "{\"type\":\"low_average_score\",\"threshold\":60}",
                        5,
                        "Your current average score is {averageScore}%. Let's aim higher!",
                        true,
                        "low_performance",
                        3,
                        seedTimestamp,
                        null
                    }
                });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationRules_IsActive",
                table: "NotificationRules",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationRules_TargetRole",
                table: "NotificationRules",
                column: "TargetRole");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationRules_Type",
                table: "NotificationRules",
                column: "Type");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NotificationRules");
        }
    }
}
