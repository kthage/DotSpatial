// ********************************************************************************************************
// Product Name: DotSpatial.Topology.dll
// Description:  The basic topology module for the new dotSpatial libraries
// ********************************************************************************************************
// The contents of this file are subject to the Lesser GNU Public License (LGPL)
// you may not use this file except in compliance with the License. You may obtain a copy of the License at
// http://dotspatial.codeplex.com/license  Alternately, you can access an earlier version of this content from
// the Net Topology Suite, which is also protected by the GNU Lesser Public License and the sourcecode
// for the Net Topology Suite can be obtained here: http://sourceforge.net/projects/nts.
//
// Software distributed under the License is distributed on an "AS IS" basis, WITHOUT WARRANTY OF
// ANY KIND, either expressed or implied. See the License for the specific language governing rights and
// limitations under the License.
//
// The Original Code is from the Net Topology Suite, which is a C# port of the Java Topology Suite.
//
// The Initial Developer to integrate this code into MapWindow 6.0 is Ted Dunsford.
//
// Contributor(s): (Open source contributors should list themselves and their modifications here).
// |         Name         |    Date    |                              Comment
// |----------------------|------------|------------------------------------------------------------
// |                      |            |
// ********************************************************************************************************

using System;
using System.Collections.Generic;
using DotSpatial.Topology.Geometries;
using DotSpatial.Topology.Utilities;

namespace DotSpatial.Topology.Index.Strtree
{
    /// <summary>
    /// A query-only R-tree created using the Sort-Tile-Recursive (STR) algorithm.
    /// For two-dimensional spatial data.
    /// The STR packed R-tree is simple to implement and maximizes space
    /// utilization; that is, as many leaves as possible are filled to capacity.
    /// Overlap between nodes is far less than in a basic R-tree. However, once the
    /// tree has been built (explicitly or on the first call to #query), items may
    /// not be added or removed.
    /// Described in: P. Rigaux, Michel Scholl and Agnes Voisard. Spatial Databases With
    /// Application To GIS. Morgan Kaufmann, San Francisco, 2002.
    /// </summary>
    [Serializable]
    public class STRtree<TItem> : AbstractSTRtree<IEnvelope, TItem>, ISpatialIndex<TItem>
    {
        #region Constant Fields

        private const int DefaultNodeCapacity = 10;

        #endregion

        #region Fields

        private static readonly IIntersectsOp IntersectsOperation = new AnonymousIntersectsOpImpl();
        private static readonly AnonymousXComparerImpl XComparer = new AnonymousXComparerImpl();
        private static readonly AnonymousYComparerImpl YComparer = new AnonymousYComparerImpl();

        #endregion

        #region Constructors

        /// <summary>
        /// Constructs an STRtree with the default (10) node capacity.
        /// </summary>
        public STRtree() : this(DefaultNodeCapacity)
        {
        }

        /// <summary>
        /// Constructs an STRtree with the given maximum number of child nodes that
        /// a node may have.
        /// </summary>
        /// <remarks>The minimum recommended capacity setting is 4.</remarks>
        public STRtree(int nodeCapacity) :
            base(nodeCapacity)
        {
        }

        #endregion

        #region Properties

        /// <summary>
        ///
        /// </summary>
        protected override IIntersectsOp IntersectsOp
        {
            get { return IntersectsOperation; }
        }

        #endregion

        #region Methods

