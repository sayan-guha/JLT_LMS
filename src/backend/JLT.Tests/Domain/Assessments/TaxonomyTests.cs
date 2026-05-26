using Xunit;
using JLT.Domain.Assessments;

namespace JLT.Tests.Domain.Assessments
{
    public class TaxonomyTests
    {
        [Fact]
        public void SubjectTopic_ShouldInitialize_Correctly()
        {
            var topic = new SubjectTopic("Physics", "Science");
            Assert.Equal("Physics", topic.Name);
            Assert.Equal("Science", topic.ParentCategory);
        }
    }
}
