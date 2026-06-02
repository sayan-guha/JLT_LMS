import { useState } from 'react';
import { TrendingUp, Users, BookOpen, Award, Download, Calendar, Filter, Search } from 'lucide-react';

type AnalyticsTab = 'skill-coverage' | 'skill-gap' | 'user-engagement' | 'user-report' | 'ai-analytics';

export function Analytics() {
  const [activeTab, setActiveTab] = useState<AnalyticsTab>('skill-coverage');

  const tabs = [
    { id: 'skill-coverage' as AnalyticsTab, label: 'Skill Coverage' },
    { id: 'skill-gap' as AnalyticsTab, label: 'Skill Gap' },
    { id: 'user-engagement' as AnalyticsTab, label: 'User Engagement' },
    { id: 'user-report' as AnalyticsTab, label: 'User Skill Report' },
    { id: 'ai-analytics' as AnalyticsTab, label: 'AI Agent Analytics' },
  ];

  const renderTabContent = () => {
    switch (activeTab) {
      case 'skill-coverage':
        return <SkillCoverageTab />;
      case 'skill-gap':
        return <SkillGapTab />;
      case 'user-engagement':
        return <UserEngagementTab />;
      case 'user-report':
        return <UserReportTab />;
      case 'ai-analytics':
        return <AIAnalyticsTab />;
      default:
        return <SkillCoverageTab />;
    }
  };

  return (
    <div className="p-8">
      {/* Header */}
      <div className="mb-8">
        <div className="flex items-center justify-between">
          <div>
            <h2 className="text-3xl font-medium text-black tracking-tight mb-2">Analytics</h2>
            <p className="text-sm text-[#575757] tracking-tight">Platform insights, usage metrics, and performance data</p>
          </div>
          <div className="flex items-center gap-3">
            <button className="flex items-center gap-2 px-4 py-2 bg-[#F5F5F5] text-sm text-black tracking-tight hover:bg-[#E5E5E5] transition-colors rounded-lg">
              <Calendar className="w-4 h-4" strokeWidth={1.5} />
              Last 30 Days
            </button>
            <button className="flex items-center gap-2 px-4 py-2 bg-[#003D82] text-white text-sm tracking-tight hover:bg-[#002D62] transition-colors rounded-lg">
              <Download className="w-4 h-4" strokeWidth={1.5} />
              Export Report
            </button>
          </div>
        </div>
      </div>

      {/* Tabs */}
      <div className="mb-8 border-b border-[rgba(0,0,0,0.08)]">
        <div className="flex gap-8">
          {tabs.map((tab) => (
            <button
              key={tab.id}
              onClick={() => setActiveTab(tab.id)}
              className={`pb-4 text-sm tracking-tight transition-all relative ${
                activeTab === tab.id
                  ? 'text-[#003D82] font-semibold'
                  : 'text-[#575757] hover:text-black font-medium'
              }`}
            >
              {tab.label}
              {activeTab === tab.id && (
                <div className="absolute bottom-0 left-0 right-0 h-0.5 bg-[#003D82]" />
              )}
            </button>
          ))}
        </div>
      </div>

      {/* Tab Content */}
      {renderTabContent()}
    </div>
  );
}

