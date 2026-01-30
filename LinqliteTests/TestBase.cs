using Linqlite.Linq;

public abstract class TestBase
{
    protected string SqlFor<T>(IQueryable<T> query, LinqliteProvider provider) => SqlCleaner.Clean(LinqliteTranslator.Translate(query, provider));
}
