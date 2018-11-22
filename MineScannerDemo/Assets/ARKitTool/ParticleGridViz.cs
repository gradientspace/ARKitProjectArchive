using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.iOS;
using g3;
using f3;

public class ParticleGridViz : MonoBehaviour 
{

	public ParticleGrid grid;

	// temp...
	public ParticleSystem pointCloudParticlePrefab;
	public float particleSize = 1.0f;
	private ParticleSystem currentPS;

	bool frameUpdated;
	Vector3[] pointSamples;
	private float last_particles_update = 0;

	private List<fMeshGameObject> CurrentMeshGOs;
	private float last_mesh_update = 0;

	public float MaxSampleDistanceM = 5.0f;  // 

	public int MinSamples = 25;


	private string ground_anchor_id;

	// Use this for initialization
	void Start () {
		UnityARSessionNativeInterface.ARAnchorAddedEvent += ARAnchorAdded;
		UnityARSessionNativeInterface.ARAnchorUpdatedEvent += ARAnchorUpdated;

		UnityARSessionNativeInterface.ARFrameUpdatedEvent += ARFrameUpdated;
		frameUpdated = false;

		currentPS = Instantiate(pointCloudParticlePrefab);

		reset();
	}



	void reset() {
		grid = new ParticleGrid() {
			GridStepSize = gsGlobal.GridSize,
			Origin = (gsGlobal.GroundY - gsGlobal.GridSize / 2.0f) * Vector3f.AxisY
		};
		if (gsGlobal.GridSize <= 0.02f)
			MaxSampleDistanceM = 0.5f;
		else if (gsGlobal.GridSize <= 0.05f)
			MaxSampleDistanceM = 1.0f;
		else if (gsGlobal.GridSize <= 0.25f)
			MaxSampleDistanceM = 2.0f;
		else
			MaxSampleDistanceM = 10.0f;

		clear_mesh();
		currentPS.Clear(true);
	}


	void ARAnchorAdded(ARPlaneAnchor arAnchor) {
		if (gsGlobal.GroundY == 0) {
			Vector3 planePos = UnityARMatrixOps.GetPosition(arAnchor.transform);
			gsGlobal.GroundY = planePos.y;
			ground_anchor_id = arAnchor.identifier;
		}
	}
	void ARAnchorUpdated(ARPlaneAnchor arAnchor) {
		if ( arAnchor.identifier == ground_anchor_id ) {
			Vector3 planePos = UnityARMatrixOps.GetPosition(arAnchor.transform);
			gsGlobal.GroundY = planePos.y;			
		}
	}



	void ARFrameUpdated(UnityARCamera arCamera)
	{
		pointSamples = arCamera.pointCloudData;
		frameUpdated = true;
	}


	// for playback hack? should refactor splat_to_grid...
	public void InjectSample(Vector3f pt, Colorf color) {
		Vector3f camPos = Camera.main.gameObject.transform.position;
		if (pt.Distance(camPos) < MaxSampleDistanceM)
			grid.AddParticle(pt, color);
	}


	void splat_to_grid(Vector3[] points) {
		Vector3f camPos = Camera.main.gameObject.transform.position;
		UnityARVideo video = Camera.main.gameObject.GetComponent<UnityARVideo>();

		for (int pi = 0; pi < points.Length; ++pi ) {
			Vector3f pt = points[pi];
			if (pt.Distance(camPos) > MaxSampleDistanceM)
				continue;

			Vector3f screenCoords = Camera.main.WorldToScreenPoint(pt);
			Colorf c = Colorf.White;
			if (video != null) {
				c = video.QueryPixel(screenCoords.x, screenCoords.y);
			}

			grid.AddParticle(points[pi], c);
		}
	}
	void do_frame_update(Vector3[] points) {
		splat_to_grid(points);
	}


