using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem.HID;
using UnityEngine.UIElements;

public class VineGeneration : MonoBehaviour
{
	public GameObject leaf;
	public Transform leafChild;

    float startSize = 0.1f;
	float endSize = .025f;
	float segLength = .1f;
    float range = 2;
	float zFightOffset = 0.001f;
    int tendrilCount = 7;
	float leafSpacing = 0.08f;
	float leafScale = 0.06f;
	float leafScaleMin = 0.02f;
	float animationTime = -1;
	float animationLength = 1;

	int randomDeg = 20;
	float splitFactor = 0.01f;
	bool random = true;

	//Calculated values;
	float deltaSizePerUnit;
	Vector3 rootNormal;
	Vector3 rootBaselineTangent;
	Mesh mesh;

	List<VineSegment?> startSegments = new List<VineSegment?>();
	List<(Transform leafTransform, float distance)> leaves = new List<(Transform, float)> ();

	public void Generate()
	{
		leafChild = transform.GetChild(0);
		rootNormal = transform.forward;
		rootBaselineTangent = transform.up;
		deltaSizePerUnit = (startSize - endSize) / range;
		transform.rotation = Quaternion.identity;
		//print(rootNormal);
		//print(rootBaselineTangent);
		//print(Vector3.Cross(rootNormal, rootBaselineTangent));

		InitializeVines();
		foreach (VineSegment? segment in startSegments)
		{
			if(segment != null)
			{
				VineSegment lastChild = (VineSegment)segment;
				while(lastChild.children.Count > 0) lastChild = lastChild.children[0];
				PropagateVines(lastChild, range - segLength);
			}
		}

		mesh = new Mesh();
		GenerateMesh(mesh);
		GetComponent<MeshFilter>().mesh = mesh;
		UpdateMesh(mesh, 0);

		animationTime = 0;

		//Transform[] childTransforms = leafChild.GetComponentsInChildren<Transform>();
		//CombineLeafMeshes(childTransforms);
	}

	private void Update()
	{
		if(animationTime >= 0)
		{
			if(animationTime <= animationLength)
			{
				UpdateMesh(mesh, animationTime / animationLength);
			}
			else
			{
				UpdateMesh(mesh, 1);
				CombineLeafMeshes();
				animationTime = -1;
			}
			animationTime += Time.deltaTime;
		}
	}

