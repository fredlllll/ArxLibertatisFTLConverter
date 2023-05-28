using ArxLibertatisEditorIO.MediumIO.FTL;
using ArxLibertatisEditorIO.RawIO.FTL;
using ArxLibertatisEditorIO.Util;
using CSWavefront.Raw;
using CSWavefront.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Numerics;
using System.Text;

namespace ArxLibertatisFTLConverter
{
    public static class ConvertOBJToFTL
    {
        class EqualsComparer<T> : IEqualityComparer<T>
        {
            public bool Equals([AllowNull] T x, [AllowNull] T y)
            {
                if (x == null && y == null)
                {
                    return true;
                }
                else if (x == null || y == null)
                {
                    return false;
                }

                return x.Equals(y);
            }

            public int GetHashCode([DisallowNull] T obj)
            {
                return obj.GetHashCode();
            }
        }

        static Vertex FromPolyVertex(ObjFile obj, PolygonVertex polyVertex)
        {
            Vertex v = new Vertex();
            Vector4 pos = obj.vertices[polyVertex.vertex];
            Vector3 norm = obj.normals[polyVertex.normal];
            v.vertex = new Vector3(pos.X, -pos.Y, pos.Z);
            v.normal = new Vector3(norm.X, -norm.Y, norm.Z);
            return v;
        }

        public static void Convert(string file)
        {
            ObjFile obj = ObjLoader.Load(file);
            var mtlFile = Path.Join(Path.GetDirectoryName(file), Path.GetFileNameWithoutExtension(file) + ".mtl");
            var ftlFile = Path.Join(Path.GetDirectoryName(file), Path.GetFileNameWithoutExtension(file) + ".ftl");
            MtlFile mtl = new MtlFile();
            if (File.Exists(mtlFile))
            {
                mtl = MtlLoader.Load(mtlFile);
            }

            Ftl ftl = new Ftl();
            ftl.dataSection3D = new DataSection3D();
            ftl.dataSection3D.header.name = Path.GetFileNameWithoutExtension(file);

            //materials
            List<Material> materials = new List<Material>();
            AutoDictionary<string, int> materialNameToIndex = new AutoDictionary<string, int>((x) => { var mat = new Material(x) { diffuseMap = x }; materials.Add(mat); return materials.Count - 1; });
            foreach (var kv in mtl.materials)
            {
                materials.Add(kv.Value);
                materialNameToIndex[kv.Key] = materialNameToIndex.Count;
            }

            //create set of all vertices
            HashSet<Vertex> allVertices = new HashSet<Vertex>(new EqualsComparer<Vertex>());
            foreach (var kv in obj.objects)
            {
                var name = kv.Key;
                var o = kv.Value;

                foreach (var kv2 in o.polygons)
                {
                    var polygons = kv2.Value;
                    for (int i = 0; i < polygons.Count; ++i)
                    {
                        var polygon = polygons[i];
                        for (int j = 0; j < 3; ++j) //only do triangles
                        {
                            var objVert = polygon.vertices[j];

                            Vertex v = FromPolyVertex(obj, objVert);

                            allVertices.Add(v);
                        }
                    }
                }
            }
            //create list of vertices and index lookup
            Dictionary<Vertex, int> vertexToIndex = new Dictionary<Vertex, int>(new EqualsComparer<Vertex>());
            foreach (var v in allVertices)
            {
                vertexToIndex[v] = ftl.dataSection3D.vertexList.Count;
                ftl.dataSection3D.vertexList.Add(v);
            }

            //create face list
            foreach (var kv in obj.objects)
            {
                var name = kv.Key;
                var o = kv.Value;

                foreach (var kv2 in o.polygons)
                {
                    var matName = kv2.Key;
                    var matIndex = materialNameToIndex[matName];
                    var polygons = kv2.Value;

                    for (int i = 0; i < polygons.Count; ++i)
                    {
                        var polygon = polygons[i];
                        var face = new Face();
                        face.textureContainerIndex = (short)matIndex;

                        var faceNormal = Vector3.Zero;

                        for (int j = 0; j < 3; ++j) //only do triangles
                        {
                            var objVert = polygon.vertices[j];
                            Vertex v = FromPolyVertex(obj, objVert);

                            faceNormal += v.normal;

                            int vertexIndex = vertexToIndex[v];

                            var ftlVert = face.vertices[j];
                            ftlVert.color = new Color(1, 1, 1);
                            ftlVert.normal = obj.normals[objVert.normal];
                            ftlVert.u = obj.uvs[objVert.uv].X;
                            ftlVert.v = 1 - obj.uvs[objVert.uv].Y;
                            ftlVert.ou = (short)(255 * ftlVert.u);
                            ftlVert.ov = (short)(255 * ftlVert.v);
                            ftlVert.vertexIndex = (ushort)vertexIndex;
                        }
                        var tmp = face.vertices[1];
                        face.vertices[1] = face.vertices[2];
                        face.vertices[2] = tmp;
                        face.normal = faceNormal / 3;
                        ftl.dataSection3D.faceList.Add(face);
                    }
                }
            }

            //write materials here cause it couldve been changed by above code
            for (int i = 0; i < materials.Count; ++i)
            {
                ftl.dataSection3D.textures.Add(materials[i].diffuseMap);
            }

            FTL_IO rawFtl = new FTL_IO();
            ftl.WriteTo(rawFtl);
            using (var ms = new MemoryStream())
            {
                rawFtl.WriteTo(ms);
                var packed = FTL_IO.EnsurePacked(ms);
                using (var fs = new FileStream(ftlFile, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
                {
                    packed.CopyTo(fs);
                }
                packed.Dispose();
            }
        }
    }
}
