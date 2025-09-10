using PROG7312_POEPART1.Models;

namespace PROG7312_POEPART1.Services
{
    public interface IssueService
    {
        void Add(Issue issue);
        IEnumerable<Issue> GetAll();
    }

    public class IssueStore : IssueService
    {
        private readonly List<Issue> _issues = new();

        public void Add(Issue issue)
        {
            _issues.Add(issue);
        }


        public IEnumerable<Issue> GetAll() => _issues;
    }
}