	void GenerateMesh(Mesh mesh)
	{
		List<Vector3> vertices = new List<Vector3>();
		List<int> triangles = new List<int>();

		float ngonScale = startSize / (2 * Mathf.Sin(Mathf.PI / tendrilCount)); 
		float apothem = startSize / (2 * Mathf.Tan(Mathf.PI / tendrilCount));

		vertices.Add(rootNormal * (zFightOffset + startSize / 4));

		for(int i = 0; i < tendrilCount; i++)
		{
			vertices.Add(rootBaselineTangent * Mathf.Cos((i - 0.5f) * Mathf.PI * 2 / tendrilCount) + Vector3.Cross(rootNormal, rootBaselineTangent) * Mathf.Sin((i - 0.5f) * Mathf.PI * 2 / tendrilCount));
			vertices[2 * i + 1] *= ngonScale;
			vertices[2 * i + 1] += rootNormal * zFightOffset; 
			vertices.Add(rootBaselineTangent * Mathf.Cos((i) * Mathf.PI * 2 / tendrilCount) + Vector3.Cross(rootNormal, rootBaselineTangent) * Mathf.Sin((i) * Mathf.PI * 2 / tendrilCount));
			vertices[2 * i + 2] *= apothem;
			vertices[2 * i + 2] += rootNormal * (zFightOffset + startSize / 4);
		}

		for(int i = 0; i < tendrilCount * 2; i++)
		{
			triangles.AddRange(new int[] { 0, i + 1, (i+1) % (tendrilCount * 2) + 1});
		}
		
		Stack<VineMeshPiece> vineMeshes = new Stack<VineMeshPiece>();
		
		for(int i = 0; i < tendrilCount; i++)
		{
			if(startSegments[i] != null) vineMeshes.Push(new VineMeshPiece(i * 2 + 1, (i*2 + 2)%(tendrilCount*2) + 1, i * 2 + 2, (VineSegment)startSegments[i]));
		}

		int vertCount = 2 * tendrilCount + 1;

		float leafDistance = 0;

		while(vineMeshes.Count > 0)
		{
			VineMeshPiece vine = vineMeshes.Pop();
			vertices.Add(vine.vine.endPointA);
			vertices.Add(vine.vine.endPointB);
			vertices.Add(vine.vine.endPointC);

			triangles.AddRange(new int[] { vine.startAIndex, vertCount, vine.startCIndex });
			triangles.AddRange(new int[] { vine.startCIndex, vertCount, vertCount + 2 });
			triangles.AddRange(new int[] { vine.startCIndex, vertCount + 2, vine.startBIndex });
			triangles.AddRange(new int[] { vine.startBIndex, vertCount + 2, vertCount + 1 });

			float distanceFromStart = 1 - (vine.vine.startSize - endSize) / (startSize - endSize);

			for(; leafDistance < vine.vine.length; leafDistance+=leafSpacing)
			{
				Vector3 leafPosition = vine.vine.start + vine.vine.direction * leafDistance + transform.position;
				Quaternion leafRotation = Quaternion.LookRotation(RandomRotation(vine.vine.normal, 0, 360) * vine.vine.direction, vine.vine.normal);
				leafRotation *= RandomRotation(vine.vine.direction, 20);
				leafRotation *= RandomRotation(Vector3.Cross(vine.vine.direction, vine.vine.normal), 20);
				leaves.Add((Instantiate(leaf, leafPosition, leafRotation, leafChild).transform, distanceFromStart + leafDistance / range));
			}
			leafDistance -= vine.vine.length;

			foreach(VineSegment child in vine.vine.children)
			{
				vineMeshes.Push(new VineMeshPiece(vertCount, vertCount + 1, vertCount + 2, child));
			}

			vertCount+=3;
		}
		
		//for(int i = 0; i < triangles.Count; i+=3) print(triangles[i] + ", " + triangles[i + 1] + ", " + triangles[i + 2]);

		mesh.SetVertices(vertices);
		mesh.SetTriangles(triangles, 0);
		mesh.RecalculateNormals();
	}

	/// <summary>
	/// Updates the mesh during the animation phase
	/// </summary>
	/// <param name="mesh">Mesh to update</param>
	/// <param name="t">Value between 0 and 1 of the progress through the animation</param>
	void UpdateMesh(Mesh mesh, float t)
	{
		Stack<VineMeshPiece> vineMeshes = new Stack<VineMeshPiece>();

		for(int i = 0; i < tendrilCount; i++)
		{
			if(startSegments[i] != null) vineMeshes.Push(new VineMeshPiece(i * 2 + 1, (i*2 + 2)%(tendrilCount*2) + 1, i * 2 + 2, (VineSegment)startSegments[i]));
		}

		int vertIndex = 2 * tendrilCount + 1;

		//float leafDistance = 0;
		Vector3[] vertices = mesh.vertices;

		while(vineMeshes.Count > 0)
		{
			VineMeshPiece vine = vineMeshes.Pop();

			float distanceFromStart = 1 - (vine.vine.startSize - endSize) / (startSize - endSize); //from 0 to 1
			float animAmount = Mathf.Clamp01(t * 3 - distanceFromStart);

			vertices[vertIndex] = Vector3.Lerp(vine.vine.start + vine.vine.direction * vine.vine.length, vine.vine.endPointA, animAmount);
			vertices[vertIndex + 1] = Vector3.Lerp(vine.vine.start + vine.vine.direction * vine.vine.length, vine.vine.endPointB, animAmount);
			vertices[vertIndex + 2] = Vector3.Lerp(vine.vine.start + vine.vine.direction * vine.vine.length, vine.vine.endPointC, animAmount);

			foreach(VineSegment child in vine.vine.children)
			{
				vineMeshes.Push(new VineMeshPiece(vertIndex, vertIndex + 1, vertIndex + 2, child));
			}

			vertIndex+=3;
		}

		foreach((Transform leafTransform, float distance) in leaves)
		{
			leafTransform.localScale = Vector3.one * Mathf.Clamp01(t * 2 - distance) * ((leafScale - leafScaleMin) * (1-distance) + leafScaleMin);
		}

		mesh.SetVertices(vertices);
	}

