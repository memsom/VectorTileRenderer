using System;
using System.Collections.Generic;

namespace ClipperLib
{
	public class PolyTree : PolyNode
	{
		internal List<PolyNode> m_AllPolys = new List<PolyNode>();

		public int Total
		{
			get
			{
				return this.m_AllPolys.Count;
			}
		}

		public PolyTree()
		{
		}

		public void Clear()
		{
			for (int i = 0; i < this.m_AllPolys.Count; i++)
			{
				this.m_AllPolys[i] = null;
			}
			this.m_AllPolys.Clear();
			this.m_Childs.Clear();
		}

		public PolyNode GetFirst()
		{
			if (this.m_Childs.Count <= 0)
			{
				return null;
			}
			return this.m_Childs[0];
		}
	}
}