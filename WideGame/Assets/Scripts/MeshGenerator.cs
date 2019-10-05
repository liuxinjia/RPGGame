using System.Collections;
using UnityEngine;

public static class MeshGenerator {

	public static MeshData GenerateTerrainMesh (float[, ] heightMap, float heightMultiplier, AnimationCurve _heightCurve, int levelOfDetail) {
		AnimationCurve heightCurve = new AnimationCurve (_heightCurve.keys);

		int meshSimplificationIncrement = (levelOfDetail == 0) ? 1 : levelOfDetail * 2;

		int borderSize = heightMap.GetLength (0);
		int meshSize = borderSize - 2 * meshSimplificationIncrement;
		int meshSizeUnSimplified = borderSize - 2;
		float topLeftX = (meshSizeUnSimplified - 1) / -2f;
		float topLeftZ = (meshSizeUnSimplified - 1) / 2f;

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
				Vector3 vertexPosition = new Vector3 (topLeftX + percent.x * meshSizeUnSimplified, height, topLeftZ - percent.y * meshSizeUnSimplified);

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

		meshData.BakeNormals ();

		return meshData;

	}
}

public class MeshData {
	public Vector3[] vertices;
	int[] triangles;
	Vector3[] bakeNormals;
	Vector2[] uvs;

	Vector3[] borderVertices;
	int[] borderTriangles;

	int borderTriangleIndex;
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
			borderTriangles[borderTriangleIndex] = a;
			borderTriangles[borderTriangleIndex + 1] = b;
			borderTriangles[borderTriangleIndex + 2] = c;
			borderTriangleIndex += 3;
		} else {
			triangles[triangleIndex] = a;
			triangles[triangleIndex + 1] = b;
			triangles[triangleIndex + 2] = c;
			triangleIndex += 3;
		}
	}

	public void BakeNormals () {
		bakeNormals = CalculateNormals ();
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
		Vector3 pointA = (indexA < 0) ? borderVertices[-indexA - 1] : vertices[indexA];
		Vector3 pointB = (indexB < 0) ? borderVertices[-indexB - 1] : vertices[indexB];
		Vector3 pointC = (indexC < 0) ? borderVertices[-indexC - 1] : vertices[indexC];

		Vector3 sideAB = pointB - pointA;
		Vector3 sideAC = pointC - pointA;

		return Vector3.Cross (sideAB, sideAC).normalized;
	}

	public Mesh CreateMesh () {
		Mesh mesh = new Mesh ();
		mesh.vertices = vertices;
		mesh.triangles = triangles;
		mesh.uv = uvs;
		mesh.normals = bakeNormals;
		return mesh;
	}

}