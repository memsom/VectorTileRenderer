using System;
using System.Collections.Generic;

namespace ClipperLib
{
	public class PolyNode
	{
		internal PolyNode m_Parent;

		internal List<IntPoint> m_polygon = new List<IntPoint>();

		internal int m_Index;

		internal List<PolyNode> m_Childs = new List<PolyNode>();

		public int ChildCount
		{
			get
			{
				return this.m_Childs.Count;
			}
		}

		public List<PolyNode> Childs
		{
			get
			{
				return this.m_Childs;
			}
		}

		public List<IntPoint> Contour
		{
			get
			{
				return this.m_polygon;
			}
		}

		public bool IsHole
		{
			get
			{
				return this.IsHoleNode();
			}
		}

		public PolyNode Parent
		{
			get
			{
				return this.m_Parent;
			}
		}

		public PolyNode()
		{
		}

		internal void AddChild(PolyNode Child)
		{
			int count = this.m_Childs.Count;
			this.m_Childs.Add(Child);
			Child.m_Parent = this;
			Child.m_Index = count;
		}

		public PolyNode GetNext()
		{
			if (this.m_Childs.Count <= 0)
			{
				return this.GetNextSiblingUp();
			}
			return this.m_Childs[0];
		}

		internal PolyNode GetNextSiblingUp()
		{
			if (this.m_Parent == null)
			{
				return null;
			}
			if (this.m_Index == this.m_Parent.m_Childs.Count - 1)
			{
				return this.m_Parent.GetNextSiblingUp();
			}
			return this.m_Parent.m_Childs[this.m_Index + 1];
		}

		private bool IsHoleNode()
		{
			bool flag = true;
			for (PolyNode i = this.m_Parent; i != null; i = i.m_Parent)
			{
				flag = !flag;
			}
			return flag;
		}
	}
}