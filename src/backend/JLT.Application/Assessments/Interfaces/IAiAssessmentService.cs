using System.Threading.Tasks;
using System.Collections.Generic;
using JLT.Domain.Assessments;

namespace JLT.Application.Assessments.Interfaces
{
    public interface IAiAssessmentService
    {
        Task<List<Question>> GenerateQuestionsAsync(string subject, string bloomsLevel, int count);
        Task<int> EvaluateSubjectiveAnswerAsync(Question question, string studentAnswer);
    }
}
