using Dock.Core.Worktrees;
using Xunit;

namespace Dock.Core.Tests.Worktrees;

public sealed class DatabaseConfigTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "dock-db-" + Guid.NewGuid().ToString("N"));

    public DatabaseConfigTests() => Directory.CreateDirectory(_root);

    [Fact]
    public void Parse_WhenHostKey_ThenPostgreSql()
    {
        var info = DatabaseInfoModel.Parse("Host=localhost;Port=5433;Database=app_local;Username=app;Password=secret", "a.json", "DefaultConnection");

        Assert.Equal((DatabaseProvider.PostgreSql, "app_local", "5433", "app"), (info!.Provider, info.SourceDb, info.Port, info.User));
    }

    [Fact]
    public void Parse_WhenInitialCatalog_ThenSqlServerWithIntegratedSecurity()
    {
        var info = DatabaseInfoModel.Parse(@"Data Source=.\SQLEXPRESS;Initial Catalog=Gestion;Integrated Security=SSPI", "Web.config", "DomainContext");

        Assert.Equal((DatabaseProvider.SqlServer, "Gestion", true), (info!.Provider, info.SourceDb, info.IntegratedSecurity));
    }

    [Fact]
    public void Parse_WhenServerAndUsername_ThenPostgreSql()
    {
        var info = DatabaseInfoModel.Parse("Server=db;Database=app;Username=app", "a.json", null);

        Assert.Equal(DatabaseProvider.PostgreSql, info!.Provider);
    }

    [Fact]
    public void Parse_WhenNoDatabase_ThenNull()
    {
        var info = DatabaseInfoModel.Parse("Host=localhost;Username=app", "a.json", null);

        Assert.Null(info);
    }

    [Fact]
    public void Locate_WhenJsonHasComments_ThenReadsDefaultConnection()
    {
        Write(@"server\Api\appsettings.json", "{\n  // commentaire\n  \"ConnectionStrings\": { /* bloc */ \"Autre\": \"Host=x;Database=autre\", \"DefaultConnection\": \"Host=localhost;Database=principale\", },\n}");

        var info = DatabaseConfigLocator.Locate(_root);

        Assert.Equal(("principale", "DefaultConnection"), (info!.SourceDb, info.ConnKey));
    }

    [Fact]
    public void Locate_WhenDevelopmentAndBaseFiles_ThenPrefersDevelopment()
    {
        Write(@"Api\appsettings.json", "{ \"ConnectionStrings\": { \"DefaultConnection\": \"Host=h;Database=base\" } }");
        Write(@"Api\appsettings.Development.json", "{ \"ConnectionStrings\": { \"DefaultConnection\": \"Host=h;Database=dev\" } }");

        var info = DatabaseConfigLocator.Locate(_root);

        Assert.Equal("dev", info!.SourceDb);
    }

    [Fact]
    public void Locate_WhenOnlyInIgnoredFolders_ThenNull()
    {
        Write(@"node_modules\pkg\appsettings.json", "{ \"ConnectionStrings\": { \"DefaultConnection\": \"Host=h;Database=ignoree\" } }");
        Write(@"Api\bin\Debug\appsettings.json", "{ \"ConnectionStrings\": { \"DefaultConnection\": \"Host=h;Database=ignoree\" } }");

        var info = DatabaseConfigLocator.Locate(_root);

        Assert.Null(info);
    }

    [Fact]
    public void Locate_WhenWebConfig_ThenReadsDomainContext()
    {
        Write(@"Site\Web.config", "<configuration><connectionStrings><add name=\"Logs\" connectionString=\"Server=s;Initial Catalog=Logs\" /><add name=\"DomainContext\" connectionString=\"Server=s;Initial Catalog=Metier;User Id=u;Password=p\" /></connectionStrings></configuration>");

        var info = DatabaseConfigLocator.Locate(_root);

        Assert.Equal(("Metier", DatabaseProvider.SqlServer), (info!.SourceDb, info.Provider));
    }

    [Fact]
    public void Rewrite_WhenPostgreSql_ThenReplacesDatabaseAndEnvironment()
    {
        var config = Write(@"Api\appsettings.Development.json", "{ \"ConnectionStrings\": { \"DefaultConnection\": \"Host=h;Database=app;Username=u\" } }");
        Write(".env", "PROJECT_NAME=app\r\nPOSTGRES_DB=app\r\nAUTRE=1\r\n");
        var info = DatabaseConfigLocator.Locate(_root)!;

        ConnectionRewriter.Rewrite(info, "app_vue", _root);

        Assert.Equal(("{ \"ConnectionStrings\": { \"DefaultConnection\": \"Host=h;Database=app_vue;Username=u\" } }", "PROJECT_NAME=app\r\nPOSTGRES_DB=app_vue\r\nAUTRE=1\r\n"), (File.ReadAllText(config), File.ReadAllText(Path.Combine(_root, ".env"))));
    }

    [Fact]
    public void Rewrite_WhenSqlServer_ThenReplacesInitialCatalogOnly()
    {
        var config = Write(@"Site\Web.config", "<add name=\"DefaultConnection\" connectionString=\"Server=s;Initial Catalog=Metier;Database=Metier\" />");
        var info = new DatabaseInfoModel(DatabaseProvider.SqlServer, "Metier", "s", null, null, null, true, config, "DefaultConnection");

        ConnectionRewriter.Rewrite(info, "Metier_vue", _root);

        Assert.Equal("<add name=\"DefaultConnection\" connectionString=\"Server=s;Initial Catalog=Metier_vue;Database=Metier\" />", File.ReadAllText(config));
    }

    [Theory]
    [InlineData("Feat-Vue.Git", "feat_vue_git")]
    [InlineData("__", "wt")]
    [InlineData("é", "wt")]
    public void Slug_WhenNameHasForbiddenCharacters_ThenSanitizes(string value, string expected)
    {
        var slug = DatabaseNames.Slug(value);

        Assert.Equal(expected, slug);
    }

    [Fact]
    public void Target_WhenPostgreSqlNameTooLong_ThenTruncatesTo63()
    {
        var info = new DatabaseInfoModel(DatabaseProvider.PostgreSql, new string('a', 60), null, null, null, null, false, "a.json", null);

        var target = DatabaseNames.Target(info, "feat/vue");

        Assert.Equal(63, target.Length);
    }

    [Fact]
    public void Identifiers_WhenNameHasDelimiters_ThenEscapesThem()
    {
        var quoted = (DatabaseNames.PostgreSqlIdentifier("a\"b"), DatabaseNames.SqlServerIdentifier("a]b"), DatabaseNames.Literal("a'b"));

        Assert.Equal(("\"a\"\"b\"", "[a]]b]", "'a''b'"), quoted);
    }

    private string Write(string relativePath, string content)
    {
        var path = Path.Combine(_root, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
        return path;
    }

    public void Dispose() => Directory.Delete(_root, true);
}
