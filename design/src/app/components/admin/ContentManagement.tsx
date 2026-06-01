import { Search, Plus, Filter, MoreVertical, Eye, Edit2, Trash2, Loader2 } from 'lucide-react';
import { useState, useEffect } from 'react';

interface ContentManagementProps {
  authToken: string;
  refreshTrigger: number;
}

export function ContentManagement({ authToken, refreshTrigger }: ContentManagementProps) {
  const [searchQuery, setSearchQuery] = useState('');
  const [selectedType, setSelectedType] = useState('all');
  const [contentList, setContentList] = useState<any[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    async function fetchContent() {
      if (!authToken) return;
      setLoading(true);
      setError(null);
      try {
        const response = await fetch('http://localhost:5126/api/learning-content?pageSize=100', {
          headers: {
            'Authorization': `Bearer ${authToken}`,
            'X-Tenant-Slug': 'demo'
          }
        });
        if (!response.ok) {
          throw new Error(`Failed to fetch content: status ${response.status}`);
        }
        const resData = await response.json();
        if (resData.success && resData.data && resData.data.items) {
          setContentList(resData.data.items);
        } else {
          setContentList([]);
        }
      } catch (err: any) {
        console.error(err);
        setError(err.message || 'Failed to fetch content');
      } finally {
        setLoading(false);
      }
    }
    fetchContent();
  }, [authToken, refreshTrigger]);

  const filteredContent = contentList.filter((item) => {
    const title = (item.title || '').toLowerCase();
    const author = (item.author || item.contentSource || '').toLowerCase();
    const category = (item.category || '').toLowerCase();
    
    const matchesSearch = title.includes(searchQuery.toLowerCase()) ||
                          author.includes(searchQuery.toLowerCase()) ||
                          category.includes(searchQuery.toLowerCase());
                          
    const matchesType = selectedType === 'all' || (item.contentType || '').toLowerCase() === selectedType;
    return matchesSearch && matchesType;
  });

  const totalContent = contentList.length;
  const publishedCount = contentList.filter(c => (c.status || '').toLowerCase() === 'published').length;
  const draftCount = contentList.filter(c => (c.status || '').toLowerCase() === 'draft').length;
  const inReviewCount = contentList.filter(c => (c.status || '').toLowerCase() === 'inreview' || (c.status || '').toLowerCase() === 'in_review').length;

  return (
    <div className="p-8">
      {/* Header */}
      <div className="mb-8">
        <h2 className="text-3xl font-medium text-black tracking-tight mb-2">Content Management</h2>
        <p className="text-sm text-[#575757] tracking-tight">Manage courses, articles, videos, and learning materials</p>
      </div>

      {/* Stats */}
      <div className="grid grid-cols-4 gap-6 mb-8">
        <div className="bg-white border border-[rgba(0,0,0,0.08)] rounded-lg p-6">
          <p className="text-xs text-[#575757] tracking-tight mb-2">Total Content</p>
          <p className="text-3xl font-medium text-black tracking-tight">{totalContent}</p>
          <p className="text-xs text-[#575757] tracking-tight mt-2">All types</p>
        </div>
        <div className="bg-white border border-[rgba(0,0,0,0.08)] rounded-lg p-6">
          <p className="text-xs text-[#575757] tracking-tight mb-2">Published</p>
          <p className="text-3xl font-medium text-black tracking-tight">{publishedCount}</p>
          <p className="text-xs text-green-600 tracking-tight mt-2">
            {totalContent > 0 ? `${Math.round((publishedCount / totalContent) * 100)}%` : '0%'} published
          </p>
        </div>
        <div className="bg-white border border-[rgba(0,0,0,0.08)] rounded-lg p-6">
          <p className="text-xs text-[#575757] tracking-tight mb-2">Drafts</p>
          <p className="text-3xl font-medium text-black tracking-tight">{draftCount}</p>
          <p className="text-xs text-[#575757] tracking-tight mt-2">Pending edits</p>
        </div>
        <div className="bg-white border border-[rgba(0,0,0,0.08)] rounded-lg p-6">
          <p className="text-xs text-[#575757] tracking-tight mb-2">In Review</p>
          <p className="text-3xl font-medium text-black tracking-tight">{inReviewCount}</p>
          <p className="text-xs text-amber-600 tracking-tight mt-2">Needs authorization</p>
        </div>
      </div>

      {/* Filters and Actions */}
      <div className="bg-white border border-[rgba(0,0,0,0.08)] rounded-lg p-6 mb-6">
        <div className="flex items-center gap-4 mb-4">
          <div className="flex-1 relative">
            <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-5 h-5 text-[#A3A3A3]" strokeWidth={1.5} />
            <input
              type="text"
              placeholder="Search content by title, author, or category..."
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              className="w-full pl-10 pr-4 py-2 bg-[#F5F5F5] text-sm text-black placeholder-[#A3A3A3] tracking-tight focus:outline-none focus:bg-[#EFEFEF] transition-colors rounded-lg"
            />
          </div>
          <button className="flex items-center gap-2 px-4 py-2 bg-[#F5F5F5] text-sm text-black tracking-tight hover:bg-[#E5E5E5] transition-colors rounded-lg">
            <Filter className="w-4 h-4" strokeWidth={1.5} />
            Filters
          </button>
          <button className="flex items-center gap-2 px-4 py-2 bg-[#003D82] text-white text-sm tracking-tight hover:bg-[#002D62] transition-colors rounded-lg">
            <Plus className="w-4 h-4" strokeWidth={1.5} />
            Add Content
          </button>
        </div>

        {/* Type Filter Tabs */}
        <div className="flex items-center gap-2">
          {['all', 'document', 'scorm', 'docebo', 'activity', 'ilt'].map((type) => (
            <button
              key={type}
              onClick={() => setSelectedType(type.toLowerCase())}
              className={`px-4 py-2 text-sm tracking-tight transition-colors rounded-lg ${
                selectedType === type.toLowerCase()
                  ? 'bg-[#003D82] text-white'
                  : 'bg-[#F5F5F5] text-[#575757] hover:bg-[#E5E5E5]'
              }`}
            >
              {type === 'all' ? 'All Types' : type.toUpperCase()}
            </button>
          ))}
        </div>
      </div>

      {/* Content Table */}
      <div className="bg-white border border-[rgba(0,0,0,0.08)] rounded-lg overflow-hidden relative min-h-[200px]">
        {loading && (
          <div className="absolute inset-0 bg-white/50 backdrop-blur-[1px] flex items-center justify-center z-10">
            <Loader2 className="w-8 h-8 animate-spin text-[#003D82]" />
          </div>
        )}

        {error && (
          <div className="p-6 text-center text-sm text-red-600 bg-red-50 border-b border-red-100">
            {error}
          </div>
        )}

        <table className="w-full">
          <thead className="bg-[#F5F5F5] border-b border-[rgba(0,0,0,0.08)]">
            <tr>
              <th className="text-left px-6 py-4 text-xs font-medium text-[#575757] tracking-tight">Title</th>
              <th className="text-left px-6 py-4 text-xs font-medium text-[#575757] tracking-tight">Type</th>
              <th className="text-left px-6 py-4 text-xs font-medium text-[#575757] tracking-tight">Category</th>
              <th className="text-left px-6 py-4 text-xs font-medium text-[#575757] tracking-tight">Author</th>
              <th className="text-left px-6 py-4 text-xs font-medium text-[#575757] tracking-tight">Status</th>
              <th className="text-left px-6 py-4 text-xs font-medium text-[#575757] tracking-tight">Source</th>
              <th className="text-left px-6 py-4 text-xs font-medium text-[#575757] tracking-tight">Duration</th>
              <th className="text-left px-6 py-4 text-xs font-medium text-[#575757] tracking-tight">Actions</th>
            </tr>
          </thead>
          <tbody>
            {!loading && filteredContent.length === 0 ? (
              <tr>
                <td colSpan={8} className="px-6 py-10 text-center text-sm text-[#575757]">
                  No content items found
                </td>
              </tr>
            ) : (
              filteredContent.map((item) => (
                <tr key={item.id} className="border-b border-[rgba(0,0,0,0.08)] hover:bg-[#FAFAFA] transition-colors">
                  <td className="px-6 py-4">
                    <p className="text-sm font-medium text-black tracking-tight max-w-xs">{item.title}</p>
                    <p className="text-xs text-[#575757] tracking-tight mt-0.5">
                      Updated {new Date(item.updatedAt || item.createdAt).toLocaleDateString()}
                    </p>
                  </td>
                  <td className="px-6 py-4">
                    <span className="inline-block px-3 py-1 rounded-full text-xs tracking-tight font-medium bg-[#F5F5F5] text-[#575757] uppercase">
                      {item.contentType}
                    </span>
                  </td>
                  <td className="px-6 py-4">
                    <p className="text-sm text-black tracking-tight">{item.category || 'General'}</p>
                  </td>
                  <td className="px-6 py-4">
                    <p className="text-sm text-black tracking-tight">{item.author || 'System'}</p>
                  </td>
                  <td className="px-6 py-4">
                    <span className={`inline-block px-3 py-1 rounded-full text-xs tracking-tight font-medium ${
                      (item.status || '').toLowerCase() === 'published' ? 'bg-green-100 text-green-700' : 'bg-yellow-100 text-yellow-700'
                    }`}>
                      {item.status}
                    </span>
                  </td>
                  <td className="px-6 py-4">
                    <p className="text-sm text-[#575757] tracking-tight">{item.contentSource || 'Local'}</p>
                  </td>
                  <td className="px-6 py-4">
                    <p className="text-sm text-black tracking-tight">
                      {item.estimatedDurationMinutes ? `${item.estimatedDurationMinutes} mins` : 'N/A'}
                    </p>
                  </td>
                  <td className="px-6 py-4">
                    <div className="flex items-center gap-1">
                      <button className="p-2 hover:bg-[#F5F5F5] rounded transition-colors">
                        <Eye className="w-4 h-4 text-[#575757]" strokeWidth={1.5} />
                      </button>
                      <button className="p-2 hover:bg-[#F5F5F5] rounded transition-colors">
                        <Edit2 className="w-4 h-4 text-[#575757]" strokeWidth={1.5} />
                      </button>
                      <button className="p-2 hover:bg-red-50 rounded transition-colors">
                        <Trash2 className="w-4 h-4 text-red-600" strokeWidth={1.5} />
                      </button>
                    </div>
                  </td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>
    </div>
  );
}
