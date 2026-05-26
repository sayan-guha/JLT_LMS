using Xunit;
using JLT.Domain.Assessments;

namespace JLT.Tests.Domain.Assessments
{
    public class QuestionTests
    {
        [Fact]
        public void Question_CanSet_DraftStatus()
        {
            var q = new Question("What is 2+2?", QuestionType.MCQ);
            Assert.Equal(QuestionStatus.Draft, q.Status);
        }
    }
}
