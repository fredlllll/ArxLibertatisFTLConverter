using CSWavefront.Raw;
using CSWavefront.Util;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;

namespace ArxLibertatisFTLConverter.Writers
{
    public class ObjWriter : IModelWriter
    {
        private class TmpMaterial
        {
            public string name;
            public string textureFile;
        }

        public string FilePath { get; set; }

        public void Write(IntermediateModel intermediateModel)
        {
            string outputDir = Path.GetDirectoryName(FilePath);
            Directory.CreateDirectory(outputDir);
            string fileName = Path.GetFileNameWithoutExtension(FilePath);
            string outputPath = Path.Combine(outputDir, fileName + ".obj");
            string outputPathMTL = Path.Combine(outputDir, fileName + ".mtl");

            var ftl = intermediateModel.Ftl;
            var dataDir = intermediateModel.DataDir;

            Vector4[] baseVerts = new Vector4[ftl.dataSection3D.vertexList.Count];
            Vector3[] baseNorms = new Vector3[baseVerts.Length];
            TmpMaterial[] materials = new TmpMaterial[ftl.dataSection3D.textures.Count];

            //load base vertex data
            for (int i = 0; i < baseVerts.Length; ++i)
            {
                var vert = ftl.dataSection3D.vertexList[i];
                baseVerts[i] = new Vector4(vert.vertex.X, -vert.vertex.Y, vert.vertex.Z, 1);
                baseNorms[i] = new Vector3(vert.normal.X, -vert.normal.Y, vert.normal.Z);
            }

            //load materials
            for (int i = 0; i < materials.Length; ++i)
            {
                string texture = ftl.dataSection3D.textures[i];
                if (dataDir != null)
                {
                    var actualTexturePath = Util.GetActualTexturePath(dataDir, texture);
                    if (actualTexturePath != null)
                    {
                        var relativeOutputPath = Path.GetRelativePath(dataDir, actualTexturePath);
                        texture = relativeOutputPath;
                        var relativeOutputDir = Path.GetDirectoryName(relativeOutputPath);
                        if (!string.IsNullOrEmpty(relativeOutputDir))
                        {
                            Directory.CreateDirectory(Path.Combine(outputDir, relativeOutputDir));
                        }
                        File.Copy(actualTexturePath, Path.Combine(outputDir, relativeOutputPath), true);
                    }
                }
                materials[i] = new TmpMaterial { name = Path.GetFileNameWithoutExtension(texture), textureFile = texture };
            }

            AutoDictionary<int, HashSet<string>> indexToGroup = new((x) => { return new HashSet<string>(); });

            //groups
            for (int i = 0; i < ftl.dataSection3D.groups.Count; ++i)
            {
                var g = ftl.dataSection3D.groups[i];
                var name = g.name.Replace(' ', '_');
                for (int j = 0; j < g.indices.Length; j++)
                {
                    indexToGroup[g.indices[j]].Add(name);
                }
            }

            ObjFile obj = new();
            obj.vertices.AddRange(baseVerts);
            obj.normals.AddRange(baseNorms);

            for (int i = 0; i < ftl.dataSection3D.faceList.Count; ++i)
            {
                var face = ftl.dataSection3D.faceList[i];
                HashSet<string> groups = new();
                string materialName = "noMaterial";
                if (face.textureContainerIndex >= 0)
                {
                    materialName = materials[face.textureContainerIndex].name;
                }
                ObjObject obje = obj.objects[materialName];

                Polygon p = new()
                {
                    hasNormals = true,
                    hasUvs = true
                };


                for (int j = 0; j < 3; ++j)
                {
                    var vert = face.vertices[j];
                    ushort baseVertIndex = vert.vertexIndex;
                    groups.UnionWith(indexToGroup[baseVertIndex]);

                    PolygonVertex pv = new()
                    {
                        vertex = baseVertIndex,
                        normal = baseVertIndex,
                        uv = obj.uvs.Count
                    };

                    obj.uvs.Add(new Vector3(vert.u, 1 - vert.v, 1)); //invert v coordinate
                    p.vertices.Add(pv);
                }

                obje.groupNames.UnionWith(groups);
                obje.polygons[materialName].Add(p);
            }

            //write out obj
            ObjSaver.Save(obj, outputPath);

            MtlFile mtl = new();
            for (int i = 0; i < materials.Length; ++i)
            {
                var myMat = materials[i];
                var mat = mtl.materials[myMat.name];
                mat.ambientColor = Vector3.Zero;
                mat.diffuseColor = Vector3.One;
                mat.specularColor = Vector3.Zero;
                mat.specularFactor = 0;
                mat.transparency = 0;
                mat.illuminationModel = IlluminationModel.HighlightOn;
                mat.diffuseMap = myMat.textureFile;
            }
            //write out mtl
            MtlSaver.Save(mtl, outputPathMTL);
        }
    }
}
