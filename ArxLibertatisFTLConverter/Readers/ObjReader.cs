using ArxLibertatisEditorIO.MediumIO.FTL;
using ArxLibertatisEditorIO.Util;
using CSWavefront.Raw;
using CSWavefront.Util;
using System.Collections.Generic;
using System.IO;
using System.Numerics;

namespace ArxLibertatisFTLConverter.Readers
{
    public class ObjReader : IModelReader
    {
        public string FilePath { get; set; }

        static Vertex FromPolyVertex(ObjFile obj, PolygonVertex polyVertex)
        {
            Vertex v = new();
            Vector4 pos = obj.vertices[polyVertex.vertex];
            Vector3 norm = obj.normals[polyVertex.normal];
            v.vertex = new Vector3(pos.X, -pos.Y, pos.Z);
            v.normal = new Vector3(norm.X, -norm.Y, norm.Z);
            return v;
        }

        public IntermediateModel Read()
        {
            ObjFile obj = ObjLoader.Load(FilePath);
            string fileName = Path.GetFileNameWithoutExtension(FilePath);
            string dataDir = Path.GetDirectoryName(FilePath);
            var mtlFile = Path.Join(dataDir, fileName + ".mtl");
            MtlFile mtl = new();
            if (File.Exists(mtlFile))
            {
                mtl = MtlLoader.Load(mtlFile);
            }

            Ftl ftl = new();
            ftl.dataSection3D = new DataSection3D();
            ftl.dataSection3D.header.name = fileName;

            //materials
            List<Material> materials = new();
            AutoDictionary<string, int> materialNameToIndex = new((x) => { var mat = new Material(x) { diffuseMap = x }; materials.Add(mat); return materials.Count - 1; });
            foreach (var kv in mtl.materials)
            {
                materials.Add(kv.Value);
                materialNameToIndex[kv.Key] = materialNameToIndex.Count;
            }

            EqualsAutoDictionary<Vertex, HashSet<string>> vertexToGroups = new(x => new HashSet<string>());

            //create set of all vertices
            HashSet<Vertex> allVertices = new(new EqualsComparer<Vertex>());
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
                            vertexToGroups[v].UnionWith(o.groupNames);
                        }
                    }
                }
            }


            //create list of vertices and index lookup
            Dictionary<Vertex, int> vertexToIndex = new(new EqualsComparer<Vertex>());
            foreach (var v in allVertices)
            {
                vertexToIndex[v] = ftl.dataSection3D.vertexList.Count;
                ftl.dataSection3D.vertexList.Add(v);
            }

            //assign groups
            AutoDictionary<string, List<int>> groups = new((x) => new List<int>());
            foreach (var kv in vertexToIndex)
            {
                var v = kv.Key;
                var vertIndex = kv.Value;

                var group_names = vertexToGroups[v];
                foreach (var group_name in group_names)
                {
                    groups[group_name].Add(vertIndex);
                }
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
                        Face face = new();
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
                        //swap vertex order
                        //(face.vertices[2], face.vertices[1]) = (face.vertices[1], face.vertices[2]);
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

            //add groups to ftl
            foreach (var kv in groups)
            {
                var g = new ArxLibertatisEditorIO.MediumIO.FTL.Group
                {
                    name = kv.Key,
                    indexes = 0, //no idea
                    origin = 0, //TODO: calculate which vertex is most center?
                    blobShadowSize = 0, //calculate from size of mesh? only one per model?
                    indices = kv.Value.ToArray()
                };

                ftl.dataSection3D.groups.Add(g);
            }

            return new IntermediateModel()
            {
                Ftl = ftl,
                DataDir = dataDir,
            };
        }
    }
}
