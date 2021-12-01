using System;
using System.Collections.Generic;
using System.IO;

namespace ClipperLib
{
#if use_int32
  using cInt = Int32;
#else
    using cInt = Int64;
#endif

    public class ClipperBase
    {
        protected const double horizontal = -3.4E+38;

        internal const long loRange = 1073741823L;

        internal const long hiRange = 4611686018427387903L;

        internal LocalMinima m_MinimaList;

        internal LocalMinima m_CurrentLM;

        internal List<List<TEdge>> m_edges = new List<List<TEdge>>();

        internal bool m_UseFullRange;

        internal ClipperBase()
        {
            this.m_MinimaList = null;
            this.m_CurrentLM = null;
            this.m_UseFullRange = false;
        }

        private TEdge AddBoundsToLML(TEdge e)
        {
            e.nextInLML = null;
            e = e.next;
            while (true)
            {
                if (e.dx != -3.4E+38)
                {
                    if (e.ycurr == e.prev.ycurr)
                    {
                        break;
                    }
                    e.nextInLML = e.prev;
                }
                else
                {
                    if (e.next.ytop < e.ytop && e.next.xbot > e.prev.xbot)
                    {
                        break;
                    }
                    if (e.xtop != e.prev.xbot)
                    {
                        this.SwapX(e);
                    }
                    e.nextInLML = e.prev;
                }
                e = e.next;
            }
            LocalMinima localMinima = new LocalMinima()
            {
                next = null,
                Y = e.prev.ybot
            };
            if (e.dx == -3.4E+38)
            {
                if (e.xbot != e.prev.xbot)
                {
                    this.SwapX(e);
                }
                localMinima.leftBound = e.prev;
                localMinima.rightBound = e;
            }
            else if (e.dx >= e.prev.dx)
            {
                localMinima.leftBound = e;
                localMinima.rightBound = e.prev;
            }
            else
            {
                localMinima.leftBound = e.prev;
                localMinima.rightBound = e;
            }
            localMinima.leftBound.side = EdgeSide.esLeft;
            localMinima.rightBound.side = EdgeSide.esRight;
            this.InsertLocalMinima(localMinima);
            while (e.next.ytop != e.ytop || e.next.dx == -3.4E+38)
            {
                e.nextInLML = e.next;
                e = e.next;
                if (e.dx != -3.4E+38 || e.xbot == e.prev.xtop)
                {
                    continue;
                }
                this.SwapX(e);
            }
            return e.next;
        }

