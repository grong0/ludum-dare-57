using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class VineGeneration : MonoBehaviour
{
    float startSize = 0.25f;
	float endSize = .1f;
	float segLength = .5f;
    float range = 5;
	float zFightOffset = 0.01f;
    int tendrilCount = 7;

	int randomDeg = 20;
	bool random = true;

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
		deltaSizePerUnit = (startSize - endSize) / range;
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

		for(int i = 1; i < tendrilCount - 1; i++)
		{
			triangles.AddRange(new int[] { 0, i, i+1 });
		}
		
		Stack<VineMeshPiece> vineMeshes = new Stack<VineMeshPiece>();

		for(int i = 0; i < tendrilCount; i++)
		{
			vineMeshes.Push(new VineMeshPiece(i, (i + 1)%tendrilCount, startSegments[i]));
		}

		int vertCount = tendrilCount;

		while(vineMeshes.Count > 0)
		{
			VineMeshPiece vine = vineMeshes.Pop();
			vertices.Add(vine.vine.start + vine.vine.direction * vine.vine.length - Vector3.Cross(vine.vine.normal, vine.vine.direction).normalized * vine.vine.endSize / 2);
			vertices.Add(vine.vine.start + vine.vine.direction * vine.vine.length + Vector3.Cross(vine.vine.normal, vine.vine.direction).normalized * vine.vine.endSize / 2);

			triangles.AddRange(new int[] { vine.startAIndex, vertCount, vine.startBIndex });
			triangles.AddRange(new int[] { vine.startBIndex, vertCount, vertCount + 1 });

			foreach(VineSegment child in vine.vine.children)
			{
				vineMeshes.Push(new VineMeshPiece(vertCount, vertCount + 1, child));
			}

			vertCount+=2;
		}

		//for(int i = 0; i < triangles.Count; i+=3) print(triangles[i] + ", " + triangles[i + 1] + ", " + triangles[i + 2]);

		mesh.SetVertices(vertices);
		mesh.SetTriangles(triangles, 0);

		return mesh;
	}

	void InitializeVines()
    {
		float apothem = startSize / (2 * Mathf.Tan(Mathf.PI / tendrilCount));
		for(int i = 0; i < tendrilCount; i++)
		{
			Vector3 direction = rootBaselineTangent * Mathf.Cos(i * Mathf.PI * 2 / tendrilCount) + Vector3.Cross(rootNormal, rootBaselineTangent) * Mathf.Sin(i * Mathf.PI * 2 / tendrilCount);
			Vector3 start = direction * apothem + rootNormal * zFightOffset;
			direction = RandomRotation(rootNormal, 180 / tendrilCount) * direction;
			startSegments.Add(GenerateVineSegment(rootNormal, direction, start, startSize));
		}
    }

	void PropagateVines(VineSegment vine, float remainingLength)
	{
		if(remainingLength <= segLength / 10) return;
		if(vine.endSize <= 0) return;

		Vector3 childStartPoint = vine.start + vine.direction * vine.length;

		if(Random.value > 0.05f) vine.children.Add(GenerateVineSegment(vine.normal, RandomRotation(vine.normal, randomDeg) * vine.direction, childStartPoint, vine.endSize));
		else
		{
			vine.children.Add(GenerateVineSegment(vine.normal, RandomRotation(vine.normal, 20, 40) * vine.direction, childStartPoint, vine.endSize)); 
			vine.children.Add(GenerateVineSegment(vine.normal, RandomRotation(vine.normal, -40, -20) * vine.direction, childStartPoint, vine.endSize));
		}

		if(Random.value < 0.03f) vine.children.Add(GenerateVineSegment(vine.normal, RandomRotation(vine.normal, 70, 90) * vine.direction, childStartPoint, vine.endSize - deltaSizePerUnit));

		foreach(VineSegment child in vine.children)
		{
			VineSegment lastChild = child;
			while(lastChild.children.Count > 0) lastChild = lastChild.children[0];
			PropagateVines(lastChild, remainingLength - lastChild.length);
		}
	}

	VineSegment GenerateVineSegment(Vector3 normal, Vector3 direction, Vector3 start, float startSize) //Refactor to take in length and spawn the second half of a split one using this function
	{
		normal.Normalize();
		direction.Normalize();
		Vector3 worldStart = start + transform.position;
		float raycastOffset = segLength / 10;
		RaycastHit hit;

		Debug.DrawLine(worldStart + normal * raycastOffset, worldStart + normal * raycastOffset + direction * segLength, Color.red, 10f);
		Debug.DrawLine(worldStart + normal * raycastOffset + direction * segLength, worldStart + normal * raycastOffset + direction * segLength - normal * raycastOffset * 2, Color.green, 10f);
		Debug.DrawLine(worldStart - normal * raycastOffset + direction * segLength, worldStart - normal * raycastOffset + direction * segLength - direction * segLength, Color.blue, 10f);

		if(Physics.Raycast(worldStart + normal * raycastOffset, direction, out hit, segLength))
		{
			print("Hit smthg");
			print(worldStart);


			(Vector3 point, Vector3 vector) edge = GetPlaneIntersection(worldStart, normal, hit.point + hit.normal * zFightOffset, hit.normal);

			Vector3 intersection = GetLineIntersection(worldStart, direction, edge.point, edge.vector);

			print(intersection);

			float newLength = Vector3.Distance(worldStart, intersection);

			print(newLength);

			VineSegment vine = new VineSegment(normal, direction, start, startSize, startSize - deltaSizePerUnit * newLength, newLength, true);

			// Compute rotation angle
			float angle = Vector3.SignedAngle(normal, hit.normal, edge.vector);
			// Rotate the vector
			Vector3 newDirection = Quaternion.AngleAxis(angle, edge.vector) * direction;

			vine.children.Add(new VineSegment(hit.normal, newDirection, intersection - transform.position, startSize, startSize - deltaSizePerUnit * segLength, segLength - newLength, false));

			return vine;
		}
		else if(Physics.Raycast(worldStart + normal * raycastOffset + direction * segLength, -normal, out hit, raycastOffset * 2))
		{
			return new VineSegment(normal, direction, start, startSize, startSize - deltaSizePerUnit * segLength, segLength, false);
		}
		else if(Physics.Raycast(worldStart - normal * raycastOffset + direction * segLength, -direction, out hit, segLength))
		{
			print("Hit smthg - but different");
			print(worldStart);

			(Vector3 point, Vector3 vector) edge = GetPlaneIntersection(worldStart, normal, hit.point + hit.normal * zFightOffset, hit.normal);

			Vector3 intersection = GetLineIntersection(worldStart, direction, edge.point, edge.vector);

			print(intersection);

			float newLength = Vector3.Distance(worldStart, intersection);

			print(newLength);

			VineSegment vine = new VineSegment(normal, direction, start, startSize, startSize - deltaSizePerUnit * newLength, newLength, true);

			// Compute rotation angle
			float angle = Vector3.SignedAngle(normal, hit.normal, edge.vector);
			// Rotate the vector
			Vector3 newDirection = Quaternion.AngleAxis(angle, edge.vector) * direction;

			vine.children.Add(new VineSegment(hit.normal, newDirection, intersection - transform.position, startSize, startSize - deltaSizePerUnit * segLength, segLength - newLength, false));

			return vine;
		}
		print("HELP IDK WHAT HAPPENED");
		return new VineSegment(normal, direction, start, startSize, startSize - deltaSizePerUnit * segLength, segLength, false);
	}

	Quaternion RandomRotation(Vector3 axis, float maxAngle) {
		return RandomRotation(axis, -maxAngle, maxAngle);
	}

	Quaternion RandomRotation(Vector3 axis, float minAngle, float maxAngle)
	{
		if(!random) return Quaternion.identity;
		float rand = Random.Range(minAngle, maxAngle);
		return Quaternion.AngleAxis(rand, axis);
	}

	static Vector3 GetLineIntersection(Vector3 startA, Vector3 dirA, Vector3 startB, Vector3 dirB, float tolerance = 0.01f)
	{
		dirA.Normalize();
		dirB.Normalize();

		Vector3 cross = Vector3.Cross(dirA, dirB);
		float denom = cross.sqrMagnitude;

		// If cross product is ~0, the lines are parallel (no intersection)
		if(denom < 1e-6f)
			return Vector3.zero;

		Vector3 delta = startB - startA;

		// Solve for scalars t and u along dirA and dirB
		float t = Vector3.Dot(Vector3.Cross(delta, dirB), cross) / denom;
		float u = Vector3.Dot(Vector3.Cross(delta, dirA), cross) / denom;

		Vector3 pointOnA = startA + dirA * t;
		Vector3 pointOnB = startB + dirB * u;

		// If the points are close enough, return the average point
		if(Vector3.Distance(pointOnA, pointOnB) <= tolerance)
			return (pointOnA + pointOnB) * 0.5f;

		return Vector3.zero; // No intersection within tolerance
	}

	static (Vector3, Vector3) GetPlaneIntersection(Vector3 pointA, Vector3 normalA, Vector3 pointB, Vector3 normalB)
	{
		// Direction of the line of intersection
		Vector3 direction = Vector3.Cross(normalA, normalB);

		// If direction is zero, planes are parallel or coincident
		if (direction.sqrMagnitude < 1e-6f)
			return (Vector3.zero, Vector3.zero); // No intersection or infinite intersection

		// Construct matrix to solve for point on line using Cramer's Rule
		Vector3 n1 = normalA;
		Vector3 n2 = normalB;

		float d1 = -Vector3.Dot(n1, pointA);
		float d2 = -Vector3.Dot(n2, pointB);

		// Solve system: A * x = b, where rows of A are normals and b = -dot(normal, point)
		// We're finding the point that lies on both planes
		Vector3 n1xn2 = Vector3.Cross(n1, n2);
		Vector3 pointOnLine =
			((Vector3.Cross(n1xn2, n2) * d1) + (Vector3.Cross(n1, n1xn2) * d2)) / Vector3.Dot(n1xn2, n1xn2);

		return (pointOnLine, direction.normalized);
	}
}

public struct VineSegment
{
    public Vector3 normal;
    public Vector3 direction;
	public Vector3 start;
	public float startSize;
    public float endSize;
    public float length;
	public bool isSplit;
    public List<VineSegment> children;

	public VineSegment(Vector3 normal, Vector3 direction, Vector3 start, float startSize, float endSize, float length, bool isSplit)
	{
		this.normal=normal;
		this.direction=direction;
		this.start=start;
		this.startSize=startSize;
		this.endSize=endSize;
		this.length=length;
		this.isSplit=isSplit;
		children = new List<VineSegment>();
	}
}

public struct VineMeshPiece
{
	public int startAIndex;
	public int startBIndex;
	public VineSegment vine;

	public VineMeshPiece(int startAIndex, int startBIndex, VineSegment vine)
	{
		this.startAIndex=startAIndex;
		this.startBIndex=startBIndex;
		this.vine=vine;
	}
}