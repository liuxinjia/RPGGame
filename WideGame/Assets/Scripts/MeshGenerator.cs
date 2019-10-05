﻿using System.Collections;
using UnityEngine;

public static class MeshGenerator {

	public static MeshData GenerateTerrainMesh (float[, ] heightMap, float heightMultiplier, AnimationCurve _heightCurve, int levelOfDetail) {
		AnimationCurve heightCurve = new AnimationCurve (_heightCurve.keys);

		int meshSimplificationIncrement = (levelOfDetail == 0) ? 1 : levelOfDetail * 2;

		int borderSize = heightMap.GetLength (0);
		int meshSize = borderSize - 2;
		float topLeftX = (meshSize - 1) / -2f;
		float topLeftZ = (meshSize - 1) / 2f;

		int verticesPerLine = (meshSize - 1) / meshSimplificationIncrement + 1;

		MeshData meshData = new MeshData (verticesPerLine);

		int[, ] vertexIndicesMap = new int[borderSize, borderSize];
		int meshVertexIndex = 0;
		int borderVertexIndex = -1;

		for (int y = 0; y < borderSize; y += meshSimplificationIncrement) {
			for (int x = 0; x < borderSize; x += meshSimplificationIncrement) {
				bool isBorderVertex = y == 0 || y == borderSize - 1 || x == 0 || x == borderSize - 1;

				if (isBorderVertex) {
					vertexIndicesMap[x, y] = borderVertexIndex;
					borderVertexIndex--;
				} else {
					vertexIndicesMap[x, y] = meshVertexIndex;
					meshVertexIndex++;
				}
			}
		}

		for (int y = 0; y < borderSize; y += meshSimplificationIncrement) {
			for (int x = 0; x < borderSize; x += meshSimplificationIncrement) {
				int vertexIndex = vertexIndicesMap[x, y];
				Vector3 percent = new Vector2 ((x - meshSimplificationIncrement) / (float) meshSize, (y - meshSimplificationIncrement) / (float) meshSize);
				float height = heightCurve.Evaluate (heightMap[x, y]) * heightMultiplier;
				Vector3 vertexPosition = new Vector3 (topLeftX + percent.x * meshSize, height, topLeftZ - percent.y * meshSize);

				meshData.AddVertex (vertexPosition, percent, vertexIndex);
				if (x < borderSize - 1 && y < borderSize - 1) {
					int a = vertexIndicesMap[x, y];
					int b = vertexIndicesMap[x + meshSimplificationIncrement, y];
					int c = vertexIndicesMap[x, y + meshSimplificationIncrement];
					int d = vertexIndicesMap[x + meshSimplificationIncrement, y + meshSimplificationIncrement];
					meshData.AddTriangle (a, d, c);
					meshData.AddTriangle (d, a, b);
				}

				vertexIndex++;
			}
		}

		return meshData;

	}
}

public class MeshData {
	public Vector3[] vertices;
	int[] triangles;
	Vector2[] uvs;

	Vector3[] borderVertices;
	int[] borderTriangles;

	int borderTriangIndex;
	int triangleIndex;

	public MeshData (int vertexPerline) {
		vertices = new Vector3[vertexPerline * vertexPerline];
		uvs = new Vector2[vertexPerline * vertexPerline];
		triangles = new int[(vertexPerline - 1) * (vertexPerline - 1) * 6];

		borderVertices = new Vector3[vertexPerline * 4 + 4];
		borderTriangles = new int[6 * 4 * vertexPerline];

	}
	public void AddVertex (Vector3 vertexPosition, Vector3 uv, int vertexIndex) {
		if (vertexIndex < 0) {
			borderVertices[-vertexIndex - 1] = vertexPosition;
		} else {
			vertices[vertexIndex] = vertexPosition;
			uvs[vertexIndex] = uv;
		}
	}

	public void AddTriangle (int a, int b, int c) {
		if (a < 0 || b < 0 || c < 0) {
			borderTriangles[borderTriangIndex] = a;
			borderTriangles[borderTriangIndex + 1] = b;
			borderTriangles[borderTriangIndex + 2] = c;
			borderTriangIndex += 3;
		} else {
			triangles[triangleIndex] = a;
			triangles[triangleIndex + 1] = b;
			triangles[triangleIndex + 2] = c;
			triangleIndex += 3;
		}
	}

	public Vector3[] CalculateNormals () {

		Vector3[] vertexNormals = new Vector3[vertices.Length];
		int triangleCount = triangles.Length / 3;
		for (int i = 0; i < triangleCount; i++) {
			int normalTriangleIndex = i * 3;
			int vertexIndexA = triangles[normalTriangleIndex];
			int vertexIndexB = triangles[normalTriangleIndex + 1];
			int vertexIndexC = triangles[normalTriangleIndex + 2];

			Vector3 triangleNormal = SurfaceNormalFromIndices (vertexIndexA, vertexIndexB, vertexIndexC);
			vertexNormals[vertexIndexA] += triangleNormal;
			vertexNormals[vertexIndexB] += triangleNormal;
			vertexNormals[vertexIndexC] += triangleNormal;
		}

		int borderTriangleCount = borderTriangles.Length / 3;
		for (int i = 0; i < borderTriangleCount; i++) {
			int normalTriangleIndex = i * 3;
			int vertexIndexA = borderTriangles[normalTriangleIndex];
			int vertexIndexB = borderTriangles[normalTriangleIndex + 1];
			int vertexIndexC = borderTriangles[normalTriangleIndex + 2];

			Vector3 triangleNormal = SurfaceNormalFromIndices (vertexIndexA, vertexIndexB, vertexIndexC);
			if (vertexIndexA >= 0) {
				vertexNormals[vertexIndexA] += triangleNormal;
			}
			if (vertexIndexB >= 0) {
				vertexNormals[vertexIndexB] += triangleNormal;
			}
			if (vertexIndexC >= 0) {
				vertexNormals[vertexIndexC] += triangleNormal;
			}
		}

		for (int i = 0; i < vertices.Length; i++) {
			vertexNormals[i].Normalize ();
		}

		return vertexNormals;
	}

	Vector3 SurfaceNormalFromIndices (int indexA, int indexB, int indexC) {
		Vector3 pointA = vertices[indexA < 0 ? -indexA + 1 : indexA];
		Vector3 pointB = vertices[indexB < 0 ? -indexB + 1 : indexB];
		Vector3 pointC = vertices[indexC < 0 ? -indexC + 1 : indexC];

		Vector3 sideAB = pointA - pointB;
		Vector3 sideAC = pointA - pointC;
		return Vector3.Cross (sideAB, sideAC).normalized;
	}

	public Mesh CreateMesh () {
		Mesh mesh = new Mesh ();
		mesh.vertices = vertices;
		mesh.triangles = triangles;
		mesh.uv = uvs;
		mesh.normals = CalculateNormals ();
		return mesh;
	}

}