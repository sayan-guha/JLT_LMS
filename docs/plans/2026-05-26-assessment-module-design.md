# Assessment Module Design

## Overview
This document outlines the design for the new Assessment Module within the JLT Learning Management System. The module is responsible for delivering multi-modal assessments, generating questions via AI, providing adaptive testing (IRT), and evaluating subjective responses.

## Architecture Approach
**Bounded Context within the Monolith**: The module will be built as a distinct, self-contained sub-domain (`JLT.Domain/Assessments`, `JLT.Application/Assessments`) within the existing backend. All AI interactions (generation/evaluation) are abstracted behind an interface in the Application layer and implemented in the Infrastructure layer.

## Section 1: Data Models & Question Organization
The core entities within the `Assessments` bounded context are:

### Taxonomy & Skills
- **SubjectTopic**: Hierarchical subjects (e.g., Science -> Physics).
- **Competency**: Skills mapped to questions (e.g., Critical Thinking).

### Question Bank
- **Question**: Contains properties for `Type` (MCQ, Multiple Response, True/False, Fill in the Blanks, Match the Following, Subjective), `MediaAttachments` (Images, Audio, Video, Documents), `Marks` (default points), `NegativeMarks`, `MarkingRubric` (for AI/Instructor grading of subjective questions), `DifficultyLevel`, `BloomsLevel`, `TenantId` (null for global), and `Status` (Draft, UnderReview, Approved, Archived).
- **Mappings**: `QuestionTopicMapping` and `QuestionCompetencyMapping`.

### Assessments
- **Assessment**: Configures the test structure (Sections, Time limits per section), `PassingCriteria`, `MaxAttempts`, `TotalTimeLimit`, `ShufflingRules`, `FeedbackConfig` (Immediate, OnSubmit, ManualRelease), and `ProctoringConfig` (Webcam tracking, Browser lock).
- **AssessmentAttempt**: Tracks a user's test attempt (StartTime, EndTime, Status).
- **AttemptAnswer**: Records individual responses and time taken per question.

### Evaluations
- **EvaluationResult**: Stores the AI-suggested score, rationale based on the rubric, and final Instructor-approved score.

## Section 2: AI Integration & Background Workers

### Question Generation Data Flow
1. **Request**: Instructor selects criteria (Subject, Bloom's Level, Question Types, Quantity) via the UI.
2. **Processing**: API enqueues a background job and immediately returns status.
3. **AI Execution**: A background worker calls the OpenAI API (GPT-4o) using a strict JSON schema mapping to our question types.
4. **Storage & Notification**: Questions are saved in `Draft` status. A SignalR notification alerts the instructor that drafts are ready for review.

### Subjective Evaluation Data Flow
1. **Trigger**: Evaluation is triggered either automatically (Practice Mode) or manually by the instructor on-demand (Exam Mode).
2. **AI Execution**: A background worker sends the Question Text, Marking Rubric, and Student Answer to the OpenAI API.
3. **Result**: The AI returns a `SuggestedScore` and `Rationale`, which are stored in the `EvaluationResult`. In practice modes, this is shown immediately to the student. In exam modes, it awaits instructor approval.

## Section 3: Delivery Engine, Adaptive Testing & Analytics

### Delivery Engine Modes
- **Practice Mode**: Immediate feedback per question, automatic AI grading.
- **Exam/Proctored Mode**: Timed, locked navigation, no immediate feedback, manual/on-demand AI grading.

### Adaptive Testing (IRT)
- Tests begin with a question of average difficulty.
- After each answer, the system estimates the student's ability ($\theta$).
- The engine dynamically queries the Question Bank for the next question that maximizes information (best fits current $\theta$) while respecting Subject/Competency constraints.

### Analytics & Reporting
- **Student Level**: Competency mastery, time per question, Bloom's Taxonomy performance.
- **Global Level**: Item analysis (difficulty/discrimination), pass rates, competency gaps.