function SkillCoverageTab() {
  return (
    <div>
      {/* Filters */}
      <div className="mb-6 flex items-center gap-4">
        <select className="px-4 py-2 border border-[rgba(0,0,0,0.08)] rounded-lg text-sm text-black tracking-tight focus:outline-none focus:ring-2 focus:ring-[#003D82]">
          <option>All Departments</option>
          <option>Operations</option>
          <option>Automation</option>
          <option>Energy Solutions</option>
          <option>Aerospace</option>
        </select>
        <select className="px-4 py-2 border border-[rgba(0,0,0,0.08)] rounded-lg text-sm text-black tracking-tight focus:outline-none focus:ring-2 focus:ring-[#003D82]">
          <option>All Job Roles</option>
          <option>VP of Operations</option>
          <option>Senior Manager</option>
          <option>Team Lead</option>
          <option>Engineer</option>
        </select>
      </div>

      {/* Skill Type Cards */}
      <div className="grid grid-cols-3 gap-6 mb-8">
        {['Core Skills', 'Soft Skills', 'Elective Skills'].map((skillType, idx) => (
          <div key={idx} className="bg-white border border-[rgba(0,0,0,0.08)] rounded-lg p-6">
            <h3 className="text-lg font-medium text-black tracking-tight mb-6">{skillType}</h3>
            <div className="space-y-4">
              {['Foundation', 'Intermediate', 'Advanced', 'Expert'].map((level, levelIdx) => {
                const count = [45, 32, 18, 8][levelIdx];
                const maxCount = 45;
                const percentage = (count / maxCount) * 100;
                return (
                  <div key={level}>
                    <div className="flex items-center justify-between mb-2">
                      <span className="text-sm text-black tracking-tight">{level}</span>
                      <span className="text-sm font-medium text-black tracking-tight">{count} users</span>
                    </div>
                    <div className="h-2 bg-[#F5F5F5] rounded-full overflow-hidden">
                      <div
                        className="h-full bg-[#003D82] rounded-full transition-all"
                        style={{ width: `${percentage}%` }}
                      ></div>
                    </div>
                  </div>
                );
              })}
            </div>
          </div>
        ))}
      </div>

      {/* Trend Chart */}
      <div className="bg-white border border-[rgba(0,0,0,0.08)] rounded-lg p-6">
        <h3 className="text-lg font-medium text-black tracking-tight mb-6">Skill Acquisition Trend</h3>
        <div className="h-64 relative">
          <svg width="100%" height="100%" viewBox="0 0 800 240">
            {/* Grid lines */}
            {[0, 1, 2, 3, 4].map((i) => (
              <line
                key={i}
                x1="40"
                y1={40 + i * 40}
                x2="780"
                y2={40 + i * 40}
                stroke="#F5F5F5"
                strokeWidth="1"
              />
            ))}

            {/* Line chart */}
            <polyline
              points={[65, 72, 78, 85, 82, 90, 95, 88, 92, 98, 105, 110]
                .map((value, idx) => {
                  const x = 40 + (idx * 60) + 30;
                  const y = 200 - (value * 1.45);
                  return `${x},${y}`;
                })
                .join(' ')}
              fill="none"
              stroke="#003D82"
              strokeWidth="2"
            />

            {/* Data points */}
            {[65, 72, 78, 85, 82, 90, 95, 88, 92, 98, 105, 110].map((value, idx) => {
              const x = 40 + (idx * 60) + 30;
              const y = 200 - (value * 1.45);
              return (
                <circle
                  key={idx}
                  cx={x}
                  cy={y}
                  r="4"
                  fill="#003D82"
                  stroke="white"
                  strokeWidth="2"
                />
              );
            })}

            {/* Month labels */}
            {['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'].map((month, idx) => (
              <text
                key={idx}
                x={40 + (idx * 60) + 30}
                y="220"
                textAnchor="middle"
                fontSize="12"
                fill="#575757"
              >
                {month}
              </text>
            ))}
          </svg>
        </div>
      </div>
    </div>
  );
}

