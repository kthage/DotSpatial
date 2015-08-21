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

using System.Collections.Generic;
using DotSpatial.Topology.Geometries;
using DotSpatial.Topology.GeometriesGraph;

namespace DotSpatial.Topology.Operation.Overlay
{
    /// <summary>
    /// Constructs <c>Point</c>s from the nodes of an overlay graph.
    /// </summary>
    public class PointBuilder
    {
        #region Fields

        private readonly IGeometryFactory _geometryFactory;
        private readonly OverlayOp _op;

        #endregion

        #region Constructors

        ///  <summary>
        /// 
        ///  </summary>
        /// <param name="op"></param>
        /// <param name="geometryFactory"></param>
        public PointBuilder(OverlayOp op, IGeometryFactory geometryFactory)
        {
            _op = op;
            _geometryFactory = geometryFactory;
        }

        #endregion

        #region Methods

        /// <summary>
        ///
        /// </summary>
        /// <param name="opCode"></param>
        /// <returns>
        /// A list of the Points in the result of the specified overlay operation.
        /// </returns>
        public IList<IGeometry> Build(SpatialFunction opCode)
        {
            var nodeList = CollectNodes(opCode);
            var resultPointList = SimplifyPoints(nodeList);
            return resultPointList;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="opCode"></param>
        /// <returns></returns>
        private IList<Node> CollectNodes(SpatialFunction opCode)
        {
            IList<Node> resultNodeList = new List<Node>();
            // add nodes from edge intersections which have not already been included in the result
            foreach (Node n in _op.Graph.Nodes)
            {
                if (!n.IsInResult)
                {
                    Label label = n.Label;
                    if (OverlayOp.IsResultOfOp(label, opCode))                    
                        resultNodeList.Add(n);                    
                }
            }
            return resultNodeList;
        }

        /// <summary>
        /// This method simplifies the resultant Geometry by finding and eliminating
        /// "covered" points.
        /// A point is covered if it is contained in another element Geometry
        /// with higher dimension (e.g. a point might be contained in a polygon,
        /// in which case the point can be eliminated from the resultant).
        /// </summary>
        /// <param name="resultNodeList"></param>
        /// <returns></returns>
        private IList<IGeometry> SimplifyPoints(IEnumerable<Node> resultNodeList)
        {
            IList<IGeometry> nonCoveredPointList = new List<IGeometry>();
            foreach (Node n in resultNodeList)
            {
                Coordinate coord = n.Coordinate;
                if (!_op.IsCoveredByLA(coord))
                {
                    IPoint pt = _geometryFactory.CreatePoint(coord);
                    nonCoveredPointList.Add(pt);
                }
            }
            return nonCoveredPointList;
        }

        #endregion
    }
}