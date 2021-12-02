using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace ClipperLib
{
	public class Clipper : ClipperBase
	{
		private List<OutRec> m_PolyOuts;

		private ClipType m_ClipType;

		private Scanbeam m_Scanbeam;

		private TEdge m_ActiveEdges;

		private TEdge m_SortedEdges;

		private IntersectNode m_IntersectNodes;

		private bool m_ExecuteLocked;

		private PolyFillType m_ClipFillType;

		private PolyFillType m_SubjFillType;

		private List<JoinRec> m_Joins;

		private List<HorzJoinRec> m_HorizJoins;

		private bool m_ReverseOutput;

		private bool m_UsingPolyTree;

		public bool ReverseSolution
		{
			get
			{
				return this.m_ReverseOutput;
			}
			set
			{
				this.m_ReverseOutput = value;
			}
		}

		public Clipper()
		{
			this.m_Scanbeam = null;
			this.m_ActiveEdges = null;
			this.m_SortedEdges = null;
			this.m_IntersectNodes = null;
			this.m_ExecuteLocked = false;
			this.m_UsingPolyTree = false;
			this.m_PolyOuts = new List<OutRec>();
			this.m_Joins = new List<JoinRec>();
			this.m_HorizJoins = new List<HorzJoinRec>();
			this.m_ReverseOutput = false;
		}

		private void AddEdgeToSEL(TEdge edge)
		{
			if (this.m_SortedEdges == null)
			{
				this.m_SortedEdges = edge;
				edge.prevInSEL = null;
				edge.nextInSEL = null;
				return;
			}
			edge.nextInSEL = this.m_SortedEdges;
			edge.prevInSEL = null;
			this.m_SortedEdges.prevInSEL = edge;
			this.m_SortedEdges = edge;
		}

		private void AddHorzJoin(TEdge e, int idx)
		{
			HorzJoinRec horzJoinRec = new HorzJoinRec()
			{
				edge = e,
				savedIdx = idx
			};
			this.m_HorizJoins.Add(horzJoinRec);
		}

		private void AddIntersectNode(TEdge e1, TEdge e2, IntPoint pt)
		{
			IntersectNode intersectNode = new IntersectNode()
			{
				edge1 = e1,
				edge2 = e2,
				pt = pt,
				next = null
			};
			if (this.m_IntersectNodes == null)
			{
				this.m_IntersectNodes = intersectNode;
				return;
			}
			if (this.ProcessParam1BeforeParam2(intersectNode, this.m_IntersectNodes))
			{
				intersectNode.next = this.m_IntersectNodes;
				this.m_IntersectNodes = intersectNode;
				return;
			}
			IntersectNode mIntersectNodes = this.m_IntersectNodes;
			while (mIntersectNodes.next != null && this.ProcessParam1BeforeParam2(mIntersectNodes.next, intersectNode))
			{
				mIntersectNodes = mIntersectNodes.next;
			}
			intersectNode.next = mIntersectNodes.next;
			mIntersectNodes.next = intersectNode;
		}

		private void AddJoin(TEdge e1, TEdge e2, int e1OutIdx, int e2OutIdx)
		{
			JoinRec joinRec = new JoinRec();
			if (e1OutIdx < 0)
			{
				joinRec.poly1Idx = e1.outIdx;
			}
			else
			{
				joinRec.poly1Idx = e1OutIdx;
			}
			joinRec.pt1a = new IntPoint(e1.xcurr, e1.ycurr);
			joinRec.pt1b = new IntPoint(e1.xtop, e1.ytop);
			if (e2OutIdx < 0)
			{
				joinRec.poly2Idx = e2.outIdx;
			}
			else
			{
				joinRec.poly2Idx = e2OutIdx;
			}
			joinRec.pt2a = new IntPoint(e2.xcurr, e2.ycurr);
			joinRec.pt2b = new IntPoint(e2.xtop, e2.ytop);
			this.m_Joins.Add(joinRec);
		}

		private void AddLocalMaxPoly(TEdge e1, TEdge e2, IntPoint pt)
		{
			this.AddOutPt(e1, pt);
			if (e1.outIdx == e2.outIdx)
			{
				e1.outIdx = -1;
				e2.outIdx = -1;
				return;
			}
			if (e1.outIdx < e2.outIdx)
			{
				this.AppendPolygon(e1, e2);
				return;
			}
			this.AppendPolygon(e2, e1);
		}

		private void AddLocalMinPoly(TEdge e1, TEdge e2, IntPoint pt)
		{
			TEdge tEdge;
			TEdge tEdge1;
			if (e2.dx == -3.4E+38 || e1.dx > e2.dx)
			{
				this.AddOutPt(e1, pt);
				e2.outIdx = e1.outIdx;
				e1.side = EdgeSide.esLeft;
				e2.side = EdgeSide.esRight;
				tEdge = e1;
				tEdge1 = (tEdge.prevInAEL != e2 ? tEdge.prevInAEL : e2.prevInAEL);
			}
			else
			{
				this.AddOutPt(e2, pt);
				e1.outIdx = e2.outIdx;
				e1.side = EdgeSide.esRight;
				e2.side = EdgeSide.esLeft;
				tEdge = e2;
				tEdge1 = (tEdge.prevInAEL != e1 ? tEdge.prevInAEL : e1.prevInAEL);
			}
			if (tEdge1 != null && tEdge1.outIdx >= 0 && Clipper.TopX(tEdge1, pt.Y) == Clipper.TopX(tEdge, pt.Y) && base.SlopesEqual(tEdge, tEdge1, this.m_UseFullRange))
			{
				this.AddJoin(tEdge, tEdge1, -1, -1);
			}
		}

		private void AddOutPt(TEdge e, IntPoint pt)
		{
			bool flag = e.side == EdgeSide.esLeft;
			if (e.outIdx < 0)
			{
				OutRec count = this.CreateOutRec();
				this.m_PolyOuts.Add(count);
				count.idx = this.m_PolyOuts.Count - 1;
				e.outIdx = count.idx;
				OutPt outPt = new OutPt();
				count.pts = outPt;
				count.bottomPt = outPt;
				outPt.pt = pt;
				outPt.idx = count.idx;
				outPt.next = outPt;
				outPt.prev = outPt;
				this.SetHoleState(e, count);
				return;
			}
			OutRec item = this.m_PolyOuts[e.outIdx];
			OutPt outPt1 = item.pts;
			if (flag && ClipperBase.PointsEqual(pt, outPt1.pt) || !flag && ClipperBase.PointsEqual(pt, outPt1.prev.pt))
			{
				return;
			}
			OutPt outPt2 = new OutPt()
			{
				pt = pt,
				idx = item.idx
			};
			if (outPt2.pt.Y == item.bottomPt.pt.Y && outPt2.pt.X < item.bottomPt.pt.X)
			{
				item.bottomPt = outPt2;
			}
			outPt2.next = outPt1;
			outPt2.prev = outPt1.prev;
			outPt2.prev.next = outPt2;
			outPt1.prev = outPt2;
			if (flag)
			{
				item.pts = outPt2;
			}
		}

		public static void AddPolyNodeToPolygons(PolyNode polynode, List<List<IntPoint>> polygons)
		{
			if (polynode.Contour.Count > 0)
			{
				polygons.Add(polynode.Contour);
			}
			foreach (PolyNode child in polynode.Childs)
			{
				Clipper.AddPolyNodeToPolygons(child, polygons);
			}
		}

		private void AppendPolygon(TEdge e1, TEdge e2)
		{
			OutRec outRec;
			EdgeSide edgeSide;
			OutRec item = this.m_PolyOuts[e1.outIdx];
			OutRec item1 = this.m_PolyOuts[e2.outIdx];
			if (!this.Param1RightOfParam2(item, item1))
			{
				outRec = (!this.Param1RightOfParam2(item1, item) ? this.GetLowermostRec(item, item1) : item);
			}
			else
			{
				outRec = item1;
			}
			OutPt outPt = item.pts;
			OutPt outPt1 = outPt.prev;
			OutPt outPt2 = item1.pts;
			OutPt outPt3 = outPt2.prev;
			if (e1.side != EdgeSide.esLeft)
			{
				if (e2.side != EdgeSide.esRight)
				{
					outPt1.next = outPt2;
					outPt2.prev = outPt1;
					outPt.prev = outPt3;
					outPt3.next = outPt;
				}
				else
				{
					this.ReversePolyPtLinks(outPt2);
					outPt1.next = outPt3;
					outPt3.prev = outPt1;
					outPt2.next = outPt;
					outPt.prev = outPt2;
				}
				edgeSide = EdgeSide.esRight;
			}
			else
			{
				if (e2.side != EdgeSide.esLeft)
				{
					outPt3.next = outPt;
					outPt.prev = outPt3;
					outPt2.prev = outPt1;
					outPt1.next = outPt2;
					item.pts = outPt2;
				}
				else
				{
					this.ReversePolyPtLinks(outPt2);
					outPt2.next = outPt;
					outPt.prev = outPt2;
					outPt1.next = outPt3;
					outPt3.prev = outPt1;
					item.pts = outPt3;
				}
				edgeSide = EdgeSide.esLeft;
			}
			if (outRec == item1)
			{
				item.bottomPt = item1.bottomPt;
				item.bottomPt.idx = item.idx;
				if (item1.FirstLeft != item)
				{
					item.FirstLeft = item1.FirstLeft;
				}
				item.isHole = item1.isHole;
			}
			item1.pts = null;
			item1.bottomPt = null;
			item1.FirstLeft = item;
			int num = e1.outIdx;
			int num1 = e2.outIdx;
			e1.outIdx = -1;
			e2.outIdx = -1;
			TEdge mActiveEdges = this.m_ActiveEdges;
			while (mActiveEdges != null)
			{
				if (mActiveEdges.outIdx != num1)
				{
					mActiveEdges = mActiveEdges.nextInAEL;
				}
				else
				{
					mActiveEdges.outIdx = num;
					mActiveEdges.side = edgeSide;
					break;
				}
			}
			for (int i = 0; i < this.m_Joins.Count; i++)
			{
				if (this.m_Joins[i].poly1Idx == num1)
				{
					this.m_Joins[i].poly1Idx = num;
				}
				if (this.m_Joins[i].poly2Idx == num1)
				{
					this.m_Joins[i].poly2Idx = num;
				}
			}
			for (int j = 0; j < this.m_HorizJoins.Count; j++)
			{
				if (this.m_HorizJoins[j].savedIdx == num1)
				{
					this.m_HorizJoins[j].savedIdx = num;
				}
			}
		}

		public static double Area(List<IntPoint> poly)
		{
			int count = poly.Count - 1;
			if (count < 2)
			{
				return 0;
			}
			if (Clipper.FullRangeNeeded(poly))
			{
				Int128 int128 = new Int128((long)0);
				int128 = Int128.Int128Mul(poly[count].X + poly[0].X, poly[0].Y - poly[count].Y);
				for (int i = 1; i <= count; i++)
				{
					int128 += Int128.Int128Mul(poly[i - 1].X + poly[i].X, poly[i].Y - poly[i - 1].Y);
				}
				return int128.ToDouble() / 2;
			}
			double x = ((double)poly[count].X + (double)poly[0].X) * ((double)poly[0].Y - (double)poly[count].Y);
			for (int j = 1; j <= count; j++)
			{
				x = x + ((double)poly[j - 1].X + (double)poly[j].X) * ((double)poly[j].Y - (double)poly[j - 1].Y);
			}
			return x / 2;
		}

		private double Area(OutRec outRec, bool UseFull64BitRange)
		{
			OutPt outPt = outRec.pts;
			if (outPt == null)
			{
				return 0;
			}
			if (!UseFull64BitRange)
			{
				double x = 0;
				do
				{
					x += (double)((outPt.pt.X + outPt.prev.pt.X) * (outPt.prev.pt.Y - outPt.pt.Y));
					outPt = outPt.next;
				}
				while (outPt != outRec.pts);
				return x / 2;
			}
			Int128 int128 = new Int128((long)0);
			do
			{
				int128 += Int128.Int128Mul(outPt.pt.X + outPt.prev.pt.X, outPt.prev.pt.Y - outPt.pt.Y);
				outPt = outPt.next;
			}
			while (outPt != outRec.pts);
			return int128.ToDouble() / 2;
		}

		internal static List<IntPoint> BuildArc(IntPoint pt, double a1, double a2, double r)
		{
			long num = (long)Math.Max(6, (int)(Math.Sqrt(Math.Abs(r)) * Math.Abs(a2 - a1)));
			if (num > (long)256)
			{
				num = (long)256;
			}
			int num1 = (int)num;
			List<IntPoint> intPoints = new List<IntPoint>(num1);
			double num2 = (a2 - a1) / (double)(num1 - 1);
			double num3 = a1;
			for (int i = 0; i < num1; i++)
			{
				intPoints.Add(new IntPoint(pt.X + Clipper.Round(Math.Cos(num3) * r), pt.Y + Clipper.Round(Math.Sin(num3) * r)));
				num3 += num2;
			}
			return intPoints;
		}

		private void BuildIntersectList(long botY, long topY)
		{
			if (this.m_ActiveEdges == null)
			{
				return;
			}
			TEdge mActiveEdges = this.m_ActiveEdges;
			this.m_SortedEdges = mActiveEdges;
			while (mActiveEdges != null)
			{
				mActiveEdges.prevInSEL = mActiveEdges.prevInAEL;
				mActiveEdges.nextInSEL = mActiveEdges.nextInAEL;
				mActiveEdges.tmpX = Clipper.TopX(mActiveEdges, topY);
				mActiveEdges = mActiveEdges.nextInAEL;
			}
			bool flag = true;
			while (flag && this.m_SortedEdges != null)
			{
				flag = false;
				mActiveEdges = this.m_SortedEdges;
				while (mActiveEdges.nextInSEL != null)
				{
					TEdge tEdge = mActiveEdges.nextInSEL;
					IntPoint intPoint = new IntPoint();
					if (mActiveEdges.tmpX <= tEdge.tmpX || !this.IntersectPoint(mActiveEdges, tEdge, ref intPoint))
					{
						mActiveEdges = tEdge;
					}
					else
					{
						if (intPoint.Y > botY)
						{
							intPoint.Y = botY;
							intPoint.X = Clipper.TopX(mActiveEdges, intPoint.Y);
						}
						this.AddIntersectNode(mActiveEdges, tEdge, intPoint);
						this.SwapPositionsInSEL(mActiveEdges, tEdge);
						flag = true;
					}
				}
				if (mActiveEdges.prevInSEL == null)
				{
					break;
				}
				mActiveEdges.prevInSEL.nextInSEL = null;
			}
			this.m_SortedEdges = null;
		}

		private void BuildResult(List<List<IntPoint>> polyg)
		{
			polyg.Clear();
			polyg.Capacity = this.m_PolyOuts.Count;
			for (int i = 0; i < this.m_PolyOuts.Count; i++)
			{
				OutRec item = this.m_PolyOuts[i];
				if (item.pts != null)
				{
					OutPt outPt = item.pts;
					int num = this.PointCount(outPt);
					if (num >= 3)
					{
						List<IntPoint> intPoints = new List<IntPoint>(num);
						for (int j = 0; j < num; j++)
						{
							intPoints.Add(outPt.pt);
							outPt = outPt.prev;
						}
						polyg.Add(intPoints);
					}
				}
			}
		}

		private void BuildResult2(PolyTree polytree)
		{
			polytree.Clear();
			polytree.m_AllPolys.Capacity = this.m_PolyOuts.Count;
			for (int i = 0; i < this.m_PolyOuts.Count; i++)
			{
				OutRec item = this.m_PolyOuts[i];
				int num = this.PointCount(item.pts);
				if (num >= 3)
				{
					this.FixHoleLinkage(item);
					PolyNode polyNode = new PolyNode();
					polytree.m_AllPolys.Add(polyNode);
					item.polyNode = polyNode;
					polyNode.m_polygon.Capacity = num;
					OutPt outPt = item.pts;
					for (int j = 0; j < num; j++)
					{
						polyNode.m_polygon.Add(outPt.pt);
						outPt = outPt.prev;
					}
				}
			}
			polytree.m_Childs.Capacity = this.m_PolyOuts.Count;
			for (int k = 0; k < this.m_PolyOuts.Count; k++)
			{
				OutRec count = this.m_PolyOuts[k];
				if (count.polyNode != null)
				{
					if (count.FirstLeft != null)
					{
						count.FirstLeft.polyNode.AddChild(count.polyNode);
					}
					else
					{
						count.polyNode.m_Index = polytree.m_Childs.Count;
						polytree.m_Childs.Add(count.polyNode);
						count.polyNode.m_Parent = polytree;
					}
				}
			}
		}

		public static List<IntPoint> CleanPolygon(List<IntPoint> poly, double delta = 1.415)
		{
			int count = poly.Count;
			if (count < 3)
			{
				return null;
			}
			List<IntPoint> intPoints = new List<IntPoint>(poly);
			int num = (int)(delta * delta);
			IntPoint item = poly[0];
			int num1 = 1;
			for (int i = 1; i < count; i++)
			{
				if ((poly[i].X - item.X) * (poly[i].X - item.X) + (poly[i].Y - item.Y) * (poly[i].Y - item.Y) > (long)num)
				{
					intPoints[num1] = poly[i];
					item = poly[i];
					num1++;
				}
			}
			item = poly[num1 - 1];
			if ((poly[0].X - item.X) * (poly[0].X - item.X) + (poly[0].Y - item.Y) * (poly[0].Y - item.Y) <= (long)num)
			{
				num1--;
			}
			if (num1 < count)
			{
				intPoints.RemoveRange(num1, count - num1);
			}
			return intPoints;
		}

		public override void Clear()
		{
			if (this.m_edges.Count == 0)
			{
				return;
			}
			this.DisposeAllPolyPts();
			base.Clear();
		}

		private void CopyAELToSEL()
		{
			TEdge mActiveEdges = this.m_ActiveEdges;
			this.m_SortedEdges = mActiveEdges;
			if (this.m_ActiveEdges == null)
			{
				return;
			}
			this.m_SortedEdges.prevInSEL = null;
			for (mActiveEdges = mActiveEdges.nextInAEL; mActiveEdges != null; mActiveEdges = mActiveEdges.nextInAEL)
			{
				mActiveEdges.prevInSEL = mActiveEdges.prevInAEL;
				mActiveEdges.prevInSEL.nextInSEL = mActiveEdges;
				mActiveEdges.nextInSEL = null;
			}
		}

		private OutRec CreateOutRec()
		{
			OutRec outRec = new OutRec()
			{
				idx = -1,
				isHole = false,
				FirstLeft = null,
				pts = null,
				bottomPt = null,
				polyNode = null
			};
			return outRec;
		}

		private void DeleteFromAEL(TEdge e)
		{
			TEdge tEdge = e.prevInAEL;
			TEdge tEdge1 = e.nextInAEL;
			if (tEdge == null && tEdge1 == null && e != this.m_ActiveEdges)
			{
				return;
			}
			if (tEdge == null)
			{
				this.m_ActiveEdges = tEdge1;
			}
			else
			{
				tEdge.nextInAEL = tEdge1;
			}
			if (tEdge1 != null)
			{
				tEdge1.prevInAEL = tEdge;
			}
			e.nextInAEL = null;
			e.prevInAEL = null;
		}

		private void DeleteFromSEL(TEdge e)
		{
			TEdge tEdge = e.prevInSEL;
			TEdge tEdge1 = e.nextInSEL;
			if (tEdge == null && tEdge1 == null && e != this.m_SortedEdges)
			{
				return;
			}
			if (tEdge == null)
			{
				this.m_SortedEdges = tEdge1;
			}
			else
			{
				tEdge.nextInSEL = tEdge1;
			}
			if (tEdge1 != null)
			{
				tEdge1.prevInSEL = tEdge;
			}
			e.nextInSEL = null;
			e.prevInSEL = null;
		}

		private void DisposeAllPolyPts()
		{
			for (int i = 0; i < this.m_PolyOuts.Count; i++)
			{
				this.DisposeOutRec(i);
			}
			this.m_PolyOuts.Clear();
		}

		private void DisposeIntersectNodes()
		{
			while (this.m_IntersectNodes != null)
			{
				IntersectNode mIntersectNodes = this.m_IntersectNodes.next;
				this.m_IntersectNodes = null;
				this.m_IntersectNodes = mIntersectNodes;
			}
		}

		private void DisposeOutPts(OutPt pp)
		{
			if (pp == null)
			{
				return;
			}
			pp.prev.next = null;
			while (pp != null)
			{
				pp = pp.next;
			}
		}

		private void DisposeOutRec(int index)
		{
			OutRec item = this.m_PolyOuts[index];
			if (item.pts != null)
			{
				this.DisposeOutPts(item.pts);
			}
			item = null;
			this.m_PolyOuts[index] = null;
		}

		private void DisposeScanbeamList()
		{
			while (this.m_Scanbeam != null)
			{
				Scanbeam mScanbeam = this.m_Scanbeam.next;
				this.m_Scanbeam = null;
				this.m_Scanbeam = mScanbeam;
			}
		}

		private void DoBothEdges(TEdge edge1, TEdge edge2, IntPoint pt)
		{
			this.AddOutPt(edge1, pt);
			this.AddOutPt(edge2, pt);
			Clipper.SwapSides(edge1, edge2);
			Clipper.SwapPolyIndexes(edge1, edge2);
		}

		private void DoEdge1(TEdge edge1, TEdge edge2, IntPoint pt)
		{
			this.AddOutPt(edge1, pt);
			Clipper.SwapSides(edge1, edge2);
			Clipper.SwapPolyIndexes(edge1, edge2);
		}

		private void DoEdge2(TEdge edge1, TEdge edge2, IntPoint pt)
		{
			this.AddOutPt(edge2, pt);
			Clipper.SwapSides(edge1, edge2);
			Clipper.SwapPolyIndexes(edge1, edge2);
		}

		private void DoMaxima(TEdge e, long topY)
		{
			TEdge maximaPair = this.GetMaximaPair(e);
			long num = e.xtop;
			for (TEdge i = e.nextInAEL; i != maximaPair; i = i.nextInAEL)
			{
				if (i == null)
				{
					throw new ClipperException("DoMaxima error");
				}
				this.IntersectEdges(e, i, new IntPoint(num, topY), Protects.ipBoth);
				this.SwapPositionsInAEL(e, i);
			}
			if (e.outIdx < 0 && maximaPair.outIdx < 0)
			{
				this.DeleteFromAEL(e);
				this.DeleteFromAEL(maximaPair);
				return;
			}
			if (e.outIdx < 0 || maximaPair.outIdx < 0)
			{
				throw new ClipperException("DoMaxima error");
			}
			this.IntersectEdges(e, maximaPair, new IntPoint(num, topY), Protects.ipNone);
		}

		private bool E2InsertsBeforeE1(TEdge e1, TEdge e2)
		{
			if (e2.xcurr != e1.xcurr)
			{
				return e2.xcurr < e1.xcurr;
			}
			return e2.dx > e1.dx;
		}

		public bool Execute(ClipType clipType, List<List<IntPoint>> solution, PolyFillType subjFillType, PolyFillType clipFillType)
		{
			if (this.m_ExecuteLocked)
			{
				return false;
			}
			this.m_ExecuteLocked = true;
			solution.Clear();
			this.m_SubjFillType = subjFillType;
			this.m_ClipFillType = clipFillType;
			this.m_ClipType = clipType;
			this.m_UsingPolyTree = false;
			bool flag = this.ExecuteInternal();
			if (flag)
			{
				this.BuildResult(solution);
			}
			this.m_ExecuteLocked = false;
			return flag;
		}

		public bool Execute(ClipType clipType, PolyTree polytree, PolyFillType subjFillType, PolyFillType clipFillType)
		{
			if (this.m_ExecuteLocked)
			{
				return false;
			}
			this.m_ExecuteLocked = true;
			this.m_SubjFillType = subjFillType;
			this.m_ClipFillType = clipFillType;
			this.m_ClipType = clipType;
			this.m_UsingPolyTree = true;
			bool flag = this.ExecuteInternal();
			if (flag)
			{
				this.BuildResult2(polytree);
			}
			this.m_ExecuteLocked = false;
			return flag;
		}

		public bool Execute(ClipType clipType, List<List<IntPoint>> solution)
		{
			return this.Execute(clipType, solution, PolyFillType.pftEvenOdd, PolyFillType.pftEvenOdd);
		}

		public bool Execute(ClipType clipType, PolyTree polytree)
		{
			return this.Execute(clipType, polytree, PolyFillType.pftEvenOdd, PolyFillType.pftEvenOdd);
		}

		private bool ExecuteInternal()
		{
			bool flag;
			try
			{
				this.Reset();
				if (this.m_CurrentLM != null)
				{
					long num = this.PopScanbeam();
					do
					{
						this.InsertLocalMinimaIntoAEL(num);
						this.m_HorizJoins.Clear();
						this.ProcessHorizontals();
						long num1 = this.PopScanbeam();
						flag = this.ProcessIntersections(num, num1);
						if (!flag)
						{
							break;
						}
						this.ProcessEdgesAtTopOfScanbeam(num1);
						num = num1;
					}
					while (this.m_Scanbeam != null);
				}
				else
				{
					return true;
				}
			}
			catch
			{
				flag = false;
			}
			if (flag)
			{
				for (int i = 0; i < this.m_PolyOuts.Count; i++)
				{
					OutRec item = this.m_PolyOuts[i];
					if (item.pts != null)
					{
						this.FixupOutPolygon(item);
						if (item.pts != null && (item.isHole ^ this.m_ReverseOutput) == this.Area(item, this.m_UseFullRange) > 0)
						{
							this.ReversePolyPtLinks(item.pts);
						}
					}
				}
				this.JoinCommonEdges();
			}
			this.m_Joins.Clear();
			this.m_HorizJoins.Clear();
			return flag;
		}

		private bool FindSegment(ref OutPt pp, ref IntPoint pt1, ref IntPoint pt2)
		{
			if (pp == null)
			{
				return false;
			}
			OutPt outPt = pp;
			IntPoint intPoint = new IntPoint(pt1);
			IntPoint intPoint1 = new IntPoint(pt2);
			do
			{
				if (base.SlopesEqual(intPoint, intPoint1, pp.pt, pp.prev.pt, true) && base.SlopesEqual(intPoint, intPoint1, pp.pt, true) && this.GetOverlapSegment(intPoint, intPoint1, pp.pt, pp.prev.pt, ref pt1, ref pt2))
				{
					return true;
				}
				pp = pp.next;
			}
			while (pp != outPt);
			return false;
		}

		private bool FirstIsBottomPt(OutPt btmPt1, OutPt btmPt2)
		{
			OutPt outPt = btmPt1.prev;
			while (ClipperBase.PointsEqual(outPt.pt, btmPt1.pt) && outPt != btmPt1)
			{
				outPt = outPt.prev;
			}
			double num = Math.Abs(this.GetDx(btmPt1.pt, outPt.pt));
			outPt = btmPt1.next;
			while (ClipperBase.PointsEqual(outPt.pt, btmPt1.pt) && outPt != btmPt1)
			{
				outPt = outPt.next;
			}
			double num1 = Math.Abs(this.GetDx(btmPt1.pt, outPt.pt));
			outPt = btmPt2.prev;
			while (ClipperBase.PointsEqual(outPt.pt, btmPt2.pt) && outPt != btmPt2)
			{
				outPt = outPt.prev;
			}
			double num2 = Math.Abs(this.GetDx(btmPt2.pt, outPt.pt));
			outPt = btmPt2.next;
			while (ClipperBase.PointsEqual(outPt.pt, btmPt2.pt) && outPt != btmPt2)
			{
				outPt = outPt.next;
			}
			double num3 = Math.Abs(this.GetDx(btmPt2.pt, outPt.pt));
			if (num >= num2 && num >= num3)
			{
				return true;
			}
			if (num1 < num2)
			{
				return false;
			}
			return num1 >= num3;
		}

		internal void FixHoleLinkage(OutRec outRec)
		{
			if (outRec.FirstLeft == null || outRec.isHole != outRec.FirstLeft.isHole && outRec.FirstLeft.pts != null)
			{
				return;
			}
			OutRec firstLeft = outRec.FirstLeft;
			while (firstLeft != null && (firstLeft.isHole == outRec.isHole || firstLeft.pts == null))
			{
				firstLeft = firstLeft.FirstLeft;
			}
			outRec.FirstLeft = firstLeft;
		}

		private void FixupFirstLefts1(OutRec OldOutRec, OutRec NewOutRec)
		{
			for (int i = 0; i < this.m_PolyOuts.Count; i++)
			{
				OutRec item = this.m_PolyOuts[i];
				if (item.pts != null && item.FirstLeft == OldOutRec && Poly2ContainsPoly1(item.pts, NewOutRec.pts))
				{
					item.FirstLeft = NewOutRec;
				}
			}
		}

		private void FixupFirstLefts2(OutRec OldOutRec, OutRec NewOutRec)
		{
			foreach (OutRec mPolyOut in this.m_PolyOuts)
			{
				if (mPolyOut.FirstLeft != OldOutRec)
				{
					continue;
				}
				mPolyOut.FirstLeft = NewOutRec;
			}
		}

		private bool FixupIntersections()
		{
			TEdge tEdge;
			if (this.m_IntersectNodes.next == null)
			{
				return true;
			}
			this.CopyAELToSEL();
			IntersectNode mIntersectNodes = this.m_IntersectNodes;
			for (IntersectNode i = this.m_IntersectNodes.next; i != null; i = mIntersectNodes.next)
			{
				TEdge tEdge1 = mIntersectNodes.edge1;
				if (tEdge1.prevInSEL == mIntersectNodes.edge2)
				{
					tEdge = tEdge1.prevInSEL;
				}
				else if (tEdge1.nextInSEL != mIntersectNodes.edge2)
				{
					while (i != null && i.edge1.nextInSEL != i.edge2 && i.edge1.prevInSEL != i.edge2)
					{
						i = i.next;
					}
					if (i == null)
					{
						return false;
					}
					this.SwapIntersectNodes(mIntersectNodes, i);
					tEdge1 = mIntersectNodes.edge1;
					tEdge = mIntersectNodes.edge2;
				}
				else
				{
					tEdge = tEdge1.nextInSEL;
				}
				this.SwapPositionsInSEL(tEdge1, tEdge);
				mIntersectNodes = mIntersectNodes.next;
			}
			this.m_SortedEdges = null;
			if (mIntersectNodes.edge1.prevInSEL == mIntersectNodes.edge2)
			{
				return true;
			}
			return mIntersectNodes.edge1.nextInSEL == mIntersectNodes.edge2;
		}

		private void FixupJoinRecs(JoinRec j, OutPt pt, int startIdx)
		{
			for (int i = startIdx; i < this.m_Joins.Count; i++)
			{
				JoinRec item = this.m_Joins[i];
				if (item.poly1Idx == j.poly1Idx && base.PointIsVertex(item.pt1a, pt))
				{
					item.poly1Idx = j.poly2Idx;
				}
				if (item.poly2Idx == j.poly1Idx && base.PointIsVertex(item.pt2a, pt))
				{
					item.poly2Idx = j.poly2Idx;
				}
			}
		}

		private void FixupOutPolygon(OutRec outRec)
		{
			OutPt outPt = null;
			outRec.pts = outRec.bottomPt;
			OutPt outPt1 = outRec.bottomPt;
			while (outPt1.prev != outPt1 && outPt1.prev != outPt1.next)
			{
				if (ClipperBase.PointsEqual(outPt1.pt, outPt1.next.pt) || base.SlopesEqual(outPt1.prev.pt, outPt1.pt, outPt1.next.pt, this.m_UseFullRange))
				{
					outPt = null;
					if (outPt1 == outRec.bottomPt)
					{
						outRec.bottomPt = null;
					}
					outPt1.prev.next = outPt1.next;
					outPt1.next.prev = outPt1.prev;
					outPt1 = outPt1.prev;
				}
				else
				{
					if (outPt1 == outPt)
					{
						if (outRec.bottomPt == null)
						{
							outRec.bottomPt = this.GetBottomPt(outPt1);
							outRec.bottomPt.idx = outRec.idx;
							outRec.pts = outRec.bottomPt;
						}
						return;
					}
					if (outPt == null)
					{
						outPt = outPt1;
					}
					outPt1 = outPt1.next;
				}
			}
			this.DisposeOutPts(outPt1);
			outRec.pts = null;
			outRec.bottomPt = null;
		}

		private static bool FullRangeNeeded(List<IntPoint> pts)
		{
			bool flag = false;
			for (int i = 0; i < pts.Count; i++)
			{
				if (Math.Abs(pts[i].X) > 4611686018427387903L || Math.Abs(pts[i].Y) > 4611686018427387903L)
				{
					throw new ClipperException("Coordinate exceeds range bounds.");
				}
				if (Math.Abs(pts[i].X) > (long)1073741823 || Math.Abs(pts[i].Y) > (long)1073741823)
				{
					flag = true;
				}
			}
			return flag;
		}

		private OutPt GetBottomPt(OutPt pp)
		{
			OutPt i;
			OutPt outPt = null;
			for (i = pp.next; i != pp; i = i.next)
			{
				if (i.pt.Y > pp.pt.Y)
				{
					pp = i;
					outPt = null;
				}
				else if (i.pt.Y == pp.pt.Y && i.pt.X <= pp.pt.X)
				{
					if (i.pt.X < pp.pt.X)
					{
						outPt = null;
						pp = i;
					}
					else if (i.next != pp && i.prev != pp)
					{
						outPt = i;
					}
				}
			}
			if (outPt != null)
			{
				while (outPt != i)
				{
					if (!this.FirstIsBottomPt(i, outPt))
					{
						pp = outPt;
					}
					outPt = outPt.next;
					while (!ClipperBase.PointsEqual(outPt.pt, pp.pt))
					{
						outPt = outPt.next;
					}
				}
			}
			return pp;
		}

		private double GetDx(IntPoint pt1, IntPoint pt2)
		{
			if (pt1.Y == pt2.Y)
			{
				return -3.4E+38;
			}
			return (double)(pt2.X - pt1.X) / (double)(pt2.Y - pt1.Y);
		}

		private OutRec GetLowermostRec(OutRec outRec1, OutRec outRec2)
		{
			OutPt outPt = outRec1.bottomPt;
			OutPt outPt1 = outRec2.bottomPt;
			if (outPt.pt.Y > outPt1.pt.Y)
			{
				return outRec1;
			}
			if (outPt.pt.Y < outPt1.pt.Y)
			{
				return outRec2;
			}
			if (outPt.pt.X < outPt1.pt.X)
			{
				return outRec1;
			}
			if (outPt.pt.X > outPt1.pt.X)
			{
				return outRec2;
			}
			if (outPt.next == outPt)
			{
				return outRec2;
			}
			if (outPt1.next == outPt1)
			{
				return outRec1;
			}
			if (this.FirstIsBottomPt(outPt, outPt1))
			{
				return outRec1;
			}
			return outRec2;
		}

		private TEdge GetMaximaPair(TEdge e)
		{
			if (this.IsMaxima(e.next, (double)e.ytop) && e.next.xtop == e.xtop)
			{
				return e.next;
			}
			return e.prev;
		}

		private TEdge GetNextInAEL(TEdge e, ClipperLib.Direction Direction)
		{
			if (Direction != ClipperLib.Direction.dLeftToRight)
			{
				return e.prevInAEL;
			}
			return e.nextInAEL;
		}

		private bool GetOverlapSegment(IntPoint pt1a, IntPoint pt1b, IntPoint pt2a, IntPoint pt2b, ref IntPoint pt1, ref IntPoint pt2)
		{
			if (Math.Abs(pt1a.X - pt1b.X) > Math.Abs(pt1a.Y - pt1b.Y))
			{
				if (pt1a.X > pt1b.X)
				{
					this.SwapPoints(ref pt1a, ref pt1b);
				}
				if (pt2a.X > pt2b.X)
				{
					this.SwapPoints(ref pt2a, ref pt2b);
				}
				if (pt1a.X <= pt2a.X)
				{
					pt1 = pt2a;
				}
				else
				{
					pt1 = pt1a;
				}
				if (pt1b.X >= pt2b.X)
				{
					pt2 = pt2b;
				}
				else
				{
					pt2 = pt1b;
				}
				return pt1.X < pt2.X;
			}
			if (pt1a.Y < pt1b.Y)
			{
				this.SwapPoints(ref pt1a, ref pt1b);
			}
			if (pt2a.Y < pt2b.Y)
			{
				this.SwapPoints(ref pt2a, ref pt2b);
			}
			if (pt1a.Y >= pt2a.Y)
			{
				pt1 = pt2a;
			}
			else
			{
				pt1 = pt1a;
			}
			if (pt1b.Y <= pt2b.Y)
			{
				pt2 = pt2b;
			}
			else
			{
				pt2 = pt1b;
			}
			return pt1.Y > pt2.Y;
		}

		internal static Clipper.DoublePoint GetUnitNormal(IntPoint pt1, IntPoint pt2)
		{
			double x = (double)(pt2.X - pt1.X);
			double y = (double)(pt2.Y - pt1.Y);
			if (x == 0 && y == 0)
			{
				return new Clipper.DoublePoint(0, 0);
			}
			double num = 1 / Math.Sqrt(x * x + y * y);
			x *= num;
			y *= num;
			return new Clipper.DoublePoint(y, -x);
		}

		private void InsertEdgeIntoAEL(TEdge edge)
		{
			edge.prevInAEL = null;
			edge.nextInAEL = null;
			if (this.m_ActiveEdges == null)
			{
				this.m_ActiveEdges = edge;
				return;
			}
			if (this.E2InsertsBeforeE1(this.m_ActiveEdges, edge))
			{
				edge.nextInAEL = this.m_ActiveEdges;
				this.m_ActiveEdges.prevInAEL = edge;
				this.m_ActiveEdges = edge;
				return;
			}
			TEdge mActiveEdges = this.m_ActiveEdges;
			while (mActiveEdges.nextInAEL != null && !this.E2InsertsBeforeE1(mActiveEdges.nextInAEL, edge))
			{
				mActiveEdges = mActiveEdges.nextInAEL;
			}
			edge.nextInAEL = mActiveEdges.nextInAEL;
			if (mActiveEdges.nextInAEL != null)
			{
				mActiveEdges.nextInAEL.prevInAEL = edge;
			}
			edge.prevInAEL = mActiveEdges;
			mActiveEdges.nextInAEL = edge;
		}

		private void InsertLocalMinimaIntoAEL(long botY)
		{
			while (this.m_CurrentLM != null && this.m_CurrentLM.Y == botY)
			{
				TEdge mCurrentLM = this.m_CurrentLM.leftBound;
				TEdge tEdge = this.m_CurrentLM.rightBound;
				this.InsertEdgeIntoAEL(mCurrentLM);
				this.InsertScanbeam(mCurrentLM.ytop);
				this.InsertEdgeIntoAEL(tEdge);
				if (!this.IsEvenOddFillType(mCurrentLM))
				{
					tEdge.windDelta = -mCurrentLM.windDelta;
				}
				else
				{
					mCurrentLM.windDelta = 1;
					tEdge.windDelta = 1;
				}
				this.SetWindingCount(mCurrentLM);
				tEdge.windCnt = mCurrentLM.windCnt;
				tEdge.windCnt2 = mCurrentLM.windCnt2;
				if (tEdge.dx != -3.4E+38)
				{
					this.InsertScanbeam(tEdge.ytop);
				}
				else
				{
					this.AddEdgeToSEL(tEdge);
					this.InsertScanbeam(tEdge.nextInLML.ytop);
				}
				if (this.IsContributing(mCurrentLM))
				{
					this.AddLocalMinPoly(mCurrentLM, tEdge, new IntPoint(mCurrentLM.xcurr, this.m_CurrentLM.Y));
				}
				if (tEdge.outIdx >= 0 && tEdge.dx == -3.4E+38)
				{
					for (int i = 0; i < this.m_HorizJoins.Count; i++)
					{
						IntPoint intPoint = new IntPoint();
						IntPoint intPoint1 = new IntPoint();
						HorzJoinRec item = this.m_HorizJoins[i];
						if (this.GetOverlapSegment(new IntPoint(item.edge.xbot, item.edge.ybot), new IntPoint(item.edge.xtop, item.edge.ytop), new IntPoint(tEdge.xbot, tEdge.ybot), new IntPoint(tEdge.xtop, tEdge.ytop), ref intPoint, ref intPoint1))
						{
							this.AddJoin(item.edge, tEdge, item.savedIdx, -1);
						}
					}
				}
				if (mCurrentLM.nextInAEL != tEdge)
				{
					if (tEdge.outIdx >= 0 && tEdge.prevInAEL.outIdx >= 0 && base.SlopesEqual(tEdge.prevInAEL, tEdge, this.m_UseFullRange))
					{
						this.AddJoin(tEdge, tEdge.prevInAEL, -1, -1);
					}
					TEdge tEdge1 = mCurrentLM.nextInAEL;
					IntPoint intPoint2 = new IntPoint(mCurrentLM.xcurr, mCurrentLM.ycurr);
					while (tEdge1 != tEdge)
					{
						if (tEdge1 == null)
						{
							throw new ClipperException("InsertLocalMinimaIntoAEL: missing rightbound!");
						}
						this.IntersectEdges(tEdge, tEdge1, intPoint2, Protects.ipNone);
						tEdge1 = tEdge1.nextInAEL;
					}
				}
				base.PopLocalMinima();
			}
		}

		private OutPt InsertPolyPtBetween(OutPt p1, OutPt p2, IntPoint pt)
		{
			OutPt outPt = new OutPt()
			{
				pt = pt
			};
			if (p2 != p1.next)
			{
				p2.next = outPt;
				p1.prev = outPt;
				outPt.next = p1;
				outPt.prev = p2;
			}
			else
			{
				p1.next = outPt;
				p2.prev = outPt;
				outPt.next = p2;
				outPt.prev = p1;
			}
			return outPt;
		}

		private void InsertScanbeam(long Y)
		{
			if (this.m_Scanbeam == null)
			{
				this.m_Scanbeam = new Scanbeam()
				{
					next = null,
					Y = Y
				};
				return;
			}
			if (Y > this.m_Scanbeam.Y)
			{
				Scanbeam scanbeam = new Scanbeam()
				{
					Y = Y,
					next = this.m_Scanbeam
				};
				this.m_Scanbeam = scanbeam;
				return;
			}
			Scanbeam mScanbeam = this.m_Scanbeam;
			while (mScanbeam.next != null && Y <= mScanbeam.next.Y)
			{
				mScanbeam = mScanbeam.next;
			}
			if (Y == mScanbeam.Y)
			{
				return;
			}
			Scanbeam scanbeam1 = new Scanbeam()
			{
				Y = Y,
				next = mScanbeam.next
			};
			mScanbeam.next = scanbeam1;
		}

		private void IntersectEdges(TEdge e1, TEdge e2, IntPoint pt, Protects protects)
		{
			PolyFillType mClipFillType;
			PolyFillType mSubjFillType;
			PolyFillType polyFillType;
			PolyFillType mSubjFillType1;
			int num;
			int num1;
			long num2;
			long num3;
			bool flag = ((Protects.ipLeft & protects) != Protects.ipNone || e1.nextInLML != null || e1.xtop != pt.X ? false : e1.ytop == pt.Y);
			bool flag1 = ((Protects.ipRight & protects) != Protects.ipNone || e2.nextInLML != null || e2.xtop != pt.X ? false : e2.ytop == pt.Y);
			bool flag2 = e1.outIdx >= 0;
			bool flag3 = e2.outIdx >= 0;
			if (e1.polyType != e2.polyType)
			{
				if (this.IsEvenOddFillType(e2))
				{
					e1.windCnt2 = (e1.windCnt2 == 0 ? 1 : 0);
				}
				else
				{
					e1.windCnt2 += e2.windDelta;
				}
				if (this.IsEvenOddFillType(e1))
				{
					e2.windCnt2 = (e2.windCnt2 == 0 ? 1 : 0);
				}
				else
				{
					e2.windCnt2 -= e1.windDelta;
				}
			}
			else if (!this.IsEvenOddFillType(e1))
			{
				if (e1.windCnt + e2.windDelta != 0)
				{
					e1.windCnt += e2.windDelta;
				}
				else
				{
					e1.windCnt = -e1.windCnt;
				}
				if (e2.windCnt - e1.windDelta != 0)
				{
					e2.windCnt -= e1.windDelta;
				}
				else
				{
					e2.windCnt = -e2.windCnt;
				}
			}
			else
			{
				int num4 = e1.windCnt;
				e1.windCnt = e2.windCnt;
				e2.windCnt = num4;
			}
			if (e1.polyType != PolyType.ptSubject)
			{
				mClipFillType = this.m_ClipFillType;
				polyFillType = this.m_SubjFillType;
			}
			else
			{
				mClipFillType = this.m_SubjFillType;
				polyFillType = this.m_ClipFillType;
			}
			if (e2.polyType != PolyType.ptSubject)
			{
				mSubjFillType = this.m_ClipFillType;
				mSubjFillType1 = this.m_SubjFillType;
			}
			else
			{
				mSubjFillType = this.m_SubjFillType;
				mSubjFillType1 = this.m_ClipFillType;
			}
			switch (mClipFillType)
			{
				case PolyFillType.pftPositive:
				{
					num = e1.windCnt;
					break;
				}
				case PolyFillType.pftNegative:
				{
					num = -e1.windCnt;
					break;
				}
				default:
				{
					num = Math.Abs(e1.windCnt);
					break;
				}
			}
			switch (mSubjFillType)
			{
				case PolyFillType.pftPositive:
				{
					num1 = e2.windCnt;
					break;
				}
				case PolyFillType.pftNegative:
				{
					num1 = -e2.windCnt;
					break;
				}
				default:
				{
					num1 = Math.Abs(e2.windCnt);
					break;
				}
			}
			if (flag2 && flag3)
			{
				if (flag || flag1 || num != 0 && num != 1 || num1 != 0 && num1 != 1 || e1.polyType != e2.polyType && this.m_ClipType != ClipType.ctXor)
				{
					this.AddLocalMaxPoly(e1, e2, pt);
				}
				else
				{
					this.DoBothEdges(e1, e2, pt);
				}
			}
			else if (flag2)
			{
				if ((num1 == 0 || num1 == 1) && (this.m_ClipType != ClipType.ctIntersection || e2.polyType == PolyType.ptSubject || e2.windCnt2 != 0))
				{
					this.DoEdge1(e1, e2, pt);
				}
			}
			else if (flag3)
			{
				if ((num == 0 || num == 1) && (this.m_ClipType != ClipType.ctIntersection || e1.polyType == PolyType.ptSubject || e1.windCnt2 != 0))
				{
					this.DoEdge2(e1, e2, pt);
				}
			}
			else if ((num == 0 || num == 1) && (num1 == 0 || num1 == 1) && !flag && !flag1)
			{
				switch (polyFillType)
				{
					case PolyFillType.pftPositive:
					{
						num2 = (long)e1.windCnt2;
						break;
					}
					case PolyFillType.pftNegative:
					{
						num2 = (long)(-e1.windCnt2);
						break;
					}
					default:
					{
						num2 = (long)Math.Abs(e1.windCnt2);
						break;
					}
				}
				switch (mSubjFillType1)
				{
					case PolyFillType.pftPositive:
					{
						num3 = (long)e2.windCnt2;
						break;
					}
					case PolyFillType.pftNegative:
					{
						num3 = (long)(-e2.windCnt2);
						break;
					}
					default:
					{
						num3 = (long)Math.Abs(e2.windCnt2);
						break;
					}
				}
				if (e1.polyType != e2.polyType)
				{
					this.AddLocalMinPoly(e1, e2, pt);
				}
				else if (num != 1 || num1 != 1)
				{
					Clipper.SwapSides(e1, e2);
				}
				else
				{
					switch (this.m_ClipType)
					{
						case ClipType.ctIntersection:
						{
							if (num2 <= (long)0 || num3 <= (long)0)
							{
								break;
							}
							this.AddLocalMinPoly(e1, e2, pt);
							break;
						}
						case ClipType.ctUnion:
						{
							if (num2 > (long)0 || num3 > (long)0)
							{
								break;
							}
							this.AddLocalMinPoly(e1, e2, pt);
							break;
						}
						case ClipType.ctDifference:
						{
							if ((e1.polyType != PolyType.ptClip || num2 <= (long)0 || num3 <= (long)0) && (e1.polyType != PolyType.ptSubject || num2 > (long)0 || num3 > (long)0))
							{
								break;
							}
							this.AddLocalMinPoly(e1, e2, pt);
							break;
						}
						case ClipType.ctXor:
						{
							this.AddLocalMinPoly(e1, e2, pt);
							break;
						}
					}
				}
			}
			if (flag != flag1 && (flag && e1.outIdx >= 0 || flag1 && e2.outIdx >= 0))
			{
				Clipper.SwapSides(e1, e2);
				Clipper.SwapPolyIndexes(e1, e2);
			}
			if (flag)
			{
				this.DeleteFromAEL(e1);
			}
			if (flag1)
			{
				this.DeleteFromAEL(e2);
			}
		}

		private bool IntersectPoint(TEdge edge1, TEdge edge2, ref IntPoint ip)
		{
			double num;
			double num1;
			if (base.SlopesEqual(edge1, edge2, this.m_UseFullRange))
			{
				return false;
			}
			if (edge1.dx == 0)
			{
				ip.X = edge1.xbot;
				if (edge2.dx != -3.4E+38)
				{
					num1 = (double)edge2.ybot - (double)edge2.xbot / edge2.dx;
					ip.Y = Clipper.Round((double)ip.X / edge2.dx + num1);
				}
				else
				{
					ip.Y = edge2.ybot;
				}
			}
			else if (edge2.dx != 0)
			{
				num = (double)edge1.xbot - (double)edge1.ybot * edge1.dx;
				num1 = (double)edge2.xbot - (double)edge2.ybot * edge2.dx;
				double num2 = (num1 - num) / (edge1.dx - edge2.dx);
				ip.Y = Clipper.Round(num2);
				if (Math.Abs(edge1.dx) >= Math.Abs(edge2.dx))
				{
					ip.X = Clipper.Round(edge2.dx * num2 + num1);
				}
				else
				{
					ip.X = Clipper.Round(edge1.dx * num2 + num);
				}
			}
			else
			{
				ip.X = edge2.xbot;
				if (edge1.dx != -3.4E+38)
				{
					num = (double)edge1.ybot - (double)edge1.xbot / edge1.dx;
					ip.Y = Clipper.Round((double)ip.X / edge1.dx + num);
				}
				else
				{
					ip.Y = edge1.ybot;
				}
			}
			if (ip.Y >= edge1.ytop && ip.Y >= edge2.ytop)
			{
				return true;
			}
			if (edge1.ytop > edge2.ytop)
			{
				ip.X = edge1.xtop;
				ip.Y = edge1.ytop;
				return Clipper.TopX(edge2, edge1.ytop) < edge1.xtop;
			}
			ip.X = edge2.xtop;
			ip.Y = edge2.ytop;
			return Clipper.TopX(edge1, edge2.ytop) > edge2.xtop;
		}

		private bool IsContributing(TEdge edge)
		{
			PolyFillType mClipFillType;
			PolyFillType mSubjFillType;
			if (edge.polyType != PolyType.ptSubject)
			{
				mClipFillType = this.m_ClipFillType;
				mSubjFillType = this.m_SubjFillType;
			}
			else
			{
				mClipFillType = this.m_SubjFillType;
				mSubjFillType = this.m_ClipFillType;
			}
			switch (mClipFillType)
			{
				case PolyFillType.pftEvenOdd:
				case PolyFillType.pftNonZero:
				{
					if (Math.Abs(edge.windCnt) == 1)
					{
						break;
					}
					return false;
				}
				case PolyFillType.pftPositive:
				{
					if (edge.windCnt == 1)
					{
						break;
					}
					return false;
				}
				default:
				{
					if (edge.windCnt == -1)
					{
						break;
					}
					return false;
				}
			}
			switch (this.m_ClipType)
			{
				case ClipType.ctIntersection:
				{
					switch (mSubjFillType)
					{
						case PolyFillType.pftEvenOdd:
						case PolyFillType.pftNonZero:
						{
							return edge.windCnt2 != 0;
						}
						case PolyFillType.pftPositive:
						{
							return edge.windCnt2 > 0;
						}
					}
					return edge.windCnt2 < 0;
				}
				case ClipType.ctUnion:
				{
					switch (mSubjFillType)
					{
						case PolyFillType.pftEvenOdd:
						case PolyFillType.pftNonZero:
						{
							return edge.windCnt2 == 0;
						}
						case PolyFillType.pftPositive:
						{
							return edge.windCnt2 <= 0;
						}
					}
					return edge.windCnt2 >= 0;
				}
				case ClipType.ctDifference:
				{
					if (edge.polyType != PolyType.ptSubject)
					{
						switch (mSubjFillType)
						{
							case PolyFillType.pftEvenOdd:
							case PolyFillType.pftNonZero:
							{
								return edge.windCnt2 != 0;
							}
							case PolyFillType.pftPositive:
							{
								return edge.windCnt2 > 0;
							}
						}
						return edge.windCnt2 < 0;
					}
					switch (mSubjFillType)
					{
						case PolyFillType.pftEvenOdd:
						case PolyFillType.pftNonZero:
						{
							return edge.windCnt2 == 0;
						}
						case PolyFillType.pftPositive:
						{
							return edge.windCnt2 <= 0;
						}
					}
					return edge.windCnt2 >= 0;
				}
			}
			return true;
		}

		private bool IsEvenOddAltFillType(TEdge edge)
		{
			if (edge.polyType == PolyType.ptSubject)
			{
				return this.m_ClipFillType == PolyFillType.pftEvenOdd;
			}
			return this.m_SubjFillType == PolyFillType.pftEvenOdd;
		}

		private bool IsEvenOddFillType(TEdge edge)
		{
			if (edge.polyType == PolyType.ptSubject)
			{
				return this.m_SubjFillType == PolyFillType.pftEvenOdd;
			}
			return this.m_ClipFillType == PolyFillType.pftEvenOdd;
		}

		private bool IsIntermediate(TEdge e, double Y)
		{
			if ((double)e.ytop != Y)
			{
				return false;
			}
			return e.nextInLML != null;
		}

		private bool IsMaxima(TEdge e, double Y)
		{
			if (e == null || (double)e.ytop != Y)
			{
				return false;
			}
			return e.nextInLML == null;
		}

		private bool IsMinima(TEdge e)
		{
			if (e == null || e.prev.nextInLML == e)
			{
				return false;
			}
			return e.next.nextInLML != e;
		}

		private bool IsTopHorz(TEdge horzEdge, double XPos)
		{
			for (TEdge i = this.m_SortedEdges; i != null; i = i.nextInSEL)
			{
				if (XPos >= (double)Math.Min(i.xcurr, i.xtop) && XPos <= (double)Math.Max(i.xcurr, i.xtop))
				{
					return false;
				}
			}
			return true;
		}

		private void JoinCommonEdges()
		{
			OutRec outRec;
			OutPt outPt;
			OutPt outPt1;
			for (int i = 0; i < this.m_Joins.Count; i++)
			{
				JoinRec item = this.m_Joins[i];
				OutRec firstLeft = this.m_PolyOuts[item.poly1Idx];
				OutRec count = this.m_PolyOuts[item.poly2Idx];
				if (firstLeft.pts != null && count.pts != null)
				{
					if (firstLeft == count)
					{
						outRec = firstLeft;
					}
					else if (!this.Param1RightOfParam2(firstLeft, count))
					{
						outRec = (!this.Param1RightOfParam2(count, firstLeft) ? this.GetLowermostRec(firstLeft, count) : firstLeft);
					}
					else
					{
						outRec = count;
					}
					if (this.JoinPoints(item, out outPt, out outPt1))
					{
						if (firstLeft != count)
						{
							this.FixupOutPolygon(firstLeft);
							int num = firstLeft.idx;
							int num1 = count.idx;
							count.pts = null;
							count.bottomPt = null;
							firstLeft.isHole = outRec.isHole;
							if (outRec == count)
							{
								firstLeft.FirstLeft = count.FirstLeft;
							}
							count.FirstLeft = firstLeft;
							for (int j = i + 1; j < this.m_Joins.Count; j++)
							{
								JoinRec joinRec = this.m_Joins[j];
								if (joinRec.poly1Idx == num1)
								{
									joinRec.poly1Idx = num;
								}
								if (joinRec.poly2Idx == num1)
								{
									joinRec.poly2Idx = num;
								}
							}
							if (this.m_UsingPolyTree)
							{
								this.FixupFirstLefts2(count, firstLeft);
							}
						}
						else
						{
							firstLeft.pts = this.GetBottomPt(outPt);
							firstLeft.bottomPt = firstLeft.pts;
							firstLeft.bottomPt.idx = firstLeft.idx;
							count = this.CreateOutRec();
							this.m_PolyOuts.Add(count);
							count.idx = this.m_PolyOuts.Count - 1;
							item.poly2Idx = count.idx;
							count.pts = this.GetBottomPt(outPt1);
							count.bottomPt = count.pts;
							count.bottomPt.idx = count.idx;
							if (Poly2ContainsPoly1(count.pts, firstLeft.pts))
							{
								count.isHole = !firstLeft.isHole;
								count.FirstLeft = firstLeft;
								this.FixupJoinRecs(item, outPt1, i + 1);
								if (this.m_UsingPolyTree)
								{
									this.FixupFirstLefts2(count, firstLeft);
								}
								this.FixupOutPolygon(firstLeft);
								this.FixupOutPolygon(count);
								if ((count.isHole ^ this.m_ReverseOutput) == this.Area(count, this.m_UseFullRange) > 0)
								{
									this.ReversePolyPtLinks(count.pts);
								}
							}
							else if (!Poly2ContainsPoly1(firstLeft.pts, count.pts))
							{
								count.isHole = firstLeft.isHole;
								count.FirstLeft = firstLeft.FirstLeft;
								this.FixupJoinRecs(item, outPt1, i + 1);
								if (this.m_UsingPolyTree)
								{
									this.FixupFirstLefts1(firstLeft, count);
								}
								this.FixupOutPolygon(firstLeft);
								this.FixupOutPolygon(count);
							}
							else
							{
								count.isHole = firstLeft.isHole;
								firstLeft.isHole = !count.isHole;
								count.FirstLeft = firstLeft.FirstLeft;
								firstLeft.FirstLeft = count;
								this.FixupJoinRecs(item, outPt1, i + 1);
								if (this.m_UsingPolyTree)
								{
									this.FixupFirstLefts2(firstLeft, count);
								}
								this.FixupOutPolygon(firstLeft);
								this.FixupOutPolygon(count);
								if ((firstLeft.isHole ^ this.m_ReverseOutput) == this.Area(firstLeft, this.m_UseFullRange) > 0)
								{
									this.ReversePolyPtLinks(firstLeft.pts);
								}
							}
						}
					}
				}
			}
		}

		private bool JoinPoints(JoinRec j, out OutPt p1, out OutPt p2)
		{
			OutPt outPt;
			OutPt outPt1;
			p1 = null;
			p2 = null;
			OutRec item = this.m_PolyOuts[j.poly1Idx];
			OutRec outRec = this.m_PolyOuts[j.poly2Idx];
			if (item == null || outRec == null)
			{
				return false;
			}
			OutPt outPt2 = item.pts;
			OutPt outPt3 = outRec.pts;
			IntPoint intPoint = j.pt2a;
			IntPoint intPoint1 = j.pt2b;
			IntPoint intPoint2 = j.pt1a;
			IntPoint intPoint3 = j.pt1b;
			if (!this.FindSegment(ref outPt2, ref intPoint, ref intPoint1))
			{
				return false;
			}
			if (item == outRec)
			{
				outPt3 = outPt2.next;
				if (!this.FindSegment(ref outPt3, ref intPoint2, ref intPoint3) || outPt3 == outPt2)
				{
					return false;
				}
			}
			else if (!this.FindSegment(ref outPt3, ref intPoint2, ref intPoint3))
			{
				return false;
			}
			if (!this.GetOverlapSegment(intPoint, intPoint1, intPoint2, intPoint3, ref intPoint, ref intPoint1))
			{
				return false;
			}
			OutPt outPt4 = outPt2.prev;
			if (ClipperBase.PointsEqual(outPt2.pt, intPoint))
			{
				p1 = outPt2;
			}
			else if (!ClipperBase.PointsEqual(outPt4.pt, intPoint))
			{
				p1 = this.InsertPolyPtBetween(outPt2, outPt4, intPoint);
			}
			else
			{
				p1 = outPt4;
			}
			if (ClipperBase.PointsEqual(outPt2.pt, intPoint1))
			{
				p2 = outPt2;
			}
			else if (ClipperBase.PointsEqual(outPt4.pt, intPoint1))
			{
				p2 = outPt4;
			}
			else if (p1 == outPt2 || p1 == outPt4)
			{
				p2 = this.InsertPolyPtBetween(outPt2, outPt4, intPoint1);
			}
			else if (!this.Pt3IsBetweenPt1AndPt2(outPt2.pt, p1.pt, intPoint1))
			{
				p2 = this.InsertPolyPtBetween(p1, outPt4, intPoint1);
			}
			else
			{
				p2 = this.InsertPolyPtBetween(outPt2, p1, intPoint1);
			}
			outPt4 = outPt3.prev;
			if (!ClipperBase.PointsEqual(outPt3.pt, intPoint))
			{
				outPt = (!ClipperBase.PointsEqual(outPt4.pt, intPoint) ? this.InsertPolyPtBetween(outPt3, outPt4, intPoint) : outPt4);
			}
			else
			{
				outPt = outPt3;
			}
			if (ClipperBase.PointsEqual(outPt3.pt, intPoint1))
			{
				outPt1 = outPt3;
			}
			else if (ClipperBase.PointsEqual(outPt4.pt, intPoint1))
			{
				outPt1 = outPt4;
			}
			else if (outPt == outPt3 || outPt == outPt4)
			{
				outPt1 = this.InsertPolyPtBetween(outPt3, outPt4, intPoint1);
			}
			else
			{
				outPt1 = (!this.Pt3IsBetweenPt1AndPt2(outPt3.pt, outPt.pt, intPoint1) ? this.InsertPolyPtBetween(outPt, outPt4, intPoint1) : this.InsertPolyPtBetween(outPt3, outPt, intPoint1));
			}
			if (p1.next == p2 && outPt.prev == outPt1)
			{
				p1.next = outPt;
				outPt.prev = p1;
				p2.prev = outPt1;
				outPt1.next = p2;
				return true;
			}
			if (p1.prev != p2 || outPt.next != outPt1)
			{
				return false;
			}
			p1.prev = outPt;
			outPt.next = p1;
			p2.next = outPt1;
			outPt1.prev = p2;
			return true;
		}

		public static List<List<IntPoint>> OffsetPolygons(List<List<IntPoint>> poly, double delta, JoinType jointype, double MiterLimit, bool AutoFix)
		{
			List<List<IntPoint>> lists = new List<List<IntPoint>>(poly.Count);
			Clipper.PolyOffsetBuilder polyOffsetBuilder = new Clipper.PolyOffsetBuilder(poly, lists, delta, jointype, MiterLimit, AutoFix);
			return lists;
		}

		public static List<List<IntPoint>> OffsetPolygons(List<List<IntPoint>> poly, double delta, JoinType jointype, double MiterLimit)
		{
			List<List<IntPoint>> lists = new List<List<IntPoint>>(poly.Count);
			Clipper.PolyOffsetBuilder polyOffsetBuilder = new Clipper.PolyOffsetBuilder(poly, lists, delta, jointype, MiterLimit, true);
			return lists;
		}

		public static List<List<IntPoint>> OffsetPolygons(List<List<IntPoint>> poly, double delta, JoinType jointype)
		{
			List<List<IntPoint>> lists = new List<List<IntPoint>>(poly.Count);
			Clipper.PolyOffsetBuilder polyOffsetBuilder = new Clipper.PolyOffsetBuilder(poly, lists, delta, jointype, 2, true);
			return lists;
		}

		public static List<List<IntPoint>> OffsetPolygons(List<List<IntPoint>> poly, double delta)
		{
			List<List<IntPoint>> lists = new List<List<IntPoint>>(poly.Count);
			Clipper.PolyOffsetBuilder polyOffsetBuilder = new Clipper.PolyOffsetBuilder(poly, lists, delta, JoinType.jtSquare, 2, true);
			return lists;
		}

		public static bool Orientation(List<IntPoint> poly)
		{
			return Clipper.Area(poly) >= 0;
		}

		private bool Param1RightOfParam2(OutRec outRec1, OutRec outRec2)
		{
			do
			{
				outRec1 = outRec1.FirstLeft;
				if (outRec1 != outRec2)
				{
					continue;
				}
				return true;
			}
			while (outRec1 != null);
			return false;
		}

		private int PointCount(OutPt pts)
		{
			if (pts == null)
			{
				return 0;
			}
			int num = 0;
			OutPt outPt = pts;
			do
			{
				num++;
				outPt = outPt.next;
			}
			while (outPt != pts);
			return num;
		}

		private static bool Poly2ContainsPoly1(OutPt outPt1, OutPt outPt2)
		{
			OutPt op = outPt1;
			do
			{
				//nb: PointInPolygon returns 0 if false, +1 if true, -1 if pt on polygon
				int res = PointInPolygon(op.pt, outPt2);
				if (res >= 0) return res > 0;
				op = op.next;
			}
			while (op != outPt1);
			return true;
		}

		public static void PolyTreeToPolygons(PolyTree polytree, List<List<IntPoint>> polygons)
		{
			polygons.Clear();
			polygons.Capacity = polytree.Total;
			Clipper.AddPolyNodeToPolygons(polytree, polygons);
		}

		private long PopScanbeam()
		{
			long y = this.m_Scanbeam.Y;
			this.m_Scanbeam = this.m_Scanbeam.next;
			return y;
		}

		private void ProcessEdgesAtTopOfScanbeam(long topY)
		{
			TEdge mActiveEdges = this.m_ActiveEdges;
			while (mActiveEdges != null)
			{
				if (!this.IsMaxima(mActiveEdges, (double)topY) || this.GetMaximaPair(mActiveEdges).dx == -3.4E+38)
				{
					if (!this.IsIntermediate(mActiveEdges, (double)topY) || mActiveEdges.nextInLML.dx != -3.4E+38)
					{
						mActiveEdges.xcurr = Clipper.TopX(mActiveEdges, topY);
						mActiveEdges.ycurr = topY;
					}
					else
					{
						if (mActiveEdges.outIdx >= 0)
						{
							this.AddOutPt(mActiveEdges, new IntPoint(mActiveEdges.xtop, mActiveEdges.ytop));
							for (int i = 0; i < this.m_HorizJoins.Count; i++)
							{
								IntPoint intPoint = new IntPoint();
								IntPoint intPoint1 = new IntPoint();
								HorzJoinRec item = this.m_HorizJoins[i];
								if (this.GetOverlapSegment(new IntPoint(item.edge.xbot, item.edge.ybot), new IntPoint(item.edge.xtop, item.edge.ytop), new IntPoint(mActiveEdges.nextInLML.xbot, mActiveEdges.nextInLML.ybot), new IntPoint(mActiveEdges.nextInLML.xtop, mActiveEdges.nextInLML.ytop), ref intPoint, ref intPoint1))
								{
									this.AddJoin(item.edge, mActiveEdges.nextInLML, item.savedIdx, mActiveEdges.outIdx);
								}
							}
							this.AddHorzJoin(mActiveEdges.nextInLML, mActiveEdges.outIdx);
						}
						this.UpdateEdgeIntoAEL(ref mActiveEdges);
						this.AddEdgeToSEL(mActiveEdges);
					}
					mActiveEdges = mActiveEdges.nextInAEL;
				}
				else
				{
					TEdge tEdge = mActiveEdges.prevInAEL;
					this.DoMaxima(mActiveEdges, topY);
					mActiveEdges = (tEdge != null ? tEdge.nextInAEL : this.m_ActiveEdges);
				}
			}
			this.ProcessHorizontals();
			for (mActiveEdges = this.m_ActiveEdges; mActiveEdges != null; mActiveEdges = mActiveEdges.nextInAEL)
			{
				if (this.IsIntermediate(mActiveEdges, (double)topY))
				{
					if (mActiveEdges.outIdx >= 0)
					{
						this.AddOutPt(mActiveEdges, new IntPoint(mActiveEdges.xtop, mActiveEdges.ytop));
					}
					this.UpdateEdgeIntoAEL(ref mActiveEdges);
					TEdge tEdge1 = mActiveEdges.prevInAEL;
					TEdge tEdge2 = mActiveEdges.nextInAEL;
					if (tEdge1 != null && tEdge1.xcurr == mActiveEdges.xbot && tEdge1.ycurr == mActiveEdges.ybot && mActiveEdges.outIdx >= 0 && tEdge1.outIdx >= 0 && tEdge1.ycurr > tEdge1.ytop && base.SlopesEqual(mActiveEdges, tEdge1, this.m_UseFullRange))
					{
						this.AddOutPt(tEdge1, new IntPoint(mActiveEdges.xbot, mActiveEdges.ybot));
						this.AddJoin(mActiveEdges, tEdge1, -1, -1);
					}
					else if (tEdge2 != null && tEdge2.xcurr == mActiveEdges.xbot && tEdge2.ycurr == mActiveEdges.ybot && mActiveEdges.outIdx >= 0 && tEdge2.outIdx >= 0 && tEdge2.ycurr > tEdge2.ytop && base.SlopesEqual(mActiveEdges, tEdge2, this.m_UseFullRange))
					{
						this.AddOutPt(tEdge2, new IntPoint(mActiveEdges.xbot, mActiveEdges.ybot));
						this.AddJoin(mActiveEdges, tEdge2, -1, -1);
					}
				}
			}
		}

		private void ProcessHorizontal(TEdge horzEdge)
		{
			Direction direction;
			long num;
			long num1;
			TEdge maximaPair;
			TEdge nextInAEL = null;
			if (horzEdge.xcurr >= horzEdge.xtop)
			{
				num = horzEdge.xtop;
				num1 = horzEdge.xcurr;
				direction = Direction.dRightToLeft;
			}
			else
			{
				num = horzEdge.xcurr;
				num1 = horzEdge.xtop;
				direction = Direction.dLeftToRight;
			}
			if (horzEdge.nextInLML == null)
			{
				maximaPair = this.GetMaximaPair(horzEdge);
			}
			else
			{
				maximaPair = null;
			}
			for (TEdge i = this.GetNextInAEL(horzEdge, direction); i != null; i = nextInAEL)
			{
				nextInAEL = this.GetNextInAEL(i, direction);
				if (maximaPair != null || direction == Direction.dLeftToRight && i.xcurr <= num1 || direction == Direction.dRightToLeft && i.xcurr >= num)
				{
					if (i.xcurr == horzEdge.xtop && maximaPair == null)
					{
						if (base.SlopesEqual(i, horzEdge.nextInLML, this.m_UseFullRange))
						{
							if (horzEdge.outIdx < 0 || i.outIdx < 0)
							{
								break;
							}
							this.AddJoin(horzEdge.nextInLML, i, horzEdge.outIdx, -1);
							break;
						}
						else if (i.dx < horzEdge.nextInLML.dx)
						{
							break;
						}
					}
					if (i == maximaPair)
					{
						if (direction != Direction.dLeftToRight)
						{
							this.IntersectEdges(i, horzEdge, new IntPoint(i.xcurr, horzEdge.ycurr), Protects.ipNone);
						}
						else
						{
							this.IntersectEdges(horzEdge, i, new IntPoint(i.xcurr, horzEdge.ycurr), Protects.ipNone);
						}
						if (maximaPair.outIdx >= 0)
						{
							throw new ClipperException("ProcessHorizontal error");
						}
						return;
					}
					if (i.dx == -3.4E+38 && !this.IsMinima(i) && i.xcurr <= i.xtop)
					{
						if (direction != Direction.dLeftToRight)
						{
							this.IntersectEdges(i, horzEdge, new IntPoint(i.xcurr, horzEdge.ycurr), (this.IsTopHorz(horzEdge, (double)i.xcurr) ? Protects.ipRight : Protects.ipBoth));
						}
						else
						{
							this.IntersectEdges(horzEdge, i, new IntPoint(i.xcurr, horzEdge.ycurr), (this.IsTopHorz(horzEdge, (double)i.xcurr) ? Protects.ipLeft : Protects.ipBoth));
						}
					}
					else if (direction != Direction.dLeftToRight)
					{
						this.IntersectEdges(i, horzEdge, new IntPoint(i.xcurr, horzEdge.ycurr), (this.IsTopHorz(horzEdge, (double)i.xcurr) ? Protects.ipRight : Protects.ipBoth));
					}
					else
					{
						this.IntersectEdges(horzEdge, i, new IntPoint(i.xcurr, horzEdge.ycurr), (this.IsTopHorz(horzEdge, (double)i.xcurr) ? Protects.ipLeft : Protects.ipBoth));
					}
					this.SwapPositionsInAEL(horzEdge, i);
				}
				else if (direction == Direction.dLeftToRight && i.xcurr > num1 && horzEdge.nextInSEL == null || direction == Direction.dRightToLeft && i.xcurr < num && horzEdge.nextInSEL == null)
				{
					break;
				}
			}
			if (horzEdge.nextInLML != null)
			{
				if (horzEdge.outIdx >= 0)
				{
					this.AddOutPt(horzEdge, new IntPoint(horzEdge.xtop, horzEdge.ytop));
				}
				this.UpdateEdgeIntoAEL(ref horzEdge);
				return;
			}
			if (horzEdge.outIdx >= 0)
			{
				this.IntersectEdges(horzEdge, maximaPair, new IntPoint(horzEdge.xtop, horzEdge.ycurr), Protects.ipBoth);
			}
			this.DeleteFromAEL(maximaPair);
			this.DeleteFromAEL(horzEdge);
		}

		private void ProcessHorizontals()
		{
			for (TEdge i = this.m_SortedEdges; i != null; i = this.m_SortedEdges)
			{
				this.DeleteFromSEL(i);
				this.ProcessHorizontal(i);
			}
		}

		private bool ProcessIntersections(long botY, long topY)
		{
			bool flag;
			if (this.m_ActiveEdges == null)
			{
				return true;
			}
			try
			{
				this.BuildIntersectList(botY, topY);
				if (this.m_IntersectNodes == null)
				{
					flag = true;
				}
				else if (!this.FixupIntersections())
				{
					flag = false;
				}
				else
				{
					this.ProcessIntersectList();
					return true;
				}
			}
			catch
			{
				this.m_SortedEdges = null;
				this.DisposeIntersectNodes();
				throw new ClipperException("ProcessIntersections error");
			}
			return flag;
		}

		private void ProcessIntersectList()
		{
			while (this.m_IntersectNodes != null)
			{
				IntersectNode mIntersectNodes = this.m_IntersectNodes.next;
				this.IntersectEdges(this.m_IntersectNodes.edge1, this.m_IntersectNodes.edge2, this.m_IntersectNodes.pt, Protects.ipBoth);
				this.SwapPositionsInAEL(this.m_IntersectNodes.edge1, this.m_IntersectNodes.edge2);
				this.m_IntersectNodes = null;
				this.m_IntersectNodes = mIntersectNodes;
			}
		}

		private bool ProcessParam1BeforeParam2(IntersectNode node1, IntersectNode node2)
		{
			bool x;
			if (node1.pt.Y != node2.pt.Y)
			{
				return node1.pt.Y > node2.pt.Y;
			}
			if (node1.edge1 == node2.edge1 || node1.edge2 == node2.edge1)
			{
				x = node2.pt.X > node1.pt.X;
				if (node2.edge1.dx <= 0)
				{
					return x;
				}
				return !x;
			}
			if (node1.edge1 != node2.edge2 && node1.edge2 != node2.edge2)
			{
				return node2.pt.X > node1.pt.X;
			}
			x = node2.pt.X > node1.pt.X;
			if (node2.edge2.dx <= 0)
			{
				return x;
			}
			return !x;
		}

		internal bool Pt3IsBetweenPt1AndPt2(IntPoint pt1, IntPoint pt2, IntPoint pt3)
		{
			if (ClipperBase.PointsEqual(pt1, pt3) || ClipperBase.PointsEqual(pt2, pt3))
			{
				return true;
			}
			if (pt1.X != pt2.X)
			{
				return pt1.X < pt3.X == pt3.X < pt2.X;
			}
			return pt1.Y < pt3.Y == pt3.Y < pt2.Y;
		}

		protected override void Reset()
		{
			base.Reset();
			this.m_Scanbeam = null;
			this.m_ActiveEdges = null;
			this.m_SortedEdges = null;
			this.DisposeAllPolyPts();
			for (LocalMinima i = this.m_MinimaList; i != null; i = i.next)
			{
				this.InsertScanbeam(i.Y);
				this.InsertScanbeam(i.leftBound.ytop);
			}
		}

		public static void ReversePolygons(List<List<IntPoint>> polys)
		{
			polys.ForEach((List<IntPoint> poly) => poly.Reverse());
		}

		private void ReversePolyPtLinks(OutPt pp)
		{
			if (pp == null)
			{
				return;
			}
			OutPt outPt = pp;
			do
			{
				OutPt outPt1 = outPt.next;
				outPt.next = outPt.prev;
				outPt.prev = outPt1;
				outPt = outPt1;
			}
			while (outPt != pp);
		}

		private static long Round(double value)
		{
			if (value >= 0)
			{
				return (long)(value + 0.5);
			}
			return (long)(value - 0.5);
		}

		private void SetHoleState(TEdge e, OutRec outRec)
		{
			bool flag = false;
			for (TEdge i = e.prevInAEL; i != null; i = i.prevInAEL)
			{
				if (i.outIdx >= 0)
				{
					flag = !flag;
					if (outRec.FirstLeft == null)
					{
						outRec.FirstLeft = this.m_PolyOuts[i.outIdx];
					}
				}
			}
			if (flag)
			{
				outRec.isHole = true;
			}
		}

		private void SetWindingCount(TEdge edge)
		{
			TEdge mActiveEdges = edge.prevInAEL;
			while (mActiveEdges != null && mActiveEdges.polyType != edge.polyType)
			{
				mActiveEdges = mActiveEdges.prevInAEL;
			}
			if (mActiveEdges == null)
			{
				edge.windCnt = edge.windDelta;
				edge.windCnt2 = 0;
				mActiveEdges = this.m_ActiveEdges;
			}
			else if (!this.IsEvenOddFillType(edge))
			{
				if (mActiveEdges.windCnt * mActiveEdges.windDelta < 0)
				{
					if (Math.Abs(mActiveEdges.windCnt) <= 1)
					{
						edge.windCnt = mActiveEdges.windCnt + mActiveEdges.windDelta + edge.windDelta;
					}
					else if (mActiveEdges.windDelta * edge.windDelta >= 0)
					{
						edge.windCnt = mActiveEdges.windCnt + edge.windDelta;
					}
					else
					{
						edge.windCnt = mActiveEdges.windCnt;
					}
				}
				else if (Math.Abs(mActiveEdges.windCnt) > 1 && mActiveEdges.windDelta * edge.windDelta < 0)
				{
					edge.windCnt = mActiveEdges.windCnt;
				}
				else if (mActiveEdges.windCnt + edge.windDelta != 0)
				{
					edge.windCnt = mActiveEdges.windCnt + edge.windDelta;
				}
				else
				{
					edge.windCnt = mActiveEdges.windCnt;
				}
				edge.windCnt2 = mActiveEdges.windCnt2;
				mActiveEdges = mActiveEdges.nextInAEL;
			}
			else
			{
				edge.windCnt = 1;
				edge.windCnt2 = mActiveEdges.windCnt2;
				mActiveEdges = mActiveEdges.nextInAEL;
			}
			if (!this.IsEvenOddAltFillType(edge))
			{
				while (mActiveEdges != edge)
				{
					edge.windCnt2 += mActiveEdges.windDelta;
					mActiveEdges = mActiveEdges.nextInAEL;
				}
				return;
			}
			while (mActiveEdges != edge)
			{
				edge.windCnt2 = (edge.windCnt2 == 0 ? 1 : 0);
				mActiveEdges = mActiveEdges.nextInAEL;
			}
		}

		public static List<List<IntPoint>> SimplifyPolygon(List<IntPoint> poly, PolyFillType fillType = 0)
		{
			List<List<IntPoint>> lists = new List<List<IntPoint>>();
			Clipper clipper = new Clipper();
			clipper.AddPolygon(poly, PolyType.ptSubject);
			clipper.Execute(ClipType.ctUnion, lists, fillType, fillType);
			return lists;
		}

		public static List<List<IntPoint>> SimplifyPolygons(List<List<IntPoint>> polys, PolyFillType fillType = 0)
		{
			List<List<IntPoint>> lists = new List<List<IntPoint>>();
			Clipper clipper = new Clipper();
			clipper.AddPolygons(polys, PolyType.ptSubject);
			clipper.Execute(ClipType.ctUnion, lists, fillType, fillType);
			return lists;
		}

		private void SwapIntersectNodes(IntersectNode int1, IntersectNode int2)
		{
			TEdge tEdge = int1.edge1;
			TEdge tEdge1 = int1.edge2;
			IntPoint intPoint = int1.pt;
			int1.edge1 = int2.edge1;
			int1.edge2 = int2.edge2;
			int1.pt = int2.pt;
			int2.edge1 = tEdge;
			int2.edge2 = tEdge1;
			int2.pt = intPoint;
		}

		internal void SwapPoints(ref IntPoint pt1, ref IntPoint pt2)
		{
			IntPoint intPoint = pt1;
			pt1 = pt2;
			pt2 = intPoint;
		}

		private static void SwapPolyIndexes(TEdge edge1, TEdge edge2)
		{
			int num = edge1.outIdx;
			edge1.outIdx = edge2.outIdx;
			edge2.outIdx = num;
		}

		private void SwapPositionsInAEL(TEdge edge1, TEdge edge2)
		{
			if (edge1.nextInAEL == edge2)
			{
				TEdge tEdge = edge2.nextInAEL;
				if (tEdge != null)
				{
					tEdge.prevInAEL = edge1;
				}
				TEdge tEdge1 = edge1.prevInAEL;
				if (tEdge1 != null)
				{
					tEdge1.nextInAEL = edge2;
				}
				edge2.prevInAEL = tEdge1;
				edge2.nextInAEL = edge1;
				edge1.prevInAEL = edge2;
				edge1.nextInAEL = tEdge;
			}
			else if (edge2.nextInAEL != edge1)
			{
				TEdge tEdge2 = edge1.nextInAEL;
				TEdge tEdge3 = edge1.prevInAEL;
				edge1.nextInAEL = edge2.nextInAEL;
				if (edge1.nextInAEL != null)
				{
					edge1.nextInAEL.prevInAEL = edge1;
				}
				edge1.prevInAEL = edge2.prevInAEL;
				if (edge1.prevInAEL != null)
				{
					edge1.prevInAEL.nextInAEL = edge1;
				}
				edge2.nextInAEL = tEdge2;
				if (edge2.nextInAEL != null)
				{
					edge2.nextInAEL.prevInAEL = edge2;
				}
				edge2.prevInAEL = tEdge3;
				if (edge2.prevInAEL != null)
				{
					edge2.prevInAEL.nextInAEL = edge2;
				}
			}
			else
			{
				TEdge tEdge4 = edge1.nextInAEL;
				if (tEdge4 != null)
				{
					tEdge4.prevInAEL = edge2;
				}
				TEdge tEdge5 = edge2.prevInAEL;
				if (tEdge5 != null)
				{
					tEdge5.nextInAEL = edge1;
				}
				edge1.prevInAEL = tEdge5;
				edge1.nextInAEL = edge2;
				edge2.prevInAEL = edge1;
				edge2.nextInAEL = tEdge4;
			}
			if (edge1.prevInAEL == null)
			{
				this.m_ActiveEdges = edge1;
				return;
			}
			if (edge2.prevInAEL == null)
			{
				this.m_ActiveEdges = edge2;
			}
		}

		private void SwapPositionsInSEL(TEdge edge1, TEdge edge2)
		{
			if (edge1.nextInSEL == null && edge1.prevInSEL == null)
			{
				return;
			}
			if (edge2.nextInSEL == null && edge2.prevInSEL == null)
			{
				return;
			}
			if (edge1.nextInSEL == edge2)
			{
				TEdge tEdge = edge2.nextInSEL;
				if (tEdge != null)
				{
					tEdge.prevInSEL = edge1;
				}
				TEdge tEdge1 = edge1.prevInSEL;
				if (tEdge1 != null)
				{
					tEdge1.nextInSEL = edge2;
				}
				edge2.prevInSEL = tEdge1;
				edge2.nextInSEL = edge1;
				edge1.prevInSEL = edge2;
				edge1.nextInSEL = tEdge;
			}
			else if (edge2.nextInSEL != edge1)
			{
				TEdge tEdge2 = edge1.nextInSEL;
				TEdge tEdge3 = edge1.prevInSEL;
				edge1.nextInSEL = edge2.nextInSEL;
				if (edge1.nextInSEL != null)
				{
					edge1.nextInSEL.prevInSEL = edge1;
				}
				edge1.prevInSEL = edge2.prevInSEL;
				if (edge1.prevInSEL != null)
				{
					edge1.prevInSEL.nextInSEL = edge1;
				}
				edge2.nextInSEL = tEdge2;
				if (edge2.nextInSEL != null)
				{
					edge2.nextInSEL.prevInSEL = edge2;
				}
				edge2.prevInSEL = tEdge3;
				if (edge2.prevInSEL != null)
				{
					edge2.prevInSEL.nextInSEL = edge2;
				}
			}
			else
			{
				TEdge tEdge4 = edge1.nextInSEL;
				if (tEdge4 != null)
				{
					tEdge4.prevInSEL = edge2;
				}
				TEdge tEdge5 = edge2.prevInSEL;
				if (tEdge5 != null)
				{
					tEdge5.nextInSEL = edge1;
				}
				edge1.prevInSEL = tEdge5;
				edge1.nextInSEL = edge2;
				edge2.prevInSEL = edge1;
				edge2.nextInSEL = tEdge4;
			}
			if (edge1.prevInSEL == null)
			{
				this.m_SortedEdges = edge1;
				return;
			}
			if (edge2.prevInSEL == null)
			{
				this.m_SortedEdges = edge2;
			}
		}

		private static void SwapSides(TEdge edge1, TEdge edge2)
		{
			EdgeSide edgeSide = edge1.side;
			edge1.side = edge2.side;
			edge2.side = edgeSide;
		}

		private static long TopX(TEdge edge, long currentY)
		{
			if (currentY == edge.ytop)
			{
				return edge.xtop;
			}
			return edge.xbot + Clipper.Round(edge.dx * (double)(currentY - edge.ybot));
		}

		private void UpdateEdgeIntoAEL(ref TEdge e)
		{
			if (e.nextInLML == null)
			{
				throw new ClipperException("UpdateEdgeIntoAEL: invalid call");
			}
			TEdge tEdge = e.prevInAEL;
			TEdge tEdge1 = e.nextInAEL;
			e.nextInLML.outIdx = e.outIdx;
			if (tEdge == null)
			{
				this.m_ActiveEdges = e.nextInLML;
			}
			else
			{
				tEdge.nextInAEL = e.nextInLML;
			}
			if (tEdge1 != null)
			{
				tEdge1.prevInAEL = e.nextInLML;
			}
			e.nextInLML.side = e.side;
			e.nextInLML.windDelta = e.windDelta;
			e.nextInLML.windCnt = e.windCnt;
			e.nextInLML.windCnt2 = e.windCnt2;
			e = e.nextInLML;
			e.prevInAEL = tEdge;
			e.nextInAEL = tEdge1;
			if (e.dx != -3.4E+38)
			{
				this.InsertScanbeam(e.ytop);
			}
		}

		internal class DoublePoint
		{
			public double X
			{
				get;
				set;
			}

			public double Y
			{
				get;
				set;
			}

			public DoublePoint(double x = 0, double y = 0)
			{
				this.X = x;
				this.Y = y;
			}
		}

		private class PolyOffsetBuilder
		{
			private const int buffLength = 128;

			private List<List<IntPoint>> pts;

			private List<IntPoint> currentPoly;

			private List<Clipper.DoublePoint> normals;

			private double delta;

			private double m_R;

			private int m_i;

			private int m_j;

			private int m_k;

			public PolyOffsetBuilder(List<List<IntPoint>> pts, List<List<IntPoint>> solution, double delta, JoinType jointype, double MiterLimit = 2, bool AutoFix = true)
			{
				if (delta == 0)
				{
					solution = pts;
					return;
				}
				this.pts = pts;
				this.delta = delta;
				if (AutoFix)
				{
					int count = pts.Count;
					int num = 0;
					while (num < count && pts[num].Count == 0)
					{
						num++;
					}
					if (num == count)
					{
						return;
					}
					IntPoint item = pts[num][0];
					for (int i = num; i < count; i++)
					{
						if (pts[i].Count != 0)
						{
							if (this.UpdateBotPt(pts[i][0], ref item))
							{
								num = i;
							}
							for (int j = pts[i].Count - 1; j > 0; j--)
							{
								if (ClipperBase.PointsEqual(pts[i][j], pts[i][j - 1]))
								{
									pts[i].RemoveAt(j);
								}
								else if (this.UpdateBotPt(pts[i][j], ref item))
								{
									num = i;
								}
							}
						}
					}
					if (!Clipper.Orientation(pts[num]))
					{
						Clipper.ReversePolygons(pts);
					}
				}
				if (MiterLimit <= 1)
				{
					MiterLimit = 1;
				}
				double miterLimit = 2 / (MiterLimit * MiterLimit);
				this.normals = new List<Clipper.DoublePoint>();
				solution.Clear();
				solution.Capacity = pts.Count;
				this.m_i = 0;
				while (this.m_i < pts.Count)
				{
					int count1 = pts[this.m_i].Count;
					if (count1 > 1 && pts[this.m_i][0].X == pts[this.m_i][count1 - 1].X && pts[this.m_i][0].Y == pts[this.m_i][count1 - 1].Y)
					{
						count1--;
					}
					if (count1 != 0 && (count1 >= 3 || delta > 0))
					{
						if (count1 != 1)
						{
							this.normals.Clear();
							this.normals.Capacity = count1;
							for (int k = 0; k < count1 - 1; k++)
							{
								this.normals.Add(Clipper.GetUnitNormal(pts[this.m_i][k], pts[this.m_i][k + 1]));
							}
							this.normals.Add(Clipper.GetUnitNormal(pts[this.m_i][count1 - 1], pts[this.m_i][0]));
							this.currentPoly = new List<IntPoint>();
							this.m_k = count1 - 1;
							this.m_j = 0;
							while (this.m_j < count1)
							{
								switch (jointype)
								{
									case JoinType.jtSquare:
									{
										this.DoSquare(1);
										break;
									}
									case JoinType.jtRound:
									{
										this.DoRound();
										break;
									}
									case JoinType.jtMiter:
									{
										this.m_R = 1 + (this.normals[this.m_j].X * this.normals[this.m_k].X + this.normals[this.m_j].Y * this.normals[this.m_k].Y);
										if (this.m_R < miterLimit)
										{
											this.DoSquare(MiterLimit);
											break;
										}
										else
										{
											this.DoMiter();
											break;
										}
									}
								}
								this.m_k = this.m_j;
								this.m_j++;
							}
							solution.Add(this.currentPoly);
						}
						else
						{
							List<IntPoint> intPoints = Clipper.BuildArc(pts[this.m_i][count1 - 1], 0, 6.28318530717959, delta);
							solution.Add(intPoints);
						}
					}
					this.m_i++;
				}
				Clipper clipper = new Clipper();
				clipper.AddPolygons(solution, PolyType.ptSubject);
				if (delta > 0)
				{
					clipper.Execute(ClipType.ctUnion, solution, PolyFillType.pftPositive, PolyFillType.pftPositive);
					return;
				}
				IntRect bounds = clipper.GetBounds();
				List<IntPoint> intPoints1 = new List<IntPoint>(4)
				{
					new IntPoint(bounds.left - (long)10, bounds.bottom + (long)10),
					new IntPoint(bounds.right + (long)10, bounds.bottom + (long)10),
					new IntPoint(bounds.right + (long)10, bounds.top - (long)10),
					new IntPoint(bounds.left - (long)10, bounds.top - (long)10)
				};
				clipper.AddPolygon(intPoints1, PolyType.ptSubject);
				clipper.Execute(ClipType.ctUnion, solution, PolyFillType.pftNegative, PolyFillType.pftNegative);
				if (solution.Count > 0)
				{
					solution.RemoveAt(0);
					for (int l = 0; l < solution.Count; l++)
					{
						solution[l].Reverse();
					}
				}
			}

			internal void AddPoint(IntPoint pt)
			{
				int count = this.currentPoly.Count;
				if (count == this.currentPoly.Capacity)
				{
					this.currentPoly.Capacity = count + 128;
				}
				this.currentPoly.Add(pt);
			}

			internal void DoMiter()
			{
				if ((this.normals[this.m_k].X * this.normals[this.m_j].Y - this.normals[this.m_j].X * this.normals[this.m_k].Y) * this.delta >= 0)
				{
					double mR = this.delta / this.m_R;
					this.AddPoint(new IntPoint(Clipper.Round((double)this.pts[this.m_i][this.m_j].X + (this.normals[this.m_k].X + this.normals[this.m_j].X) * mR), Clipper.Round((double)this.pts[this.m_i][this.m_j].Y + (this.normals[this.m_k].Y + this.normals[this.m_j].Y) * mR)));
					return;
				}
				IntPoint intPoint = new IntPoint(Clipper.Round((double)this.pts[this.m_i][this.m_j].X + this.normals[this.m_k].X * this.delta), Clipper.Round((double)this.pts[this.m_i][this.m_j].Y + this.normals[this.m_k].Y * this.delta));
				IntPoint intPoint1 = new IntPoint(Clipper.Round((double)this.pts[this.m_i][this.m_j].X + this.normals[this.m_j].X * this.delta), Clipper.Round((double)this.pts[this.m_i][this.m_j].Y + this.normals[this.m_j].Y * this.delta));
				this.AddPoint(intPoint);
				this.AddPoint(this.pts[this.m_i][this.m_j]);
				this.AddPoint(intPoint1);
			}

			internal void DoRound()
			{
				IntPoint intPoint = new IntPoint(Clipper.Round((double)this.pts[this.m_i][this.m_j].X + this.normals[this.m_k].X * this.delta), Clipper.Round((double)this.pts[this.m_i][this.m_j].Y + this.normals[this.m_k].Y * this.delta));
				IntPoint intPoint1 = new IntPoint(Clipper.Round((double)this.pts[this.m_i][this.m_j].X + this.normals[this.m_j].X * this.delta), Clipper.Round((double)this.pts[this.m_i][this.m_j].Y + this.normals[this.m_j].Y * this.delta));
				this.AddPoint(intPoint);
				if ((this.normals[this.m_k].X * this.normals[this.m_j].Y - this.normals[this.m_j].X * this.normals[this.m_k].Y) * this.delta < 0)
				{
					this.AddPoint(this.pts[this.m_i][this.m_j]);
				}
				else if (this.normals[this.m_j].X * this.normals[this.m_k].X + this.normals[this.m_j].Y * this.normals[this.m_k].Y < 0.985)
				{
					double num = Math.Atan2(this.normals[this.m_k].Y, this.normals[this.m_k].X);
					double num1 = Math.Atan2(this.normals[this.m_j].Y, this.normals[this.m_j].X);
					if (this.delta > 0 && num1 < num)
					{
						num1 += 6.28318530717959;
					}
					else if (this.delta < 0 && num1 > num)
					{
						num1 -= 6.28318530717959;
					}
					List<IntPoint> intPoints = Clipper.BuildArc(this.pts[this.m_i][this.m_j], num, num1, this.delta);
					for (int i = 0; i < intPoints.Count; i++)
					{
						this.AddPoint(intPoints[i]);
					}
				}
				this.AddPoint(intPoint1);
			}

			internal void DoSquare(double mul)
			{
				IntPoint intPoint = new IntPoint(Clipper.Round((double)this.pts[this.m_i][this.m_j].X + this.normals[this.m_k].X * this.delta), Clipper.Round((double)this.pts[this.m_i][this.m_j].Y + this.normals[this.m_k].Y * this.delta));
				IntPoint intPoint1 = new IntPoint(Clipper.Round((double)this.pts[this.m_i][this.m_j].X + this.normals[this.m_j].X * this.delta), Clipper.Round((double)this.pts[this.m_i][this.m_j].Y + this.normals[this.m_j].Y * this.delta));
				if ((this.normals[this.m_k].X * this.normals[this.m_j].Y - this.normals[this.m_j].X * this.normals[this.m_k].Y) * this.delta < 0)
				{
					this.AddPoint(intPoint);
					this.AddPoint(this.pts[this.m_i][this.m_j]);
					this.AddPoint(intPoint1);
					return;
				}
				double num = Math.Atan2(this.normals[this.m_k].Y, this.normals[this.m_k].X);
				double num1 = Math.Atan2(-this.normals[this.m_j].Y, -this.normals[this.m_j].X);
				num = Math.Abs(num1 - num);
				if (num > 3.14159265358979)
				{
					num = 6.28318530717959 - num;
				}
				double num2 = Math.Tan((3.14159265358979 - num) / 4) * Math.Abs(this.delta * mul);
				intPoint = new IntPoint((long)((double)intPoint.X - this.normals[this.m_k].Y * num2), (long)((double)intPoint.Y + this.normals[this.m_k].X * num2));
				this.AddPoint(intPoint);
				intPoint1 = new IntPoint((long)((double)intPoint1.X + this.normals[this.m_j].Y * num2), (long)((double)intPoint1.Y - this.normals[this.m_j].X * num2));
				this.AddPoint(intPoint1);
			}

			internal bool UpdateBotPt(IntPoint pt, ref IntPoint botPt)
			{
				if (pt.Y <= botPt.Y && (pt.Y != botPt.Y || pt.X >= botPt.X))
				{
					return false;
				}
				botPt = pt;
				return true;
			}
		}
	}
}