function SkillGapTab() {
  const departments = [
    { name: 'Operations', gap: 23, totalSkills: 120, acquiredSkills: 92 },
    { name: 'Automation', gap: 31, totalSkills: 115, acquiredSkills: 79 },
    { name: 'Energy Solutions', gap: 18, totalSkills: 100, acquiredSkills: 82 },
    { name: 'Aerospace', gap: 27, totalSkills: 110, acquiredSkills: 80 },
    { name: 'Building Technologies', gap: 15, totalSkills: 95, acquiredSkills: 81 },
  ];

  const jobRoles = [
    { role: 'Junior Engineer', gap: 45, department: 'Automation' },
    { role: 'Associate Analyst', gap: 42, department: 'Operations' },
    { role: 'Technical Specialist', gap: 38, department: 'Energy Solutions' },
    { role: 'Project Coordinator', gap: 35, department: 'Aerospace' },
    { role: 'Systems Engineer', gap: 33, department: 'Automation' },
    { role: 'Operations Analyst', gap: 31, department: 'Operations' },
    { role: 'Quality Engineer', gap: 29, department: 'Building Technologies' },
    { role: 'Safety Coordinator', gap: 27, department: 'Operations' },
    { role: 'Process Engineer', gap: 25, department: 'Automation' },
    { role: 'Design Engineer', gap: 23, department: 'Aerospace' },
  ];

  return (
    <div>
      <div className="grid grid-cols-2 gap-6">
        {/* Department Skill Gaps */}
        <div className="bg-white border border-[rgba(0,0,0,0.08)] rounded-lg p-6">
          <h3 className="text-lg font-medium text-black tracking-tight mb-6">Department Skill Gaps</h3>
          <div className="space-y-4">
            {departments.map((dept, idx) => (
              <div key={idx} className="pb-4 border-b border-[rgba(0,0,0,0.08)] last:border-0 last:pb-0">
                <div className="flex items-center justify-between mb-2">
                  <span className="text-sm font-medium text-black tracking-tight">{dept.name}</span>
                  <span className="text-sm text-[#575757] tracking-tight">
                    {dept.acquiredSkills}/{dept.totalSkills} skills
                  </span>
                </div>
                <div className="flex items-center gap-3">
                  <div className="flex-1 h-2 bg-[#F5F5F5] rounded-full overflow-hidden">
                    <div
                      className="h-full bg-red-500 rounded-full"
                      style={{ width: `${dept.gap}%` }}
                    ></div>
                  </div>
                  <span className="text-sm font-medium text-red-600 tracking-tight">{dept.gap}% gap</span>
                </div>
              </div>
            ))}
          </div>
        </div>

        {/* Top 10 Job Roles with Maximum Gaps */}
        <div className="bg-white border border-[rgba(0,0,0,0.08)] rounded-lg p-6">
          <h3 className="text-lg font-medium text-black tracking-tight mb-6">Top 10 Roles - Highest Skill Gaps</h3>
          <div className="space-y-3">
            {jobRoles.map((job, idx) => (
              <div key={idx} className="flex items-center justify-between pb-3 border-b border-[rgba(0,0,0,0.08)] last:border-0 last:pb-0">
                <div className="flex-1">
                  <p className="text-sm font-medium text-black tracking-tight">{job.role}</p>
                  <p className="text-xs text-[#575757] tracking-tight mt-0.5">{job.department}</p>
                </div>
                <div className="flex items-center gap-2">
                  <div className="w-24 h-2 bg-[#F5F5F5] rounded-full overflow-hidden">
                    <div
                      className="h-full bg-red-500 rounded-full"
                      style={{ width: `${job.gap}%` }}
                    ></div>
                  </div>
                  <span className="text-sm font-medium text-red-600 tracking-tight w-12 text-right">{job.gap}%</span>
                </div>
              </div>
            ))}
          </div>
        </div>
      </div>
    </div>
  );
}

