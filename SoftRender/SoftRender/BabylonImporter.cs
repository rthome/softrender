using Newtonsoft.Json;
using SharpDX;
using SoftRender.Engine;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Storage;

namespace SoftRender
{
    class BabylonImporter
    {
        public async Task<Mesh[]> LoadFileAsync(string filename)
        {
            var meshes = new List<Mesh>();
            var file = await Package.Current.InstalledLocation.GetFileAsync(filename);
            var data = await FileIO.ReadTextAsync(file);
            dynamic jsonObject = JsonConvert.DeserializeObject(data);

            for (int meshIndex = 0; meshIndex < jsonObject.meshes.Count; meshIndex++)
            {
                var vertices = jsonObject.meshes[meshIndex].vertices;
                var indices = jsonObject.meshes[meshIndex].indices;
                var uvCount = jsonObject.meshes[meshIndex].uvCount.Value;

                var vertexStep = 1;
                switch ((int)uvCount)
                {
                    case 0:
                        vertexStep = 6;
                        break;
                    case 1:
                        vertexStep = 8;
                        break;
                    case 2:
                        vertexStep = 10;
                        break;
                }

                var vertexCount = vertices.Count / vertexStep;
                var faceCount = indices.Count / 3;
                var mesh = new Mesh(jsonObject.meshes[meshIndex].name.Value, vertexCount, faceCount);
                for (int index = 0; index < vertexCount; index++)
                {
                    var x = (float)vertices[index * vertexStep].Value;
                    var y = (float)vertices[index * vertexStep + 1].Value;
                    var z = (float)vertices[index * vertexStep + 2].Value;
                    var nx = (float)vertices[index * vertexStep + 3].Value;
                    var ny = (float)vertices[index * vertexStep + 4].Value;
                    var nz = (float)vertices[index * vertexStep + 5].Value;
                    mesh.Vertices[index] = new Vertex
                    {
                        Coordinates = new Vector3(x, y, z),
                        Normal = Vector3.Normalize(new Vector3(nx, ny, nz)),
                    };
                }
                for (var index = 0; index < faceCount; index++)
                {
                    var v0 = (int)indices[index * 3].Value;
                    var v1 = (int)indices[index * 3 + 1].Value;
                    var v2 = (int)indices[index * 3 + 2].Value;
                    mesh.Faces[index] = new Face(v0, v1, v2 );
                }

                var position = jsonObject.meshes[meshIndex].position;
                mesh.Position = new Vector3((float)position[0].Value, (float)position[1].Value, (float)position[2].Value);
                meshes.Add(mesh);
            }

            return meshes.ToArray();
        }
    }
}
