using System.Collections.Generic;
using UnityEngine;

public class VineGeneration : MonoBehaviour
{
    float startSize = 0.5f;
	float segLength = 0.5f;
    float range = 5;
	float zFightOffset = 0.01f;
    int tendrilCount = 5;

	//Calculated values;
	float deltaSizePerUnit;
	Vector3 rootNormal;
	Vector3 rootBaselineTangent;
	Mesh mesh;

    List<VineSegment> startSegments = new List<VineSegment>();

	private void Start()
	{
		rootNormal = transform.forward;
		rootBaselineTangent = transform.up;
		deltaSizePerUnit = startSize / range;
		transform.rotation = Quaternion.identity;
		print(rootNormal);
		print(rootBaselineTangent);
		print(Vector3.Cross(rootNormal, rootBaselineTangent));

		InitializeVines();
		foreach (VineSegment segment in startSegments)
		{
			PropagateVines(segment, range - segment.length);
		}

		mesh = GenerateMesh();
		GetComponent<MeshFilter>().mesh = mesh;
	}

	Mesh GenerateMesh()
	{
		Mesh mesh = new Mesh();

		List<Vector3> vertices = new List<Vector3>();
		List<int> triangles = new List<int>();

		float ngonScale = startSize / (2 * Mathf.Sin(Mathf.PI / tendrilCount));

		for(int i = 0; i < tendrilCount; i++)
		{
			vertices.Add(rootBaselineTangent * Mathf.Cos((i - 0.5f) * Mathf.PI * 2 / tendrilCount) + Vector3.Cross(rootNormal, rootBaselineTangent) * Mathf.Sin((i - 0.5f) * Mathf.PI * 2 / tendrilCount));
			vertices[i] *= ngonScale;
			vertices[i] += rootNormal * zFightOffset;
		}

		triangles.AddRange(new int[] { 0, 1, 2 });
		triangles.AddRange(new int[] { 0, 2, 3 });
		triangles.AddRange(new int[] { 0, 3, 4 });

		float apothem = ngonScale * Mathf.Cos(Mathf.PI / tendrilCount);
		Stack<VineMeshPiece> vineMeshes = new Stack<VineMeshPiece>();

		for(int i = 0; i < tendrilCount; i++)
		{
			Vector3 start = rootBaselineTangent * Mathf.Cos(i * Mathf.PI * 2 / tendrilCount) + Vector3.Cross(rootNormal, rootBaselineTangent) * Mathf.Sin(i * Mathf.PI * 2 / tendrilCount);
			vineMeshes.Push(new VineMeshPiece(i, (i + 1)%tendrilCount, start, startSegments[i]));
		}

		int vertCount = tendrilCount;

		while(vineMeshes.Count > 0)
		{
			VineMeshPiece vine = vineMeshes.Pop();
			vertices.Add(vertices[vine.startAIndex] + vine.vine.direction.normalized * vine.vine.length);
			vertices.Add(vertices[vine.startBIndex] + vine.vine.direction.normalized * vine.vine.length);

			triangles.AddRange(new int[] { vine.startAIndex, vertCount, vine.startBIndex });
			triangles.AddRange(new int[] { vine.startBIndex, vertCount, vertCount + 1 });

			foreach(VineSegment child in vine.vine.children)
			{
				vineMeshes.Push(new VineMeshPiece(vertCount, vertCount + 1, vine.start + vine.vine.direction, child));
			}

			vertCount+=2;
		}

		for(int i = 0; i < triangles.Count; i+=3) print(triangles[i] + ", " + triangles[i + 1] + ", " + triangles[i + 2]);

		mesh.SetVertices(vertices);
		mesh.SetTriangles(triangles, 0);

		return mesh;
	}

	void InitializeVines()
    {
		for(int i = 0; i < tendrilCount; i++)
		{
			Vector3 direction = rootBaselineTangent * Mathf.Cos(i * Mathf.PI * 2 / tendrilCount) + Vector3.Cross(rootNormal, rootBaselineTangent) * Mathf.Sin(i * Mathf.PI * 2 / tendrilCount);
			startSegments.Add(GenerateVineSegment(rootNormal, direction, startSize));
		}
    }

	void PropagateVines(VineSegment vine, float remainingLength)
	{
		if(remainingLength <= segLength / 10) return;
		
		vine.children.Add(GenerateVineSegment(vine.normal, vine.direction, vine.endSize));

		foreach(VineSegment child in vine.children)
		{
			PropagateVines(child, remainingLength - child.length);
		}
	}

	VineSegment GenerateVineSegment(Vector3 normal, Vector3 direction, float startSize)
	{
		//print(direction + ", " + startSize);
		return new VineSegment(rootNormal, direction, startSize, startSize - deltaSizePerUnit * segLength, segLength);
	}
}

public struct VineSegment
{
    public Vector3 normal;
    public Vector3 direction;
    public float startSize;
    public float endSize;
    public float length;
    public List<VineSegment> children;

	public VineSegment(Vector3 normal, Vector3 direction, float startSize, float endSize, float length)
	{
		this.normal=normal;
		this.direction=direction;
		this.startSize=startSize;
		this.endSize=endSize;
		this.length=length;
        children = new List<VineSegment>();
	}
}

public struct VineMeshPiece
{
	public int startAIndex;
	public int startBIndex;
	public Vector3 start;
	public VineSegment vine;

	public VineMeshPiece(int startAIndex, int startBIndex, Vector3 start, VineSegment vine)
	{
		this.startAIndex=startAIndex;
		this.startBIndex=startBIndex;
		this.start=start;
		this.vine=vine;
	}
}