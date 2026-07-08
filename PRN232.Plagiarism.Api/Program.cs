using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using PRN232.Plagiarism.Application;
using PRN232.Plagiarism.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add controllers support
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register clean architecture layers
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<PRN232.Plagiarism.Infrastructure.Persistence.PlagiarismDbContext>();
    
    try
    {
        dbContext.Database.ExecuteSqlRaw("CREATE SCHEMA IF NOT EXISTS plag;");
        
        // Cập nhật cấu trúc database: Thêm cột và bảng mới cho Plagiarism Pipeline nếu chưa có
        dbContext.Database.ExecuteSqlRaw(@"
            ALTER TABLE plag.""PlagiarismRecords"" ADD COLUMN IF NOT EXISTS ""ProjectGuids"" text[] NULL;
            ALTER TABLE plag.""PlagiarismRecords"" ADD COLUMN IF NOT EXISTS ""WorkspacePath"" character varying(500) NULL;

            UPDATE plag.""PlagiarismRecords"" SET ""ProjectGuids"" = ARRAY[]::text[] WHERE ""ProjectGuids"" IS NULL;
            UPDATE plag.""PlagiarismRecords"" SET ""WorkspacePath"" = '' WHERE ""WorkspacePath"" IS NULL;

            CREATE TABLE IF NOT EXISTS plag.""PlagiarismComparisons"" (
                ""Id"" uuid NOT NULL,
                ""ExamId"" uuid NOT NULL,
                ""SubmissionIdA"" uuid NOT NULL,
                ""SubmissionIdB"" uuid NOT NULL,
                ""StudentIdA"" character varying(50) NOT NULL,
                ""StudentIdB"" character varying(50) NOT NULL,
                ""SimilarityScore"" numeric(5,2) NOT NULL,
                ""GuidMatched"" boolean NOT NULL,
                ""ScannedAt"" timestamp with time zone NOT NULL,
                CONSTRAINT ""PK_PlagiarismComparisons"" PRIMARY KEY (""Id"")
            );
        ");
    }
    catch (System.Exception ex)
    {
        System.Console.WriteLine($"[DB Setup Warning - Safe to ignore if tables do not exist yet] {ex.Message}");
    }
    
    var databaseCreator = dbContext.Database.GetService<Microsoft.EntityFrameworkCore.Storage.IDatabaseCreator>() 
        as Microsoft.EntityFrameworkCore.Storage.RelationalDatabaseCreator;
    if (databaseCreator != null)
    {
        var tableExists = false;
        try
        {
            var connection = dbContext.Database.GetDbConnection();
            var wasOpen = connection.State == System.Data.ConnectionState.Open;
            if (!wasOpen) connection.Open();
            try
            {
                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = "SELECT EXISTS (SELECT FROM pg_tables WHERE schemaname = 'plag' AND tablename = 'PlagiarismRecords');";
                    tableExists = (bool)(cmd.ExecuteScalar() ?? false);
                }
            }
            finally
            {
                if (!wasOpen) connection.Close();
            }
        }
        catch (System.Exception ex)
        {
            System.Console.WriteLine($"[DB Check Error] {ex.Message}");
        }

        if (!tableExists)
        {
            try
            {
                databaseCreator.CreateTables();
                System.Console.WriteLine("Database tables created successfully in plag schema.");
            }
            catch (System.Exception ex)
            {
                System.Console.WriteLine("==================================================");
                System.Console.WriteLine("DB CREATION ERROR IN PLAGIARISM SERVICE:");
                System.Console.WriteLine(ex.ToString());
                System.Console.WriteLine("==================================================");
            }
        }
        else
        {
            System.Console.WriteLine("Database tables already exist in plag schema. Skipping creation.");
        }
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