        public bool AddPolygon(List<IntPoint> pg, PolyType polyType)
        {
            long num;
            int count = pg.Count;
            if (count < 3)
            {
                return false;
            }
            List<IntPoint> intPoints = new List<IntPoint>(count)
            {
                new IntPoint(pg[0].X, pg[0].Y)
            };
            int num1 = 0;
            for (int i = 1; i < count; i++)
            {
                num = (!this.m_UseFullRange ? (long)1073741823 : 4611686018427387903L);
                if (Math.Abs(pg[i].X) > num || Math.Abs(pg[i].Y) > num)
                {
                    if (Math.Abs(pg[i].X) > 4611686018427387903L || Math.Abs(pg[i].Y) > 4611686018427387903L)
                    {
                        throw new ClipperException("Coordinate exceeds range bounds");
                    }
                    num = 4611686018427387903L;
                    this.m_UseFullRange = true;
                }
                if (!ClipperBase.PointsEqual(intPoints[num1], pg[i]))
                {
                    if (num1 <= 0 || !this.SlopesEqual(intPoints[num1 - 1], intPoints[num1], pg[i], this.m_UseFullRange))
                    {
                        num1++;
                    }
                    else if (ClipperBase.PointsEqual(intPoints[num1 - 1], pg[i]))
                    {
                        num1--;
                    }
                    if (num1 >= intPoints.Count)
                    {
                        intPoints.Add(new IntPoint(pg[i].X, pg[i].Y));
                    }
                    else
                    {
                        intPoints[num1] = pg[i];
                    }
                }
            }
            if (num1 < 2)
            {
                return false;
            }
            for (count = num1 + 1; count > 2; count--)
            {
                if (ClipperBase.PointsEqual(intPoints[num1], intPoints[0]))
                {
                    num1--;
                }
                else if (ClipperBase.PointsEqual(intPoints[0], intPoints[1]) || this.SlopesEqual(intPoints[num1], intPoints[0], intPoints[1], this.m_UseFullRange))
                {
                    int num2 = num1;
                    num1 = num2 - 1;
                    intPoints[0] = intPoints[num2];
                }
                else if (!this.SlopesEqual(intPoints[num1 - 1], intPoints[num1], intPoints[0], this.m_UseFullRange))
                {
                    if (!this.SlopesEqual(intPoints[0], intPoints[1], intPoints[2], this.m_UseFullRange))
                    {
                        break;
                    }
                    for (int k = 2; k <= num1; k++)
                    {
                        intPoints[k - 1] = intPoints[k];
                    }
                    num1--;
                }
                else
                {
                    num1--;
                }
            }
            if (count < 3)
            {
                return false;
            }
            List<TEdge> tEdges = new List<TEdge>(count);
            for (int l = 0; l < count; l++)
            {
                tEdges.Add(new TEdge());
            }
            this.m_edges.Add(tEdges);
            tEdges[0].xcurr = intPoints[0].X;
            tEdges[0].ycurr = intPoints[0].Y;
            this.InitEdge(tEdges[count - 1], tEdges[0], tEdges[count - 2], intPoints[count - 1], polyType);
            for (int m = count - 2; m > 0; m--)
            {
                this.InitEdge(tEdges[m], tEdges[m + 1], tEdges[m - 1], intPoints[m], polyType);
            }
            this.InitEdge(tEdges[0], tEdges[1], tEdges[count - 1], intPoints[0], polyType);
            TEdge item = tEdges[0];
            TEdge tEdge = item;
            do
            {
                item.xcurr = item.xbot;
                item.ycurr = item.ybot;
                if (item.ytop < tEdge.ytop)
                {
                    tEdge = item;
                }
                item = item.next;
            }
            while (item != tEdges[0]);
            if (tEdge.windDelta > 0)
            {
                tEdge = tEdge.next;
            }
            if (tEdge.dx == -3.4E+38)
            {
                tEdge = tEdge.next;
            }
            item = tEdge;
            do
            {
                item = this.AddBoundsToLML(item);
            }
            while (item != tEdge);
            return true;
        }

        public bool AddPolygons(List<List<IntPoint>> ppg, PolyType polyType)
        {
            bool flag = false;
            for (int i = 0; i < ppg.Count; i++)
            {
                if (this.AddPolygon(ppg[i], polyType))
                {
                    flag = true;
                }
            }
            return flag;
        }

        public virtual void Clear()
        {
            this.DisposeLocalMinimaList();
            for (int i = 0; i < this.m_edges.Count; i++)
            {
                for (int j = 0; j < this.m_edges[i].Count; j++)
                {
                    this.m_edges[i][j] = null;
                }
                this.m_edges[i].Clear();
            }
            this.m_edges.Clear();
            this.m_UseFullRange = false;
        }

        private void DisposeLocalMinimaList()
        {
            while (this.m_MinimaList != null)
            {
                LocalMinima mMinimaList = this.m_MinimaList.next;
                this.m_MinimaList = null;
                this.m_MinimaList = mMinimaList;
            }
            this.m_CurrentLM = null;
        }

        public IntRect GetBounds()
        {
            IntRect intRect = new IntRect();
            LocalMinima mMinimaList = this.m_MinimaList;
            if (mMinimaList == null)
            {
                return intRect;
            }
            intRect.left = mMinimaList.leftBound.xbot;
            intRect.top = mMinimaList.leftBound.ybot;
            intRect.right = mMinimaList.leftBound.xbot;
            intRect.bottom = mMinimaList.leftBound.ybot;
            while (mMinimaList != null)
            {
                if (mMinimaList.leftBound.ybot > intRect.bottom)
                {
                    intRect.bottom = mMinimaList.leftBound.ybot;
                }
                TEdge tEdge = mMinimaList.leftBound;
                while (true)
                {
                    TEdge tEdge1 = tEdge;
                    while (tEdge.nextInLML != null)
                    {
                        if (tEdge.xbot < intRect.left)
                        {
                            intRect.left = tEdge.xbot;
                        }
                        if (tEdge.xbot > intRect.right)
                        {
                            intRect.right = tEdge.xbot;
                        }
                        tEdge = tEdge.nextInLML;
                    }
                    if (tEdge.xbot < intRect.left)
                    {
                        intRect.left = tEdge.xbot;
                    }
                    if (tEdge.xbot > intRect.right)
                    {
                        intRect.right = tEdge.xbot;
                    }
                    if (tEdge.xtop < intRect.left)
                    {
                        intRect.left = tEdge.xtop;
                    }
                    if (tEdge.xtop > intRect.right)
                    {
                        intRect.right = tEdge.xtop;
                    }
                    if (tEdge.ytop < intRect.top)
                    {
                        intRect.top = tEdge.ytop;
                    }
                    if (tEdge1 != mMinimaList.leftBound)
                    {
                        break;
                    }
                    tEdge = mMinimaList.rightBound;
                }
                mMinimaList = mMinimaList.next;
            }
            return intRect;
        }

