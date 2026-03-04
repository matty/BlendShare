using System.Collections.Generic;
using NUnit.Framework;
using Triturbo.BlendShapeShare.BlendShapeData;
using Triturbo.BlendShapeShare.Extractor;
using UnityEngine;

namespace Triturbo.BlendShapeShare.Tests
{
    public class BlendShapeRoundTripTests
    {
        private const int VertexCount = 6;
        private const int BoneCount = 3;

        [Test]
        public void RoundTrip_BlendShapesPreserved()
        {
            var baseMesh = MeshFactory.CreateBaseMesh(VertexCount);
            var shapeNames = new[] { "Smile", "Blink" };
            var sourceMesh = MeshFactory.CreateMeshWithBlendShapes(baseMesh, shapeNames);

            // Extract
            var meshData = new MeshData(sourceMesh, new List<string>(shapeNames));
            var options = new BlendShapesExtractorOptions { includeWeights = false, includeColors = false };
            meshData.ExtractUnityBlendShapes(sourceMesh, null, options);

            // Apply to a copy of the base mesh
            var targetMesh = Object.Instantiate(baseMesh);
            var result = BlendShapeAppender.CreateBlendShapesMesh(meshData, targetMesh);

            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.blendShapeCount);
            Assert.AreEqual("Smile", result.GetBlendShapeName(0));
            Assert.AreEqual("Blink", result.GetBlendShapeName(1));

            // Verify delta values for first shape
            var deltaVertices = new Vector3[VertexCount];
            var deltaNormals = new Vector3[VertexCount];
            var deltaTangents = new Vector3[VertexCount];
            result.GetBlendShapeFrameVertices(0, 0, deltaVertices, deltaNormals, deltaTangents);

            // Shape 0 ("Smile") has scale = 0.01
            for (int v = 0; v < VertexCount; v++)
            {
                Assert.AreEqual(0.01f, deltaVertices[v].x, 0.0001f, $"Vertex {v} delta X mismatch");
                Assert.AreEqual(0.01f, deltaVertices[v].y, 0.0001f, $"Vertex {v} delta Y mismatch");
                Assert.AreEqual(0.01f, deltaVertices[v].z, 0.0001f, $"Vertex {v} delta Z mismatch");
            }

            Object.DestroyImmediate(baseMesh);
            Object.DestroyImmediate(sourceMesh);
            Object.DestroyImmediate(targetMesh);
            Object.DestroyImmediate(result);
        }

        [Test]
        public void RoundTrip_WeightsPreserved()
        {
            var baseMesh = MeshFactory.CreateBaseMesh(VertexCount);
            var sourceMesh = MeshFactory.CreateMeshWithBlendShapes(baseMesh, new[] { "Shape" });
            sourceMesh = MeshFactory.CreateMeshWithWeights(sourceMesh, BoneCount);

            var meshData = new MeshData(sourceMesh, new List<string> { "Shape" });
            var options = new BlendShapesExtractorOptions { includeWeights = true, includeColors = false };
            meshData.ExtractUnityBlendShapes(sourceMesh, null, options);

            // Apply
            var targetMesh = Object.Instantiate(baseMesh);
            var result = BlendShapeAppender.CreateBlendShapesMesh(meshData, targetMesh);

            Assert.IsNotNull(result);

            // Verify weights were applied
            var bonesPerVertex = result.GetBonesPerVertex();
            Assert.AreEqual(VertexCount, bonesPerVertex.Length);
            for (int i = 0; i < VertexCount; i++)
            {
                Assert.AreEqual(2, bonesPerVertex[i]);
            }

            var allWeights = result.GetAllBoneWeights();
            Assert.AreEqual(0.7f, allWeights[0].weight, 0.001f);
            Assert.AreEqual(0.3f, allWeights[1].weight, 0.001f);

            // Verify bind poses
            Assert.AreEqual(BoneCount, result.bindposes.Length);

            Object.DestroyImmediate(baseMesh);
            Object.DestroyImmediate(sourceMesh);
            Object.DestroyImmediate(targetMesh);
            Object.DestroyImmediate(result);
        }

