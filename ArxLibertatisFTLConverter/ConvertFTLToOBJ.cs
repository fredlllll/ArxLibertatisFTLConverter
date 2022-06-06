using ArxLibertatisEditorIO.RawIO.FTL;
using ArxLibertatisEditorIO.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;

namespace ArxLibertatisFTLConverter
{
    public static class ConvertFTLToOBJ
    {
        private class Material
        {
            public string name;
            public string textureFile;
        }
        private class Mesh
        {
            private Dictionary<int, List<Face>> faceData = new Dictionary<int, List<Face>>();
            private List<Face> allFaces = new List<Face>();

            public IReadOnlyList<Face> GetFaceList(int materialIndex)
            {
                if (!faceData.TryGetValue(materialIndex, out List<Face> faces))
                {
                    faces = new List<Face>();
                    faceData[materialIndex] = faces;
                }
                return faces;
            }

            public void AddFace(int materialIndex, Face face)
            {
                if (!faceData.TryGetValue(materialIndex, out List<Face> faces))
                {
                    faces = new List<Face>();
                    faceData[materialIndex] = faces;
                }
                faces.Add(face);
                allFaces.Add(face);
            }

            public IReadOnlyList<Face> GetFaces()
            {
                return allFaces;
            }
        }

        private class Face
        {
            public int[] indices = new int[3];
            public Vector3[] vertices = new Vector3[3];
            public Vector3[] normals = new Vector3[3];
            public Vector2[] uvs = new Vector2[3];
        }
        public static void Convert(string file)
        {
            string parentDir = Path.GetDirectoryName(file);
            string fileName = Path.GetFileNameWithoutExtension(file);
            string outputDir = Path.Combine(parentDir, fileName + "_output");
            Directory.CreateDirectory(outputDir);
            string outputName = Path.Combine(outputDir, fileName + ".obj");
            string outputNameMTL = Path.Combine(outputDir, fileName + ".mtl");
            string gameDir = Util.GetParentWithName(parentDir, "game");
            string dataDir = null;
            if (gameDir != null)
            {
                dataDir = Path.GetDirectoryName(gameDir);
            }
            else
            {
                Console.WriteLine("could not find game dir");
            }


            FTL_IO ftl = new FTL_IO();

            using (var fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                var s = FTL_IO.EnsureUnpacked(fs);
                ftl.ReadFrom(s);
            }

            Vector3[] baseVerts = new Vector3[ftl._3DDataSection.vertexList.Length];
            Vector3[] baseNorms = new Vector3[baseVerts.Length];
            Material[] materials = new Material[ftl._3DDataSection.textureContainers.Length];

            //load base vertex data
            for (int i = 0; i < baseVerts.Length; ++i)
            {
                var vert = ftl._3DDataSection.vertexList[i];
                baseVerts[i] = new Vector3(vert.vert.x, -vert.vert.y, vert.vert.z);
                baseNorms[i] = new Vector3(vert.norm.x, -vert.norm.y, vert.norm.z);
            }

            //load materials
            for (int i = 0; i < materials.Length; ++i)
            {
                var texConName = IOHelper.GetString(ftl._3DDataSection.textureContainers[i].name);
                if (dataDir != null)
                {
                    string tmpName = Path.Combine(dataDir, Path.GetDirectoryName(texConName), Path.GetFileNameWithoutExtension(texConName));
                    if (File.Exists(tmpName + ".jpg"))
                    {
                        texConName = tmpName + ".jpg";
                    }
                    else if (File.Exists(tmpName + ".bmp"))
                    {
                        texConName = tmpName + ".bmp";
                    }

                    if (File.Exists(texConName))
                    {
                        File.Copy(texConName, Path.Combine(outputDir, Path.GetFileName(texConName)), true);
                    }
                    else
                    {
                        File.WriteAllText(Path.Combine(outputDir, Path.GetFileName(texConName)), "could not find texture");
                    }
                }

                Material mat = new Material { name = Path.GetFileNameWithoutExtension(texConName), textureFile = texConName };
                materials[i] = mat;
            }

            Mesh mesh = new Mesh();
            //load faces
            for (int j = 0; j < ftl._3DDataSection.faceList.Length; ++j)
            {
                var face = ftl._3DDataSection.faceList[j];
                var f = new Face();
                for (int i = 0; i < 3; ++i)
                {
                    ushort baseVertIndex = face.vid[i];

                    f.vertices[i] = baseVerts[baseVertIndex];
                    f.normals[i] = baseVerts[baseVertIndex];
                    f.uvs[i] = new Vector2(face.u[i], 1 - face.v[i]);
                }
                mesh.AddFace(face.texid, f);
            }

            //write out obj
            IFormatProvider format = System.Globalization.CultureInfo.InvariantCulture;
            string floatFormat = "0.00000";
            using (var objStream = new FileStream(outputName, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
            using (StreamWriter sw = new StreamWriter(objStream))
            {
                var faceList = mesh.GetFaces();
                sw.WriteLine("mtllib " + Path.GetFileName(outputNameMTL));

                sw.WriteLine("o " + Path.GetFileNameWithoutExtension(file));
                //write vertices
                sw.WriteLine("# vertices");
                for (int i = 0, vertIndex = 0; i < faceList.Count; ++i)
                {
                    for (int j = 0; j < 3; ++j)
                    {
                        var f = faceList[i];
                        var v = f.vertices[j];
                        sw.Write("v ");
                        sw.Write(v.X.ToString(floatFormat, format) + " ");
                        sw.Write(v.Y.ToString(floatFormat, format) + " ");
                        sw.WriteLine(v.Z.ToString(floatFormat, format));

                        //set index
                        f.indices[j] = vertIndex++;
                    }
                }

                //write texture coordinates
                sw.WriteLine("# texture coordinates");
                for (int i = 0; i < faceList.Count; ++i)
                {
                    for (int j = 0; j < 3; ++j)
                    {
                        var v = faceList[i].uvs[j];
                        sw.Write("vt ");
                        sw.Write(v.X.ToString(floatFormat, format) + " ");
                        sw.WriteLine(v.Y.ToString(floatFormat, format));
                    }
                }

                //write normals
                sw.WriteLine("# normals");
                for (int i = 0; i < faceList.Count; ++i)
                {
                    for (int j = 0; j < 3; ++j)
                    {
                        var v = faceList[i].vertices[j];
                        sw.Write("vn ");
                        sw.Write(v.X.ToString(floatFormat, format) + " ");
                        sw.Write(v.Y.ToString(floatFormat, format) + " ");
                        sw.WriteLine(v.Z.ToString(floatFormat, format));
                    }
                }

                //write face data
                for (int i = 0; i < materials.Length; ++i)
                {
                    var mat = materials[i];
                    var matFaceList = mesh.GetFaceList(i);
                    sw.WriteLine("usemtl " + mat.name);
                    for (int j = 0; j < matFaceList.Count; ++j)
                    {
                        var f = matFaceList[j];
                        sw.Write("f");
                        int i1 = f.indices[0] + 1;
                        int i2 = f.indices[1] + 1;
                        int i3 = f.indices[2] + 1;
                        sw.WriteLine($" {i1}/{i1}/{i1} {i3}/{i3}/{i3} {i2}/{i2}/{i2}");
                    }
                }
            }

            //write out mtl
            using (var mtlStream = new FileStream(outputNameMTL, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
            using (StreamWriter sw = new StreamWriter(mtlStream))
            {
                for (int i = 0; i < materials.Length; ++i)
                {
                    var mat = materials[i];
                    sw.WriteLine("newmtl " + mat.name);
                    sw.WriteLine("Ka 0 0 0");
                    sw.WriteLine("Kd 1 1 1");
                    sw.WriteLine("Ks 0 0 0");
                    sw.WriteLine("Ns 0");
                    sw.WriteLine("d 1");
                    sw.WriteLine("Tr 0");
                    sw.WriteLine("illum 2");
                    sw.WriteLine("map_Kd " + Path.GetFileName(mat.textureFile));
                }
            }
        }
    }
}
