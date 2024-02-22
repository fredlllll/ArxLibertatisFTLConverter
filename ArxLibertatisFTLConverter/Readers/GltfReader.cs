using System;
using System.IO;
using ArxLibertatisEditorIO.MediumIO.FTL;
using SharpGLTF.Schema2;

namespace ArxLibertatisFTLConverter.Readers
{
    public class GltfReader : IModelReader
    {
        public string FilePath { get; set; }

        public IntermediateModel Read()
        {
            var modelRoot = ModelRoot.Load(FilePath);

            string dataDir = Path.GetDirectoryName(FilePath);
            var ftl = new Ftl();
            var section3D = ftl.dataSection3D = new DataSection3D();

            var scene = modelRoot.DefaultScene;

            foreach (var visualChild in scene.VisualChildren)
            {
                var mesh = visualChild.Mesh;
                if (mesh == null)
                {
                    continue;
                }

                int textureId = section3D.textures.Count;
                section3D.textures.Add(mesh.Name);

                var primitive = mesh.Primitives[0]; //TODO: for now only support one primitive per mesh

                if (primitive.DrawPrimitiveType != PrimitiveType.TRIANGLES)
                {
                    throw new NotSupportedException($"only triangles supported for mesh type, {primitive.DrawPrimitiveType} found");
                }

                var vertexPositions = primitive.GetVertices(Util.GltfVertexAttributePosition);
                var vertexNormals = primitive.GetVertices(Util.GltfVertexAttributeNormal);
                var vertexUv = primitive.GetVertices(Util.GltfVertexAttributeTextureCoordinate0);

                var posArr = vertexPositions.AsVector3Array();
                var normArr = vertexNormals.AsVector3Array();
                var uvArr = vertexUv.AsVector2Array();

                for (int i = 0; i < posArr.Count; i++)
                {
                    var vert = new Vertex() { vertex = posArr[i], normal = normArr[i] }; //TODO: do we have to mirror coordinates here?
                    section3D.vertexList.Add(vert);
                }


                foreach ((int a, int b, int c) in primitive.GetTriangleIndices())
                {
                    var face = new Face()
                    {
                        textureContainerIndex = (short)textureId,
                    };

                    var n1 = normArr[a];
                    var n2 = normArr[b];
                    var n3 = normArr[c];
                    face.normal = (n1 + n2 + n3) / 3; //just average vertex normals, TODO: calculate from vertex positions instead?

                    var uv1 = uvArr[a];
                    var uv2 = uvArr[b];
                    var uv3 = uvArr[c];

                    face.vertices[0].vertexIndex = (ushort)a;
                    face.vertices[0].u = uv1.X;
                    face.vertices[0].v = uv1.Y; //TODO: do i have to invert y here too?
                    face.vertices[0].normal = n1;
                    face.vertices[0].color = new ArxLibertatisEditorIO.Util.Color(1, 1, 1); //TODO: also get color from mesh?
                    
                    face.vertices[1].vertexIndex = (ushort)b;
                    face.vertices[1].u = uv2.X;
                    face.vertices[1].v = uv2.Y;
                    face.vertices[1].normal = n2;
                    face.vertices[1].color = new ArxLibertatisEditorIO.Util.Color(1, 1, 1);

                    face.vertices[2].vertexIndex = (ushort)c;
                    face.vertices[2].u = uv3.X;
                    face.vertices[2].v = uv3.Y;
                    face.vertices[2].normal = n3;
                    face.vertices[2].color = new ArxLibertatisEditorIO.Util.Color(1, 1, 1);
                    section3D.faceList.Add(face);
                }
            }

            return new IntermediateModel()
            {
                DataDir = dataDir,
                Ftl = ftl,
            };
        }
    }
}