	// Update is called once per frame
	void Update () {
		if (gsGlobal.ResetPending)
			reset();

		if (MinSamples != gsGlobal.MinSampleCount) {
			MinSamples = gsGlobal.MinSampleCount;
			last_particles_update = 0;
			last_mesh_update = 0;
		}

		if (gsGlobal.EnableTracking && frameUpdated) {
			try {
				do_frame_update(pointSamples);
			}catch(Exception e){
				string sMessage = "Exception in splat_to_grid()!\n" + e.Message + "\n" + e.StackTrace;
				Debug.Log(sMessage);
				gsGlobal.TextErrorHandlerF(sMessage);
			}
			frameUpdated = false;
		}

		if (gsGlobal.EnableTracking && Time.realtimeSinceStartup - last_particles_update > 1) {
			try {
				update_particles();
			} catch (Exception e) {
				string sMessage = "Exception in update_particles()!\n" + e.Message + "\n" + e.StackTrace;
				Debug.Log(sMessage);
				gsGlobal.TextErrorHandlerF(sMessage);
			}
			last_particles_update = Time.realtimeSinceStartup;
		}
		currentPS.gameObject.SetVisible(gsGlobal.ShowDots);


		if (gsGlobal.EnableTracking && Time.realtimeSinceStartup - last_mesh_update > 2.5) {
			try {
				update_mesh();
			} catch (Exception e) {
				string sMessage = "Exception in update_mesh()!\n" + e.Message + "\n" + e.StackTrace;
				Debug.Log(sMessage);
				gsGlobal.TextErrorHandlerF(sMessage);
			}
			last_mesh_update = Time.realtimeSinceStartup;
		}
		set_mesh_visible(gsGlobal.ShowMesh);

	}




	void update_particles() {
		int count = grid.GridPointCount(MinSamples);
		ParticleSystem.Particle[] particles = new ParticleSystem.Particle[count];

		Vector3f camPos = Camera.main.transform.position;
		int index = 0;
		foreach (Vector3f v in grid.GridPoints(MinSamples)) {
			particles[index].position = v;

			// this doesn't work...
			float fDist = camPos.Distance(v);
			float t = MathUtil.Clamp(1.0f - fDist / 2.0f, 0.1f, 1.0f);

			particles[index].startColor = new Colorf(t);
			particles[index].startSize = particleSize;
			index++;
		}
		currentPS.SetParticles(particles, count);		
	}



	void clear_mesh() {
		if ( CurrentMeshGOs != null ) {
			foreach (var go in CurrentMeshGOs)
				go.Destroy();
		}
		CurrentMeshGOs = null;	
	}
	void set_mesh_visible(bool bSet) {
		if (CurrentMeshGOs != null) {
			foreach (var go in CurrentMeshGOs)
				go.SetVisible(bSet);
		}
	}

	void update_mesh() {
		clear_mesh();

		AxisAlignedBox3i bounds = grid.Extents;
		Vector3i minCorner = bounds.Min;
		Vector3f cornerXYZ = grid.ToXYZ(minCorner);

		Bitmap3d bmp;
		try {
			bmp = new Bitmap3d(bounds.Diagonal + Vector3i.One);
		} catch(Exception e) {
			Debug.Log("update_mesh: exception allocating grid of size " + bounds.Diagonal);
			throw e;
		}

		foreach ( Vector3i idx in grid.GridIndices(MinSamples) ) {
			Vector3i bidx = idx - minCorner;
			try {
				bmp.Set(bidx, true);
			} catch (Exception e) {
				Debug.Log("bad index is " + bidx + "  grid dims " + bmp.Dimensions);
				throw e;
			}
		}

		// get rid of one-block tubes, floaters, etc. 
		// todo: use a queue instead of passes? or just descend into
		//  nbrs when changing one block? one pass to compute counts and
		//  then another to remove? (yes that is a good idea...)
		bmp.Filter(2);
		bmp.Filter(2);
		bmp.Filter(2);
		bmp.Filter(2);
		bmp.Filter(2);
		bmp.Filter(2);


		VoxelSurfaceGenerator gen = new VoxelSurfaceGenerator() {
			Voxels = bmp, Clockwise = false,
			MaxMeshElementCount = 65000,
			ColorSourceF = (idx) => {
				idx = idx + minCorner;
				return grid.GetColor(idx);
			}
		};
		gen.Generate();
		List<DMesh3> meshes = gen.Meshes;

		List<fMeshGameObject> newMeshGOs = new List<fMeshGameObject>();
		foreach (DMesh3 mesh in meshes) {
			MeshTransforms.Scale(mesh, grid.GridStepSize);
			MeshTransforms.Translate(mesh, cornerXYZ);

			Mesh m = UnityUtil.DMeshToUnityMesh(mesh, false);
			fMeshGameObject meshGO = GameObjectFactory.CreateMeshGO("gridmesh", m, false, true);
			meshGO.SetMaterial(MaterialUtil.CreateStandardVertexColorMaterialF(Colorf.White));
			newMeshGOs.Add(meshGO);
		}

		CurrentMeshGOs = newMeshGOs;
	}

}