	public void CombineLeafMeshes()
	{
		List<CombineInstance> combine = new List<CombineInstance>();

		foreach((Transform t, float f) in leaves)
		{
			if(t == leafChild) continue;
			MeshFilter mf = t.GetComponent<MeshFilter>();
			if(mf == null) continue;

			Mesh meshCopy = Instantiate(mf.sharedMesh);

			CombineInstance ci = new CombineInstance();
			ci.mesh = meshCopy;
			ci.transform = t.localToWorldMatrix;
			combine.Add(ci);
		}

		Mesh combinedMesh = new Mesh();
		combinedMesh.CombineMeshes(combine.ToArray());
		leafChild.position = Vector3.zero;

		leafChild.GetComponent<MeshFilter>().mesh = combinedMesh;

		foreach((Transform t, float f) in leaves)
		{
			if(t == leafChild) continue; 
			Destroy(t.gameObject);
		}
		foreach(CombineInstance ci in combine)
		{
			Destroy(ci.mesh);
		}
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
		if(vine.endSize <= endSize) return;

		Vector3 childStartPoint = vine.start + vine.direction * vine.length;

		if(Random.value > splitFactor && random) vine.AddChild(GenerateVineSegment(vine.normal, RandomRotation(vine.normal, randomDeg) * vine.direction, childStartPoint, vine.endSize));
		else
		{
			vine.AddChild(GenerateVineSegment(vine.normal, RandomRotation(vine.normal, 20, 40) * vine.direction, childStartPoint, vine.endSize)); 
			vine.AddChild(GenerateVineSegment(vine.normal, RandomRotation(vine.normal, -40, -20) * vine.direction, childStartPoint, vine.endSize));
		}

		//if(Random.value < 0.03f) vine.AddChild(GenerateVineSegment(vine.normal, RandomRotation(vine.normal, 70, 90) * vine.direction, childStartPoint, vine.endSize - deltaSizePerUnit));

		foreach(VineSegment child in vine.children)
		{
			VineSegment lastChild = child;
			while(lastChild.children.Count > 0) lastChild = lastChild.children[0];
			PropagateVines(lastChild, remainingLength - lastChild.length);
		}
	}

