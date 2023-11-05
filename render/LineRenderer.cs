using System;
using System.Collections.Generic;
using System.Linq;

namespace AnimLib;

/// <summary>
/// A line renderer that turns a linear spline into a triangle mesh geometry.
/// </summary>
public static class LineRenderer {

    public static void UpdateLineMesh(ColoredTriangleMeshGeometry geometry, Vector3[] line, float width, Color color, string guid){ 
        // TODO: this hits GC HARD
        var geom = LineToMesh(line, width, color, guid);
        geometry.colors = geom.colors;
        geometry.edgeCoordinates = geom.edgeCoordinates;
        geometry.indices = geom.indices;
        geometry.vertices = geom.vertices;
        geometry.Dirty = true;
    }

    public static void UpdateLineMesh(ColoredTriangleMeshGeometry geometry, Vector3[] line, Color[] colors, float width, string guid) {
        // TODO: this hits GC HARD
        var geom = LineToMesh(line, colors, width, guid);
        geometry.colors = geom.colors;
        geometry.edgeCoordinates = geom.edgeCoordinates;
        geometry.indices = geom.indices;
        geometry.vertices = geom.vertices;
        geometry.Dirty = true;
    }

    public static ColoredTriangleMeshGeometry LineToMesh(Vector3[] line, float width, Color color, string guid) {
        var colors = Enumerable.Repeat(color, line.Length).ToArray();
        return LineToMesh(line, colors, width, guid);
    }
    public static ColoredTriangleMeshGeometry LineToMesh(Vector3[] line, Color[] colors, float width, string guid) {
        var vertices = new List<Vector3>(); 
        var indices = new List<uint>();
        var vcolors = new List<Color>();
        var edgeCoords = new List<Vector2>();
        Vector2 prevDir = Vector2.RIGHT;
        Vector2 prevCWP = Vector2.ZERO;
        uint prevS1Idx = 1;
        uint prevEnd = 0;
        vertices.Add(line[0]);
        edgeCoords.Add(new Vector2(0.0f, 0.0f));
        vcolors.Add(colors[0]);
        for(int i = 0; i < line.Length-1; i++) {
            uint s1Idx = (uint)vertices.Count;
            Vector2 start = line[i];
            Vector2 end = line[i + 1];
            if((end-start).Length < float.Epsilon*16) {
                continue;
            }
            Vector2 dir = (end-start).Normalized;
            // clockwise perpendicular vector
            Vector2 cwP = width*0.5f * new Vector2(dir.y, -dir.x);
            Vector2 ccwP = width*0.5f * new Vector2(-dir.y, dir.x);

            //  s2   (1)                  e2  (1)
            // start (0) --------------> end  (0)
            //  s1   (1)                  e1  (1)

            Vector2 s1 = start + cwP;
            Vector2 s2 = start + ccwP;
            Vector2 e1 = end + cwP;
            Vector2 e2 = end + ccwP;

            Vector2 bisector;
            float dot = Vector2.Dot(dir, prevDir);
            if(dot < 0.99f && i != 0) { // current and previous line segment are in same direction
                bisector = (dir-prevDir)*0.5f;
                bisector.Normalize();
                dot = MathF.Abs(Vector2.Dot(bisector, cwP.Normalized));
                bisector *= (0.5f*width)/MathF.Abs(dot);
            } else {
                if(dir.x*prevDir.y - prevDir.x*dir.y > 0) {
                    bisector = cwP;
                } else {
                    bisector = ccwP;
                }
            }
            float z = line[0].z;
            Vector3 sharedVertex = new Vector3(start + bisector, z);
            
            if(dir.x*prevDir.y - prevDir.x*dir.y > 0) {
                if(i != 0) 
                    vertices[(int)prevS1Idx+2] = sharedVertex;
                vertices.Add(sharedVertex);
                edgeCoords.Add(new Vector2(0.0f, 1.0f));
                vcolors.Add(colors[i]);
                vertices.Add(new Vector3(s2, z));
                edgeCoords.Add(new Vector2(0.0f, 1.0f));
                vcolors.Add(colors[i]);
            } else {
                if(i != 0)
                    vertices[(int)prevS1Idx+3] = sharedVertex;
                vertices.Add(new Vector3(s1, z));
                edgeCoords.Add(new Vector2(0.0f, 1.0f));
                vcolors.Add(colors[i]);
                vertices.Add(sharedVertex);
                edgeCoords.Add(new Vector2(0.0f, 1.0f));
                vcolors.Add(colors[i]);
            }

            vertices.Add(new Vector3(e1, z));
            edgeCoords.Add(new Vector2(0.0f, 1.0f));
            vcolors.Add(colors[i+1]);
            vertices.Add(new Vector3(e2, z));
            edgeCoords.Add(new Vector2(0.0f, 1.0f));
            vcolors.Add(colors[i+1]);
            vertices.Add(new Vector3(end, z));
            edgeCoords.Add(new Vector2(0.0f, 0.0f));
            vcolors.Add(colors[i+1]);

            indices.Add((uint)(s1Idx+0)); indices.Add(prevEnd); indices.Add((uint)(s1Idx+2));
            indices.Add((uint)(s1Idx+1)); indices.Add((uint)(s1Idx+3)); indices.Add((uint)(s1Idx+4));
            indices.Add(prevEnd); indices.Add((uint)(s1Idx+4)); indices.Add((uint)(s1Idx+2));
            indices.Add(prevEnd); indices.Add((uint)(s1Idx+1)); indices.Add((uint)(s1Idx+4));

            // --prevdir--O
            //             \
            //              dir
            //               \
            if(i > 0) {
                // when line segments are at different angle, there will be a gap
                // we fill that gap with a circle sector
                // the sector shape is inverse of the union of the 2 line segment rectangles
                prevCWP = prevCWP.Normalized;
                cwP = cwP.Normalized;
                Vector2 startD = prevCWP;
                Vector2 endD = cwP;
                uint startIdx = (uint)(prevS1Idx+2);
                uint endIdx = (uint)(s1Idx);
                if(startD.x*endD.y - endD.x*startD.y < 0) {
                    startD *= -1.0f;
                    endD *= -1.0f;
                    startIdx++;
                    endIdx++;
                }
                bool flip = false;
                float angle = MathF.Acos(Vector2.Dot(startD.Normalized, endD.Normalized));
                if(startD.x*endD.y - endD.x*startD.y < 0) {
                    angle *= -1.0f;
                    flip = true;
                }

                Vector2 prevP = startD;
                for(int k = 1; k <= 10; k++) {
                    uint idx = (uint)vertices.Count;
                    float t = (float)k * 0.1f;
                    float cangle = t * angle;
                    Vector2 P = startD.Rotated(cangle);
                    if(k == 10) {
                        if(!flip) {
                            indices.Add(prevEnd); indices.Add(endIdx); indices.Add((uint)(idx-1));
                        } else {
                            indices.Add(prevEnd); indices.Add((uint)(idx-1)); indices.Add(endIdx);
                        }
                    } else if(k == 1) {
                        vertices.Add(new Vector3((start + P*width*0.5f), z));
                        edgeCoords.Add(new Vector2(0.0f, 1.0f));
                        vcolors.Add(colors[i]);
                        if(!flip) {
                            indices.Add(prevEnd); indices.Add((uint)(idx)); indices.Add(startIdx);
                        } else {
                            indices.Add(prevEnd); indices.Add(startIdx); indices.Add((uint)(idx));
                        }
                    } else {
                        vertices.Add(new Vector3((start + P*width*0.5f), z));
                        edgeCoords.Add(new Vector2(0.0f, 1.0f));
                        vcolors.Add(colors[i]);
                        if(!flip) {
                            indices.Add(prevEnd); indices.Add((uint)(idx)); indices.Add((uint)(idx-1));
                        } else {
                            indices.Add(prevEnd); indices.Add((uint)(idx-1)); indices.Add((uint)(idx));
                        }
                    }
                }
            }
            prevDir = dir;
            prevCWP = cwP;
            prevS1Idx = s1Idx;
            prevEnd = (uint)(s1Idx + 4);
        }
        System.Diagnostics.Debug.Assert(vertices.Count == edgeCoords.Count);
        System.Diagnostics.Debug.Assert(vertices.Count == vcolors.Count);
        return new ColoredTriangleMeshGeometry(guid) {
            vertices = vertices.ToArray(),
            indices = indices.ToArray(),
            colors = vcolors.ToArray(),
            edgeCoordinates = edgeCoords.ToArray(),
        };
    }
}