        private void InitEdge(TEdge e, TEdge eNext, TEdge ePrev, IntPoint pt, PolyType polyType)
        {
            e.next = eNext;
            e.prev = ePrev;
            e.xcurr = pt.X;
            e.ycurr = pt.Y;
            if (e.ycurr < e.next.ycurr)
            {
                e.xtop = e.xcurr;
                e.ytop = e.ycurr;
                e.xbot = e.next.xcurr;
                e.ybot = e.next.ycurr;
                e.windDelta = -1;
            }
            else
            {
                e.xbot = e.xcurr;
                e.ybot = e.ycurr;
                e.xtop = e.next.xcurr;
                e.ytop = e.next.ycurr;
                e.windDelta = 1;
            }
            this.SetDx(e);
            e.polyType = polyType;
            e.outIdx = -1;
        }

        private void InsertLocalMinima(LocalMinima newLm)
        {
            if (this.m_MinimaList == null)
            {
                this.m_MinimaList = newLm;
                return;
            }
            if (newLm.Y >= this.m_MinimaList.Y)
            {
                newLm.next = this.m_MinimaList;
                this.m_MinimaList = newLm;
                return;
            }
            LocalMinima mMinimaList = this.m_MinimaList;
            while (mMinimaList.next != null && newLm.Y < mMinimaList.next.Y)
            {
                mMinimaList = mMinimaList.next;
            }
            newLm.next = mMinimaList.next;
            mMinimaList.next = newLm;
        }

        //------------------------------------------------------------------------------

        //See "The Point in Polygon Problem for Arbitrary Polygons" by Hormann & Agathos
        //http://citeseerx.ist.psu.edu/viewdoc/download?doi=10.1.1.88.5498&rep=rep1&type=pdf
        public static int PointInPolygon(IntPoint pt, OutPt op)
        {
            //returns 0 if false, +1 if true, -1 if pt ON polygon boundary
            int result = 0;
            OutPt startOp = op;
            cInt ptx = pt.X, pty = pt.Y;
            cInt poly0x = op.pt.X, poly0y = op.pt.Y;
            do
            {
                op = op.next;
                cInt poly1x = op.pt.X, poly1y = op.pt.Y;

                if (poly1y == pty)
                {
                    if ((poly1x == ptx) || (poly0y == pty &&
                      ((poly1x > ptx) == (poly0x < ptx)))) return -1;
                }
                if ((poly0y < pty) != (poly1y < pty))
                {
                    if (poly0x >= ptx)
                    {
                        if (poly1x > ptx) result = 1 - result;
                        else
                        {
                            double d = (double)(poly0x - ptx) * (poly1y - pty) -
                              (double)(poly1x - ptx) * (poly0y - pty);
                            if (d == 0) return -1;
                            if ((d > 0) == (poly1y > poly0y)) result = 1 - result;
                        }
                    }
                    else
                    {
                        if (poly1x > ptx)
                        {
                            double d = (double)(poly0x - ptx) * (poly1y - pty) -
                              (double)(poly1x - ptx) * (poly0y - pty);
                            if (d == 0) return -1;
                            if ((d > 0) == (poly1y > poly0y)) result = 1 - result;
                        }
                    }
                }
                poly0x = poly1x; poly0y = poly1y;
            } while (startOp != op);
            return result;
        }

