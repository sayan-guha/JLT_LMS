# Assessment Module Implementation Plan

> **For Antigravity:** REQUIRED WORKFLOW: Use `.agent/workflows/execute-plan.md` to execute this plan in single-flow mode.

**Goal:** Build the new Assessment Module as a bounded context within the existing monolithic backend, including Domain entities for Questions, Assessments, and AI evaluations, along with their EF Core configurations and background worker interfaces.

**Architecture:** Bounded Context within the Monolith. The domain is isolated in `JLT.Domain/Assessments`. AI operations are defined via interfaces in the Application layer and implemented using OpenAI in the Infrastructure layer. Background jobs will be triggered via MediatR/Hangfire.

**Tech Stack:** C#, .NET 8, EF Core, xUnit, Moq.

---

### Task 1: Create Taxonomy & Skills Entities

**Files:**
- Create: `src/backend/JLT.Domain/Assessments/SubjectTopic.cs`
- Create: `src/backend/JLT.Domain/Assessments/Competency.cs`
- Test: `src/backend/JLT.Tests/Domain/Assessments/TaxonomyTests.cs`

**Step 1: Write the failing test**
```csharp
using Xunit;
using JLT.Domain.Assessments;

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
```

**Step 2: Run test to verify it fails**
Run: `dotnet test src/backend/JLT.Tests/JLT.Tests.csproj --filter "TaxonomyTests"`
Expected: FAIL (types not found)

**Step 3: Write minimal implementation**
```csharp
// src/backend/JLT.Domain/Assessments/SubjectTopic.cs
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
```

**Step 4: Run test to verify it passes**
Run: `dotnet test src/backend/JLT.Tests/JLT.Tests.csproj --filter "TaxonomyTests"`
Expected: PASS

**Step 5: Commit**
```bash
git add src/backend/JLT.Domain/Assessments/SubjectTopic.cs src/backend/JLT.Tests/Domain/Assessments/TaxonomyTests.cs
git commit -m "feat(domain): add SubjectTopic entity"
```

### Task 2: Create Question Bank Entities

**Files:**
- Create: `src/backend/JLT.Domain/Assessments/Question.cs`
- Test: `src/backend/JLT.Tests/Domain/Assessments/QuestionTests.cs`

**Step 1: Write the failing test**
```csharp
using Xunit;
using JLT.Domain.Assessments;

public class QuestionTests
{
    [Fact]
    public void Question_CanSet_DraftStatus()
    {
        var q = new Question("What is 2+2?", QuestionType.MCQ);
        Assert.Equal(QuestionStatus.Draft, q.Status);
    }
}
```

**Step 2: Run test to verify it fails**
Run: `dotnet test src/backend/JLT.Tests/JLT.Tests.csproj --filter "QuestionTests"`
Expected: FAIL

**Step 3: Write minimal implementation**
```csharp
// src/backend/JLT.Domain/Assessments/QuestionEnums.cs
namespace JLT.Domain.Assessments
{
    public enum QuestionType { MCQ, Subjective, TrueFalse, FillInBlanks, MatchTheFollowing }
    public enum QuestionStatus { Draft, UnderReview, Approved, Archived }
}

// src/backend/JLT.Domain/Assessments/Question.cs
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
```

**Step 4: Run test to verify it passes**
Run: `dotnet test src/backend/JLT.Tests/JLT.Tests.csproj --filter "QuestionTests"`
Expected: PASS

**Step 5: Commit**
```bash
git add src/backend/JLT.Domain/Assessments/Question.cs src/backend/JLT.Domain/Assessments/QuestionEnums.cs src/backend/JLT.Tests/Domain/Assessments/QuestionTests.cs
git commit -m "feat(domain): add Question entity and enums"
```

### Task 3: Create AI Interfaces

**Files:**
- Create: `src/backend/JLT.Application/Assessments/Interfaces/IAiAssessmentService.cs`

**Step 1: Write the failing test**
N/A (Interface definition)

**Step 2: Run test to verify it fails**
N/A

**Step 3: Write minimal implementation**
```csharp
// src/backend/JLT.Application/Assessments/Interfaces/IAiAssessmentService.cs
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
```

**Step 4: Run test to verify it passes**
Run: `dotnet build src/backend/JLT.sln`
Expected: Build Succeeded

**Step 5: Commit**
```bash
git add src/backend/JLT.Application/Assessments/Interfaces/IAiAssessmentService.cs
git commit -m "feat(app): add IAiAssessmentService interface"
```
