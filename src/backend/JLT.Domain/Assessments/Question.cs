using System;

namespace JLT.Domain.Assessments
{
    public class Question
    {
        public Guid Id { get; set; }
        public string Text { get; set; }
        public QuestionType Type { get; set; }
        public QuestionStatus Status { get; set; }
        public int Marks { get; set; } = 1;
        public int NegativeMarks { get; set; } = 0;
        public Guid? TenantId { get; set; }

        public Question(string text, QuestionType type)
        {
            Id = Guid.NewGuid();
            Text = text;
            Type = type;
            Status = QuestionStatus.Draft;
        }
    }
}
