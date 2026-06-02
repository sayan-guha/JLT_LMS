import { Plus, Edit2, Trash2, ChevronRight, ChevronDown, Search } from 'lucide-react';
import { useState, useMemo } from 'react';

const skillCategories = [
  {
    id: 1,
    name: 'Core Skills',
    description: 'Technical skills essential for operations roles',
    skillCount: 12,
    expanded: true,
    skills: [
      { id: 1, name: 'Process Control Systems', level: 'Expert', courses: 8, users: 342, jobRoles: ['Operations Manager', 'Plant Engineer'] },
      { id: 2, name: 'Industrial Safety', level: 'Advanced', courses: 6, users: 1205, jobRoles: ['Operations Manager', 'Safety Officer', 'Plant Engineer'] },
      { id: 3, name: 'Building Automation', level: 'Advanced', courses: 7, users: 567, jobRoles: ['Facility Manager', 'Automation Specialist'] },
      { id: 4, name: 'Manufacturing Operations', level: 'Intermediate', courses: 5, users: 892, jobRoles: ['Operations Manager', 'Production Supervisor'] },
    ],
  },
  {
    id: 2,
    name: 'Soft Skills',
    description: 'Leadership and interpersonal competencies',
    skillCount: 8,
    expanded: false,
    skills: [
      { id: 5, name: 'Strategic Thinking', level: 'Advanced', courses: 4, users: 1543, jobRoles: ['Operations Manager', 'Director', 'Plant Engineer'] },
      { id: 6, name: 'Leadership', level: 'Advanced', courses: 6, users: 2104, jobRoles: ['Operations Manager', 'Director', 'Production Supervisor'] },
      { id: 7, name: 'Change Management', level: 'Intermediate', courses: 3, users: 789, jobRoles: ['Operations Manager', 'Director', 'HR Manager'] },
    ],
  },
  {
    id: 3,
    name: 'Elective Skills',
    description: 'Specialized skills for specific domains',
    skillCount: 15,
    expanded: false,
    skills: [
      { id: 8, name: 'Sustainable Aviation Fuels', level: 'Intermediate', courses: 4, users: 234, jobRoles: ['Sustainability Manager', 'Plant Engineer'] },
      { id: 9, name: 'Carbon Capture Technologies', level: 'Foundation', courses: 3, users: 187, jobRoles: ['Sustainability Manager', 'Environmental Engineer'] },
      { id: 10, name: 'Energy Efficiency', level: 'Intermediate', courses: 5, users: 654, jobRoles: ['Facility Manager', 'Sustainability Manager', 'Plant Engineer'] },
    ],
  },
];

const jobRoles = [
  'All Job Roles',
  'Operations Manager',
  'Plant Engineer',
  'Safety Officer',
  'Facility Manager',
  'Automation Specialist',
  'Production Supervisor',
  'Director',
  'HR Manager',
  'Sustainability Manager',
  'Environmental Engineer',
];