        //internal bool PointInPolygon(IntPoint pt, OutPt pp, bool UseFulllongRange)
        //{
        //	OutPt outPt = pp;
        //	bool flag = false;
        //	if (!UseFulllongRange)
        //	{
        //		do
        //		{
        //			if ((outPt.pt.Y <= pt.Y && pt.Y < outPt.prev.pt.Y || outPt.prev.pt.Y <= pt.Y && pt.Y < outPt.pt.Y) && pt.X - outPt.pt.X < (outPt.prev.pt.X - outPt.pt.X) * (pt.Y - outPt.pt.Y) / (outPt.prev.pt.Y - outPt.pt.Y))
        //			{
        //				flag = !flag;
        //			}
        //			outPt = outPt.next;
        //		}
        //		while (outPt != pp);
        //	}
        //	else
        //	{
        //		do
        //		{
        //			if ((outPt.pt.Y <= pt.Y && pt.Y < outPt.prev.pt.Y || outPt.prev.pt.Y <= pt.Y && pt.Y < outPt.pt.Y) && new Int128(pt.X - outPt.pt.X) < (Int128.Int128Mul(outPt.prev.pt.X - outPt.pt.X, pt.Y - outPt.pt.Y) / new Int128(outPt.prev.pt.Y - outPt.pt.Y)))
        //			{
        //				flag = !flag;
        //			}
        //			outPt = outPt.next;
        //		}
        //		while (outPt != pp);
        //	}
        //	return flag;
        //}

        internal bool PointIsVertex(IntPoint pt, OutPt pp)
        {
            OutPt outPt = pp;
            do
            {
                if (ClipperBase.PointsEqual(outPt.pt, pt))
                {
                    return true;
                }
                outPt = outPt.next;
            }
            while (outPt != pp);
            return false;
        }

        protected static bool PointsEqual(IntPoint pt1, IntPoint pt2)
        {
            if (pt1.X != pt2.X)
            {
                return false;
            }
            return pt1.Y == pt2.Y;
        }

        protected void PopLocalMinima()
        {
            if (this.m_CurrentLM == null)
            {
                return;
            }
            this.m_CurrentLM = this.m_CurrentLM.next;
        }

        protected virtual void Reset()
        {
            TEdge j;
            this.m_CurrentLM = this.m_MinimaList;
            for (LocalMinima i = this.m_MinimaList; i != null; i = i.next)
            {
                for (j = i.leftBound; j != null; j = j.nextInLML)
                {
                    j.xcurr = j.xbot;
                    j.ycurr = j.ybot;
                    j.side = EdgeSide.esLeft;
                    j.outIdx = -1;
                }
                for (j = i.rightBound; j != null; j = j.nextInLML)
                {
                    j.xcurr = j.xbot;
                    j.ycurr = j.ybot;
                    j.side = EdgeSide.esRight;
                    j.outIdx = -1;
                }
            }
        }

        private void SetDx(TEdge e)
        {
            e.deltaX = e.xtop - e.xbot;
            e.deltaY = e.ytop - e.ybot;
            if (e.deltaY == (long)0)
            {
                e.dx = -3.4E+38;
                return;
            }
            e.dx = (double)e.deltaX / (double)e.deltaY;
        }

        internal bool SlopesEqual(TEdge e1, TEdge e2, bool UseFullRange)
        {
            if (UseFullRange)
            {
                return Int128.Int128Mul(e1.deltaY, e2.deltaX) == Int128.Int128Mul(e1.deltaX, e2.deltaY);
            }
            return e1.deltaY * e2.deltaX == e1.deltaX * e2.deltaY;
        }

        protected bool SlopesEqual(IntPoint pt1, IntPoint pt2, IntPoint pt3, bool UseFullRange)
        {
            if (UseFullRange)
            {
                return Int128.Int128Mul(pt1.Y - pt2.Y, pt2.X - pt3.X) == Int128.Int128Mul(pt1.X - pt2.X, pt2.Y - pt3.Y);
            }
            return (pt1.Y - pt2.Y) * (pt2.X - pt3.X) - (pt1.X - pt2.X) * (pt2.Y - pt3.Y) == (long)0;
        }

        protected bool SlopesEqual(IntPoint pt1, IntPoint pt2, IntPoint pt3, IntPoint pt4, bool UseFullRange)
        {
            if (UseFullRange)
            {
                return Int128.Int128Mul(pt1.Y - pt2.Y, pt3.X - pt4.X) == Int128.Int128Mul(pt1.X - pt2.X, pt3.Y - pt4.Y);
            }
            return (pt1.Y - pt2.Y) * (pt3.X - pt4.X) - (pt1.X - pt2.X) * (pt3.Y - pt4.Y) == (long)0;
        }

        private void SwapX(TEdge e)
        {
            e.xcurr = e.xtop;
            e.xtop = e.xbot;
            e.xbot = e.xcurr;
        }
    }
}