using System.Collections.Generic;
using NUnit.Framework;
using Triturbo.BlendShapeShare.BlendShapeData;
using UnityEngine;

namespace Triturbo.BlendShapeShare.Tests
{
    public class MeshDataTests
    {
        [Test]
        public void MeshData_Constructor_SetsCorrectFields()
        {
            var mesh = MeshFactory.CreateBaseMesh(4);
            var shapeNames = new List<string> { "Shape_A", "Shape_B" };

            var meshData = new MeshData(mesh, shapeNames);

            Assert.AreEqual("TestMesh", meshData.m_MeshName);
            Assert.AreEqual(4, meshData.m_VertexCount);
            Assert.AreEqual(mesh, meshData.m_OriginMesh);
            Assert.AreEqual(2, meshData.m_ShapeNames.Count);
            Assert.IsTrue(meshData.ContainsBlendShape("Shape_A"));
            Assert.IsTrue(meshData.ContainsBlendShape("Shape_B"));

            Object.DestroyImmediate(mesh);
        }

        [Test]
        public void MeshData_IsValidTarget_ReturnsTrueForMatchingMesh()
        {
            var mesh = MeshFactory.CreateBaseMesh(4);
            var meshData = new MeshData(mesh, new List<string>());

            // Same mesh should be valid
            Assert.IsTrue(meshData.IsValidTarget(mesh));

            // A copy with identical vertices should also be valid
            var copy = Object.Instantiate(mesh);
            Assert.IsTrue(meshData.IsValidTarget(copy));

            Object.DestroyImmediate(mesh);
            Object.DestroyImmediate(copy);
        }

        [Test]
        public void MeshData_IsValidTarget_ReturnsFalseForDifferentMesh()
        {
            var mesh = MeshFactory.CreateBaseMesh(4);
            var meshData = new MeshData(mesh, new List<string>());

            // Different vertex count
            var differentMesh = MeshFactory.CreateBaseMesh(8);
            Assert.IsFalse(meshData.IsValidTarget(differentMesh));

            // Null mesh
            Assert.IsFalse(meshData.IsValidTarget(null));

            Object.DestroyImmediate(mesh);
            Object.DestroyImmediate(differentMesh);
        }

        [Test]
        public void MeshData_HasSkinningData_FalseWhenEmpty()
        {
            var mesh = MeshFactory.CreateBaseMesh(4);
            var meshData = new MeshData(mesh, new List<string>());

            Assert.IsFalse(meshData.HasSkinningData);

            Object.DestroyImmediate(mesh);
        }

        [Test]
        public void MeshData_HasVertexColors_FalseWhenEmpty()
        {
            var mesh = MeshFactory.CreateBaseMesh(4);
            var meshData = new MeshData(mesh, new List<string>());

            Assert.IsFalse(meshData.HasVertexColors);

            Object.DestroyImmediate(mesh);
        }
    }
}
