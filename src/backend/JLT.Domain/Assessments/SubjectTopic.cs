using System;

namespace JLT.Domain.Assessments
{
    public class SubjectTopic
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string ParentCategory { get; set; }

        public SubjectTopic(string name, string parentCategory)
        {
            Id = Guid.NewGuid();
            Name = name;
            ParentCategory = parentCategory;
        }
    }
}