	VineSegment? GenerateVineSegment(Vector3 normal, Vector3 direction, Vector3 start, float startSize) //Refactor to take in length and spawn the second half of a split one using this function
	{
		normal.Normalize();
		direction.Normalize();
		Vector3 worldStart = start + transform.position;
		float raycastOffset = segLength / 10;
		RaycastHit hit;

		//Debug.DrawLine(worldStart + normal * raycastOffset, worldStart + normal * raycastOffset + direction * segLength, Color.red, 10f);
		//Debug.DrawLine(worldStart + normal * raycastOffset + direction * segLength, worldStart + normal * raycastOffset + direction * segLength - normal * raycastOffset * 2, Color.green, 10f);
		//Debug.DrawLine(worldStart - normal * raycastOffset + direction * segLength, worldStart - normal * raycastOffset + direction * segLength - direction * segLength, Color.blue, 10f);

		if(Physics.Raycast(worldStart + normal * raycastOffset, direction, out hit, segLength))
		{
			//print("Hit smthg");
			//print(worldStart);


			(Vector3 point, Vector3 vector) edge = GetPlaneIntersection(worldStart, normal, hit.point + hit.normal * zFightOffset, hit.normal);

			Vector3 intersection = GetLineIntersection(worldStart, direction, edge.point, edge.vector);

			//print(intersection);

			float newLength = Vector3.Distance(worldStart, intersection);

			//print(newLength);

			//Generate vine with end points along the edge
			float endPointOffset = (startSize - deltaSizePerUnit * newLength) / Vector3.Cross(direction, edge.vector).magnitude;
			Vector3 endPointA = start + direction * newLength + edge.vector * endPointOffset / 2 + hit.normal * zFightOffset;
			Vector3 endPointB = start + direction * newLength - edge.vector * endPointOffset / 2 + hit.normal * zFightOffset;
			Vector3 endPointC = start + direction * newLength + normal * endPointOffset / 4 + hit.normal * (zFightOffset + endPointOffset / 4);

			VineSegment vine = new VineSegment(normal, direction, start, startSize, startSize - deltaSizePerUnit * newLength, newLength, endPointA, endPointB, endPointC); //Use overloaded constructor

			// Compute rotation angle
			float angle = Vector3.SignedAngle(normal, hit.normal, edge.vector);
			// Rotate the vector
			Vector3 newDirection = Quaternion.AngleAxis(angle, edge.vector) * direction;

			vine.children.Add(new VineSegment(hit.normal, newDirection, intersection - transform.position + hit.normal * zFightOffset, startSize, startSize - deltaSizePerUnit * segLength, segLength - newLength));

			return vine;
		}
		else if(Physics.Raycast(worldStart + normal * raycastOffset + direction * segLength, -normal, out hit, raycastOffset * 2))
		{
			return new VineSegment(normal, direction, start, startSize, startSize - deltaSizePerUnit * segLength, segLength);
		}
		else if(Physics.Raycast(worldStart - normal * raycastOffset + direction * segLength, -direction, out hit, segLength))
		{
			//print("Hit smthg - but different");
			//print(worldStart);

			(Vector3 point, Vector3 vector) edge = GetPlaneIntersection(worldStart, normal, hit.point + hit.normal * zFightOffset, hit.normal);

			Vector3 intersection = GetLineIntersection(worldStart, direction, edge.point, edge.vector);

			//print(intersection);

			float newLength = Vector3.Distance(worldStart, intersection);

			//print(newLength);

			float endPointOffset = (startSize - deltaSizePerUnit * newLength) / Vector3.Cross(direction, edge.vector).magnitude;
			Vector3 endPointA = start + direction * newLength - edge.vector * endPointOffset / 2 + hit.normal * zFightOffset;
			Vector3 endPointB = start + direction * newLength + edge.vector * endPointOffset / 2 + hit.normal * zFightOffset;
			Vector3 endPointC = start + direction * newLength + normal * endPointOffset / 4 + hit.normal * (zFightOffset + endPointOffset / 4);

			VineSegment vine = new VineSegment(normal, direction, start, startSize, startSize - deltaSizePerUnit * newLength, newLength, endPointA, endPointB, endPointC); //Use overloaded constructor

			// Compute rotation angle
			float angle = Vector3.SignedAngle(normal, hit.normal, edge.vector);
			// Rotate the vector
			Vector3 newDirection = Quaternion.AngleAxis(angle, edge.vector) * direction;

			vine.AddChild(new VineSegment(hit.normal, newDirection, intersection - transform.position + hit.normal * zFightOffset, startSize, startSize - deltaSizePerUnit * segLength, segLength - newLength)); //Should be recalling this function

			return vine;
		}
		print("HELP IDK WHAT HAPPENED");
		return null;
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
	public Vector3 endPointA;
	public Vector3 endPointB;
	public Vector3 endPointC;
	public List<VineSegment> children;

	public VineSegment(Vector3 normal, Vector3 direction, Vector3 start, float startSize, float endSize, float length, Vector3 endPointA, Vector3 endPointB, Vector3 endPointC)
	{
		this.normal=normal;
		this.direction=direction;
		this.start=start;
		this.startSize=startSize;
		this.endSize=endSize;
		this.length=length;
		this.endPointA=endPointA;
		this.endPointB=endPointB;
		this.endPointC=endPointC;
		children = new List<VineSegment>();
	}

	public VineSegment(Vector3 normal, Vector3 direction, Vector3 start, float startSize, float endSize, float length)
	{
		this.normal=normal;
		this.direction=direction;
		this.start=start;
		this.startSize=startSize;
		this.endSize=endSize;
		this.length=length;

		endPointA = start + direction * length - Vector3.Cross(normal, direction).normalized * endSize / 2;
		endPointB = start + direction * length + Vector3.Cross(normal, direction).normalized * endSize / 2;
		endPointC = start + direction * length + normal * endSize / 4;
		children = new List<VineSegment>();
	}

	public void AddChild(VineSegment? child)
	{
		if(child != null) children.Add((VineSegment)child);
	}
}

public struct VineMeshPiece
{
	public int startAIndex;
	public int startBIndex;
	public int startCIndex;
	public VineSegment vine;

	public VineMeshPiece(int startAIndex, int startBIndex, int startCIndex, VineSegment vine)
	{
		this.startAIndex=startAIndex;
		this.startBIndex=startBIndex;
		this.startCIndex=startCIndex;
		this.vine=vine;
	}
}