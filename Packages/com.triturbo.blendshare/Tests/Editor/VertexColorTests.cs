using System.Collections.Generic;
using NUnit.Framework;
using Triturbo.BlendShapeShare.BlendShapeData;
using Triturbo.BlendShapeShare.Extractor;
using UnityEngine;

namespace Triturbo.BlendShapeShare.Tests
{
    public class VertexColorTests
    {
        private const int VertexCount = 4;

        private Mesh _sourceMesh;
        private MeshData _meshData;
        private BlendShapesExtractorOptions _options;

        [SetUp]
        public void SetUp()
        {
            _sourceMesh = MeshFactory.CreateBaseMesh(VertexCount);
            _sourceMesh = MeshFactory.CreateMeshWithColors(_sourceMesh);
            _sourceMesh = MeshFactory.CreateMeshWithBlendShapes(_sourceMesh, new[] { "TestShape" });

            _meshData = new MeshData(_sourceMesh, new List<string> { "TestShape" });
            _options = new BlendShapesExtractorOptions
            {
                includeWeights = false,
                includeColors = true
            };
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_sourceMesh);
        }

        [Test]
        public void ExtractColors_StoresColorArray()
        {
            _meshData.ExtractUnityBlendShapes(_sourceMesh, null, _options);

            Assert.IsTrue(_meshData.HasVertexColors);
            Assert.AreEqual(VertexCount, _meshData.m_Colors.Length);

            // First vertex: t=0 → (0, 0.5, 1, 1)
            Assert.AreEqual(0f, _meshData.m_Colors[0].r, 0.01f);
            Assert.AreEqual(0.5f, _meshData.m_Colors[0].g, 0.01f);
            Assert.AreEqual(1f, _meshData.m_Colors[0].b, 0.01f);
        }

        [Test]
        public void ExtractColors_FallsBackToColors32()
        {
            // Create a mesh with Color32 instead of Color
            var mesh32 = MeshFactory.CreateBaseMesh(VertexCount);
            mesh32 = MeshFactory.CreateMeshWithBlendShapes(mesh32, new[] { "TestShape" });
            var colors32 = new Color32[VertexCount];
            for (int i = 0; i < VertexCount; i++)
            {
                colors32[i] = new Color32(255, 128, 0, 255);
            }
            mesh32.colors32 = colors32;

            var meshData32 = new MeshData(mesh32, new List<string> { "TestShape" });
            meshData32.ExtractUnityBlendShapes(mesh32, null, _options);

            Assert.IsTrue(meshData32.HasVertexColors);
            Assert.AreEqual(VertexCount, meshData32.m_Colors.Length);

            // Color32(255,128,0,255) → approximately Color(1, 0.502, 0, 1)
            Assert.AreEqual(1f, meshData32.m_Colors[0].r, 0.01f);
            Assert.AreEqual(0.5f, meshData32.m_Colors[0].g, 0.02f);
            Assert.AreEqual(0f, meshData32.m_Colors[0].b, 0.01f);

            Object.DestroyImmediate(mesh32);
        }

        [Test]
        public void ExtractColors_SkippedWhenDisabled()
        {
            _options.includeColors = false;
            _meshData.ExtractUnityBlendShapes(_sourceMesh, null, _options);

            Assert.IsFalse(_meshData.HasVertexColors);
        }

        [Test]
        public void ApplyColors_SetsCorrectColors()
        {
            _meshData.ExtractUnityBlendShapes(_sourceMesh, null, _options);

            var targetMesh = MeshFactory.CreateBaseMesh(VertexCount);
            BlendShapeAppender.ApplySkinningAndColors(_meshData, targetMesh);

            Color[] appliedColors = targetMesh.colors;
            Assert.AreEqual(VertexCount, appliedColors.Length);

            for (int i = 0; i < VertexCount; i++)
            {
                Assert.AreEqual(_meshData.m_Colors[i].r, appliedColors[i].r, 0.001f, $"Color.r mismatch at vertex {i}");
                Assert.AreEqual(_meshData.m_Colors[i].g, appliedColors[i].g, 0.001f, $"Color.g mismatch at vertex {i}");
                Assert.AreEqual(_meshData.m_Colors[i].b, appliedColors[i].b, 0.001f, $"Color.b mismatch at vertex {i}");
                Assert.AreEqual(_meshData.m_Colors[i].a, appliedColors[i].a, 0.001f, $"Color.a mismatch at vertex {i}");
            }

            Object.DestroyImmediate(targetMesh);
        }

        [Test]
        public void ApplyColors_SkipsOnVertexCountMismatch()
        {
            _meshData.ExtractUnityBlendShapes(_sourceMesh, null, _options);

            var targetMesh = MeshFactory.CreateBaseMesh(VertexCount + 5);
            BlendShapeAppender.ApplySkinningAndColors(_meshData, targetMesh);

            // Colors should NOT have been applied
            Assert.AreEqual(0, targetMesh.colors.Length);

            Object.DestroyImmediate(targetMesh);
        }

        [Test]
        public void ApplyColors_RoundTrip_PreservesAllData()
        {
            _meshData.ExtractUnityBlendShapes(_sourceMesh, null, _options);

            var targetMesh = MeshFactory.CreateBaseMesh(VertexCount);
            BlendShapeAppender.ApplySkinningAndColors(_meshData, targetMesh);

            // Extract colors from the target mesh
            var roundTripData = new MeshData(targetMesh, new List<string>());
            var roundTripOptions = new BlendShapesExtractorOptions { includeWeights = false, includeColors = true };
            roundTripData.ExtractUnityBlendShapes(targetMesh, null, roundTripOptions);

            Assert.IsTrue(roundTripData.HasVertexColors);
            Assert.AreEqual(_meshData.m_Colors.Length, roundTripData.m_Colors.Length);

            for (int i = 0; i < _meshData.m_Colors.Length; i++)
            {
                Assert.AreEqual(_meshData.m_Colors[i].r, roundTripData.m_Colors[i].r, 0.001f);
                Assert.AreEqual(_meshData.m_Colors[i].g, roundTripData.m_Colors[i].g, 0.001f);
                Assert.AreEqual(_meshData.m_Colors[i].b, roundTripData.m_Colors[i].b, 0.001f);
                Assert.AreEqual(_meshData.m_Colors[i].a, roundTripData.m_Colors[i].a, 0.001f);
            }

            Object.DestroyImmediate(targetMesh);
        }
    }
}
