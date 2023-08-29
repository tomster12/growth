﻿/**
 * Copyright 2019 Oskar Sigvardsson
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 *
 * NOTICE: This file has been significantly changed compared to the original.
 */


using System;
using System.Collections.Generic;
using UnityEngine;


namespace GK
{
    public class VoronoiClipper
    {
        public enum VertexType { SITE_VERTEX, POLYGON, POLYGON_INTERSECTION }

        [Serializable]
        public class ClippedVertex
        {
            public Vector2 vertex;
            public VertexType type;
            public int vertexUID = -1;
            public int intersectionFromUID = -1, intersectionToUID = -1;
            
            private ClippedVertex(Vector2 vertex, VertexType type)
            {
                this.vertex = vertex;
                this.type = type;
            }

            public static ClippedVertex NewPolygonVertex(Vector2 vertex, int vertexUID)
            {
                ClippedVertex v = new ClippedVertex(vertex, VertexType.POLYGON);
                v.vertexUID = vertexUID;
                v.intersectionFromUID = vertexUID;
                v.intersectionToUID = vertexUID;
                return v;
            }

            public static ClippedVertex NewPolygonIntersectionVertex(Vector2 vertex, int vertexUID, int intersectionFromUID, int intersectionToUID)
            {
                ClippedVertex v = new ClippedVertex(vertex, VertexType.POLYGON_INTERSECTION);
                v.vertexUID = vertexUID;
                v.intersectionFromUID = intersectionFromUID;
                v.intersectionToUID = intersectionToUID;
                return v;
            }

            public static ClippedVertex NewSiteVertex(Vector2 vertex, int vertexUID)
            {
                ClippedVertex v = new ClippedVertex(vertex, VertexType.SITE_VERTEX);
                v.vertexUID = vertexUID;
                return v;
            }
        }

        [Serializable]
        public class ClippedSite
        {
            public List<ClippedVertex> clippedVertices;
            public Vector2 clippedCentroid;
        }


        /// <summary>
        /// List of all the clipped sites
        /// </summary>
        private Dictionary<string, int> intersectionIndexMap = new Dictionary<String, int>();
        private int nextIntersectionIndex = -1;
        public List<ClippedSite> clippedSites;


        /// <summary>
        /// Create a new Voronoi clipper
        /// </summary>
        public VoronoiClipper() { }


        /// <summary>
        /// Clip all sites of a voronoi diagram iteratively.
        /// </summary>
        public void ClipDiagram(VoronoiDiagram diag, List<Vector2> clipperPolygon)
        {
            // Reset variables
            intersectionIndexMap.Clear();
            nextIntersectionIndex = diag.Vertices.Count + clipperPolygon.Count;
            clippedSites = new List<ClippedSite>();

            // Iterate through and clip each site
            for (int i = 0; i < diag.Sites.Count; i++)
            {
                ClippedSite clippedSite = ClipSite(diag, clipperPolygon, i);
                clippedSites.Add(clippedSite);
            }
        }


