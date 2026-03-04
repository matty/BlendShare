using UnityEngine;

namespace Triturbo.BlendShapeShare.Tests
{
    /// <summary>
    /// Creates test meshes programmatically with known values for deterministic testing.
    /// </summary>
    public static class MeshFactory
    {
        /// <summary>
        /// Creates a simple mesh with the given vertex count.
        /// Vertices are placed along a line with predictable positions.
        /// A single triangle is created from the first 3 vertices (or a degenerate one if fewer).
        /// </summary>
        public static Mesh CreateBaseMesh(int vertexCount)
        {
            var mesh = new Mesh();
            mesh.name = "TestMesh";

            var vertices = new Vector3[vertexCount];
            var normals = new Vector3[vertexCount];
            for (int i = 0; i < vertexCount; i++)
            {
                vertices[i] = new Vector3(i * 0.1f, i * 0.2f, i * 0.3f);
                normals[i] = Vector3.up;
            }

            mesh.vertices = vertices;
            mesh.normals = normals;

            // Need at least 3 vertices for a triangle
            if (vertexCount >= 3)
            {
                mesh.triangles = new int[] { 0, 1, 2 };
            }

            return mesh;
        }

        /// <summary>
        /// Adds blend shapes with known delta values to a mesh.
        /// Each shape gets one frame with deltaVertices = (shapeIndex+1) * 0.01 in all axes per vertex.
        /// </summary>
        public static Mesh CreateMeshWithBlendShapes(Mesh baseMesh, string[] shapeNames)
        {
            var mesh = Object.Instantiate(baseMesh);
            mesh.name = baseMesh.name;

            int vertexCount = mesh.vertexCount;
            for (int s = 0; s < shapeNames.Length; s++)
            {
                var deltaVertices = new Vector3[vertexCount];
                var deltaNormals = new Vector3[vertexCount];
                var deltaTangents = new Vector3[vertexCount];

                float scale = (s + 1) * 0.01f;
                for (int v = 0; v < vertexCount; v++)
                {
                    deltaVertices[v] = new Vector3(scale, scale, scale);
                    deltaNormals[v] = new Vector3(0, scale, 0);
                    deltaTangents[v] = Vector3.zero;
                }

                mesh.AddBlendShapeFrame(shapeNames[s], 100f, deltaVertices, deltaNormals, deltaTangents);
            }

            return mesh;
        }

        /// <summary>
        /// Sets up bone weights on a mesh using the lossless BoneWeight1 API.
        /// Each vertex gets 2 bones with weights 0.7 and 0.3.
        /// </summary>
        public static Mesh CreateMeshWithWeights(Mesh mesh, int boneCount)
        {
            int vertexCount = mesh.vertexCount;

            // Each vertex has 2 bones
            var bonesPerVertex = new byte[vertexCount];
            var boneWeight1List = new Unity.Collections.NativeArray<BoneWeight1>(vertexCount * 2, Unity.Collections.Allocator.Temp);
            var bonesPerVertexNative = new Unity.Collections.NativeArray<byte>(vertexCount, Unity.Collections.Allocator.Temp);

            for (int i = 0; i < vertexCount; i++)
            {
                bonesPerVertex[i] = 2;
                bonesPerVertexNative[i] = 2;

                int bone0 = i % boneCount;
                int bone1 = (i + 1) % boneCount;

                boneWeight1List[i * 2] = new BoneWeight1 { boneIndex = bone0, weight = 0.7f };
                boneWeight1List[i * 2 + 1] = new BoneWeight1 { boneIndex = bone1, weight = 0.3f };
            }

            mesh.SetBoneWeights(bonesPerVertexNative, boneWeight1List);

            // Set bind poses
            var bindPoses = new Matrix4x4[boneCount];
            for (int i = 0; i < boneCount; i++)
            {
                bindPoses[i] = Matrix4x4.TRS(
                    new Vector3(i, 0, 0),
                    Quaternion.identity,
                    Vector3.one
                ).inverse;
            }
            mesh.bindposes = bindPoses;

            bonesPerVertexNative.Dispose();
            boneWeight1List.Dispose();

            return mesh;
        }

        /// <summary>
        /// Sets vertex colors with a known gradient pattern.
        /// Vertex i gets color (i/count, 0.5, 1 - i/count, 1).
        /// </summary>
        public static Mesh CreateMeshWithColors(Mesh mesh)
        {
            int vertexCount = mesh.vertexCount;
            var colors = new Color[vertexCount];
            for (int i = 0; i < vertexCount; i++)
            {
                float t = vertexCount > 1 ? (float)i / (vertexCount - 1) : 0f;
                colors[i] = new Color(t, 0.5f, 1f - t, 1f);
            }
            mesh.colors = colors;
            return mesh;
        }

        /// <summary>
        /// Creates a GameObject with a SkinnedMeshRenderer, bones, and the given mesh.
        /// </summary>
        public static GameObject CreateSkinnedMeshRenderer(Mesh mesh, int boneCount = 0)
        {
            var go = new GameObject("TestSMR");
            var smr = go.AddComponent<SkinnedMeshRenderer>();

            if (boneCount > 0)
            {
                var bones = new Transform[boneCount];
                for (int i = 0; i < boneCount; i++)
                {
                    var boneGO = new GameObject($"Bone_{i}");
                    boneGO.transform.SetParent(go.transform);
                    boneGO.transform.localPosition = new Vector3(i, 0, 0);
                    bones[i] = boneGO.transform;
                }
                smr.bones = bones;
            }

            smr.sharedMesh = mesh;
            return go;
        }

        /// <summary>
        /// Generates bone name array matching what CreateSkinnedMeshRenderer produces.
        /// </summary>
        public static string[] CreateBoneNames(int boneCount)
        {
            var names = new string[boneCount];
            for (int i = 0; i < boneCount; i++)
            {
                names[i] = $"Bone_{i}";
            }
            return names;
        }
    }
}
