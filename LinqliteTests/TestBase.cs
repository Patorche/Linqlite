using Linqlite.Linq;

public abstract class TestBase
{
    protected string SqlFor<T>(IQueryable<T> query) => LinqliteTranslator.Translate(query);
}