        /// <summary>
        /// Clip site of voronoi diagram using polygon (must be convex),
        /// returning the clipped vertices in clipped list. Modifies neither
        /// polygon nor diagram, so can be run in parallel for several sites at
        /// once. 
        /// </summary>
        private ClippedSite ClipSite(VoronoiDiagram diag, List<Vector2> polygon, int site)
        {
            // Initialize variables
            List<ClippedVertex> verticesCurrent = new List<ClippedVertex>();
            List<ClippedVertex> verticesNext = new List<ClippedVertex>();

            // Setup intersection indexing
            Func<int, int, int, int, int> getIntersectionUI = (int ev0, int ev1, int v0, int v1) =>
            {
                String key = "";
                key += (ev0 < ev1) ? (ev0 + "," + ev1 + ",") : (ev1 + "," + ev0 + ",");
                key += (v0 < v1) ? (v0 + "," + v1) : (v1 + "," + v0);
                if (!intersectionIndexMap.ContainsKey(key)) intersectionIndexMap.Add(key, nextIntersectionIndex++);
                int intersectionUID = intersectionIndexMap[key];
                return intersectionUID;
            };

            // Setup polygon points
            verticesCurrent.Clear();
            for (int i = 0; i < polygon.Count; i++)
            {
                verticesCurrent.Add(ClippedVertex.NewPolygonVertex( polygon[i], diag.Vertices.Count + i));
            }


            // Find first / last edge of site
            int firstEdge = diag.FirstEdgeBySite[site];
            int lastEdge;
            if (site == diag.Sites.Count - 1) lastEdge = diag.Edges.Count - 1;
            else lastEdge = diag.FirstEdgeBySite[site + 1] - 1;

            // Loop over each edge and extract start / direction
            for (int ei = firstEdge; ei <= lastEdge; ei++)
            {
                verticesNext.Clear();
                var edge = diag.Edges[ei];
                Vector2 lv, ld;


                // - Edge is ray so take direction
                if (edge.Type == VoronoiDiagram.EdgeType.RayCCW || edge.Type == VoronoiDiagram.EdgeType.RayCW)
                {
                    lv = diag.Vertices[edge.Vert0];
                    ld = edge.Direction;
                    if (edge.Type == VoronoiDiagram.EdgeType.RayCW) ld *= -1;

                }

                // - Edge is segment so create direction
                else if (edge.Type == VoronoiDiagram.EdgeType.Segment)
                {
                    var lcv0 = diag.Vertices[edge.Vert0];
                    var lcv1 = diag.Vertices[edge.Vert1];
                    lv = lcv0;
                    ld = lcv1 - lcv0;

                }

                // - Edge is line and not supported
                else if (edge.Type == VoronoiDiagram.EdgeType.Line)
                {
                    throw new NotSupportedException("Haven't implemented voronoi halfplanes yet");
                }

                // - Should not happen
                else { Debug.Assert(false); return null; }


                // Trim all external vertices down based on current edge
                for (int cvi0 = 0; cvi0 < verticesCurrent.Count; cvi0++)
                {
                    var cvi1 = cvi0 == verticesCurrent.Count - 1 ? 0 : cvi0 + 1;
                    var cv0 = verticesCurrent[cvi0];
                    var cv1 = verticesCurrent[cvi1];
                    var v0 = cv0.vertex;
                    var v1 = cv1.vertex;
                    var p0Inside = Geom.ToTheLeft(v0, lv, lv + ld);
                    var p1Inside = Geom.ToTheLeft(v1, lv, lv + ld);


                    // - Clipped edge is firmly inside - add cp1 because cp0 is already added
                    if (p0Inside && p1Inside)
                    {
                        verticesNext.Add(cv1);
                    }

                    // - Clipped edge is firmly outside - fully ignore
                    else if (!p0Inside && !p1Inside) { }

                    // - On the boundary
                    else
                    {
                        float m0, m1;
                        Geom.LineLineIntersection(lv, ld.normalized, v0, (v1 - v0).normalized, out m0, out m1);
                        var intersection = lv + ld.normalized * m0;

                        // - Intersecting an outer edge
                        if (
                            cv0.type == VertexType.POLYGON || cv1.type == VertexType.POLYGON
                            || (cv0.intersectionFromUID == cv1.intersectionFromUID && cv0.intersectionFromUID != -1)
                        ) {
                            int intersectionUID = getIntersectionUI(edge.Vert0, edge.Vert1, cv0.intersectionFromUID, cv1.intersectionToUID);
                            verticesNext.Add(ClippedVertex.NewPolygonIntersectionVertex( intersection, intersectionUID, cv0.intersectionFromUID, cv1.intersectionToUID ));
                        }

                        // - Intersection at a site point
                        else
                        {
                            verticesNext.Add(ClippedVertex.NewSiteVertex( intersection, m0 < 0.0001f ? edge.Vert0 : edge.Vert1 ));
                        }
                        
                        // - Points straddling start of inside
                        if (p1Inside) verticesNext.Add(cv1);
                    }
                }


                // Update to use next set of points
                var tmp = verticesCurrent;
                verticesCurrent = verticesNext;
                verticesNext = tmp;
            }


            // Create clipped site
            ClippedSite clippedSite = new ClippedSite();
            clippedSite.clippedCentroid = diag.Sites[site];
            clippedSite.clippedVertices = new List<ClippedVertex>();
            clippedSite.clippedVertices.AddRange(verticesCurrent);
            return clippedSite;
        }
    }
}