        [Test]
        public void RoundTrip_ColorsPreserved()
        {
            var baseMesh = MeshFactory.CreateBaseMesh(VertexCount);
            var sourceMesh = MeshFactory.CreateMeshWithBlendShapes(baseMesh, new[] { "Shape" });
            sourceMesh = MeshFactory.CreateMeshWithColors(sourceMesh);

            var meshData = new MeshData(sourceMesh, new List<string> { "Shape" });
            var options = new BlendShapesExtractorOptions { includeWeights = false, includeColors = true };
            meshData.ExtractUnityBlendShapes(sourceMesh, null, options);

            var targetMesh = Object.Instantiate(baseMesh);
            var result = BlendShapeAppender.CreateBlendShapesMesh(meshData, targetMesh);

            Assert.IsNotNull(result);

            Color[] colors = result.colors;
            Assert.AreEqual(VertexCount, colors.Length);

            // Verify gradient pattern
            for (int i = 0; i < VertexCount; i++)
            {
                float t = (float)i / (VertexCount - 1);
                Assert.AreEqual(t, colors[i].r, 0.01f, $"Color.r mismatch at vertex {i}");
                Assert.AreEqual(0.5f, colors[i].g, 0.01f, $"Color.g mismatch at vertex {i}");
                Assert.AreEqual(1f - t, colors[i].b, 0.01f, $"Color.b mismatch at vertex {i}");
            }

            Object.DestroyImmediate(baseMesh);
            Object.DestroyImmediate(sourceMesh);
            Object.DestroyImmediate(targetMesh);
            Object.DestroyImmediate(result);
        }

        [Test]
        public void RoundTrip_AllDataPreserved()
        {
            var baseMesh = MeshFactory.CreateBaseMesh(VertexCount);
            var shapeNames = new[] { "ShapeA", "ShapeB" };
            var sourceMesh = MeshFactory.CreateMeshWithBlendShapes(baseMesh, shapeNames);
            sourceMesh = MeshFactory.CreateMeshWithWeights(sourceMesh, BoneCount);
            sourceMesh = MeshFactory.CreateMeshWithColors(sourceMesh);

            var meshData = new MeshData(sourceMesh, new List<string>(shapeNames));
            var options = new BlendShapesExtractorOptions { includeWeights = true, includeColors = true };
            meshData.ExtractUnityBlendShapes(sourceMesh, null, options);

            var targetMesh = Object.Instantiate(baseMesh);
            var result = BlendShapeAppender.CreateBlendShapesMesh(meshData, targetMesh);

            Assert.IsNotNull(result);

            // Blend shapes
            Assert.AreEqual(2, result.blendShapeCount);

            // Weights
            var bonesPerVertex = result.GetBonesPerVertex();
            Assert.AreEqual(VertexCount, bonesPerVertex.Length);

            // Colors
            Assert.AreEqual(VertexCount, result.colors.Length);

            Object.DestroyImmediate(baseMesh);
            Object.DestroyImmediate(sourceMesh);
            Object.DestroyImmediate(targetMesh);
            Object.DestroyImmediate(result);
        }

        [Test]
        public void FinalFilter_IncludesMeshesWithOnlyWeights()
        {
            // Simulates the final filter in ExtractBlendShapes that keeps meshes
            // with blend shapes OR skinning data OR vertex colors
            var mesh = MeshFactory.CreateBaseMesh(VertexCount);
            mesh = MeshFactory.CreateMeshWithWeights(mesh, BoneCount);

            var meshData = new MeshData(mesh, new List<string>());
            var options = new BlendShapesExtractorOptions { includeWeights = true, includeColors = false };
            meshData.ExtractUnityBlendShapes(mesh, null, options);

            // Simulate the final filter logic from ExtractBlendShapes
            bool included = meshData.BlendShapes.Count > 0 || meshData.HasSkinningData || meshData.HasVertexColors;
            Assert.IsTrue(included, "Mesh with only weights should be included in final filter");

            Object.DestroyImmediate(mesh);
        }

        [Test]
        public void FinalFilter_IncludesMeshesWithOnlyColors()
        {
            var mesh = MeshFactory.CreateBaseMesh(VertexCount);
            mesh = MeshFactory.CreateMeshWithColors(mesh);

            var meshData = new MeshData(mesh, new List<string>());
            var options = new BlendShapesExtractorOptions { includeWeights = false, includeColors = true };
            meshData.ExtractUnityBlendShapes(mesh, null, options);

            bool included = meshData.BlendShapes.Count > 0 || meshData.HasSkinningData || meshData.HasVertexColors;
            Assert.IsTrue(included, "Mesh with only colors should be included in final filter");

            Object.DestroyImmediate(mesh);
        }
    }
}
