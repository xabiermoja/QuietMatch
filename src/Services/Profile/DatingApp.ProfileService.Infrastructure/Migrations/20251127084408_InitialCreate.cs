using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DatingApp.ProfileService.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "member_profiles",
                columns: table => new
                {
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    full_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    date_of_birth = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    gender = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    city = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    country = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    latitude = table.Column<decimal>(type: "numeric(9,6)", nullable: true),
                    longitude = table.Column<decimal>(type: "numeric(9,6)", nullable: true),
                    personality_openness = table.Column<int>(type: "integer", nullable: true),
                    personality_conscientiousness = table.Column<int>(type: "integer", nullable: true),
                    personality_extraversion = table.Column<int>(type: "integer", nullable: true),
                    personality_agreeableness = table.Column<int>(type: "integer", nullable: true),
                    personality_neuroticism = table.Column<int>(type: "integer", nullable: true),
                    personality_about_me = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    personality_life_philosophy = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    values_family_orientation = table.Column<int>(type: "integer", nullable: true),
                    values_career_ambition = table.Column<int>(type: "integer", nullable: true),
                    values_spirituality = table.Column<int>(type: "integer", nullable: true),
                    values_adventure = table.Column<int>(type: "integer", nullable: true),
                    values_intellectual_curiosity = table.Column<int>(type: "integer", nullable: true),
                    values_social_justice = table.Column<int>(type: "integer", nullable: true),
                    values_financial_security = table.Column<int>(type: "integer", nullable: true),
                    values_environmentalism = table.Column<int>(type: "integer", nullable: true),
                    lifestyle_exercise_frequency = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    lifestyle_diet_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    lifestyle_smoking_status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    lifestyle_drinking_frequency = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    lifestyle_has_pets = table.Column<bool>(type: "boolean", nullable: true),
                    lifestyle_wants_children = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    preferences_age_min = table.Column<int>(type: "integer", nullable: true),
                    preferences_age_max = table.Column<int>(type: "integer", nullable: true),
                    preferences_max_distance_km = table.Column<int>(type: "integer", nullable: true),
                    preferences_languages = table.Column<List<string>>(type: "jsonb", nullable: true),
                    preferences_gender = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    exposure_level = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    completion_percentage = table.Column<int>(type: "integer", nullable: false),
                    is_complete = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    deleted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_member_profiles", x => x.user_id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_member_profiles_created_at",
                table: "member_profiles",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_member_profiles_deleted_at",
                table: "member_profiles",
                column: "deleted_at");

            migrationBuilder.CreateIndex(
                name: "ix_member_profiles_is_complete",
                table: "member_profiles",
                column: "is_complete");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "member_profiles");
        }
    }
}
