using Linqlite.Linq;

public abstract class TestBase
{
    protected string SqlFor<T>(IQueryable<T> query) => SqlCleaner.Clean(LinqliteTranslator.Translate(query));
}