function UserEngagementTab() {
  const topCourses = [
    { name: 'Strategic Leadership in Technology', completions: 342, adoption: 85 },
    { name: 'Introduction to Digital Thread', completions: 298, adoption: 78 },
    { name: 'Advanced Analytics', completions: 276, adoption: 72 },
    { name: 'Energy Transition Leadership', completions: 254, adoption: 68 },
    { name: 'AI in Industrial Automation', completions: 231, adoption: 65 },
    { name: 'Cybersecurity Fundamentals', completions: 218, adoption: 62 },
    { name: 'Building Automation Systems', completions: 205, adoption: 58 },
    { name: 'Process Control Excellence', completions: 192, adoption: 55 },
    { name: 'Sustainable Aviation Technologies', completions: 178, adoption: 52 },
    { name: 'Leadership Communication', completions: 165, adoption: 48 },
  ];

  return (
    <div>
      <div className="grid grid-cols-2 gap-6 mb-8">
        {/* Monthly Completions Trend */}
        <div className="bg-white border border-[rgba(0,0,0,0.08)] rounded-lg p-6 col-span-2">
          <h3 className="text-lg font-medium text-black tracking-tight mb-6">Course Completions - Monthly Trend</h3>
          <div className="h-64 relative">
            <svg width="100%" height="100%" viewBox="0 0 800 240">
              {/* Grid lines */}
              {[0, 1, 2, 3, 4].map((i) => (
                <line
                  key={i}
                  x1="40"
                  y1={40 + i * 40}
                  x2="780"
                  y2={40 + i * 40}
                  stroke="#F5F5F5"
                  strokeWidth="1"
                />
              ))}

              {/* Line chart */}
              <polyline
                points={[120, 135, 145, 158, 162, 175, 182, 178, 190, 198, 205, 215]
                  .map((value, idx) => {
                    const x = 40 + (idx * 60) + 30;
                    const y = 200 - ((value - 100) * 1.6);
                    return `${x},${y}`;
                  })
                  .join(' ')}
                fill="none"
                stroke="#003D82"
                strokeWidth="2"
              />

              {/* Data points */}
              {[120, 135, 145, 158, 162, 175, 182, 178, 190, 198, 205, 215].map((value, idx) => {
                const x = 40 + (idx * 60) + 30;
                const y = 200 - ((value - 100) * 1.6);
                return (
                  <circle
                    key={idx}
                    cx={x}
                    cy={y}
                    r="4"
                    fill="#003D82"
                    stroke="white"
                    strokeWidth="2"
                  />
                );
              })}

              {/* Month labels */}
              {['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'].map((month, idx) => (
                <text
                  key={idx}
                  x={40 + (idx * 60) + 30}
                  y="220"
                  textAnchor="middle"
                  fontSize="12"
                  fill="#575757"
                >
                  {month}
                </text>
              ))}
            </svg>
          </div>
        </div>
      </div>

      {/* Top 10 Courses */}
      <div className="bg-white border border-[rgba(0,0,0,0.08)] rounded-lg p-6">
        <h3 className="text-lg font-medium text-black tracking-tight mb-6">Top 10 Courses - Completion & Adoption</h3>
        <div className="space-y-4">
          {topCourses.map((course, idx) => (
            <div key={idx} className="pb-4 border-b border-[rgba(0,0,0,0.08)] last:border-0 last:pb-0">
              <div className="flex items-center justify-between mb-2">
                <span className="text-sm font-medium text-black tracking-tight flex-1">{course.name}</span>
                <span className="text-sm text-[#575757] tracking-tight ml-4">{course.completions} completions</span>
              </div>
              <div className="flex items-center gap-3">
                <div className="flex-1 h-2 bg-[#F5F5F5] rounded-full overflow-hidden">
                  <div
                    className="h-full bg-[#003D82] rounded-full"
                    style={{ width: `${course.adoption}%` }}
                  ></div>
                </div>
                <span className="text-sm font-medium text-[#003D82] tracking-tight w-12 text-right">{course.adoption}%</span>
              </div>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}

function UserReportTab() {
  const [selectedUser, setSelectedUser] = useState<string | null>(null);

  return (
    <div>
      {/* Filters */}
      <div className="mb-6 flex items-center gap-4">
        <select className="px-4 py-2 border border-[rgba(0,0,0,0.08)] rounded-lg text-sm text-black tracking-tight focus:outline-none focus:ring-2 focus:ring-[#003D82]">
          <option>All Departments</option>
          <option>Operations</option>
          <option>Automation</option>
          <option>Energy Solutions</option>
          <option>Aerospace</option>
        </select>
        <select className="px-4 py-2 border border-[rgba(0,0,0,0.08)] rounded-lg text-sm text-black tracking-tight focus:outline-none focus:ring-2 focus:ring-[#003D82]">
          <option>All Job Roles</option>
          <option>VP of Operations</option>
          <option>Senior Manager</option>
          <option>Team Lead</option>
          <option>Engineer</option>
        </select>
        <div className="flex-1 relative">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-[#575757]" strokeWidth={1.5} />
          <input
            type="text"
            placeholder="Search user by name..."
            className="w-full pl-10 pr-4 py-2 border border-[rgba(0,0,0,0.08)] rounded-lg text-sm text-black tracking-tight focus:outline-none focus:ring-2 focus:ring-[#003D82]"
            onChange={(e) => setSelectedUser(e.target.value || null)}
          />
        </div>
      </div>

      {selectedUser !== null ? (
        <div className="grid grid-cols-3 gap-6">
          {/* Skill Coverage Radar */}
          <div className="bg-white border border-[rgba(0,0,0,0.08)] rounded-lg p-6">
            <h3 className="text-lg font-medium text-black tracking-tight mb-4">Skill Coverage</h3>
            <div className="aspect-square flex items-center justify-center mb-4">
              <svg width="100%" height="100%" viewBox="0 0 300 300">
                {/* Background circles */}
                {[0.2, 0.4, 0.6, 0.8, 1.0].map((scale) => (
                  <polygon
                    key={scale}
                    points="150,50 233.25,200 66.75,200"
                    fill="none"
                    stroke="#E5E5E5"
                    strokeWidth="1"
                    transform={`translate(150, 150) scale(${scale}) translate(-150, -150)`}
                  />
                ))}

                {/* Axis lines */}
                <line x1="150" y1="150" x2="150" y2="50" stroke="#D4D4D4" strokeWidth="1" />
                <line x1="150" y1="150" x2="233.25" y2="200" stroke="#D4D4D4" strokeWidth="1" />
                <line x1="150" y1="150" x2="66.75" y2="200" stroke="#D4D4D4" strokeWidth="1" />

                {/* Data polygon - Core: 85%, Soft: 72%, Elective: 68% */}
                <polygon
                  points={`150,${150 - (85)}, ${150 + (72 * 0.866)},${150 + (72 * 0.5)}, ${150 - (68 * 0.866)},${150 + (68 * 0.5)}`}
                  fill="#003D82"
                  fillOpacity="0.3"
                  stroke="#003D82"
                  strokeWidth="2"
                />

                {/* Data points */}
                <circle cx="150" cy={150 - 85} r="4" fill="#003D82" />
                <circle cx={150 + (72 * 0.866)} cy={150 + (72 * 0.5)} r="4" fill="#003D82" />
                <circle cx={150 - (68 * 0.866)} cy={150 + (68 * 0.5)} r="4" fill="#003D82" />

                {/* Labels */}
                <text x="150" y="40" textAnchor="middle" fontSize="12" fill="#575757">Core Skills</text>
                <text x="150" y="30" textAnchor="middle" fontSize="14" fontWeight="600" fill="#003D82">85%</text>

                <text x="250" y="210" textAnchor="middle" fontSize="12" fill="#575757">Soft Skills</text>
                <text x="250" y="225" textAnchor="middle" fontSize="14" fontWeight="600" fill="#003D82">72%</text>

                <text x="50" y="210" textAnchor="middle" fontSize="12" fill="#575757">Elective</text>
                <text x="50" y="225" textAnchor="middle" fontSize="14" fontWeight="600" fill="#003D82">68%</text>
              </svg>
            </div>
            <div className="text-center p-4 bg-[#F5F5F5] rounded-lg">
              <p className="text-xs text-[#575757] tracking-tight mb-1">Total Skill Score</p>
              <p className="text-3xl font-medium text-[#003D82] tracking-tight">782</p>
            </div>
          </div>

          {/* Skill-wise Completion */}
          <div className="bg-white border border-[rgba(0,0,0,0.08)] rounded-lg p-6">
            <h3 className="text-lg font-medium text-black tracking-tight mb-4">Skill-wise Completion</h3>
            <div className="space-y-3">
              {[
                { name: 'Advanced Analytics', level: 'Foundation', completion: 45 },
                { name: 'Digital Thread', level: 'Intermediate', completion: 60 },
                { name: 'Communication Skills', level: 'Intermediate', completion: 75 },
                { name: 'Process Control', level: 'Expert', completion: 100 },
                { name: 'Building Automation', level: 'Advanced', completion: 85 },
                { name: 'AI in Automation', level: 'Foundation', completion: 30 },
              ].map((skill, idx) => (
                <div key={idx} className="pb-3 border-b border-[rgba(0,0,0,0.08)] last:border-0 last:pb-0">
                  <div className="flex items-center justify-between mb-1">
                    <span className="text-sm font-medium text-black tracking-tight">{skill.name}</span>
                    <span className="text-xs text-[#575757] tracking-tight">{skill.level}</span>
                  </div>
                  <div className="flex items-center gap-2">
                    <div className="flex-1 h-2 bg-[#F5F5F5] rounded-full overflow-hidden">
                      <div
                        className="h-full bg-[#003D82] rounded-full"
                        style={{ width: `${skill.completion}%` }}
                      ></div>
                    </div>
                    <span className="text-xs font-medium text-[#003D82] tracking-tight w-10 text-right">{skill.completion}%</span>
                  </div>
                </div>
              ))}
            </div>
          </div>

          {/* Skill Acquisition Analysis */}
          <div className="bg-white border border-[rgba(0,0,0,0.08)] rounded-lg p-6">
            <h3 className="text-lg font-medium text-black tracking-tight mb-4">Skill Acquisition Analysis</h3>
            <div className="space-y-4 text-sm text-[#575757] tracking-tight leading-relaxed">
              <p>
                <strong className="text-black">Overall Performance:</strong> Sarah Chen demonstrates strong proficiency across core operational skills, with particular expertise in Process Control Systems (Expert level) and Building Automation (Advanced level).
              </p>
              <p>
                <strong className="text-black">Strengths:</strong> Exceptional performance in technical core skills with 85% coverage. Her expertise in automation and control systems positions her well for strategic operational leadership roles.
              </p>
              <p>
                <strong className="text-black">Development Areas:</strong> Focus on advancing AI in Automation (currently at Foundation, 30% complete) and deepening Advanced Analytics capabilities to support data-driven decision making.
              </p>
              <p>
                <strong className="text-black">Recommendations:</strong> Consider enrolling in the Machine Learning for Operations course to bridge the AI skill gap. Her strong foundation makes her an ideal candidate for the upcoming Digital Transformation workshop.
              </p>
            </div>
          </div>
        </div>
      ) : (
        <div className="bg-white border border-[rgba(0,0,0,0.08)] rounded-lg p-12 text-center">
          <Users className="w-12 h-12 text-[#A3A3A3] mx-auto mb-4" strokeWidth={1.5} />
          <p className="text-sm text-[#575757] tracking-tight">
            Select department and job role filters, then search for a user to view their detailed skill report
          </p>
        </div>
      )}
    </div>
  );
}

function AIAnalyticsTab() {
  const agents = [
    { name: 'Aria', conversations: 1243, terms: ['automation', 'manufacturing', 'process', 'control', 'systems'] },
    { name: 'Nova', conversations: 987, terms: ['energy', 'sustainability', 'carbon', 'renewable', 'efficiency'] },
    { name: 'Orion', conversations: 756, terms: ['aerospace', 'aviation', 'safety', 'compliance', 'systems'] },
    { name: 'Marcus', conversations: 892, terms: ['digital', 'innovation', 'transformation', 'technology', 'strategy'] },
    { name: 'Priya', conversations: 634, terms: ['operations', 'leadership', 'management', 'excellence', 'performance'] },
  ];

  return (
    <div>
      {/* Filters */}
      <div className="mb-6 flex items-center gap-4">
        <select className="px-4 py-2 border border-[rgba(0,0,0,0.08)] rounded-lg text-sm text-black tracking-tight focus:outline-none focus:ring-2 focus:ring-[#003D82]">
          <option>All Departments</option>
          <option>Operations</option>
          <option>Automation</option>
          <option>Energy Solutions</option>
          <option>Aerospace</option>
        </select>
        <select className="px-4 py-2 border border-[rgba(0,0,0,0.08)] rounded-lg text-sm text-black tracking-tight focus:outline-none focus:ring-2 focus:ring-[#003D82]">
          <option>All Job Roles</option>
          <option>VP of Operations</option>
          <option>Senior Manager</option>
          <option>Team Lead</option>
          <option>Engineer</option>
        </select>
      </div>

      {/* Conversation Trend */}
      <div className="bg-white border border-[rgba(0,0,0,0.08)] rounded-lg p-6 mb-6">
        <h3 className="text-lg font-medium text-black tracking-tight mb-6">AI Conversations - Last 6 Months</h3>
        <div className="h-64 relative">
          <svg width="100%" height="100%" viewBox="0 0 700 240">
            {/* Grid lines */}
            {[0, 1, 2, 3, 4].map((i) => (
              <line
                key={i}
                x1="40"
                y1={40 + i * 40}
                x2="660"
                y2={40 + i * 40}
                stroke="#F5F5F5"
                strokeWidth="1"
              />
            ))}

            {/* Line chart */}
            <polyline
              points={[520, 580, 640, 720, 780, 850]
                .map((value, idx) => {
                  const x = 40 + (idx * 100) + 50;
                  const y = 200 - ((value - 500) * 0.48);
                  return `${x},${y}`;
                })
                .join(' ')}
              fill="none"
              stroke="#003D82"
              strokeWidth="2"
            />

            {/* Data points */}
            {[520, 580, 640, 720, 780, 850].map((value, idx) => {
              const x = 40 + (idx * 100) + 50;
              const y = 200 - ((value - 500) * 0.48);
              return (
                <circle
                  key={idx}
                  cx={x}
                  cy={y}
                  r="4"
                  fill="#003D82"
                  stroke="white"
                  strokeWidth="2"
                />
              );
            })}

            {/* Month labels */}
            {['Nov', 'Dec', 'Jan', 'Feb', 'Mar', 'Apr'].map((month, idx) => (
              <text
                key={idx}
                x={40 + (idx * 100) + 50}
                y="220"
                textAnchor="middle"
                fontSize="12"
                fill="#575757"
              >
                {month}
              </text>
            ))}
          </svg>
        </div>
      </div>

      {/* Per-Agent Analytics */}
      <div className="grid grid-cols-2 gap-6">
        {agents.map((agent, idx) => (
          <div key={idx} className="bg-white border border-[rgba(0,0,0,0.08)] rounded-lg p-6">
            <div className="flex items-center justify-between mb-4">
              <h3 className="text-lg font-medium text-black tracking-tight">{agent.name}</h3>
              <span className="text-sm text-[#575757] tracking-tight">{agent.conversations} conversations</span>
            </div>
            <div className="bg-[#F5F5F5] rounded-lg p-4">
              <p className="text-xs text-[#575757] tracking-tight mb-3">Most Used Terms</p>
              <div className="flex flex-wrap gap-2">
                {agent.terms.map((term, termIdx) => {
                  const sizes = ['text-lg', 'text-base', 'text-sm', 'text-sm', 'text-xs'];
                  return (
                    <span
                      key={termIdx}
                      className={`${sizes[termIdx]} font-medium text-[#003D82] tracking-tight`}
                    >
                      {term}
                    </span>
                  );
                })}
              </div>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}