export function SkillStructure() {
  const [categories, setCategories] = useState(skillCategories);
  const [searchTerm, setSearchTerm] = useState('');
  const [selectedJobRole, setSelectedJobRole] = useState('All Job Roles');

  const toggleCategory = (categoryId: number) => {
    setCategories(categories.map(cat =>
      cat.id === categoryId ? { ...cat, expanded: !cat.expanded } : cat
    ));
  };

  // Filter categories based on search term and job role
  const filteredCategories = useMemo(() => {
    return categories.map(category => {
      const filteredSkills = category.skills.filter(skill => {
        const matchesSearch = skill.name.toLowerCase().includes(searchTerm.toLowerCase());
        const matchesJobRole = selectedJobRole === 'All Job Roles' || skill.jobRoles.includes(selectedJobRole);
        return matchesSearch && matchesJobRole;
      });

      return {
        ...category,
        skills: filteredSkills,
        skillCount: filteredSkills.length,
      };
    }).filter(category => category.skills.length > 0);
  }, [categories, searchTerm, selectedJobRole]);

  return (
    <div className="p-8">
      {/* Header */}
      <div className="mb-8">
        <h2 className="text-3xl font-medium text-black tracking-tight mb-2">Skill Structure</h2>
        <p className="text-sm text-[#575757] tracking-tight">Manage skill taxonomy, categories, and learning paths</p>
      </div>

      {/* Stats */}
      <div className="grid grid-cols-3 gap-6 mb-8">
        <div className="bg-white border border-[rgba(0,0,0,0.08)] rounded-lg p-6">
          <p className="text-xs text-[#575757] tracking-tight mb-2">Total Skills</p>
          <p className="text-3xl font-medium text-black tracking-tight">35</p>
          <p className="text-xs text-[#575757] tracking-tight mt-2">Across 3 categories</p>
        </div>
        <div className="bg-white border border-[rgba(0,0,0,0.08)] rounded-lg p-6">
          <p className="text-xs text-[#575757] tracking-tight mb-2">Total Courses</p>
          <p className="text-3xl font-medium text-black tracking-tight">128</p>
          <p className="text-xs text-[#575757] tracking-tight mt-2">Linked to skills</p>
        </div>
        <div className="bg-white border border-[rgba(0,0,0,0.08)] rounded-lg p-6">
          <p className="text-xs text-[#575757] tracking-tight mb-2">Learning Paths</p>
          <p className="text-3xl font-medium text-black tracking-tight">24</p>
          <p className="text-xs text-[#575757] tracking-tight mt-2">Active journeys</p>
        </div>
      </div>

      {/* Search and Filters */}
      <div className="mb-6 space-y-4">
        <div className="flex items-center gap-4">
          <div className="flex-1 relative">
            <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 w-4 h-4 text-[#575757]" strokeWidth={1.5} />
            <input
              type="text"
              placeholder="Search skills..."
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
              className="w-full pl-10 pr-4 py-2 border border-[rgba(0,0,0,0.08)] rounded-lg text-sm text-black tracking-tight placeholder:text-[#575757] focus:outline-none focus:ring-2 focus:ring-[#003D82] focus:border-transparent"
            />
          </div>
          <div className="w-64">
            <select
              value={selectedJobRole}
              onChange={(e) => setSelectedJobRole(e.target.value)}
              className="w-full px-4 py-2 border border-[rgba(0,0,0,0.08)] rounded-lg text-sm text-black tracking-tight focus:outline-none focus:ring-2 focus:ring-[#003D82] focus:border-transparent bg-white"
            >
              {jobRoles.map((role) => (
                <option key={role} value={role}>
                  {role}
                </option>
              ))}
            </select>
          </div>
        </div>
      </div>

      {/* Actions */}
      <div className="flex items-center justify-between mb-6">
        <div className="flex items-center gap-3">
          <button className="flex items-center gap-2 px-4 py-2 bg-[#003D82] text-white text-sm tracking-tight hover:bg-[#002D62] transition-colors rounded-lg">
            <Plus className="w-4 h-4" strokeWidth={1.5} />
            Add Category
          </button>
          <button className="flex items-center gap-2 px-4 py-2 bg-[#F5F5F5] text-sm text-black tracking-tight hover:bg-[#E5E5E5] transition-colors rounded-lg">
            <Plus className="w-4 h-4" strokeWidth={1.5} />
            Add Skill
          </button>
        </div>
        {(searchTerm || selectedJobRole !== 'All Job Roles') && (
          <button
            onClick={() => {
              setSearchTerm('');
              setSelectedJobRole('All Job Roles');
            }}
            className="text-sm text-[#003D82] hover:underline tracking-tight"
          >
            Clear filters
          </button>
        )}
      </div>

      {/* Skill Categories */}
      <div className="space-y-4">
        {filteredCategories.length === 0 ? (
          <div className="bg-white border border-[rgba(0,0,0,0.08)] rounded-lg p-12 text-center">
            <p className="text-sm text-[#575757] tracking-tight">No skills found matching your search criteria</p>
          </div>
        ) : (
          filteredCategories.map((category) => (
          <div key={category.id} className="bg-white border border-[rgba(0,0,0,0.08)] rounded-lg overflow-hidden">
            {/* Category Header */}
            <div
              className="flex items-center justify-between p-6 cursor-pointer hover:bg-[#FAFAFA] transition-colors"
              onClick={() => toggleCategory(category.id)}
            >
              <div className="flex items-center gap-4 flex-1">
                <button className="w-8 h-8 flex items-center justify-center hover:bg-[#F5F5F5] rounded transition-colors">
                  {category.expanded ? (
                    <ChevronDown className="w-5 h-5 text-[#575757]" strokeWidth={1.5} />
                  ) : (
                    <ChevronRight className="w-5 h-5 text-[#575757]" strokeWidth={1.5} />
                  )}
                </button>
                <div className="flex-1">
                  <h3 className="text-lg font-medium text-black tracking-tight mb-1">{category.name}</h3>
                  <p className="text-sm text-[#575757] tracking-tight">{category.description}</p>
                </div>
                <div className="text-right">
                  <p className="text-2xl font-medium text-black tracking-tight">{category.skillCount}</p>
                  <p className="text-xs text-[#575757] tracking-tight">Skills</p>
                </div>
              </div>
              <div className="flex items-center gap-2 ml-4">
                <button
                  onClick={(e) => {
                    e.stopPropagation();
                  }}
                  className="p-2 hover:bg-[#F5F5F5] rounded transition-colors"
                >
                  <Edit2 className="w-4 h-4 text-[#575757]" strokeWidth={1.5} />
                </button>
                <button
                  onClick={(e) => {
                    e.stopPropagation();
                  }}
                  className="p-2 hover:bg-red-50 rounded transition-colors"
                >
                  <Trash2 className="w-4 h-4 text-red-600" strokeWidth={1.5} />
                </button>
              </div>
            </div>

            {/* Skills List */}
            {category.expanded && (
              <div className="border-t border-[rgba(0,0,0,0.08)]">
                <table className="w-full">
                  <thead className="bg-[#F5F5F5]">
                    <tr>
                      <th className="text-left px-6 py-3 text-xs font-medium text-[#575757] tracking-tight">Skill Name</th>
                      <th className="text-left px-6 py-3 text-xs font-medium text-[#575757] tracking-tight">Max Level</th>
                      <th className="text-left px-6 py-3 text-xs font-medium text-[#575757] tracking-tight">Courses</th>
                      <th className="text-left px-6 py-3 text-xs font-medium text-[#575757] tracking-tight">Users Enrolled</th>
                      <th className="text-left px-6 py-3 text-xs font-medium text-[#575757] tracking-tight">Actions</th>
                    </tr>
                  </thead>
                  <tbody>
                    {category.skills.map((skill) => (
                      <tr key={skill.id} className="border-b border-[rgba(0,0,0,0.08)] last:border-0 hover:bg-[#FAFAFA] transition-colors">
                        <td className="px-6 py-4">
                          <p className="text-sm font-medium text-black tracking-tight">{skill.name}</p>
                        </td>
                        <td className="px-6 py-4">
                          <span className="inline-block px-3 py-1 rounded-full text-xs tracking-tight font-medium bg-[#003D82] text-white">
                            {skill.level}
                          </span>
                        </td>
                        <td className="px-6 py-4">
                          <p className="text-sm text-black tracking-tight">{skill.courses}</p>
                        </td>
                        <td className="px-6 py-4">
                          <p className="text-sm text-black tracking-tight">{skill.users.toLocaleString()}</p>
                        </td>
                        <td className="px-6 py-4">
                          <div className="flex items-center gap-2">
                            <button className="p-2 hover:bg-[#F5F5F5] rounded transition-colors">
                              <Edit2 className="w-4 h-4 text-[#575757]" strokeWidth={1.5} />
                            </button>
                            <button className="p-2 hover:bg-red-50 rounded transition-colors">
                              <Trash2 className="w-4 h-4 text-red-600" strokeWidth={1.5} />
                            </button>
                          </div>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </div>
          ))
        )}
      </div>
    </div>
  );
}
