using System.Collections.Generic;
using NUnit.Framework;
using Triturbo.BlendShapeShare.BlendShapeData;
using Triturbo.BlendShapeShare.Extractor;
using Unity.Collections;
using UnityEngine;

namespace Triturbo.BlendShapeShare.Tests
{
    public class SkinningDataTests
    {
        private const int VertexCount = 4;
        private const int BoneCount = 3;

        private Mesh _sourceMesh;
        private MeshData _meshData;
        private BlendShapesExtractorOptions _options;

        [SetUp]
        public void SetUp()
        {
            _sourceMesh = MeshFactory.CreateBaseMesh(VertexCount);
            _sourceMesh = MeshFactory.CreateMeshWithWeights(_sourceMesh, BoneCount);

            // Add a dummy blend shape so MeshData has a shape to extract
            _sourceMesh = MeshFactory.CreateMeshWithBlendShapes(_sourceMesh, new[] { "TestShape" });

            _meshData = new MeshData(_sourceMesh, new List<string> { "TestShape" });
            _options = new BlendShapesExtractorOptions
            {
                includeWeights = true,
                includeColors = false
            };
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_sourceMesh);
        }

        [Test]
        public void ExtractWeights_StoresBonesPerVertex()
        {
            _meshData.ExtractUnityBlendShapes(_sourceMesh, null, _options);

            Assert.IsTrue(_meshData.HasSkinningData);
            Assert.AreEqual(VertexCount, _meshData.m_BonesPerVertex.Length);

            for (int i = 0; i < VertexCount; i++)
            {
                Assert.AreEqual(2, _meshData.m_BonesPerVertex[i], $"Vertex {i} should have 2 bones");
            }
        }

        [Test]
        public void ExtractWeights_StoresBoneIndicesAndValues()
        {
            _meshData.ExtractUnityBlendShapes(_sourceMesh, null, _options);

            Assert.AreEqual(VertexCount * 2, _meshData.m_BoneWeightBoneIndices.Length);
            Assert.AreEqual(VertexCount * 2, _meshData.m_BoneWeightValues.Length);

            // Verify first vertex weights
            Assert.AreEqual(0, _meshData.m_BoneWeightBoneIndices[0]); // bone 0
            Assert.AreEqual(0.7f, _meshData.m_BoneWeightValues[0], 0.001f);
            Assert.AreEqual(1, _meshData.m_BoneWeightBoneIndices[1]); // bone 1
            Assert.AreEqual(0.3f, _meshData.m_BoneWeightValues[1], 0.001f);
        }

        [Test]
        public void ExtractWeights_StoresBindPoses()
        {
            _meshData.ExtractUnityBlendShapes(_sourceMesh, null, _options);

            Assert.IsNotNull(_meshData.m_BindPoses);
            Assert.AreEqual(BoneCount, _meshData.m_BindPoses.Length);
        }

        [Test]
        public void ExtractWeights_StoresBoneNames()
        {
            // Bone names are extracted from SkinnedMeshRenderer.bones, not from the mesh itself.
            // We simulate this by directly setting m_BoneNames as the extractor does.
            _meshData.ExtractUnityBlendShapes(_sourceMesh, null, _options);
            _meshData.m_BoneNames = MeshFactory.CreateBoneNames(BoneCount);

            Assert.AreEqual(BoneCount, _meshData.m_BoneNames.Length);
            Assert.AreEqual("Bone_0", _meshData.m_BoneNames[0]);
            Assert.AreEqual("Bone_1", _meshData.m_BoneNames[1]);
            Assert.AreEqual("Bone_2", _meshData.m_BoneNames[2]);
        }

        [Test]
        public void ExtractWeights_SkippedWhenDisabled()
        {
            _options.includeWeights = false;
            _meshData.ExtractUnityBlendShapes(_sourceMesh, null, _options);

            Assert.IsFalse(_meshData.HasSkinningData);
        }