        /// <summary>
        ///
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        private static double Avg(double a, double b)
        {
            return (a + b) / 2d;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        private static double CentreX(IEnvelope e)
        {
            return Avg(e.Minimum.X, e.Maximum.X);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        private static double CentreY(IEnvelope e)
        {
            return Avg(e.Minimum.Y, e.Maximum.Y);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="level"></param>
        /// <returns></returns>
        protected override AbstractNode<IEnvelope, TItem> CreateNode(int level)
        {
            return new AnonymousAbstractNodeImpl(level);
        }

        /// <summary>
        /// Creates the parent level for the given child level. First, orders the items
        /// by the x-values of the midpoints, and groups them into vertical slices.
        /// For each slice, orders the items by the y-values of the midpoints, and
        /// group them into runs of size M (the node capacity). For each run, creates
        /// a new (parent) node.
        /// </summary>
        /// <param name="childBoundables"></param>
        /// <param name="newLevel"></param>
        protected override IList<IBoundable<IEnvelope, TItem>> CreateParentBoundables(IList<IBoundable<IEnvelope, TItem>> childBoundables, int newLevel)
        {
            Assert.IsTrue(childBoundables.Count != 0);
            var minLeafCount = (int)Math.Ceiling((childBoundables.Count / (double)NodeCapacity));
            var sortedChildBoundables = new List<IBoundable<IEnvelope, TItem>>(childBoundables);
            sortedChildBoundables.Sort(XComparer);
            var verticalSlices = VerticalSlices(sortedChildBoundables,
                                                    (int)Math.Ceiling(Math.Sqrt(minLeafCount)));
            var tempList = CreateParentBoundablesFromVerticalSlices(verticalSlices, newLevel);
            return tempList;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="childBoundables"></param>
        /// <param name="newLevel"></param>
        /// <returns></returns>
        protected IList<IBoundable<IEnvelope, TItem>> CreateParentBoundablesFromVerticalSlice(IList<IBoundable<IEnvelope, TItem>> childBoundables, int newLevel)
        {
            return base.CreateParentBoundables(childBoundables, newLevel);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="verticalSlices"></param>
        /// <param name="newLevel"></param>
        /// <returns></returns>
        private List<IBoundable<IEnvelope, TItem>> CreateParentBoundablesFromVerticalSlices(IList<IBoundable<IEnvelope, TItem>>[] verticalSlices, int newLevel)
        {
            Assert.IsTrue(verticalSlices.Length > 0);
            var parentBoundables = new List<IBoundable<IEnvelope, TItem>>();
            for (int i = 0; i < verticalSlices.Length; i++)
            {
                var tempList = CreateParentBoundablesFromVerticalSlice(verticalSlices[i], newLevel);
                foreach (var o in tempList)
                    parentBoundables.Add(o);
            }
            return parentBoundables;
        }

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        protected override IComparer<IBoundable<IEnvelope, TItem>> GetComparer()
        {
            return YComparer;
        }

        /// <summary>
        /// Inserts an item having the given bounds into the tree.
        /// </summary>
        /// <param name="itemEnv"></param>
        /// <param name="item"></param>
        public new void Insert(IEnvelope itemEnv, TItem item)
        {
            if (itemEnv.IsNull)
                return;
            base.Insert(itemEnv, item);
        }

        /// <summary>
        /// Finds the two nearest items in the tree, 
        /// using <see cref="IItemDistance{IEnvelope, TItem}"/> as the distance metric.
        /// A Branch-and-Bound tree traversal algorithm is used
        /// to provide an efficient search.
        /// </summary>
        /// <param name="itemDist">A distance metric applicable to the items in this tree</param>
        /// <returns>The pair of the nearest items</returns>
        public TItem[] NearestNeighbour(IItemDistance<IEnvelope, TItem> itemDist)
        {
            var bp = new BoundablePair<TItem>(Root, Root, itemDist);
            return NearestNeighbour(bp);
        }

        /// <summary>
        /// Finds the item in this tree which is nearest to the given <paramref name="item"/>, 
        /// using <see cref="IItemDistance{IEnvelope,TItem}"/> as the distance metric.
        /// A Branch-and-Bound tree traversal algorithm is used
        /// to provide an efficient search.
        /// <para/>
        /// The query <paramref name="item"/> does <b>not</b> have to be 
        /// contained in the tree, but it does 
        /// have to be compatible with the <paramref name="itemDist"/> 
        /// distance metric. 
        /// </summary>
        /// <param name="env">The IEnvelope of the query item</param>
        /// <param name="item">The item to find the nearest neighbour of</param>
        /// <param name="itemDist">A distance metric applicable to the items in this tree and the query item</param>
        /// <returns>The nearest item in this tree</returns>
        public TItem NearestNeighbour(IEnvelope env, TItem item, IItemDistance<IEnvelope, TItem> itemDist)
        {
            var bnd = new ItemBoundable<IEnvelope, TItem>(env, item);
            var bp = new BoundablePair<TItem>(Root, bnd, itemDist);
            return NearestNeighbour(bp)[0];
        }

        /// <summary>
        /// Finds the two nearest items from this tree 
        /// and another tree,
        /// using <see cref="IItemDistance{IEnvelope, TItem}"/> as the distance metric.
        /// A Branch-and-Bound tree traversal algorithm is used
        /// to provide an efficient search.
        /// The result value is a pair of items, 
        /// the first from this tree and the second
        /// from the argument tree.
        /// </summary>
        /// <param name="tree">Another tree</param>
        /// <param name="itemDist">A distance metric applicable to the items in the trees</param>
        /// <returns>The pair of the nearest items, one from each tree</returns>
        public TItem[] NearestNeighbour(STRtree<TItem> tree, IItemDistance<IEnvelope, TItem> itemDist)
        {
            var bp = new BoundablePair<TItem>(Root, tree.Root, itemDist);
            return NearestNeighbour(bp);
        }

        private static TItem[] NearestNeighbour(BoundablePair<TItem> initBndPair)
        {
            return NearestNeighbour(initBndPair, Double.PositiveInfinity);
        }

        private static TItem[] NearestNeighbour(BoundablePair<TItem> initBndPair, double maxDistance)
        {
            var distanceLowerBound = maxDistance;
            BoundablePair<TItem> minPair = null;

            // initialize internal structures
            var priQ = new PriorityQueue<BoundablePair<TItem>>();

            // initialize queue
            priQ.Add(initBndPair);

            while (!priQ.IsEmpty() && distanceLowerBound > 0.0)
            {
                // pop head of queue and expand one side of pair
                var bndPair = priQ.Poll();
                var currentDistance = bndPair.Distance; //bndPair.GetDistance();

                /**
                 * If the distance for the first node in the queue
                 * is >= the current minimum distance, all other nodes
                 * in the queue must also have a greater distance.
                 * So the current minDistance must be the true minimum,
                 * and we are done.
                 */
                if (currentDistance >= distanceLowerBound)
                    break;

                /**
                 * If the pair members are leaves
                 * then their distance is the exact lower bound.
                 * Update the distanceLowerBound to reflect this
                 * (which must be smaller, due to the test 
                 * immediately prior to this). 
                 */
                if (bndPair.IsLeaves)
                {
                    // assert: currentDistance < minimumDistanceFound
                    distanceLowerBound = currentDistance;
                    minPair = bndPair;
                }
                else
                {
                    // testing - does allowing a tolerance improve speed?
                    // Ans: by only about 10% - not enough to matter
                    /*
                    double maxDist = bndPair.getMaximumDistance();
                    if (maxDist * .99 < lastComputedDistance) 
                      return;
                    //*/

                    /**
                     * Otherwise, expand one side of the pair,
                     * (the choice of which side to expand is heuristically determined) 
                     * and insert the new expanded pairs into the queue
                     */
                    bndPair.ExpandToQueue(priQ, distanceLowerBound);
                }
            }
            if (minPair != null)
                // done - return items with min distance
                return new[]
                       {
                           ((ItemBoundable<IEnvelope, TItem>) minPair.GetBoundable(0)).Item,
                           ((ItemBoundable<IEnvelope, TItem>) minPair.GetBoundable(1)).Item
                       };
            return null;
        }

        /// <summary>
        /// Returns items whose bounds intersect the given IEnvelope.
        /// </summary>
        /// <param name="searchEnv"></param>
        public new IList<TItem> Query(IEnvelope searchEnv)
        {
            //Yes this method does something. It specifies that the bounds is an
            //IEnvelope. super.query takes an object, not an IEnvelope. [Jon Aquino 10/24/2003]
            return base.Query(searchEnv);
        }

        /// <summary>
        /// Returns items whose bounds intersect the given IEnvelope.
        /// </summary>
        /// <param name="searchEnv"></param>
        /// <param name="visitor"></param>
        public new void Query(IEnvelope searchEnv, IItemVisitor<TItem> visitor)
        {
            //Yes this method does something. It specifies that the bounds is an
            //IEnvelope. super.query takes an Object, not an IEnvelope. [Jon Aquino 10/24/2003]
            base.Query(searchEnv, visitor);
        }

        /// <summary>
        /// Removes a single item from the tree.
        /// </summary>
        /// <param name="itemEnv">The IEnvelope of the item to remove.</param>
        /// <param name="item">The item to remove.</param>
        /// <returns><c>true</c> if the item was found.</returns>
        public new bool Remove(IEnvelope itemEnv, TItem item)
        {
            return base.Remove(itemEnv, item);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="childBoundables">Must be sorted by the x-value of the IEnvelope midpoints.</param>
        /// <param name="sliceCount"></param>
        protected IList<IBoundable<IEnvelope, TItem>>[] VerticalSlices(IList<IBoundable<IEnvelope, TItem>> childBoundables, int sliceCount)
        {
            var sliceCapacity = (int)Math.Ceiling(childBoundables.Count / (double)sliceCount);
            var slices = new IList<IBoundable<IEnvelope, TItem>>[sliceCount];
            var i = childBoundables.GetEnumerator();
            for (var j = 0; j < sliceCount; j++)
            {
                slices[j] = new List<IBoundable<IEnvelope, TItem>>();
                var boundablesAddedToSlice = 0;
                /* 
                 *          Diego Guidi says:
                 *          the line below introduce an error: 
                 *          the first element at the iteration (not the first) is lost! 
                 *          This is simply a different implementation of Iteration in .NET against Java
                 */
                // while (i.MoveNext() && boundablesAddedToSlice < sliceCapacity)
                while (boundablesAddedToSlice < sliceCapacity && i.MoveNext())
                {
                    var childBoundable = i.Current;
                    slices[j].Add(childBoundable);
                    boundablesAddedToSlice++;
                }
            }
            return slices;
        }

        #endregion

        #region Classes

        [Serializable]
        private class AnonymousAbstractNodeImpl : AbstractNode<IEnvelope, TItem>
        {
            #region Constructors

            public AnonymousAbstractNodeImpl(int nodeCapacity) :
                base(nodeCapacity)
            {
            }

            #endregion

            #region Methods

            protected override IEnvelope ComputeBounds()
            {
                IEnvelope bounds = new Envelope();
                foreach (var childBoundable in ChildBoundables)
                {
                    bounds.ExpandToInclude(childBoundable.Bounds);
                }
                return bounds.IsNull ? null : bounds;
            }

            #endregion
        }

        private class AnonymousIntersectsOpImpl : IIntersectsOp
        {
            #region Methods

            public bool Intersects(IEnvelope aBounds, IEnvelope bBounds)
            {
                return aBounds.Intersects(bBounds);
            }

            #endregion
        }

        private class AnonymousXComparerImpl : Comparer<IBoundable<IEnvelope, TItem>>
        {
            #region Methods

            public override int Compare(IBoundable<IEnvelope, TItem> o1, IBoundable<IEnvelope, TItem> o2)
            {
                return CompareDoubles(CentreX(o1.Bounds),
                                      CentreX(o2.Bounds));
            }

            #endregion
        }

        private class AnonymousYComparerImpl : Comparer<IBoundable<IEnvelope, TItem>>
        {
            #region Methods

            public override int Compare(IBoundable<IEnvelope, TItem> o1, IBoundable<IEnvelope, TItem> o2)
            {
                return CompareDoubles(CentreY(o1.Bounds),
                                      CentreY(o2.Bounds));
            }

            #endregion
        }

        #endregion
    }
}