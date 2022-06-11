using ArxLibertatisEditorIO.RawIO.FTL;
using ArxLibertatisEditorIO.Util;
using CSWavefront.Raw;
using CSWavefront.Util;
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

        public static void Convert(string file)
        {
            string parentDir = Path.GetDirectoryName(file);
            string fileName = Path.GetFileNameWithoutExtension(file);
            string outputDir = Path.Combine(parentDir, fileName + "_FTLToOBJ");
            Directory.CreateDirectory(outputDir);
            string outputName = Path.Combine(outputDir, fileName + ".obj");
            string outputNameMTL = Path.Combine(outputDir, fileName + ".mtl");
            string gameDir = Util.GetParentWithName(parentDir, "game");
            if (gameDir != null)
            {
                Settings.dataDir = Path.GetDirectoryName(gameDir);
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

            Vector4[] baseVerts = new Vector4[ftl._3DDataSection.vertexList.Length];
            Vector3[] baseNorms = new Vector3[baseVerts.Length];
            Material[] materials = new Material[ftl._3DDataSection.textureContainers.Length];

            //load base vertex data
            for (int i = 0; i < baseVerts.Length; ++i)
            {
                var vert = ftl._3DDataSection.vertexList[i];
                baseVerts[i] = new Vector4(vert.vert.x, -vert.vert.y, vert.vert.z, 1);
                baseNorms[i] = new Vector3(vert.norm.x, -vert.norm.y, vert.norm.z);
            }

            //load materials
            for (int i = 0; i < materials.Length; ++i)
            {
                var texConName = IOHelper.GetString(ftl._3DDataSection.textureContainers[i].name);
                if (Settings.dataDir != null)
                {
                    string tmpName = Path.Combine(Settings.dataDir, Path.GetDirectoryName(texConName), Path.GetFileNameWithoutExtension(texConName));
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

            AutoDictionary<int, HashSet<string>> indexToGroup = new AutoDictionary<int, HashSet<string>>((x) => { return new HashSet<string>(); });

            //groups
            for (int i = 0; i < ftl._3DDataSection.groups.Length; ++i)
            {
                var g = ftl._3DDataSection.groups[i];
                var name = IOHelper.GetStringSafe(g.group.name).Replace(' ', '_');
                for (int j = 0; j < g.indices.Length; j++)
                {
                    indexToGroup[g.indices[j]].Add(name);
                }
            }

            ObjFile obj = new ObjFile();
            obj.vertices.AddRange(baseVerts);
            obj.normals.AddRange(baseNorms);

            for (int i = 0; i < ftl._3DDataSection.faceList.Length; ++i)
            {
                var face = ftl._3DDataSection.faceList[i];
                HashSet<string> groups = new HashSet<string>();
                string materialName = "noMaterial";
                if (face.texid >= 0)
                {
                    materialName = materials[face.texid].name;
                }
                ObjObject obje = obj.objects[materialName];

                Polygon p = new Polygon
                {
                    hasNormals = true,
                    hasUvs = true
                };
                for (int j = 0; j < 3; ++j)
                {
                    ushort baseVertIndex = face.vid[j];
                    groups.UnionWith(indexToGroup[baseVertIndex]);

                    PolygonVertex pv = new PolygonVertex
                    {
                        vertex = baseVertIndex,
                        normal = baseVertIndex,
                        uv = obj.uvs.Count
                    };

                    obj.uvs.Add(new Vector3(face.u[j], 1 - face.v[j], 1));
                    p.vertices.Add(pv);
                }

                obje.groupNames.UnionWith(groups);
                obje.polygons[materialName].Add(p);
            }

            //write out obj
            ObjSaver.Save(obj, outputName);

            MtlFile mtl = new MtlFile();
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
                mat.diffuseMap = Path.GetFileName(myMat.textureFile);
            }
            //write out mtl
            MtlSaver.Save(mtl, outputNameMTL);
        }
    }
}
