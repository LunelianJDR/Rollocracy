CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260309133521_InitialCreate') THEN
    CREATE TABLE "AttributeDefinitions" (
        "Id" uuid NOT NULL,
        "GameSystemId" uuid NOT NULL,
        "Name" text NOT NULL,
        "MinValue" integer NOT NULL,
        "MaxValue" integer NOT NULL,
        "DefaultValue" integer NOT NULL,
        CONSTRAINT "PK_AttributeDefinitions" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260309133521_InitialCreate') THEN
    CREATE TABLE "CharacterAttributeValues" (
        "Id" uuid NOT NULL,
        "CharacterId" uuid NOT NULL,
        "AttributeDefinitionId" uuid NOT NULL,
        "Value" integer NOT NULL,
        CONSTRAINT "PK_CharacterAttributeValues" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260309133521_InitialCreate') THEN
    CREATE TABLE "Characters" (
        "Id" uuid NOT NULL,
        "PlayerSessionId" uuid NOT NULL,
        "Name" text NOT NULL,
        "Health" integer NOT NULL,
        "MaxHealth" integer NOT NULL,
        "Experience" integer NOT NULL,
        "Level" integer NOT NULL,
        "IsAlive" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_Characters" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260309133521_InitialCreate') THEN
    CREATE TABLE "GameEvents" (
        "Id" uuid NOT NULL,
        "SessionId" uuid NOT NULL,
        "EventType" text NOT NULL,
        "EventData" text NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_GameEvents" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260309133521_InitialCreate') THEN
    CREATE TABLE "GameSystems" (
        "Id" uuid NOT NULL,
        "StreamerId" uuid NOT NULL,
        "Name" text NOT NULL,
        "Description" text NOT NULL,
        CONSTRAINT "PK_GameSystems" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260309133521_InitialCreate') THEN
    CREATE TABLE "GameTestConsequences" (
        "Id" uuid NOT NULL,
        "GameTestId" uuid NOT NULL,
        "CharacterId" uuid NOT NULL,
        "Type" integer NOT NULL,
        "Value" integer NOT NULL,
        "PreviousValue" integer NOT NULL,
        CONSTRAINT "PK_GameTestConsequences" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260309133521_InitialCreate') THEN
    CREATE TABLE "GameTests" (
        "Id" uuid NOT NULL,
        "SessionId" uuid NOT NULL,
        "AttributeDefinitionId" uuid NOT NULL,
        "DiceFormula" text NOT NULL,
        "Modifier" integer NOT NULL,
        "TargetValue" integer NOT NULL,
        "Comparison" integer NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "DurationSeconds" integer NOT NULL,
        "IsCancelled" boolean NOT NULL,
        "TargetType" integer NOT NULL,
        "Status" integer NOT NULL,
        CONSTRAINT "PK_GameTests" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260309133521_InitialCreate') THEN
    CREATE TABLE "PlayerSessions" (
        "Id" uuid NOT NULL,
        "SessionId" uuid NOT NULL,
        "PlayerName" text NOT NULL,
        "JoinedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_PlayerSessions" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260309133521_InitialCreate') THEN
    CREATE TABLE "PlayerTestRolls" (
        "Id" uuid NOT NULL,
        "GameTestId" uuid NOT NULL,
        "PlayerSessionId" uuid NOT NULL,
        "TotalResult" integer NOT NULL,
        "DiceDetails" text NOT NULL,
        "IsSuccess" boolean NOT NULL,
        "IsAutoRolled" boolean NOT NULL,
        "RolledAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_PlayerTestRolls" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260309133521_InitialCreate') THEN
    CREATE TABLE "Sessions" (
        "Id" uuid NOT NULL,
        "StreamerId" uuid NOT NULL,
        "GameSystemId" uuid NOT NULL,
        "SessionName" text NOT NULL,
        "IsActive" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_Sessions" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260309133521_InitialCreate') THEN
    CREATE TABLE "Streamers" (
        "Id" uuid NOT NULL,
        "DisplayName" text NOT NULL,
        "TwitchId" text NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_Streamers" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260309133521_InitialCreate') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260309133521_InitialCreate', '10.0.5');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260309210604_AddSessionCode') THEN
    ALTER TABLE "Sessions" ADD "SessionCode" text NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260309210604_AddSessionCode') THEN
    ALTER TABLE "PlayerSessions" ADD "IsGameMaster" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260309210604_AddSessionCode') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260309210604_AddSessionCode', '10.0.5');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260310140357_AddUserAccounts') THEN
    DROP TABLE "Streamers";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260310140357_AddUserAccounts') THEN
    ALTER TABLE "Sessions" RENAME COLUMN "StreamerId" TO "GameMasterUserAccountId";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260310140357_AddUserAccounts') THEN
    ALTER TABLE "Sessions" RENAME COLUMN "SessionCode" TO "SessionSlug";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260310140357_AddUserAccounts') THEN
    ALTER TABLE "GameSystems" RENAME COLUMN "StreamerId" TO "OwnerUserAccountId";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260310140357_AddUserAccounts') THEN
    ALTER TABLE "Sessions" ADD "SessionPasswordHash" text NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260310140357_AddUserAccounts') THEN
    ALTER TABLE "PlayerSessions" ADD "UserAccountId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260310140357_AddUserAccounts') THEN
    CREATE TABLE "UserAccounts" (
        "Id" uuid NOT NULL,
        "Username" text NOT NULL,
        "PasswordHash" text NOT NULL,
        "IsGameMaster" boolean NOT NULL,
        "IsTwitchLinked" boolean NOT NULL,
        "TwitchLogin" text,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_UserAccounts" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260310140357_AddUserAccounts') THEN
    CREATE UNIQUE INDEX "IX_Sessions_GameMasterUserAccountId_SessionSlug" ON "Sessions" ("GameMasterUserAccountId", "SessionSlug");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260310140357_AddUserAccounts') THEN
    CREATE UNIQUE INDEX "IX_UserAccounts_Username" ON "UserAccounts" ("Username");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260310140357_AddUserAccounts') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260310140357_AddUserAccounts', '10.0.5');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260310145716_SessionPasswordPlainText') THEN
    ALTER TABLE "Sessions" RENAME COLUMN "SessionPasswordHash" TO "SessionPassword";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260310145716_SessionPasswordPlainText') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260310145716_SessionPasswordPlainText', '10.0.5');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260310223640_AddGameSystemTraitsAndResolutionMode') THEN
    ALTER TABLE "GameSystems" ADD "TestResolutionMode" integer NOT NULL DEFAULT 0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260310223640_AddGameSystemTraitsAndResolutionMode') THEN
    CREATE TABLE "CharacterTraitValues" (
        "Id" uuid NOT NULL,
        "CharacterId" uuid NOT NULL,
        "TraitDefinitionId" uuid NOT NULL,
        "TraitOptionId" uuid NOT NULL,
        CONSTRAINT "PK_CharacterTraitValues" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260310223640_AddGameSystemTraitsAndResolutionMode') THEN
    CREATE TABLE "TraitDefinitions" (
        "Id" uuid NOT NULL,
        "GameSystemId" uuid NOT NULL,
        "Name" text NOT NULL,
        CONSTRAINT "PK_TraitDefinitions" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260310223640_AddGameSystemTraitsAndResolutionMode') THEN
    CREATE TABLE "TraitOptions" (
        "Id" uuid NOT NULL,
        "TraitDefinitionId" uuid NOT NULL,
        "Name" text NOT NULL,
        CONSTRAINT "PK_TraitOptions" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260310223640_AddGameSystemTraitsAndResolutionMode') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260310223640_AddGameSystemTraitsAndResolutionMode', '10.0.5');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260310231533_LinkSessionToSelectableGameSystem') THEN
    ALTER TABLE "Sessions" ALTER COLUMN "GameSystemId" DROP NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260310231533_LinkSessionToSelectableGameSystem') THEN
    ALTER TABLE "GameSystems" ADD "LockedToSessionId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260310231533_LinkSessionToSelectableGameSystem') THEN
    ALTER TABLE "GameSystems" ADD "SourceGameSystemId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260310231533_LinkSessionToSelectableGameSystem') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260310231533_LinkSessionToSelectableGameSystem', '10.0.5');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260310234038_AddGaugeSystem') THEN
    ALTER TABLE "Characters" DROP COLUMN "Experience";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260310234038_AddGaugeSystem') THEN
    ALTER TABLE "Characters" DROP COLUMN "Health";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260310234038_AddGaugeSystem') THEN
    ALTER TABLE "Characters" DROP COLUMN "Level";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260310234038_AddGaugeSystem') THEN
    ALTER TABLE "Characters" DROP COLUMN "MaxHealth";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260310234038_AddGaugeSystem') THEN
    CREATE TABLE "CharacterGaugeValues" (
        "Id" uuid NOT NULL,
        "CharacterId" uuid NOT NULL,
        "GaugeDefinitionId" uuid NOT NULL,
        "Value" integer NOT NULL,
        CONSTRAINT "PK_CharacterGaugeValues" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260310234038_AddGaugeSystem') THEN
    CREATE TABLE "GaugeDefinitions" (
        "Id" uuid NOT NULL,
        "GameSystemId" uuid NOT NULL,
        "Name" text NOT NULL,
        "MinValue" integer NOT NULL,
        "MaxValue" integer NOT NULL,
        "DefaultValue" integer NOT NULL,
        "IsHealthGauge" boolean NOT NULL,
        CONSTRAINT "PK_GaugeDefinitions" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260310234038_AddGaugeSystem') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260310234038_AddGaugeSystem', '10.0.5');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260310235716_AddUserLanguage') THEN
    ALTER TABLE "UserAccounts" ADD "Language" text NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260310235716_AddUserLanguage') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260310235716_AddUserLanguage', '10.0.5');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260311134441_AddCharacterBiography') THEN
    ALTER TABLE "Characters" ADD "Biography" text NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260311134441_AddCharacterBiography') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260311134441_AddCharacterBiography', '10.0.5');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260311150753_AddGameTestV1') THEN
    ALTER TABLE "PlayerTestRolls" DROP COLUMN "RolledAt";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260311150753_AddGameTestV1') THEN
    ALTER TABLE "GameTests" DROP COLUMN "Comparison";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260311150753_AddGameTestV1') THEN
    ALTER TABLE "GameTests" DROP COLUMN "DurationSeconds";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260311150753_AddGameTestV1') THEN
    ALTER TABLE "GameTests" DROP COLUMN "Modifier";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260311150753_AddGameTestV1') THEN
    ALTER TABLE "PlayerTestRolls" RENAME COLUMN "TotalResult" TO "FinalValue";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260311150753_AddGameTestV1') THEN
    ALTER TABLE "PlayerTestRolls" RENAME COLUMN "DiceDetails" TO "PlayerNameSnapshot";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260311150753_AddGameTestV1') THEN
    ALTER TABLE "GameTests" RENAME COLUMN "TargetValue" TO "ResolutionModeSnapshot";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260311150753_AddGameTestV1') THEN
    ALTER TABLE "GameTests" RENAME COLUMN "TargetType" TO "DiceSides";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260311150753_AddGameTestV1') THEN
    ALTER TABLE "GameTests" RENAME COLUMN "Status" TO "DiceCount";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260311150753_AddGameTestV1') THEN
    ALTER TABLE "GameTests" RENAME COLUMN "IsCancelled" TO "IsClosed";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260311150753_AddGameTestV1') THEN
    ALTER TABLE "GameTests" RENAME COLUMN "DiceFormula" TO "AttributeNameSnapshot";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260311150753_AddGameTestV1') THEN
    ALTER TABLE "GameTests" RENAME COLUMN "CreatedAt" TO "CreatedAtUtc";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260311150753_AddGameTestV1') THEN
    ALTER TABLE "PlayerTestRolls" ADD "AttributeValueSnapshot" integer NOT NULL DEFAULT 0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260311150753_AddGameTestV1') THEN
    ALTER TABLE "PlayerTestRolls" ADD "CharacterId" uuid NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260311150753_AddGameTestV1') THEN
    ALTER TABLE "PlayerTestRolls" ADD "CharacterNameSnapshot" text NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260311150753_AddGameTestV1') THEN
    ALTER TABLE "PlayerTestRolls" ADD "DiceResultsJson" text NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260311150753_AddGameTestV1') THEN
    ALTER TABLE "PlayerTestRolls" ADD "DiceTotal" integer NOT NULL DEFAULT 0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260311150753_AddGameTestV1') THEN
    ALTER TABLE "PlayerTestRolls" ADD "HasRolled" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260311150753_AddGameTestV1') THEN
    ALTER TABLE "PlayerTestRolls" ADD "RolledAtUtc" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260311150753_AddGameTestV1') THEN
    ALTER TABLE "GameTests" ADD "AutoRollAtUtc" timestamp with time zone NOT NULL DEFAULT TIMESTAMPTZ '-infinity';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260311150753_AddGameTestV1') THEN
    ALTER TABLE "GameTests" ADD "ClosedAtUtc" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260311150753_AddGameTestV1') THEN
    ALTER TABLE "GameTests" ADD "SuccessThreshold" integer;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260311150753_AddGameTestV1') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260311150753_AddGameTestV1', '10.0.5');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260311191725_AddGameTestV2DifficultyFiltersConsequences') THEN
    ALTER TABLE "GameTestConsequences" RENAME COLUMN "Type" TO "TargetKind";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260311191725_AddGameTestV2DifficultyFiltersConsequences') THEN
    ALTER TABLE "GameTestConsequences" RENAME COLUMN "PreviousValue" TO "ModifierMode";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260311191725_AddGameTestV2DifficultyFiltersConsequences') THEN
    ALTER TABLE "GameTestConsequences" RENAME COLUMN "CharacterId" TO "TargetDefinitionId";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260311191725_AddGameTestV2DifficultyFiltersConsequences') THEN
    ALTER TABLE "PlayerTestRolls" ADD "EffectiveAttributeValue" integer NOT NULL DEFAULT 0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260311191725_AddGameTestV2DifficultyFiltersConsequences') THEN
    ALTER TABLE "GameTests" ADD "DifficultyValue" integer NOT NULL DEFAULT 0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260311191725_AddGameTestV2DifficultyFiltersConsequences') THEN
    ALTER TABLE "GameTests" ADD "ModifierMode" integer NOT NULL DEFAULT 0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260311191725_AddGameTestV2DifficultyFiltersConsequences') THEN
    ALTER TABLE "GameTests" ADD "TargetScope" integer NOT NULL DEFAULT 0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260311191725_AddGameTestV2DifficultyFiltersConsequences') THEN
    ALTER TABLE "GameTestConsequences" ADD "ApplyOn" integer NOT NULL DEFAULT 0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260311191725_AddGameTestV2DifficultyFiltersConsequences') THEN
    ALTER TABLE "GameTestConsequences" ADD "TargetNameSnapshot" text NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260311191725_AddGameTestV2DifficultyFiltersConsequences') THEN
    CREATE TABLE "GameTestTraitFilters" (
        "Id" uuid NOT NULL,
        "GameTestId" uuid NOT NULL,
        "TraitDefinitionId" uuid NOT NULL,
        "TraitOptionId" uuid NOT NULL,
        CONSTRAINT "PK_GameTestTraitFilters" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260311191725_AddGameTestV2DifficultyFiltersConsequences') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260311191725_AddGameTestV2DifficultyFiltersConsequences', '10.0.5');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260311200940_AddTraitFilterModeToGameTest') THEN
    ALTER TABLE "GameTests" ADD "TraitFilterMode" integer NOT NULL DEFAULT 0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260311200940_AddTraitFilterModeToGameTest') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260311200940_AddTraitFilterModeToGameTest', '10.0.5');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260311205937_AddGameTestRollbackAndCharacterDeathTime') THEN
    ALTER TABLE "Characters" ADD "DiedAtUtc" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260311205937_AddGameTestRollbackAndCharacterDeathTime') THEN
    CREATE TABLE "GameTestAppliedEffects" (
        "Id" uuid NOT NULL,
        "GameTestId" uuid NOT NULL,
        "CharacterId" uuid NOT NULL,
        "TargetKind" integer NOT NULL,
        "TargetDefinitionId" uuid NOT NULL,
        "PreviousValue" integer NOT NULL,
        "NewValue" integer NOT NULL,
        "PreviousIsAlive" boolean NOT NULL,
        "NewIsAlive" boolean NOT NULL,
        "PreviousDiedAtUtc" timestamp with time zone,
        "NewDiedAtUtc" timestamp with time zone,
        "AppliedAtUtc" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_GameTestAppliedEffects" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260311205937_AddGameTestRollbackAndCharacterDeathTime') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260311205937_AddGameTestRollbackAndCharacterDeathTime', '10.0.5');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260311220530_AddSessionPollV1') THEN
    CREATE TABLE "SessionPollOptions" (
        "Id" uuid NOT NULL,
        "SessionPollId" uuid NOT NULL,
        "Label" text NOT NULL,
        "DisplayOrder" integer NOT NULL,
        CONSTRAINT "PK_SessionPollOptions" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260311220530_AddSessionPollV1') THEN
    CREATE TABLE "SessionPolls" (
        "Id" uuid NOT NULL,
        "SessionId" uuid NOT NULL,
        "Question" text NOT NULL,
        "IsClosed" boolean NOT NULL,
        "CreatedAtUtc" timestamp with time zone NOT NULL,
        "ClosedAtUtc" timestamp with time zone,
        CONSTRAINT "PK_SessionPolls" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260311220530_AddSessionPollV1') THEN
    CREATE TABLE "SessionPollVotes" (
        "Id" uuid NOT NULL,
        "SessionPollId" uuid NOT NULL,
        "SessionPollOptionId" uuid NOT NULL,
        "PlayerSessionId" uuid NOT NULL,
        "CharacterId" uuid NOT NULL,
        "VotedAtUtc" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_SessionPollVotes" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260311220530_AddSessionPollV1') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260311220530_AddSessionPollV1', '10.0.5');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260312122900_AddPollV2WeightedVotesAndConsequences') THEN
    ALTER TABLE "SessionPollVotes" ADD "VoteWeight" numeric NOT NULL DEFAULT 0.0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260312122900_AddPollV2WeightedVotesAndConsequences') THEN
    ALTER TABLE "SessionPolls" ADD "ConsequencesApplied" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260312122900_AddPollV2WeightedVotesAndConsequences') THEN
    CREATE TABLE "SessionPollAppliedEffects" (
        "Id" uuid NOT NULL,
        "SessionPollId" uuid NOT NULL,
        "CharacterId" uuid NOT NULL,
        "SessionPollVoteId" uuid NOT NULL,
        "SessionPollOptionId" uuid NOT NULL,
        "TargetKind" integer NOT NULL,
        "TargetDefinitionId" uuid NOT NULL,
        "PreviousValue" integer NOT NULL,
        "NewValue" integer NOT NULL,
        "PreviousIsAlive" boolean NOT NULL,
        "NewIsAlive" boolean NOT NULL,
        "PreviousDiedAtUtc" timestamp with time zone,
        "NewDiedAtUtc" timestamp with time zone,
        "AppliedAtUtc" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_SessionPollAppliedEffects" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260312122900_AddPollV2WeightedVotesAndConsequences') THEN
    CREATE TABLE "SessionPollOptionConsequences" (
        "Id" uuid NOT NULL,
        "SessionPollOptionId" uuid NOT NULL,
        "TargetKind" integer NOT NULL,
        "TargetDefinitionId" uuid NOT NULL,
        "TargetNameSnapshot" text NOT NULL,
        "ModifierMode" integer NOT NULL,
        "Value" integer NOT NULL,
        CONSTRAINT "PK_SessionPollOptionConsequences" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260312122900_AddPollV2WeightedVotesAndConsequences') THEN
    CREATE TABLE "SessionPollWeightRules" (
        "Id" uuid NOT NULL,
        "SessionPollId" uuid NOT NULL,
        "TraitDefinitionId" uuid NOT NULL,
        "TraitOptionId" uuid NOT NULL,
        "WeightBonus" numeric NOT NULL,
        CONSTRAINT "PK_SessionPollWeightRules" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260312122900_AddPollV2WeightedVotesAndConsequences') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260312122900_AddPollV2WeightedVotesAndConsequences', '10.0.5');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260312194143_AddMaxPlayersPerSessionAndFixPlayerSessionRoles') THEN
    ALTER TABLE "UserAccounts" ADD "MaxPlayersPerSession" integer NOT NULL DEFAULT 0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260312194143_AddMaxPlayersPerSessionAndFixPlayerSessionRoles') THEN
    ALTER TABLE "UserAccounts" ADD CONSTRAINT "CK_UserAccounts_MaxPlayersPerSession_Range" CHECK ("MaxPlayersPerSession" >= 0 AND "MaxPlayersPerSession" <= 5000);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260312194143_AddMaxPlayersPerSessionAndFixPlayerSessionRoles') THEN

    UPDATE "PlayerSessions" AS ps
    SET "IsGameMaster" = CASE
        WHEN s."GameMasterUserAccountId" = ps."UserAccountId" THEN TRUE
        ELSE FALSE
    END
    FROM "Sessions" AS s
    WHERE s."Id" = ps."SessionId";

    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260312194143_AddMaxPlayersPerSessionAndFixPlayerSessionRoles') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260312194143_AddMaxPlayersPerSessionAndFixPlayerSessionRoles', '10.0.5');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260312211230_AddGameSystemSnapshots') THEN
    CREATE TABLE "GameSystemSnapshots" (
        "Id" uuid NOT NULL,
        "GameSystemId" uuid NOT NULL,
        "OwnerUserAccountId" uuid NOT NULL,
        "SnapshotJson" text NOT NULL,
        "CreatedAtUtc" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_GameSystemSnapshots" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_GameSystemSnapshots_GameSystems_GameSystemId" FOREIGN KEY ("GameSystemId") REFERENCES "GameSystems" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260312211230_AddGameSystemSnapshots') THEN
    CREATE INDEX "IX_GameSystemSnapshots_GameSystemId_CreatedAtUtc" ON "GameSystemSnapshots" ("GameSystemId", "CreatedAtUtc");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260312211230_AddGameSystemSnapshots') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260312211230_AddGameSystemSnapshots', '10.0.5');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260313103832_AddDiceDefaultsAndComputedFoundations') THEN
    ALTER TABLE "AttributeDefinitions" ADD "DefaultValueMode" int NOT NULL DEFAULT 0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260313103832_AddDiceDefaultsAndComputedFoundations') THEN
    ALTER TABLE "AttributeDefinitions" ADD "DefaultValueFlatBonus" int NOT NULL DEFAULT 0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260313103832_AddDiceDefaultsAndComputedFoundations') THEN
    ALTER TABLE "AttributeDefinitions" ADD "DefaultValueDiceCount" int NOT NULL DEFAULT 0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260313103832_AddDiceDefaultsAndComputedFoundations') THEN
    ALTER TABLE "AttributeDefinitions" ADD "DefaultValueDiceSides" int NOT NULL DEFAULT 0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260313103832_AddDiceDefaultsAndComputedFoundations') THEN
    CREATE TABLE "DerivedStatDefinitions" (
        "Id" uuid NOT NULL,
        "GameSystemId" uuid NOT NULL,
        "Name" character varying(120) NOT NULL,
        "RoundMode" integer NOT NULL,
        "DisplayOrder" integer NOT NULL,
        CONSTRAINT "PK_DerivedStatDefinitions" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_DerivedStatDefinitions_GameSystems_GameSystemId" FOREIGN KEY ("GameSystemId") REFERENCES "GameSystems" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260313103832_AddDiceDefaultsAndComputedFoundations') THEN
    CREATE TABLE "DerivedStatComponents" (
        "Id" uuid NOT NULL,
        "DerivedStatDefinitionId" uuid NOT NULL,
        "AttributeDefinitionId" uuid NOT NULL,
        "Weight" integer NOT NULL,
        CONSTRAINT "PK_DerivedStatComponents" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_DerivedStatComponents_DerivedStatDefinitions_DerivedStatDefinitionId" FOREIGN KEY ("DerivedStatDefinitionId") REFERENCES "DerivedStatDefinitions" ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_DerivedStatComponents_AttributeDefinitions_AttributeDefinitionId" FOREIGN KEY ("AttributeDefinitionId") REFERENCES "AttributeDefinitions" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260313103832_AddDiceDefaultsAndComputedFoundations') THEN
    CREATE TABLE "MetricDefinitions" (
        "Id" uuid NOT NULL,
        "GameSystemId" uuid NOT NULL,
        "Name" character varying(120) NOT NULL,
        "RoundMode" integer NOT NULL,
        "DisplayOrder" integer NOT NULL,
        CONSTRAINT "PK_MetricDefinitions" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_MetricDefinitions_GameSystems_GameSystemId" FOREIGN KEY ("GameSystemId") REFERENCES "GameSystems" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260313103832_AddDiceDefaultsAndComputedFoundations') THEN
    CREATE TABLE "MetricComponents" (
        "Id" uuid NOT NULL,
        "MetricDefinitionId" uuid NOT NULL,
        "AttributeDefinitionId" uuid NOT NULL,
        "Weight" integer NOT NULL,
        CONSTRAINT "PK_MetricComponents" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_MetricComponents_MetricDefinitions_MetricDefinitionId" FOREIGN KEY ("MetricDefinitionId") REFERENCES "MetricDefinitions" ("Id") ON DELETE CASCADE,
        CONSTRAINT "FK_MetricComponents_AttributeDefinitions_AttributeDefinitionId" FOREIGN KEY ("AttributeDefinitionId") REFERENCES "AttributeDefinitions" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260313103832_AddDiceDefaultsAndComputedFoundations') THEN
    CREATE INDEX "IX_DerivedStatDefinitions_GameSystemId" ON "DerivedStatDefinitions" ("GameSystemId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260313103832_AddDiceDefaultsAndComputedFoundations') THEN
    CREATE INDEX "IX_DerivedStatComponents_DerivedStatDefinitionId" ON "DerivedStatComponents" ("DerivedStatDefinitionId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260313103832_AddDiceDefaultsAndComputedFoundations') THEN
    CREATE INDEX "IX_DerivedStatComponents_AttributeDefinitionId" ON "DerivedStatComponents" ("AttributeDefinitionId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260313103832_AddDiceDefaultsAndComputedFoundations') THEN
    CREATE INDEX "IX_MetricDefinitions_GameSystemId" ON "MetricDefinitions" ("GameSystemId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260313103832_AddDiceDefaultsAndComputedFoundations') THEN
    CREATE INDEX "IX_MetricComponents_MetricDefinitionId" ON "MetricComponents" ("MetricDefinitionId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260313103832_AddDiceDefaultsAndComputedFoundations') THEN
    CREATE INDEX "IX_MetricComponents_AttributeDefinitionId" ON "MetricComponents" ("AttributeDefinitionId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260313103832_AddDiceDefaultsAndComputedFoundations') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260313103832_AddDiceDefaultsAndComputedFoundations', '10.0.5');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260313105048_FixDerivedStatDefinitionColumns') THEN
    ALTER TABLE "DerivedStatDefinitions" ADD "MinValue" integer NOT NULL DEFAULT 0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260313105048_FixDerivedStatDefinitionColumns') THEN
    ALTER TABLE "DerivedStatDefinitions" ADD "MaxValue" integer NOT NULL DEFAULT 100;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260313105048_FixDerivedStatDefinitionColumns') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260313105048_FixDerivedStatDefinitionColumns', '10.0.5');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260313140344_AddMetricsAndAutomaticPollVoteWeight') THEN
    ALTER TABLE "MetricDefinitions" ADD "BaseValue" integer NOT NULL DEFAULT 0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260313140344_AddMetricsAndAutomaticPollVoteWeight') THEN
    ALTER TABLE "MetricDefinitions" ADD "MinValue" integer NOT NULL DEFAULT 0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260313140344_AddMetricsAndAutomaticPollVoteWeight') THEN
    ALTER TABLE "MetricDefinitions" ADD "MaxValue" integer NOT NULL DEFAULT 100;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260313140344_AddMetricsAndAutomaticPollVoteWeight') THEN
    ALTER TABLE "SessionPolls" ADD "VoteWeightMode" integer NOT NULL DEFAULT 0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260313140344_AddMetricsAndAutomaticPollVoteWeight') THEN
    ALTER TABLE "SessionPolls" ADD "MetricDefinitionId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260313140344_AddMetricsAndAutomaticPollVoteWeight') THEN
    ALTER TABLE "SessionPolls" ADD "MetricNameSnapshot" text NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260313140344_AddMetricsAndAutomaticPollVoteWeight') THEN
    CREATE INDEX "IX_SessionPolls_MetricDefinitionId" ON "SessionPolls" ("MetricDefinitionId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260313140344_AddMetricsAndAutomaticPollVoteWeight') THEN
    ALTER TABLE "SessionPolls" ADD CONSTRAINT "FK_SessionPolls_MetricDefinitions_MetricDefinitionId" FOREIGN KEY ("MetricDefinitionId") REFERENCES "MetricDefinitions" ("Id") ON DELETE SET NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260313140344_AddMetricsAndAutomaticPollVoteWeight') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260313140344_AddMetricsAndAutomaticPollVoteWeight', '10.0.5');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260313144239_AddTalentsItemsAndModifiers') THEN
    CREATE TABLE "TalentDefinitions" (
        "Id" uuid NOT NULL,
        "GameSystemId" uuid NOT NULL,
        "Name" text NOT NULL,
        "Description" text,
        "DisplayOrder" integer NOT NULL,
        CONSTRAINT "PK_TalentDefinitions" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260313144239_AddTalentsItemsAndModifiers') THEN
    CREATE TABLE "ItemDefinitions" (
        "Id" uuid NOT NULL,
        "GameSystemId" uuid NOT NULL,
        "Name" text NOT NULL,
        "Description" text,
        "DisplayOrder" integer NOT NULL,
        CONSTRAINT "PK_ItemDefinitions" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260313144239_AddTalentsItemsAndModifiers') THEN
    CREATE TABLE "TalentModifierDefinitions" (
        "Id" uuid NOT NULL,
        "TalentDefinitionId" uuid NOT NULL,
        "TargetType" integer NOT NULL,
        "TargetId" uuid NOT NULL,
        "AddValue" integer NOT NULL,
        CONSTRAINT "PK_TalentModifierDefinitions" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260313144239_AddTalentsItemsAndModifiers') THEN
    CREATE TABLE "ItemModifierDefinitions" (
        "Id" uuid NOT NULL,
        "ItemDefinitionId" uuid NOT NULL,
        "TargetType" integer NOT NULL,
        "TargetId" uuid NOT NULL,
        "AddValue" integer NOT NULL,
        CONSTRAINT "PK_ItemModifierDefinitions" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260313144239_AddTalentsItemsAndModifiers') THEN
    CREATE TABLE "CharacterTalents" (
        "Id" uuid NOT NULL,
        "CharacterId" uuid NOT NULL,
        "TalentDefinitionId" uuid NOT NULL,
        CONSTRAINT "PK_CharacterTalents" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260313144239_AddTalentsItemsAndModifiers') THEN
    CREATE TABLE "CharacterItems" (
        "Id" uuid NOT NULL,
        "CharacterId" uuid NOT NULL,
        "ItemDefinitionId" uuid NOT NULL,
        CONSTRAINT "PK_CharacterItems" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260313144239_AddTalentsItemsAndModifiers') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260313144239_AddTalentsItemsAndModifiers', '10.0.5');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260313145652_AddChoiceOptionModifiers') THEN
    CREATE TABLE "ChoiceOptionModifierDefinitions" (
        "Id" uuid NOT NULL,
        "TraitOptionId" uuid NOT NULL,
        "TargetType" integer NOT NULL,
        "TargetId" uuid NOT NULL,
        "AddValue" integer NOT NULL,
        CONSTRAINT "PK_ChoiceOptionModifierDefinitions" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260313145652_AddChoiceOptionModifiers') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260313145652_AddChoiceOptionModifiers', '10.0.5');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260314145334_AddCharacterModifiersFoundation') THEN
    CREATE TABLE "CharacterModifiers" (
        "Id" uuid NOT NULL,
        "CharacterId" uuid NOT NULL,
        "TargetType" integer NOT NULL,
        "TargetId" uuid NOT NULL,
        "AddValue" integer NOT NULL,
        "SourceType" integer NOT NULL,
        "SourceId" uuid NOT NULL,
        "SourceNameSnapshot" text NOT NULL,
        "CreatedAtUtc" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_CharacterModifiers" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260314145334_AddCharacterModifiersFoundation') THEN
    CREATE INDEX "IX_CharacterModifiers_CharacterId" ON "CharacterModifiers" ("CharacterId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260314145334_AddCharacterModifiersFoundation') THEN
    CREATE INDEX "IX_CharacterModifiers_CharacterId_TargetType_TargetId" ON "CharacterModifiers" ("CharacterId", "TargetType", "TargetId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260314145334_AddCharacterModifiersFoundation') THEN
    CREATE INDEX "IX_CharacterModifiers_SourceType_SourceId" ON "CharacterModifiers" ("SourceType", "SourceId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260314145334_AddCharacterModifiersFoundation') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260314145334_AddCharacterModifiersFoundation', '10.0.5');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260315134430_AddMassDistributionBatch') THEN
    CREATE TABLE "MassDistributionBatches" (
        "Id" uuid NOT NULL,
        "SessionId" uuid NOT NULL,
        "CreatedByUserAccountId" uuid NOT NULL,
        "Name" text NOT NULL,
        "TargetCharacterCount" integer NOT NULL,
        "CreatedAtUtc" timestamp with time zone NOT NULL,
        "FilterSnapshotJson" text NOT NULL,
        "EffectsSnapshotJson" text NOT NULL,
        CONSTRAINT "PK_MassDistributionBatches" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260315134430_AddMassDistributionBatch') THEN
    CREATE INDEX "IX_MassDistributionBatches_SessionId" ON "MassDistributionBatches" ("SessionId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260315134430_AddMassDistributionBatch') THEN
    CREATE INDEX "IX_MassDistributionBatches_SessionId_CreatedAtUtc" ON "MassDistributionBatches" ("SessionId", "CreatedAtUtc");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260315134430_AddMassDistributionBatch') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260315134430_AddMassDistributionBatch', '10.0.5');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260315141122_AddMassDistributionUndoFields') THEN
    ALTER TABLE "MassDistributionBatches" ADD "IsUndone" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260315141122_AddMassDistributionUndoFields') THEN
    ALTER TABLE "MassDistributionBatches" ADD "UndoneAtUtc" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260315141122_AddMassDistributionUndoFields') THEN
    ALTER TABLE "MassDistributionBatches" ADD "UndoSnapshotJson" text NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260315141122_AddMassDistributionUndoFields') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260315141122_AddMassDistributionUndoFields', '10.0.5');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260315145304_AddGameTestConsequenceOperationType') THEN
    ALTER TABLE "GameTestConsequences" ADD "OperationType" integer NOT NULL DEFAULT 1;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260315145304_AddGameTestConsequenceOperationType') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260315145304_AddGameTestConsequenceOperationType', '10.0.5');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260315192423_ExtendGameTestAppliedEffectForCommonRollback') THEN
    ALTER TABLE "GameTestAppliedEffects" ADD "OperationType" integer NOT NULL DEFAULT 1;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260315192423_ExtendGameTestAppliedEffectForCommonRollback') THEN
    ALTER TABLE "GameTestAppliedEffects" ADD "PreviousHasTargetLink" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260315192423_ExtendGameTestAppliedEffectForCommonRollback') THEN
    ALTER TABLE "GameTestAppliedEffects" ADD "NewHasTargetLink" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260315192423_ExtendGameTestAppliedEffectForCommonRollback') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260315192423_ExtendGameTestAppliedEffectForCommonRollback', '10.0.5');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260315195437_AddPollOptionConsequenceOperationType') THEN
    ALTER TABLE "SessionPollOptionConsequences" ADD "OperationType" integer NOT NULL DEFAULT 1;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260315195437_AddPollOptionConsequenceOperationType') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260315195437_AddPollOptionConsequenceOperationType', '10.0.5');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260315230819_ExtendSessionPollAppliedEffectForCommonRollback') THEN
    ALTER TABLE "SessionPollAppliedEffects" ADD "OperationType" integer NOT NULL DEFAULT 1;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260315230819_ExtendSessionPollAppliedEffectForCommonRollback') THEN
    ALTER TABLE "SessionPollAppliedEffects" ADD "PreviousHasTargetLink" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260315230819_ExtendSessionPollAppliedEffectForCommonRollback') THEN
    ALTER TABLE "SessionPollAppliedEffects" ADD "NewHasTargetLink" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260315230819_ExtendSessionPollAppliedEffectForCommonRollback') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260315230819_ExtendSessionPollAppliedEffectForCommonRollback', '10.0.5');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260316090559_MetricsFormula6A') THEN
    CREATE TABLE "MetricFormulaSteps" (
        "Id" uuid NOT NULL,
        "MetricDefinitionId" uuid NOT NULL,
        "Order" integer NOT NULL,
        "OperationType" integer NOT NULL,
        "SourceType" integer NOT NULL,
        "SourceId" uuid,
        "ConstantValue" numeric(18,4) NOT NULL,
        CONSTRAINT "PK_MetricFormulaSteps" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260316090559_MetricsFormula6A') THEN
    CREATE INDEX "IX_MetricFormulaSteps_MetricDefinitionId_Order" ON "MetricFormulaSteps" ("MetricDefinitionId", "Order");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260316090559_MetricsFormula6A') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260316090559_MetricsFormula6A', '10.0.5');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260316110714_ModifierMetric6C') THEN
    ALTER TABLE "TalentModifierDefinitions" ADD "SourceMetricId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260316110714_ModifierMetric6C') THEN
    ALTER TABLE "TalentModifierDefinitions" ADD "ValueMode" integer NOT NULL DEFAULT 0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260316110714_ModifierMetric6C') THEN
    ALTER TABLE "ItemModifierDefinitions" ADD "SourceMetricId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260316110714_ModifierMetric6C') THEN
    ALTER TABLE "ItemModifierDefinitions" ADD "ValueMode" integer NOT NULL DEFAULT 0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260316110714_ModifierMetric6C') THEN
    ALTER TABLE "ChoiceOptionModifierDefinitions" ADD "SourceMetricId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260316110714_ModifierMetric6C') THEN
    ALTER TABLE "ChoiceOptionModifierDefinitions" ADD "ValueMode" integer NOT NULL DEFAULT 0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260316110714_ModifierMetric6C') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260316110714_ModifierMetric6C', '10.0.5');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260316121312_DynamicEffectMetrics6D') THEN
    ALTER TABLE "SessionPollOptionConsequences" ADD "SourceMetricId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260316121312_DynamicEffectMetrics6D') THEN
    ALTER TABLE "SessionPollOptionConsequences" ADD "ValueMode" integer NOT NULL DEFAULT 0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260316121312_DynamicEffectMetrics6D') THEN
    ALTER TABLE "GameTestConsequences" ADD "SourceMetricId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260316121312_DynamicEffectMetrics6D') THEN
    ALTER TABLE "GameTestConsequences" ADD "ValueMode" integer NOT NULL DEFAULT 0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260316121312_DynamicEffectMetrics6D') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260316121312_DynamicEffectMetrics6D', '10.0.5');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260316142916_GameTestsV2_7A') THEN
    ALTER TABLE "GameTests" RENAME COLUMN "AttributeNameSnapshot" TO "TargetNameSnapshot";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260316142916_GameTestsV2_7A') THEN
    ALTER TABLE "GameTests" RENAME COLUMN "AttributeDefinitionId" TO "TargetDefinitionId";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260316142916_GameTestsV2_7A') THEN
    ALTER TABLE "PlayerTestRolls" ADD "Outcome" integer NOT NULL DEFAULT 0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260316142916_GameTestsV2_7A') THEN
    ALTER TABLE "GameTests" ADD "CriticalFailureValueSnapshot" integer;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260316142916_GameTestsV2_7A') THEN
    ALTER TABLE "GameTests" ADD "CriticalSuccessValueSnapshot" integer;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260316142916_GameTestsV2_7A') THEN
    ALTER TABLE "GameTests" ADD "TargetKind" integer NOT NULL DEFAULT 0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260316142916_GameTestsV2_7A') THEN
    ALTER TABLE "GameTests" ADD "UseSystemDefaultDice" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260316142916_GameTestsV2_7A') THEN
    ALTER TABLE "GameSystems" ADD "CriticalFailureValue" integer;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260316142916_GameTestsV2_7A') THEN
    ALTER TABLE "GameSystems" ADD "CriticalSuccessValue" integer;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260316142916_GameTestsV2_7A') THEN
    ALTER TABLE "GameSystems" ADD "DefaultTestDiceCount" integer NOT NULL DEFAULT 0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260316142916_GameTestsV2_7A') THEN
    ALTER TABLE "GameSystems" ADD "DefaultTestDiceSides" integer NOT NULL DEFAULT 0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260316142916_GameTestsV2_7A') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260316142916_GameTestsV2_7A', '10.0.5');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260316231424_TraitGrantRevoke_7B') THEN
    ALTER TABLE "ChoiceOptionModifierDefinitions" RENAME COLUMN "TraitOptionId" TO "ChoiceOptionDefinitionId";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260316231424_TraitGrantRevoke_7B') THEN
    ALTER TABLE "ChoiceOptionModifierDefinitions" RENAME COLUMN "AddValue" TO "Value";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260316231424_TraitGrantRevoke_7B') THEN
    ALTER TABLE "ChoiceOptionModifierDefinitions" ADD "OperationType" integer NOT NULL DEFAULT 0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260316231424_TraitGrantRevoke_7B') THEN
    ALTER TABLE "ChoiceOptionModifierDefinitions" ADD "TargetNameSnapshot" text NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260316231424_TraitGrantRevoke_7B') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260316231424_TraitGrantRevoke_7B', '10.0.5');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317095645_SessionSpecialRoles_7C') THEN
    ALTER TABLE "PlayerSessions" ADD "SpecialRole" integer NOT NULL DEFAULT 0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317095645_SessionSpecialRoles_7C') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260317095645_SessionSpecialRoles_7C', '10.0.5');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317124026_Npcs_7D') THEN
    ALTER TABLE "Characters" ADD "IsNpc" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317124026_Npcs_7D') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260317124026_Npcs_7D', '10.0.5');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317144520_RandomDraws_7E') THEN
    CREATE TABLE "SessionRandomDraws" (
        "Id" uuid NOT NULL,
        "SessionId" uuid NOT NULL,
        "CreatedByUserAccountId" uuid NOT NULL,
        "Name" text NOT NULL,
        "RequestedCount" integer NOT NULL,
        "ResultSnapshotJson" text NOT NULL,
        "CreatedAtUtc" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_SessionRandomDraws" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317144520_RandomDraws_7E') THEN
    CREATE INDEX "IX_SessionRandomDraws_SessionId" ON "SessionRandomDraws" ("SessionId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317144520_RandomDraws_7E') THEN
    CREATE INDEX "IX_SessionRandomDraws_SessionId_CreatedAtUtc" ON "SessionRandomDraws" ("SessionId", "CreatedAtUtc");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260317144520_RandomDraws_7E') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260317144520_RandomDraws_7E', '10.0.5');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260318110944_SessionGauges_7F') THEN
    CREATE TABLE "SessionGauges" (
        "Id" uuid NOT NULL,
        "SessionId" uuid NOT NULL,
        "Name" text NOT NULL,
        "MinValue" integer NOT NULL,
        "MaxValue" integer NOT NULL,
        "CurrentValue" integer NOT NULL,
        "CreatedAtUtc" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_SessionGauges" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260318110944_SessionGauges_7F') THEN
    CREATE INDEX "IX_SessionGauges_SessionId" ON "SessionGauges" ("SessionId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260318110944_SessionGauges_7F') THEN
    CREATE INDEX "IX_SessionGauges_SessionId_Name" ON "SessionGauges" ("SessionId", "Name");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260318110944_SessionGauges_7F') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260318110944_SessionGauges_7F', '10.0.5');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260322210137_AddTraitCreationFlags') THEN
    ALTER TABLE "TraitOptions" ADD "IsLockedForCharacterCreation" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260322210137_AddTraitCreationFlags') THEN
    ALTER TABLE "TraitDefinitions" ADD "IsRandomSelectionGroup" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260322210137_AddTraitCreationFlags') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260322210137_AddTraitCreationFlags', '10.0.5');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260323132942_AddGameSystemIsGeneric') THEN
    ALTER TABLE "GameSystems" ADD "IsGeneric" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260323132942_AddGameSystemIsGeneric') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260323132942_AddGameSystemIsGeneric', '10.0.5');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260323213956_AddSessionPollEligibleCharacters') THEN
    CREATE TABLE "SessionPollEligibleCharacters" (
        "Id" uuid NOT NULL,
        "SessionPollId" uuid NOT NULL,
        "CharacterId" uuid NOT NULL,
        CONSTRAINT "PK_SessionPollEligibleCharacters" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260323213956_AddSessionPollEligibleCharacters') THEN
    CREATE INDEX "IX_SessionPollEligibleCharacters_CharacterId" ON "SessionPollEligibleCharacters" ("CharacterId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260323213956_AddSessionPollEligibleCharacters') THEN
    CREATE INDEX "IX_SessionPollEligibleCharacters_SessionPollId" ON "SessionPollEligibleCharacters" ("SessionPollId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260323213956_AddSessionPollEligibleCharacters') THEN
    CREATE UNIQUE INDEX "IX_SessionPollEligibleCharacters_SessionPollId_CharacterId" ON "SessionPollEligibleCharacters" ("SessionPollId", "CharacterId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260323213956_AddSessionPollEligibleCharacters') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260323213956_AddSessionPollEligibleCharacters', '10.0.5');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260324093224_AddGameTestGlobalThresholds') THEN
    ALTER TABLE "GameTests" ADD "GlobalConsequencesApplied" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260324093224_AddGameTestGlobalThresholds') THEN
    ALTER TABLE "GameTests" ADD "GlobalSuccessThreshold1Percent" integer;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260324093224_AddGameTestGlobalThresholds') THEN
    ALTER TABLE "GameTests" ADD "GlobalSuccessThreshold2Percent" integer;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260324093224_AddGameTestGlobalThresholds') THEN
    ALTER TABLE "GameTests" ADD "GlobalSuccessThreshold3Percent" integer;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260324093224_AddGameTestGlobalThresholds') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260324093224_AddGameTestGlobalThresholds', '10.0.5');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260326214852_U1_UserAccount_Foundations') THEN
    ALTER TABLE "UserAccounts" ALTER COLUMN "Language" SET DEFAULT 'fr';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260326214852_U1_UserAccount_Foundations') THEN
    ALTER TABLE "UserAccounts" ADD "Email" text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260326214852_U1_UserAccount_Foundations') THEN
    ALTER TABLE "UserAccounts" ADD "IsEmailVerified" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260326214852_U1_UserAccount_Foundations') THEN
    ALTER TABLE "UserAccounts" ADD "LastSensitiveChangeAtUtc" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260326214852_U1_UserAccount_Foundations') THEN
    ALTER TABLE "UserAccounts" ADD "LastSensitiveChangeType" text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260326214852_U1_UserAccount_Foundations') THEN
    ALTER TABLE "UserAccounts" ADD "WantsToBeGameMaster" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260326214852_U1_UserAccount_Foundations') THEN
    CREATE UNIQUE INDEX "IX_UserAccounts_Email" ON "UserAccounts" ("Email");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260326214852_U1_UserAccount_Foundations') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260326214852_U1_UserAccount_Foundations', '10.0.5');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260326224617_U2_LocalAccountSecurity') THEN
    CREATE TABLE "AccountSecurityTokens" (
        "Id" uuid NOT NULL,
        "UserAccountId" uuid NOT NULL,
        "Purpose" text NOT NULL,
        "TokenHash" text NOT NULL,
        "EmailSnapshot" text,
        "CreatedAtUtc" timestamp with time zone NOT NULL,
        "ExpiresAtUtc" timestamp with time zone NOT NULL,
        "ConsumedAtUtc" timestamp with time zone,
        CONSTRAINT "PK_AccountSecurityTokens" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260326224617_U2_LocalAccountSecurity') THEN
    CREATE UNIQUE INDEX "IX_AccountSecurityTokens_TokenHash" ON "AccountSecurityTokens" ("TokenHash");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260326224617_U2_LocalAccountSecurity') THEN
    CREATE INDEX "IX_AccountSecurityTokens_UserAccountId" ON "AccountSecurityTokens" ("UserAccountId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260326224617_U2_LocalAccountSecurity') THEN
    CREATE INDEX "IX_AccountSecurityTokens_UserAccountId_Purpose" ON "AccountSecurityTokens" ("UserAccountId", "Purpose");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260326224617_U2_LocalAccountSecurity') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260326224617_U2_LocalAccountSecurity', '10.0.5');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260327141200_U3_TwitchAuth') THEN
    ALTER TABLE "UserAccounts" ADD "TwitchDisplayName" text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260327141200_U3_TwitchAuth') THEN
    ALTER TABLE "UserAccounts" ADD "TwitchUserId" text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260327141200_U3_TwitchAuth') THEN
    CREATE TABLE "TwitchPendingAuthSessions" (
        "Id" uuid NOT NULL,
        "PublicToken" text NOT NULL,
        "OAuthState" text NOT NULL,
        "FlowType" text NOT NULL,
        "CurrentUserAccountId" uuid,
        "TwitchUserId" text,
        "TwitchLogin" text,
        "TwitchDisplayName" text,
        "TwitchEmail" text,
        "MatchedUserAccountId" uuid,
        "Language" text NOT NULL,
        "CreatedAtUtc" timestamp with time zone NOT NULL,
        "ExpiresAtUtc" timestamp with time zone NOT NULL,
        "ConsumedAtUtc" timestamp with time zone,
        CONSTRAINT "PK_TwitchPendingAuthSessions" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260327141200_U3_TwitchAuth') THEN
    CREATE INDEX "IX_TwitchPendingAuthSessions_CurrentUserAccountId" ON "TwitchPendingAuthSessions" ("CurrentUserAccountId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260327141200_U3_TwitchAuth') THEN
    CREATE INDEX "IX_TwitchPendingAuthSessions_ExpiresAtUtc" ON "TwitchPendingAuthSessions" ("ExpiresAtUtc");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260327141200_U3_TwitchAuth') THEN
    CREATE INDEX "IX_TwitchPendingAuthSessions_MatchedUserAccountId" ON "TwitchPendingAuthSessions" ("MatchedUserAccountId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260327141200_U3_TwitchAuth') THEN
    CREATE UNIQUE INDEX "IX_TwitchPendingAuthSessions_OAuthState" ON "TwitchPendingAuthSessions" ("OAuthState");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260327141200_U3_TwitchAuth') THEN
    CREATE UNIQUE INDEX "IX_TwitchPendingAuthSessions_PublicToken" ON "TwitchPendingAuthSessions" ("PublicToken");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260327141200_U3_TwitchAuth') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260327141200_U3_TwitchAuth', '10.0.5');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260329211948_U4_GameMasterAndSubscriptions') THEN
    CREATE TABLE "SubscriptionPlans" (
        "Id" uuid NOT NULL,
        "Code" text NOT NULL,
        "Name" text NOT NULL,
        "MonthlyPriceTtc" numeric(18,2) NOT NULL,
        "MaxPlayersPerSession" integer NOT NULL,
        "DisplayOrder" integer NOT NULL,
        "IsActive" boolean NOT NULL,
        CONSTRAINT "PK_SubscriptionPlans" PRIMARY KEY ("Id"),
        CONSTRAINT "CK_SubscriptionPlans_MaxPlayersPerSession_Range" CHECK ("MaxPlayersPerSession" >= 0 AND "MaxPlayersPerSession" <= 5000)
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260329211948_U4_GameMasterAndSubscriptions') THEN
    CREATE TABLE "UserSubscriptions" (
        "Id" uuid NOT NULL,
        "UserAccountId" uuid NOT NULL,
        "CurrentPlanId" uuid NOT NULL,
        "PendingPlanId" uuid,
        "StartedAtUtc" timestamp with time zone NOT NULL,
        "NextRenewalAtUtc" timestamp with time zone NOT NULL,
        "CancelAtRenewal" boolean NOT NULL,
        "CreatedAtUtc" timestamp with time zone NOT NULL,
        "UpdatedAtUtc" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_UserSubscriptions" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260329211948_U4_GameMasterAndSubscriptions') THEN
    INSERT INTO "SubscriptionPlans" ("Id", "Code", "DisplayOrder", "IsActive", "MaxPlayersPerSession", "MonthlyPriceTtc", "Name")
    VALUES ('0c1c29b4-c9b9-4e3b-a4aa-4106d7dd6a10', 'aventurier', 1, TRUE, 15, 0.0, 'Aventurier');
    INSERT INTO "SubscriptionPlans" ("Id", "Code", "DisplayOrder", "IsActive", "MaxPlayersPerSession", "MonthlyPriceTtc", "Name")
    VALUES ('2f3df112-4d0d-4ef7-8b53-0c6fcb88b701', 'heros', 2, TRUE, 75, 5.0, 'Héros');
    INSERT INTO "SubscriptionPlans" ("Id", "Code", "DisplayOrder", "IsActive", "MaxPlayersPerSession", "MonthlyPriceTtc", "Name")
    VALUES ('5fa71ab1-3f0c-401d-8c90-6b76a2d2c703', 'legende', 4, TRUE, 500, 20.0, 'Légende');
    INSERT INTO "SubscriptionPlans" ("Id", "Code", "DisplayOrder", "IsActive", "MaxPlayersPerSession", "MonthlyPriceTtc", "Name")
    VALUES ('8c48c20d-5f5d-4b5a-b997-d863cf7be702', 'champion', 3, TRUE, 200, 10.0, 'Champion');
    INSERT INTO "SubscriptionPlans" ("Id", "Code", "DisplayOrder", "IsActive", "MaxPlayersPerSession", "MonthlyPriceTtc", "Name")
    VALUES ('8d9d4e4a-9d2b-4204-8c16-2f7ab0cb2f05', 'divin', 6, TRUE, 5000, 100.0, 'Divin');
    INSERT INTO "SubscriptionPlans" ("Id", "Code", "DisplayOrder", "IsActive", "MaxPlayersPerSession", "MonthlyPriceTtc", "Name")
    VALUES ('c7c1d657-7fd4-42bd-b4f4-7047a6d43704', 'mythique', 5, TRUE, 1500, 50.0, 'Mythique');
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260329211948_U4_GameMasterAndSubscriptions') THEN
    CREATE UNIQUE INDEX "IX_SubscriptionPlans_Code" ON "SubscriptionPlans" ("Code");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260329211948_U4_GameMasterAndSubscriptions') THEN
    CREATE INDEX "IX_UserSubscriptions_CurrentPlanId" ON "UserSubscriptions" ("CurrentPlanId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260329211948_U4_GameMasterAndSubscriptions') THEN
    CREATE INDEX "IX_UserSubscriptions_PendingPlanId" ON "UserSubscriptions" ("PendingPlanId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260329211948_U4_GameMasterAndSubscriptions') THEN
    CREATE UNIQUE INDEX "IX_UserSubscriptions_UserAccountId" ON "UserSubscriptions" ("UserAccountId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260329211948_U4_GameMasterAndSubscriptions') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260329211948_U4_GameMasterAndSubscriptions', '10.0.5');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260331082159_L6_SessionItems') THEN
    ALTER TABLE "ItemDefinitions" ALTER COLUMN "GameSystemId" DROP NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260331082159_L6_SessionItems') THEN
    ALTER TABLE "ItemDefinitions" ADD "SessionId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260331082159_L6_SessionItems') THEN
    CREATE INDEX "IX_ItemDefinitions_GameSystemId" ON "ItemDefinitions" ("GameSystemId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260331082159_L6_SessionItems') THEN
    CREATE INDEX "IX_ItemDefinitions_GameSystemId_DisplayOrder" ON "ItemDefinitions" ("GameSystemId", "DisplayOrder");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260331082159_L6_SessionItems') THEN
    CREATE INDEX "IX_ItemDefinitions_SessionId" ON "ItemDefinitions" ("SessionId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260331082159_L6_SessionItems') THEN
    CREATE INDEX "IX_ItemDefinitions_SessionId_DisplayOrder" ON "ItemDefinitions" ("SessionId", "DisplayOrder");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260331082159_L6_SessionItems') THEN
    ALTER TABLE "ItemDefinitions" ADD CONSTRAINT "CK_ItemDefinitions_Scope" CHECK (("GameSystemId" IS NOT NULL AND "SessionId" IS NULL) OR ("GameSystemId" IS NULL AND "SessionId" IS NOT NULL));
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260331082159_L6_SessionItems') THEN
    CREATE INDEX "IX_CharacterItems_CharacterId" ON "CharacterItems" ("CharacterId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260331082159_L6_SessionItems') THEN
    CREATE UNIQUE INDEX "IX_CharacterItems_CharacterId_ItemDefinitionId" ON "CharacterItems" ("CharacterId", "ItemDefinitionId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260331082159_L6_SessionItems') THEN
    CREATE INDEX "IX_CharacterItems_ItemDefinitionId" ON "CharacterItems" ("ItemDefinitionId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260331082159_L6_SessionItems') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260331082159_L6_SessionItems', '10.0.5');
    END IF;
END $EF$;
COMMIT;