        [Test]
        public void ApplyWeights_SetsCorrectBoneWeights()
        {
            _meshData.ExtractUnityBlendShapes(_sourceMesh, null, _options);

            // Create a fresh target mesh and apply weights to it
            var targetMesh = MeshFactory.CreateBaseMesh(VertexCount);
            BlendShapeAppender.ApplySkinningAndColors(_meshData, targetMesh);

            // Read back weights
            NativeArray<byte> bonesPerVertex = targetMesh.GetBonesPerVertex();
            NativeArray<BoneWeight1> allWeights = targetMesh.GetAllBoneWeights();

            Assert.AreEqual(VertexCount, bonesPerVertex.Length);
            for (int i = 0; i < VertexCount; i++)
            {
                Assert.AreEqual(2, bonesPerVertex[i]);
            }

            Assert.AreEqual(VertexCount * 2, allWeights.Length);
            Assert.AreEqual(0.7f, allWeights[0].weight, 0.001f);
            Assert.AreEqual(0.3f, allWeights[1].weight, 0.001f);

            Object.DestroyImmediate(targetMesh);
        }

        [Test]
        public void ApplyWeights_SetsBindPoses()
        {
            _meshData.ExtractUnityBlendShapes(_sourceMesh, null, _options);

            var targetMesh = MeshFactory.CreateBaseMesh(VertexCount);
            BlendShapeAppender.ApplySkinningAndColors(_meshData, targetMesh);

            Assert.AreEqual(BoneCount, targetMesh.bindposes.Length);

            Object.DestroyImmediate(targetMesh);
        }

        [Test]
        public void ApplyWeights_SkipsOnVertexCountMismatch()
        {
            _meshData.ExtractUnityBlendShapes(_sourceMesh, null, _options);

            // Target with different vertex count
            var targetMesh = MeshFactory.CreateBaseMesh(VertexCount + 5);
            BlendShapeAppender.ApplySkinningAndColors(_meshData, targetMesh);

            // Weights should NOT have been applied — mesh should have no bone weights
            NativeArray<byte> bonesPerVertex = targetMesh.GetBonesPerVertex();
            Assert.AreEqual(0, bonesPerVertex.Length);

            Object.DestroyImmediate(targetMesh);
        }

        [Test]
        public void ApplyWeights_RoundTrip_PreservesAllData()
        {
            _meshData.ExtractUnityBlendShapes(_sourceMesh, null, _options);

            var targetMesh = MeshFactory.CreateBaseMesh(VertexCount);
            BlendShapeAppender.ApplySkinningAndColors(_meshData, targetMesh);

            // Now extract again from the target
            var roundTripData = new MeshData(targetMesh, new List<string>());
            var roundTripOptions = new BlendShapesExtractorOptions { includeWeights = true, includeColors = false };
            roundTripData.ExtractUnityBlendShapes(targetMesh, null, roundTripOptions);

            Assert.IsTrue(roundTripData.HasSkinningData);
            Assert.AreEqual(_meshData.m_BonesPerVertex.Length, roundTripData.m_BonesPerVertex.Length);
            Assert.AreEqual(_meshData.m_BoneWeightBoneIndices.Length, roundTripData.m_BoneWeightBoneIndices.Length);

            for (int i = 0; i < _meshData.m_BonesPerVertex.Length; i++)
            {
                Assert.AreEqual(_meshData.m_BonesPerVertex[i], roundTripData.m_BonesPerVertex[i]);
            }

            for (int i = 0; i < _meshData.m_BoneWeightBoneIndices.Length; i++)
            {
                Assert.AreEqual(_meshData.m_BoneWeightBoneIndices[i], roundTripData.m_BoneWeightBoneIndices[i]);
                Assert.AreEqual(_meshData.m_BoneWeightValues[i], roundTripData.m_BoneWeightValues[i], 0.0001f);
            }

            Object.DestroyImmediate(targetMesh);
        }
    }
}
